using Godot;

namespace murph9.RallyGame2.godot.Cars;

public partial class CarUI : Node2D {

    public Car Car { get; set; }

    public override void _Process(double delta) {
        if (Car == null) return;

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

    private string V3toStr(Vector3 v) {
        return $"({float.Round(v.X, 2)}, {float.Round(v.Y, 2)}, {float.Round(v.Z, 2)})";
    }
}
