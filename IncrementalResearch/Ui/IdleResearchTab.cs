using IncrementalResearch.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IncrementalResearch.Ui;

// The idle research tab of the research screen: a toggle button on top of the tech tree and a panel that covers it.
// The host stays active so it can show or hide the button as the idle game unlocks.
internal sealed class IdleResearchTab : MonoBehaviour
{
    private const float RefreshInterval = 0.25f;
    // The panel covers the whole screen; its content starts below the toggle button.
    private const float ContentTopMargin = 56f;

    private ResearchScreen screen;
    private RectTransform host;
    private RectTransform sideBarRect;
    private RectTransform toggleRect;
    private GameObject toggleButton;
    private LocText toggleLabel;
    private GameObject panel;

    private LocText insightLabel;
    private LocText baseRateLabel;
    private LocText statusLabel;
    private LocText unitLabel;
    private LocText productionLabel;
    private Button expandButton;
    private LocText expandLabel;

    private float nextRefresh;

    public static IdleResearchTab Instance { get; private set; }

    // The panel only counts as open while the research screen itself is shown.
    public bool IsOpen => panel != null && panel.activeSelf && screen != null && screen.IsScreenActive();

    public static void Create(ResearchScreen screen)
    {
        var host = UiFactory.CreateStretched("IdleResearchTab", screen.transform, 0f, 0f, 0f, 0f);
        var tab = host.gameObject.AddComponent<IdleResearchTab>();
        Instance = tab;
        tab.host = host;
        tab.screen = screen;
        var sideBar = HarmonyLib.Traverse.Create(screen).Field("sideBar").GetValue<Component>();
        tab.sideBarRect = sideBar != null ? sideBar.transform as RectTransform : null;
        tab.Build(host);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Build(RectTransform host)
    {
        // Toggle button, top left, right of the research screen's side bar.
        var button = UiFactory.CreateButton(host, STRINGS.UI.TAB.OPEN, 0f, 0f, 200f, 36f, Toggle, out toggleLabel);
        toggleRect = (RectTransform)button.transform;
        toggleButton = button.gameObject;
        PlaceToggleButton();

        // Panel covering the tech tree.
        var panelRect = UiFactory.CreateStretched("Panel", host, 0f, 0f, 0f, 0f);
        var background = panelRect.gameObject.AddComponent<Image>();
        background.color = UiFactory.PanelColor;
        panel = panelRect.gameObject;
        var content = UiFactory.CreateStretched("Content", panelRect, 0f, ContentTopMargin, 0f, 0f);

        UiFactory.CreateLabel(content, STRINGS.UI.TAB.TITLE, 40f, 30f, 600f, 40f, fontSize: 28f);
        insightLabel = UiFactory.CreateLabel(content, "", 40f, 90f, 600f, 30f, fontSize: 22f);
        baseRateLabel = UiFactory.CreateLabel(content, "", 40f, 125f, 600f, 30f);
        statusLabel = UiFactory.CreateLabel(content, "", 40f, 160f, 900f, 30f);

        UiFactory.CreateLabel(content, STRINGS.UI.ARCHIVE.SIDESCREEN_TITLE, 40f, 230f, 600f, 34f, fontSize: 22f);
        unitLabel = UiFactory.CreateLabel(content, "", 60f, 270f, 600f, 30f);
        productionLabel = UiFactory.CreateLabel(content, "", 60f, 305f, 600f, 30f);
        expandButton = UiFactory.CreateButton(content, "", 60f, 350f, 460f, 40f, Expand, out expandLabel);

        // The button stays above the panel.
        toggleRect.SetAsLastSibling();

        panel.SetActive(false);
        toggleButton.SetActive(false);
    }

    private void Toggle()
    {
        SetOpen(!panel.activeSelf);
    }

    // Keeps the button clear of the side bar (on the left) and the management menu (on the right).
    private void PlaceToggleButton()
    {
        var x = 340f;
        if (sideBarRect != null && sideBarRect.rect.width > 0f)
        {
            var corners = new Vector3[4];
            sideBarRect.GetWorldCorners(corners);
            x = host.InverseTransformPoint(corners[2]).x - host.rect.xMin + 10f;
        }

        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(x, -4f);
    }

    public void SetOpen(bool open)
    {
        panel.SetActive(open);
        toggleLabel.text = open ? STRINGS.UI.TAB.CLOSE : STRINGS.UI.TAB.OPEN;
        Refresh();
    }

    private void Expand()
    {
        KnowledgeState.Instance?.TryExpandTier1();
        Refresh();
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
        PlaceToggleButton();
        var state = KnowledgeState.Instance;
        var unlocked = state != null && state.Unlocked;
        if (toggleButton.activeSelf != unlocked)
        {
            toggleButton.SetActive(unlocked);
        }

        if (!unlocked)
        {
            if (panel.activeSelf)
            {
                SetOpen(false);
            }

            return;
        }

        if (!panel.activeSelf)
        {
            return;
        }

        insightLabel.text = string.Format(STRINGS.UI.TAB.INSIGHT, NumberFormat.FormatScaled(state.KnowledgeScaled));
        baseRateLabel.text = string.Format(STRINGS.UI.TAB.BASE_RATE, NumberFormat.Format(state.BaseRate));
        statusLabel.text = RecordArchive.AnyOperational ? STRINGS.UI.TAB.STATUS_ACTIVE : STRINGS.UI.TAB.STATUS_INACTIVE;
        unitLabel.text = string.Format(STRINGS.UI.TAB.UNIT_VALUE, STRINGS.UI.ARCHIVE.UNIT, state.HoldingsTier1);
        productionLabel.text = string.Format(STRINGS.UI.TAB.PRODUCTION, NumberFormat.Format(state.ProductionTier1));
        expandLabel.text = string.Format(STRINGS.UI.ARCHIVE.EXPAND_BUTTON, STRINGS.UI.ARCHIVE.UNIT,
            NumberFormat.FormatScaled(state.NextExpansionCostScaled));
        expandButton.interactable = state.CanExpandTier1();
    }
}
