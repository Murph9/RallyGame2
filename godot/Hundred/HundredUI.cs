using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUI : HBoxContainer {

    private readonly Dictionary<string, Tuple<Part, Control>> partMappings = [];

    public override void _Ready() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        var allParts = state.CarDetails.GetAllPartsInTree();

        var partContainer = GetNode<VBoxContainer>("VBoxContainerLeft/VBoxContainerParts");

        foreach (var part in allParts) {
            var hbox = new HBoxContainer();
            hbox.AddChild(new Label() {
                Text = part.Name
            });
            hbox.AddChild(new Label() {
                Text = part.CurrentLevel.ToString()
            });
            hbox.AddChild(new TextureRect() {
                Texture = part.IconImage,
                CustomMinimumSize = new Vector2(25, 25),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            });

            partMappings.Add(part.Name, new(part, hbox));
            partContainer.AddChild(hbox);
        }
    }

    public override void _Process(double delta) {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        var timeLabel = GetNode<Label>("VBoxContainerLeft/VBoxContainerState/HBoxContainerTime/Label");
        timeLabel.Text = GenerateTimeString(state.TotalTimePassed);

        var progressBar = GetNode<ProgressBar>("VBoxContainerLeft/VBoxContainerState/HBoxContainerDistance/ProgressBar");
        var value = state.DistanceTravelled / state.TargetDistance * 100;
        progressBar.Value = value;

        var progressLabel = GetNode<Label>("VBoxContainerLeft/VBoxContainerState/HBoxContainerDistance/Label");
        progressLabel.Text = Math.Round(value, 2) + " km";

        var rivalInfo = GetNode<Label>("VBoxContainerLeft/VBoxContainerState/VBoxContainerRival/Label");
        rivalInfo.Text = state.RivalRaceMessage;

        var moneyInfo = GetNode<Label>("VBoxContainerLeft/VBoxContainerState/VBoxContainerMoney/Label");
        moneyInfo.Text = "$" + state.Money;

        // show the parts and their current levels
        var allParts = state.CarDetails.GetAllPartsInTree();
        foreach (var part in allParts) {
            var uiPart = partMappings[part.Name];
            uiPart.Item2.GetChild<Label>(1).Text = part.CurrentLevel.ToString();
        }

        var goalInfo = GetNode<Label>("VBoxContainerCenter/LabelGoal");
        if (!state.Goal.InProgress) {
            goalInfo.Text = $"Goal {state.Goal.Type} starts at {state.Goal.StartDistance}";
            if (state.Goal.RealStartingDistance > state.Goal.StartDistance) // its set show the distance left
                goalInfo.Text += $" in {state.Goal.RealStartingDistance - state.DistanceTravelled}m";
        } else {
            goalInfo.Text = " some math with FinishedMessage";
        }
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
