using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// JANUS — Menu Manager
///
/// Core controller for the JANUS assessment menu. Manages:
///   • Patient info display (ID, session counter)
///   • Floor plan selection (3-card grid)
///   • Module list with selection highlight
///   • Session state machine (Idle / Running / Paused / Ended)
///   • Hardware status bar
///   • Menu visibility and world-space placement
///
/// ATTACH TO: The JANUS_Menu Canvas GameObject.
/// Requires: JANUSMenuSetup, JANUSVRInputHandler, JANUSHardwareMonitor
///           on the same GameObject.
///
/// VISIBILITY: SetVisible hides the menu by disabling the Canvas component
/// and a CanvasGroup, NOT by disabling the entire GameObject. This keeps
/// all scripts (including JANUSVRInputHandler) alive so the Menu button
/// can still be heard when the menu is hidden.
///
/// ── UI HIERARCHY EXPECTED ────────────────────────────────────────────────
/// JANUS_Menu (Canvas)
///   Panel_Root
///     Header
///       Text_Title          ← "JANUS" wordmark
///       Text_PatientID
///       Text_SessionCounter
///     Section_FloorPlans
///       Card_FloorPlan_0
///       Card_FloorPlan_1
///       Card_FloorPlan_2
///     Section_Modules
///       Row_Module_0 … Row_Module_N
///     Footer_Status
///       Text_HardwareStatus
///       Btn_Pause
///       Btn_End
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class JANUSMenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Patient info
    // ─────────────────────────────────────────────────────────────────────

    [Header("Patient Info")]
    [SerializeField] private Text patientIDText;
    [SerializeField] private Text sessionCounterText;

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Floor Plan Cards
    // ─────────────────────────────────────────────────────────────────────

    [Header("Floor Plan Selection")]
    [Tooltip("Assign one FloorPlanData ScriptableObject per card. Order matches card 0, 1, 2.")]
    [SerializeField] private FloorPlanData[] floorPlans = new FloorPlanData[3];

    [Tooltip("The card root GameObjects (each needs a Button, outline Image, and checkmark Image child).")]
    [SerializeField] private FloorPlanCard[] floorPlanCards = new FloorPlanCard[3];

    [System.Serializable]
    public class FloorPlanCard
    {
        public Button    CardButton;
        public Image     OutlineImage;
        public GameObject CheckMark;
        public Text      NameText;
        public Text      DescText;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Module Rows
    // ─────────────────────────────────────────────────────────────────────

    [Header("Assessment Modules")]
    [SerializeField] private ModuleRow[] moduleRows;

    [System.Serializable]
    public class ModuleRow
    {
        public Button    RowButton;
        public string    ModuleID;
        public Text      LabelText;
        public Image     Background;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Session Controls
    // ─────────────────────────────────────────────────────────────────────

    [Header("Session Controls")]
    [SerializeField] private Button beginButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button endButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text elapsedText;

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Colours (minimalist palette)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Colours")]
    [SerializeField] private Color colSelected    = new Color(0.17f, 0.37f, 0.54f, 1f);
    [SerializeField] private Color colUnselected  = new Color(0.88f, 0.86f, 0.82f, 1f);
    [SerializeField] private Color colHwOK        = new Color(0.24f, 0.48f, 0.35f, 1f);
    [SerializeField] private Color colHwWarn       = new Color(0.60f, 0.42f, 0.16f, 1f);
    [SerializeField] private Color colHwCritical   = new Color(0.72f, 0.18f, 0.18f, 1f);

    // ─────────────────────────────────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────────────────────────────────

    private enum SessionState { Idle, Running, Paused, Ended }
    private SessionState _state = SessionState.Idle;

    private string  _patientID      = "—";
    private int     _currentSession = 0;
    private int     _totalSessions  = 0;
    private int     _selectedFloor  = 0;
    private string  _selectedModule = "";
    private float   _sessionStart;
    private bool    _visible        = false;

    private JANUSHardwareMonitor _hw;
    private Canvas               _canvas;
    private CanvasGroup          _canvasGroup;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvas      = GetComponent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _hw          = GetComponent<JANUSHardwareMonitor>();

        // Ensure CanvasGroup exists
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        WireButtons();
        RefreshFloorCards();
        RefreshModuleRows();
        RefreshPatientDisplay();
        RefreshSessionControls();

        if (_hw != null)
            _hw.OnStatusChanged += OnHardwareStatusChanged;

        // Start hidden — shown when clinician presses Menu button
        // This hides the Canvas but keeps the GameObject ACTIVE
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_hw != null)
            _hw.OnStatusChanged -= OnHardwareStatusChanged;
    }

    private void Update()
    {
        // Update elapsed timer while session is running
        if (_state == SessionState.Running && elapsedText != null)
        {
            float elapsed = Time.time - _sessionStart;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            elapsedText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Button wiring
    // ─────────────────────────────────────────────────────────────────────

    private void WireButtons()
    {
        for (int i = 0; i < floorPlanCards.Length; i++)
        {
            int idx = i;
            if (floorPlanCards[i]?.CardButton != null)
                floorPlanCards[i].CardButton.onClick.AddListener(() => SelectFloor(idx));
        }

        foreach (var row in moduleRows)
        {
            if (row?.RowButton == null) continue;
            string id = row.ModuleID;
            row.RowButton.onClick.AddListener(() => SelectModule(id));
        }

        if (beginButton != null) beginButton.onClick.AddListener(OnBeginPressed);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPausePressed);
        if (endButton   != null) endButton.onClick.AddListener(OnEndPressed);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Auto-wiring (called by JANUSMenuSetup)
    // ─────────────────────────────────────────────────────────────────────

    public void WireFromSetup(
        Text patientID, Text sessionCounter, Text elapsed,
        FloorPlanCard[] cards, ModuleRow[] modules,
        Button begin, Button pause, Button end, Text status)
    {
        patientIDText      = patientID;
        sessionCounterText = sessionCounter;
        elapsedText        = elapsed;
        floorPlanCards     = cards;
        moduleRows         = modules;
        beginButton        = begin;
        pauseButton        = pause;
        endButton          = end;
        statusText         = status;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Patient / Session
    // ─────────────────────────────────────────────────────────────────────

    public void LoadPatient(string patientID, int currentSession, int totalSessions)
    {
        _patientID      = patientID;
        _currentSession = currentSession;
        _totalSessions  = totalSessions;
        _state          = SessionState.Idle;
        RefreshPatientDisplay();
        RefreshSessionControls();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Floor Plan
    // ─────────────────────────────────────────────────────────────────────

    public void SelectFloor(int index)
    {
        if (index < 0 || index >= floorPlans.Length) return;
        _selectedFloor = index;
        RefreshFloorCards();

        if (floorPlans[index] != null)
            JANUSEvents.OnFloorPlanSelected?.Invoke(floorPlans[index]);
    }

    public FloorPlanData GetSelectedFloor() =>
        (_selectedFloor >= 0 && _selectedFloor < floorPlans.Length)
            ? floorPlans[_selectedFloor] : null;

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Module
    // ─────────────────────────────────────────────────────────────────────

    public void SelectModule(string moduleID)
    {
        _selectedModule = moduleID;
        RefreshModuleRows();
        JANUSEvents.OnModuleSelected?.Invoke(moduleID);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Visibility
    //
    // IMPORTANT: We hide by disabling Canvas + CanvasGroup, NOT by
    // calling gameObject.SetActive(false). This keeps all scripts
    // on this GameObject alive (especially JANUSVRInputHandler).
    // ─────────────────────────────────────────────────────────────────────

    public void SetVisible(bool visible)
    {
        _visible = visible;

        // Disable/enable the Canvas component to stop rendering
        _canvas.enabled = visible;

        // CanvasGroup controls interaction and visibility
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;

        if (visible) JANUSEvents.OnMenuOpened?.Invoke();
        else         JANUSEvents.OnMenuClosed?.Invoke();
    }

    public void ToggleVisible() => SetVisible(!_visible);

    /// <summary>
    /// Positions the menu in front of the player's current view.
    /// Call after the XR rig has initialised.
    /// </summary>
    public void PlaceInFrontOfPlayer(float distance = 1.0f)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var fwd = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
        transform.position = cam.transform.position + fwd * distance + Vector3.up * -0.05f;
        transform.rotation = Quaternion.LookRotation(fwd);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Session button handlers
    // ─────────────────────────────────────────────────────────────────────

    private void OnBeginPressed()
    {
        if (_state == SessionState.Idle && !string.IsNullOrEmpty(_selectedModule))
        {
            _state        = SessionState.Running;
            _sessionStart = Time.time;
            JANUSEvents.OnModuleBegin?.Invoke(_selectedModule);
            RefreshSessionControls();
        }
    }

    private void OnPausePressed()
    {
        if (_state == SessionState.Running)
        {
            _state = SessionState.Paused;
            JANUSEvents.OnSessionPaused?.Invoke();
        }
        else if (_state == SessionState.Paused)
        {
            _state = SessionState.Running;
            JANUSEvents.OnSessionResumed?.Invoke();
        }
        RefreshSessionControls();
    }

    private void OnEndPressed()
    {
        float duration = _state == SessionState.Running ? Time.time - _sessionStart : 0f;
        _state = SessionState.Ended;
        JANUSEvents.OnSessionEnded?.Invoke(_patientID, duration);
        RefreshSessionControls();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Hardware status callback
    // ─────────────────────────────────────────────────────────────────────

    private void OnHardwareStatusChanged(JANUSHardwareMonitor.HardwareStatus status)
    {
        if (statusText == null) return;

        statusText.text = _hw.GetStatusSummary();
        statusText.color = status switch
        {
            JANUSHardwareMonitor.HardwareStatus.Critical => colHwCritical,
            JANUSHardwareMonitor.HardwareStatus.Warn     => colHwWarn,
            _                                            => colHwOK,
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI refresh helpers
    // ─────────────────────────────────────────────────────────────────────

    private void RefreshPatientDisplay()
    {
        if (patientIDText      != null) patientIDText.text      = _patientID;
        if (sessionCounterText != null) sessionCounterText.text = $"Session {_currentSession} / {_totalSessions}";
    }

    private void RefreshFloorCards()
    {
        for (int i = 0; i < floorPlanCards.Length; i++)
        {
            var card = floorPlanCards[i];
            if (card == null) continue;

            bool selected = i == _selectedFloor;

            if (card.OutlineImage != null)
                card.OutlineImage.color = selected ? colSelected : colUnselected;

            if (card.CheckMark != null)
                card.CheckMark.SetActive(selected);

            if (i < floorPlans.Length && floorPlans[i] != null)
            {
                if (card.NameText != null) card.NameText.text = floorPlans[i].LayoutName;
                if (card.DescText != null) card.DescText.text = floorPlans[i].Description;
            }
        }
    }

    private void RefreshModuleRows()
    {
        foreach (var row in moduleRows)
        {
            if (row == null) continue;
            bool active = row.ModuleID == _selectedModule;
            if (row.Background != null)
                row.Background.color = active ? colSelected : Color.clear;
        }
    }

    private void RefreshSessionControls()
    {
        // Begin Module button — only active when Idle and a module is selected
        if (beginButton != null)
        {
            beginButton.interactable = _state == SessionState.Idle
                                       && !string.IsNullOrEmpty(_selectedModule);
        }

        // Pause button — toggles between Pause/Resume during a running session
        if (pauseButton != null)
        {
            var label = pauseButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = _state switch
                {
                    SessionState.Running => "Pause",
                    SessionState.Paused  => "Resume",
                    _                    => "Pause",
                };
            }
            pauseButton.interactable = _state == SessionState.Running
                                       || _state == SessionState.Paused;
        }

        // End Session button — active while running or paused
        if (endButton != null)
            endButton.interactable = _state == SessionState.Running
                                     || _state == SessionState.Paused;
    }
}
