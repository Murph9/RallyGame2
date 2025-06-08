using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public partial class RelicManager : Node {
    private readonly HundredGlobalState _hundredGlobalState;
    internal HundredGlobalState HundredGlobalState => _hundredGlobalState;

    private readonly List<Relic> _relics = [];

    public RelicManager(HundredGlobalState hundredGlobalState) {
        _hundredGlobalState = hundredGlobalState;

        _hundredGlobalState.TrafficCollision += TrafficCollision;
        _hundredGlobalState.CarDamage += CarDamaged;
        _hundredGlobalState.CarDetailsChanged += () => CarDetailsChanged(false);
        _hundredGlobalState.RivalRaceStarted += RivalRaceStarted;
        _hundredGlobalState.RivalRaceWon += RivalRaceWon;
        _hundredGlobalState.RivalRaceLost += RivalRaceLost;
    }

    public Relic GenerateRelic(RelicType type, float strength = 1) {
        var relic = Activator.CreateInstance(Type.GetType(type), this, type, strength);
        if (relic != null)
            return relic as Relic;

        throw new Exception("Unknown relic type: " + type);
    }

    public void AddRelic(Relic relic) {
        if (relic == null) return;

        _relics.Add(relic);

        // trigger all things that need to modify the car state
        CarDetailsChanged(true);
    }

    public List<Relic> GetRelics() => _relics;

    public List<RelicType> GetValidRelics() {
        return RelicType.ALL_RELIC_CLASSES
            .Select(x => new RelicType(x.FullName))
            .Except(_relics.Select(x => new RelicType(x.GetType().FullName)))
            .ToList();
    }
    public List<RelicType> GetAllRelics() {
        return RelicType.ALL_RELIC_CLASSES
            .Select(x => new RelicType(x.FullName))
            .ToList();
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

    private void CarDamaged(float amount) {
        if (_hundredGlobalState.Car.RigidBody.Freeze) return;

        foreach (var relic in _relics) {
            if (relic is IDamagedRelic t) {
                t.DamageTaken(_hundredGlobalState.Car, amount);
            }
        }
    }

    private void TrafficCollision(Car otherCar, Vector3 apparentVelocity) {
        if (_hundredGlobalState.Car.RigidBody.Freeze) return;

        foreach (var relic in _relics) {
            if (relic is IOnTrafficCollisionRelic t) {
                t.TrafficCollision(_hundredGlobalState.Car, otherCar, apparentVelocity);
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


    private void CarDetailsChanged(bool onlyNew) {
        foreach (var relic in _relics) {
            if (relic is IOnPurchaseRelic key) {
                // if onlyNew is called we check if its applied first (i.e. when a new relic is added)
                if (onlyNew && key.Applied) continue;

                key.CarUpdated(_hundredGlobalState.Car);
            }
        }
    }

    private void RivalRaceStarted(Car rival) {
        foreach (var relic in _relics) {
            if (relic is IRivalRaceRelic rivalRelic) {
                rivalRelic.RivalRaceStarted(rival);
            }
        }
    }

    private void RivalRaceWon(Car rival, double moneyWon) {
        foreach (var relic in _relics) {
            if (relic is IRivalRaceRelic rivalRelic) {
                rivalRelic.RivalRaceWon(rival, moneyWon);
            }
        }
    }

    private void RivalRaceLost(Car rival, double moneyWon) {
        foreach (var relic in _relics) {
            if (relic is IRivalRaceRelic rivalRelic) {
                rivalRelic.RivalRaceLost(rival, moneyWon);
            }
        }
    }
}
