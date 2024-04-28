using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.scenes;

public partial class GlobalState : Node {
    public readonly List<Part> PartsUpgraded = [];

    private readonly List<RoundResult> _roundResults = [];
    public IEnumerable<RoundResult> RoundResults => _roundResults;

    public double Money;
    public CarDetails CarDetails;
    public RoundReward RoundReward { get; private set; }
    public RoundGoal RoundGoal { get; private set; }
    // TODO some details about the track so it generates the same every time
    public WorldDetails WorldDetails { get; private set; }

    public void AddResult(RoundResult result) {
        _roundResults.Add(result);
    }

    public double SecondsToWin(int roundDiff = 0) {
        return 15 * Mathf.Exp(-(_roundResults.Count + roundDiff)/40f) + WorldDetails.ExpectedFinishTime;
    }

    public void Reset() {
        Money = 0;
        CarDetails = null;
        _roundResults.Clear();

        RoundReward = null;
        RoundGoal = null;
        WorldDetails = null;
    }

    public void SetReward(RoundReward roundReward) => RoundReward = roundReward;

    public void SetGoal(RoundGoal roundGoal) => RoundGoal = roundGoal;

    public void SetWorldDetails(WorldDetails worldDetails) => WorldDetails = worldDetails;
}

public record RoundResult {
    public double Time;
}

public record RoundReward {
    public double Money;
    public int PartCount;
    public int PartChoiceCount = 3;
}

public record RoundGoal {
    public double Time;
}

public record WorldDetails {
    public double ExpectedFinishTime => RoadManager.ExpectedFinishTime;
    public RoadManager RoadManager;
}
