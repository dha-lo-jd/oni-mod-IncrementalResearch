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
            public static LocString TEXT_MI = "Insight: {0} (+{1}/s)  MI: {2}";
        }

        public static class SIDE
        {
            public static LocString HEADER = "Insight research in progress";
            public static LocString OPEN_BUTTON = "Insight Research";
        }

        public static class TUTORIAL
        {
            public static LocString STAGE1_TITLE = "Insight Research Is Now Possible";
            public static LocString STAGE1_BODY =
                "<i>Must the pursuit of knowledge always be rewarded? No. The adventures and challenges of the mind know neither profit nor end. The footprints of that striving become a road, and the Insight stored up without ever taking shape may become a staircase to glory... or it may not. Such is research. Better than boredom, and that is enough, isn't it?</i>\n\n" +
                "Every technology has been researched. The research buildings have nothing left to work on, but the colony's research records are still worth something.\n\n" +
                "Build a <b>Record Archive</b> (Equipment menu, Research tab). While it is powered, the colony's research output keeps turning into <b>Insight</b>.";
            public static LocString STAGE2_TITLE = "The Record Archive Is Running";
            public static LocString STAGE2_BODY =
                "Research buildings keep working even though the research tree is complete, and their output is now turned into Insight.\n\n" +
                "Insight is shown above the build menu. Open the <b>Insight</b> button on the research screen to see the details.";
            public static LocString STAGE3_TITLE = "Expanding the Archive";
            public static LocString STAGE4_TITLE = "Upgrades";
            public static LocString STAGE4_BODY =
                "An upgrade is now within reach. Open the Insight tab on the research screen: upgrades add multipliers and other effects, and each can be bought only once. Upgrades cost Insight too, so weigh them against the next expansion.";
            public static LocString STAGE5_TITLE = "Paradigm Shift";
            public static LocString STAGE5_BODY =
                "<i>It's time for a shift! (Not the work kind, and not the bit kind...)\n\n" +
                "The pursuit of knowledge knows no limit, but you cannot explore above the summit of a mountain. You can still dive to the bottom of the sea. You can still head for distant, unexplored lands. The time for change has come. A new perspective will give rise to new challenges. Much will be lost, but the 'Meta-Insight' gained in exchange will help.</i>\n\n" +
                "You have produced enough Insight for a paradigm shift. Shifting resets Insight, its expansions and its upgrades, and grants Meta-Insight: a separate currency that multiplies all production from then on, and can be spent on its own expansions and upgrades. The longer you wait before shifting, the more you earn, but with diminishing returns.";
            public static LocString STAGE6_TITLE = "Schools of Thought";
            public static LocString STAGE6_BODY =
                "The colony has shifted often enough to form schools of thought. At each paradigm shift you can now choose a school for the next cycle: each favors some tiers of facilities over others. You may also choose none.";
            public static LocString STAGE3_BODY =
                "You have enough Insight to expand the Record Archive. Select it and use the expand button in its side screen: each expansion raises its production, but the next one costs more.";
        }

        public static class UPGRADES
        {
            public static LocString HEADER = "Upgrades";
            public static LocString BUY = "Buy ({0} Insight)";
            public static LocString BUY_MI = "Buy ({0} MI)";
            public static LocString PURCHASED = "Purchased: {0}";
            public static LocString BULK_TEN = "x10";
            public static LocString BULK_MAX = "Max";
            public static LocString AVAILABLE_TOOLTIP = "Upgrades available: {0}";

            public static class T1_INDEX
            {
                public static LocString NAME = "Indexing";
                public static LocString DESC = "Record Archive production x2.";
            }

            public static class T1_CLASSIFY
            {
                public static LocString NAME = "Classification System";
                public static LocString DESC = "Record Archive production x2.";
            }

            public static class T1_CROSSREF
            {
                public static LocString NAME = "Cross-References";
                public static LocString DESC = "Record Archive production x3.";
            }

            public static class T1_SHELVING
            {
                public static LocString NAME = "Efficient Shelving";
                public static LocString DESC = "Expansion cost growth drops from 1.15 to 1.13.";
            }

            public static class T1_BULK
            {
                public static LocString NAME = "Bulk Expansion";
                public static LocString DESC = "Adds x10 and Max buttons for expanding.";
            }

            public static class M_HEADSTART
            {
                public static LocString NAME = "Accumulated Foundations";
                public static LocString DESC = "After a paradigm shift, start with 100 Insight.";
            }

            public static class M_DISCOUNT
            {
                public static LocString NAME = "Inherited Index";
                public static LocString DESC = "Base cost of Insight expansions x0.5.";
            }

            public static class M_AUTO_UPGRADE
            {
                public static LocString NAME = "Routine Purchasing";
                public static LocString DESC = "Automatically buys affordable Insight upgrades.";
            }

            public static class T1_AUTO_EXPAND
            {
                public static LocString NAME = "Auto-Cataloging";
                public static LocString DESC = "Automatically expands the Record Archive whenever affordable. Kept through paradigm shifts.";
            }

            public static class M_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Theorizing";
                public static LocString DESC = "Automatically builds theories whenever affordable.";
            }

            public static class G_NETWORK
            {
                public static LocString NAME = "Record Network";
                public static LocString DESC = "Production of every facility x1.5. Only the Record Archive exists so far.";
            }
        }

        public static class PARADIGM
        {
            public static LocString HEADER = "Paradigm Shift";
            public static LocString MI_LINE = "Meta-Insight: {0} (earned {1}, production x{2})";
            public static LocString PRODUCED = "Insight produced this cycle: {0}";
            public static LocString GAIN_NONE = "Produce {0} Insight in one cycle to earn Meta-Insight.";
            public static LocString SHIFT_BUTTON = "Paradigm Shift (+{0} MI)";
            public static LocString MI_UNIT = "Theories";
            public static LocString MI_EXPAND_BUTTON = "Build theories to increase {0} ({1} MI)";
            public static LocString CONFIRM_TITLE = "Paradigm Shift";
            public static LocString CONFIRM_GAIN = "You will gain {0} MI.";
            public static LocString CONFIRM_RESET = "Insight, Insight expansions and Insight upgrades are reset.";
            public static LocString CONFIRM_KEEP = "Meta-Insight and everything bought with it are kept.";
            public static LocString CONFIRM_SCHOOL = "Next school: {0}";
            public static LocString CONFIRM_OK = "Shift";
        }

        public static class SCHOOLS
        {
            public static LocString PICK = "Next school:";
            public static LocString NONE = "None";
            public static LocString CURRENT = "Current school: {0}";

            public static class EXPERIMENTALISM
            {
                public static LocString NAME = "Experimentalism";
                public static LocString DESC = "Lower tiers (1-2) produce x1.5, upper tiers (3 and up) x0.8.";
            }

            public static class THEORETICAL
            {
                public static LocString NAME = "Theoretical School";
                public static LocString DESC = "Upper tiers (3 and up) produce x1.5, lower tiers (1-2) x0.8.";
            }

            public static class SYSTEMATIC
            {
                public static LocString NAME = "Systematism";
                public static LocString DESC = "All tiers produce x1.2.";
            }
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
