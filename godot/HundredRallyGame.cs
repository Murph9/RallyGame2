using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Hundred;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.DebugGUI;
using System;

namespace murph9.RallyGame2.godot;

internal readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);

public partial class HundredRallyGame : Node {

    // The manager of the game

    private InfiniteRoadManager _roadManager;
    private HundredRacingScene _racingScene;
    private HundredUI _ui;

    private Checkpoint _currentStop;

    private RivalRace? _rivalRaceDetails;

    private float _nextDistanceMilestone = 100; // in meters

    public HundredRallyGame() {
    }

    public override void _Ready() {
        DebugGUI.IsActive = false; // TODO

        _roadManager = new InfiniteRoadManager();
        _roadManager.StopCreated += StopTriggeredAt;
        AddChild(_roadManager);

        _racingScene = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRacingScene))).Instantiate<HundredRacingScene>();
        _racingScene.InitialPosition = _roadManager.GetInitialSpawn();
        AddChild(_racingScene);

        _ui = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUI))).Instantiate<HundredUI>();
        AddChild(_ui);
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("menu_back")) {
            // TODO actually pause
            GetTree().ChangeSceneToFile("res://Main.tscn");
            return;
        }

        if (Input.IsActionJustPressed("car_reset")) {
            // reset back to last road thing
            var pos = _roadManager.GetPassedCheckpoint(_racingScene.CarPos);
            _racingScene.ResetCarTo(pos);
        }
    }

    public override void _PhysicsProcess(double delta) {
        _ui.DistanceTravelled = _racingScene.DistanceTravelled;
        _ui.SpeedKMH = _racingScene.CarLinearVelocity.Length() * 3.6f;

        if (_racingScene.DistanceTravelled > _nextDistanceMilestone && !_rivalRaceDetails.HasValue) {
            GD.Print("Queuing piece because of next trigger " + _nextDistanceMilestone);
            if (_nextDistanceMilestone == 100) {
                _nextDistanceMilestone = 500;
            } else {
                _nextDistanceMilestone += 500;
            }

            _roadManager.TriggerStop();
        }

        if (_rivalRaceDetails.HasValue && !_rivalRaceDetails.Value.CheckpointSent
                && _rivalRaceDetails.Value.StartDistance + _rivalRaceDetails.Value.RaceDistance < _racingScene.DistanceTravelled) {
            GD.Print("Triggering race end because: " + (_rivalRaceDetails.Value.StartDistance + _rivalRaceDetails.Value.RaceDistance) + "<" + _racingScene.DistanceTravelled);
            _roadManager.TriggerRaceEnd();
            _rivalRaceDetails = _rivalRaceDetails.Value with { CheckpointSent = true };
        }
    }

    private void StopTriggeredAt(Transform3D transform) {
        _currentStop = Checkpoint.AsBox(transform, Vector3.One * 20, new Color(0, 0, 0, 0.4f)); // should be invisible
        _currentStop.ThingEntered += PlayerHitStop;
        AddChild(_currentStop);
    }

    private void PlayerHitStop(Node3D node) {
        if (node.GetParent() is not Car) return;

        if (_rivalRaceDetails == null) {
            if (_racingScene.IsMainCar(node)) {
                // race start checkpoint triggered
                CallDeferred(MethodName.ResetStop);

                GD.Print("Spawning Rival");
                // create car in the center of the road which we are going to race against
                var ai = new TrafficAiInputs(_roadManager, false);
                var rival = new Car(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY), ai, _currentStop.GlobalTransform);
                rival.RigidBody.Transform = _currentStop.GlobalTransform;
                rival.RigidBody.LinearVelocity = _currentStop.GlobalTransform.Basis * Vector3.Back * ai.TargetSpeed;
                AddChild(rival);

                _rivalRaceDetails = new RivalRace(rival, _racingScene.DistanceTravelled, 100, false);
                _ui.RivalDetails = "Rival race started, dist: " + _rivalRaceDetails.Value.RaceDistance + "m";
            }
        } else {
            // oh the race is over?
            bool raceOver = false;
            if (_racingScene.IsMainCar(node)) {
                CallDeferred(MethodName.ResetStop);
                _ui.RivalDetails = "Nice win";
                raceOver = true;
            } else if (node == _rivalRaceDetails.Value.Rival.RigidBody) {
                CallDeferred(MethodName.ResetStop);
                _ui.RivalDetails = "You lost";
                raceOver = true;
            }

            if (raceOver) {
                RemoveChild(_rivalRaceDetails.Value.Rival);
                _rivalRaceDetails = null;
            }
        }
    }

    private void ResetStop() {
        RemoveChild(_currentStop);
        _currentStop = null;
    }
}
