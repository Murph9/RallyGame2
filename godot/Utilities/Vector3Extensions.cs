using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public static class Vector3Extensions {
    public static Vector2 ToV2XZ(this Vector3 vec) => new(vec.X, vec.Z);
}
