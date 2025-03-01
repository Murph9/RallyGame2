using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public class GoalState(GoalType Type, float TriggerDistance, float EndDistance) {
    public GoalType Type { get; } = Type;
    public float TriggerDistance { get; } = TriggerDistance;
    public float EndDistance { get; } = EndDistance;

    public float ZoneStartDistance { get; set; }
    public double GoalStartTime { get; set; }

    public bool Ready { get; set; } = true;
    public bool EndPlaced { get; set; }
    public bool InProgress { get; set; }
    public bool? IsSuccessful { get; private set; } = null;

    public void StartDistanceIs(float distanceAtPos) {
        ZoneStartDistance = distanceAtPos;
    }

    public void StartHitAt(double totalTimePassed) {
        InProgress = true;
        GoalStartTime = totalTimePassed;
    }

    public void EndedAt(double gameTime, float endDistance, Vector3 carLinearVelocity) {
        InProgress = false;
        Ready = false;

        IsSuccessful = Type switch {
            GoalType.SpeedTrap => SpeedTrapWasSuccessful(gameTime, carLinearVelocity),
            GoalType.AverageSpeedSection => AverageSpeedWasSuccessful(gameTime, carLinearVelocity),
            GoalType.TimeTrial => (bool?)TimeTrialWasSuccessful(gameTime, carLinearVelocity),
            _ => throw new Exception("Unknown type " + Type),
        };
    }

    public string ProgressString(double gameTime, float distance) {
        if (IsSuccessful.HasValue) {
            var successString = IsSuccessful.Value ? "" : "not ";
            return $"Goal {Type}: Was {successString}successful";
        }

        if (!InProgress) {
            var text = $"Goal {Type} starts at {TriggerDistance}";
            if (ZoneStartDistance > TriggerDistance) // its set show the distance left
                text += $" in {Math.Round(ZoneStartDistance - distance)}m";
            return text;
        }

        switch (Type) {
            case GoalType.SpeedTrap:
                var targetSpeed = SpeedTrapTargetMs(gameTime, ZoneStartDistance);
                return $"SpeedTrap: Hit {targetSpeed} km/h in {Math.Round(EndDistance - distance)}m";
            case GoalType.AverageSpeedSection:
                var targetAvgSpeed = SpeedTrapTargetMs(gameTime, ZoneStartDistance);
                return $"AverageSpeed: Target {targetAvgSpeed}km/h, current: {Math.Round(MyMath.MsToKmh(distance / gameTime), 1)}";
            case GoalType.TimeTrial:
                var targetTime = TimeTrialTargetSec(gameTime, ZoneStartDistance);
                return $"Target {Math.Round(targetTime)} sec, remaining: {Math.Round(targetTime - gameTime, 2)} sec, distance remaining {EndDistance} m";
        }

        return null;
    }

    // forumla: start at 50km/h -> 150km/h at the end
    private static float SpeedTrapTargetMs(double gameTime, float distance) => (50 + distance / (100 * 1000) * 100) / 3.6f;
    // forumla: start at 50km/h -> 100km/h at the end
    private static float AverageSpeedTargetMs(double gameTime, float distance) => (50 + distance / (100 * 1000) * 50) / 3.6f;
    // formula: start at 60msec/(50km/h) -> 100/sec
    private float TimeTrialTargetSec(double gameTime, float distance) {
        var distancePerKm = 1 / AverageSpeedTargetMs(gameTime, distance);
        var goalDistance = EndDistance - ZoneStartDistance;

        return goalDistance * distancePerKm;
    }

    private bool SpeedTrapWasSuccessful(double gameTime, Vector3 carLinearVelocity) {
        var target = SpeedTrapTargetMs(gameTime, ZoneStartDistance);
        return target < carLinearVelocity.Length();
    }

    private bool AverageSpeedWasSuccessful(double gameTime, Vector3 carLinearVelocity) {
        var timeDiff = gameTime - GoalStartTime;
        var targetSpeed = AverageSpeedTargetMs(gameTime, ZoneStartDistance);
        var goalDistance = EndDistance - ZoneStartDistance;

        return targetSpeed < goalDistance / timeDiff;
    }

    private bool TimeTrialWasSuccessful(double gameTime, Vector3 carLinearVelocity) {
        var targetTime = TimeTrialTargetSec(gameTime, ZoneStartDistance);

        var timeDiff = gameTime - GoalStartTime;
        return targetTime > timeDiff;
    }
};
