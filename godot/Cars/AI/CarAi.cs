using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.Debug3D;
using System;

namespace murph9.RallyGame2.godot.Cars.AI;

public abstract partial class CarAi : Node3D, ICarInputs {

    private const float POINT_TARGET_BUFFER = 3; // car width used in TooFast calc

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

    protected void SteerAt(Transform3D pos) => SteerAt(pos.Origin);
    protected void SteerAt(Vector3 pos) {
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
        var circleOffsetPos = new Vector2(vel.Y, -vel.X).Normalized() * (currentMaxTurnRadius + POINT_TARGET_BUFFER);

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

    protected bool IsTooFastForWall(Vector3 wallStart, Vector3 wallDir) {
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

        var leftDistance = DistanceToRay(wallStartXZ, wallStartXZ + wallDirXZ, leftCircleCenter);
        var rightDistance = DistanceToRay(wallStartXZ, wallStartXZ + wallDirXZ, rightCircleCenter);

        var tooFast = currentMaxTurnRadius > Mathf.Max(leftDistance, rightDistance);
        if (tooFast) {
            DebugShapes.INSTANCE.AddCircleXYDebug3D(ToString() + "leftcircle", leftCircleCenter.ToV3XZ(posY), currentMaxTurnRadius, 64, leftDistance > rightDistance ? Colors.Purple : Colors.Transparent);
            DebugShapes.INSTANCE.AddCircleXYDebug3D(ToString() + "rightcircle", rightCircleCenter.ToV3XZ(posY), currentMaxTurnRadius, 64, rightDistance > leftDistance ? Colors.Purple : Colors.Transparent);
        }
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
