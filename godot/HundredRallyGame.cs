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
    private HundredRacingScene _racingScene;
    private HundredUI _ui;

    public HundredRallyGame() {
    }

    public override void _Ready() {
        _roadManager = new InfiniteRoadManager();
        AddChild(_roadManager);

        _racingScene = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRacingScene))).Instantiate<HundredRacingScene>();
        AddChild(_racingScene);

        _ui = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUI))).Instantiate<HundredUI>();
        AddChild(_ui);
    }


    public override void _Process(double delta) {
        if (Input.IsKeyPressed(Key.Escape)) {
            // TODO actually pause
            GetTree().ChangeSceneToFile("res://Main.tscn");
            return;
        }

        if (Input.IsKeyPressed(Key.Enter)) {
            // reset back to last road thing
            var pos = _roadManager.LastUpdatePos;
            _racingScene.ResetCarTo(pos);
        }
    }

    public override void _PhysicsProcess(double delta) {
        _roadManager.UpdateCarPos(_racingScene.CarPos);

        _ui.DistanceTravelled = _racingScene.DistanceTravelled;
        _ui.SpeedKMH = _racingScene.CarLinearVelocity.Length() * 3.6f;
    }
}
