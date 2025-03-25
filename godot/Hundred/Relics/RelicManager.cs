using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public partial class RelicManager : Node {
    private readonly HundredGlobalState _hundredGlobalState;

    private readonly List<Relic> _relics = [];

    public RelicManager(HundredGlobalState hundredGlobalState) {
        _hundredGlobalState = hundredGlobalState;

        _hundredGlobalState.TrafficCollision += TrafficCollision;
        _hundredGlobalState.CarDetailsChanged += () => CarDetailsChanged(false);
    }

    public void AddRelic(RelicType type, float strength = 1) {
        if (type == RelicType.BOUNCY) {
            _relics.Add(new BouncyRelic(strength));
        } else if (type == RelicType.JUMP) {
            _relics.Add(new JumpRelic(strength));
        } else if (type == RelicType.BIGFAN) {
            _relics.Add(new BigFanRelic(strength));
        } else if (type == RelicType.FUELREDUCE) {
            _relics.Add(new FuelReductionRelic(strength));
        } else {
            throw new Exception("Unknown relic type: " + type);
        }

        // trigger all things that need to modify the car state
        CarDetailsChanged(true);
    }

    public List<Relic> GetRelics() => _relics;

    private void TrafficCollision(Car otherCar, Vector3 relativeVelocity) {
        if (_hundredGlobalState.Car.RigidBody.Freeze) return;

        foreach (var relic in _relics) {
            if (relic is IOnTrafficCollisionRelic t) {
                t.TrafficCollision(_hundredGlobalState.Car, otherCar, relativeVelocity);
            }
        }
    }

    private void ActionPressed(string action) {
        if (_hundredGlobalState.Car.RigidBody.Freeze) return;

        foreach (var relic in _relics) {
            if (relic is IOnKeyRelic key) {
                key.ActionPressed(_hundredGlobalState.Car, action);
            }
        }
    }

    public override void _Process(double delta) {
        if (!IsInstanceValid(_hundredGlobalState.Car?.RigidBody)) return;
        if (_hundredGlobalState.Car?.RigidBody.Freeze ?? true) return;

        if (Input.IsActionJustPressed("car_action_1")) {
            ActionPressed("car_action_1");
        }
        if (Input.IsActionJustPressed("car_action_2")) {
            ActionPressed("car_action_2");
        }
        if (Input.IsActionJustPressed("car_action_3")) {
            ActionPressed("car_action_3");
        }
        if (Input.IsActionJustPressed("car_action_4")) {
            ActionPressed("car_action_4");
        }

        foreach (var relic in _relics) {
            relic._Process(_hundredGlobalState.Car, delta);
        }
    }

    public override void _PhysicsProcess(double delta) {
        if (!IsInstanceValid(_hundredGlobalState.Car?.RigidBody)) return;
        if (_hundredGlobalState.Car?.RigidBody?.Freeze ?? true) return;

        foreach (var relic in _relics) {
            relic._PhysicsProcess(_hundredGlobalState.Car, delta);
        }
    }


    private void CarDetailsChanged(bool onlyNew) {
        foreach (var relic in _relics) {
            if (relic is IOnPurchaseRelic key) {
                // if onlyNew is called we check if its applied first (i.e. when a new relic is added)
                if (onlyNew && key.Applied) continue;

                key.CarUpdated(_hundredGlobalState.Car);
            }
        }
    }

    public List<RelicType> GetValidRelics() {
        return RelicType.ALL_RELIC_TYPES.Except(_relics.Select(x => x.Type)).ToList();
    }
}
