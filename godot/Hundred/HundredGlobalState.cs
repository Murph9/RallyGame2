using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Hundred.Relics;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);

public partial class HundredGlobalState : Node {

    [Signal]
    public delegate void MoneyIncreasedEventHandler(float amount);
    [Signal]
    public delegate void MoneyDecreasedEventHandler(float amount);
    [Signal]
    public delegate void CarDetailsChangedEventHandler();
    [Signal]
    public delegate void SecondPassedEventHandler(int totalTime);
    [Signal]
    public delegate void RivalRaceStartedEventHandler();
    [Signal]
    public delegate void RivalRaceResetEventHandler();
    [Signal]
    public delegate void RivalRaceWonEventHandler();
    [Signal]
    public delegate void RivalRaceLostEventHandler();

    public RelicManager RelicManager { get; private set; }

    public float TargetDistance { get; private set; }

    public float NextShopDistance { get; private set; }
    public float ShopSpread { get; private set; }

    public Car Car { get; private set; }
    public CarDetails CarDetails { get; private set; }
    public double TotalTimePassed { get; private set; }
    public float Money { get; private set; }
    public float DistanceTravelled { get; private set; }

    public float CurrentSpeedMs { get; private set; }
    public float CurrentSpeedKMH => MyMath.MsToKmh(CurrentSpeedMs);

    public RivalRace? RivalRaceDetails { get; private set; }
    public string RivalRaceMessage { get; private set; }

    public int ShopPartCount { get; private set; }
    public int ShopRelicCount { get; private set; }

    public float GoalSpread { get; private set; }
    public GoalState Goal { get; private set; }

    public HundredGlobalState() {
        Reset();
    }

    public void Reset() {
        if (RelicManager != null) {
            RemoveChild(RelicManager);
        }

        TargetDistance = 100 * 1000; // m

        Car = null;
        CarDetails = null;
        NextShopDistance = 100; // m
        ShopSpread = 500; // m

        TotalTimePassed = 0; // s
        Money = 0; // $
        DistanceTravelled = 0; // m
        CurrentSpeedMs = 0; // m/s

        RivalRaceDetails = null;
        RivalRaceMessage = null;

        ShopPartCount = 3;
        ShopRelicCount = 2;

        GoalSpread = 1000;
        Goal = new(CalcGoalType(), GoalSpread, 500);

        RelicManager = new RelicManager(this);
        AddChild(RelicManager);
    }

    private static GoalType CalcGoalType() {
        var options = Enum.GetValues(typeof(GoalType));
        var index = new RandomNumberGenerator().RandiRange(0, options.Length - 1);
        return (GoalType)options.GetValue(index);
    }

    public void GenerateNewGoal() {
        Goal = new(CalcGoalType(), Goal.TriggerDistance + GoalSpread, 500);
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
        CurrentSpeedMs = value;
    }

    public void SetNextShopDistance(float value) {
        NextShopDistance = value;
    }

    public void RivalStarted(RivalRace race, string message) {
        RivalRaceDetails = race;
        RivalRaceMessage = message;

        EmitSignal(SignalName.RivalRaceStarted);
    }

    public void RivalStopped() {
        RivalRaceDetails = null;

        EmitSignal(SignalName.RivalRaceReset);
    }

    public void RivalCheckpointSet() {
        if (RivalRaceDetails.HasValue)
            RivalRaceDetails = RivalRaceDetails.Value with { CheckpointSent = true };
    }

    public void RivalRaceFinished(bool playerWon, string message, float moneyDiff) {
        RivalRaceMessage = message;
        if (playerWon)
            EmitSignal(SignalName.RivalRaceWon);
        else
            EmitSignal(SignalName.RivalRaceLost);

        AddMoney(moneyDiff);
    }

    // and a list of events so we can track them here
    [Signal]
    public delegate void TrafficCollisionEventHandler(Car trafficCar); // TODO this might need some other data
    public void CollisionWithTraffic(Car trafficCar) => EmitSignal(SignalName.TrafficCollision, trafficCar);

}
