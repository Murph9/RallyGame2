using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.scenes;

public partial class GlobalState : Node {
    public double Money;
    public CarDetails CarDetails;
    // TODO some details about the track so it generates the same every time

    private readonly List<RoundResult> _roundResults = [];
    public IEnumerable<RoundResult> RoundResults => _roundResults;
    public void AddResult(RoundResult result) {
        _roundResults.Add(result);
    }

    public double SecondsToWin(int roundDiff = 0) => 60 - (_roundResults.Count + roundDiff);

    public void Reset() {
        Money = 0;
        CarDetails = null;
        _roundResults.Clear();
    }
}

public class RoundResult {
    public double Time;
}
