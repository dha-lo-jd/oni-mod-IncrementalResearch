using System;
using System.Numerics;

namespace IncrementalResearch.Core;

// Tuning constants. See docs/spec-knowledge-tier1.md.
public static class KnowledgeRules
{
    // Amounts of Insight are held as integers in thousandths and shown divided by this. Fractions below one
    // thousandth are dropped (carried over per tick), so no floating point error builds up in the stored amount.
    public const int Scale = 1000;

    // Most expansions bought in one go (the Max button, and automatic purchasing in one tick). Expansion costs
    // grow exponentially, so this is rarely reached; it only keeps a huge stockpile from stalling the game.
    public const int MaxExpansionsPerAction = 1000;

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
    public const double MiThreshold = 1e14d;
    // Meta-Insight gained = x^(p0 - c * log10(x)), x = produced / MiThreshold. The exponent starts just below 1
    // and falls slowly as x grows, so that the gain tapers off instead of running away.
    public const double MiExponent = 0.98d;
    public const double MiExponentTaper = 1e-4d;
    // Production multiplier = 1 + k * M, k = MiMultiplierBase + MiMultiplierPerExpansion * expansions, M = earned.
    // Beyond M * k = 1 the multiplier is tapered the same way (see MiMultiplierBonus).
    public const double MiMultiplierBase = 0.01d;
    public const double MiMultiplierPerExpansion = 0.0005d;
    public const double MiMultiplierTaper = 5e-5d;
    public const double MiExpansionBaseCost = 3d;
    public const double MiExpansionCostGrowth = 1.5d;
    // Base rate that the production formula is normalised to, and how strongly the real rate counts: the
    // effective rate is Ref * (S / Ref)^Exponent, so colonies with a much larger research output are not
    // hurried through the pacing. Only used for the idle game; the displayed base rate stays the real one.
    public const double BaseRateReference = 8d;
    public const double BaseRateExponent = 0.5d;

    public static double EffectiveBaseRate(double baseRate)
    {
        return baseRate <= 0d ? 0d : BaseRateReference * Math.Pow(baseRate / BaseRateReference, BaseRateExponent);
    }

    // Meta-Insight gained by shifting after producing the given Insight (in thousandths) in one cycle.
    public static BigInteger MiGain(BigInteger producedScaled)
    {
        if (producedScaled.Sign <= 0)
        {
            return BigInteger.Zero;
        }

        // Log10 keeps this safe for values beyond the range of double.
        var lgX = BigInteger.Log10(producedScaled) - Math.Log10(Scale) - Math.Log10(MiThreshold);
        if (lgX < 0d)
        {
            return BigInteger.Zero;
        }

        var exponent = MiExponent * lgX - MiExponentTaper * lgX * lgX;
        return new BigInteger(Math.Floor(Math.Pow(10d, Math.Min(exponent, 300d)) + 1e-9));
    }

    // The part of the production multiplier that comes from the Meta-Insight earned (the multiplier is 1 + this).
    public static double MiMultiplierBonus(double coefficient, BigInteger earned)
    {
        if (earned.Sign <= 0 || coefficient <= 0d)
        {
            return 0d;
        }

        var lg = Math.Log10(coefficient) + BigInteger.Log10(earned);
        if (lg <= 0d)
        {
            return Math.Pow(10d, lg);
        }

        return Math.Pow(10d, Math.Min(lg - MiMultiplierTaper * lg * lg, 300d));
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

    // Insight at or above this (in thousandths) counts as infinite. Far beyond what a double rate can reach.
    public static readonly BigInteger InfiniteScaled = BigInteger.Pow(10, 400);

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
