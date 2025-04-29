using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public class GoalState(GoalType goal, WorldType roadType, float totalDistance, float goalLength) {
    public GoalType Type { get; } = goal;
    public WorldType RoadType { get; } = roadType;
    public float TotalDistance { get; } = totalDistance - goalLength;
    public float Length { get; } = goalLength;

    public float ZoneStartDistance { get; set; }
    public double StartTime { get; set; }

    public bool Ready { get; set; } = true;
    public bool EndPlaced { get; set; }
    public bool InProgress { get; set; }
    public bool? IsSuccessful { get; private set; } = null;

    public float EndDistance => TotalDistance + Length;

    public void StartDistanceIs(float distanceAtPos) {
        ZoneStartDistance = distanceAtPos;
    }

    public void StartHitAt(double totalTimePassed) {
        InProgress = true;
        StartTime = totalTimePassed;
    }

    public bool EndedAt(double gameTime, float endDistance, Vector3 carLinearVelocity) {
        InProgress = false;
        Ready = false;

        IsSuccessful = Type switch {
            GoalType.SpeedTrap => SpeedTrapWasSuccessful(gameTime, carLinearVelocity),
            GoalType.AverageSpeedSection => AverageSpeedWasSuccessful(gameTime, carLinearVelocity),
            GoalType.TimeTrial => (bool?)TimeTrialWasSuccessful(gameTime, carLinearVelocity),
            GoalType.Nothing => true,
            _ => throw new Exception("Unknown type " + Type),
        };

        return IsSuccessful ?? false;
    }

    public string ProgressString(double gameTime, float distance) {
        if (IsSuccessful.HasValue) {
            var successString = IsSuccessful.Value ? "" : "not ";
            return $"Goal {Type}: Was {successString}successful";
        }

        if (!InProgress) {
            var text = $"Goal {Type} starts at {Math.Round(TotalDistance / 1000f, 1)} km";
            if (ZoneStartDistance > TotalDistance) // its set show the distance left
                text += $" in {Math.Round(ZoneStartDistance - distance)}m";
            return text;
        }

        var remainingDistance = Math.Round(EndDistance - distance) / 1000;

        switch (Type) {
            case GoalType.SpeedTrap:
                var targetSpeed = SpeedTrapTargetMs(gameTime, ZoneStartDistance);
                return $"SpeedTrap: Hit {Math.Round(MyMath.MsToKmh(targetSpeed))} km/h in {remainingDistance} km";
            case GoalType.AverageSpeedSection:
                var targetAvgSpeed = SpeedTrapTargetMs(gameTime, ZoneStartDistance);
                return $"AverageSpeed: Target {Math.Round(MyMath.MsToKmh(targetAvgSpeed))}km/h, current: {Math.Round(MyMath.MsToKmh(distance / gameTime), 1)}";
            case GoalType.TimeTrial:
                var targetTime = TimeTrialTargetSec(gameTime, ZoneStartDistance);
                var timeDiff = gameTime - StartTime;
                return $"Target {Math.Round(targetTime)} sec, remaining: {Math.Round(targetTime - timeDiff, 2)} sec, distance remaining {remainingDistance} km";
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
        return Length * distancePerKm;
    }

    private bool SpeedTrapWasSuccessful(double gameTime, Vector3 carLinearVelocity) {
        var target = SpeedTrapTargetMs(gameTime, ZoneStartDistance);
        return target < carLinearVelocity.Length();
    }

    private bool AverageSpeedWasSuccessful(double gameTime, Vector3 carLinearVelocity) {
        var timeDiff = gameTime - StartTime;
        var targetSpeed = AverageSpeedTargetMs(gameTime, ZoneStartDistance);
        var goalDistance = Length - ZoneStartDistance;

        return targetSpeed < goalDistance / timeDiff;
    }

    private bool TimeTrialWasSuccessful(double gameTime, Vector3 carLinearVelocity) {
        var targetTime = TimeTrialTargetSec(gameTime, ZoneStartDistance);

        var timeDiff = gameTime - StartTime;
        return targetTime > timeDiff;
    }
};
