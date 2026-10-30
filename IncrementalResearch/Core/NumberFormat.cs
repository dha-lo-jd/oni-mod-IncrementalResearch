using System.Numerics;

namespace IncrementalResearch.Core;

public static class NumberFormat
{
    // Plain up to 1e6, scientific notation beyond.
    public static string Format(double value)
    {
        return value >= 1e6 ? value.ToString("0.###e+0") : value.ToString("0.##");
    }

    // Formats a whole number of any magnitude (Meta-Insight).
    public static string FormatWhole(BigInteger value)
    {
        if (value < 1000000)
        {
            return value.ToString();
        }

        var digits = value.ToString();
        return digits[0] + "." + digits.Substring(1, 3) + "e+" + (digits.Length - 1);
    }

    // Formats an amount held in thousandths (see KnowledgeRules.Scale). Works for any magnitude.
    public static string FormatScaled(BigInteger scaled)
    {
        if (scaled >= KnowledgeRules.InfiniteScaled)
        {
            return Format(double.PositiveInfinity);
        }

        if (scaled < KnowledgeRules.Scale * 1000000)
        {
            return Format((double)scaled / KnowledgeRules.Scale);
        }

        var digits = (scaled / KnowledgeRules.Scale).ToString();
        return digits[0] + "." + digits.Substring(1, 3) + "e+" + (digits.Length - 1);
    }
}
