using Godot;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot;

public partial class InfiniteRallyGame : Node {

    // Tracks the full game

    private InfiniteRoadManager _roadManager;
    private InfiniteRacingScreen _racingScreen;

    public InfiniteRallyGame() {
    }

    public override void _Ready() {
        _roadManager = new InfiniteRoadManager();
        AddChild(_roadManager);

        _racingScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(InfiniteRacingScreen))).Instantiate<InfiniteRacingScreen>();
        AddChild(_racingScreen);
    }

    public override void _PhysicsProcess(double delta) {
        _roadManager.UpdateCarPos(_racingScreen.GetCarPos());
    }
}
