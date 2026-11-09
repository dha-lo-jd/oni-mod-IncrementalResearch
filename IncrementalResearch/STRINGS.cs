namespace IncrementalResearch;

// All user-visible text. Translations are loaded from translations/<language code>.po (see IncrementalResearchMod).
public static class STRINGS
{
    public static class UI
    {
        public static LocString TITLE = "Project Ⅲ:Incremental Insight Initiative";

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
            // The caption is small and dim; the value takes the large, bright style.
            public static LocString INSIGHT = "<size=55%><color=#9AA6C8>Insight</color></size>  {0}";
            public static LocString RATE = "+{0}/s";
            public static LocString MI = "<size=75%><color=#9AA6C8>MI</color></size>  {0}";
            public static LocString ICON_GLYPH = "Ⅲ";
            public static LocString TOOLTIP = "Click to change the size";
            public static LocString MODE_LABEL = "Main screen readout";
            public static LocString MODE_FULL = "Full";
            public static LocString MODE_COMPACT = "Compact";
            public static LocString MODE_ICON = "Icon";
            public static LocString MODE_HIDDEN = "Hidden";
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
                "<b>Project III (Three) Begins</b>\n\n" +
                "<i>The Duplicants built the 'Record Archive' to preserve the fruits of their research and the footprints of their inquiry. Then they launched the \"Incremental Insight Initiative\", known as \"Project III\": a grand plan to multiply and gather the crystals of knowledge that, for now, point in no vector.\n" +
                "For the moment, most of the plan is nothing but pie in the sky. But if they never give up on persistence and accumulation, they may yet reach the sky...\n\n" +
                "\"The aim of scholarship is not to find answers. It is to pose the right questions and get pleasantly lost.\"  - Albert L. Davoski</i>\n\n" +
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
            public static LocString STAGE7_TITLE = "Blueprint: Compute Room";
            public static LocString STAGE7_BODY =
                "<b>God Does Arithmetic (Because It Was Outsourced!)</b>\n\n" +
                "<i>Calculation is inseparable from the pursuit of knowledge. Yet there is only so much a notebook can hold, and it is exhausting to keep the pen moving. The Duplicants seem to have decided to wait instead for an -oracle- to descend from a machine far wiser than themselves.\n\n\n" +
                "\"The act of calculating is not labor. It is art.\"  - Albert L. Davoski</i>\n\n" +
                "The Record Archive has been expanded far enough to draw up the next facility. The Compute Room (Equipment menu, Research tab) is now available. Build it from steel and keep its room at 30 C or below (it gives off more heat the more you expand it): while it runs, it builds up the performance of the Record Archive. If it stops, its activity falls and the performance fades toward x1, but nothing built up is lost.";
            public static LocString STAGE8_TITLE = "Blueprint: Vacuum Lab";
            public static LocString STAGE8_BODY =
                "<b>Duplicants Love a Vacuum</b>\n\n" +
                "<i>Is there something \"in\" a vacuum? Whatever the philosophers say, the hole of a giant donut is perfect for all manner of research. It would be far more unnatural to dislike such a convenient thing. At any rate, a room packed with vacuum like sardines should fill our tin cans with Insight as well.\n\n\n" +
                "\"Make the air clean. Yes, take it out.\"  - Albert L. Davoski</i>\n\n" +
                "The Compute Room has been expanded far enough to draw up the next facility. The Vacuum Lab (Equipment menu, Research tab) is now available. It needs a vacuum around it, and heat does not escape in a vacuum, so mind its temperature (it gives off more heat the more you expand it). While it runs, it builds up the performance of the Compute Room.";
            public static LocString STAGE9_TITLE = "Blueprint: High-Energy Particle Accelerator";
            public static LocString STAGE9_BODY =
                "<b>Accelerationist Elementary Particles</b>\n\n" +
                "<i>Companies and elementary particles are alike in that speed is everything. They are also alike in that their inner workings are never settled, but auditing is outside our jurisdiction. Now the speeds the particle accelerator produces will carry the Duplicants' matters (and Matter) ever closer to the speed of light...\n\n\"The Earth and classical mechanics escape their cage the same way: just accelerate.\"  - Albert L. Davoski</i>\n\n\n" +
                "The Vacuum Lab has been expanded far enough to draw up the next facility. The High-Energy Particle Accelerator (Equipment menu, Research tab) is now available. Build it from steel and plastic and keep it powered: it draws more power the more you expand it (up to 10 kW). While it runs, it builds up the performance of the Vacuum Lab.";
            public static LocString STAGE10_TITLE = "Blueprint: Excited Quantum Confinement Structure";
            public static LocString STAGE10_BODY =
                "<b>Excite, All Quanta of Creation!</b>\n\n" +
                "<i>The Duplicants have finally begun manufacturing 'something they don't quite understand': the Unknown itself. As a first step, the design of a strange vessel that confines uncertainty is complete. Whatever is inside the vessel, at least the blueprint is fixed without a shred of ambiguity!\n\n\"Many things work better when kept confined: quanta, plasma, and scholars.\"  - Albert L. Davoski</i>\n\n\n" +
                "The Accelerator has been expanded far enough to draw up the next facility. The Excited Quantum Confinement Structure (Equipment menu, Research tab) is now available. Build it from refined metal and glass and keep it powered: it draws more power the more you expand it (up to 24 kW). While it runs, it builds up the performance of the Accelerator.";
            public static LocString STAGE11_TITLE = "Blueprint: Coherence Observation Net";
            public static LocString STAGE11_BODY =
                "<b>Who Observes the Observer?</b>\n\n" +
                "<i>Project III is steadily advancing. The Coherence Observation Net will soon find the 'buried treasure'. That the treasure collapses upon being observed is a trifling matter. Incidentally, this outlook has been confirmed by observation. Who made that observation has not yet been observed.\n\n\"Observe not only the observation, but also the arrogance of the act of observing.\"  - Albert L. Davoski</i>\n\n\n" +
                "The Confinement Structure has been expanded far enough to draw up the next facility. The Coherence Observation Net (Equipment menu, Research tab) is now available. Build it from refined metal and fiber, and keep a band of cells to its left and right clear of tiles (up to 40 cells in total, as tall as the facility; it widens as you expand it). While it runs, it builds up the performance of the Confinement Structure.";
            public static LocString STAGE12_TITLE = "Blueprint: Insight Degeneracy Extraction Furnace";
            public static LocString STAGE12_BODY =
                "<b>Ignorance Is Power</b>\n\n" +
                "<i>The moment has finally come. Insight is no longer something to search for and find. A sage once preached 'knowing that you know nothing', and the Duplicants have realized it literally: they degenerate ignorance itself and extract knowledge from it.\n\n\"Fortunately we know next to nothing, and remain lost forever.\"  - Albert L. Davoski</i>\n\n\n" +
                "The Observation Net has been expanded far enough to draw up the final facility. The Insight Degeneracy Extraction Furnace (Equipment menu, Research tab) is now available. Build it from steel, glass and ceramic, and keep a rectangle of cells above its base clear of tiles (up to 32 wide and 16 tall; it grows as you expand it). While it runs, it builds up the performance of the Observation Net.";
            public static LocString STAGE13_TITLE = "Singularity";
            public static LocString STAGE13_BODY =
                "<b>Beyond the End of Knowledge, the Journey Continues</b>\n\n" +
                "<i>Insight keeps being born. Its accumulation comes to foresee even its own end, and in exchange for everything that came before, it scoops an 'answer' out of the sea of singularity. What that answer answers is uncertain, but that it is useful is beyond doubt. Incidentally, the only Critical Constant confirmed so far is '42'.</i>\n\n\n" +
                "Everything for a singularity is in sight. Deliver Data Banks to the Record Archive (Duplicants carry them in); the Insight tab shows how many are needed. A singularity resets the idle game and grants Critical Constant (CC), which buys permanent expansions and automation.";
            public static LocString STAGE3_BODY =
                "You have enough Insight to expand the Record Archive. Select it and use the expand button in its side screen: each expansion raises its production, but the next one costs more.";
        }

        public static class UPGRADES
        {
            public static LocString HEADER = "Upgrades";
            public static LocString BUY = "Buy ({0} Insight)";
            public static LocString BUY_MI = "Buy ({0} MI)";
            public static LocString BUY_CC = "Buy ({0} CC)";
            public static LocString PURCHASED = "Purchased: {0}";
            public static LocString BULK_TEN = "x10";
            public static LocString BULK_MAX = "Max";
            public static LocString AVAILABLE_TOOLTIP = "Upgrades available: {0}";
            public static LocString AUTOMATION_HEADER = "Automation";
            public static LocString AUTO_ON = "{0}: ON";
            public static LocString AUTO_OFF = "{0}: OFF";
            public static LocString AUTO_SHIFT_PRE = "Shift at";
            public static LocString AUTO_SHIFT_POST = "% of Meta-Insight earned";

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
                public static LocString DESC = "Adds x10 and Max buttons for expanding (Max buys up to 1000 at a time). Kept through paradigm shifts.";
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

            public static class C_AUTO_SHIFT
            {
                public static LocString NAME = "Automatic Shift";
                public static LocString DESC = "Automatically makes a paradigm shift once the Meta-Insight it would give reaches the set percentage of what has been earned so far (25% at first).";
            }

            public static class C_AUTO_MI_UPGRADE
            {
                public static LocString NAME = "Delegated Purchasing";
                public static LocString DESC = "Automatically buys affordable Meta-Insight upgrades.";
            }

            public static class C_AUTO_EXPAND
            {
                public static LocString NAME = "Self-Adjusting Constants";
                public static LocString DESC = "Automatically buys Critical Constant expansions whenever affordable.";
            }

            public static class T1_AUTO_EXPAND
            {
                public static LocString NAME = "Auto-Cataloging";
                public static LocString DESC = "Automatically expands the Record Archive whenever affordable. Kept through paradigm shifts.";
            }

            public static class T2_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Computation";
                public static LocString DESC = "Automatically expands the Compute Room whenever affordable. Kept through paradigm shifts.";
            }

            public static class T3_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Experiments";
                public static LocString DESC = "Automatically expands the Vacuum Lab whenever affordable. Kept through paradigm shifts.";
            }

            public static class T4_FOCUS
            {
                public static LocString NAME = "Beam Focusing";
                public static LocString DESC = "Accelerator build-up speed x2.";
            }

            public static class T4_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Collisions";
                public static LocString DESC = "Automatically expands the Accelerator whenever affordable. Kept through paradigm shifts.";
            }

            public static class T5_RESONANCE
            {
                public static LocString NAME = "Stable Resonance";
                public static LocString DESC = "Confinement Structure build-up speed x2.";
            }

            public static class T5_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Confinement";
                public static LocString DESC = "Automatically expands the Confinement Structure whenever affordable. Kept through paradigm shifts.";
            }

            public static class T6_APERTURE
            {
                public static LocString NAME = "Wider Aperture";
                public static LocString DESC = "Observation Net build-up speed x2.";
            }

            public static class T6_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Observation";
                public static LocString DESC = "Automatically expands the Observation Net whenever affordable. Kept through paradigm shifts.";
            }

            public static class T7_CHANNEL
            {
                public static LocString NAME = "Deeper Degeneracy";
                public static LocString DESC = "Extraction Furnace build-up speed x2.";
            }

            public static class T7_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Extraction";
                public static LocString DESC = "Automatically expands the Extraction Furnace whenever affordable. Kept through paradigm shifts.";
            }

            public static class M_AUTO_EXPAND
            {
                public static LocString NAME = "Automated Theorizing";
                public static LocString DESC = "Automatically builds theories whenever affordable.";
            }

            public static class T2_PARALLEL
            {
                public static LocString NAME = "Parallelization";
                public static LocString DESC = "Compute Room build-up speed x2.";
            }

            public static class T2_PIPELINE
            {
                public static LocString NAME = "Pipeline Processing";
                public static LocString DESC = "Compute Room build-up speed x2.";
            }

            public static class T2_COOLING
            {
                public static LocString NAME = "Optimized Cooling";
                public static LocString DESC = "Compute Nodes expansion cost growth drops from 1.18 to 1.16.";
            }

            public static class T3_ISOLATION
            {
                public static LocString NAME = "Line Isolation";
                public static LocString DESC = "Vacuum Lab build-up speed x2.";
            }

            public static class T3_CHAMBER
            {
                public static LocString NAME = "High Vacuum";
                public static LocString DESC = "Experiment Lines expansion cost growth drops from 1.22 to 1.20.";
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

        public static class SINGULARITY
        {
            public static LocString HEADER = "Singularity";
            public static LocString STATUS = "Singularities: {0}    Critical Constant: {1} CC";
            public static LocString CC_NOTE = "Critical Constant (CC): a constant fixed by a singularity. Not a liquid.";
            public static LocString NEED = "Needs: the last blueprint unlocked, {0} expansions of the last facility (now {1}), and every automatic expansion (now {2}/{3}).";
            public static LocString DATABANKS = "Data Banks delivered to the Record Archive: {0} / {1}";
            public static LocString BUTTON = "Singularity (+{0} CC)";
            public static LocString BUTTON_LOCKED = "Singularity (conditions not met)";
            public static LocString CC_PRODUCTION = "Constant of production (now x{0}): expand ({1} CC)";
            public static LocString CC_MI = "Constant of Meta-Insight (gain now x{0}): expand ({1} CC)";
            public static LocString CONFIRM_TITLE = "Singularity";
            public static LocString CONFIRM_GAIN = "You will gain {0} CC. {1} Data Banks in the Record Archive are consumed.";
            public static LocString CONFIRM_RESET = "Insight, Meta-Insight, everything bought with them, the paradigm shift count and the facility blueprints are reset.";
            public static LocString CONFIRM_KEEP = "Critical Constant and everything bought with it are kept. Buildings already built stay as they are.";
            public static LocString CONFIRM_OK = "Singularity";
        }

        public static class REWARDS
        {
            public static LocString HEADER = "Rewards";
            public static LocString TICKETS = "Free draws: attributes {0}, interests {1}    Critical Constant: {2} CC";
            public static LocString ATTR_STATUS = "Attribute bonus: {0} / {1} draws, +{2} in all";
            public static LocString ATTR_NONE = "No attribute bonus yet.";
            public static LocString ATTR_BUY_TICKET = "Attribute bonus (+0.1, random): use a free draw";
            public static LocString ATTR_BUY_CC = "Attribute bonus (+0.1, random): {0} CC";
            public static LocString ATTR_MAXED = "Attribute bonus: at the limit";
            public static LocString ATTR_RESET = "Reset all bonuses ({0} CC)";
            public static LocString INTEREST_HEADER = "Interests";
            public static LocString INTEREST_NONE = "There are no Duplicants.";
            public static LocString INTEREST_STATUS = "Granted: {0} ({1} / {2})";
            public static LocString INTEREST_NOTHING = "Granted: none (0 / {0})";
            public static LocString INTEREST_BUY_TICKET = "Grant a random interest: use a free draw";
            public static LocString INTEREST_BUY_CC = "Grant a random interest: {0} CC";
            public static LocString INTEREST_FULL = "This Duplicant cannot receive more interests";
            public static LocString INTEREST_RESET = "Remove granted interests ({0} CC)";
            public static LocString PREV = "<";
            public static LocString NEXT = ">";
            public static LocString MODIFIER_DESC = "Reward from the Incremental Insight Initiative";
            public static LocString MILESTONE_TITLE = "Paradigm Shift Milestone";
            public static LocString MILESTONE_BODY = "You have made {0} paradigm shift(s).\n\nReward: {1} free attribute draw(s) and {2} free interest grant(s). Use them in the Rewards box of the Insight tab.";
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

        public static class TIERS
        {
            public static LocString UNIT_2 = "Compute Nodes";
            public static LocString UNIT_3 = "Experiment Lines";
            public static LocString UNIT_4 = "Beam Lines";
            public static LocString UNIT_5 = "Confinement Cells";
            public static LocString UNIT_6 = "Observation Nodes";
            public static LocString UNIT_7 = "Extraction Channels";
            public static LocString REQUIRE_POWER = "Draws {0} kW while running (grows as it is expanded, up to a limit)";
            public static LocString REQUIRE_BAND = "Needs a clear band {0} cells long, left and right of the facility, as tall as the facility. Blocked cells now: {1}";
            public static LocString REQUIRE_RECT = "Needs a clear area {0} wide and {1} tall, from the base of the facility upward. Blocked cells now: {2}";
            public static LocString BLOCKED_NAME = "Space is blocked";
            public static LocString BLOCKED_TOOLTIP = "Natural solid or built tiles are in the area this facility needs. Its activity is falling.";
            public static LocString TOO_HOT_NAME = "Too hot to compute";
            public static LocString TOO_HOT_TOOLTIP = "The Compute Room only runs while its cell is cool enough. Its activity is falling.";
            public static LocString NOT_VACUUM_NAME = "Not a vacuum";
            public static LocString NOT_VACUUM_TOOLTIP = "The Vacuum Lab only runs when every cell it occupies is thin enough. Its activity is falling.";
            public static LocString REQUIRE_TEMPERATURE = "Runs at {0} C or below. Extra heat from expansion: +{1} kDTU/s (grows as it is expanded, up to a limit)";
            public static LocString REQUIRE_MASS = "Runs when every occupied cell holds {0} g of gas or less. Extra heat from expansion: +{1} kDTU/s (grows as it is expanded, up to a limit)";
            public static LocString STATUS_TOOLTIP = "Insight: {0}\n{1}: {2}\nActivity: {3}%\nPerformance given to the tier below: x{4}";
            public static LocString HEADER_BONUS = "Performance: x{0} (built up: {1}, activity: {2}%)";
            public static LocString PRODUCTION_RATE = "Build-up: +{0}/s";
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
            public static class IR_COMPUTEROOM
            {
                public static LocString NAME = "Compute Room";
                public static LocString DESC = "A room of racks that keeps the Record Archive sharp.";
                public static LocString EFFECT =
                    "Builds up the performance of the Record Archive while running. Runs only while its cell is cool enough, and gives off more heat the more it is expanded.\n\nIts activity falls while stopped, but the built-up performance is kept.";
            }

            public static class IR_VACUUMLAB
            {
                public static LocString NAME = "Vacuum Lab";
                public static LocString DESC = "A sealed laboratory where experiments need nothing in the way.";
                public static LocString EFFECT =
                    "Builds up the performance of the Compute Room while running. Runs only when every cell it occupies is thin enough, and gives off more heat the more it is expanded.\n\nIts activity falls while stopped, but the built-up performance is kept.";
            }

            public static class IR_ACCELERATOR
            {
                public static LocString NAME = "High-Energy Particle Accelerator";
                public static LocString DESC = "A ring that smashes the colony's records together at absurd energies.";
                public static LocString EFFECT =
                    "Builds up the performance of the Vacuum Lab while running. Draws more electric power the more it is expanded, up to a limit.\n\nIts activity falls while stopped, but the built-up performance is kept.";
            }

            public static class IR_CONFINEMENT
            {
                public static LocString NAME = "Excited Quantum Confinement Structure";
                public static LocString DESC = "A lattice that holds excited states still long enough to read them.";
                public static LocString EFFECT =
                    "Builds up the performance of the Accelerator while running. Draws more electric power the more it is expanded, up to a limit.\n\nIts activity falls while stopped, but the built-up performance is kept.";
            }

            public static class IR_OBSERVATIONNET
            {
                public static LocString NAME = "Coherence Observation Net";
                public static LocString DESC = "A net of sensors strung out sideways, watching for coherence.";
                public static LocString EFFECT =
                    "Builds up the performance of the Confinement Structure while running. Needs a clear band of cells to its left and right, as tall as the facility, that grows the more it is expanded, up to a limit. Natural solid and built tiles block it; gas, liquid, wires, pipes and ladders do not.\n\nIts activity falls while stopped, but the built-up performance is kept.";
            }

            public static class IR_EXTRACTIONFURNACE
            {
                public static LocString NAME = "Insight Degeneracy Extraction Furnace";
                public static LocString DESC = "A furnace that squeezes the last drop of Insight out of a degenerate state of knowledge.";
                public static LocString EFFECT =
                    "Builds up the performance of the Observation Net while running. Needs a clear rectangle from its base upward, centered on it, that grows the more it is expanded, up to a limit. Natural solid and built tiles block it; gas, liquid, wires, pipes and ladders do not.\n\nIts activity falls while stopped, but the built-up performance is kept.";
            }

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
