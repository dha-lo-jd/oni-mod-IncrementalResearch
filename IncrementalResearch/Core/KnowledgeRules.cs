using System;

namespace IncrementalResearch.Core;

// Tuning constants. See docs/spec-knowledge-tier1.md.
public static class KnowledgeRules
{
    public const double ScaleBaseCost = 10d;
    public const double ScaleCostGrowth = 1.15d;

    // Time constant (game seconds) of the moving average of the research output.
    public const float BaseRateTimeConstant = 600f;

    public static double ScaleCost(int scale)
    {
        return ScaleBaseCost * Math.Pow(ScaleCostGrowth, scale);
    }

    public static float PointWeight(string researchTypeId)
    {
        switch (researchTypeId)
        {
            case "basic": return 1f;
            case "advanced": return 2f;
            default: return 0f;
        }
    }
}
