using System;
using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Hundred;

public readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);

public partial class HundredGlobalState : Node {
    public float TargetDistance { get; set; }

    public float NextShopDistance { get; set; }
    public float ShopSpread { get; set; }

    public CarDetails CarDetails { get; set; }
    public double TotalTimePassed { get; set; }
    public float Money { get; set; }
    public float DistanceTravelled { get; set; }
    public float CurrentSpeedKMH { get; set; }

    public RivalRace? RivalRaceDetails { get; set; }
    public string RivalRaceMessage { get; set; }

    public int ShopPartCount { get; set; }

    public float GoalSpread { get; set; }
    public GoalState Goal { get; set; }

    public HundredGlobalState() {
        Reset();
    }

    public void Reset() {
        TargetDistance = 100 * 1000; // m

        CarDetails = null;
        NextShopDistance = 100; // m
        ShopSpread = 500; // m

        TotalTimePassed = 0; // s
        Money = 0; // $
        DistanceTravelled = 0; // m
        CurrentSpeedKMH = 0; // km/h

        RivalRaceDetails = null;
        RivalRaceMessage = null;

        ShopPartCount = 3;

        GoalSpread = 5000;
        Goal = new(CalcGoalType(), GoalSpread, GoalSpread + 500);
    }

    private static GoalType CalcGoalType() {
        var options = Enum.GetValues(typeof(GoalType));
        var index = new RandomNumberGenerator().RandiRange(0, options.Length - 1);
        return (GoalType)options.GetValue(index);
    }
}