using System;
using System.Numerics;
using System.Runtime.Serialization;
using Database;
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

    private BigInteger knowledge = BigInteger.Zero;
    private double knowledgeRemainder;
    private float pendingPoints;
    private float researchCompleteCheckTimer;
    private bool researchComplete;
    private bool idleMode;

    public BigInteger KnowledgeScaled => knowledge;
    public int ScaleTier1 => scaleTier1;
    // The facility itself counts as the first unit; expansions add to it.
    public int HoldingsTier1 => scaleTier1 + 1;
    public double BaseRate => baseRate;
    public double MultiplierTier1 => 1d;

    public double ProductionTier1 =>
        RecordArchive.AnyOperational ? baseRate * HoldingsTier1 * MultiplierTier1 : 0d;

    public BigInteger NextExpansionCostScaled => KnowledgeRules.ExpansionCost(scaleTier1);

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
    }

    [OnDeserialized]
    private void OnDeserialized()
    {
        if (!BigInteger.TryParse(knowledgeText, out knowledge))
        {
            knowledge = BigInteger.Zero;
        }
    }

    public void RecordResearchPoints(string researchTypeId, float points)
    {
        pendingPoints += points * KnowledgeRules.PointWeight(researchTypeId);
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

    public void Sim200ms(float dt)
    {
        UpdateResearchComplete(dt);

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
        }
    }
}
