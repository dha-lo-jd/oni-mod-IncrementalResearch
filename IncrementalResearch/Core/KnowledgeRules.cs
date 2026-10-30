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
    public static BigInteger ExpansionCost(int expansions, double growth = ScaleCostGrowth)
    {
        return new BigInteger(Math.Floor(ScaleBaseCost * Math.Pow(growth, expansions) * Scale));
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
