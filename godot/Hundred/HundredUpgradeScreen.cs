using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUpgradeScreen : CenterContainer {

    // Note this event handler doesn't output the changed CarDetails object
    // This is because event handlers only support godot types
    [Signal]
    public delegate void ClosedEventHandler(bool carChanged);

    private ICollection<PartDetails> _currentPartOptions = [];

    private PartDetails _appliedPart;
    private PartLevel _targetLevel;
    private float _moneyPaid;
    private Button _buttonPressed;

    public override void _EnterTree() {
        // on enter tree so we can reset the buttons and current car details
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        LoadOptions(state);
        ReloadStats(state);
    }

    public override void _ExitTree() {
        _appliedPart = null;
        _moneyPaid = 0;
        _buttonPressed = null;

        var optionsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerOptions");
        foreach (var child in optionsBox.GetChildren()) {
            optionsBox.RemoveChild(child);
            child.QueueFree();
        }
    }

    public void SetParts(List<PartDetails> parts) {
        _currentPartOptions = [.. parts];
    }

    public (PartDetails, PartLevel, float) GetChangedDetails() => (_appliedPart, _targetLevel, _moneyPaid);

    private void LoadOptions(HundredGlobalState state) {
        var optionsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerOptions");

        foreach (var part in _currentPartOptions) {
            var container = new HBoxContainer();
            container.AddChild(new TextureRect() {
                Texture = part.IconImage,
                CustomMinimumSize = new Vector2(100, 100),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            });
            var currentLevel = state.CarDetails.LevelOfPart(part);
            var targetLevel = currentLevel + 1;

            container.AddChild(new Label() {
                Text = $"{part.Name} lvl {targetLevel} for ${part.LevelCost[(int)targetLevel]}"
            });

            var optionButton = new Button() {
                Text = "Choose",
                Disabled = part.LevelCost[(int)targetLevel] > state.Money
            };
            optionButton.Pressed += () => {
                if (_appliedPart == part)
                    return;

                _appliedPart = part;
                _targetLevel = targetLevel;
                _buttonPressed = optionButton;
                ReloadStats(state, part, currentLevel, targetLevel);
            };
            container.AddChild(optionButton);

            optionsBox.AddChild(container);
        }

        var saveButton = new Button() {
            Text = "Buy"
        };
        saveButton.Pressed += () => {
            if (_appliedPart != null) {
                _moneyPaid = (float)_appliedPart.LevelCost[(int)_targetLevel];
            }
            EmitSignal(SignalName.Closed, _appliedPart != null);
        };
        optionsBox.AddChild(saveButton);

        var chooseNothing = new Button() {
            Text = "Leave"
        };
        chooseNothing.Pressed += () => {
            EmitSignal(SignalName.Closed, false);
        };
        optionsBox.AddChild(chooseNothing);
    }

    private void ReloadStats(HundredGlobalState state, PartDetails previewPart = null, PartLevel fromLevel = PartLevel.Common, PartLevel toLevel = PartLevel.Common) {
        var statsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerStats");

        // remove any existing things because this is a dumb view for now
        foreach (var n in statsBox.GetChildren().ToArray()) {
            statsBox.RemoveChild(n);
            n.QueueFree();
        }

        var stats = new RichTextLabel() {
            LayoutMode = 2,
            BbcodeEnabled = true,
            SizeFlagsHorizontal = SizeFlags.Fill,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.Off
        };
        statsBox.AddChild(stats);

        stats.PushColor(Colors.White);

        if (previewPart != null) {
            // Use CalcDeltaForPart to show exactly what this upgrade changes — no clone needed
            var deltas = state.CarDetails.CalcDeltaForPart(previewPart, fromLevel, toLevel).ToList();

            if (deltas.Count == 0) {
                stats.AppendText("No stat changes at this level.");
            } else {
                stats.PushTable(3);

                stats.PushCell(); stats.AppendText("Stat"); stats.Pop();
                stats.PushCell(); stats.AppendText("Current"); stats.Pop();
                stats.PushCell(); stats.AppendText("New"); stats.Pop();

                foreach (var delta in deltas) {
                    stats.PushCell();
                    stats.AppendText(delta.FieldName);
                    stats.Pop();

                    stats.PushCell();
                    stats.PushColor(Colors.LightBlue);
                    stats.AppendText(delta.FromValue != null ? GodotClassHelper.ToStringWithRounding(delta.FromValue, 2) : "-");
                    stats.Pop();
                    stats.Pop();

                    stats.PushCell();
                    var isImprovement = delta.HigherIs == HigherIs.Good
                        ? (dynamic)delta.ToValue > (dynamic)delta.FromValue
                        : delta.HigherIs == HigherIs.Bad
                            ? (dynamic)delta.ToValue < (dynamic)delta.FromValue
                            : true;
                    stats.PushColor(isImprovement ? Colors.Green : Colors.Orange);
                    stats.AppendText(delta.ToValue != null ? GodotClassHelper.ToStringWithRounding(delta.ToValue, 2) : "-");
                    stats.Pop();
                    stats.Pop();
                }

                stats.Pop(); // table
            }
        } else {
            stats.AppendText("Select a part to preview changes.");
        }

        stats.Pop(); // white

        // Torque graph always shows current car (no clone needed when no part selected)
        var torqueCurveGraph = new TorqueCurveGraph(state.CarDetails, null, null, null);
        statsBox.AddChild(torqueCurveGraph);
    }

    private void ReloadStats(HundredGlobalState state) => ReloadStats(state, null);
}
