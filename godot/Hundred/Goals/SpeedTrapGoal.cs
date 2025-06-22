using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class SpeedTrapGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    public override GoalType Type => GoalType.SpeedTrap;

    // forumla: start at 50km/h -> 150km/h at the end
    public override double TargetScore() => MyMath.KmhToMs(50 + 100 * (StartDistance / (100 * 1000f)));

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) {
        return $"Hit {MyMath.MsToKmh(TargetScore()):0} km/h in {TimeoutTime - gameTime:0.#} sec";
    }

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => GoalActiveFor > (TimeoutTime - 1) && TargetScore() < carSpeed;
}
