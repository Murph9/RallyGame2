using System;
using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World;

namespace murph9.RallyGame2.godot.Component;

public partial class CircuitRoadManager : Node3D {
	[Signal]
    public delegate void LoadedEventHandler();

    private readonly SimpleWorldPieces _world;

    public IWorld World => _world;

	public Curve3D RoadCurve { get; private set; }

	public float ExpectedFinishTime { get; private set; }

    public CircuitRoadManager() {
        _world = new SimpleWorldPieces();
    }

    public override void _Ready() {
        AddChild(_world);
		var state = GetNode<GlobalState>("/root/GlobalState");
		var carDetails = state.CarDetails;

		// calc track length
		RoadCurve = new Curve3D() {
			BakeInterval = 5,
		};
		foreach (var curvePoint in _world.GetCurve3DPoints()) {
			if (curvePoint.PIn.HasValue) AddChild(DebugHelper.GenerateWorldText("PIn", curvePoint.Point + curvePoint.PIn.Value));
			if (curvePoint.POut.HasValue) AddChild(DebugHelper.GenerateWorldText("POut", curvePoint.Point + curvePoint.POut.Value));
			RoadCurve.AddPoint(curvePoint.Point + new Vector3(0, 1, 0), curvePoint.PIn, curvePoint.POut);
		}

		// car sim props
		var totalTime = 0f;
		var curSpeed = 1f;
		var totalDistance = 0f;

		// draw it
		var points = RoadCurve.GetBakedPoints();
		for (int i = 1; i < points.Length - 1; i++) {
			// calc radius from points around this one
			var before = points[i-1];
			var cur = points[i];
			var after = points[i+1];

			var radius = MyMath.GetCircleCenterFrom(before, cur, after);

			// simulate the car going along the road (on the line)
			var distanceOfStep = cur.DistanceTo(after);
			// Console.WriteLine("r guess " + CarRoughCalc.BestRadiusAtSpeed(Car.Details, curSpeed) + " road " + radius);
			if (CarRoughCalc.BestRadiusAtSpeed(carDetails, curSpeed) < radius) {
				curSpeed += (float)(CarRoughCalc.CalcBestAccel(carDetails, curSpeed) * distanceOfStep) / (float)carDetails.TotalMass;
			} else {
				curSpeed -= carDetails.BrakeMaxTorque * carDetails.WheelDetails[0].Radius * distanceOfStep / (float)carDetails.TotalMass;
			}
			curSpeed = Math.Max(0, curSpeed); // the math isn't amazing so make sure it doesn't go negative

			totalTime += distanceOfStep / curSpeed;
			totalDistance += distanceOfStep;
			// Console.WriteLine("t: " + totalTime + " s: " + curSpeed + " d: " + totalDistance);
		}

		ExpectedFinishTime = (float)Math.Ceiling(totalTime);

		EmitSignal(SignalName.Loaded);
    }
}
