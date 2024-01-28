using System.Collections.Generic;
using Godot;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot.scenes;

public partial class GlobalState : Node {
    public double Money;
    public int Rounds;
    public CarDetails CarDetails;
    // TODO some details about the track so it generates the same every time

    public readonly List<RoundResult> RoundResults = [];

    public int SecondsToWin() => 60 - Rounds;
}

public class RoundResult {
    public bool Successful;
    public double Time;
}
