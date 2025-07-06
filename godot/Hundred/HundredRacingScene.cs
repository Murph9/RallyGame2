using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Sim;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredRacingScene : Node3D {
    // Tracks the current driving stuff

    [Signal]
    public delegate void FinishedEventHandler();
    [Signal]
    public delegate void RestartEventHandler();

    private Car _car;

    public Transform3D InitialPosition { get; set; } = Transform3D.Identity;
    public float PlayerDistanceTravelled => _car.DistanceTravelled;
    public Vector3 PlayerCarPos => _car.RigidBody.GlobalPosition;
    public Vector3 PlayerCarLinearVelocity => _car.RigidBody.LinearVelocity;
    public bool PlayerIsAccelerating => _car.Inputs.AccelCur > 0;

    public override void _Ready() {
        // load from global state
        var hundredState = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        var state = GetNode<GlobalState>("/root/GlobalState");
        _car = new Car(hundredState.CarDetails, new RacingAiInputs(state.RoadManager), true, InitialPosition);

        UpdateWithNewCar(hundredState);
    }

    public void ReplaceCarWithState() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (_car.Details == state.CarDetails) return;

        Callable.From(() => {
            var hundredState = GetNode<HundredGlobalState>("/root/HundredGlobalState");
            var newCar = _car.CloneWithNewDetails(hundredState.CarDetails);
            RemoveChild(_car);
            _car.QueueFree();
            _car = newCar;

            UpdateWithNewCar(hundredState);
        }).CallDeferred();
    }

    public void Exit() {
        EmitSignal(SignalName.Restart);
    }

    public void ResetCarTo(Transform3D transform) {
        _car.ResetCarTo(transform);
    }

    public void StopDriving() {
        _car.SetActive(false);
    }

    public void StartDriving() {
        _car.SetActive(true);
    }

    public bool IsMainCar(Node3D node) => _car.RigidBody == node;

    private void UpdateWithNewCar(HundredGlobalState hundredState) {
        AddChild(_car);
        _car.RigidBody.BodyEntered += (node) => {
            var car = node.GetParentOrNull<Car>();
            var collisionDiff = _car.CalcLastFrameVelocityDiff();

            if (car == null) {
                hundredState.CollisionWithOther(collisionDiff);
                return;
            }

            // TODO we should use the details from the contact physics.GetContactLocalVelocityAtPosition from:
            // rid = _car.RigidBody.GetRid() and PhysicsServer3D.BodyGetDirectState(rid)
            hundredState.CollisionWithTraffic(car, _car.RigidBody.LinearVelocity - car.RigidBody.LinearVelocity, collisionDiff);
        };
        hundredState.SetCarDetails(_car.Details);

        GetNode<GlobalState>("/root/GlobalState").PlayerCar = _car;
    }
}
