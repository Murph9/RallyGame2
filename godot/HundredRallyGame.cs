using Godot;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Hundred;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot;

public partial class HundredRallyGame : Node {

    // Tracks the full game

    private InfiniteRoadManager _roadManager;
    private HundredRacingScreen _racingScreen;

    private HundredUI _ui;

    public HundredRallyGame() {
    }

    public override void _Ready() {
        _roadManager = new InfiniteRoadManager();
        AddChild(_roadManager);

        _racingScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRacingScreen))).Instantiate<HundredRacingScreen>();
        AddChild(_racingScreen);

        _ui = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUI))).Instantiate<HundredUI>();
        AddChild(_ui);
    }

    public override void _PhysicsProcess(double delta) {
        _roadManager.UpdateCarPos(_racingScreen.CarPos);

        _ui.DistanceTravelled = _racingScreen.DistanceTravelled;
        _ui.SpeedKMH = _racingScreen.CarLinearVelocity.Length() * 3.6f;
    }
}
