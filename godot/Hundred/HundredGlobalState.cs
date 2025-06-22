using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Hundred.Goals;
using murph9.RallyGame2.godot.Hundred.Relics;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred;

public class RivalRace(Car rival, float startDistance, float raceDistance) {
    public Car Rival { get; init; } = rival;
    public float StartDistance { get; init; } = startDistance;
    public float RaceDistance { get; init; } = raceDistance;
    public bool CheckpointSet { get; set; }
    public string Message { get; set; }
}

public partial class HundredGlobalState : Node {

    [Signal]
    public delegate void GoalAddedEventHandler();
    [Signal]
    public delegate void GoalWonEventHandler();
    [Signal]
    public delegate void GoalLostEventHandler();

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

    public List<RivalRace> RivalRaceDetails { get; init; } = [];
    public double RivalWinBaseAmount { get; private set; }
    public float RivalRaceDistance { get; private set; }
    public float RivalRaceSpeedDiff { get; private set; }

    public int ShopPartCount { get; private set; }
    public int ShopRelicCount { get; private set; }
    public float ShopCountdownAmount { get; private set; }
    public double ShopResetTimer { get; private set; }

    public int GoalsWon { get; private set; }
    public int GoalsLost { get; private set; }
    public float GoalDistanceSpread { get; private set; }
    public float GoalNextTriggerDistance { get; private set; }
    public float GoalTimeoutSeconds { get; private set; }
    public IReadOnlyCollection<GoalState> Goals { get; private set; } = [];

    public HundredGlobalState() {
        Reset();
    }

    public override void _PhysicsProcess(double delta) {
        foreach (var goal in Goals) {
            if (goal.Successful.HasValue) {
                if (goal.Successful.Value) {
                    EmitSignal(SignalName.GoalWon);
                    GoalsWon++;
                } else {
                    EmitSignal(SignalName.GoalLost);
                    GoalsLost++;
                }
                RemoveGoal(goal);
            }
        }
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

        RivalRaceDetails.Clear();
        RivalWinBaseAmount = 1000;
        RivalRaceDistance = 100;
        RivalRaceSpeedDiff = 3;

        ShopPartCount = 3;
        ShopRelicCount = 2;
        ShopCountdownAmount = 60; // sec
        ShopResetTimer = ShopCountdownAmount;

#if DEBUG
        ShopRelicCount = 5;
        Money = 1000000;
#endif

        GoalTimeoutSeconds = 5 * 60; // sec
        GoalDistanceSpread = 200; // m
        GoalsWon = 0;
        GoalsLost = 0;

        RelicManager = new RelicManager(this);
        AddChild(RelicManager);
    }

    public void AddNewGoal() {
        GoalNextTriggerDistance = DistanceTravelled + GoalDistanceSpread;

        var validNewGoals = Enum.GetValues<GoalType>()
            .Except([GoalType.Nothing])
            .Except(Goals.Select(x => x.Type))
            .ToList();
        if (validNewGoals.Count == 0) {
            return;
        }

        var goal = RandHelper.RandFromList(validNewGoals).Generate(DistanceTravelled, (float)TotalTimePassed + GoalTimeoutSeconds);
        AddChild(goal);

        Goals = Goals.Append(goal).ToList();

        EmitSignal(SignalName.GoalAdded);
    }

    public void RemoveGoal(GoalState goalState) {
        Goals = Goals.Except([goalState]).ToList();
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

        if (GoalNextTriggerDistance < DistanceTravelled) {
            AddNewGoal();
        }
    }

    public void ShopTimerReduced(double delta) => ShopResetTimer -= delta;
    public void ShopTimerReset() => ShopResetTimer = ShopCountdownAmount;

    public void SetDistanceTravelled(float newDistanceTravelled) {
        DistanceTravelled = Mathf.Max(DistanceTravelled, newDistanceTravelled); // please no negative progress
    }

    public void SetCurrentSpeedMs(float value) {
        CurrentPlayerSpeed = value;
    }

    public void RivalStarted(RivalRace rival, string message) {
        RivalRaceDetails.Add(rival);
        rival.Message = message;

        EmitSignal(SignalName.RivalRaceStarted, rival.Rival);
    }

    public void RivalStopped(Car rival) {
        // get rivalRace obj for that car
        var rivalRace = RivalRaceDetails.Single(x => x.Rival == rival);
        RivalRaceDetails.Remove(rivalRace);

        EmitSignal(SignalName.RivalRaceStopped, rival);
    }

    public void RivalRaceFinished(Car rival, bool playerWon, string message, float moneyDiff) {
        var rivalRace = RivalRaceDetails.Single(x => x.Rival == rival);
        rivalRace.Message = message;
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
    [Signal]
    public delegate void CarAnyCollisionEventHandler();
    public void CollisionWithOther(Vector3 resultVelocityDifference) {
        var amount = resultVelocityDifference.Length();
        if (amount < 0.01f) {
            // we'll be nice to the physics engine
            return;
        }

        EmitSignal(SignalName.CarAnyCollision);

        if (amount >= 2) {
            Car.Damage += amount;
            EmitSignal(SignalName.CarDamage, amount);
        }
    }
}
