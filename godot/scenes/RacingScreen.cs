using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class RacingScreen : Node3D {
	[Signal]
    public delegate void ClosedEventHandler();

	private readonly RacingUI _racingUI;

	private Car Car;
	private Vector3[] Checkpoints;

	public List<double> LapTimes { get; init; } = [];

	public int CurrentLap { get; private set; }
	public int CurrentCheckpoint { get; private set; }
	public double LapTimer { get; private set; }

	public RacingScreen() {
		var uiScene = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(RacingUI)));
		_racingUI = uiScene.Instantiate<RacingUI>();
		_racingUI.Racing = this;
		AddChild(_racingUI);
	}

	public override void _Ready() {

		var worldPieces = new SimpleWorldPieces();
        AddChild(worldPieces);

		var state = GetNode<GlobalState>("/root/GlobalState");
        Car = new Car(state.CarDetails, worldPieces.GetSpawn());
		AddChild(Car);

		int i = 0;
		Checkpoints = worldPieces.GetCheckpoints().ToArray();
		foreach (var checkpoint in Checkpoints) {
			AddChild(DebugHelper.GenerateWorldText("Checkpoint: " + i.ToString(), checkpoint + new Vector3(0, 1, 0)));
			i++;
		}
	}

	public override void _Process(double delta)
	{
		var pos = Car.RigidBody.GlobalPosition;

		if (pos.DistanceTo(Checkpoints[CurrentCheckpoint]) < 15) {
			if (CurrentCheckpoint == 0) {
				CurrentLap++;
				if (CurrentLap > 1)
					LapTimes.Add(LapTimer);
				LapTimer = 0;
			}
			CurrentCheckpoint++;
			if (CurrentCheckpoint + 1 > Checkpoints.Length) {
				CurrentCheckpoint = 0;
			}
		}

		LapTimer += delta;

		if (CurrentLap > 3) {
			EmitSignal(SignalName.Closed);
		}
	}

	public (Vector3, Vector3) GetCarAndCheckpointPos() {
		return (Car.RigidBody.Position, Checkpoints[CurrentCheckpoint]);
	}

	public void Exit() {
		GetTree().ChangeSceneToFile("res://main.tscn");
	}
}
