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

    public float DistanceTravelled => _car.DistanceTravelled;
    public Vector3 CarPos => _car.RigidBody.GlobalPosition;
    public Vector3 CarLinearVelocity => _car.RigidBody.LinearVelocity;

    public HundredRacingScene() {
    }

    public override void _Ready() {
        _car = new Car(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY), Transform3D.Identity);
        // TODO hardcoded
        _car.RigidBody.Transform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
        AddChild(_car);
    }

    public override void _Process(double delta) {
        
    }

    public void Exit() {
        EmitSignal(SignalName.Restart);
    }

    public void ResetCarTo(Vector3 pos) {
        _car.RigidBody.Position = pos;
        _car.RigidBody.LinearVelocity *= 0;
    }

    public void StopDriving() {
        _car.IgnoreInputs();
    }

    public void StartDriving() {
        _car.AcceptInputs();
    }
}
