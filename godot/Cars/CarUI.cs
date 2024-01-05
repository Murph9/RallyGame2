using Godot;
using System;

namespace murph9.RallyGame2.godot.Cars;

public partial class CarUI : Control {

    public Car Car { get; set; }

    public override void _Ready() {
        for (int i = 0; i < Car.Wheels.Length; i++) {
            var node = GetNode<WheelUI>("WheelGridContainer/WheelUi" + i);
            node.Wheel = Car.Wheels[i];
        }
    }

    public override void _Draw() {
        var screenSize = GetViewportRect().Size;

        var arcSize = 100;
        DrawArc(screenSize - new Vector2(arcSize + 10, arcSize + 10), arcSize, (float)Math.PI, (float)((1 + Car.Engine.CurRPM/10000f)*Math.PI), 32, Colors.White, 10, true);

        var speed = Car.RigidBody.LinearVelocity.Length();
        speed *= 3.6f;
        var defaultFont = ThemeDB.FallbackFont;
        int defaultFontSize = ThemeDB.FallbackFontSize;
        var rpmStr = Car.Engine.CurRPM.ToString();
        DrawString(defaultFont, screenSize - new Vector2(defaultFontSize * rpmStr.Length, 30), rpmStr, HorizontalAlignment.Right, -1, defaultFontSize);

        // show gear
        DrawString(defaultFont, screenSize - new Vector2(100, 10), Car.Engine.CurGear.ToString(), HorizontalAlignment.Right, -1, defaultFontSize * 2);

        // show speed
        var speedStr = float.Round(speed, 0).ToString();
        DrawString(defaultFont, screenSize - new Vector2(defaultFontSize * speedStr.Length, 10), speedStr, HorizontalAlignment.Right, -1, defaultFontSize * 2);
    }

    public override void _Process(double delta) {
        if (Car == null) return;

        QueueRedraw(); // TODO please don't call this every frame if its not needed

        var wheels = GetNode<GridContainer>("WheelGridContainer");
        wheels.Position = new Vector2(GetViewportRect().End.X - wheels.Size.X, 0);

        GetNode<ProgressBar>("VBoxContainer/ProgressBarSteering").Value = Car.SteeringLeft - Car.SteeringRight;
        GetNode<ProgressBar>("VBoxContainer/HBoxContainer/ProgressBarAccel").Value = Car.AccelCur;
        GetNode<ProgressBar>("VBoxContainer/HBoxContainer/ProgressBarBrake").Value = Car.BrakingCur;

        GetNode<RichTextLabel>("RichTextLabel").Text =
$@"{Car}
Position: {V3toStr(Car.RigidBody.Position)}
Velocity: {V3toStr(Car.RigidBody.LinearVelocity)}
DriftAngle: {float.Round(Car.DriftAngle, 2)}
Drag: { V3toStr(Car.DragForce)}
";
    }

    private static string V3toStr(Vector3 v) {
        return $"({float.Round(v.X, 2)}, {float.Round(v.Y, 2)}, {float.Round(v.Z, 2)})";
    }
}
