using Godot;
using System;

namespace murph9.RallyGame2.godot.Cars;

public partial class CarUI : Node2D {

    public Car Car { get; set; }

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

        for (var i = 0; i < Car.Wheels.Length; i++) {
            var w = Car.Wheels[i];
            GetNode<Label>("VBoxContainer/GridContainer/wheelVBC"+i+"/Label").Text = w.Details.id + " " + float.Round(w.RadSec, 2);
            GetNode<ProgressBar>("VBoxContainer/GridContainer/wheelVBC"+i+"/ProgressBar").Value = w.SusTravelDistance / Car.Details.SusByWheelNum(w.Details.id).TravelTotal();
            GetNode<Line2D>("VBoxContainer/GridContainer/wheelVBC"+i+"/Line2D").Points = new Vector2[] {
                new (80, 40),
                40* new Vector2(w.GripDir.X, w.GripDir.Z) / Car.Details.mass + new Vector2(80, 40),
            };

            GetNode<Label>("VBoxContainer/GridContainer/wheelVBC"+i+"/HBoxContainer/LabelSlip").Text = float.Round(w.SlipAngle, 2).ToString();
            GetNode<Label>("VBoxContainer/GridContainer/wheelVBC"+i+"/HBoxContainer/LabelRatio").Text = float.Round(w.SlipRatio, 2).ToString();
        }

        GetNode<ProgressBar>("VBoxContainer2/ProgressBarSteering").Value = Car.SteeringLeft - Car.SteeringRight;
        GetNode<ProgressBar>("VBoxContainer2/HBoxContainer/ProgressBarAccel").Value = Car.AccelCur;
        GetNode<ProgressBar>("VBoxContainer2/HBoxContainer/ProgressBarBrake").Value = Car.BrakingCur;

        GetNode<RichTextLabel>("RichTextLabel").Text =
$@"{Car}
Position: {V3toStr(Car.RigidBody.Position)}
Velocity: {V3toStr(Car.RigidBody.LinearVelocity)}
DriftAngle: {float.Round(Car.DriftAngle, 2)}
";
    }

    private static string V3toStr(Vector3 v) {
        return $"({float.Round(v.X, 2)}, {float.Round(v.Y, 2)}, {float.Round(v.Z, 2)})";
    }
}
