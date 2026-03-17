using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;

namespace murph9.RallyGame2.godot.PayDay.Parts;

/// <summary>
/// A part that has been dropped as loot (with a rarity tier).
/// Extends RefCounted so it can be passed through Godot signals as a Variant.
/// </summary>
public partial class CollectedPart(PartDetails part, PartLevel rarity) : RefCounted {
    public PartDetails Part { get; init; } = part;
    public PartLevel Rarity { get; init; } = rarity;
}
