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
    record struct GoalTypeDetails(bool TargetIsSpeed, string DescriptionFormatString, string ActiveDetailFormatString);
    private readonly static Dictionary<GoalType, GoalTypeDetails> DETAILS = new() {
        {GoalType.Nothing, new(false, "Free", "Completed")},
        {GoalType.SpeedTrap, new(true, "Hit the speed trap at least {0:0.#} km/h", "Hit {0:0} km/h in {1:0.#} km")},
        {GoalType.AverageSpeedSection, new(true, "Maintain the average speed of {0:0.#} km/h", "Maintain {0:0}km/h, current: {1:0}km/h")},
        {GoalType.TimeTrial, new(false, "Complete the section in under {0:0.#} seconds", "Time remaining: {0:0.#} sec, distance remaining {1:0.##} km")},
        {GoalType.MinimumSpeed, new(true, "Don't drop below {0:0.#} km/h", "Keep above {0:0} km/h, Max time below 5 sec, current {1:0.##} sec")},
        {GoalType.SingularSpeed, new(true, "Hit {0:0.#} km/h at any point", "Achieve {0:0} km/h once, current best: {1:0.##} km/h")}
    };

    public static string GetDescriptionFormat(this GoalType type) => DETAILS[type].DescriptionFormatString;
    public static string GetActiveDetailFormat(this GoalType type) => DETAILS[type].ActiveDetailFormatString;
    public static bool TargetIsSpeed(this GoalType type) => DETAILS[type].TargetIsSpeed;

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
