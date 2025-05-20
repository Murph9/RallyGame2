using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.Procedural;
using System;

namespace murph9.RallyGame2.godot.Hundred;

public class GoalState(GoalType goal, WorldType roadType, float startDistance, float goalLength, float goalZoneLength) {
    public GoalType Type { get; } = goal;
    public WorldType RoadType { get; } = roadType;

    public float ZoneLength { get; } = goalZoneLength;
    public float FullLength { get; } = goalLength;
    public float GlobalStartDistance { get; } = startDistance;
    public float GlobalZoneStartDistance { get; } = startDistance + goalLength - goalZoneLength;
    public float GlobalEndDistance => GlobalStartDistance + FullLength;

    public float ActualZoneStartDistance { get; set; }
    public double ActualZoneStartTime { get; private set; }

    public bool ZoneActive { get; set; }
    public bool? ZoneWon { get; private set; }

    public double TimeSpentBelowTargetSpeed { get; private set; }

    public double TargetScore => Type.GoalValue(GlobalZoneStartDistance, ZoneLength);

    public void ZoneStartHit(double totalTimePassed) {
        ZoneActive = true;
        ActualZoneStartTime = totalTimePassed;
    }

    public bool SetSuccessful(double gameTime, Vector3 carLinearVelocity) {
        ZoneActive = false;

        ZoneWon = Type switch {
            GoalType.SpeedTrap => TargetScore < carLinearVelocity.Length(),
            GoalType.AverageSpeedSection => TargetScore < (GlobalStartDistance + ZoneLength - ActualZoneStartDistance) / (gameTime - ActualZoneStartTime),
            GoalType.TimeTrial => TargetScore > (gameTime - ActualZoneStartTime),
            GoalType.MinimumSpeed => TimeSpentBelowTargetSpeed > 5, // TODO
            GoalType.Nothing => true,
            _ => throw new Exception("Unknown type " + Type),
        };

        return ZoneWon.Value;
    }

    public void _PhysicsProcess(double delta, float carLinearVelocity) {
        if (ZoneActive) {
            if (carLinearVelocity < TargetScore) {
                TimeSpentBelowTargetSpeed += delta;
            }
        }
    }

    public string ProgressString(double gameTime, float distance, float carLinearVelocity) {
        if (ZoneWon.HasValue) {
            var successString = ZoneWon.Value ? "" : "not ";
            return $"Goal {Type}: Was {successString}successful";
        }

        if (!ZoneActive) {
            var text = $"Goal {Type} starts at {Math.Round(GlobalZoneStartDistance / 1000f, 1)} km";
            if (ActualZoneStartDistance > 0) // its set, so show the distance to the start of it
                text += $" in {Math.Round(ActualZoneStartDistance - distance)}m";
            return text;
        }

        var remainingDistance = Math.Round(GlobalEndDistance - distance) / 1000;

        switch (Type) {
            case GoalType.Nothing:
                return "Nothing to do, Free win";
            case GoalType.SpeedTrap:
                return $"SpeedTrap: Hit {Math.Round(MyMath.MsToKmh(TargetScore))} km/h in {Math.Round(remainingDistance, 2)} km";
            case GoalType.AverageSpeedSection:
                return $"AverageSpeed: Target {Math.Round(MyMath.MsToKmh(TargetScore))}km/h, current: {Math.Round(MyMath.MsToKmh(distance / gameTime), 1)}";
            case GoalType.TimeTrial:
                var timeDiff = gameTime - ActualZoneStartTime;
                return $"Target {Math.Round(TargetScore)} sec, remaining: {Math.Round(TargetScore - timeDiff, 2)} sec, distance remaining {Math.Round(remainingDistance, 2)} km";
            case GoalType.MinimumSpeed:
                return $"Keep above {Math.Round(MyMath.MsToKmh(TargetScore))} km/h, Max time below 5 sec, current {Math.Round(TimeSpentBelowTargetSpeed, 2)} sec";
        }

        return null;
    }
};
