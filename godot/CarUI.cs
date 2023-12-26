using Godot;

namespace murph9.RallyGame2.godot;

public partial class CarUI : Node2D {

    public Car Car { get; set; }

    public override void _Process(double delta) {
        if (Car == null) return;

        GetNode<Label>("VBoxContainer/NameLabel").Text = Car.ToString() + " @ " + Car.RigidBody.Position;

        for (var i = 0; i < Car.Wheels.Length; i++) {
            var w = Car.Wheels[i];
            GetNode<Label>("VBoxContainer/GridContainer/wheelVBC"+i+"/Label").Text = w.Details.id + " " + w.RadSec;
            GetNode<ProgressBar>("VBoxContainer/GridContainer/wheelVBC"+i+"/ProgressBar").Value = w.SusTravelFraction;
            GetNode<Line2D>("VBoxContainer/GridContainer/wheelVBC"+i+"/Line2D").Points = new Vector2[] {
                new (80, 40),
                40* new Vector2(w.GripDir.X, w.GripDir.Z) / Car.Details.mass + new Vector2(80, 40),
            };
        }
    }
}
