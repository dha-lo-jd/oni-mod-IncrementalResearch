using System;
using System.Numerics;

namespace IncrementalResearch.Core;

// Tuning constants. See docs/spec-knowledge-tier1.md.
public static class KnowledgeRules
{
    // Amounts of Insight are held as integers in thousandths and shown divided by this. Fractions below one
    // thousandth are dropped (carried over per tick), so no floating point error builds up in the stored amount.
    public const int Scale = 1000;

    public const double ScaleBaseCost = 10d;
    public const double ScaleCostGrowth = 1.15d;

    // Time constant (game seconds) of the moving average of the research output.
    public const float BaseRateTimeConstant = 600f;

    // Cost of the next expansion, in thousandths of Insight.
    public static BigInteger ExpansionCost(int expansions, double growth = ScaleCostGrowth, double baseFactor = 1d)
    {
        return new BigInteger(Math.Floor(ScaleBaseCost * baseFactor * Math.Pow(growth, expansions) * Scale));
    }

    // Paradigm shift. See docs/spec-paradigm-shift.md.
    // Insight produced in one cycle (in whole Insight) that is worth 1 Meta-Insight.
    public const double MiThreshold = 100000d;
    public const double MiExponent = 0.5d;
    // Production multiplier per Meta-Insight earned, and the bonus each Meta-Insight expansion adds to it.
    public const double MiMultiplierBase = 0.1d;
    public const double MiMultiplierPerExpansion = 0.02d;
    public const double MiExpansionBaseCost = 3d;
    public const double MiExpansionCostGrowth = 1.5d;

    // Meta-Insight gained by shifting after producing the given Insight (in thousandths) in one cycle.
    public static BigInteger MiGain(BigInteger producedScaled)
    {
        if (producedScaled.Sign <= 0)
        {
            return BigInteger.Zero;
        }

        // Log10 keeps this safe for values beyond the range of double.
        var exponent = (BigInteger.Log10(producedScaled) - Math.Log10(Scale) - Math.Log10(MiThreshold)) * MiExponent;
        if (exponent < 0d)
        {
            return BigInteger.Zero;
        }

        return new BigInteger(Math.Floor(Math.Pow(10d, Math.Min(exponent, 300d)) + 1e-9));
    }

    // Tiers 2 and 3. See docs/spec-tiers-2-3.md.
    public const int MaxTier = 3;
    // Expansions of the tier below that unlock the blueprint of tier 2 and tier 3.
    public const int Tier2UnlockExpansions = 25;
    public const int Tier3UnlockExpansions = 15;
    // How fast the upper tiers build up the performance of the tier below them.
    public const double BonusRateTier2 = 0.002d;
    public const double BonusRateTier3 = 0.001d;
    // Time constants (seconds) of the activity of a facility rising when it runs and falling when it stops.
    public const double ActivityRiseTime = 20d;
    public const double ActivityFallTime = 120d;

    public static double BaseCost(int tier)
    {
        return tier == 1 ? ScaleBaseCost : tier == 2 ? 1000d : 1000000d;
    }

    public static double BaseGrowth(int tier)
    {
        return tier == 1 ? ScaleCostGrowth : tier == 2 ? 1.18d : 1.22d;
    }

    // The base-game load of the tier 2 and 3 facilities grows as the tier is expanded.
    // It stops growing at a fixed holdings count, so that feasibility in the main game never caps the tier.
    public const int EnvironmentTightenExpansions = 40;
    // Highest temperature (K) at which the Compute Room still runs: 30 C.
    public const float ComputeRoomMaxTemperature = 303.15f;
    // Extra self heat (kW = kDTU/s) of the Compute Room at the cap, on top of the building's own.
    public const float ComputeRoomExtraHeatAtCap = 16f;
    // Most gas mass (kg) per cell at which the Vacuum Lab still runs. Not tightened for now.
    public const float VacuumLabMaxCellMass = 0.05f;

    private static double TightenProgress(int holdings)
    {
        return Math.Min(Math.Max(holdings, 0), EnvironmentTightenExpansions) / (double)EnvironmentTightenExpansions;
    }

    // Extra heat (kW) the Compute Room gives off while running, growing linearly with its holdings up to the cap.
    public static float ComputeRoomExtraHeat(int holdings)
    {
        return ComputeRoomExtraHeatAtCap * (float)TightenProgress(holdings);
    }

    public static BigInteger TierExpansionCost(int tier, int expansions, double growth, double baseFactor)
    {
        return new BigInteger(Math.Floor(BaseCost(tier) * baseFactor * Math.Pow(growth, expansions) * Scale));
    }

    public static BigInteger MiExpansionCost(int expansions)
    {
        return new BigInteger(Math.Floor(MiExpansionBaseCost * Math.Pow(MiExpansionCostGrowth, expansions)));
    }

    public static float PointWeight(string researchTypeId)
    {
        switch (researchTypeId)
        {
            case "basic": return 1f;
            case "advanced": return 2f;
            case "nuclear": return 3f;
            case "space": return 4f;
            case "orbital": return 4f;
            default: return 0f;
        }
    }
}
