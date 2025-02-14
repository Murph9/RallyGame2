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
    private const float RIVAL_RACE_WIN_MONEY = 1000;

    private InfiniteRoadManager _roadManager;
    private HundredRacingScene _racingScene;
    private HundredUI _ui;

    private Checkpoint _raceFinishLine;

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
        state.CarDetails = CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY);

        _roadManager = new InfiniteRoadManager(100);
        _roadManager.ShopPlaced += ShopTriggeredAt;
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
            state.TotalTimePassed += delta;

            if (Input.IsActionJustPressed("menu_back")) {
                Pause();
            }

            if (Input.IsActionJustPressed("car_reset")) {
                var pos = _roadManager.GetPassedCheckpoint(_racingScene.PlayerCarPos);
                // reset back to last road thing
                _racingScene.ResetCarTo(pos);
            }
        }

        _playerLineDebug3D.Start = _racingScene.PlayerCarPos;
        _playerLineDebug3D.End = _roadManager.GetNextCheckpoint(_racingScene.PlayerCarPos).Origin;

        var newDistanceTravelled = _roadManager.TotalDistanceWithCheckpoint(_playerLineDebug3D.End);
        state.DistanceTravelled = Mathf.Max(state.DistanceTravelled, newDistanceTravelled); // please no negative progress
    }

    public override void _PhysicsProcess(double delta) {
        // update state object for this physics frame
        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        state.CurrentSpeedKMH = _racingScene.PlayerCarLinearVelocity.Length() * 3.6f;

        // trigger next shop stop
        if (_racingScene.PlayerDistanceTravelled > state.NextShopDistance) {
            GD.Print("Queuing shop because of next trigger " + state.NextShopDistance);
            if (state.NextShopDistance < state.ShopSpread) {
                state.NextShopDistance = state.ShopSpread; // the first one is much closer than normal
            } else {
                state.NextShopDistance += state.ShopSpread;
            }

            _roadManager.TriggerShop();
        }

        // check the current race state
        if (state.RivalRaceDetails.HasValue) {
            var rival = state.RivalRaceDetails;
            if (!rival.Value.CheckpointSent && rival.Value.StartDistance + rival.Value.RaceDistance < _racingScene.PlayerDistanceTravelled) {
                GD.Print("Triggering race end because: " + (rival.Value.StartDistance + rival.Value.RaceDistance) + "<" + _racingScene.PlayerDistanceTravelled);
                var checkpoints = _roadManager.GetNextCheckpoints(_racingScene.PlayerCarPos, false, 0);
                var checkpoint = checkpoints.Skip(10).FirstOrDefault();
                if (checkpoint == default) {
                    checkpoint = checkpoints.Last();
                }

                state.RivalRaceDetails = rival.Value with { CheckpointSent = true };
                // spawn checkpoint there
                _raceFinishLine = Checkpoint.AsBox(checkpoint, Vector3.One * 20, new Color(1, 1, 1, 0.7f)); // should be invisible-ish
                AddChild(_raceFinishLine);
                _raceFinishLine.ThingEntered += (Node3D node) => {
                    if (node.GetParent() is not Car) return;

                    if (state.RivalRaceDetails != null) {
                        // oh the race is over?
                        if (_racingScene.IsMainCar(node)) {
                            CallDeferred(MethodName.ResetRivalRace);
                            state.RivalRaceMessage = "Nice win";
                            state.Money += RIVAL_RACE_WIN_MONEY;
                        }
                        if (node == state.RivalRaceDetails.Value.Rival.RigidBody) {
                            CallDeferred(MethodName.ResetRivalRace);
                            state.RivalRaceMessage = "You lost";
                        }
                    }
                };
            }
        } else {
            var closestRival = _roadManager.GetClosestOpponent(_racingScene.PlayerCarPos);
            if (closestRival != null && closestRival.RigidBody.GlobalPosition.DistanceTo(_racingScene.PlayerCarPos) < 10 && (closestRival.RigidBody.LinearVelocity - _racingScene.PlayerCarLinearVelocity).Length() < 3) {
                GD.Print("Challenged rival");
                state.RivalRaceDetails = new RivalRace(closestRival, _racingScene.PlayerDistanceTravelled, 100, false);
                state.RivalRaceMessage = "Rival race started, dist: " + state.RivalRaceDetails.Value.RaceDistance + "m";

                var newAi = new RacingAiInputs(_roadManager) {
                    RoadWidth = 10
                };
                closestRival.ChangeInputsTo(newAi);
            }
        }
    }

    private void ShopTriggeredAt(Transform3D transform) {
        transform.Origin += transform.Basis * new Vector3(30, 0, -10);
        var shopTrigger = Checkpoint.AsBox(transform, Vector3.One * 8, new Color(0, 0, 0, 0.4f)); // should be invisible
        AddChild(shopTrigger);
        shopTrigger.ThingEntered += (Node3D node) => {
            if (node.GetParent() is not Car) return;
            if (!_racingScene.IsMainCar(node)) return;

            GD.Print("Hit shop trigger");
            CallDeferred(MethodName.ResumeFromNode, shopTrigger);
            CallDeferred(MethodName.ShowShop);
        };
    }

    private void ResumeFromNode(Node node) {
        _paused = false;
        RemoveChild(node);
    }

    private void ResetRivalRace() {
        RemoveChild(_raceFinishLine);
        _raceFinishLine = null;

        var state = GetNode<HundredGlobalState>("/root/HundredGlobalState");

        // change ai to stop then revert to the current one so they still exist
        RevertRivalAi(state.RivalRaceDetails.Value.Rival, state.RivalRaceDetails.Value.Rival.Inputs);
        state.RivalRaceDetails.Value.Rival.ChangeInputsTo(new StopAiInputs(_roadManager));

        state.RivalRaceDetails = null;

        // no need to delete the rival, its not like it should disappear
    }
    private static async void RevertRivalAi(Car rival, ICarInputs oldAi) {
        await Task.Delay(TimeSpan.FromSeconds(5));
        rival.ChangeInputsTo(oldAi);
    }

    private void ShowShop() {
        _racingScene.StopDriving();
        _roadManager.SetPaused(true);

        var upgrade = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredUpgradeScreen))).Instantiate<HundredUpgradeScreen>();
        upgrade.Closed += () => {
            _roadManager.SetPaused(false);
            _racingScene.ReplaceCarWithState();
            _racingScene.StartDriving();

            CallDeferred(MethodName.ResumeFromNode, upgrade);
        };
        AddChild(upgrade);
    }

    private void Pause() {
        _paused = true;
        _racingScene.StopDriving();
        _roadManager.SetPaused(true);

        var pauseScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredPauseScreen))).Instantiate<HundredPauseScreen>();
        pauseScreen.Resume += () => {
            _roadManager.SetPaused(false);
            _racingScene.ReplaceCarWithState();
            _racingScene.StartDriving();

            CallDeferred(MethodName.ResumeFromNode, pauseScreen);
        };
        pauseScreen.Quit += () => {
            // TODO actually pause
            GetTree().ChangeSceneToFile("res://Main.tscn");
        };
        AddChild(pauseScreen);
    }
}
