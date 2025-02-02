using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUpgradeScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    private CarDetails _oldCarDetails;
    private Part _appliedPart;

    public override void _Ready() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        LoadOptions(state);
        LoadStats(state);
    }

    private void LoadOptions(HundredGlobalState state) {
        var optionsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainerOptions");

        // keep the existing cardetails incase it doesn't change
        _oldCarDetails = state.CarDetails;

        // clone it so we don't modify the original
        state.CarDetails = state.CarDetails.Clone();
        var allParts = state.CarDetails.GetAllPartsInTree().Where(x => x.CurrentLevel < x.Levels.Length - 1).ToList();

        for (int i = 0; i < state.ShopPartCount; i++) {
            if (allParts.Count <= 0) {
                break;
            }

            var part = allParts[Mathf.Abs((int)(GD.Randi() % allParts.Count))];
            allParts.Remove(part);
            var optionButton = new Button() {
                Text = $"{part.Name} lvl {part.CurrentLevel + 1} for ${part.LevelCost[part.CurrentLevel + 1]}"
            };
            optionButton.Pressed += () => {
                if (_appliedPart == part)
                    return;
                if (_appliedPart != null) {
                    _appliedPart.CurrentLevel--;
                }

                part.CurrentLevel++;
                state.CarDetails.LoadSelf(Main.DEFAULT_GRAVITY);
                _appliedPart = part;
                LoadStats(state);
            };
            optionsBox.AddChild(optionButton);
        }

        var saveButton = new Button() {
            Text = "Buy"
        };
        saveButton.Pressed += () => {
            EmitSignal(SignalName.Closed);
        };
        optionsBox.AddChild(saveButton);

        var chooseNothing = new Button() {
            Text = "Leave"
        };
        chooseNothing.Pressed += () => {
            state.CarDetails = _oldCarDetails;
            EmitSignal(SignalName.Closed);
        };
        optionsBox.AddChild(chooseNothing);
    }

    private void LoadStats(HundredGlobalState state) {
        var statsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainerStats");

        // remove any existing things because this is a dumb view for now
        foreach (var n in statsBox.GetChildren().ToArray()) {
            statsBox.RemoveChild(n);
            n.Free();
        }

        statsBox.AddChild(new Label() {
            Text = state.CarDetails.Name
        });

        var stats = new RichTextLabel() {
            LayoutMode = 2,
            BbcodeEnabled = true,
            SizeFlagsHorizontal = SizeFlags.Fill,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.Off
        };
        statsBox.AddChild(stats);

        var maxTorque = state.CarDetails.Engine.MaxTorque();
        var maxKw = state.CarDetails.Engine.MaxKw();
        stats.AppendText($"Max Torque (Nm): {double.Round(maxTorque.Item1, 2)} @ {maxTorque.Item2} rpm\n");
        stats.AppendText($"Max Power (kW): {double.Round(maxKw.Item1, 2)} @ {maxKw.Item2} rpm\n");
        stats.PushColor(Colors.White);
        stats.PushTable(4);
        var prevDetails = _oldCarDetails.GetResultsInTree();
        var details = state.CarDetails.GetResultsInTree();
        foreach (var entry in details) {
            // if the values are different show them:
            if ((dynamic)entry.Value != (dynamic)prevDetails.First(x => x.Name == entry.Name).Value) {
                // https://stackoverflow.com/a/8855857/9353639
                // TODO support array diff detection

                stats.PushCell();
                stats.AppendText(entry.Name);
                stats.Pop();

                stats.PushCell();
                stats.PushColor(Colors.Blue);
                stats.AppendText(GodotClassHelper.ToStringWithRounding(prevDetails.First(x => x.Name == entry.Name).Value, 2));
                stats.Pop();
                stats.Pop();
                stats.PushCell();
                stats.PushColor(Colors.Green);
                stats.AppendText(GodotClassHelper.ToStringWithRounding(entry.Value, 2));
                stats.Pop();
                stats.Pop();

                stats.PushCell();
                stats.PushColor(Colors.Gray);
                stats.AppendText(string.Join(", ", entry.BecauseOf.Select(x => $"[color={x.Color}]{x.Name}[/color]")));
                stats.Pop();
                stats.Pop();
            }
        }

        stats.Pop();
        stats.Pop();

        var torqueCurveGraph = new TorqueCurveGraph(state.CarDetails, _oldCarDetails);
        statsBox.AddChild(torqueCurveGraph);
    }
}
