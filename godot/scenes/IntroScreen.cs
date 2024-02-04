using Godot;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot.scenes;

public partial class IntroScreen : CenterContainer, IScene {
	[Signal]
    public delegate void ClosedEventHandler();

	public override void _Ready() {
		var state = GetNode<GlobalState>("/root/GlobalState");
		if (state.CarDetails != null) {
			state.Reset();
		}

		state.CarDetails = CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY);

		var root = GetNode<PanelContainer>("PanelContainer");

		var main = new VBoxContainer();
		root.AddChild(main);

		main.AddChild(new Label() {
			Text = "Driver for cut throat team"
		});
		main.AddChild(new Label() {
			Text = "You start with a base car and have to meet each rounds expectations otherwise you are cut from the team"
		});
		main.AddChild(new Label() {
			Text = "There will be the option to upgrade the car as you go, but the requirements improve as you go"
		});
		main.AddChild(new Label() {
			Text = "You will lose eventually"
		});

		// button to start
		var b = new Button() {
			Text = "Start"
		};
		b.Pressed += () => EmitSignal(SignalName.Closed);
		main.AddChild(b);
	}

	public override void _Process(double delta) {}
}
