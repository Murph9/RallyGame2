using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class TrafficCollisionsGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    public override GoalType Type => GoalType.TrafficCollisions;

    private readonly HashSet<Car> TrafficCollisions = [];

    public override double TargetScore() => 6;

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => TrafficCollisions.Count >= TargetScore();

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) {
        return $"Hit {TargetScore()} traffic, current: {TrafficCollisions.Count}";
    }

    public override void _Ready() {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        state.TrafficCollision += ProcessTrafficCollision;
    }

    private void ProcessTrafficCollision(Car other, Vector3 diff) {
        TrafficCollisions.Add(other);
    }
}
