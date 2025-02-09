using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Cars.AI;

public abstract partial class CarAi : Node3D, ICarInputs {

    protected readonly IRoadManager _roadManager;

    public Car Car { get; set; }

    public bool IsAi => true;

    public bool HandbrakeCur { get; protected set; }

    public float AccelCur { get; protected set; }

    public float BrakingCur { get; protected set; }

    public float Steering { get; protected set; }

    protected bool _listeningToInputs = true;

    public CarAi(IRoadManager roadManager) {
        _roadManager = roadManager;
    }

    public void AcceptInputs() => _listeningToInputs = true;
    public void IgnoreInputs() => _listeningToInputs = false;
    public void ReadInputs() { }

    protected void DriveAt(Transform3D pos) => DriveAt(pos.Origin);
    protected void DriveAt(Vector3 pos) {
        var curPos = Car.RigidBody.GlobalPosition;
        var curDir = Car.RigidBody.GlobalTransform.Basis * Vector3.Back;

        var steeringWant = (pos - curPos).ToV2XZ().AngleTo(curDir.ToV2XZ());

        Steering = Mathf.Clamp(steeringWant, -Car.Details.MaxSteerAngle, Car.Details.MaxSteerAngle);
    }

    protected bool IsTooSlowForPoint(Transform3D target) => IsTooSlowForPoint(target.Origin);
    protected bool IsTooSlowForPoint(Vector3 target) => IsTooSlowForPoint(target.ToV2XZ());

    protected bool IsTooSlowForPoint(Vector2 target) {
        var pos = Car.RigidBody.GlobalPosition.ToV2XZ();
        var vel = Car.RigidBody.LinearVelocity.ToV2XZ();

        if (pos.DistanceTo(target) < 3) {
            return true;
        }

        var currentMaxTurnRadius = (float)CarRoughCalc.BestRadiusAtSpeed(Car.Details, vel.Length()); // should this use the 2d or 3d version of velocity?

        // generate a 2d sloped cone zone forwards using 2 circles on both sides
        // the space in the middle is where we can go
        var circleOffsetPos = new Vector2(vel.Y, -vel.X).Normalized() * currentMaxTurnRadius;

        var leftCircleCenter = pos + circleOffsetPos;
        var targetDistanceToLeftCenter = (target - leftCircleCenter).Length();

        var rightCircleCenter = pos - circleOffsetPos;
        var targetDistanceToRightCenter = (target - rightCircleCenter).Length();

        return targetDistanceToLeftCenter > currentMaxTurnRadius && targetDistanceToRightCenter > currentMaxTurnRadius;
    }

    protected bool IsDrifting() {
        if (Car.RigidBody.LinearVelocity.Length() < 3 * 3)
            return false; // prevent this from blocking all moving

        if (Car.RigidBody.AngularVelocity.LengthSquared() > 0.7 * 0.7)
            return true; // starting a drift

        // in a drift
        return Car.DriftAngle > Car.Details.MinDriftAngle;
    }
}
