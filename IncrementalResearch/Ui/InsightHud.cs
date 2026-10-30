using IncrementalResearch.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IncrementalResearch.Ui;

// Small readout above the build menu, so the Insight can be followed on the main screen
// (the game is paused while the Insight tab is open).
internal sealed class InsightHud : MonoBehaviour
{
    private const float RefreshInterval = 0.25f;
    private const float Width = 840f;
    private const float Height = 100f;
    // Negative: the category bar rect reaches above its buttons.
    private const float Gap = -48f;
    private const float FontSize = 42f;
    private const float VisibleAlpha = 0.7f;

    private RectTransform rect;
    private RectTransform anchorRect;
    private LocText label;
    private CanvasGroup group;
    private float nextRefresh;

    public static void Create(PlanScreen planScreen)
    {
        // The plan screen sits inside a scroll rect, so the readout goes on the screen space canvas instead.
        var canvas = GameScreenManager.Instance != null ? GameScreenManager.Instance.ssOverlayCanvas : null;
        var parent = canvas != null ? canvas.transform : planScreen.transform.parent;
        Debug.Log("[IncrementalResearch] Insight readout parent: " + parent.name);
        var go = new GameObject("InsightHud", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        var hud = go.AddComponent<InsightHud>();
        hud.rect = (RectTransform)go.transform;
        hud.anchorRect = planScreen.buildingGroupsRoot;

        hud.rect.anchorMin = Vector2.zero;
        hud.rect.anchorMax = Vector2.zero;
        hud.rect.pivot = new Vector2(0f, 0f);
        hud.rect.sizeDelta = new Vector2(Width, Height);

        var background = go.AddComponent<Image>();
        background.color = new Color(0.192f, 0.205f, 0.258f, 1f);
        background.raycastTarget = false;

        hud.label = UiFactory.CreateLabel(hud.rect, "", 24f, 0f, Width - 48f, Height, TextAlignmentOptions.MidlineLeft, FontSize);
        // Hidden by alpha rather than deactivated, so that Update keeps running.
        hud.group = go.AddComponent<CanvasGroup>();
        hud.group.alpha = 0f;
        hud.group.blocksRaycasts = false;
        hud.group.interactable = false;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshInterval;
        Refresh();
    }

    private void Refresh()
    {
        var state = KnowledgeState.Instance;
        var tabOpen = IdleResearchTab.Instance != null && IdleResearchTab.Instance.IsOpen;
        var visible = state != null && state.Unlocked && !tabOpen;
        group.alpha = visible ? VisibleAlpha : 0f;

        if (!visible)
        {
            return;
        }

        var knowledge = NumberFormat.FormatScaled(state.KnowledgeScaled);
        var production = NumberFormat.Format(state.ProductionTier1);
        // Meta-Insight is only mentioned once the colony has some.
        label.text = state.MetaInsightEarned > 0
            ? string.Format(STRINGS.UI.HUD.TEXT_MI, knowledge, production, NumberFormat.FormatWhole(state.MetaInsight))
            : string.Format(STRINGS.UI.HUD.TEXT, knowledge, production);
        Place();
    }

    // Sits on the top left corner of the build menu's category bar.
    private void Place()
    {
        var parent = (RectTransform)rect.parent;
        var x = 10f;
        var y = 200f;
        if (anchorRect != null && anchorRect.gameObject.activeInHierarchy)
        {
            var corners = new Vector3[4];
            anchorRect.GetWorldCorners(corners);
            var local = (Vector2)parent.InverseTransformPoint(corners[1]);
            x = local.x - parent.rect.xMin;
            y = local.y - parent.rect.yMin + Gap;
        }

        rect.anchoredPosition = new Vector2(x, y);
    }
}
