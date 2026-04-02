using UnityEngine;

/// <summary>
/// JANUS — Floor Plan Data (ScriptableObject)
///
/// Stores metadata for a single floor plan layout card.
/// Create instances via Assets → Create → JANUS → Floor Plan Data.
///
/// The JANUSMenuManager reads LayoutName, Description, and RoomCount
/// to populate the three floor plan selection cards. Thumbnail is used
/// by JANUSMenuSetup when building the card UI.
/// </summary>
[CreateAssetMenu(fileName = "FloorPlan_New", menuName = "JANUS/Floor Plan Data")]
public class FloorPlanData : ScriptableObject
{
    [Header("Display")]
    [Tooltip("Name shown on the card (e.g. 'Layout A').")]
    public string LayoutName = "Layout";

    [Tooltip("Short descriptor (e.g. '3 rooms · Standard').")]
    public string Description = "";

    [Tooltip("Optional thumbnail sprite for the card preview.")]
    public Sprite Thumbnail;

    [Header("Layout Properties")]
    [Tooltip("Number of rooms in this layout.")]
    public int RoomCount = 3;

    [Tooltip("Difficulty tier: Standard, Extended, Complex.")]
    public string Complexity = "Standard";

    [Header("Scene Reference")]
    [Tooltip("Scene name or addressable key to load for this floor plan.")]
    public string SceneKey = "";
}
