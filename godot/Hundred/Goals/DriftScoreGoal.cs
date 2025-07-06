using System;
using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.Hundred.Goals;

public partial class DriftScoreGoal(float startDistance, float timeoutTime) : GoalState(startDistance, timeoutTime) {
    public override GoalType Type => GoalType.DriftScore;

    private float _driftTotal;
    private float _currentDrift;
    private float _lastDrift;

    // 2000 then up to 20000 by the end
    public override double TargetScore() => MyMath.KmhToMs(2000 + 18000 * (StartDistance / (100 * 1000f)));

    protected override bool CheckSuccessful(double gameTime, float carSpeed) => _driftTotal >= TargetScore();

    protected override string Progress(double gameTime, float currentDistance, float carLinearVelocity) {
        return $"Accumulate a drift score of {Mathf.Round(TargetScore())}, current: {Mathf.Round(_driftTotal)}";
    }

    public override void _Ready() {

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        state.CarAnyCollision += () => _currentDrift = 0;
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        var state = GetNode<GlobalState>("/root/GlobalState");
        if (state.PlayerCar.IsDrifting()) {
            _currentDrift += (float)state.PlayerCar.DriftFrameAmount(delta);
        } else {
            if (_currentDrift > 0) {
                _lastDrift = _currentDrift;
                _driftTotal += _currentDrift;
            }
            _currentDrift = 0;
        }
    }
}
