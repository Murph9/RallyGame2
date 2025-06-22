using Godot;
using murph9.RallyGame2.godot.Hundred.Goals;

namespace murph9.RallyGame2.godot.Hundred;

public abstract partial class GoalState(float startDistance, float timeoutTime) : Node3D {
    public float TimeoutTime { get; } = timeoutTime;
    public float StartDistance { get; } = startDistance;

    public double GoalActiveFor { get; protected set; }
    public bool? Successful { get; protected set; } = null;

    public abstract GoalType Type { get; }
    public abstract double TargetScore();

    protected abstract bool CheckSuccessful(double gameTime, float carSpeed);

    public override void _PhysicsProcess(double delta) {
        GoalActiveFor += delta;

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (state.TotalTimePassed > TimeoutTime) {
            Successful = false;
        } else if (!Successful.HasValue && CheckSuccessful(state.TotalTimePassed, state.CurrentPlayerSpeed)) {
            Successful = true;
        }
    }

    public string ProgressString(double gameTime, float currentDistance, float carLinearVelocity) {
        if (Successful.HasValue && Successful.Value) {
            var successString = Successful.Value ? "" : "not ";
            return $"Goal {Type}: Was {successString}successful";
        }

        return Progress(gameTime, currentDistance, carLinearVelocity);
    }
    protected abstract string Progress(double gameTime, float currentDistance, float carLinearVelocity);
};
