using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred;

public enum GoalType {
    Nothing,
    SpeedTrap,
    AverageSpeedSection,
    TimeTrial,
    MinimumSpeed,
    SingularSpeed,
}


public static class GoalTypeExtensions {
    record struct GoalTypeDetails(string Description);
    private readonly static Dictionary<GoalType, GoalTypeDetails> DETAILS = new() {
        {GoalType.Nothing, new("Empty Goal")},
        {GoalType.SpeedTrap, new("Get a particular speed at the end of the zone")},
        {GoalType.AverageSpeedSection, new("Maintain a speed average until the end")},
        {GoalType.TimeTrial, new("Complete the section in under a minimum time")},
        {GoalType.MinimumSpeed, new("Make sure you don't drop below the minimum speed")},
        {GoalType.SingularSpeed, new("Hit a high speed at any point")}
    };

    public static string Description(this GoalType type) => DETAILS[type].Description;

    public static double GoalValue(this GoalType type, double distance, double totalLength) {
        var distanceFraction = distance / (100f * 1000f);

        return type switch {
            GoalType.Nothing => 0,
            // forumla: start at 50km/h -> 150km/h at the end
            GoalType.SpeedTrap => MyMath.KmhToMs(50 + 100 * distanceFraction),
            // forumla: start at 50km/h -> 100km/h at the end
            GoalType.AverageSpeedSection => MyMath.KmhToMs(50 + 50 * distanceFraction),
            // formula: start at 60msec/(50km/h -> 100/sec)
            GoalType.TimeTrial => totalLength / MyMath.KmhToMs(50 + 50 * distanceFraction),
            // formula: start at 25km/h -> 50km/h
            GoalType.MinimumSpeed => MyMath.KmhToMs(25 + 25 * distanceFraction),
            // forumla: start at 75km/h -> 200km/h at the end
            GoalType.SingularSpeed => MyMath.KmhToMs(75 + 125 * distanceFraction),
            _ => throw new Exception(type + " doesn't support a goal value"),
        };
    }
}
