using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Hundred;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.Debug3D;
using murph9.RallyGame2.godot.Utilities.DebugGUI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace murph9.RallyGame2.godot;

public partial class HundredRallyGame : Node {

    // The manager of the game

    private InfiniteRoadManager _roadManager;
    private HundredRacingScene _racingScene;
    private HundredUI _ui;

    private LineDebug3D _playerLineDebug3D = new();

    private bool _paused = false;

    public HundredRallyGame() {
#if DEBUG
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
#endif
    }

    public override void _Ready() {
        DebugGUI.IsActive = false; // TODO

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (state.CarDetails != null) {
            state.Reset(); // reset if its not been set
        }
        state.SetCarDetails(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY));

        _roadManager = new InfiniteRoadManager(300, World.Procedural.WorldType.Simple2);
        _roadManager.RoadNextPoint += RoadPlacedAt;
        AddChild(_roadManager);

        _racingScene = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRacingScene))).Instantiate<HundredRacingScene>();
        _racingScene.InitialPosition = _roadManager.GetInitialSpawn();
        AddChild(_racingScene);

        _ui = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUI))).Instantiate<HundredUI>();
        AddChild(_ui);

        _playerLineDebug3D.Colour = Colors.DarkGoldenrod;
        AddChild(_playerLineDebug3D);
    }

    public override void _Process(double delta) {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        // update state
        if (!_paused) {
            state.AddTotalTimePassed(delta);

            if (Input.IsActionJustPressed("menu_back")) {
                ShowPause();
            }

            if (Input.IsActionJustPressed("car_reset")) {
                var pos = _roadManager.GetPassedCheckpoint(_racingScene.PlayerCarPos);
                // reset back to last road thing
                _racingScene.ResetCarTo(pos);
            }

            if (state.ShopStoppedTimer > state.ShopStoppedTriggerAmount && state.ShopCooldownTimer < 0) {
                CallDeferred(MethodName.ShowShop);
            }

#if DEBUG
            if (Input.IsKeyPressed(Key.Key8)) {
                CallDeferred(MethodName.ShowShop);
            }
            if (Input.IsKeyPressed(Key.Key9)) {
                CallDeferred(MethodName.ShowRelicShop, false);
            }
#endif
        }

        _playerLineDebug3D.Start = _racingScene.PlayerCarPos;
        _playerLineDebug3D.End = _roadManager.GetNextCheckpoint(_racingScene.PlayerCarPos).Origin;

        var newDistanceTravelled = _roadManager.TotalDistanceFromCheckpoint(_playerLineDebug3D.End);
        state.SetDistanceTravelled(newDistanceTravelled);
    }

    public override void _PhysicsProcess(double delta) {
        if (_paused) return;

        // update state object for this physics frame
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        var currentPlayerSpeed = _racingScene.PlayerCarLinearVelocity.Length();
        state.SetCurrentSpeedMs(currentPlayerSpeed);

        if (currentPlayerSpeed < 1 && !_racingScene.PlayerIsAccelerating)
            state.ShopStoppedTimer += delta;
        else {
            state.ShopStoppedTimer = 0;
            state.ShopCooldownTimer -= delta; // TODO you need to accel for 5 seconds for this
        }

        // check the current race states
        foreach (var rival in state.RivalRaceDetails) {
            if (!rival.CheckpointSet && rival.StartDistance + rival.RaceDistance < _racingScene.PlayerDistanceTravelled) {
                // TODO trigger the race end from the road placed event so its as close to the total distance as possible
                GD.Print("Triggering race end because: " + (rival.StartDistance + rival.RaceDistance) + "<" + _racingScene.PlayerDistanceTravelled);
                var checkpoints = _roadManager.GetNextCheckpoints(_racingScene.PlayerCarPos, false, 0);
                var checkpoint = checkpoints.Skip(10).FirstOrDefault();
                if (checkpoint == default) {
                    checkpoint = checkpoints.Last();
                }

                rival.CheckpointSet = true;

                // spawn checkpoint there
                CreateCheckpoint(checkpoint, node => {
                    if (state.RivalRaceDetails != null) {
                        // oh the race is over?
                        if (_racingScene.IsMainCar(node)) {
                            CallDeferred(MethodName.ResetRivalRace, rival.Rival);
                            state.RivalRaceFinished(rival.Rival, true, "Nice win", (float)state.RivalWinBaseAmount);
                            return true;
                        } else if (node == rival.Rival.RigidBody) {
                            CallDeferred(MethodName.ResetRivalRace, rival.Rival);
                            state.RivalRaceFinished(rival.Rival, false, "You Lost", 0);
                            return true;
                        }
                    }
                    return false;
                });
            }
        }

        var closestRival = _roadManager.GetClosestOpponent(_racingScene.PlayerCarPos);
        if (closestRival != null && !state.RivalRaceDetails.Any(x => x.Rival == closestRival)) {
            if (closestRival.RigidBody.GlobalPosition.DistanceTo(_racingScene.PlayerCarPos) < 10 && (closestRival.RigidBody.LinearVelocity - _racingScene.PlayerCarLinearVelocity).Length() < state.RivalRaceSpeedDiff) {
                state.RivalStarted(new RivalRace(closestRival, _racingScene.PlayerDistanceTravelled, state.RivalRaceDistance), "Rival race started, dist: " + state.RivalRaceDistance + "m");

                var newAi = new RacingAiInputs(_roadManager) {
                    RoadWidth = 10
                };
                closestRival.ChangeInputsTo(newAi);
            }
        }
    }

    private void RemoveNode(Node node) {
        RemoveChild(node);
    }

    private void RoadPlacedAt(float distanceAtPos, Transform3D transform) {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        // create the trigger area for the start of the goal zone
        if (state.Goal.ActualZoneStartDistance == 0 && state.Goal.GlobalZoneStartDistance < distanceAtPos) {
            state.Goal.ActualZoneStartDistance = distanceAtPos;

            CreateCheckpoint(transform, (node) => {
                if (!_racingScene.IsMainCar(node)) return false;
                GD.Print("Starting the goal: " + state.Goal.Type);

                state.Goal.ZoneStartHit(state.TotalTimePassed);
                return true;
            });
        }

        // create the trigger for the end of the zone
        if (!state.Goal.ZoneActive && state.Goal.GlobalEndDistance < distanceAtPos) {
            GD.Print("Creating end trigger for the goal: " + state.Goal.Type);
            _roadManager.StopAfter(state.Goal.GlobalEndDistance); // stop roads from being created after this point

            CreateCheckpoint(transform, (node) => {
                if (!_racingScene.IsMainCar(node)) return false;

                state.Goal.SetSuccessful(state.TotalTimePassed, _racingScene.PlayerCarLinearVelocity);

                CallDeferred(MethodName.ShowRelicShop, true);
                return true;
            });
        }
    }

    private void ResetRivalRace(Car rival) {
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        // change ai to stop then revert to the current one so they still exist
        RevertRivalAi(rival, rival.Inputs);
        rival.ChangeInputsTo(new StopAiInputs(_roadManager));

        state.RivalStopped(rival);

        // no need to delete the rival, its not like it should disappear
    }
    private static async void RevertRivalAi(Car rival, ICarInputs oldAi) {
        await Task.Delay(TimeSpan.FromSeconds(5));
        rival.ChangeInputsTo(oldAi);
    }

    private void ShowRelicShop(bool showShop) {
        SetPauseState(true);

        var relics = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRelicScreen))).Instantiate<HundredRelicScreen>();
        relics.Closed += () => {
            SetPauseState(false);
            CallDeferred(MethodName.RemoveNode, relics);

            // check if the current goal is not active
            var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
            if (showShop) {
                CallDeferred(MethodName.ShowGoalSelect);
            }
        };
        AddChild(relics);
    }

    private void ShowShop() {
        SetPauseState(true);

        var upgrade = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUpgradeScreen))).Instantiate<HundredUpgradeScreen>();
        upgrade.Closed += (carChanged) => {
            var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
            if (carChanged) {
                var changeDetails = upgrade.GetChangedDetails();

                state.SetCarDetails(changeDetails.Item1);
                state.AddMoney(-changeDetails.Item2);
            }

            SetPauseState(false);
            state.ShopCooldownTimer = state.ShopCooldownTriggerAmount;
            _racingScene.ReplaceCarWithState();
            CallDeferred(MethodName.RemoveNode, upgrade);
        };
        AddChild(upgrade);
    }

    private void ShowGoalSelect() {
        SetPauseState(true);

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        var goalSelect = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredGoalSelectScreen))).Instantiate<HundredGoalSelectScreen>();
        goalSelect.Closed += () => {
            SetPauseState(false);
            CallDeferred(MethodName.RemoveNode, goalSelect);

            _roadManager.UpdateWorldType(state.Goal.RoadType);
            _roadManager.StopAfter(0); // and continue placing new road
        };
        AddChild(goalSelect);
    }

    private void SetPauseState(bool paused) {
        _paused = paused;
        if (paused)
            _racingScene.StopDriving();
        else
            _racingScene.StartDriving();
        _roadManager.SetPaused(paused);
    }

    private void ShowPause() {
        SetPauseState(true);

        var pauseScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredPauseScreen))).Instantiate<HundredPauseScreen>();
        pauseScreen.Resume += () => {
            SetPauseState(false);
            CallDeferred(MethodName.RemoveNode, pauseScreen);
        };
        pauseScreen.Quit += () => {
            GetTree().ChangeSceneToFile("res://Main.tscn");
        };
        AddChild(pauseScreen);
    }

    private void CreateCheckpoint(Transform3D transform, Func<Node3D, bool> action) {
        var checkpoint = Checkpoint.AsBox(transform, Vector3.One * 20, new Color(1, 1, 1, 0.7f)); // should be invisible-ish
        AddChild(checkpoint);
        checkpoint.ThingEntered += node => {
            if (node.GetParent() is not Car) return;
            if (action(node))
                CallDeferred(MethodName.RemoveNode, checkpoint);
        };
    }
}
