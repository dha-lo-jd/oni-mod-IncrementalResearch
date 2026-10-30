namespace IncrementalResearch.Core;

// Names of the facility tiers. See docs/spec-tiers-2-3.md.
public static class Tiers
{
    // Name of the unit a tier's expansions increase (Holdings, Compute Nodes, Experiment Lines).
    public static string UnitName(int tier)
    {
        return tier == 1 ? STRINGS.UI.ARCHIVE.UNIT : tier == 2 ? STRINGS.UI.TIERS.UNIT_2 : STRINGS.UI.TIERS.UNIT_3;
    }

    public static string FacilityName(int tier)
    {
        return tier == 1
            ? STRINGS.UI.ARCHIVE.SIDESCREEN_TITLE
            : tier == 2
                ? STRINGS.BUILDINGS.PREFABS.IR_COMPUTEROOM.NAME
                : STRINGS.BUILDINGS.PREFABS.IR_VACUUMLAB.NAME;
    }
}
