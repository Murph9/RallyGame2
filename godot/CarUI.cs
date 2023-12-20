using Godot;

namespace murph9.RallyGame2.godot;

public partial class CarUI : Node2D {

    public Car Car { get; set; }

    public override void _Process(double delta) {
        if (Car == null) return;

        GetNode<Label>("VBoxContainer/NameLabel").Text = Car.ToString();

        for (var i = 0; i < Car.Wheels.Length; i++) {
            GetNode<Label>("VBoxContainer/GridContainer/wheelVBC"+i+"/Label").Text = Car.Wheels[i].Name;
            GetNode<ProgressBar>("VBoxContainer/GridContainer/wheelVBC"+i+"/ProgressBar").Value = Car.Wheels[i].SusTravelFraction;
            GetNode<CheckBox>("VBoxContainer/GridContainer/wheelVBC"+i+"/CheckBox").ButtonPressed = Car.Wheels[i].InContact;
        }
    }
}
