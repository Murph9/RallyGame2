using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class RivalWinsGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    public override GoalType Type => GoalType.RivalWins;

    private int _rivalWins;

    public override double TargetScore() => 3;

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => _rivalWins >= TargetScore();

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) {
        return $"Beat {TargetScore()} rivals, current: {_rivalWins}";
    }

    public override void _Ready() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        state.RivalRaceWon += (Car other, double _) => _rivalWins++;
    }
}
