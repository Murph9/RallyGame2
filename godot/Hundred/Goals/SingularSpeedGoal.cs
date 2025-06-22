using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class SingularSpeedGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    public override GoalType Type => GoalType.SingularSpeed;

    public float HighestZoneSpeed { get; private set; }

    // forumla: start at 100km/h -> 250km/h at the end
    public override double TargetScore() => MyMath.KmhToMs(100 + 150 * (StartDistance / (100 * 1000f)));
    public override string Description() => $"Hit {TargetScore():0.#} km/h at any point";

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => TargetScore() < HighestZoneSpeed;

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) {
        return $"Achieve {MyMath.MsToKmh(TargetScore()):0} km/h once, current best: {MyMath.MsToKmh(HighestZoneSpeed):0.##} km/h";
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (Type == GoalType.SingularSpeed) {
            HighestZoneSpeed = Mathf.Max(state.CurrentPlayerSpeed, HighestZoneSpeed);
        }
    }
}
