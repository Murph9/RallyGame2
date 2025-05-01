using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Hundred.Relics;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred;

public readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);

public partial class HundredGlobalState : Node {

    [Signal]
    public delegate void GoalChangedEventHandler();

    [Signal]
    public delegate void MoneyIncreasedEventHandler(float amount);
    [Signal]
    public delegate void MoneyDecreasedEventHandler(float amount);
    [Signal]
    public delegate void CarDetailsChangedEventHandler();
    [Signal]
    public delegate void SecondPassedEventHandler(int totalTime);
    [Signal]
    public delegate void RivalRaceStartedEventHandler(Car Rival);
    [Signal]
    public delegate void RivalRaceStoppedEventHandler(Car Rival);
    [Signal]
    public delegate void RivalRaceWonEventHandler(Car Rival, double moneyWon);
    [Signal]
    public delegate void RivalRaceLostEventHandler(Car Rival, double moneyLost);

    public RelicManager RelicManager { get; private set; }

    public float TargetDistance { get; private set; }

    public Car Car { get; private set; }
    public CarDetails CarDetails { get; private set; }
    public double TotalTimePassed { get; private set; }
    public float Money { get; private set; }
    public float DistanceTravelled { get; private set; }

    public float CurrentPlayerSpeed { get; private set; }

    public RivalRace? RivalRaceDetails { get; private set; }
    public string RivalRaceMessage { get; private set; }
    public double RivalWinBaseAmount { get; private set; }

    public int ShopPartCount { get; private set; }
    public int ShopRelicCount { get; private set; }

    public int GoalSelectCount { get; private set; }
    public float GoalSpread { get; private set; }
    public float GoalZoneLength { get; private set; }
    public GoalState Goal { get; private set; }

    public double ShopStoppedTimer { get; set; }
    public double ShopCooldownTimer { get; set; }
    public double ShopStoppedTriggerAmount { get; private set; }
    public double ShopCooldownTriggerAmount { get; private set; }

    public HundredGlobalState() {
        Reset();
    }

    public override void _PhysicsProcess(double delta) {
        Goal._PhysicsProcess(delta, CurrentPlayerSpeed);
    }

    public void Reset() {
        if (RelicManager != null) {
            RemoveChild(RelicManager);
        }

        TargetDistance = 100 * 1000; // m

        Car = null;
        CarDetails = null;

        TotalTimePassed = 0; // s
        Money = 0; // $
        DistanceTravelled = 0; // m
        CurrentPlayerSpeed = 0; // m/s

        RivalRaceDetails = null;
        RivalRaceMessage = null;
        RivalWinBaseAmount = 1000;

        ShopPartCount = 3;
        ShopRelicCount = 2;

        ShopStoppedTriggerAmount = 3;
        ShopCooldownTriggerAmount = 5;

        GoalSpread = 1000;
        GoalZoneLength = 250;
        GoalSelectCount = 2;

        var randGoal = RandHelper.RandFromList(Enum.GetValues<GoalType>().Except([GoalType.Nothing]).ToArray());
        Goal = new(randGoal, WorldType.Simple2, 0, GoalSpread, GoalZoneLength);

        RelicManager = new RelicManager(this);
        AddChild(RelicManager);
    }

    public void SetGoal(GoalState goal) {
        if (goal is null) {
            GD.Print("Updated goal is null, please dont");
        }

        Goal = goal;
        EmitSignal(SignalName.GoalChanged);
    }

    public IEnumerable<GoalState> GenerateNewGoals(int count) {
        var goalsWithoutNothing = Enum.GetValues<GoalType>().Except([GoalType.Nothing]).ToList();
        var roadTypes = Enum.GetValues<WorldType>().ToList();
        for (var i = 0; i < count; i++) {

            var goalType = RandHelper.RandFromList(goalsWithoutNothing);
            goalsWithoutNothing.Remove(goalType);

            var roadType = RandHelper.RandFromList(roadTypes);
            roadTypes.Remove(roadType);

            yield return new GoalState(goalType, roadType, Goal.GlobalEndDistance, GoalSpread, GoalZoneLength);
        }
    }

    public void AddMoney(float delta) {
        Money += delta;

        if (delta > 0)
            EmitSignal(SignalName.MoneyIncreased, Math.Abs(delta));
        if (delta < 0)
            EmitSignal(SignalName.MoneyDecreased, Math.Abs(delta));
    }

    public void SetCar(Car car) {
        Car = car;
        EmitSignal(SignalName.CarDetailsChanged); // this is when the car object (and details) actually changes
    }

    public void SetCarDetails(CarDetails carDetails) {
        CarDetails = carDetails;
        // not sure if this needs an event, its an internal detail
    }
    public void AddTotalTimePassed(double delta) {
        TotalTimePassed += delta;

        if (Math.Floor(TotalTimePassed - delta) != Math.Floor(TotalTimePassed))
            EmitSignal(SignalName.SecondPassed, Math.Floor(TotalTimePassed));
    }
    public void SetDistanceTravelled(float newDistanceTravelled) {
        DistanceTravelled = Mathf.Max(DistanceTravelled, newDistanceTravelled); // please no negative progress
    }

    public void SetCurrentSpeedMs(float value) {
        CurrentPlayerSpeed = value;
    }

    public void RivalStarted(RivalRace race, string message) {
        RivalRaceDetails = race;
        RivalRaceMessage = message;

        EmitSignal(SignalName.RivalRaceStarted, race.Rival);
    }

    public void RivalStopped() {
        var rival = RivalRaceDetails.Value.Rival;
        RivalRaceDetails = null;

        EmitSignal(SignalName.RivalRaceStopped, rival);
    }

    public void RivalCheckpointSet() {
        if (RivalRaceDetails.HasValue)
            RivalRaceDetails = RivalRaceDetails.Value with { CheckpointSent = true };
    }

    public void RivalRaceFinished(Car rival, bool playerWon, string message, float moneyDiff) {
        RivalRaceMessage = message;
        if (playerWon)
            EmitSignal(SignalName.RivalRaceWon, rival, moneyDiff);
        else
            EmitSignal(SignalName.RivalRaceLost, rival, 0);

        AddMoney(moneyDiff);
    }

    // Physics based events
    [Signal]
    public delegate void TrafficCollisionEventHandler(Car trafficCar, Vector3 apparentVelocity);
    public void CollisionWithTraffic(Car trafficCar, Vector3 apparentVelocity, Vector3 resultVelocityDifference) {
        EmitSignal(SignalName.TrafficCollision, trafficCar, apparentVelocity);

        CollisionWithOther(resultVelocityDifference);
    }

    [Signal]
    public delegate void CarDamageEventHandler(float amount);
    public void CollisionWithOther(Vector3 resultVelocityDifference) {
        var amount = resultVelocityDifference.Length();
        if (amount < 2)
            return;
        Car.Damage += amount;
        EmitSignal(SignalName.CarDamage, amount);
    }
}
