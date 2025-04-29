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
}
