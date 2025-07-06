using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public partial class RelicManager : Node {
    internal HundredGlobalState HundredGlobalState { get; private set; }
    internal GlobalState GlobalState { get; private set; }

    private readonly List<Relic> _relics = [];

    public RelicManager() {
        // a little validation of all relics
        var allRelics = GetAllPossibleRelics();
        foreach (var relic in allRelics) {
            var relicClass = GenerateRelic(relic);

            if (relicClass.RequiredRelics.Length == 0) {
                continue;
            }

            if (relicClass.RequiredRelics.Any(x => !x.Name.Contains('.'))) {
                throw new ArgumentException(relicClass.GetType().Name + " has a required relic with just a class name. Please use typeof(<relicclass>).FullName");
            }
        }
    }

    public override void _Ready() {
        HundredGlobalState = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        GlobalState = GetNode<GlobalState>("/root/GlobalState");

        HundredGlobalState.TrafficCollision += TrafficCollision;
        HundredGlobalState.CarDamage += CarDamaged;
        HundredGlobalState.CarAnyCollision += CarAnyCollision;
        HundredGlobalState.CarDetailsChanged += () => CarDetailsChanged(false);
        HundredGlobalState.RivalRaceStarted += RivalRaceStarted;
        HundredGlobalState.RivalRaceWon += RivalRaceWon;
        HundredGlobalState.RivalRaceLost += RivalRaceLost;
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

    public IEnumerable<RelicType> GetAvailableRelics() {
        var currentRelics = _relics.Select(x => new RelicType(x.GetType().FullName));
        var allMinusCurrent = RelicType.ALL_RELIC_CLASSES
            .Select(x => new RelicType(x.FullName))
            .Except(currentRelics)
            .ToList();

        foreach (var relicOption in allMinusCurrent) {
            var option = GenerateRelic(relicOption);

            if (option.RequiredRelics.Length == 0) {
                yield return relicOption;
                continue;
            }

            // figure out if all the required relics are in the current relics
            if (!option.RequiredRelics.Except(currentRelics).Any()) {
                yield return relicOption;
            }
        }
    }

    public List<RelicType> GetAllPossibleRelics() {
        return RelicType.ALL_RELIC_CLASSES
            .Select(x => new RelicType(x.FullName))
            .ToList();
    }

    public override void _Process(double delta) {
        if (!IsInstanceValid(GlobalState.PlayerCar?.RigidBody)) return;
        if (GlobalState.PlayerCar?.RigidBody.Freeze ?? true) return;

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
            relic._Process(GlobalState.PlayerCar, delta);
        }
    }

    public override void _PhysicsProcess(double delta) {
        if (!IsInstanceValid(GlobalState.PlayerCar?.RigidBody)) return;
        if (GlobalState.PlayerCar?.RigidBody?.Freeze ?? true) return;

        foreach (var relic in _relics) {
            relic._PhysicsProcess(GlobalState.PlayerCar, delta);
        }
    }

    private void CarDamaged(float amount) {
        if (GlobalState.PlayerCar.RigidBody.Freeze) return;

        foreach (var damaged in AllRelicsOfType<IDamagedRelic>()) {
            damaged.DamageTaken(GlobalState.PlayerCar, amount);
        }
    }

    private void TrafficCollision(Car otherCar, Vector3 apparentVelocity) {
        if (GlobalState.PlayerCar.RigidBody.Freeze) return;

        foreach (var trafficCollision in AllRelicsOfType<IOnTrafficCollisionRelic>()) {
            trafficCollision.TrafficCollision(GlobalState.PlayerCar, otherCar, apparentVelocity);
        }
    }

    private void CarAnyCollision() {
        if (GlobalState.PlayerCar.RigidBody.Freeze) return;

        foreach (var trafficCollision in AllRelicsOfType<IAnyCollisionRelic>()) {
            trafficCollision.AnyCollision(GlobalState.PlayerCar);
        }
    }

    private void ActionPressed(string action) {
        if (GlobalState.PlayerCar.RigidBody.Freeze) return;

        foreach (var onKey in AllRelicsOfType<IOnKeyRelic>()) {
            onKey.ActionPressed(GlobalState.PlayerCar, action);
        }
    }


    private void CarDetailsChanged(bool onlyNew) {
        foreach (var onPurchace in AllRelicsOfType<IOnPurchaseRelic>()) {
            // if onlyNew is called we check if its applied first (i.e. when a new relic is added)
            if (onlyNew && onPurchace.Applied) continue;

            onPurchace.CarUpdated(GlobalState.PlayerCar);
        }

        // enable any models required
        foreach (var relic in _relics) {
            foreach (var addition in relic.CarModelAdditions) {
                GlobalState.PlayerCar.ToggleAddition(addition, true);
            }
        }
    }

    private void RivalRaceStarted(Car rival) {
        foreach (var rr in AllRelicsOfType<IRivalRaceRelic>()) {
            rr.RivalRaceStarted(rival);
        }
    }

    private void RivalRaceWon(Car rival, double moneyWon) {
        foreach (var rr in AllRelicsOfType<IRivalRaceRelic>()) {
            rr.RivalRaceWon(rival, moneyWon);
        }
    }

    private void RivalRaceLost(Car rival, double moneyWon) {
        foreach (var rr in AllRelicsOfType<IRivalRaceRelic>()) {
            rr.RivalRaceLost(rival, moneyWon);
        }
    }

    private IEnumerable<T> AllRelicsOfType<T>() where T : class {
        return _relics.Where(x => x is T t).Select(x => x as T);
    }
}
