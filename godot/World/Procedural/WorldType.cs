using System.Collections.Generic;

namespace murph9.RallyGame2.godot.World.Procedural;

public enum WorldType {
    Simple,
    Simple2,
    Ditch
}

public static class WorldTypeExtensions {
    record struct WorldTypeDetails(string Description);
    private readonly static Dictionary<WorldType, WorldTypeDetails> DETAILS = new() {
        {WorldType.Simple, new("Basic Road")},
        {WorldType.Simple2, new("Basic road with middle")},
        {WorldType.Ditch, new("Road with ditches on either side")},
    };

    public static string Description(this WorldType type) => DETAILS[type].Description;
}
