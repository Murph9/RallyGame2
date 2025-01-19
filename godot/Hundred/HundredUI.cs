using System;
using Godot;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUI : VBoxContainer {

    public float TargetDistance { get; set; } = 100 * 1000;
    public float MinimumSpeed { get; set; } = 50;
    public float DistanceTravelled { get; set; }
    public float SpeedKMH { get; set; }

    public double MinimumSpeedProgress { get; private set; }

    public override void _Ready() {}

    public override void _Process(double delta) {
        var progressBar = GetNode<ProgressBar>("CenterContainer/VBoxContainer/HBoxContainerDistance/ProgressBar");
        var value = DistanceTravelled / TargetDistance * 100;
        progressBar.Value = value;
        
        var progressLabel = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainerDistance/Label");
        progressLabel.Text = Math.Round(value, 2) + " km";

        if (SpeedKMH < MinimumSpeed) {
            MinimumSpeedProgress += delta;
        } else {
            MinimumSpeedProgress -= delta;
        }
        if (MinimumSpeedProgress < 0) MinimumSpeedProgress = 0;

        var progressLabel2 = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainerSpeed/Label");
        progressLabel2.Text = "Target: " + MinimumSpeed + " km/h";

        var progressBar2 = GetNode<ProgressBar>("CenterContainer/VBoxContainer/HBoxContainerSpeed/ProgressBar");
        progressBar2.Value = MinimumSpeedProgress;
    }
}
