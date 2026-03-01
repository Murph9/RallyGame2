using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component.Rarity;

namespace murph9.RallyGame2.godot.PayDay.Parts;

/// <summary>
/// A part that has been dropped as loot (with a rarity tier).
/// Extends RefCounted so it can be passed through Godot signals as a Variant.
/// </summary>
public partial class CollectedPart(Part part, PartRarity rarity) : RefCounted {
    public Part Part { get; set; } = part;
    public PartRarity Rarity { get; set; } = rarity;
}
