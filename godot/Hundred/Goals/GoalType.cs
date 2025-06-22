namespace murph9.RallyGame2.godot.Hundred.Goals;

public enum GoalType {
    Nothing,
    SpeedTrap,
    MinimumSpeed,
    SingularSpeed,
    TrafficCollisions,
    RivalWins,
}

public static class GoalTypeExtensions {
    public static GoalState Generate(this GoalType type, float startDistance, float timeoutSeconds) => type switch {
        GoalType.Nothing => new NothingGoal(startDistance, timeoutSeconds),
        GoalType.MinimumSpeed => new MinimumSpeedGoal(startDistance, timeoutSeconds),
        GoalType.SingularSpeed => new SingularSpeedGoal(startDistance, timeoutSeconds),
        GoalType.SpeedTrap => new SpeedTrapGoal(startDistance, timeoutSeconds),
        GoalType.TrafficCollisions => new TrafficCollisionsGoal(startDistance, timeoutSeconds),
        GoalType.RivalWins => new RivalWinsGoal(startDistance, timeoutSeconds),
        _ => null
    };
}
