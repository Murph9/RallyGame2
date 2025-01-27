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
using System.Data;

namespace murph9.RallyGame2.godot;

internal readonly record struct RivalRace(Car Rival, float StartDistance, float RaceDistance, bool CheckpointSent);
internal class HundredGameState {
    public double TotalTimePassed { get; set; }
    public float NextDistanceMilestone { get; set; } = 100; // in meters
    public RivalRace? RivalRaceDetails { get; set; }
}

public partial class HundredRallyGame : Node {

    // The manager of the game

    private readonly HundredGameState _state = new();

    private InfiniteRoadManager _roadManager;
    private HundredRacingScene _racingScene;
    private HundredUI _ui;

    private Checkpoint _currentStop;

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
        // update state
        _state.TotalTimePassed += delta;


        _ui.TotalTime = _state.TotalTimePassed;

        if (Input.IsActionJustPressed("menu_back")) {
            // TODO actually pause
            GetTree().ChangeSceneToFile("res://Main.tscn");
            return;
        }

        if (Input.IsActionJustPressed("car_reset")) {
            // reset back to last road thing
            var pos = _roadManager.GetPassedCheckpoint(_racingScene.PlayerCarPos);
            _racingScene.ResetCarTo(pos);
        }
    }

    public override void _PhysicsProcess(double delta) {
        _ui.DistanceTravelled = _racingScene.PlayerDistanceTravelled;
        _ui.CurrentSpeedKMH = _racingScene.PlayerCarLinearVelocity.Length() * 3.6f;

        if (_racingScene.PlayerDistanceTravelled > _state.NextDistanceMilestone && !_state.RivalRaceDetails.HasValue) {
            GD.Print("Queuing piece because of next trigger " + _state.NextDistanceMilestone);
            if (_state.NextDistanceMilestone == 100) {
                _state.NextDistanceMilestone = 500;
            } else {
                _state.NextDistanceMilestone += 500;
            }

            _roadManager.TriggerStop();
        }

        var rival = _state.RivalRaceDetails;

        if (rival.HasValue && !rival.Value.CheckpointSent
                && rival.Value.StartDistance + rival.Value.RaceDistance < _racingScene.PlayerDistanceTravelled) {
            GD.Print("Triggering race end because: " + (rival.Value.StartDistance + rival.Value.RaceDistance) + "<" + _racingScene.PlayerDistanceTravelled);
            _roadManager.TriggerRaceEnd();
            _state.RivalRaceDetails = rival.Value with { CheckpointSent = true };
        }
    }

    private void StopTriggeredAt(Transform3D transform) {
        _currentStop = Checkpoint.AsBox(transform, Vector3.One * 20, new Color(0, 0, 0, 0.4f)); // should be invisible
        _currentStop.ThingEntered += PlayerHitStop;
        AddChild(_currentStop);
    }

    private void PlayerHitStop(Node3D node) {
        if (node.GetParent() is not Car) return;

        if (_state.RivalRaceDetails == null) {
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

                _state.RivalRaceDetails = new RivalRace(rival, _racingScene.PlayerDistanceTravelled, 100, false);
                _ui.RivalDetails = "Rival race started, dist: " + _state.RivalRaceDetails.Value.RaceDistance + "m";
            }
        } else {
            // oh the race is over?
            bool raceOver = false;
            if (_racingScene.IsMainCar(node)) {
                CallDeferred(MethodName.ResetStop);
                _ui.RivalDetails = "Nice win";
                raceOver = true;
            } else if (node == _state.RivalRaceDetails.Value.Rival.RigidBody) {
                CallDeferred(MethodName.ResetStop);
                _ui.RivalDetails = "You lost";
                raceOver = true;
            }

            if (raceOver) {
                RemoveChild(_state.RivalRaceDetails.Value.Rival);
                _state.RivalRaceDetails = null;
            }
        }
    }

    private void ResetStop() {
        RemoveChild(_currentStop);
        _currentStop = null;
    }
}
