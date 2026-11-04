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
    // Ids of the automation upgrades the player has switched off, comma separated.
    [Serialize] private string automationOffText = "";
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
    // Tiers 4 to 7 (see docs/spec-tiers-4-7.md).
    [Serialize] private int scaleTier4;
    [Serialize] private int scaleTier5;
    [Serialize] private int scaleTier6;
    [Serialize] private int scaleTier7;
    [Serialize] private double bonusTier3;
    [Serialize] private double bonusTier4;
    [Serialize] private double bonusTier5;
    [Serialize] private double bonusTier6;
    [Serialize] private double activityTier4;
    [Serialize] private double activityTier5;
    [Serialize] private double activityTier6;
    [Serialize] private double activityTier7;
    [Serialize] private bool tier4Unlocked;
    [Serialize] private bool tier5Unlocked;
    [Serialize] private bool tier6Unlocked;
    [Serialize] private bool tier7Unlocked;
    // Singularity (see docs/spec-singularity.md). Critical Constant (CC) and its expansions survive a singularity.
    [Serialize] private int singularityCount;
    [Serialize] private int criticalConstant;
    [Serialize] private int scaleCcProduction;
    [Serialize] private int scaleCcMi;
    // Bit i is set once tutorial dialog i has been shown. tutorialStage is the old counter, kept for old saves.
    [Serialize] private int tutorialFlags;

    // Per tier values, indexed by tier (index 0 unused). Copied to and from the serialized fields above.
    private readonly int[] scales = new int[KnowledgeRules.MaxTier + 1];
    private readonly double[] bonuses = new double[KnowledgeRules.MaxTier + 1];
    private readonly double[] activities = new double[KnowledgeRules.MaxTier + 1];
    private readonly bool[] unlocked = new bool[KnowledgeRules.MaxTier + 1];

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
    private readonly HashSet<string> automationOff = new HashSet<string>();
    private const int MaxAutoPurchasesPerTick = KnowledgeRules.MaxExpansionsPerAction;

    public BigInteger KnowledgeScaled => knowledge;
    public int ScaleTier1 => scales[1];
    // The facility itself counts as the first unit; expansions add to it.
    public int HoldingsTier1 => scales[1] + 1;
    public double BaseRate => baseRate;
    public double MultiplierTier1 => TierMultiplier(1);

    // Everything that multiplies the production of a tier except the built-up performance.
    public double TierMultiplier(int tier)
    {
        return ProductionMultiplier(tier) * MetaInsightMultiplier * SchoolMultiplier(tier) * CcProductionMultiplier;
    }

    public int SingularityCount => singularityCount;
    public int CriticalConstant => criticalConstant;
    public int ScaleCcProduction => scaleCcProduction;
    public int ScaleCcMi => scaleCcMi;
    public double CcProductionMultiplier => Math.Pow(KnowledgeRules.CcProductionStep, scaleCcProduction);
    public double CcMiGainMultiplier => Math.Pow(KnowledgeRules.CcMiGainStep, scaleCcMi);

    public bool IsTierUnlocked(int tier)
    {
        return tier == 1 || (tier > 1 && tier <= KnowledgeRules.MaxTier && unlocked[tier]);
    }

    public int GetScale(int tier)
    {
        return scales[tier];
    }

    public int Holdings(int tier)
    {
        return GetScale(tier) + 1;
    }

    public double Bonus(int tier)
    {
        return bonuses[tier];
    }

    // Activity of the facility of a tier (tier 2 and 3).
    public double Activity(int tier)
    {
        return activities[tier];
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

    public BigInteger MetaInsightGain
    {
        get
        {
            var gain = KnowledgeRules.MiGain(produced);
            if (scaleCcMi > 0 && gain.Sign > 0)
            {
                gain = gain * new BigInteger(Math.Round(CcMiGainMultiplier * 1000d)) / 1000;
            }

            return gain;
        }
    }

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
    public bool Unlocked => researchComplete || knowledge > 0 || scales[1] > 0 || shiftCount > 0 ||
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
        scaleTier1 = scales[1];
        scaleTier2 = scales[2];
        scaleTier3 = scales[3];
        scaleTier4 = scales[4];
        scaleTier5 = scales[5];
        scaleTier6 = scales[6];
        scaleTier7 = scales[7];
        bonusTier1 = bonuses[1];
        bonusTier2 = bonuses[2];
        bonusTier3 = bonuses[3];
        bonusTier4 = bonuses[4];
        bonusTier5 = bonuses[5];
        bonusTier6 = bonuses[6];
        activityTier2 = activities[2];
        activityTier3 = activities[3];
        activityTier4 = activities[4];
        activityTier5 = activities[5];
        activityTier6 = activities[6];
        activityTier7 = activities[7];
        tier2Unlocked = unlocked[2];
        tier3Unlocked = unlocked[3];
        tier4Unlocked = unlocked[4];
        tier5Unlocked = unlocked[5];
        tier6Unlocked = unlocked[6];
        tier7Unlocked = unlocked[7];
        knowledgeText = knowledge.ToString();
        upgradesText = string.Join(",", upgrades);
        automationOffText = string.Join(",", automationOff);
        producedText = produced.ToString();
        metaInsightText = metaInsight.ToString();
        metaInsightEarnedText = metaInsightEarned.ToString();
    }

    [OnDeserialized]
    private void OnDeserialized()
    {
        scales[1] = scaleTier1;
        scales[2] = scaleTier2;
        scales[3] = scaleTier3;
        scales[4] = scaleTier4;
        scales[5] = scaleTier5;
        scales[6] = scaleTier6;
        scales[7] = scaleTier7;
        bonuses[1] = bonusTier1;
        bonuses[2] = bonusTier2;
        bonuses[3] = bonusTier3;
        bonuses[4] = bonusTier4;
        bonuses[5] = bonusTier5;
        bonuses[6] = bonusTier6;
        activities[2] = activityTier2;
        activities[3] = activityTier3;
        activities[4] = activityTier4;
        activities[5] = activityTier5;
        activities[6] = activityTier6;
        activities[7] = activityTier7;
        unlocked[2] = tier2Unlocked;
        unlocked[3] = tier3Unlocked;
        unlocked[4] = tier4Unlocked;
        unlocked[5] = tier5Unlocked;
        unlocked[6] = tier6Unlocked;
        unlocked[7] = tier7Unlocked;

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

        automationOff.Clear();
        foreach (var id in (automationOffText ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            automationOff.Add(id);
        }

        // Saves that counted the dialogs shown so far.
        if (tutorialFlags == 0 && tutorialStage > 0)
        {
            tutorialFlags = (1 << tutorialStage) - 1;
        }

        // Saves from before the tutorial existed have already played through it.
        if (tutorialFlags == 0 && (scales[1] > 0 || knowledge > 0))
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

    // An automation is active when it is bought and the player has not switched it off.
    public bool IsAutomationActive(string id)
    {
        return HasUpgrade(id) && !automationOff.Contains(id);
    }

    public bool IsAutomationEnabled(string id)
    {
        return !automationOff.Contains(id);
    }

    public void ToggleAutomation(string id)
    {
        if (!automationOff.Remove(id))
        {
            automationOff.Add(id);
        }
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
        switch (currency)
        {
            case UpgradeCurrency.Insight: return knowledge;
            case UpgradeCurrency.MetaInsight: return metaInsight;
            default: return criticalConstant;
        }
    }

    // Visible once the tier has been expanded and the colony has shifted enough, and the required upgrade is bought.
    public bool IsUpgradeVisible(UpgradeDef def)
    {
        return !HasUpgrade(def.Id) && GetScale(def.IsGlobal ? 1 : def.Tier) >= def.RequiredExpansions && shiftCount >= def.RequiredShifts &&
               singularityCount >= def.RequiredSingularities &&
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
        else if (def.Currency == UpgradeCurrency.MetaInsight)
        {
            metaInsight -= def.CostAmount;
        }
        else
        {
            criticalConstant -= (int)def.CostAmount;
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

    public void DebugAddCriticalConstant(int amount)
    {
        criticalConstant += amount;
    }

    // Debug: while set, production is infinite. Not saved, so it ends with the session.
    public bool DebugInfiniteProduction { get; private set; }

    public void DebugToggleInfiniteProduction()
    {
        DebugInfiniteProduction = !DebugInfiniteProduction;
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

    // Critical Constant expansions: kind 0 raises production, kind 1 raises the Meta-Insight gained by a shift.
    public int NextCcExpansionCost(int kind)
    {
        return KnowledgeRules.CcExpansionCost(kind == 0 ? scaleCcProduction : scaleCcMi);
    }

    public bool CanExpandCc(int kind)
    {
        return criticalConstant >= NextCcExpansionCost(kind);
    }

    public bool TryExpandCc(int kind)
    {
        var cost = NextCcExpansionCost(kind);
        if (criticalConstant < cost)
        {
            return false;
        }

        criticalConstant -= cost;
        if (kind == 0)
        {
            scaleCcProduction++;
        }
        else
        {
            scaleCcMi++;
        }

        return true;
    }

    // The singularity is shown once the last blueprint is unlocked, or once the colony has had one.
    public bool SingularityVisible => unlocked[KnowledgeRules.MaxTier] || singularityCount > 0;
    public int DatabanksRequired => KnowledgeRules.DatabanksRequired(singularityCount);
    public int DatabanksStored => RecordArchive.DatabanksStored;
    public int NextCcGain => KnowledgeRules.CcGain(singularityCount);

    public bool SingularityConditionsMet =>
        unlocked[KnowledgeRules.MaxTier] && shiftCount >= KnowledgeRules.SingularityMinShifts;

    public bool CanSingularity => SingularityConditionsMet && DatabanksStored >= DatabanksRequired;

    // Singularity: resets the whole idle game except Critical Constant, its expansions and its upgrades.
    public bool TrySingularity()
    {
        if (!CanSingularity || !RecordArchive.ConsumeDatabanks(DatabanksRequired))
        {
            return false;
        }

        criticalConstant += NextCcGain;
        singularityCount++;
        for (var tier = 1; tier <= KnowledgeRules.MaxTier; tier++)
        {
            scales[tier] = 0;
            bonuses[tier] = 0d;
            if (tier > 1)
            {
                unlocked[tier] = false;
            }
        }

        knowledge = BigInteger.Zero;
        produced = BigInteger.Zero;
        knowledgeRemainder = 0d;
        metaInsight = BigInteger.Zero;
        metaInsightEarned = BigInteger.Zero;
        shiftCount = 0;
        scaleMi = 0;
        schoolId = "";
        foreach (var def in Upgrades.All)
        {
            if (def.Currency != UpgradeCurrency.CriticalConstant)
            {
                upgrades.Remove(def.Id);
            }
        }

        RefreshBuildMenu();
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

        for (var tier = 1; tier <= KnowledgeRules.MaxTier; tier++)
        {
            scales[tier] = 0;
            bonuses[tier] = 0d;
        }

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
        scales[tier]++;

        return true;
    }

    // Expands up to the given number of times (or as often as affordable); returns how many were bought.
    public int TryExpandMany(int tier, int maxCount)
    {
        maxCount = Math.Min(maxCount, KnowledgeRules.MaxExpansionsPerAction);
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

        AddProduction(DebugInfiniteProduction ? double.PositiveInfinity : ProductionTier1 * KnowledgeRules.Scale * dt);

        if (IsAutomationActive(Upgrades.AutoExpandMi))
        {
            for (var i = 0; i < MaxAutoPurchasesPerTick && TryExpandMi(); i++)
            {
            }
        }

        if (IsAutomationActive(Upgrades.AutoUpgrade))
        {
            foreach (var def in Upgrades.All)
            {
                if (def.Currency == UpgradeCurrency.Insight)
                {
                    TryBuyUpgrade(def);
                }
            }
        }

        if (IsAutomationActive(Upgrades.AutoMiUpgrade))
        {
            foreach (var def in Upgrades.All)
            {
                if (def.Currency == UpgradeCurrency.MetaInsight)
                {
                    TryBuyUpgrade(def);
                }
            }
        }

        // Upgrades come first, then expansions with what is left.
        AutoExpand();

        if (IsAutomationActive(Upgrades.AutoExpandCc))
        {
            for (var i = 0; i < MaxAutoPurchasesPerTick; i++)
            {
                // The cheaper of the two expansions first, so that neither starves the other.
                var kind = NextCcExpansionCost(0) <= NextCcExpansionCost(1) ? 0 : 1;
                if (!TryExpandCc(kind) && !TryExpandCc(1 - kind))
                {
                    break;
                }
            }
        }

        if (IsAutomationActive(Upgrades.AutoShift) && CanShift &&
            MetaInsightGain * KnowledgeRules.AutoShiftDivisor >= metaInsightEarned)
        {
            TryShift(schoolId);
        }

        var newIdleMode = researchComplete && RecordArchive.AnyOperational;
        if (newIdleMode != idleMode)
        {
            idleMode = newIdleMode;
            RefreshResearchCenters();
        }
    }

    // Expands the tiers that have automation, always the cheapest affordable expansion first, so that no tier
    // starves the others.
    private void AutoExpand()
    {
        for (var i = 0; i < MaxAutoPurchasesPerTick; i++)
        {
            var best = 0;
            BigInteger bestCost = BigInteger.Zero;
            foreach (var def in Upgrades.All)
            {
                if (def.Effect != UpgradeEffect.AutoExpandInsight || !IsAutomationActive(def.Id) ||
                    !CanExpand(def.Tier))
                {
                    continue;
                }

                var cost = NextExpansionCost(def.Tier);
                if (best == 0 || cost < bestCost)
                {
                    best = def.Tier;
                    bestCost = cost;
                }
            }

            if (best == 0 || !TryExpand(best))
            {
                return;
            }
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

        for (var tier = 2; tier <= KnowledgeRules.MaxTier; tier++)
        {
            if (!unlocked[tier] && scales[tier - 1] >= KnowledgeRules.UnlockExpansions(tier))
            {
                unlocked[tier] = true;
                RefreshBuildMenu();
            }

            activities[tier] = ApproachActivity(activities[tier], TierFacility.AnyOperational(tier), dt);
        }

        // The built-up performance only grows while the facility runs, and the lower the activity, the slower.
        for (var tier = 2; tier <= KnowledgeRules.MaxTier; tier++)
        {
            bonuses[tier - 1] += dt * BonusRate(tier);
        }
    }

    // Performance per second that the facility of a tier (2 and up) builds up for the tier below, as things stand.
    public double BonusRate(int tier)
    {
        // The Meta-Insight multiplier is deliberately left out: it already multiplies the production of the
        // tier below, and counting it here as well would make it compound with itself and run away.
        // A tier that has a tier above it is itself raised by that tier's performance.
        return KnowledgeRules.BonusRate(tier) * EffectiveBaseRate * Holdings(tier) * BuildUpMultiplier(tier) *
               Performance(tier) * activities[tier];
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
            case 12: return SingularityConditionsMet;
            default: return unlocked[stage - 4];
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
