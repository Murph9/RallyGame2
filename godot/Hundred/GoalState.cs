using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred;

public class GoalState(GoalType goal, float startDistance, float timeoutTime) { // TODO abstract class
    public GoalType Type { get; } = goal;

    public float TimeoutTime { get; } = timeoutTime;
    public float StartDistance { get; } = startDistance;

    // goal specific tracking:
    public double TimeSpentBelowTargetSpeed { get; private set; }
    public double HighestZoneSpeed { get; private set; }

    public double GoalActiveFor { get; private set; }
    public bool? Successful { get; private set; } = null;

    public double TargetScore => Type.GoalValue(StartDistance);

    public bool? CheckSuccessful(double gameTime, float carSpeed) {
        if (gameTime > TimeoutTime) {
            Successful = false;
        } else {
            var successful = Type switch {
                GoalType.SpeedTrap => GoalActiveFor > (TimeoutTime - 1) && TargetScore < carSpeed,
                GoalType.MinimumSpeed => GoalActiveFor > (TimeoutTime - 1) && TimeSpentBelowTargetSpeed < 5,
                GoalType.SingularSpeed => TargetScore < HighestZoneSpeed,
                GoalType.Nothing => true,
                _ => throw new Exception("Unknown type " + Type),
            };

            if (successful) {
                Successful = true;
            }
        }

        return Successful;
    }

    public void _PhysicsProcess(double delta, float carLinearVelocity) {
        GoalActiveFor += delta;

        if (Type == GoalType.MinimumSpeed && carLinearVelocity < TargetScore) {
            TimeSpentBelowTargetSpeed += delta;
        }

        if (Type == GoalType.SingularSpeed) {
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

    public string ProgressString(double gameTime, float currentDistance, float carLinearVelocity) {
        if (Successful.HasValue && Successful.Value) {
            var successString = Successful.Value ? "" : "not ";
            return $"Goal {Type}: Was {successString}successful";
        }

        var formatString = Type.GetActiveDetailFormat();

        var args = new List<object>();
        switch (Type) {
            case GoalType.Nothing:
                break;
            case GoalType.SpeedTrap:
                args.Add(MyMath.MsToKmh(TargetScore));
                args.Add(TimeoutTime - gameTime);
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
