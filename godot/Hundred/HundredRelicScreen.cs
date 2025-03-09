using Godot;
using murph9.RallyGame2.godot.Hundred.Relics;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredRelicScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    private RelicType _selected;

    public override void _Ready() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        var optionsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerOptions");

        var allRelics = state.RelicManager.GetValidRelics();

        for (int i = 0; i < state.ShopRelicCount; i++) {
            if (allRelics.Count <= 0) {
                break;
            }

            var relic = allRelics[Mathf.Abs((int)(GD.Randi() % allRelics.Count))];
            allRelics.Remove(relic);

            var container = new HBoxContainer();
            container.AddChild(new TextureRect() {
                // Texture = relic.GetType().Name,
                CustomMinimumSize = new Vector2(100, 100),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            });
            container.AddChild(new Label() {
                Text = $"{relic.Name}"
            });
            var optionButton = new Button() {
                Text = "Choose",
            };
            optionButton.Pressed += () => {
                if (_selected == relic)
                    return;

                _selected = relic;
            };
            container.AddChild(optionButton);

            optionsBox.AddChild(container);
        }

        var saveButton = new Button() {
            Text = "Select"
        };
        saveButton.Pressed += () => {
            state.RelicManager.AddRelic(_selected, 1);
            EmitSignal(SignalName.Closed);
        };
        optionsBox.AddChild(saveButton);

        var chooseNothing = new Button() {
            Text = "Leave"
        };
        chooseNothing.Pressed += () => {
            EmitSignal(SignalName.Closed);
        };
        optionsBox.AddChild(chooseNothing);
    }
}
