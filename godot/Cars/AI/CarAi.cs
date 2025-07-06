using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.Debug3D;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public abstract partial class CarAi(IRoadManager roadManager) : Node3D, ICarInputs {

    private const float POINT_TARGET_BUFFER = 3; // car width used in TooFast calc

    protected readonly IRoadManager _roadManager = roadManager;

    private Car _car;
    public Car Car {
        get { return _car; }
        set {
            _car = value;
            _car.Details.TractionControl = true; // my ai is not good enough to ignore traction control
        }
    }

    public bool IsAi => true;

    public bool HandbrakeCur { get; protected set; }

    public float AccelCur { get; protected set; }

    public float BrakingCur { get; protected set; }

    public float Steering { get; protected set; }

    protected bool _listeningToInputs = true;

    public void AcceptInputs() => _listeningToInputs = true;
    public void IgnoreInputs() => _listeningToInputs = false;
    public void ReadInputs() { }

    private float GetWantSteerAngleToTarget(Vector3 pos) {
        var curPos = Car.RigidBody.GlobalPosition;
        var curDir = Car.RigidBody.GlobalTransform.Basis * Vector3.Back;

        return (pos - curPos).ToV2XZ().AngleTo(curDir.ToV2XZ());
    }
    protected void SteerAt(Transform3D pos) => SteerAt(pos.Origin);
    protected void SteerAt(Vector3 pos) {
        var steeringWant = GetWantSteerAngleToTarget(pos);
        Steering = Mathf.Clamp(steeringWant, -Car.Details.MaxSteerAngle, Car.Details.MaxSteerAngle);
    }
    protected bool ShouldTurnLeftFor(Vector3 pos) => GetWantSteerAngleToTarget(pos) > 0;
    protected bool ShouldTurnRightFor(Vector3 pos) => GetWantSteerAngleToTarget(pos) < 0;

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
        var circleOffsetPos = new Vector2(vel.Y, -vel.X).Normalized() * (currentMaxTurnRadius + POINT_TARGET_BUFFER);

        var leftCircleCenter = pos + circleOffsetPos;
        var targetDistanceToLeftCenter = (target - leftCircleCenter).Length();

        var rightCircleCenter = pos - circleOffsetPos;
        var targetDistanceToRightCenter = (target - rightCircleCenter).Length();

        return targetDistanceToLeftCenter > currentMaxTurnRadius && targetDistanceToRightCenter > currentMaxTurnRadius;
    }

    protected void FlipIfSlowUpsideDown() {
        if (Car.RigidBody.LinearVelocity.Length() > 10)
            return;
        if (Car.RigidBody.GlobalTransform.Basis.Y.AngleTo(Vector3.Up) > Mathf.Pi / 2) {
            // TODO this isn't quite right but its pretty close to the direction they want to go
            var nextCheckPoints = _roadManager.GetPassedCheckpoint(Car.RigidBody.GlobalPosition);
            Car.ResetCarTo(new Transform3D(nextCheckPoints.Basis, Car.RigidBody.Transform.Origin));
        }
    }

    protected bool IsDrifting() {
        if (Car.RigidBody.LinearVelocity.Length() < 2)
            return false; // prevent this from blocking all moving

        if (Car.RigidBody.AngularVelocity.LengthSquared() > 0.7)
            return true; // starting a drift

        if (Car.RigidBody.LinearVelocity.Length() < 8 && Car.DriftAngle > 5)
            return true; // drifting and has speed

        // check if the drive wheels are spinning
        var driveWheelSlipRatioTotal = Car.Wheels.Where(x => Car.Details.IsIdADriveWheel(x.Details.Id)).Sum(x => x.SlipRatio);
        if (driveWheelSlipRatioTotal * 5 > Car.Wheels.Length) {
            return true;
        }

        // in a drift
        return Car.DriftAngle > Car.Details.MinDriftAngle;
    }

    protected bool IsTooFastForWall(Vector3 checkpointPos, Vector3 wallStart, Vector3 wallDir) {
        // Similar to the point version but to avoid a wall
        var wallStartXZ = wallStart.ToV2XZ();
        var wallDirXZ = wallDir.ToV2XZ();

        var pos = Car.RigidBody.GlobalPosition.ToV2XZ();
        var posY = Car.RigidBody.GlobalPosition.Y;
        var vel = Car.RigidBody.LinearVelocity.ToV2XZ();

        var currentMaxTurnRadius = (float)CarRoughCalc.BestRadiusAtSpeed(Car.Details, vel.Length()); // should this use the 2d or 3d version of velocity?

        var circleOffsetPos = new Vector2(vel.Y, -vel.X).Normalized() * currentMaxTurnRadius;

        var leftCircleCenter = pos + circleOffsetPos;
        var rightCircleCenter = pos - circleOffsetPos;

        float distance;
        if (ShouldTurnLeftFor(checkpointPos)) {
            distance = DistanceToRay(wallStartXZ, wallStartXZ + wallDirXZ, leftCircleCenter);
            DebugShapes.INSTANCE.AddCircleXYDebug3D(ToString() + "fastcircle", leftCircleCenter.ToV3XZ(posY), currentMaxTurnRadius, 256, Colors.Purple);
        } else {
            distance = DistanceToRay(wallStartXZ, wallStartXZ + wallDirXZ, rightCircleCenter);
            DebugShapes.INSTANCE.AddCircleXYDebug3D(ToString() + "fastcircle", rightCircleCenter.ToV3XZ(posY), currentMaxTurnRadius, 256, Colors.Purple);
        }

        var tooFast = currentMaxTurnRadius > distance;

        return tooFast;
    }

    private static float DistanceToRay(Vector2 wallStart, Vector2 wallEnd, Vector2 point) {

        var vectorToObject = point - wallStart;
        var wallDirNormalized = (wallEnd - wallStart).Normalized();
        var dotted = wallDirNormalized.Dot(vectorToObject);

        // use the origin as its the closest point
        var closest = wallStart;
        if (dotted > (wallEnd - wallStart).Length()) {
            // then its the end of the wall
            closest = wallEnd;
        } else if (dotted > 0) {
            // find the projected point along the ray
            closest = wallStart + wallDirNormalized * dotted;
        }

        return (closest - point).Length();
    }
}
