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
