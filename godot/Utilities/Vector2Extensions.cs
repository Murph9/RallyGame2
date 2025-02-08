using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public static class Vector2Extensions {
    public static Vector3 ToV3XZ(this Vector2 vec, float y = 0) => new(vec.X, y, vec.Y);
}
