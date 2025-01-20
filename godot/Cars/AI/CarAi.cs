using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Cars.AI;

public abstract partial class CarAi : Node3D {
    protected readonly IRoadManager _roadManager;
    public Car Car { get; set; }

    public CarAi(IRoadManager roadManager) {
        _roadManager = roadManager;
    }

    protected void DriveAt(Transform3D pos) => DriveAt(pos.Origin);
    protected void DriveAt(Vector3 pos) {
        var curPos = Car.RigidBody.GlobalPosition;
        var curVel = Car.RigidBody.LinearVelocity;

        var angle = curPos.ToV2XZ().AngleToPoint(pos.ToV2XZ());
        GD.Print(angle);

        if (angle < 0) {
            // turn left
            // Car._Input() TODO
        }
    }
}

public partial class TrafficCarAi : CarAi {

    private static readonly float MAX_SPEED = 50;

    public TrafficCarAi(IRoadManager roadManager) : base(roadManager) {

    }

    public override void _PhysicsProcess(double delta) {
        var nextCheckPoint = _roadManager.GetNextCheckpoint(Car.RigidBody.GlobalPosition);

        // TODO drive towards it
        DriveAt(nextCheckPoint);
    }
}
