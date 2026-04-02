using System;

/// <summary>
/// JANUS — Global Event Bus
///
/// Static delegates that any script can invoke or subscribe to.
/// Keeps coupling between JANUS subsystems loose — the menu, input handler,
/// hardware monitor, and assessment modules all communicate through here.
///
/// USAGE:
///   Subscribe:   JANUSEvents.OnFloorPlanSelected += MyHandler;
///   Unsubscribe: JANUSEvents.OnFloorPlanSelected -= MyHandler;
///   Fire:        JANUSEvents.OnFloorPlanSelected?.Invoke(data);
/// </summary>
public static class JANUSEvents
{
    // ── Menu Visibility ────────────────────────────────────────────────
    public static Action OnMenuOpened;
    public static Action OnMenuClosed;

    // ── Floor Plan Selection ───────────────────────────────────────────
    public static Action<FloorPlanData> OnFloorPlanSelected;

    // ── Module Selection & Lifecycle ───────────────────────────────────
    public static Action<string> OnModuleSelected;
    public static Action<string> OnModuleBegin;

    // ── Session Lifecycle ──────────────────────────────────────────────
    public static Action OnSessionPaused;
    public static Action OnSessionResumed;

    /// <summary>Fired when the clinician ends the session. Carries patient ID and elapsed seconds.</summary>
    public static Action<string, float> OnSessionEnded;

    // ── Hardware ───────────────────────────────────────────────────────
    /// <summary>Carries a human-readable warning string (e.g. "Left controller battery critical").</summary>
    public static Action<string> OnHardwareWarning;

    // ── Gaze Dwell ─────────────────────────────────────────────────────
    /// <summary>Progress 0-1 while the user dwells on a UI element via head gaze.</summary>
    public static Action<float> OnGazeDwellProgress;
}
