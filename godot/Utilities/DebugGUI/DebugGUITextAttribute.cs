using Godot;
using System;

namespace murph9.RallyGame2.godot.Utilities.DebugGUI;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DebugGUITextAttribute : Attribute {
    public Color Color { get; private set; }

    public DebugGUITextAttribute(
        // Color
        float r = 1,
        float g = 1,
        float b = 1
    ) {
        Color = new Color(r, g, b, 0.9f);
    }
}
