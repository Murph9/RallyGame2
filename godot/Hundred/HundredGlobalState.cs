using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.scenes;

public readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);

public partial class HundredGlobalState : Node {
    public readonly List<Part> PartsUpgraded = [];

    public double TotalTimePassed { get; set; }
    public float NextDistanceMilestone { get; set; } = 100; // in meters
    public float Money { get; set; } = 1000;
    public CarDetails CarDetails { get; set; }
    public RivalRace? RivalRaceDetails { get; set; }

    public HundredGlobalState() {
        Reset();
    }

    public void Reset() {
        TotalTimePassed = 0;
        NextDistanceMilestone = 100;
        Money = 0;
        CarDetails = null;
        RivalRaceDetails = null;

        PartsUpgraded.Clear();
    }
}