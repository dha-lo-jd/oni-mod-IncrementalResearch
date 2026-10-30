using System.Collections.Generic;
using System.Numerics;

namespace IncrementalResearch.Core;

public enum UpgradeEffect
{
    // Multiplies the production of the scope.
    ProductionMultiplier,
    // Replaces the growth rate of the expansion cost (the lowest purchased value counts).
    ExpansionCostGrowth,
    // Multiplies the base cost of the Insight expansion.
    ExpansionCostMultiplier,
    // Insight (in whole units) the colony starts a cycle with after a paradigm shift.
    StartingKnowledge,
    // Buys affordable Insight upgrades automatically.
    AutoBuyUpgrades,
    // Buys Insight expansions automatically while affordable.
    AutoExpandInsight,
    // Buys Meta-Insight expansions automatically while affordable.
    AutoExpandMetaInsight,
    // Unlocks a convenience feature, identified by the upgrade id.
    Convenience,
}

public enum UpgradeCurrency
{
    Insight,
    MetaInsight,
}

// A one-time purchase that adds a multiplier or an effect. See docs/spec-upgrades.md.
public sealed class UpgradeDef
{
    public string Id;
    public LocString Name;
    public LocString Description;
    public UpgradeCurrency Currency;
    // 0 for upgrades that apply to every tier.
    public int Tier;
    // Cost in whole units of the currency.
    public long Cost;
    public UpgradeEffect Effect;
    public double Value;
    // Unlocks (becomes visible) once the tier has been expanded this many times...
    public int RequiredExpansions;
    // ...and the colony has made this many paradigm shifts.
    public int RequiredShifts;
    public string RequiredUpgradeId;
    // Kept by a paradigm shift even though it is bought with Insight (the automation upgrades).
    public bool PersistsOnShift;

    public bool IsGlobal => Tier == 0;

    // Insight is held in thousandths (KnowledgeRules.Scale); Meta-Insight in whole units.
    public BigInteger CostAmount =>
        Currency == UpgradeCurrency.Insight ? new BigInteger(Cost) * KnowledgeRules.Scale : new BigInteger(Cost);
}

public static class Upgrades
{
    public const string Index = "T1_INDEX";
    public const string Classify = "T1_CLASSIFY";
    public const string CrossRef = "T1_CROSSREF";
    public const string Shelving = "T1_SHELVING";
    public const string Bulk = "T1_BULK";
    public const string Network = "G_NETWORK";
    public const string Parallel = "T2_PARALLEL";
    public const string Pipeline = "T2_PIPELINE";
    public const string Cooling = "T2_COOLING";
    public const string Isolation = "T3_ISOLATION";
    public const string Chamber = "T3_CHAMBER";
    public const string AutoExpand = "T1_AUTO_EXPAND";
    public const string AutoExpandMi = "M_AUTO_EXPAND";
    public const string HeadStart = "M_HEADSTART";
    public const string Discount = "M_DISCOUNT";
    public const string AutoUpgrade = "M_AUTO_UPGRADE";

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
        new UpgradeDef
        {
            Id = Parallel, Name = STRINGS.UI.UPGRADES.T2_PARALLEL.NAME, Description = STRINGS.UI.UPGRADES.T2_PARALLEL.DESC,
            Tier = 2, Cost = 50000, Effect = UpgradeEffect.ProductionMultiplier, Value = 2d, RequiredExpansions = 5,
        },
        new UpgradeDef
        {
            Id = Pipeline, Name = STRINGS.UI.UPGRADES.T2_PIPELINE.NAME, Description = STRINGS.UI.UPGRADES.T2_PIPELINE.DESC,
            Tier = 2, Cost = 2000000, Effect = UpgradeEffect.ProductionMultiplier, Value = 2d, RequiredExpansions = 15,
            RequiredUpgradeId = Parallel,
        },
        new UpgradeDef
        {
            Id = Cooling, Name = STRINGS.UI.UPGRADES.T2_COOLING.NAME, Description = STRINGS.UI.UPGRADES.T2_COOLING.DESC,
            Tier = 2, Cost = 500000, Effect = UpgradeEffect.ExpansionCostGrowth, Value = 1.16d, RequiredExpansions = 10,
        },
        new UpgradeDef
        {
            Id = Isolation, Name = STRINGS.UI.UPGRADES.T3_ISOLATION.NAME, Description = STRINGS.UI.UPGRADES.T3_ISOLATION.DESC,
            Tier = 3, Cost = 5000000000, Effect = UpgradeEffect.ProductionMultiplier, Value = 2d, RequiredExpansions = 5,
        },
        new UpgradeDef
        {
            Id = Chamber, Name = STRINGS.UI.UPGRADES.T3_CHAMBER.NAME, Description = STRINGS.UI.UPGRADES.T3_CHAMBER.DESC,
            Tier = 3, Cost = 50000000000, Effect = UpgradeEffect.ExpansionCostGrowth, Value = 1.20d, RequiredExpansions = 10,
        },
        new UpgradeDef
        {
            Id = AutoExpand, Name = STRINGS.UI.UPGRADES.T1_AUTO_EXPAND.NAME,
            Description = STRINGS.UI.UPGRADES.T1_AUTO_EXPAND.DESC, Tier = 1, Cost = 2000,
            Effect = UpgradeEffect.AutoExpandInsight, Value = 0d, RequiredExpansions = 12, PersistsOnShift = true,
        },
        new UpgradeDef
        {
            Id = HeadStart, Name = STRINGS.UI.UPGRADES.M_HEADSTART.NAME, Description = STRINGS.UI.UPGRADES.M_HEADSTART.DESC,
            Currency = UpgradeCurrency.MetaInsight, Tier = 0, Cost = 3, Effect = UpgradeEffect.StartingKnowledge,
            Value = 100d, RequiredShifts = 1,
        },
        new UpgradeDef
        {
            Id = Discount, Name = STRINGS.UI.UPGRADES.M_DISCOUNT.NAME, Description = STRINGS.UI.UPGRADES.M_DISCOUNT.DESC,
            Currency = UpgradeCurrency.MetaInsight, Tier = 1, Cost = 8, Effect = UpgradeEffect.ExpansionCostMultiplier,
            Value = 0.5d, RequiredShifts = 2,
        },
        new UpgradeDef
        {
            Id = AutoUpgrade, Name = STRINGS.UI.UPGRADES.M_AUTO_UPGRADE.NAME,
            Description = STRINGS.UI.UPGRADES.M_AUTO_UPGRADE.DESC, Currency = UpgradeCurrency.MetaInsight, Tier = 0,
            Cost = 20, Effect = UpgradeEffect.AutoBuyUpgrades, Value = 0d, RequiredShifts = 3,
        },
        new UpgradeDef
        {
            Id = AutoExpandMi, Name = STRINGS.UI.UPGRADES.M_AUTO_EXPAND.NAME,
            Description = STRINGS.UI.UPGRADES.M_AUTO_EXPAND.DESC, Currency = UpgradeCurrency.MetaInsight, Tier = 0,
            Cost = 15, Effect = UpgradeEffect.AutoExpandMetaInsight, Value = 0d, RequiredShifts = 2,
        },
    };
}
