using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;
using Database;
using IncrementalResearch.Ui;
using KSerialization;

namespace IncrementalResearch.Core;

// Colony-wide idle game state, stored in the save game.
[SerializationConfig(MemberSerialization.OptIn)]
public sealed class KnowledgeState : KMonoBehaviour, ISim200ms
{
    public static KnowledgeState Instance { get; private set; }

    // Insight in thousandths (KnowledgeRules.Scale). Saved as text since the value has no size limit.
    [Serialize] private string knowledgeText = "0";
    // Number of expansions bought for the tier 1 facility.
    [Serialize] private int scaleTier1;
    // Moving average of the weighted research output (points per second).
    [Serialize] private double baseRate;
    // Number of tutorial dialogs already shown (see Ui/TutorialDialog).
    [Serialize] private int tutorialStage;
    // Ids of the purchased upgrades, comma separated.
    [Serialize] private string upgradesText = "";
    // Paradigm shift (see docs/spec-paradigm-shift.md). Big numbers are saved as text.
    [Serialize] private string producedText = "0";
    [Serialize] private string metaInsightText = "0";
    [Serialize] private string metaInsightEarnedText = "0";
    [Serialize] private int shiftCount;
    [Serialize] private int scaleMi;
    // Id of the school chosen at the last paradigm shift, empty for none.
    [Serialize] private string schoolId = "";
    // Tiers 2 and 3 (see docs/spec-tiers-2-3.md). bonusN is the performance built up for tier N by tier N+1;
    // activityN is how active the facility of tier N is (0 to 1), which scales the bonus it gives.
    [Serialize] private int scaleTier2;
    [Serialize] private int scaleTier3;
    [Serialize] private double bonusTier1;
    [Serialize] private double bonusTier2;
    [Serialize] private double activityTier2;
    [Serialize] private double activityTier3;
    [Serialize] private bool tier2Unlocked;
    [Serialize] private bool tier3Unlocked;
    // Bit i is set once tutorial dialog i has been shown. tutorialStage is the old counter, kept for old saves.
    [Serialize] private int tutorialFlags;

    private BigInteger knowledge = BigInteger.Zero;
    // Insight produced in the current cycle, in thousandths. Decides the Meta-Insight gained by a shift.
    private BigInteger produced = BigInteger.Zero;
    private BigInteger metaInsight = BigInteger.Zero;
    private BigInteger metaInsightEarned = BigInteger.Zero;
    private double knowledgeRemainder;
    private float pendingPoints;
    private float researchCompleteCheckTimer;
    private bool researchComplete;
    private bool idleMode;
    private bool tutorialDialogOpen;
    private bool buildMenuRefreshed;
    private readonly HashSet<string> upgrades = new HashSet<string>();

    public BigInteger KnowledgeScaled => knowledge;
    public int ScaleTier1 => scaleTier1;
    // The facility itself counts as the first unit; expansions add to it.
    public int HoldingsTier1 => scaleTier1 + 1;
    public double BaseRate => baseRate;
    public double MultiplierTier1 => TierMultiplier(1);

    // Everything that multiplies the production of a tier except the built-up performance.
    public double TierMultiplier(int tier)
    {
        return ProductionMultiplier(tier) * MetaInsightMultiplier * SchoolMultiplier(tier);
    }

    public bool IsTierUnlocked(int tier)
    {
        return tier == 1 || (tier == 2 ? tier2Unlocked : tier3Unlocked);
    }

    public int GetScale(int tier)
    {
        return tier == 1 ? scaleTier1 : tier == 2 ? scaleTier2 : scaleTier3;
    }

    public int Holdings(int tier)
    {
        return GetScale(tier) + 1;
    }

    public double Bonus(int tier)
    {
        return tier == 1 ? bonusTier1 : bonusTier2;
    }

    // Activity of the facility of a tier (tier 2 and 3).
    public double Activity(int tier)
    {
        return tier == 2 ? activityTier2 : activityTier3;
    }

    // Performance of a tier given by the facility above it: 1 plus the built-up bonus scaled by that activity.
    public double Performance(int tier)
    {
        return tier >= KnowledgeRules.MaxTier ? 1d : 1d + Activity(tier + 1) * Bonus(tier);
    }

    public bool SchoolsUnlocked => shiftCount >= Schools.UnlockShifts;
    public SchoolDef CurrentSchool => Schools.Find(schoolId);

    public double SchoolMultiplier(int tier)
    {
        var school = CurrentSchool;
        return school == null ? 1d : school.Multiplier(tier);
    }

    public BigInteger MetaInsight => metaInsight;
    public BigInteger MetaInsightEarned => metaInsightEarned;
    public int ShiftCount => shiftCount;
    public int ScaleMi => scaleMi;
    public BigInteger ProducedScaled => produced;

    // Production multiplier from the Meta-Insight earned so far. Spending Meta-Insight does not lower it.
    public double MetaInsightMultiplier => 1d + KnowledgeRules.MiMultiplierBonus(
        KnowledgeRules.MiMultiplierBase + KnowledgeRules.MiMultiplierPerExpansion * scaleMi, metaInsightEarned);

    // Rate the idle game runs on; see KnowledgeRules.BaseRateReference.
    private double EffectiveBaseRate => KnowledgeRules.EffectiveBaseRate(baseRate);

    public BigInteger MetaInsightGain => KnowledgeRules.MiGain(produced);
    public bool CanShift => MetaInsightGain >= 1;
    public BigInteger NextMiExpansionCost => KnowledgeRules.MiExpansionCost(scaleMi);

    public double ProductionTier1 =>
        RecordArchive.AnyOperational ? EffectiveBaseRate * HoldingsTier1 * MultiplierTier1 * Performance(1) : 0d;

    public BigInteger NextExpansionCostScaled => NextExpansionCost(1);

    public BigInteger NextExpansionCost(int tier)
    {
        return KnowledgeRules.TierExpansionCost(tier, GetScale(tier), ExpansionCostGrowth(tier),
            ExpansionCostFactor(tier));
    }

    private double ExpansionCostFactor(int tier)
    {
        var factor = 1d;
        foreach (var def in Upgrades.All)
        {
            if (def.Effect == UpgradeEffect.ExpansionCostMultiplier && def.Tier == tier && HasUpgrade(def.Id))
            {
                factor *= def.Value;
            }
        }

        return factor;
    }

    public double ExpansionCostGrowth(int tier)
    {
        var growth = KnowledgeRules.BaseGrowth(tier);
        foreach (var def in Upgrades.All)
        {
            if (def.Effect == UpgradeEffect.ExpansionCostGrowth && def.Tier == tier && HasUpgrade(def.Id))
            {
                growth = Math.Min(growth, def.Value);
            }
        }

        return growth;
    }

    // True when there is nothing left to research and an archive keeps the research buildings running.
    public bool IdleMode => idleMode;

    // The Insight tab is shown once the research tree is complete or the idle game has been touched.
    public bool Unlocked => researchComplete || knowledge > 0 || scaleTier1 > 0 || shiftCount > 0 ||
                            RecordArchive.AnyExists;

    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();
        Instance = this;
    }

    protected override void OnCleanUp()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnCleanUp();
    }

    [OnSerializing]
    private void OnSerializing()
    {
        knowledgeText = knowledge.ToString();
        upgradesText = string.Join(",", upgrades);
        producedText = produced.ToString();
        metaInsightText = metaInsight.ToString();
        metaInsightEarnedText = metaInsightEarned.ToString();
    }

    [OnDeserialized]
    private void OnDeserialized()
    {
        if (!BigInteger.TryParse(knowledgeText, out knowledge))
        {
            knowledge = BigInteger.Zero;
        }

        if (!BigInteger.TryParse(producedText, out produced))
        {
            produced = BigInteger.Zero;
        }

        if (!BigInteger.TryParse(metaInsightText, out metaInsight))
        {
            metaInsight = BigInteger.Zero;
        }

        if (!BigInteger.TryParse(metaInsightEarnedText, out metaInsightEarned))
        {
            metaInsightEarned = BigInteger.Zero;
        }

        upgrades.Clear();
        foreach (var id in (upgradesText ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            upgrades.Add(id);
        }

        // Saves that counted the dialogs shown so far.
        if (tutorialFlags == 0 && tutorialStage > 0)
        {
            tutorialFlags = (1 << tutorialStage) - 1;
        }

        // Saves from before the tutorial existed have already played through it.
        if (tutorialFlags == 0 && (scaleTier1 > 0 || knowledge > 0))
        {
            tutorialFlags = (1 << TutorialDialog.StageCount) - 1;
        }
    }

    public void RecordResearchPoints(string researchTypeId, float points)
    {
        pendingPoints += points * KnowledgeRules.PointWeight(researchTypeId);
    }

    public bool HasUpgrade(string id)
    {
        return upgrades.Contains(id);
    }

    // Product of the production multipliers that apply to a tier: its own upgrades and the global ones.
    public double ProductionMultiplier(int tier)
    {
        var result = 1d;
        foreach (var def in Upgrades.All)
        {
            if (def.Effect == UpgradeEffect.ProductionMultiplier && (def.Tier == tier || def.IsGlobal) &&
                HasUpgrade(def.Id))
            {
                result *= def.Value;
            }
        }

        return result;
    }

    private BigInteger Balance(UpgradeCurrency currency)
    {
        return currency == UpgradeCurrency.Insight ? knowledge : metaInsight;
    }

    // Visible once the tier has been expanded and the colony has shifted enough, and the required upgrade is bought.
    public bool IsUpgradeVisible(UpgradeDef def)
    {
        return !HasUpgrade(def.Id) && GetScale(def.IsGlobal ? 1 : def.Tier) >= def.RequiredExpansions && shiftCount >= def.RequiredShifts &&
               (def.RequiredUpgradeId == null || HasUpgrade(def.RequiredUpgradeId));
    }

    public bool CanBuyUpgrade(UpgradeDef def)
    {
        return IsUpgradeVisible(def) && Balance(def.Currency) >= def.CostAmount;
    }

    public int AffordableUpgradeCount()
    {
        var count = 0;
        foreach (var def in Upgrades.All)
        {
            if (def.Currency == UpgradeCurrency.Insight && CanBuyUpgrade(def))
            {
                count++;
            }
        }

        return count;
    }

    public bool TryBuyUpgrade(UpgradeDef def)
    {
        if (!CanBuyUpgrade(def))
        {
            return false;
        }

        if (def.Currency == UpgradeCurrency.Insight)
        {
            Spend(def.CostAmount);
        }
        else
        {
            metaInsight -= def.CostAmount;
        }

        upgrades.Add(def.Id);
        return true;
    }

    // Debug helpers, used by the debug row of the Insight tab (only shown when the game's debug mode is enabled).
    public void DebugAddInsight(BigInteger wholeInsight)
    {
        var added = wholeInsight * KnowledgeRules.Scale;
        knowledge += added;
        produced += added;
    }

    public void DebugAddMetaInsight(int amount)
    {
        metaInsight += amount;
        metaInsightEarned += amount;
    }

    public void DebugResetTutorial()
    {
        tutorialStage = 0;
        tutorialFlags = 0;
    }

    public bool CanExpandMi()
    {
        return metaInsight >= NextMiExpansionCost;
    }

    public bool TryExpandMi()
    {
        var cost = NextMiExpansionCost;
        if (metaInsight < cost)
        {
            return false;
        }

        metaInsight -= cost;
        scaleMi++;
        return true;
    }

    // Paradigm shift: resets Insight, the Insight expansions and the Insight upgrades, and grants Meta-Insight.
    public bool TryShift(string newSchoolId = null)
    {
        var gain = MetaInsightGain;
        if (gain < 1)
        {
            return false;
        }

        metaInsight += gain;
        metaInsightEarned += gain;
        shiftCount++;
        schoolId = SchoolsUnlocked && Schools.Find(newSchoolId) != null ? newSchoolId : "";

        scaleTier1 = 0;
        scaleTier2 = 0;
        scaleTier3 = 0;
        bonusTier1 = 0d;
        bonusTier2 = 0d;
        produced = BigInteger.Zero;
        knowledgeRemainder = 0d;
        var startingKnowledge = 0d;
        foreach (var def in Upgrades.All)
        {
            if (!HasUpgrade(def.Id))
            {
                continue;
            }

            if (def.Currency == UpgradeCurrency.Insight && !def.PersistsOnShift)
            {
                upgrades.Remove(def.Id);
            }
            else if (def.Effect == UpgradeEffect.StartingKnowledge)
            {
                startingKnowledge += def.Value;
            }
        }

        knowledge = new BigInteger(startingKnowledge * KnowledgeRules.Scale);
        return true;
    }

    public bool CanExpandTier1()
    {
        return CanExpand(1);
    }

    public bool TryExpandTier1()
    {
        return TryExpand(1);
    }

    public int TryExpandTier1Many(int maxCount)
    {
        return TryExpandMany(1, maxCount);
    }

    public bool CanExpand(int tier)
    {
        return IsTierUnlocked(tier) && knowledge >= NextExpansionCost(tier);
    }

    public bool TryExpand(int tier)
    {
        if (!IsTierUnlocked(tier))
        {
            return false;
        }

        var cost = NextExpansionCost(tier);
        if (knowledge < cost)
        {
            return false;
        }

        Spend(cost);
        switch (tier)
        {
            case 1: scaleTier1++; break;
            case 2: scaleTier2++; break;
            default: scaleTier3++; break;
        }

        return true;
    }

    // Expands up to the given number of times (or as often as affordable); returns how many were bought.
    public int TryExpandMany(int tier, int maxCount)
    {
        var bought = 0;
        while (bought < maxCount && TryExpand(tier))
        {
            bought++;
        }

        return bought;
    }

    public void Sim200ms(float dt)
    {
        UpdateResearchComplete(dt);
        UpdateTutorial();
        UpdateTiers(dt);

        var alpha = 1d - Math.Exp(-dt / KnowledgeRules.BaseRateTimeConstant);
        baseRate += (pendingPoints / dt - baseRate) * alpha;
        pendingPoints = 0f;

        AddProduction(ProductionTier1 * KnowledgeRules.Scale * dt);

        if (HasUpgrade(Upgrades.AutoExpandMi))
        {
            while (TryExpandMi())
            {
            }
        }

        if (HasUpgrade(Upgrades.AutoUpgrade))
        {
            foreach (var def in Upgrades.All)
            {
                if (def.Currency == UpgradeCurrency.Insight)
                {
                    TryBuyUpgrade(def);
                }
            }
        }

        // Upgrades come first, then expansions with what is left.
        if (HasUpgrade(Upgrades.AutoExpand))
        {
            TryExpandTier1Many(int.MaxValue);
        }

        var newIdleMode = researchComplete && RecordArchive.AnyOperational;
        if (newIdleMode != idleMode)
        {
            idleMode = newIdleMode;
            RefreshResearchCenters();
        }
    }

    // Infinite Insight is held as a fixed huge value: spending leaves it unchanged, and it is shown as "Infinity".
    private void Spend(BigInteger amount)
    {
        if (knowledge >= KnowledgeRules.InfiniteScaled)
        {
            return;
        }

        knowledge -= amount;
    }

    // Adds an amount in thousandths. The part below one thousandth is carried over to the next tick.
    private void AddProduction(double scaledAmount)
    {
        if (double.IsPositiveInfinity(scaledAmount))
        {
            // The rate overflowed a double: the Insight held is infinite as well.
            knowledge = KnowledgeRules.InfiniteScaled;
            produced = KnowledgeRules.InfiniteScaled;
            return;
        }

        if (double.IsNaN(scaledAmount) || scaledAmount <= 0d)
        {
            return;
        }

        if (knowledge >= KnowledgeRules.InfiniteScaled)
        {
            return;
        }

        var exact = scaledAmount + knowledgeRemainder;
        var whole = Math.Floor(exact);
        knowledgeRemainder = exact - whole;
        var added = new BigInteger(whole);
        knowledge += added;
        produced += added;
    }

    // Unlocks the blueprints of the upper tiers, and builds up the performance they give to the tier below.
    private void UpdateTiers(float dt)
    {
        // The build menu may have been built before this state was loaded.
        if (!buildMenuRefreshed)
        {
            buildMenuRefreshed = true;
            RefreshBuildMenu();
        }

        if (!tier2Unlocked && scaleTier1 >= KnowledgeRules.Tier2UnlockExpansions)
        {
            tier2Unlocked = true;
            RefreshBuildMenu();
        }

        if (!tier3Unlocked && scaleTier2 >= KnowledgeRules.Tier3UnlockExpansions)
        {
            tier3Unlocked = true;
            RefreshBuildMenu();
        }

        activityTier2 = ApproachActivity(activityTier2, TierFacility.AnyOperational(2), dt);
        activityTier3 = ApproachActivity(activityTier3, TierFacility.AnyOperational(3), dt);

        // The built-up performance only grows while the facility runs, and the lower the activity, the slower.
        bonusTier1 += dt * BonusRate(2);
        bonusTier2 += dt * BonusRate(3);
    }

    // Performance per second that the facility of a tier (2 or 3) builds up for the tier below, as things stand.
    public double BonusRate(int tier)
    {
        // The Meta-Insight multiplier is deliberately left out: it already multiplies the production of the
        // tier below, and counting it here as well would make it compound with itself and run away.
        if (tier == 2)
        {
            return KnowledgeRules.BonusRateTier2 * EffectiveBaseRate * Holdings(2) * BuildUpMultiplier(2) *
                   Performance(2) * activityTier2;
        }

        return KnowledgeRules.BonusRateTier3 * EffectiveBaseRate * Holdings(3) * BuildUpMultiplier(3) *
               activityTier3;
    }

    private double BuildUpMultiplier(int tier)
    {
        return ProductionMultiplier(tier) * SchoolMultiplier(tier);
    }

    private static double ApproachActivity(double activity, bool running, float dt)
    {
        var target = running ? 1d : 0d;
        var time = running ? KnowledgeRules.ActivityRiseTime : KnowledgeRules.ActivityFallTime;
        return activity + (target - activity) * (1d - Math.Exp(-dt / time));
    }

    private static void RefreshBuildMenu()
    {
        if (PlanScreen.Instance != null)
        {
            PlanScreen.Instance.ForceRefreshAllBuildingToggles();
        }
    }

    private bool StageReached(int stage)
    {
        switch (stage)
        {
            case 0: return researchComplete;
            case 1: return RecordArchive.AnyOperational;
            case 2: return CanExpandTier1();
            case 3: return AffordableUpgradeCount() > 0;
            case 4: return CanShift;
            case 5: return SchoolsUnlocked;
            case 6: return tier2Unlocked;
            default: return tier3Unlocked;
        }
    }

    // Shows the dialog of the first stage that has been reached and not shown yet, one at a time.
    private void UpdateTutorial()
    {
        if (tutorialDialogOpen)
        {
            return;
        }

        for (var stage = 0; stage < TutorialDialog.StageCount; stage++)
        {
            if ((tutorialFlags & (1 << stage)) != 0 || !StageReached(stage))
            {
                continue;
            }

            tutorialFlags |= 1 << stage;
            tutorialDialogOpen = true;
            TutorialDialog.Show(stage, () => tutorialDialogOpen = false);
            return;
        }
    }

    private void UpdateResearchComplete(float dt)
    {
        researchCompleteCheckTimer -= dt;
        if (researchCompleteCheckTimer > 0f)
        {
            return;
        }

        researchCompleteCheckTimer = 5f;
        var complete = true;
        foreach (Tech tech in Db.Get().Techs.resources)
        {
            if (!tech.IsComplete())
            {
                complete = false;
                break;
            }
        }

        researchComplete = complete;
    }

    private static void RefreshResearchCenters()
    {
        foreach (var center in Components.ResearchCenters.Items)
        {
            if (center is ResearchCenter researchCenter)
            {
                researchCenter.Trigger((int)GameHashes.ResearchPointsChanged);
            }
            else if (center is NuclearResearchCenter nuclearCenter)
            {
                // Its state machine only re-evaluates the selected research on this event.
                nuclearCenter.Trigger((int)GameHashes.ActiveResearchChanged);
            }
        }
    }
}
