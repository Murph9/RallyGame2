using System;
using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public class GodotClassHelper {

    // TODO remove all uses of .tscn and manual paths

    // a little hack to get the root namespace, apprently this isn't possible
    private static string OFFSET_TO_FIND_ROOT => typeof(GodotClassHelper).FullName.TrimSuffix("Utilities.GodotClassHelper");

    public static string GetScenePath(Type type, string suffix = ".tscn") {
        var typeProjPath = type.FullName.TrimPrefix(OFFSET_TO_FIND_ROOT);
        return $"res://{typeProjPath.Trim('.').Replace('.', '/')}{suffix}";
    }
}