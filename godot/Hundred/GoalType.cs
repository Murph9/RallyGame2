using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred;

public enum GoalType {
    Nothing, SpeedTrap, AverageSpeedSection, TimeTrial
}


public static class GoalTypeExtensions {
    record struct GoalTypeDetails(string Description);
    private readonly static Dictionary<GoalType, GoalTypeDetails> DETAILS = new() {
        {GoalType.Nothing, new("Empty Goal")},
        {GoalType.SpeedTrap, new("Get a particular speed at the end of the zone")},
        {GoalType.AverageSpeedSection, new("Maintain a speed average until the end")},
        {GoalType.TimeTrial, new("Complete the section in under a minimum time")},
    };

    public static string Description(this GoalType type) => DETAILS[type].Description;

    public static double GoalValue(this GoalType type, double gameTime, double distance, double totalLength) {
        return type switch {
            // forumla: start at 50km/h -> 150km/h at the end
            GoalType.SpeedTrap => (50 + distance / (100 * 1000) * 100) / 3.6f,
            // forumla: start at 50km/h -> 100km/h at the end
            GoalType.AverageSpeedSection => (50 + distance / (100 * 1000) * 50) / 3.6f,
            // formula: start at 60msec/(50km/h) -> 100/sec
            GoalType.TimeTrial => totalLength / (50 + distance / (100 * 1000) * 50) / 3.6f,
            _ => throw new Exception(type + " doesn't support a goal value"),
        };
    }
}
