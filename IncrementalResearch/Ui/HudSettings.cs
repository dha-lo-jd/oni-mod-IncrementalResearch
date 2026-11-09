using System;

namespace IncrementalResearch.Ui;

// How the Insight readout on the main screen is shown.
internal enum HudMode
{
    Full = 0,
    Compact = 1,
    Icon = 2,
    Hidden = 3
}

// The player's choice of readout. Kept in the game's player preferences, so it applies to every save.
internal static class HudSettings
{
    private const string Key = "IncrementalResearch.HudMode";

    private static HudMode? mode;

    public static event System.Action Changed;

    public static HudMode Mode
    {
        get
        {
            if (mode == null)
            {
                var value = KPlayerPrefs.GetInt(Key, (int)HudMode.Full);
                mode = value >= (int)HudMode.Full && value <= (int)HudMode.Hidden ? (HudMode)value : HudMode.Full;
            }

            return mode.Value;
        }
        set
        {
            if (mode == value)
            {
                return;
            }

            mode = value;
            KPlayerPrefs.SetInt(Key, (int)value);
            KPlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    // Clicking the readout steps it down: full, compact, icon, and back to full. Hidden is chosen in the Insight tab.
    public static void Cycle()
    {
        switch (Mode)
        {
            case HudMode.Full:
                Mode = HudMode.Compact;
                break;
            case HudMode.Compact:
                Mode = HudMode.Icon;
                break;
            default:
                Mode = HudMode.Full;
                break;
        }
    }
}
