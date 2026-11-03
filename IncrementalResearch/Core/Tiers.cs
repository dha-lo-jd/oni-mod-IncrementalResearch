namespace IncrementalResearch.Core;

// Names of the facility tiers. See docs/spec-tiers-2-3.md and docs/spec-tiers-4-7.md.
public static class Tiers
{
    // Name of the unit a tier's expansions increase (Holdings, Compute Nodes, Experiment Lines, ...).
    public static string UnitName(int tier)
    {
        switch (tier)
        {
            case 1: return STRINGS.UI.ARCHIVE.UNIT;
            case 2: return STRINGS.UI.TIERS.UNIT_2;
            case 3: return STRINGS.UI.TIERS.UNIT_3;
            case 4: return STRINGS.UI.TIERS.UNIT_4;
            case 5: return STRINGS.UI.TIERS.UNIT_5;
            case 6: return STRINGS.UI.TIERS.UNIT_6;
            default: return STRINGS.UI.TIERS.UNIT_7;
        }
    }

    public static string FacilityName(int tier)
    {
        switch (tier)
        {
            case 1: return STRINGS.UI.ARCHIVE.SIDESCREEN_TITLE;
            case 2: return STRINGS.BUILDINGS.PREFABS.IR_COMPUTEROOM.NAME;
            case 3: return STRINGS.BUILDINGS.PREFABS.IR_VACUUMLAB.NAME;
            case 4: return STRINGS.BUILDINGS.PREFABS.IR_ACCELERATOR.NAME;
            case 5: return STRINGS.BUILDINGS.PREFABS.IR_CONFINEMENT.NAME;
            case 6: return STRINGS.BUILDINGS.PREFABS.IR_OBSERVATIONNET.NAME;
            default: return STRINGS.BUILDINGS.PREFABS.IR_EXTRACTIONFURNACE.NAME;
        }
    }
}
