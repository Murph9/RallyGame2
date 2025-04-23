using Godot;
using murph9.RallyGame2.godot.Hundred.Relics;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredRelicScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    private Relic _selected;

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
            var generatedRelic = state.RelicManager.GenerateRelic(relic, 1);

            var container = new HBoxContainer();
            container.AddChild(new ColorRect() {
                Color = Colors.Aqua,
                CustomMinimumSize = new Vector2(50, 50),
            });
            container.AddChild(new Label() {
                Text = generatedRelic.GetType().Name
            });
            container.AddChild(new RichTextLabel() {
                Text = generatedRelic.DescriptionBBCode,
                LayoutMode = 2,
                FitContent = true,
                BbcodeEnabled = true,
                AutowrapMode = TextServer.AutowrapMode.Off
            });
            var optionButton = new Button() {
                Text = "Choose",
            };
            optionButton.Pressed += () => {
                if (_selected == generatedRelic)
                    return;

                _selected = generatedRelic;
            };
            container.AddChild(optionButton);

            optionsBox.AddChild(container);
        }

        var saveButton = new Button() {
            Text = "Select"
        };
        saveButton.Pressed += () => {
            state.RelicManager.AddRelic(_selected);
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
