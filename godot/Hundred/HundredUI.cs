using Godot;
using murph9.RallyGame2.godot.scenes;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUI : VBoxContainer {

    public override void _Process(double delta) {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        var timeLabel = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainerTime/Label");
        timeLabel.Text = GenerateTimeString(state.TotalTimePassed);

        var progressBar = GetNode<ProgressBar>("CenterContainer/VBoxContainer/HBoxContainerDistance/ProgressBar");
        var value = state.DistanceTravelled / state.TargetDistance * 100;
        progressBar.Value = value;

        var progressLabel = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainerDistance/Label");
        progressLabel.Text = Math.Round(value, 2) + " km";

        var progressLabel2 = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainerSpeed/Label");
        progressLabel2.Text = "Target: " + state.MinimumSpeedKMH + " km/h";

        var progressBar2 = GetNode<ProgressBar>("CenterContainer/VBoxContainer/HBoxContainerSpeed/ProgressBar");
        progressBar2.Value = state.MinimumSpeedProgress;

        var rivalInfo = GetNode<Label>("CenterContainer/VBoxContainer/VBoxContainerRival/Label");
        rivalInfo.Text = state.RivalRaceMessage;

        var moneyInfo = GetNode<Label>("CenterContainer/VBoxContainer/VBoxContainerMoney/Label");
        moneyInfo.Text = "$" + state.Money;
    }

    private static string GenerateTimeString(double time) {
        var hours = (int)time / 60 / 60;
        var mins = (int)time / 60 % 60;
        string output = null;
        if (hours > 0)
            output += hours + " hrs ";
        if (mins > 0)
            output += mins + " min ";
        return output + ((int)time % 60) + " sec";
    }
}
