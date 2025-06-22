using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class MinimumSpeedGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    public override GoalType Type => GoalType.MinimumSpeed;

    public double TimeSpentBelowTargetSpeed { get; private set; }

    public override string Description() => $"Don't drop below {TargetScore():0.#} km/h";
    // formula: start at 25km/h -> 50km/h
    public override double TargetScore() => MyMath.KmhToMs(25 + 25 * (StartDistance / (100 * 1000f)));

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => GoalActiveFor > (TimeoutTime - 1) && TimeSpentBelowTargetSpeed < 5;

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) {
        return $"Keep above {MyMath.MsToKmh(TargetScore()):0} km/h, Max time below 5 sec, current {TimeSpentBelowTargetSpeed:0.##} sec";
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (Type == GoalType.MinimumSpeed && state.CurrentPlayerSpeed < TargetScore()) {
            TimeSpentBelowTargetSpeed += delta;
        }
    }
}
