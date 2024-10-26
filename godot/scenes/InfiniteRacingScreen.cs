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

public partial class InfiniteRacingScreen : Node3D {
    // Tracks the current driving stuff

    [Signal]
    public delegate void FinishedEventHandler();
    [Signal]
    public delegate void RestartEventHandler();

    // private readonly RacingUI _racingUI;

    private Car Car;

    public InfiniteRacingScreen() {
        // AddChild(_racingUI);
    }

    public override void _Ready() {
        Car = new Car(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY), Transform3D.Identity);
        // TODO hardcoded
        Car.RigidBody.Transform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
        AddChild(Car);
    }

    public override void _Process(double delta) {
        
    }

    public void Exit() {
        EmitSignal(SignalName.Restart);
    }

    public void StopDriving() {
        Car.IgnoreInputs();
    }

    public void StartDriving() {
        Car.AcceptInputs();
    }

    public Vector3 GetCarPos() {
        return Car.RigidBody.GlobalPosition;
    }
}
