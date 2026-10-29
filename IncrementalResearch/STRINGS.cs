namespace IncrementalResearch;

// All user-visible text. Translations are loaded from translations/<language code>.po (see IncrementalResearchMod).
public static class STRINGS
{
    public static class UI
    {
        public static LocString TITLE = "Incremental Research";

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
