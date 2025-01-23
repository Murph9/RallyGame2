using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class HundredRacingScene : Node3D {
    // Tracks the current driving stuff

    [Signal]
    public delegate void FinishedEventHandler();
    [Signal]
    public delegate void RestartEventHandler();

    private Car _car;

    public Transform3D InitialPosition { get; set; } = Transform3D.Identity;
    public float DistanceTravelled => _car.DistanceTravelled;
    public Vector3 CarPos => _car.RigidBody.GlobalPosition;
    public Vector3 CarLinearVelocity => _car.RigidBody.LinearVelocity;

    public override void _Ready() {
        _car = new Car(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY), null, Transform3D.Identity);
        _car.RigidBody.Transform = InitialPosition;
        AddChild(_car);
    }

    public override void _Process(double delta) {

    }

    public void Exit() {
        EmitSignal(SignalName.Restart);
    }

    public void ResetCarTo(Transform3D transform) {
        _car.RigidBody.Position = transform.Origin + new Vector3(0, 0.5f, 0);
        _car.RigidBody.Basis = transform.Basis;

        _car.RigidBody.LinearVelocity *= 0;
        _car.RigidBody.AngularVelocity *= 0;
    }

    public void StopDriving() {
        _car.Inputs.IgnoreInputs();
    }

    public void StartDriving() {
        _car.Inputs.AcceptInputs();
    }

    public bool IsMainCar(Node3D node) => _car.RigidBody == node;
}
