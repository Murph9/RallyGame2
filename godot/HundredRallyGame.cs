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

    private const int ROAD_TYPE_CHANGE_COUNT = 25;

    private InfiniteRoadManager _roadManager;
    private HundredRacingScene _racingScene;
    private HundredUpgradeScreen _upgradeScreen;
    private HundredUI _ui;

    private LineDebug3D _playerLineDebug3D = new();

    private bool _shopCountdownStarted;

    private bool _paused = false;

    public HundredRallyGame() {
#if DEBUG
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
#endif
    }

    public override void _Ready() {
        DebugGUI.IsActive = false;

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

            if (_shopCountdownStarted) {
                state.ShopTimerReduced(delta);
                if (state.ShopResetTimer < 0) {
                    _upgradeScreen = null; // TODO memory leak?

                    state.ShopTimerReset();
                    _shopCountdownStarted = false;
                }
            }

            if (Input.IsActionJustPressed("menu_back")) {
                ShowPause();
            }

            if (Input.IsActionJustPressed("car_reset")) {
                var pos = _roadManager.GetPassedCheckpoint(_racingScene.PlayerCarPos);
                // reset back to last road thing
                _racingScene.ResetCarTo(pos);
            }

            if (Input.IsKeyPressed(Key.Tab)) {
                CallDeferred(MethodName.ShowShop);
            }

#if DEBUG
            if (Input.IsKeyPressed(Key.Key9)) {
                CallDeferred(MethodName.ShowRelicShop);
            }
#endif
        }

        _playerLineDebug3D.Start = _racingScene.PlayerCarPos;
        _playerLineDebug3D.End = _roadManager.GetNextCheckpoint(_racingScene.PlayerCarPos).Origin;
    }

    public override void _PhysicsProcess(double delta) {
        if (_paused) return;

        // update state object for this physics frame
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        var newDistanceTravelled = _roadManager.TotalDistanceFromCheckpoint(_playerLineDebug3D.End);
        state.SetDistanceTravelled(newDistanceTravelled);

        var currentPlayerSpeed = _racingScene.PlayerCarLinearVelocity.Length();
        state.SetCurrentSpeedMs(currentPlayerSpeed);

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
        if (_roadManager.PiecesPlaced % ROAD_TYPE_CHANGE_COUNT != 0)
            return;

        // generate new road types occasionally
        var otherTypesToPick = InfiniteRoadManager.GetWorldTypes().Where(x => x != _roadManager.CurrentWorldType);
        var newRoadType = RandHelper.RandFromList(otherTypesToPick.ToArray());

        _roadManager.UpdateWorldType(newRoadType);
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

    private void ShowRelicShop() {
        SetPauseState(true);

        var relics = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredRelicScreen))).Instantiate<HundredRelicScreen>();
        relics.Closed += () => {
            SetPauseState(false);
            CallDeferred(MethodName.RemoveNode, relics);
        };
        AddChild(relics);
    }

    private void ShowShop() {
        SetPauseState(true);

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
        if (_upgradeScreen == null) {
            _upgradeScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUpgradeScreen))).Instantiate<HundredUpgradeScreen>();
            _upgradeScreen.SetParts(state.Car.Details.GetAllPartsInTree()
                .Where(x => x.CurrentLevel < x.Levels.Length - 1)
                .OrderBy(x => GD.Randi())
                .Take(state.ShopPartCount)
                .ToList());

            _upgradeScreen.Closed += (carChanged) => {
                var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");
                if (carChanged) {
                    _shopCountdownStarted = true;

                    var changedDetails = _upgradeScreen.GetChangedDetails();
                    var newCarDetails = state.CarDetails.Clone();
                    newCarDetails.ApplyPartChange(changedDetails.Item1, changedDetails.Item1.CurrentLevel + 1);

                    state.SetCarDetails(newCarDetails);
                    state.AddMoney(-changedDetails.Item2);
                }

                SetPauseState(false);
                _racingScene.ReplaceCarWithState();
                CallDeferred(MethodName.RemoveNode, _upgradeScreen);
            };
        }

        AddChild(_upgradeScreen);
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
