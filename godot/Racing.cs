using Godot;
using murph9.RallyGame2.godot.Cars;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class Racing : Node3D {
	private Car Car;
	private Vector3[] Checkpoints;

	private RacingUI _racingUI;

	public int CurrentLap { get; set; }
	public int CurrentCheckpoint { get; set; }
	public double LapTimer { get; set; }
	public List<double> LapTimes { get; set; } = new ();

	public override void _Ready()
	{
		var uiScene = GD.Load<PackedScene>("res://RacingUI.tscn");
		_racingUI = uiScene.Instantiate<RacingUI>();
		_racingUI.Racing = this;
		AddChild(_racingUI);

		var worldPieces = new SimpleWorldPieces();
        AddChild(worldPieces);

        var details = CarType.Runner.LoadCarDetails(Main.DEFAULT_GRAVITY);
        Car = new Car(details, worldPieces.GetSpawn());
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
	}

	public (Vector3, Vector3) GetCarAndCheckpointPos() {
		return (Car.RigidBody.Position, Checkpoints[CurrentCheckpoint]);
	}

	public void Exit() {
		GetTree().ChangeSceneToFile("res://main.tscn");
	}
}
