using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUpgradeScreen : CenterContainer {

    // Note this event handler doesn't output the changed CarDetails object
    // This is because event handlers only support godot types
    [Signal]
    public delegate void ClosedEventHandler(bool carChanged);

    private ICollection<Part> _currentPartOptions = [];

    private Part _appliedPart;
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

    public void SetParts(List<Part> parts) {
        _currentPartOptions = [.. parts];
    }

    public (Part, float) GetChangedDetails() => (_appliedPart, _moneyPaid);

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
            var alreadyBought = part.CurrentLevel + 1 == state.CarDetails.LevelOfPart(part);

            if (alreadyBought) {
                container.AddChild(new Label() {
                    Text = $"{part.Name} lvl {part.CurrentLevel + 1} bought"
                });
            } else {
                container.AddChild(new Label() {
                    Text = $"{part.Name} lvl {part.CurrentLevel + 1} for ${part.LevelCost[part.CurrentLevel + 1]}"
                });
            }
            var optionButton = new Button() {
                Text = "Choose",
                Disabled = alreadyBought || part.LevelCost[part.CurrentLevel + 1] > state.Money
            };
            optionButton.Pressed += () => {
                if (_appliedPart == part)
                    return;

                // clone it so we don't modify the original
                var currentClone = state.CarDetails.Clone();
                currentClone.ApplyPartChange(part, part.CurrentLevel + 1);

                _appliedPart = part;
                _buttonPressed = optionButton;
                ReloadStats(state, currentClone);
            };
            container.AddChild(optionButton);

            optionsBox.AddChild(container);
        }

        var saveButton = new Button() {
            Text = "Buy"
        };
        saveButton.Pressed += () => {
            if (_appliedPart != null) {
                _moneyPaid = (float)_appliedPart.LevelCost[_appliedPart.CurrentLevel + 1];
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

    private void ReloadStats(HundredGlobalState state, CarDetails currentClone = null) {
        var statsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerStats");

        // if no changes yet, just duplicate it
        currentClone ??= state.CarDetails;

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
        stats.PushTable(3);
        var prevDetails = state.CarDetails.GetPartResultsInTree();
        var details = currentClone.GetPartResultsInTree();

        stats.PushCell();
        stats.Pop();
        stats.PushCell();
        stats.AppendText("Current  ");
        stats.Pop();
        stats.PushCell();
        stats.AppendText("New  ");
        stats.Pop();

        var maxTorquePrev = state.CarDetails.Engine.MaxTorque();
        var maxTorque = currentClone.Engine.MaxTorque();
        stats.PushCell();
        stats.AppendText($"Max Torque (Nm):");
        stats.Pop();
        stats.PushCell();
        stats.AppendText($"{double.Round(maxTorquePrev.Item1, 2)} @ {maxTorquePrev.Item2} rpm");
        stats.Pop();
        stats.PushCell();
        if (maxTorque != maxTorquePrev) {
            stats.AppendText($"{double.Round(maxTorque.Item1, 2)} @ {maxTorque.Item2} rpm");
        }
        stats.Pop();

        var maxKwPrev = state.CarDetails.Engine.MaxKw();
        var maxKw = currentClone.Engine.MaxKw();
        stats.PushCell();
        stats.AppendText("Max Power (kW):");
        stats.Pop();
        stats.PushCell();
        stats.AppendText($"{double.Round(maxKwPrev.Item1, 2)} @ {maxKwPrev.Item2} rpm\n");
        stats.Pop();
        stats.PushCell();
        if (maxKw != maxKwPrev) {
            stats.AppendText($"{double.Round(maxKw.Item1, 2)} @ {maxKw.Item2} rpm");
        }
        stats.Pop();

        foreach (var entry in details) {
            // if the values are different show them:
            if (!DynamicsEqual(entry.Value, prevDetails.First(x => x.Name == entry.Name).Value)) {
                // https://stackoverflow.com/a/8855857/9353639

                // TODO support array diff detection

                stats.PushCell();
                stats.AppendText(entry.Name);
                stats.Pop();

                stats.PushCell();
                stats.PushColor(Colors.LightBlue);
                stats.AppendText(GodotClassHelper.ToStringWithRounding(prevDetails.First(x => x.Name == entry.Name).Value, 2));
                stats.Pop();
                stats.Pop();
                stats.PushCell();
                stats.PushColor(Colors.Green);
                stats.AppendText(GodotClassHelper.ToStringWithRounding(entry.Value, 2));
                stats.Pop();
                stats.Pop();

                /*stats.PushCell();
                stats.PushColor(Colors.Gray);
                stats.AppendText(string.Join(", ", entry.BecauseOf.Select(x => $"[color={x.Color}]{x.Name}[/color]")));
                stats.Pop();
                stats.Pop();*/
            }
        }

        stats.Pop();
        stats.Pop();

        var torqueCurveGraph = new TorqueCurveGraph(currentClone, null, state.CarDetails, null);
        statsBox.AddChild(torqueCurveGraph);
    }

    private static bool DynamicsEqual(object obj1, object obj2) {
        if (obj1 is bool b1 && obj2 is bool b2) {
            return b1 == b2;
        } else if (obj1 is int i1 && obj2 is int i2) {
            return i1 == i2;
        } else if (obj1 is float f1 && obj2 is float f2) {
            return f1 == f2;
        } else if (obj1 is double d1 && obj2 is double d2) {
            return d1 == d2;
        } else if (obj1 is string s1 && obj2 is string s2) {
            return s1 == s2;
        } else if (obj1 is float[] fa1 && obj2 is float[] fa2) {
            if (fa1.Length != fa2.Length) return false;
            for (int i = 0; i < fa1.Length; i++) {
                if (fa1[i] != fa2[i]) return false;
            }
            return true;
        }

        GD.PushError("What type is " + obj1.GetType());
        if ((dynamic)obj1 == (dynamic)obj2)
            return true;
        return false;
    }
}
