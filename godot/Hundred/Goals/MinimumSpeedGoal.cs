using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class MinimumSpeedGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    private const float MAX_SLOW_SPEED_TIME = 15;
    public override GoalType Type => GoalType.MinimumSpeed;

    public double TimeSpentBelowTargetSpeed { get; private set; }

    // formula: start at 25km/h -> 50km/h
    public override double TargetScore() => MyMath.KmhToMs(25 + 25 * (StartDistance / (100 * 1000f)));

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => GoalActiveFor > (TimeoutTime - 1) && TimeSpentBelowTargetSpeed < MAX_SLOW_SPEED_TIME;

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) {
        return $"Keep above {MyMath.MsToKmh(TargetScore()):0} km/h, Max time below {MAX_SLOW_SPEED_TIME} sec, current {TimeSpentBelowTargetSpeed:0.##} sec";
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (state.CurrentPlayerSpeed < TargetScore()) {
            TimeSpentBelowTargetSpeed += delta;
        }

        if (TimeSpentBelowTargetSpeed > MAX_SLOW_SPEED_TIME) {
            Successful = false;
        }
    }
}
