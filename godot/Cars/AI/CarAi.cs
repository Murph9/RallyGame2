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

        var angle = (pos - curPos).ToV2XZ().AngleTo(curDir.ToV2XZ());

        if (angle > 0) {
            Steering = Math.Min(0.2f, angle); // clamp to value
        } else {
            Steering = Math.Max(-0.2f, angle); // clamp to value
        }
    }
}
