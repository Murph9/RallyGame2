using Godot;
using System;

namespace murph9.RallyGame2.godot.Utilities.DebugGUI;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DebugGUIGraphAttribute : Attribute {
    public float Min { get; private set; }
    public float Max { get; private set; }
    public Color Color { get; private set; }
    public int Group { get; private set; }
    public bool AutoScale { get; private set; }

    public DebugGUIGraphAttribute(
        // Line color
        float r = 1,
        float g = 1,
        float b = 1,
        // Values at top/bottom of graph
        float min = 0,
        float max = 1,
        // Offset position on screen
        int group = 0,
        // Auto-adjust min/max to fit the values
        bool autoScale = true
    )
    {
        Color = new Color(r, g, b, 0.9f);
        Min = min;
        Max = max;
        Group = group;
        AutoScale = autoScale;
    }
}
