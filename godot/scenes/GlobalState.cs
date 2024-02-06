using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.scenes;

public partial class GlobalState : Node {
    public double Money;
    public CarDetails CarDetails;
    public readonly List<Part> PartsUpgraded = [];
    // TODO some details about the track so it generates the same every time

    private readonly List<RoundResult> _roundResults = [];
    public IEnumerable<RoundResult> RoundResults => _roundResults;

    public RoundReward RoundReward { get; private set; }
    public RoundGoal RoundGoal { get; private set; }

    public void AddResult(RoundResult result) {
        _roundResults.Add(result);
    }

    public double SecondsToWin(int roundDiff = 0) {
        // calc best possible time on the map
        // TODO hard coded here until the track is fixed
        const float BEST_POSSIBLE_TIME = 10;
        return 15 * Mathf.Exp(-(_roundResults.Count + roundDiff)/40f) + BEST_POSSIBLE_TIME;
    }

    public void Reset() {
        Money = 0;
        CarDetails = null;
        _roundResults.Clear();

        RoundReward = null;
        RoundGoal = null;
    }

    public void SetReward(RoundReward roundReward) => RoundReward = roundReward;

    public void SetGoal(RoundGoal roundGoal) => RoundGoal = roundGoal;
}

public class RoundResult {
    public double Time;
}

public class RoundReward {
    public double Money;
    public int PartCount;
    public int PartChoiceCount = 3;
}

public class RoundGoal {
    public double Time;
}
