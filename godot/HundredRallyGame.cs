using Godot;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Hundred;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot;

public partial class HundredRallyGame : Node {

    // The manager of the game

    private InfiniteRoadManager _roadManager;
    private HundredRacingScene _racingScene;
    private HundredUI _ui;

    private Node3D CheckpointNode;

    public HundredRallyGame() {
    }

    public override void _Ready() {
        _roadManager = new InfiniteRoadManager();
        AddChild(_roadManager);

        _racingScene = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRacingScene))).Instantiate<HundredRacingScene>();
        AddChild(_racingScene);

        _ui = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUI))).Instantiate<HundredUI>();
        AddChild(_ui);

        CheckpointNode = new Node3D();
        AddChild(CheckpointNode);

        var boxMesh = new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = new Vector3(5, 5, 5)
            },
            Position = new Vector3(0, 0, 0),
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = new Color(0, 1, 1, 0.8f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha
            }
        };
        CheckpointNode.AddChild(boxMesh);
    }


    public override void _Process(double delta) {
        if (Input.IsKeyPressed(Key.Escape)) {
            // TODO actually pause
            GetTree().ChangeSceneToFile("res://Main.tscn");
            return;
        }

        if (Input.IsKeyPressed(Key.Enter)) {
            // reset back to last road thing
            var pos = _roadManager.GetClosestPointTo(_racingScene.CarPos);
            _racingScene.ResetCarTo(pos);
        }

        var nextCheckpoint = _roadManager.GetNextCheckpoint(_racingScene.CarPos, true);
        CheckpointNode.Transform = nextCheckpoint;
    }

    public override void _PhysicsProcess(double delta) {
        _roadManager.UpdateCarPos(_racingScene.CarPos);

        _ui.DistanceTravelled = _racingScene.DistanceTravelled;
        _ui.SpeedKMH = _racingScene.CarLinearVelocity.Length() * 3.6f;
    }
}
