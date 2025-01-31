using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.scenes;

public readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);

public partial class HundredGlobalState : Node {
    public readonly List<Part> PartsUpgraded = [];

    public float TargetDistance { get; set; }
    public float MinimumSpeedKMH { get; set; }

    public CarDetails CarDetails { get; set; }
    public float NextDistanceMilestone { get; set; }
    public double TotalTimePassed { get; set; }
    public float Money { get; set; } = 1000;
    public float DistanceTravelled { get; set; }
    public float CurrentSpeedKMH { get; set; }
    public double MinimumSpeedProgress { get; set; }

    public RivalRace? RivalRaceDetails { get; set; }
    public string RivalRaceMessage { get; set; }

    public int ShopPartCount { get; set; }

    public HundredGlobalState() {
        Reset();
    }

    public void Reset() {
        TargetDistance = 100 * 1000; // m
        MinimumSpeedKMH = 50; // km/h

        CarDetails = null;
        NextDistanceMilestone = 100; // m

        TotalTimePassed = 0;
        Money = 0; // $
        DistanceTravelled = 0;
        CurrentSpeedKMH = 0;
        MinimumSpeedProgress = 0;

        RivalRaceDetails = null;
        RivalRaceMessage = null;

        ShopPartCount = 3;

        PartsUpgraded.Clear();
    }
}