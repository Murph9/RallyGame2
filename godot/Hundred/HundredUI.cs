using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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

        var goalInfo = GetNode<Label>("%GoalLabel");
        goalInfo.Text = state.Goal.ProgressString(state.TotalTimePassed, state.DistanceTravelled);

        var shopInfo = GetNode<Label>("%ShopTimerLabel");
        if (state.ShopCooldownTimer > 0) {
            shopInfo.Text = "Shop cooldown";
        } else if (state.ShopStoppedTimer > 0) {
            shopInfo.Text = "Shop opening soon";
        } else {
            shopInfo.Text = "";
        }

        // update relic view
        var relicContainer = GetNode<VBoxContainer>("VBoxContainerLeft/VBoxContainerRelics");
        // TODO perf
        foreach (var relicView in relicContainer.GetAllChildrenOfType<HBoxContainer>().ToArray()) {
            relicContainer.RemoveChild(relicView);
        }

        foreach (var relic in state.RelicManager.GetRelics()) {
            var hbox = new HBoxContainer();

            hbox.AddChild(new Label() {
                Text = relic.GetType().Name
            });
            hbox.AddChild(new ColorRect() {
                Color = Colors.Aqua,
                CustomMinimumSize = new Vector2(50, 50),
            });
            hbox.AddChild(new Label() {
                Text = relic.Delay > 0 ? Math.Round(relic.Delay, 1).ToString() : ""
            });

            relicContainer.AddChild(hbox);
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
