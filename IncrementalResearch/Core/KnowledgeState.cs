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
    private readonly HashSet<string> upgrades = new HashSet<string>();

    public BigInteger KnowledgeScaled => knowledge;
    public int ScaleTier1 => scaleTier1;
    // The facility itself counts as the first unit; expansions add to it.
    public int HoldingsTier1 => scaleTier1 + 1;
    public double BaseRate => baseRate;
    public double MultiplierTier1 => ProductionMultiplier(1) * MetaInsightMultiplier * SchoolMultiplier(1);

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
    public double MetaInsightMultiplier =>
        1d + (KnowledgeRules.MiMultiplierBase + KnowledgeRules.MiMultiplierPerExpansion * scaleMi) *
        (double)metaInsightEarned;

    public BigInteger MetaInsightGain => KnowledgeRules.MiGain(produced);
    public bool CanShift => MetaInsightGain >= 1;
    public BigInteger NextMiExpansionCost => KnowledgeRules.MiExpansionCost(scaleMi);

    public double ProductionTier1 =>
        RecordArchive.AnyOperational ? baseRate * HoldingsTier1 * MultiplierTier1 : 0d;

    public BigInteger NextExpansionCostScaled => KnowledgeRules.ExpansionCost(scaleTier1, ExpansionCostGrowth, ExpansionCostFactor);

    private double ExpansionCostFactor
    {
        get
        {
            var factor = 1d;
            foreach (var def in Upgrades.All)
            {
                if (def.Effect == UpgradeEffect.ExpansionCostMultiplier && HasUpgrade(def.Id))
                {
                    factor *= def.Value;
                }
            }

            return factor;
        }
    }

    public double ExpansionCostGrowth
    {
        get
        {
            var growth = KnowledgeRules.ScaleCostGrowth;
            foreach (var def in Upgrades.All)
            {
                if (def.Effect == UpgradeEffect.ExpansionCostGrowth && HasUpgrade(def.Id))
                {
                    growth = Math.Min(growth, def.Value);
                }
            }

            return growth;
        }
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

        // Saves from before the tutorial existed have already played through it.
        if (tutorialStage == 0 && (scaleTier1 > 0 || knowledge > 0))
        {
            tutorialStage = TutorialDialog.StageCount;
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
        return !HasUpgrade(def.Id) && scaleTier1 >= def.RequiredExpansions && shiftCount >= def.RequiredShifts &&
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
            knowledge -= def.CostAmount;
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
        produced = BigInteger.Zero;
        knowledgeRemainder = 0d;
        var startingKnowledge = 0d;
        foreach (var def in Upgrades.All)
        {
            if (!HasUpgrade(def.Id))
            {
                continue;
            }

            if (def.Currency == UpgradeCurrency.Insight)
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
        return knowledge >= NextExpansionCostScaled;
    }

    public bool TryExpandTier1()
    {
        var cost = NextExpansionCostScaled;
        if (knowledge < cost)
        {
            return false;
        }

        knowledge -= cost;
        scaleTier1++;
        return true;
    }

    // Expands up to the given number of times (or as often as affordable); returns how many were bought.
    public int TryExpandTier1Many(int maxCount)
    {
        var bought = 0;
        while (bought < maxCount && TryExpandTier1())
        {
            bought++;
        }

        return bought;
    }

    public void Sim200ms(float dt)
    {
        UpdateResearchComplete(dt);
        UpdateTutorial();

        var alpha = 1d - Math.Exp(-dt / KnowledgeRules.BaseRateTimeConstant);
        baseRate += (pendingPoints / dt - baseRate) * alpha;
        pendingPoints = 0f;

        AddProduction(ProductionTier1 * KnowledgeRules.Scale * dt);

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

        var newIdleMode = researchComplete && RecordArchive.AnyOperational;
        if (newIdleMode != idleMode)
        {
            idleMode = newIdleMode;
            RefreshResearchCenters();
        }
    }

    // Adds an amount in thousandths. The part below one thousandth is carried over to the next tick.
    private void AddProduction(double scaledAmount)
    {
        if (double.IsNaN(scaledAmount) || double.IsInfinity(scaledAmount) || scaledAmount <= 0d)
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

    private void UpdateTutorial()
    {
        if (tutorialDialogOpen || tutorialStage >= TutorialDialog.StageCount)
        {
            return;
        }

        bool reached;
        switch (tutorialStage)
        {
            case 0: reached = researchComplete; break;
            case 1: reached = RecordArchive.AnyOperational; break;
            case 2: reached = CanExpandTier1(); break;
            case 3: reached = AffordableUpgradeCount() > 0; break;
            case 4: reached = CanShift; break;
            default: reached = SchoolsUnlocked; break;
        }

        if (!reached)
        {
            return;
        }

        tutorialDialogOpen = true;
        TutorialDialog.Show(tutorialStage, () => tutorialDialogOpen = false);
        tutorialStage++;
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
