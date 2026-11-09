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
    private const float CompactWidth = 330f;
    private const float CompactHeight = 44f;
    private const float IconSize = 48f;
    private const float CompactAccentWidth = 6f;

    private RectTransform rect;
    private RectTransform anchorRect;
    private LocText insightLabel;
    private LocText rateLabel;
    private LocText metaLabel;
    private LocText iconLabel;
    private RectTransform accentRect;
    private CanvasGroup group;
    private HudMode? appliedMode;
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
        hud.accentRect = UiFactory.CreateRect("Accent", hud.rect, 0f, 0f, AccentWidth, Height);
        var accent = hud.accentRect.gameObject.AddComponent<Image>();
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
        // The icon form shows a single glyph.
        hud.iconLabel = UiFactory.CreateLabel(hud.rect, STRINGS.UI.HUD.ICON_GLYPH, 0f, 0f, IconSize, IconSize,
            TextAlignmentOptions.Center, 30f);
        hud.iconLabel.color = UiFactory.InsightColor;
        hud.iconLabel.gameObject.SetActive(false);

        // Clicking the readout steps it down to a smaller form.
        background.raycastTarget = true;
        var button = go.AddComponent<Button>();
        button.targetGraphic = background;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        button.colors = colors;
        button.onClick.AddListener(HudSettings.Cycle);
        go.AddComponent<ToolTip>().SetSimpleTooltip(STRINGS.UI.HUD.TOOLTIP);

        // Hidden by alpha rather than deactivated, so that Update keeps running.
        hud.group = go.AddComponent<CanvasGroup>();
        hud.group.alpha = 0f;
        hud.group.blocksRaycasts = false;
        hud.group.interactable = false;
        HudSettings.Changed += hud.OnModeChanged;
    }

    private void OnDestroy()
    {
        HudSettings.Changed -= OnModeChanged;
    }

    private void OnModeChanged()
    {
        nextRefresh = 0f;
    }

    // Sizes and arranges the parts for the chosen form.
    private void Apply(HudMode mode)
    {
        appliedMode = mode;
        var full = mode == HudMode.Full;
        var compact = mode == HudMode.Compact;
        var icon = mode == HudMode.Icon;
        var textWidth = Width - AccentWidth - 32f;

        if (full)
        {
            rect.sizeDelta = new Vector2(Width, Height);
            UiFactory.Place(accentRect, 0f, 0f, AccentWidth, Height);
            UiFactory.Place(insightLabel.rectTransform, AccentWidth + 16f, 6f, textWidth, 58f);
            UiFactory.Place(rateLabel.rectTransform, AccentWidth + 16f, 62f, textWidth * 0.55f, 36f);
            UiFactory.Place(metaLabel.rectTransform, AccentWidth + 16f + textWidth * 0.58f, 62f, textWidth * 0.42f, 36f);
            UiFactory.SetAutoSize(insightLabel, 18f, InsightMaxSize);
            UiFactory.SetAutoSize(rateLabel, 12f, RowMaxSize);
        }
        else if (compact)
        {
            rect.sizeDelta = new Vector2(CompactWidth, CompactHeight);
            UiFactory.Place(accentRect, 0f, 0f, CompactAccentWidth, CompactHeight);
            UiFactory.Place(insightLabel.rectTransform, CompactAccentWidth + 10f, 2f, 190f, CompactHeight - 4f);
            UiFactory.Place(rateLabel.rectTransform, CompactAccentWidth + 205f, 2f, CompactWidth - CompactAccentWidth - 215f,
                CompactHeight - 4f);
            UiFactory.SetAutoSize(insightLabel, 12f, 26f);
            UiFactory.SetAutoSize(rateLabel, 10f, 20f);
        }
        else if (icon)
        {
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            UiFactory.Place(accentRect, 0f, 0f, CompactAccentWidth, IconSize);
        }

        insightLabel.gameObject.SetActive(!icon);
        rateLabel.gameObject.SetActive(!icon);
        metaLabel.gameObject.SetActive(full);
        iconLabel.gameObject.SetActive(icon);
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
        var mode = HudSettings.Mode;
        var visible = state != null && state.Unlocked && !tabOpen && mode != HudMode.Hidden;
        group.alpha = visible ? VisibleAlpha : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;

        if (!visible)
        {
            return;
        }

        if (appliedMode != mode)
        {
            Apply(mode);
        }

        if (mode != HudMode.Icon)
        {
            var value = NumberFormat.FormatScaled(state.KnowledgeScaled);
            // The compact form leaves out the caption.
            SetText(insightLabel, mode == HudMode.Full ? string.Format(STRINGS.UI.HUD.INSIGHT, value) : value);
            SetText(rateLabel, string.Format(STRINGS.UI.HUD.RATE, NumberFormat.Format(state.ProductionTier1)));
        }

        if (mode == HudMode.Full)
        {
            // Meta-Insight is only mentioned once the colony has some.
            SetText(metaLabel, state.MetaInsightEarned > 0
                ? string.Format(STRINGS.UI.HUD.MI, NumberFormat.FormatWhole(state.MetaInsight))
                : "");
        }

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
