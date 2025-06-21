using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Hundred.Relics;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUI : HBoxContainer {

    private readonly Dictionary<string, Tuple<Part, Control>> _partMappings = [];
    private readonly Dictionary<Car, HundredInProgressItem> _rivalDetails = [];
    private readonly Dictionary<HundredInProgressItem, Label> _uiLabelMap = [];
    private readonly Dictionary<RelicType, Container> _relicMappings = [];
    private HundredInProgressItem _goalInProgress;
    private HundredInProgressUi _inProgressUi;

    public override void _Ready() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        state.GoalChanged += GoalChanged;
        state.RivalRaceStarted += RivalStarted;
        state.RivalRaceStopped += RivalStopped;

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

            _partMappings.Add(part.Name, new(part, hbox));
            partContainer.AddChild(hbox);
        }

        var relicContainer = GetNode<VBoxContainer>("VBoxContainerLeft/VBoxContainerRelics");
        foreach (var relic in state.RelicManager.GetAllPossibleRelics()) {
            var hbox = new HBoxContainer();
            hbox.AddChild(new ColorRect() {
                Color = Colors.Aqua,
                CustomMinimumSize = new Vector2(45, 45),
            });
            hbox.AddChild(new Label() {
                Text = relic.GetType().Name
            });
            hbox.AddChild(new Label() {
                Text = ""
            });
            hbox.AddChild(new Label() {
                Text = ""
            });
            hbox.Visible = false;
            _relicMappings.Add(relic, hbox);
            relicContainer.AddChild(hbox);
        }

        _inProgressUi = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredInProgressUi))).Instantiate<HundredInProgressUi>();
        GetNode<VBoxContainer>("VBoxContainerRight").AddChild(_inProgressUi);

        // and initialize the UI with the goal
        GoalChanged();
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

        var moneyInfo = GetNode<Label>("VBoxContainerLeft/VBoxContainerState/VBoxContainerMoney/Label");
        moneyInfo.Text = "$" + state.Money;

        var shopTimer = GetNode<Label>("VBoxContainerLeft/VBoxContainerState/HBoxContainerShopTimer/TimerLabel");
        shopTimer.Text = Math.Round(state.ShopResetTimer, 1) + " sec till shop reset";

        // show the parts and their current levels
        var allParts = state.CarDetails.GetAllPartsInTree();
        foreach (var part in allParts) {
            var uiPart = _partMappings[part.Name];
            uiPart.Item2.GetChild<Label>(1).Text = part.CurrentLevel.ToString();
        }

        // update relic view
        foreach (var relic in state.RelicManager.GetRelics()) {
            var hbox = _relicMappings[relic.RelicType];
            hbox.Visible = true;
            var children = hbox.GetChildren();

            var color = children[0] as ColorRect;
            color.Color = Colors.Aqua;
            color.CustomMinimumSize = new Vector2(45, 45);

            var nameLabel = children[1] as Label;
            nameLabel.Text = relic.GetType().Name;

            var delayLabel = children[2] as Label;
            delayLabel.Text = relic.Delay > 0 ? Math.Round(relic.Delay, 1).ToString() : "";

            var outputLabel = children[2] as Label;
            outputLabel.Text = relic.OutputStrength != 1 ? Math.Round(relic.OutputStrength, 1) + "" : "";
        }

        if (_goalInProgress != null) {
            _uiLabelMap[_goalInProgress].Text = state.Goal.ProgressString(state.TotalTimePassed, state.DistanceTravelled, state.CurrentPlayerSpeed);
        }

        foreach (var rivalRaceUi in _rivalDetails) {
            // TODO are you winning?
            _uiLabelMap[rivalRaceUi.Value].Text = "RivalRace: " + rivalRaceUi.Key.Name;
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

    private void GoalChanged() {
        if (_goalInProgress is not null) {
            _uiLabelMap.Remove(_goalInProgress);
            _inProgressUi.Remove(_goalInProgress);
        }
        _goalInProgress = _inProgressUi.Add();

        _uiLabelMap[_goalInProgress] = new Label();
        _goalInProgress.AddChild(_uiLabelMap[_goalInProgress]);
    }

    private void RivalStarted(Car rival) {
        var uiElement = _inProgressUi.Add();
        _rivalDetails.Add(rival, uiElement);

        _uiLabelMap[uiElement] = new Label();
        uiElement.AddChild(_uiLabelMap[uiElement]);
    }

    private void RivalStopped(Car rival) {
        var uiElement = _rivalDetails[rival];
        _inProgressUi.Remove(uiElement);
        _rivalDetails.Remove(rival);

        _uiLabelMap.Remove(uiElement);
    }
}
