using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class RacingScreen : Node3D {
	[Signal]
	public delegate void FinishedEventHandler();
	[Signal]
	public delegate void RestartEventHandler();

	private readonly RacingUI _racingUI;
	private readonly List<Checkpoint> _checkpointNodes = [];

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
		var state = GetNode<GlobalState>("/root/GlobalState");
		Car = new Car(state.CarDetails, null, state.WorldDetails.CircuitRoadManager.World.GetSpawn());
		Car.RigidBody.Position += new Vector3(-15, 0, 0);
		AddChild(Car);

		var roadManager = state.WorldDetails.CircuitRoadManager;

		var checkpoints = roadManager.World.GetCheckpoints().ToArray();
		for (var i = 0; i < checkpoints.Length; i++) {
			var curCheckpoint = checkpoints[i];

			AddChild(DebugHelper.GenerateWorldText("Checkpoint: " + i.ToString(), curCheckpoint.Origin + new Vector3(0, 1, 0)));

			var size = new Vector3(12, 12, 12);
			if (i == 0) {
				size = new Vector3(1, 12, 12);
			}
			var index = i; // this is to protect the lambda below from losing the index

			var checkArea = Checkpoint.AsBox(curCheckpoint, size, new Color(1, 1, 1, 0.3f));
			checkArea.ThingEntered += (Node3D node) => { CheckpointDetection(index, node); };
			_checkpointNodes.Add(checkArea);
			AddChild(checkArea);
		}

		var trackCurve = new Curve3D() {
			BakeInterval = 5,
		};
		foreach (var curvePoint in roadManager.World.GetCurve3DPoints()) {
			if (curvePoint.PIn.HasValue) AddChild(DebugHelper.GenerateWorldText("PIn", curvePoint.Point + curvePoint.PIn.Value));
			if (curvePoint.POut.HasValue) AddChild(DebugHelper.GenerateWorldText("POut", curvePoint.Point + curvePoint.POut.Value));
			trackCurve.AddPoint(curvePoint.Point + new Vector3(0, 1, 0), curvePoint.PIn, curvePoint.POut);
		}

		// draw it
		var last = trackCurve.GetPointPosition(0);
		var points = trackCurve.GetBakedPoints();
		for (int i = 1; i < points.Length - 1; i++) {
			// calc radius from points around this one
			var before = points[i - 1];
			var cur = points[i];
			var after = points[i + 1];

			var radius = MyMath.GetCircleCenterFrom(before, cur, after);
			var colour = Mathf.InverseLerp(100, 10, radius);

			var l = BoxLine(new Color() {
				R = (float)colour,
				G = (float)colour,
				B = (float)colour
			}, last, cur);
			l.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			AddChild(l);
			last = cur;
		}
	}

	private void CheckpointDetection(int checkId, Node3D node) {
		if (checkId == CurrentCheckpoint && node.GetParent() is Car) { // TODO hope there is one car
			if (CurrentCheckpoint == 0) {
				CurrentLap++;
				if (CurrentLap > 1)
					LapTimes.Add(LapTimer);
				LapTimer = 0;
			}
			CurrentCheckpoint++;
			if (CurrentCheckpoint + 1 > _checkpointNodes.Count) {
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
			EmitSignal(SignalName.Finished);
			CurrentLap = 0;
		}
	}

	public (Vector3, Vector3) GetCarAndCheckpointPos() {
		return (Car.RigidBody.Position, _checkpointNodes[CurrentCheckpoint].Position);
	}

	public void Exit() {
		EmitSignal(SignalName.Restart);
	}

	public void StopDriving() {
		Car.Inputs.IgnoreInputs();
	}

	public void StartDriving() {
		ReplaceCarWithState();
		Car.Inputs.AcceptInputs();

		LapTimes.Clear();
		LapTimer = 0;
		CurrentCheckpoint = 0;
		CurrentLap = 0;

	}

	public void ReplaceCarWithState() {
		RemoveChild(Car);
		Car.QueueFree();

		// clone into new car
		var state = GetNode<GlobalState>("/root/GlobalState");
		Car = new Car(state.CarDetails, null, state.WorldDetails.CircuitRoadManager.World.GetSpawn());
		Car.RigidBody.Position += new Vector3(-15, 0, 0);
		AddChild(Car);
	}

	private static MeshInstance3D BoxLine(Color c, Vector3 start, Vector3 end) {
		var mat = new StandardMaterial3D() {
			AlbedoColor = c
		};
		var length = (end - start).Length();

		var mesh = new BoxMesh() {
			Size = new Vector3(length, 0.1f, 0.1f),
			Material = mat
		};
		var meshObj = new MeshInstance3D() {
			Transform = new Transform3D(new Basis(new Quaternion(new Vector3(1, 0, 0), (end - start).Normalized())), start.Lerp(end, 0.5f)),
			Mesh = mesh
		};

		return meshObj;
	}
}
