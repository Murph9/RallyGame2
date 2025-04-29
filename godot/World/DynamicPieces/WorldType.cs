using System.Collections.Generic;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public enum WorldType {
    Simple,
    Simple2,
    Ditch
}

public static class WorldTypeExtensions {
    record struct WorldTypeDetails(string Description, float RoadWidth);
    private readonly static Dictionary<WorldType, WorldTypeDetails> DETAILS = new() {
        {WorldType.Simple, new("Basic Road", 6)},
        {WorldType.Simple2, new("Basic road with middle", 10)},
        {WorldType.Ditch, new("Road with ditches on either side", 4)},
    };

    public static string Description(this WorldType type) => DETAILS[type].Description;
    public static float RoadWidth(this WorldType type) => DETAILS[type].RoadWidth;
}
