using HarmonyLib;
using IncrementalResearch.Core;
using IncrementalResearch.Ui;
using UnityEngine;

namespace IncrementalResearch.Patches;

// While the research buildings run in idle mode there is no tech to show: their side screen shows the state of the
// Insight research instead, and its button jumps to the Insight tab.
[HarmonyPatch(typeof(ResearchSideScreen), "OnPrefabInit")]
public static class ResearchSideScreen_OnPrefabInit_Patch
{
    public static void Postfix(ResearchSideScreen __instance)
    {
        __instance.selectResearchButton.onClick += () =>
        {
            if (KnowledgeState.Instance != null && KnowledgeState.Instance.IdleMode)
            {
                // Runs after the original handler, which has opened the research screen.
                IdleResearchTab.Instance?.SetOpen(true);
            }
        };

        var adapter = __instance.gameObject.AddComponent<IdleSideScreenAdapter>();
        adapter.Initialize(__instance);
    }
}

[HarmonyPatch(typeof(ResearchSideScreen), "RefreshDisplayState")]
public static class ResearchSideScreen_RefreshDisplayState_Patch
{
    public static void Postfix(ResearchSideScreen __instance)
    {
        var adapter = __instance.GetComponent<IdleSideScreenAdapter>();
        adapter?.Apply();
    }
}

// Opening the research screen any other way shows the tech tree.
[HarmonyPatch(typeof(ResearchScreen), nameof(ResearchScreen.Show))]
public static class ResearchScreen_Show_Patch
{
    public static void Postfix(bool show)
    {
        if (show)
        {
            IdleResearchTab.Instance?.SetOpen(false);
        }
    }
}

internal sealed class IdleSideScreenAdapter : MonoBehaviour
{
    private const float RefreshInterval = 0.5f;

    private ResearchSideScreen screen;
    private LocText buttonLabel;
    private string originalButtonText;
    private float nextRefresh;
    private bool applied;

    public void Initialize(ResearchSideScreen sideScreen)
    {
        screen = sideScreen;
        buttonLabel = sideScreen.selectResearchButton.GetComponentInChildren<LocText>();
        originalButtonText = buttonLabel != null ? buttonLabel.text : null;
    }

    public void Apply()
    {
        var state = KnowledgeState.Instance;
        var idle = state != null && state.IdleMode;
        if (idle)
        {
            screen.DescriptionText.text = "<b>" + STRINGS.UI.SIDE.HEADER + "</b>\n"
                + string.Format(STRINGS.UI.TAB.INSIGHT, NumberFormat.FormatScaled(state.KnowledgeScaled)) + "\n"
                + string.Format(STRINGS.UI.TAB.PRODUCTION, NumberFormat.Format(state.ProductionTier1));
            if (buttonLabel != null)
            {
                buttonLabel.text = STRINGS.UI.SIDE.OPEN_BUTTON;
            }

            applied = true;
        }
        else if (applied)
        {
            // The original text is rewritten by the screen itself; only the button label has to be restored.
            if (buttonLabel != null && originalButtonText != null)
            {
                buttonLabel.text = originalButtonText;
            }

            applied = false;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshInterval;
        if (KnowledgeState.Instance != null && KnowledgeState.Instance.IdleMode)
        {
            Apply();
        }
    }
}
