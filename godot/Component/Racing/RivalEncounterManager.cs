using System;
using System.Linq;
using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Component;

namespace murph9.RallyGame2.godot.Component.Racing;

/// <summary>
/// Manages rival car encounters during a racing run.
/// Periodically spawns a rival alongside the player. When close enough and
/// at matching speed, a race is triggered. The rival's rarity hint indicates
/// how hard they are and what reward is on offer.
/// </summary>
public partial class RivalEncounterManager : Node {

    [Signal]
    public delegate void SpawnedRivalEventHandler(Car rival);
    [Signal]
    public delegate void RivalRaceStartedEventHandler(Car rival);
    [Signal]
    public delegate void RivalWonEventHandler();
    [Signal]
    public delegate void RivalLostEventHandler();

    private const float RIVAL_SPAWN_INTERVAL = 2f;  // seconds between spawn attempts
    private const float RACE_TRIGGER_DISTANCE = 6f;  // metres — must be this close
    protected const float RACE_DISTANCE = 500f; // metres of race length
    protected const float SPEED_MATCH_WINDOW = 3f;   // seconds both must hold matching speed
    private const float SPEED_MATCH_DIFF_MS = 1f;   // m/s tolerance for speed match

    protected InfiniteRoadManager _roadManager;
    protected Car _playerCar;

    protected float _spawnTimer;
    protected Car _currentRival;

    protected float _playerStartDist;
    protected bool _checkpointSet;
    protected bool _raceActive;
    protected double _speedMatchTimer;

    private Checkpoint _raceCheckpoint;

    /// <summary>Metres driven by the player since the race started (0 when no race is active).</summary>
    protected float RaceDistanceDriven => _raceActive ? _playerCar.DistanceTravelled - _playerStartDist : 0f;

    /// <summary>
    /// When the finish checkpoint has been placed, returns the straight-line distance
    /// from the player to it. Returns -1 while the checkpoint has not yet spawned.
    /// </summary>
    protected float RaceCheckpointDistance =>
        _checkpointSet && IsInstanceValid(_raceCheckpoint)
            ? _playerCar.RigidBody.GlobalPosition.DistanceTo(_raceCheckpoint.GlobalPosition)
            : -1f;

    public void Init(InfiniteRoadManager roadManager, Car playerCar) {
        _roadManager = roadManager;
        _playerCar = playerCar;
        _spawnTimer = RIVAL_SPAWN_INTERVAL;
    }

    public override void _PhysicsProcess(double delta) {
        if (_roadManager == null || _playerCar == null) return;

        if (_currentRival == null || !IsInstanceValid(_currentRival.RigidBody)) {
            if (_currentRival != null) {
                // Node was freed externally (e.g. road culling) — reset without ending the race formally
                _raceActive = false;
                _checkpointSet = false;
                if (_raceCheckpoint != null) { RemoveChild(_raceCheckpoint); _raceCheckpoint = null; }
                _currentRival = null;
            }
            _spawnTimer -= (float)delta;
            if (_spawnTimer <= 0) {
                SpawnRival();
                _spawnTimer = RIVAL_SPAWN_INTERVAL;
            }
            return;
        }

        // Respawn rival if it has fallen well below the road surface
        var nextCheckpointY = _roadManager.GetNextCheckpoint(_currentRival.RigidBody.GlobalPosition, false, 0).Origin.Y;
        if (_currentRival.RigidBody.GlobalPosition.Y + 20f < nextCheckpointY) {
            RespawnRivalNearPlayer();
        }

        if (!_raceActive) {
            float dist = _currentRival.RigidBody.GlobalPosition.DistanceTo(_playerCar.RigidBody.GlobalPosition);
            float speedDiff = (_currentRival.RigidBody.LinearVelocity - _playerCar.RigidBody.LinearVelocity).Length();

            if (dist < RACE_TRIGGER_DISTANCE && speedDiff < SPEED_MATCH_DIFF_MS) {
                _speedMatchTimer += delta;
                if (_speedMatchTimer >= SPEED_MATCH_WINDOW) {
                    StartRace();
                }
            } else {
                _speedMatchTimer = 0;
            }
        } else {
            if (!_checkpointSet && _playerCar.DistanceTravelled - _playerStartDist >= RACE_DISTANCE) {
                _checkpointSet = true;

                var checkpoints = _roadManager.GetNextCheckpoints(_playerCar.RigidBody.GlobalPosition, false, 0);
                var checkpoint = checkpoints.Skip(10).FirstOrDefault();
                if (checkpoint == default) {
                    checkpoint = checkpoints.Last();
                }

                GD.Print("Placing rival race checkpoint at distance: " + _playerCar.DistanceTravelled);
                CreateRaceCheckpoint(checkpoint);
            }
        }
    }

    private void RespawnRivalNearPlayer() {
        var t = _playerCar.RigidBody.GlobalTransform;
        t.Origin += t.Basis.X * 3f;
        _currentRival.RigidBody.GlobalTransform = t;
        _currentRival.RigidBody.LinearVelocity = _playerCar.RigidBody.LinearVelocity;
        _currentRival.RigidBody.AngularVelocity = Vector3.Zero;
    }

    private void SpawnRival() {
        // Pick a random car make; higher rarity rivals use faster CarMakes in future tuning
        var make = RandHelper.RandFromList(Enum.GetValues<CarMake>().Except([CarMake.Runner]).ToList());
        var details = make.LoadFromFile(Main.DEFAULT_GRAVITY);

        // Spawn offset to the side of the player
        var spawnTransform = _playerCar.RigidBody.GlobalTransform;
        spawnTransform.Origin += spawnTransform.Basis.X * 3f;

        _currentRival = new Car(details, new TrafficAiInputs(_roadManager, false), false, spawnTransform);
        GetParent().AddChild(_currentRival);

        _speedMatchTimer = 0;
        _raceActive = false;

        EmitSignal(SignalName.SpawnedRival, _currentRival);
    }

    private void StartRace() {
        _raceActive = true;
        _playerStartDist = _playerCar.DistanceTravelled;
        _checkpointSet = false;
        _currentRival.ChangeInputsTo(new RacingAiInputs(_roadManager));

        EmitSignal(SignalName.RivalRaceStarted, _currentRival);
    }

    private void CreateRaceCheckpoint(Transform3D transform) {
        _raceCheckpoint = Checkpoint.AsBox(transform, Vector3.One * 20, new Color(1, 1, 1, 0.7f));
        AddChild(_raceCheckpoint);
        _raceCheckpoint.ThingEntered += node => {
            if (node.GetParent() is not Car) return;
            if (node == _playerCar.RigidBody) {
                CallDeferred(MethodName.EndRaceDeferred, true);
            } else if (node == _currentRival?.RigidBody) {
                CallDeferred(MethodName.EndRaceDeferred, false);
            }
        };
    }

    private void EndRaceDeferred(bool playerWon) => EndRace(playerWon);

    private void EndRace(bool playerWon) {
        if (!_raceActive) return; // guard against duplicate calls
        _raceActive = false;
        _checkpointSet = false;

        if (_raceCheckpoint != null) {
            RemoveChild(_raceCheckpoint);
            _raceCheckpoint = null;
        }

        if (playerWon) {
            EmitSignal(SignalName.RivalWon);
        } else {
            EmitSignal(SignalName.RivalLost);
        }

        _currentRival.ChangeInputsTo(new StopAiInputs(_roadManager));
        _currentRival = null;
    }
}
