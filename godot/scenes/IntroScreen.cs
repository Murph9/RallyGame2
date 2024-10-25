using System;
using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Component;

namespace murph9.RallyGame2.godot.scenes;

public partial class IntroScreen : CenterContainer, IScene {
	[Signal]
    public delegate void ClosedEventHandler();

	private Button _goButton;
	private bool _roadLoaded;

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
			Text = "Hey Driver"
		});
		main.AddChild(new Label() {
			Text = "You start with a base car and have to meet each rounds expectations otherwise you are cut from the team"
		});
		main.AddChild(new Label() {
			Text = "There will be the option to upgrade the car as you go, but the requirements improve as you go"
		});
		main.AddChild(new Label() {
			Text = "They will kick you out eventually, but try to have some fun"
		});

		_goButton = new Button() {
			Text = "Start"
		};
		_goButton.Pressed += () => EmitSignal(SignalName.Closed);
		_goButton.Disabled = true;
		main.AddChild(_goButton);
	}

	public override void _Process(double delta) {
		if (_roadLoaded) {
			_roadLoaded = false;

			_goButton.Disabled = false;

			// move camera to see the track
			var cam = GetViewport().GetCamera3D();
			if (cam is not null) {
				cam.Position = new Vector3(-20.4f, 2.3f, 0); // TODO some fixed value
				cam.LookAt(new Vector3(0, 0, 0));
			}
		}
	}

    public void RoadLoaded(CircuitRoadManager roadManager) {
		Console.WriteLine("road loaded");

		var state = GetNode<GlobalState>("/root/GlobalState");
		state.SetWorldDetails(new WorldDetails() {
			CircuitRoadManager = roadManager
		});

		_roadLoaded = true;
    }
}
