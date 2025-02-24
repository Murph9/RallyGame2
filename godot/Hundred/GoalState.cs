namespace murph9.RallyGame2.godot.Hundred;

public class GoalState(GoalType Type, float StartDistance, float EndDistance) {
    public GoalType Type { get; } = Type;
    public float StartDistance { get; } = StartDistance;
    public float EndDistance { get; } = EndDistance;

    public float RealStartingDistance { get; set; }
    public double GoalStartTime { get; set; }

    public bool Ready { get; set; } = true;
    public bool EndPlaced { get; set; }
    public bool InProgress { get; set; }
    public string FinishedMessage { get; private set; }

    public void StartDistanceIs(float distanceAtPos) {
        RealStartingDistance = distanceAtPos;
    }

    public void StartHitAt(double totalTimePassed) {
        InProgress = true;
        GoalStartTime = totalTimePassed;
    }

    public void EndedAt(double gameTime, float distance) {
        FinishedMessage = "yay";
        InProgress = false;
        Ready = false;
    }

};
