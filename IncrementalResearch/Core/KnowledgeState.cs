using System;
using Database;
using KSerialization;

namespace IncrementalResearch.Core;

// Colony-wide idle game state, stored in the save game.
[SerializationConfig(MemberSerialization.OptIn)]
public sealed class KnowledgeState : KMonoBehaviour, ISim200ms
{
    public static KnowledgeState Instance { get; private set; }

    [Serialize] private double knowledge;
    [Serialize] private int scaleTier1;
    // Moving average of the weighted research output (points per second).
    [Serialize] private double baseRate;

    private float pendingPoints;
    private float researchCompleteCheckTimer;
    private bool researchComplete;
    private bool idleMode;

    public double Knowledge => knowledge;
    public int ScaleTier1 => scaleTier1;
    public double BaseRate => baseRate;
    public double MultiplierTier1 => 1d;
    public double ProductionTier1 => RecordArchive.AnyOperational ? baseRate * scaleTier1 * MultiplierTier1 : 0d;
    public double NextScaleCostTier1 => KnowledgeRules.ScaleCost(scaleTier1);

    // True when there is nothing left to research and an archive keeps the research buildings running.
    public bool IdleMode => idleMode;

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

    public void RecordResearchPoints(string researchTypeId, float points)
    {
        pendingPoints += points * KnowledgeRules.PointWeight(researchTypeId);
    }

    public bool CanExpandTier1()
    {
        return knowledge >= NextScaleCostTier1;
    }

    public bool TryExpandTier1()
    {
        var cost = NextScaleCostTier1;
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

        knowledge += ProductionTier1 * dt;

        var newIdleMode = researchComplete && RecordArchive.AnyOperational;
        if (newIdleMode != idleMode)
        {
            idleMode = newIdleMode;
            RefreshResearchCenters();
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
        }
    }
}
