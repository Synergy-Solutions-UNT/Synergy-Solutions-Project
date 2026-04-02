using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// JANUS — Menu Setup (Programmatic UI Builder)
///
/// Builds the entire JANUS menu UI hierarchy at runtime so you don't need
/// to manually assemble dozens of GameObjects in the Inspector. Attach this
/// to the same JANUS_Menu Canvas GameObject as JANUSMenuManager.
///
/// RUN ORDER: This runs in Awake() before JANUSMenuManager.Start(), so all
/// UI elements exist by the time WireButtons() is called.
///
/// The generated hierarchy matches the JANUS_Menu_Preview mockup:
///
///   JANUS_Menu (Canvas)
///     Panel_Root
///       Header            — title, device status dots
///       Section_Session   — patient ID, progress, elapsed time
///       Section_FloorPlans— 3 selectable layout cards
///       Section_Modules   — numbered module rows with status
///       Footer_Controls   — Begin Module / Pause / End Session
///       Footer_Status     — version, canvas info, date
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(JANUSMenuManager))]
public class JANUSMenuSetup : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    // Palette — matches the neutral/warm tones in the mockup
    // ─────────────────────────────────────────────────────────────────

    private static readonly Color colBg         = new Color(0.96f, 0.95f, 0.93f, 1f);  // warm off-white
    private static readonly Color colPanel      = new Color(1f, 1f, 1f, 1f);            // white cards
    private static readonly Color colBorder     = new Color(0.85f, 0.83f, 0.80f, 1f);   // subtle border
    private static readonly Color colLabel      = new Color(0.55f, 0.53f, 0.48f, 1f);   // muted label
    private static readonly Color colText       = new Color(0.18f, 0.17f, 0.15f, 1f);   // dark text
    private static readonly Color colAccent     = new Color(0.28f, 0.42f, 0.35f, 1f);   // muted green
    private static readonly Color colCardBorder = new Color(0.78f, 0.76f, 0.72f, 1f);   // card outline
    private static readonly Color colDivider    = new Color(0.88f, 0.86f, 0.82f, 1f);   // section dividers

    // Status colors
    private static readonly Color colInProgress = new Color(0.28f, 0.42f, 0.35f, 1f);
    private static readonly Color colComplete   = new Color(0.28f, 0.42f, 0.35f, 1f);
    private static readonly Color colPending    = new Color(0.65f, 0.63f, 0.58f, 1f);

    private JANUSMenuManager _manager;

    // ── Tracked references for auto-wiring ──────────────────────────
    private Text _patientIDText;
    private Text _sessionCounterText;
    private Text _elapsedText;
    private JANUSMenuManager.FloorPlanCard[] _floorCards;
    private JANUSMenuManager.ModuleRow[] _moduleRows;
    private Button _beginButton;
    private Button _pauseButton;
    private Button _endButton;
    private Text _statusText;

    private void Awake()
    {
        _manager = GetComponent<JANUSMenuManager>();
        _floorCards = new JANUSMenuManager.FloorPlanCard[3];
        BuildUI();

        // Auto-wire all references into the manager
        _manager.WireFromSetup(
            _patientIDText, _sessionCounterText, _elapsedText,
            _floorCards, _moduleRows,
            _beginButton, _pauseButton, _endButton, _statusText);
    }

    // ─────────────────────────────────────────────────────────────────
    // Main build
    // ─────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Clean any existing children
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // Root panel fills the canvas
        var root = CreatePanel("Panel_Root", transform, colBg);
        var rootRect = root.GetComponent<RectTransform>();
        StretchFill(rootRect);

        var rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(24, 24, 16, 16);
        rootLayout.spacing = 12f;
        rootLayout.childForceExpandWidth  = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childControlWidth      = true;
        rootLayout.childControlHeight     = true;

        var rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Header ──────────────────────────────────────────────────
        BuildHeader(root.transform);

        // ── Session Info ────────────────────────────────────────────
        BuildSessionSection(root.transform);

        // ── Divider ─────────────────────────────────────────────────
        CreateDivider(root.transform);

        // ── Floor Plans ─────────────────────────────────────────────
        BuildFloorPlanSection(root.transform);

        // ── Divider ─────────────────────────────────────────────────
        CreateDivider(root.transform);

        // ── Assessment Modules ──────────────────────────────────────
        BuildModulesSection(root.transform);

        // ── Divider ─────────────────────────────────────────────────
        CreateDivider(root.transform);

        // ── Footer Controls ─────────────────────────────────────────
        BuildFooterControls(root.transform);

        // ── Footer Status ───────────────────────────────────────────
        BuildFooterStatus(root.transform);
    }

    // ─────────────────────────────────────────────────────────────────
    // Section Builders
    // ─────────────────────────────────────────────────────────────────

    private void BuildHeader(Transform parent)
    {
        var header = CreateRow("Header", parent, 40f);

        // Title
        var title = CreateText("Text_Title", header.transform, "JANUS", 22, FontStyle.Bold, colText);
        title.GetComponent<LayoutElement>().flexibleWidth = 0;
        title.GetComponent<LayoutElement>().preferredWidth = 120;

        var subtitle = CreateText("Text_Subtitle", header.transform, "Assessment System", 12, FontStyle.Normal, colLabel);
        subtitle.GetComponent<LayoutElement>().flexibleWidth = 1;

        // Status dots — Headset, Tracking, Controller
        string[] statuses = { "Headset", "Tracking", "L. Controller" };
        foreach (var s in statuses)
        {
            var dot = CreateText("Status_" + s, header.transform, s, 10, FontStyle.Normal, colLabel);
            dot.GetComponent<LayoutElement>().flexibleWidth  = 0;
            dot.GetComponent<LayoutElement>().preferredWidth = 80;
            var dotText = dot.GetComponent<Text>();
            dotText.alignment = TextAnchor.MiddleRight;
        }
    }

    private void BuildSessionSection(Transform parent)
    {
        // Section label
        CreateSectionLabel("SESSION", parent);

        // Card row with 3 cells: Patient, Progress, Elapsed
        var card = CreatePanel("Card_Session", parent, colPanel);
        AddOutline(card, colBorder);
        var cardLayout = card.AddComponent<HorizontalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 12, 12);
        cardLayout.spacing = 24f;
        cardLayout.childForceExpandWidth  = true;
        cardLayout.childForceExpandHeight = false;

        var cardLE = card.AddComponent<LayoutElement>();
        cardLE.preferredHeight = 60;

        // Patient
        var patientCell = CreateVerticalCell("Cell_Patient", card.transform);
        CreateText("Label_Patient", patientCell.transform, "PATIENT", 9, FontStyle.Normal, colLabel);
        var pid = CreateText("Text_PatientID", patientCell.transform, "PTN-2026-0047", 14, FontStyle.Bold, colText);
        _patientIDText = pid.GetComponent<Text>();

        // Progress
        var progressCell = CreateVerticalCell("Cell_Progress", card.transform);
        CreateText("Label_Progress", progressCell.transform, "PROGRESS", 9, FontStyle.Normal, colLabel);
        var sc = CreateText("Text_SessionCounter", progressCell.transform, "Session 3 of 5", 14, FontStyle.Bold, colText);
        _sessionCounterText = sc.GetComponent<Text>();

        // Elapsed
        var elapsedCell = CreateVerticalCell("Cell_Elapsed", card.transform);
        CreateText("Label_Elapsed", elapsedCell.transform, "ELAPSED", 9, FontStyle.Normal, colLabel);
        var el = CreateText("Text_Elapsed", elapsedCell.transform, "00:00", 14, FontStyle.Bold, colText);
        _elapsedText = el.GetComponent<Text>();
    }

    private void BuildFloorPlanSection(Transform parent)
    {
        CreateSectionLabel("ENVIRONMENT — FLOOR PLAN", parent);

        var row = CreateRow("Section_FloorPlans", parent, 130f);
        row.GetComponent<HorizontalLayoutGroup>().spacing = 12f;

        string[] names = { "Layout A", "Layout B", "Layout C" };
        string[] descs = { "3 rooms · Standard", "4 rooms · Extended", "5 rooms · Complex" };

        for (int i = 0; i < 3; i++)
        {
            var card = CreatePanel($"Card_FloorPlan_{i}", row.transform, colPanel);
            AddOutline(card, colCardBorder);

            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.flexibleWidth = 1;
            cardLE.preferredHeight = 120;

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(8, 8, 8, 8);
            cardLayout.spacing = 4f;
            cardLayout.childAlignment         = TextAnchor.MiddleCenter;
            cardLayout.childForceExpandWidth   = true;
            cardLayout.childForceExpandHeight  = false;

            // Thumbnail placeholder
            var thumb = CreatePanel("Thumbnail", card.transform, new Color(0.92f, 0.90f, 0.87f, 1f));
            var thumbLE = thumb.AddComponent<LayoutElement>();
            thumbLE.preferredHeight = 60;

            // Checkmark (hidden by default, shown on selected)
            var check = CreateText("CheckMark", card.transform, "✓", 14, FontStyle.Bold, colAccent);
            check.GetComponent<Text>().alignment = TextAnchor.UpperRight;
            check.SetActive(i == 0); // first selected by default

            // Name and description
            var nameGo = CreateText("Text_Name", card.transform, names[i], 12, FontStyle.Bold, colText);
            var descGo = CreateText("Text_Desc", card.transform, descs[i], 9, FontStyle.Normal, colLabel);

            // Button component for click interaction
            var btn = card.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.94f, 0.96f, 0.94f, 1f);
            colors.pressedColor     = new Color(0.88f, 0.92f, 0.88f, 1f);
            btn.colors = colors;

            // Track for auto-wiring
            _floorCards[i] = new JANUSMenuManager.FloorPlanCard
            {
                CardButton   = btn,
                OutlineImage = card.GetComponent<Image>(),
                CheckMark    = check,
                NameText     = nameGo.GetComponent<Text>(),
                DescText     = descGo.GetComponent<Text>()
            };
        }
    }

    private void BuildModulesSection(Transform parent)
    {
        CreateSectionLabel("ASSESSMENT MODULES", parent);

        var container = new GameObject("Section_Modules");
        container.transform.SetParent(parent, false);
        var containerLayout = container.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 6f;
        containerLayout.childForceExpandWidth  = true;
        containerLayout.childForceExpandHeight = false;
        containerLayout.childControlWidth      = true;
        containerLayout.childControlHeight     = true;

        // Module data
        string[] nums    = { "01", "02", "03" };
        string[] titles  = { "Spatial Navigation", "Attention & Focus", "Memory Recall" };
        string[] descs   = {
            "Orientation and wayfinding within selected layout",
            "Sustained attention protocol",
            "Short and long-term encoding tasks"
        };
        string[] statuses = { "In progress", "Complete", "Pending" };
        Color[]  statusCols = { colInProgress, colComplete, colPending };
        string[] moduleIDs = { "spatial_nav", "attention", "memory_recall" };

        _moduleRows = new JANUSMenuManager.ModuleRow[3];

        for (int i = 0; i < 3; i++)
        {
            var row = CreatePanel($"Row_Module_{i}", container.transform, colPanel);
            AddOutline(row, colBorder);

            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 55;

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(16, 16, 8, 8);
            rowLayout.spacing = 12f;
            rowLayout.childForceExpandWidth  = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment         = TextAnchor.MiddleLeft;

            // Number
            var num = CreateText("Text_Num", row.transform, nums[i], 12, FontStyle.Normal, colLabel);
            num.GetComponent<LayoutElement>().preferredWidth = 24;

            // Title + Description vertical group
            var info = CreateVerticalCell("Cell_Info", row.transform);
            info.GetComponent<LayoutElement>().flexibleWidth = 1;
            var titleGo = CreateText("Text_Title", info.transform, titles[i], 13, FontStyle.Bold, colText);
            CreateText("Text_Desc", info.transform, descs[i], 10, FontStyle.Normal, colLabel);

            // Status
            var status = CreateText("Text_Status", row.transform, statuses[i], 11, FontStyle.Italic, statusCols[i]);
            status.GetComponent<LayoutElement>().preferredWidth = 80;
            status.GetComponent<Text>().alignment = TextAnchor.MiddleRight;

            // Button for selection
            var btn = row.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.94f, 0.96f, 0.94f, 1f);
            colors.pressedColor     = new Color(0.88f, 0.92f, 0.88f, 1f);
            btn.colors = colors;

            // Track for auto-wiring
            _moduleRows[i] = new JANUSMenuManager.ModuleRow
            {
                RowButton  = btn,
                ModuleID   = moduleIDs[i],
                LabelText  = titleGo.GetComponent<Text>(),
                Background = row.GetComponent<Image>()
            };
        }
    }

    private void BuildFooterControls(Transform parent)
    {
        var row = CreateRow("Footer_Controls", parent, 48f);
        row.GetComponent<HorizontalLayoutGroup>().spacing = 12f;

        // Begin Module (primary)
        _beginButton = CreateFooterButton("Btn_Begin", row.transform, "Begin Module", colAccent, Color.white);

        // Pause (secondary)
        _pauseButton = CreateFooterButton("Btn_Pause", row.transform, "Pause", colPanel, colLabel);

        // End Session (secondary)
        _endButton = CreateFooterButton("Btn_End", row.transform, "End Session", colPanel, colLabel);
    }

    private void BuildFooterStatus(Transform parent)
    {
        var row = CreateRow("Footer_Status", parent, 20f);

        CreateText("Text_Version", row.transform, "JANUS · v1.0 · Unity 2023 LTS", 8, FontStyle.Normal, colLabel)
            .GetComponent<LayoutElement>().flexibleWidth = 1;

        var canvas = CreateText("Text_Canvas", row.transform, "World Space Canvas · 1 m", 8, FontStyle.Normal, colLabel);
        canvas.GetComponent<LayoutElement>().flexibleWidth = 1;
        canvas.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        var date = CreateText("Text_Date", row.transform, System.DateTime.Now.ToString("yyyy-MM-dd"), 8, FontStyle.Normal, colLabel);
        date.GetComponent<LayoutElement>().flexibleWidth = 1;
        date.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private GameObject CreatePanel(string name, Transform parent, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bg;
        return go;
    }

    private void AddOutline(GameObject panel, Color color, float width = 1f)
    {
        var outline = panel.AddComponent<Outline>();
        outline.effectColor    = color;
        outline.effectDistance  = new Vector2(width, -width);
    }

    private GameObject CreateText(string name, Transform parent, string content,
                                   int size, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.text      = content;
        text.fontSize  = size;
        text.fontStyle = style;
        text.color     = color;
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleLeft;

        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = size + 8;

        return go;
    }

    private GameObject CreateRow(string name, Transform parent, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = true;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        return go;
    }

    private GameObject CreateVerticalCell(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        go.AddComponent<LayoutElement>();

        return go;
    }

    private void CreateSectionLabel(string text, Transform parent)
    {
        var label = CreateText("Label_" + text.Replace(" ", ""), parent, text, 9, FontStyle.Normal, colLabel);
        label.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        var le = label.GetComponent<LayoutElement>();
        le.preferredHeight = 20;
    }

    private void CreateDivider(Transform parent)
    {
        var div = CreatePanel("Divider", parent, colDivider);
        var le = div.AddComponent<LayoutElement>();
        le.preferredHeight = 1;
        le.flexibleWidth   = 1;
    }

    private Button CreateFooterButton(string name, Transform parent,
                                     string label, Color bg, Color textColor)
    {
        var go = CreatePanel(name, parent, bg);
        AddOutline(go, colCardBorder);

        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1;
        le.preferredHeight = 42;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(bg.r * 0.95f, bg.g * 0.95f, bg.b * 0.95f, 1f);
        colors.pressedColor     = new Color(bg.r * 0.85f, bg.g * 0.85f, bg.b * 0.85f, 1f);
        btn.colors = colors;

        var text = CreateText("Text_Label", go.transform, label, 13, FontStyle.Bold, textColor);
        text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        StretchFill(text.GetComponent<RectTransform>());

        return btn;
    }

    private void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
