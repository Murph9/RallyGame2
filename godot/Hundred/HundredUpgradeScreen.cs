using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component;
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
        var optionsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerOptions");

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

            var container = new HBoxContainer();
            container.AddChild(new TextureRect() {
                Texture = part.IconImage,
                CustomMinimumSize = new Vector2(100, 100),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            });
            container.AddChild(new Label() {
                Text = $"{part.Name} lvl {part.CurrentLevel + 1} for ${part.LevelCost[part.CurrentLevel + 1]}"
            });
            var optionButton = new Button() {
                Text = "Choose",
                Disabled = part.LevelCost[part.CurrentLevel + 1] > state.Money
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
            container.AddChild(optionButton);

            optionsBox.AddChild(container);
        }

        var saveButton = new Button() {
            Text = "Buy"
        };
        saveButton.Pressed += () => {
            if (_appliedPart != null)
                state.Money -= (float)_appliedPart.LevelCost[_appliedPart.CurrentLevel];
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
        var statsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerStats");

        // remove any existing things because this is a dumb view for now
        foreach (var n in statsBox.GetChildren().ToArray()) {
            statsBox.RemoveChild(n);
            n.Free();
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
        var prevDetails = _oldCarDetails.GetResultsInTree();
        var details = state.CarDetails.GetResultsInTree();

        stats.PushCell();
        stats.Pop();
        stats.PushCell();
        stats.AppendText("Current  ");
        stats.Pop();
        stats.PushCell();
        stats.AppendText("New  ");
        stats.Pop();

        var maxTorque = state.CarDetails.Engine.MaxTorque();
        var maxTorquePrev = _oldCarDetails.Engine.MaxTorque();
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

        var maxKw = state.CarDetails.Engine.MaxKw();
        var maxKwPrev = _oldCarDetails.Engine.MaxKw();
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

        var torqueCurveGraph = new TorqueCurveGraph(state.CarDetails, null, _oldCarDetails, null);
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
