using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public class ScreenHelper {

    /// <summary>Clamps a screen-space position to within margin of the screen edges.</summary>
    public static Vector2 ClampToScreenEdge(Vector2 screenPos, Vector2 screenSize, float margin) {
        var center = screenSize / 2f;
        var dir = screenPos - center;
        float halfW = screenSize.X / 2f - margin;
        float halfH = screenSize.Y / 2f - margin;

        float scaleX = dir.X != 0f ? halfW / Mathf.Abs(dir.X) : float.MaxValue;
        float scaleY = dir.Y != 0f ? halfH / Mathf.Abs(dir.Y) : float.MaxValue;
        float scale = Mathf.Min(scaleX, scaleY);

        if (scale < 1f)
            return center + dir * scale;
        // Point is already inside – clamp it to edge
        return center + dir.Normalized() * Mathf.Min(halfW, halfH);
    }
}