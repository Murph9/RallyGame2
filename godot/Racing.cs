using Godot;
using murph9.RallyGame2.godot.Cars;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Debug;
using murph9.RallyGame2.godot.World;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class Racing : Node3D
{
	private Car _car;
	private Vector3[] _checkpoints;

	private int _currentLap;
	private int _currentCheckpoint;
	private double _lapTimer;
	private List<double> _lapTimes = new ();

	public override void _Ready()
	{
		var worldPieces = new SimpleWorldPieces();
        AddChild(worldPieces);

        var details = CarType.Runner.LoadCarDetails(Main.DEFAULT_GRAVITY);
        _car = new Car(details, worldPieces.GetSpawn());
		AddChild(_car);

		int i = 0;
		_checkpoints = worldPieces.GetCheckpoints().ToArray();
		foreach (var checkpoint in _checkpoints) {
			AddChild(DebugHelper.GenerateWorldText("Checkpoint: " + i.ToString(), checkpoint + new Vector3(0, 1, 0)));
			i++;
		}

		AddChild(new Line2D() {
			Width = 3,
			Name = "checkpointLine",
			Points = new Vector2[] { new (), new () }
		});
	}

	public override void _Process(double delta)
	{
		var pos = _car.RigidBody.GlobalPosition;

		if (pos.DistanceTo(_checkpoints[_currentCheckpoint]) < 5) {
			if (_currentCheckpoint == 0) {
				_currentLap++;
				if (_currentLap > 1)
					_lapTimes.Add(_lapTimer);
				_lapTimer = 0;
			}
			_currentCheckpoint++;
			if (_currentCheckpoint + 1 > _checkpoints.Length) {
				_currentCheckpoint = 0;
			}
		}

		_lapTimer += delta;

		GetNode<Label>("VBoxContainer/GridContainer/LapLabel").Text = "Lap: " + _currentLap;
		GetNode<Label>("VBoxContainer/GridContainer/CheckpointLabel").Text = "Checkpoint: " + _currentCheckpoint;
		GetNode<Label>("VBoxContainer/GridContainer/TimeLabel").Text = "Lap Time: " + double.Round(_lapTimer, 3) + "\n"
				+ string.Join('\n', _lapTimes.Select(x => double.Round(x, 1)));

		var cam = GetViewport().GetCamera3D();
		GetNode<Line2D>("checkpointLine").Points = new Vector2[] {
			cam.UnprojectPosition(_car.RigidBody.Position + new Vector3(0, 0.3f, 0)),
			cam.UnprojectPosition(_checkpoints[_currentCheckpoint] + new Vector3(0, 0.3f, 0))
		};
	}

	public void _on_back_button_pressed() {
		GetTree().ChangeSceneToFile("res://main.tscn");
	}
}
