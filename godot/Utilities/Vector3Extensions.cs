using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public static class Vector3Extensions {
    public static Vector2 ToV2XZ(this Vector3 vec) => new(vec.X, vec.Z);

    public static string ToRoundedString(this Vector3 vec, int places = 2) {
        var sigFig = Mathf.Pow(10, places);
        vec *= sigFig;
        return $"({Mathf.Round(vec.X) / sigFig},{Mathf.Round(vec.Y) / sigFig},{Mathf.Round(vec.Z) / sigFig})";
    }
}
