using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.Procedural;
using System;
using System.Collections.Generic;

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
    public double HighestZoneSpeed { get; private set; }

    public double TargetScore => Type.GoalValue(GlobalZoneStartDistance, ZoneLength);

    public void ZoneStartHit(double totalTimePassed) {
        ZoneActive = true;
        ActualZoneStartTime = totalTimePassed;
    }

    public bool SetSuccessful(double gameTime, Vector3 carLinearVelocity) {
        ZoneActive = false;

        ZoneWon = Type switch {
            GoalType.SpeedTrap => TargetScore < carLinearVelocity.Length(),

            // TODO this doesn't work
            GoalType.AverageSpeedSection => TargetScore < (GlobalStartDistance + ZoneLength - ActualZoneStartDistance) / (gameTime - ActualZoneStartTime),
            GoalType.TimeTrial => TargetScore > (gameTime - ActualZoneStartTime),
            GoalType.MinimumSpeed => TimeSpentBelowTargetSpeed < 5, // TODO
            GoalType.SingularSpeed => TargetScore < HighestZoneSpeed,
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

            HighestZoneSpeed = Mathf.Max(carLinearVelocity, HighestZoneSpeed);
        }
    }

    public string Description() {
        var value = TargetScore;
        if (Type.TargetIsSpeed()) {
            value = MyMath.MsToKmh(value);
        }

        return string.Format(Type.GetDescriptionFormat(), value);
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

        var formatString = Type.GetActiveDetailFormat();

        var args = new List<object>();
        switch (Type) {
            case GoalType.Nothing:
                break;
            case GoalType.SpeedTrap:
                args.Add(MyMath.MsToKmh(TargetScore));
                args.Add(remainingDistance);
                break;
            case GoalType.AverageSpeedSection:
                args.Add(MyMath.MsToKmh(TargetScore));
                args.Add(MyMath.MsToKmh(distance / gameTime));
                break;
            case GoalType.TimeTrial:
                args.Add(TargetScore - (gameTime - ActualZoneStartTime));
                args.Add(remainingDistance);
                break;
            case GoalType.MinimumSpeed:
                args.Add(MyMath.MsToKmh(TargetScore));
                args.Add(TimeSpentBelowTargetSpeed);
                break;
            case GoalType.SingularSpeed:
                args.Add(MyMath.MsToKmh(TargetScore));
                args.Add(MyMath.MsToKmh(HighestZoneSpeed));
                break;
        }

        return string.Format(formatString, args.ToArray());
    }
};
