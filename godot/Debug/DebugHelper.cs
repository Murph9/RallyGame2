using Godot;

namespace murph9.RallyGame2.godot.Debug;

public class DebugHelper {

    public static Node GenerateWorldText(string text, Vector3 position) {
        var uiScene = GD.Load<PackedScene>("res://Debug/WorldText.tscn");
        var instance = uiScene.Instantiate<WorldText>();
        instance.Position = position;
        instance.SetText(text);
        return instance;
    }
}
