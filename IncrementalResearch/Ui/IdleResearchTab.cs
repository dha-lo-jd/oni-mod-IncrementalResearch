using System.Collections.Generic;
using IncrementalResearch.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IncrementalResearch.Ui;

// The idle research tab of the research screen: a toggle button on top of the tech tree and a panel that covers it.
// The host stays active so it can show or hide the button as the idle game unlocks.
internal sealed class IdleResearchTab : MonoBehaviour
{
    private const float RefreshInterval = 0.25f;
    // The panel covers the whole screen; its content starts below the toggle button.
    private const float ContentTopMargin = 56f;
    private const float UpgradeRowHeight = 56f;
    private const float ContentMinHeight = 800f;
    private static readonly Color SelectedButtonColor = new Color(0.30f, 0.45f, 0.65f, 1f);
    // The upgrade list starts lower when the paradigm shift block above it is shown.
    private const float ParadigmTop = 420f;
    private const float UpgradeHeaderTop = 420f;
    private const float UpgradeHeaderTopWithParadigm = 600f;
    private const float SchoolBlockHeight = 100f;

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
    private Button expandTenButton;
    private Button expandMaxButton;
    private LocText upgradesHeader;
    private GameObject debugRow;
    private LocText schoolLabel;
    private LocText schoolPickLabel;
    private readonly List<(string id, Button button, Image image)> schoolButtons =
        new List<(string, Button, Image)>();
    // School picked for the next cycle; null means none. Starts as the current school.
    private string pickedSchool;
    private bool schoolPickInitialized;
    private RectTransform scrollContent;
    private LocText paradigmHeader;
    private LocText miLabel;
    private LocText miUnitLabel;
    private LocText producedLabel;
    private Button shiftButton;
    private LocText shiftLabel;
    private Button miExpandButton;
    private LocText miExpandLabel;
    private LocText purchasedLabel;
    private readonly List<UpgradeRow> upgradeRows = new List<UpgradeRow>();

    private float nextRefresh;

    private sealed class UpgradeRow
    {
        public UpgradeDef Def;
        public GameObject Root;
        public LocText Name;
        public LocText Description;
        public Button Buy;
        public LocText BuyLabel;
    }

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
        // The content scrolls with the mouse wheel, since the lists below can grow taller than the screen.
        var viewport = UiFactory.CreateStretched("Viewport", panelRect, 0f, ContentTopMargin, 0f, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var catcher = viewport.gameObject.AddComponent<Image>();
        catcher.color = new Color(0f, 0f, 0f, 0f);
        var content = UiFactory.CreateStretched("Content", viewport, 0f, 0f, 0f, 0f);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.offsetMin = new Vector2(0f, -ContentMinHeight);
        content.offsetMax = Vector2.zero;
        scrollContent = content;
        var scroll = viewport.gameObject.AddComponent<KScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        UiFactory.CreateLabel(content, STRINGS.UI.TAB.TITLE, 40f, 30f, 600f, 40f, fontSize: 28f);
        insightLabel = UiFactory.CreateLabel(content, "", 40f, 90f, 600f, 30f, fontSize: 22f);
        baseRateLabel = UiFactory.CreateLabel(content, "", 40f, 125f, 600f, 30f);
        statusLabel = UiFactory.CreateLabel(content, "", 40f, 160f, 900f, 30f);

        UiFactory.CreateLabel(content, STRINGS.UI.ARCHIVE.SIDESCREEN_TITLE, 40f, 230f, 600f, 34f, fontSize: 22f);
        unitLabel = UiFactory.CreateLabel(content, "", 60f, 270f, 600f, 30f);
        productionLabel = UiFactory.CreateLabel(content, "", 60f, 305f, 600f, 30f);
        expandButton = UiFactory.CreateButton(content, "", 60f, 350f, 460f, 40f, Expand, out expandLabel);

        expandTenButton = UiFactory.CreateButton(content, STRINGS.UI.UPGRADES.BULK_TEN, 540f, 350f, 90f, 40f,
            () => ExpandMany(10), out _);
        expandMaxButton = UiFactory.CreateButton(content, STRINGS.UI.UPGRADES.BULK_MAX, 640f, 350f, 90f, 40f,
            () => ExpandMany(int.MaxValue), out _);

        paradigmHeader = UiFactory.CreateLabel(content, STRINGS.UI.PARADIGM.HEADER, 40f, ParadigmTop, 600f, 34f, fontSize: 22f);
        miLabel = UiFactory.CreateLabel(content, "", 60f, ParadigmTop + 40f, 960f, 30f);
        miUnitLabel = UiFactory.CreateLabel(content, "", 60f, ParadigmTop + 75f, 400f, 30f);
        producedLabel = UiFactory.CreateLabel(content, "", 460f, ParadigmTop + 75f, 560f, 30f);
        shiftButton = UiFactory.CreateButton(content, "", 60f, ParadigmTop + 115f, 380f, 40f, Shift, out shiftLabel);
        miExpandButton = UiFactory.CreateButton(content, "", 460f, ParadigmTop + 115f, 460f, 40f, ExpandMi,
            out miExpandLabel);

        schoolLabel = UiFactory.CreateLabel(content, "", 60f, ParadigmTop + 165f, 960f, 30f);
        schoolPickLabel = UiFactory.CreateLabel(content, STRINGS.UI.SCHOOLS.PICK, 60f, ParadigmTop + 205f, 200f, 30f);
        var pickX = 270f;
        AddSchoolButton(content, null, STRINGS.UI.SCHOOLS.NONE, ref pickX);
        foreach (var school in Schools.All)
        {
            AddSchoolButton(content, school.Id, school.Name.text, ref pickX);
        }

        upgradesHeader = UiFactory.CreateLabel(content, STRINGS.UI.UPGRADES.HEADER, 40f, UpgradeHeaderTop, 600f, 34f, fontSize: 22f);
        foreach (var def in Upgrades.All)
        {
            var row = new UpgradeRow { Def = def };
            var captured = def;
            row.Root = UiFactory.CreateRect("UpgradeRow", content, 0f, 0f, 1000f, UpgradeRowHeight).gameObject;
            var rowRect = (RectTransform)row.Root.transform;
            row.Name = UiFactory.CreateLabel(rowRect, def.Name.text, 60f, 0f, 640f, 28f, fontSize: 20f);
            row.Description = UiFactory.CreateLabel(rowRect, def.Description.text, 60f, 28f, 640f, 28f);
            row.Buy = UiFactory.CreateButton(rowRect, "", 720f, 6f, 300f, 40f, () => BuyUpgrade(captured), out row.BuyLabel);
            upgradeRows.Add(row);
        }

        purchasedLabel = UiFactory.CreateLabel(content, "", 60f, 0f, 960f, 30f);

        BuildDebugRow(viewport);

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

    private void ExpandMany(int count)
    {
        KnowledgeState.Instance?.TryExpandTier1Many(count);
        Refresh();
    }

    // Shortcuts for testing, shown only in the game's debug mode. Not translated.
    private void BuildDebugRow(RectTransform content)
    {
        var rect = UiFactory.CreateRect("DebugRow", content, 0f, 0f, 1000f, 44f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(40f, 20f);
        debugRow = rect.gameObject;
        var x = 0f;
        foreach (var (text, action) in new (string, UnityAction)[]
                 {
                     ("[Debug] +1e3 Insight", () => KnowledgeState.Instance?.DebugAddInsight(1000)),
                     ("+1e6", () => KnowledgeState.Instance?.DebugAddInsight(1000000)),
                     ("+1e9", () => KnowledgeState.Instance?.DebugAddInsight(1000000000)),
                     ("+10 MI", () => KnowledgeState.Instance?.DebugAddMetaInsight(10)),
                     ("Reset tutorial", () => KnowledgeState.Instance?.DebugResetTutorial()),
                 })
        {
            var width = text.Length > 8 ? 200f : 100f;
            UiFactory.CreateButton(rect, text, x, 0f, width, 40f, action, out _);
            x += width + 10f;
        }

        debugRow.SetActive(false);
    }

    private void AddSchoolButton(RectTransform parent, string id, string text, ref float x)
    {
        var captured = id;
        var button = UiFactory.CreateButton(parent, text, x, ParadigmTop + 200f, 160f, 40f,
            () => pickedSchool = captured, out _);
        schoolButtons.Add((id, button, button.GetComponent<Image>()));
        x += 170f;
    }

    private void Shift()
    {
        var state = KnowledgeState.Instance;
        if (state == null || !state.CanShift)
        {
            return;
        }

        var schoolName = state.SchoolsUnlocked
            ? (Schools.Find(pickedSchool)?.Name.text ?? STRINGS.UI.SCHOOLS.NONE.text)
            : null;
        var school = state.SchoolsUnlocked ? pickedSchool : null;
        ShiftDialog.Show(state.MetaInsightGain, schoolName, () =>
        {
            KnowledgeState.Instance?.TryShift(school);
            Refresh();
        });
    }

    private void ExpandMi()
    {
        KnowledgeState.Instance?.TryExpandMi();
        Refresh();
    }

    private void BuyUpgrade(UpgradeDef def)
    {
        KnowledgeState.Instance?.TryBuyUpgrade(def);
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
        var debug = DebugHandler.enabled;
        if (debugRow.activeSelf != debug)
        {
            debugRow.SetActive(debug);
        }

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
        var bulk = state.HasUpgrade(Upgrades.Bulk);
        expandTenButton.gameObject.SetActive(bulk);
        expandMaxButton.gameObject.SetActive(bulk);
        expandTenButton.interactable = state.CanExpandTier1();
        expandMaxButton.interactable = state.CanExpandTier1();

        var paradigm = state.ShiftCount > 0 || state.MetaInsight > 0 || state.CanShift;
        paradigmHeader.gameObject.SetActive(paradigm);
        miLabel.gameObject.SetActive(paradigm);
        miUnitLabel.gameObject.SetActive(paradigm);
        producedLabel.gameObject.SetActive(paradigm);
        shiftButton.gameObject.SetActive(paradigm);
        miExpandButton.gameObject.SetActive(paradigm);
        if (paradigm)
        {
            miLabel.text = string.Format(STRINGS.UI.PARADIGM.MI_LINE, NumberFormat.FormatWhole(state.MetaInsight),
                NumberFormat.FormatWhole(state.MetaInsightEarned), NumberFormat.Format(state.MetaInsightMultiplier));
            miUnitLabel.text = string.Format(STRINGS.UI.TAB.UNIT_VALUE, STRINGS.UI.PARADIGM.MI_UNIT, state.ScaleMi);
            producedLabel.text = string.Format(STRINGS.UI.PARADIGM.PRODUCED,
                NumberFormat.FormatScaled(state.ProducedScaled));
            shiftLabel.text = state.CanShift
                ? string.Format(STRINGS.UI.PARADIGM.SHIFT_BUTTON, NumberFormat.FormatWhole(state.MetaInsightGain))
                : string.Format(STRINGS.UI.PARADIGM.GAIN_NONE, NumberFormat.Format(KnowledgeRules.MiThreshold));
            shiftButton.interactable = state.CanShift;
            miExpandLabel.text = string.Format(STRINGS.UI.PARADIGM.MI_EXPAND_BUTTON, STRINGS.UI.PARADIGM.MI_UNIT,
                NumberFormat.FormatWhole(state.NextMiExpansionCost));
            miExpandButton.interactable = state.CanExpandMi();
        }

        var schools = paradigm && state.SchoolsUnlocked;
        if (!schoolPickInitialized)
        {
            pickedSchool = state.CurrentSchool?.Id;
            schoolPickInitialized = true;
        }

        schoolLabel.gameObject.SetActive(schools);
        schoolPickLabel.gameObject.SetActive(schools);
        foreach (var (id, button, image) in schoolButtons)
        {
            button.gameObject.SetActive(schools);
            image.color = id == pickedSchool ? SelectedButtonColor : UiFactory.ButtonColor;
        }

        if (schools)
        {
            var current = state.CurrentSchool;
            schoolLabel.text = current == null
                ? string.Format(STRINGS.UI.SCHOOLS.CURRENT, STRINGS.UI.SCHOOLS.NONE.text)
                : string.Format(STRINGS.UI.SCHOOLS.CURRENT, current.Name.text + " - " + current.Description.text);
        }

        RefreshUpgrades(state, paradigm, schools);
    }

    private void RefreshUpgrades(KnowledgeState state, bool paradigm, bool schools)
    {
        var headerTop = paradigm ? UpgradeHeaderTopWithParadigm : UpgradeHeaderTop;
        if (schools)
        {
            headerTop += SchoolBlockHeight;
        }

        UiFactory.Place((RectTransform)upgradesHeader.transform, 40f, headerTop, 600f, 34f);
        var y = headerTop + 40f;
        foreach (var row in upgradeRows)
        {
            var visible = state.IsUpgradeVisible(row.Def);
            row.Root.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            UiFactory.Place((RectTransform)row.Root.transform, 0f, y, 1000f, UpgradeRowHeight);
            y += UpgradeRowHeight;
            row.BuyLabel.text = row.Def.Currency == UpgradeCurrency.Insight
                ? string.Format(STRINGS.UI.UPGRADES.BUY, NumberFormat.FormatScaled(row.Def.CostAmount))
                : string.Format(STRINGS.UI.UPGRADES.BUY_MI, NumberFormat.FormatWhole(row.Def.CostAmount));
            row.Buy.interactable = state.CanBuyUpgrade(row.Def);
        }

        var names = new List<string>();
        foreach (var def in Upgrades.All)
        {
            if (state.HasUpgrade(def.Id))
            {
                names.Add(def.Name.text);
            }
        }

        purchasedLabel.gameObject.SetActive(names.Count > 0);
        if (names.Count > 0)
        {
            UiFactory.Place((RectTransform)purchasedLabel.transform, 60f, y + 10f, 960f, 30f);
            purchasedLabel.text = string.Format(STRINGS.UI.UPGRADES.PURCHASED, string.Join(", ", names));
            y += 50f;
        }

        // Room for the debug row at the bottom of the viewport.
        scrollContent.offsetMin = new Vector2(0f, -Mathf.Max(ContentMinHeight, y + 80f));
    }
}
