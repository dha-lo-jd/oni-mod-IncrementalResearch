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
    private const float Width = 640f;
    private const float Height = 104f;
    private const float AccentWidth = 8f;
    // Negative: the category bar rect reaches above its buttons.
    private const float Gap = -48f;
    private const float InsightMaxSize = 46f;
    private const float RowMaxSize = 30f;
    private const float VisibleAlpha = 0.7f;

    private RectTransform rect;
    private RectTransform anchorRect;
    private LocText insightLabel;
    private LocText rateLabel;
    private LocText metaLabel;
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

        // A thin gold edge on the left marks the readout; the numbers sit in three colors on two rows.
        var accent = UiFactory.CreateRect("Accent", hud.rect, 0f, 0f, AccentWidth, Height).gameObject.AddComponent<Image>();
        accent.color = UiFactory.InsightColor;
        accent.raycastTarget = false;
        var textWidth = Width - AccentWidth - 32f;
        hud.insightLabel = UiFactory.CreateLabel(hud.rect, "", AccentWidth + 16f, 6f, textWidth, 58f,
            TextAlignmentOptions.MidlineLeft, InsightMaxSize);
        hud.insightLabel.color = UiFactory.InsightColor;
        UiFactory.SetAutoSize(hud.insightLabel, 18f, InsightMaxSize);
        hud.rateLabel = UiFactory.CreateLabel(hud.rect, "", AccentWidth + 16f, 62f, textWidth * 0.55f, 36f,
            TextAlignmentOptions.MidlineLeft, RowMaxSize);
        hud.rateLabel.color = UiFactory.RateColor;
        UiFactory.SetAutoSize(hud.rateLabel, 12f, RowMaxSize);
        hud.metaLabel = UiFactory.CreateLabel(hud.rect, "", AccentWidth + 16f + textWidth * 0.58f, 62f,
            textWidth * 0.42f, 36f, TextAlignmentOptions.MidlineRight, RowMaxSize);
        hud.metaLabel.color = UiFactory.MetaColor;
        UiFactory.SetAutoSize(hud.metaLabel, 12f, RowMaxSize);
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

        SetText(insightLabel, string.Format(STRINGS.UI.HUD.INSIGHT, NumberFormat.FormatScaled(state.KnowledgeScaled)));
        SetText(rateLabel, string.Format(STRINGS.UI.HUD.RATE, NumberFormat.Format(state.ProductionTier1)));
        // Meta-Insight is only mentioned once the colony has some.
        SetText(metaLabel, state.MetaInsightEarned > 0
            ? string.Format(STRINGS.UI.HUD.MI, NumberFormat.FormatWhole(state.MetaInsight))
            : "");
        Place();
    }

    private static void SetText(LocText target, string value)
    {
        if (target.text != value)
        {
            target.text = value;
        }
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
