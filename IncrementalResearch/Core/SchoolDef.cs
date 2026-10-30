using System.Collections.Generic;

namespace IncrementalResearch.Core;

// A school of thought, chosen at each paradigm shift once schools are unlocked. It scales the production of
// the lower tiers (1-2) and the upper tiers (3 and up). See docs/spec-paradigm-shift.md.
public sealed class SchoolDef
{
    public string Id;
    public LocString Name;
    public LocString Description;
    public double LowerMultiplier;
    public double UpperMultiplier;

    public double Multiplier(int tier)
    {
        return tier <= Schools.LowerTierMax ? LowerMultiplier : UpperMultiplier;
    }
}

public static class Schools
{
    // Paradigm shifts needed before the schools are unlocked.
    public const int UnlockShifts = 5;
    public const int LowerTierMax = 2;

    public const string Experimentalism = "EXPERIMENTALISM";
    public const string Theoretical = "THEORETICAL";
    public const string Systematic = "SYSTEMATIC";

    public static readonly IReadOnlyList<SchoolDef> All = new List<SchoolDef>
    {
        new SchoolDef
        {
            Id = Experimentalism, Name = STRINGS.UI.SCHOOLS.EXPERIMENTALISM.NAME,
            Description = STRINGS.UI.SCHOOLS.EXPERIMENTALISM.DESC, LowerMultiplier = 1.5d, UpperMultiplier = 0.8d,
        },
        new SchoolDef
        {
            Id = Theoretical, Name = STRINGS.UI.SCHOOLS.THEORETICAL.NAME,
            Description = STRINGS.UI.SCHOOLS.THEORETICAL.DESC, LowerMultiplier = 0.8d, UpperMultiplier = 1.5d,
        },
        new SchoolDef
        {
            Id = Systematic, Name = STRINGS.UI.SCHOOLS.SYSTEMATIC.NAME,
            Description = STRINGS.UI.SCHOOLS.SYSTEMATIC.DESC, LowerMultiplier = 1.2d, UpperMultiplier = 1.2d,
        },
    };

    public static SchoolDef Find(string id)
    {
        foreach (var school in All)
        {
            if (school.Id == id)
            {
                return school;
            }
        }

        return null;
    }
}
