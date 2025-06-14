using Godot;
using murph9.RallyGame2.godot.World.Procedural;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredGoalSelectScreen : CenterContainer {
    [Signal]
    public delegate void ClosedEventHandler();

    private GoalState _selected;

    public override void _Ready() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        var optionsBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer/VBoxContainer/VBoxContainerOptions");

        var goalOptions = state.GenerateNewGoals(state.GoalSelectCount);

        foreach (var goal in goalOptions) {
            var container = new HBoxContainer();
            container.AddChild(new ColorRect() {
                Color = Colors.Aqua,
                CustomMinimumSize = new Vector2(50, 50),
            });
            container.AddChild(new Label() {
                Text = goal.Description()
            });
            container.AddChild(new RichTextLabel() {
                Text = goal.RoadType.Description(),
                LayoutMode = 2,
                FitContent = true,
                BbcodeEnabled = true,
                AutowrapMode = TextServer.AutowrapMode.Off
            });
            var optionButton = new Button() {
                Text = "Choose",
            };
            optionButton.Pressed += () => {
                _selected = goal;
            };
            container.AddChild(optionButton);

            optionsBox.AddChild(container);
        }

        var selectButton = new Button() {
            Text = "Select"
        };
        selectButton.Pressed += () => {
            if (_selected == null)
                return;

            state.SetGoal(_selected);
            EmitSignal(SignalName.Closed);
        };
        optionsBox.AddChild(selectButton);
    }
}
