using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Hundred;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.DebugGUI;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class HundredRallyGame : Node {

    // The manager of the game

    private InfiniteRoadManager _roadManager;
    private HundredRacingScene _racingScene;
    private HundredUI _ui;

    private Checkpoint _raceFinishLine;
    private Checkpoint _currentStop;

    public HundredRallyGame() {
    }

    public override void _Ready() {
        DebugGUI.IsActive = false; // TODO

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (state.CarDetails != null) {
            state.Reset(); // reset if its not been set
        }
        state.CarDetails = CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY);

        _roadManager = new InfiniteRoadManager();
        _roadManager.ShopPlaced += ShopTriggeredAt;
        AddChild(_roadManager);

        _racingScene = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRacingScene))).Instantiate<HundredRacingScene>();
        _racingScene.InitialPosition = _roadManager.GetInitialSpawn();
        AddChild(_racingScene);

        _ui = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUI))).Instantiate<HundredUI>();
        AddChild(_ui);
    }

    public override void _Process(double delta) {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        // update state
        state.TotalTimePassed += delta;
        _ui.TotalTime = state.TotalTimePassed;

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
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        _ui.DistanceTravelled = _racingScene.PlayerDistanceTravelled;
        _ui.CurrentSpeedKMH = _racingScene.PlayerCarLinearVelocity.Length() * 3.6f;

        if (_racingScene.PlayerDistanceTravelled > state.NextDistanceMilestone) {
            GD.Print("Queuing shop because of next trigger " + state.NextDistanceMilestone);
            if (state.NextDistanceMilestone == 100) {
                state.NextDistanceMilestone = 500;
            } else {
                state.NextDistanceMilestone += 500;
            }

            _roadManager.TriggerShop();
        }

        if (state.RivalRaceDetails == null) {
            var closestRival = _roadManager.GetClosestOpponent(_racingScene.PlayerCarPos);
            if (closestRival != null && closestRival.RigidBody.GlobalPosition.DistanceTo(_racingScene.PlayerCarPos) < 10 && (closestRival.RigidBody.LinearVelocity - _racingScene.PlayerCarLinearVelocity).Length() < 3) {
                GD.Print("Challenged rival");
                state.RivalRaceDetails = new RivalRace(closestRival, _racingScene.PlayerDistanceTravelled, 100, false);
                _ui.RivalDetails = "Rival race started, dist: " + state.RivalRaceDetails.Value.RaceDistance + "m";
            }
        } else {
            var rival = state.RivalRaceDetails;
            if (!rival.Value.CheckpointSent && rival.Value.StartDistance + rival.Value.RaceDistance < _racingScene.PlayerDistanceTravelled) {
                GD.Print("Triggering race end because: " + (rival.Value.StartDistance + rival.Value.RaceDistance) + "<" + _racingScene.PlayerDistanceTravelled);
                var checkpoints = _roadManager.GetNextCheckpoints(_racingScene.PlayerCarPos, false, 0);
                var checkpoint = checkpoints.Skip(10).FirstOrDefault();
                if (checkpoint == default) {
                    checkpoint = checkpoints.Last();
                }

                state.RivalRaceDetails = rival.Value with { CheckpointSent = true };
                // TODO spawn checkpoint there
                _raceFinishLine = Checkpoint.AsBox(checkpoint, Vector3.One * 20, new Color(1, 1, 1, 0.7f)); // should be invisible-ish
                AddChild(_raceFinishLine);
                _raceFinishLine.ThingEntered += (Node3D node) => {
                    if (node.GetParent() is not Car) return;

                    if (state.RivalRaceDetails != null) {
                        // oh the race is over?
                        if (_racingScene.IsMainCar(node)) {
                            CallDeferred(MethodName.ResetStop);
                            _ui.RivalDetails = "Nice win";
                        }
                        if (node == state.RivalRaceDetails.Value.Rival.RigidBody) {
                            CallDeferred(MethodName.ResetStop);
                            _ui.RivalDetails = "You lost";
                        }
                    }
                };
            }
        }
    }

    private void ShopTriggeredAt(Transform3D transform) {
        _currentStop = Checkpoint.AsBox(transform, Vector3.One * 20, new Color(0, 0, 0, 0.4f)); // should be invisible
        AddChild(_currentStop);
        _currentStop.ThingEntered += (Node3D node) => {
            if (node.GetParent() is not Car) return;

            // TODO if shop
        };
    }

    private void ResetStop() {
        RemoveChild(_raceFinishLine);
        _raceFinishLine = null;

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        state.RivalRaceDetails = null;

        // no need to delete the rival, its not like it should disappear
    }
}
