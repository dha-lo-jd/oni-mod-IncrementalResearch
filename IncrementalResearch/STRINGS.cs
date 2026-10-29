namespace IncrementalResearch;

// All user-visible text. Translations are loaded from translations/<language code>.po (see IncrementalResearchMod).
public static class STRINGS
{
    public static class UI
    {
        public static LocString TITLE = "Incremental Research";

        public static class TAB
        {
            public static LocString OPEN = "Insight";
            public static LocString CLOSE = "Research Tree";
            public static LocString TITLE = "Insight";
            public static LocString INSIGHT = "Insight: {0}";
            public static LocString BASE_RATE = "Base rate: {0}/s";
            public static LocString STATUS_ACTIVE = "Record Archive is running. Research buildings keep working after all research is complete.";
            public static LocString STATUS_INACTIVE = "No powered Record Archive. Insight is not being produced.";
            public static LocString UNIT_VALUE = "{0}: {1}";
            public static LocString PRODUCTION = "Production: {0}/s";
        }

        public static class HUD
        {
            public static LocString TEXT = "Insight: {0} (+{1}/s)";
        }

        public static class SIDE
        {
            public static LocString HEADER = "Insight research in progress";
            public static LocString OPEN_BUTTON = "Insight Research";
        }

        public static class ARCHIVE
        {
            public static LocString SIDESCREEN_TITLE = "Record Archive";
            public static LocString UNIT = "Holdings";
            public static LocString EXPAND_BUTTON = "Expand to increase {0} ({1} Insight)";
            public static LocString STATUS_TOOLTIP =
                "Insight: {0}\nBase rate: {1}/s\n{3}: {2}\nProduction: {4}/s";
        }
    }

    public static class BUILDINGS
    {
        public static class PREFABS
        {
            public static class IR_RECORDARCHIVE
            {
                public static LocString NAME = "Record Archive";
                public static LocString DESC = "Stores the colony's research records.";
                public static LocString EFFECT =
                    "Generates Insight from the colony's research output while powered.\n\nKeeps research buildings running after all research is complete.";
            }
        }
    }
}
