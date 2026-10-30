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

    private BigInteger knowledge = BigInteger.Zero;
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
    public double MultiplierTier1 => ProductionMultiplier(1);

    public double ProductionTier1 =>
        RecordArchive.AnyOperational ? baseRate * HoldingsTier1 * MultiplierTier1 : 0d;

    public BigInteger NextExpansionCostScaled => KnowledgeRules.ExpansionCost(scaleTier1, ExpansionCostGrowth);

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
    public bool Unlocked => researchComplete || knowledge > 0 || scaleTier1 > 0 || RecordArchive.AnyExists;

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
    }

    [OnDeserialized]
    private void OnDeserialized()
    {
        if (!BigInteger.TryParse(knowledgeText, out knowledge))
        {
            knowledge = BigInteger.Zero;
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

    // Visible once the tier has been expanded enough and the required upgrade has been bought.
    public bool IsUpgradeVisible(UpgradeDef def)
    {
        return !HasUpgrade(def.Id) && scaleTier1 >= def.RequiredExpansions &&
               (def.RequiredUpgradeId == null || HasUpgrade(def.RequiredUpgradeId));
    }

    public bool CanBuyUpgrade(UpgradeDef def)
    {
        return IsUpgradeVisible(def) && knowledge >= def.CostScaled;
    }

    public int AffordableUpgradeCount()
    {
        var count = 0;
        foreach (var def in Upgrades.All)
        {
            if (CanBuyUpgrade(def))
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

        knowledge -= def.CostScaled;
        upgrades.Add(def.Id);
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
        knowledge += new BigInteger(whole);
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
            default: reached = AffordableUpgradeCount() > 0; break;
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
