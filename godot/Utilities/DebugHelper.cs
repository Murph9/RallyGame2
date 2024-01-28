using Godot;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities;

public class DebugHelper {

    public static WorldText GenerateWorldText(string text, Vector3 position) {
        var uiScene = GD.Load<PackedScene>("res://Utilities/WorldText.tscn");
        var instance = uiScene.Instantiate<WorldText>();
        instance.Position = position;
        instance.SetText(text);
        return instance;
    }

    public static bool IsNumeric(object o){
        var numType = typeof(System.Numerics.INumber<>);
        return o.GetType().GetInterfaces().Any(iface =>
            iface.IsGenericType && (iface.GetGenericTypeDefinition() == numType));
    }
}
