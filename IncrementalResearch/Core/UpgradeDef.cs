using System.Collections.Generic;
using System.Numerics;

namespace IncrementalResearch.Core;

public enum UpgradeEffect
{
    // Multiplies the production of the scope.
    ProductionMultiplier,
    // Replaces the growth rate of the expansion cost (the lowest purchased value counts).
    ExpansionCostGrowth,
    // Unlocks a convenience feature, identified by the upgrade id.
    Convenience,
}

// A one-time purchase that adds a multiplier or an effect. See docs/spec-upgrades.md.
public sealed class UpgradeDef
{
    public string Id;
    public LocString Name;
    public LocString Description;
    // 0 for upgrades that apply to every tier.
    public int Tier;
    // Cost in Insight, in whole units (scaled by KnowledgeRules.Scale when paid).
    public long Cost;
    public UpgradeEffect Effect;
    public double Value;
    // Unlocks (becomes visible) once the tier has been expanded this many times.
    public int RequiredExpansions;
    public string RequiredUpgradeId;

    public bool IsGlobal => Tier == 0;
    public BigInteger CostScaled => new BigInteger(Cost) * KnowledgeRules.Scale;
}

public static class Upgrades
{
    public const string Index = "T1_INDEX";
    public const string Classify = "T1_CLASSIFY";
    public const string CrossRef = "T1_CROSSREF";
    public const string Shelving = "T1_SHELVING";
    public const string Bulk = "T1_BULK";
    public const string Network = "G_NETWORK";

    public static readonly IReadOnlyList<UpgradeDef> All = new List<UpgradeDef>
    {
        new UpgradeDef
        {
            Id = Index, Name = STRINGS.UI.UPGRADES.T1_INDEX.NAME, Description = STRINGS.UI.UPGRADES.T1_INDEX.DESC,
            Tier = 1, Cost = 50, Effect = UpgradeEffect.ProductionMultiplier, Value = 2d, RequiredExpansions = 3,
        },
        new UpgradeDef
        {
            Id = Classify, Name = STRINGS.UI.UPGRADES.T1_CLASSIFY.NAME, Description = STRINGS.UI.UPGRADES.T1_CLASSIFY.DESC,
            Tier = 1, Cost = 1000, Effect = UpgradeEffect.ProductionMultiplier, Value = 2d, RequiredExpansions = 15,
            RequiredUpgradeId = Index,
        },
        new UpgradeDef
        {
            Id = CrossRef, Name = STRINGS.UI.UPGRADES.T1_CROSSREF.NAME, Description = STRINGS.UI.UPGRADES.T1_CROSSREF.DESC,
            Tier = 1, Cost = 20000, Effect = UpgradeEffect.ProductionMultiplier, Value = 3d, RequiredExpansions = 30,
            RequiredUpgradeId = Classify,
        },
        new UpgradeDef
        {
            Id = Shelving, Name = STRINGS.UI.UPGRADES.T1_SHELVING.NAME, Description = STRINGS.UI.UPGRADES.T1_SHELVING.DESC,
            Tier = 1, Cost = 500, Effect = UpgradeEffect.ExpansionCostGrowth, Value = 1.13d, RequiredExpansions = 10,
        },
        new UpgradeDef
        {
            Id = Bulk, Name = STRINGS.UI.UPGRADES.T1_BULK.NAME, Description = STRINGS.UI.UPGRADES.T1_BULK.DESC,
            Tier = 1, Cost = 300, Effect = UpgradeEffect.Convenience, Value = 0d, RequiredExpansions = 5,
        },
        new UpgradeDef
        {
            Id = Network, Name = STRINGS.UI.UPGRADES.G_NETWORK.NAME, Description = STRINGS.UI.UPGRADES.G_NETWORK.DESC,
            Tier = 0, Cost = 5000, Effect = UpgradeEffect.ProductionMultiplier, Value = 1.5d, RequiredExpansions = 20,
        },
    };
}
