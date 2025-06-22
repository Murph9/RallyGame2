namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class NothingGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    public override GoalType Type => GoalType.Nothing;

    public override string Description() => "Free";
    public override double TargetScore() => 0;

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => true;

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) => "Completed";
}
