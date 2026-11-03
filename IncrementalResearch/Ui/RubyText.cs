using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IncrementalResearch.Ui;

// Converts the markup [base|ruby] into TextMeshPro tags that draw the ruby small, above the base text.
// TextMeshPro has no ruby support, so the layout is built from <size>, <voffset> and <space>,
// assuming full-width (1 em) characters for the base text and 0.5 em for the ruby.
// Text without the markup passes through unchanged.
// Square brackets are used because the game's translation loader rejects braces in translated strings.
internal static class RubyText
{
    private const float RubyScale = 0.5f;
    private const float RubyRise = 0.95f;

    private static readonly Regex Pattern = new Regex(@"\[([^\[\]|]+)\|([^\[\]|]+)\]", RegexOptions.Compiled);

    public static string Convert(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('[') < 0)
        {
            return text;
        }

        return Pattern.Replace(text, match => Build(match.Groups[1].Value, match.Groups[2].Value));
    }

    private static string Build(string baseText, string ruby)
    {
        var baseWidth = Width(baseText, 1f);
        var rubyWidth = Width(ruby, RubyScale);
        var total = System.Math.Max(baseWidth, rubyWidth);
        var basePad = (total - baseWidth) / 2f;
        var rubyPad = (total - rubyWidth) / 2f;

        var sb = new StringBuilder();
        // Ruby first, centered over the span.
        AppendSpace(sb, rubyPad);
        sb.Append("<voffset=").Append(F(RubyRise)).Append("em><size=").Append(F(RubyScale * 100f)).Append("%>")
            .Append(ruby).Append("</size></voffset>");
        // Back to the start of the span, then the base text centered in it.
        AppendSpace(sb, -(rubyPad + rubyWidth) + basePad);
        sb.Append(baseText);
        AppendSpace(sb, total - baseWidth - basePad);
        return sb.ToString();
    }

    // Width in em; ASCII characters count as half width.
    private static float Width(string s, float scale)
    {
        var width = 0f;
        foreach (var c in s)
        {
            width += c < 0x80 ? 0.5f : 1f;
        }

        return width * scale;
    }

    private static void AppendSpace(StringBuilder sb, float em)
    {
        if (System.Math.Abs(em) > 0.001f)
        {
            sb.Append("<space=").Append(F(em)).Append("em>");
        }
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
