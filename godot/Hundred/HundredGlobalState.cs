using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Hundred.Relics;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);

public partial class HundredGlobalState : Node {

    public RelicManager RelicManager { get; private set; }

    public float TargetDistance { get; private set; }

    public float NextShopDistance { get; private set; }
    public float ShopSpread { get; private set; }

    public CarDetails CarDetails { get; private set; }
    public double TotalTimePassed { get; private set; }
    public float Money { get; private set; }
    public float DistanceTravelled { get; private set; }

    public float CurrentSpeedMs { get; private set; }
    public float CurrentSpeedKMH => MyMath.MsToKmh(CurrentSpeedMs);

    public RivalRace? RivalRaceDetails { get; private set; }
    public string RivalRaceMessage { get; private set; }

    public int ShopPartCount { get; private set; }

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
    }

    public void SetCarDetails(CarDetails carDetails) => CarDetails = carDetails;
    public void AddTotalTimePassed(double delta) {
        TotalTimePassed += delta;
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
    }

    public void RivalStopped() {
        RivalRaceDetails = null;
    }

    public void RivalCheckpointSet() {
        if (RivalRaceDetails.HasValue)
            RivalRaceDetails = RivalRaceDetails.Value with { CheckpointSent = true };
    }

    public void RivalRaceFinished(bool playerWon, string message, float moneyDiff) {
        RivalRaceMessage = message;

        AddMoney(moneyDiff);
    }

    public void UpdateCarDetails(CarDetails newCarDetails) {
        CarDetails = newCarDetails;
    }

    // TODO events based on this
}
