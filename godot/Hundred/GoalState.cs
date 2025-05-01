using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public class GoalState(GoalType goal, WorldType roadType, float totalDistance, float goalSegmentLength) {
    public GoalType Type { get; } = goal;
    public WorldType RoadType { get; } = roadType;
    public float TotalDistance { get; } = totalDistance - goalSegmentLength;
    public float GoalSegmentLength { get; } = goalSegmentLength;

    public float ZoneStartDistance { get; set; }
    public double StartTime { get; set; }

    public bool Ready { get; set; } = true;
    public bool EndPlaced { get; set; }
    public bool InProgress { get; set; }
    public bool? IsSuccessful { get; private set; } = null;

    public float EndDistance => TotalDistance + GoalSegmentLength;

    public void StartDistanceIs(float distanceAtPos) {
        ZoneStartDistance = distanceAtPos;
    }

    public void StartHitAt(double totalTimePassed) {
        InProgress = true;
        StartTime = totalTimePassed;
    }

    public bool EndedAt(double gameTime, Vector3 carLinearVelocity) {
        InProgress = false;
        Ready = false;

        IsSuccessful = Type switch {
            GoalType.SpeedTrap => SpeedTrapWasSuccessful(gameTime, carLinearVelocity),
            GoalType.AverageSpeedSection => AverageSpeedWasSuccessful(gameTime),
            GoalType.TimeTrial => (bool?)TimeTrialWasSuccessful(gameTime),
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
                var targetSpeed = Type.GoalValue(gameTime, ZoneStartDistance, GoalSegmentLength);
                return $"SpeedTrap: Hit {Math.Round(MyMath.MsToKmh(targetSpeed))} km/h in {Math.Round(remainingDistance, 2)} km";
            case GoalType.AverageSpeedSection:
                var targetAvgSpeed = Type.GoalValue(gameTime, ZoneStartDistance, GoalSegmentLength);
                return $"AverageSpeed: Target {Math.Round(MyMath.MsToKmh(targetAvgSpeed))}km/h, current: {Math.Round(MyMath.MsToKmh(distance / gameTime), 1)}";
            case GoalType.TimeTrial:
                var targetTime = Type.GoalValue(gameTime, ZoneStartDistance, GoalSegmentLength);
                var timeDiff = gameTime - StartTime;
                return $"Target {Math.Round(targetTime)} sec, remaining: {Math.Round(targetTime - timeDiff, 2)} sec, distance remaining {Math.Round(remainingDistance, 2)} km";
        }

        return null;
    }

    private bool SpeedTrapWasSuccessful(double gameTime, Vector3 carLinearVelocity) {
        var target = GoalType.SpeedTrap.GoalValue(gameTime, ZoneStartDistance, GoalSegmentLength);
        return target < carLinearVelocity.Length();
    }

    private bool AverageSpeedWasSuccessful(double gameTime) {
        var timeDiff = gameTime - StartTime;
        var targetSpeed = GoalType.AverageSpeedSection.GoalValue(gameTime, ZoneStartDistance, GoalSegmentLength);
        var goalDistance = GoalSegmentLength - ZoneStartDistance;

        return targetSpeed < goalDistance / timeDiff;
    }

    private bool TimeTrialWasSuccessful(double gameTime) {
        var targetTime = GoalType.TimeTrial.GoalValue(gameTime, ZoneStartDistance, GoalSegmentLength);

        var timeDiff = gameTime - StartTime;
        return targetTime > timeDiff;
    }
};
