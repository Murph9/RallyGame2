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
	[Signal]
	public delegate void RestartEventHandler();

	private readonly RacingUI _racingUI;
	private readonly List<Checkpoint> _checkpoints = [];

	private Car Car;

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
		Car.RigidBody.Position += new Vector3(-15, 0, 0);
		AddChild(Car);

		foreach (var (checkpoint, index) in worldPieces.GetCheckpoints().WithIndex()) {
			AddChild(DebugHelper.GenerateWorldText("Checkpoint: " + index.ToString(), checkpoint + new Vector3(0, 1, 0)));

			var size = new Vector3(12, 12, 12);
			if (index == 0) {
				size = new Vector3(1, 12, 12);
			}
			var checkArea = Checkpoint.AsBox(checkpoint, size, new Color(1, 1, 1, 0.3f));
			checkArea.ThingEntered += (Node3D node) => { CheckpointDetection(index, node); };
			_checkpoints.Add(checkArea);
			AddChild(checkArea);
		}
	}

	private void CheckpointDetection(int checkId, Node3D node) {
		if (checkId == CurrentCheckpoint && node == Car.RigidBody) {
			if (CurrentCheckpoint == 0) {
				CurrentLap++;
				if (CurrentLap > 1)
					LapTimes.Add(LapTimer);
				LapTimer = 0;
			}
			CurrentCheckpoint++;
			if (CurrentCheckpoint + 1 > _checkpoints.Count) {
				CurrentCheckpoint = 0;
			}
		}
	}

	public override void _Process(double delta) {
		LapTimer += delta;

		if (CurrentLap > 1) {
			var state = GetNode<GlobalState>("/root/GlobalState");
			state.AddResult(new RoundResult() {
				Time = LapTimes.Min()
			});
			EmitSignal(SignalName.Closed);
		}
	}

	public (Vector3, Vector3) GetCarAndCheckpointPos() {
		return (Car.RigidBody.Position, _checkpoints[CurrentCheckpoint].Position);
	}

	public void Exit() {
		EmitSignal(SignalName.Restart);
	}
}
