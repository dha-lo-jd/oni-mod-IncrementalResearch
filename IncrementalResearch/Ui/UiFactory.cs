using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IncrementalResearch.Ui;

// Builds the few widgets the idle research tab needs in code. Text reuses the game's own tooltip text style
// (a LocText without a style renders invisible), and layout rects are top-left anchored with y growing downward.
internal static class UiFactory
{
    public static readonly Color PanelColor = new(0.192f, 0.205f, 0.258f, 1f);
    public static readonly Color ButtonColor = new(0.243f, 0.263f, 0.341f, 1f);
    public static readonly Color ButtonDisabledColor = new(0.15f, 0.16f, 0.2f, 1f);

    public static RectTransform CreateRect(string name, Transform parent, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return Place(go.GetComponent<RectTransform>(), x, y, width, height);
    }

    public static RectTransform Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        // Called on every refresh; writing unchanged values would still dirty the layout and disturb scrolling.
        var position = new Vector2(x, -y);
        if (rect.anchoredPosition != position)
        {
            rect.anchoredPosition = position;
        }

        var size = new Vector2(width, height);
        if (rect.sizeDelta != size)
        {
            rect.sizeDelta = size;
        }

        return rect;
    }

    public static RectTransform CreateStretched(string name, Transform parent, float left, float top, float right, float bottom)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    public static LocText CreateLabel(Transform parent, string text, float x, float y, float width, float height,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left, float fontSize = 0f)
    {
        var rect = CreateRect("Label", parent, x, y, width, height);

        // Awake runs on activation; configure first so LocText doesn't look up a null string key.
        rect.gameObject.SetActive(false);
        var label = rect.gameObject.AddComponent<LocText>();
        label.key = "";
        label.allowOverride = true;
        if (ToolTipScreen.Instance != null)
        {
            label.textStyleSetting = ToolTipScreen.Instance.defaultTooltipBodyStyle;
        }

        rect.gameObject.SetActive(true);

        label.ApplySettings();
        label.alignment = alignment;
        if (fontSize > 0f)
        {
            label.fontSize = fontSize;
        }

        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = text;
        return label;
    }

    public static Button CreateButton(Transform parent, string text, float x, float y, float width, float height,
        UnityAction onClick, out LocText label)
    {
        var rect = CreateRect("Button", parent, x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = ButtonColor;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);
        label = CreateLabel(rect, text, 0f, 0f, width, height, TextAlignmentOptions.Center);
        return button;
    }
}
