using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component.Racing;

public enum RivalStakeType { Money, Parts }

/// <summary>
/// Manages rival car encounters during a racing run.
/// Up to MAX_RIVALS rivals can exist simultaneously. Each rival independently
/// negotiates a speed-match with the player and then races them. Every rival
/// is assigned a stake at spawn — either a money reward or a specific part
/// from their car — so the player always knows what is on offer before the race.
/// </summary>
public partial class RivalEncounterManager : Node {

    [Signal]
    public delegate void SpawnedRivalEventHandler(Car rival);
    [Signal]
    public delegate void RivalRaceStartedEventHandler(Car rival);
    [Signal]
    public delegate void RivalWonEventHandler(Car rival);
    [Signal]
    public delegate void RivalLostEventHandler(Car rival);

    private const int MAX_RIVALS = 3;
    private const float RIVAL_SPAWN_INTERVAL = 2f;
    private const float RACE_TRIGGER_DISTANCE = 6f;
    protected const float RACE_DISTANCE = 500f;
    protected const float SPEED_MATCH_WINDOW = 3f;
    private const float SPEED_MATCH_DIFF_MS = 2f;

    protected InfiniteRoadManager _roadManager;
    protected Car _playerCar;

    private float _spawnTimer;
    private readonly List<RivalEntry> _rivals = [];

    /// <summary>Returns the entry for a given rival car, or null if not tracked.</summary>
    protected RivalEntry GetRivalEntry(Car rival) =>
        _rivals.FirstOrDefault(e => e.Car == rival);

    /// <summary>Metres driven by the player since that rival's race started.</summary>
    protected float RaceDistanceDrivenFor(RivalEntry entry) =>
        entry.RaceActive ? _playerCar.DistanceTravelled - entry.PlayerStartDist : 0f;

    /// <summary>
    /// Straight-line distance from the player to the finish checkpoint for this rival.
    /// Returns -1 while the checkpoint has not been placed yet.
    /// </summary>
    protected float RaceCheckpointDistanceFor(RivalEntry entry) =>
        entry.CheckpointSet && IsInstanceValid(entry.RaceCheckpoint)
            ? _playerCar.RigidBody.GlobalPosition.DistanceTo(entry.RaceCheckpoint.GlobalPosition)
            : -1f;

    public void Init(InfiniteRoadManager roadManager, Car playerCar) {
        _roadManager = roadManager;
        _playerCar = playerCar;
        _spawnTimer = RIVAL_SPAWN_INTERVAL;
    }

    public override void _PhysicsProcess(double delta) {
        if (_roadManager == null || _playerCar == null) return;

        // Spawn new rivals while under the cap
        if (_rivals.Count < MAX_RIVALS) {
            _spawnTimer -= (float)delta;
            if (_spawnTimer <= 0) {
                SpawnRival();
                _spawnTimer = RIVAL_SPAWN_INTERVAL;
            }
        }

        // Process each rival independently
        for (int i = _rivals.Count - 1; i >= 0; i--) {
            var entry = _rivals[i];

            // If the node was freed externally, clean up silently
            if (!IsInstanceValid(entry.Car?.RigidBody)) {
                CleanupEntry(entry, i);
                continue;
            }

            // Respawn rival if it has fallen below the road surface
            var nextCheckpointY = _roadManager
                .GetNextCheckpoint(entry.Car.RigidBody.GlobalPosition, false, 0).Origin.Y;
            if (entry.Car.RigidBody.GlobalPosition.Y + 20f < nextCheckpointY) {
                RespawnRivalNearPlayer(entry);
            }

            if (!entry.RaceActive) {
                float dist = entry.Car.RigidBody.GlobalPosition
                    .DistanceTo(_playerCar.RigidBody.GlobalPosition);
                float speedDiff = (entry.Car.RigidBody.LinearVelocity
                    - _playerCar.RigidBody.LinearVelocity).Length();

                if (dist < RACE_TRIGGER_DISTANCE && speedDiff < SPEED_MATCH_DIFF_MS) {
                    entry.SpeedMatchTimer += delta;
                    if (entry.SpeedMatchTimer >= SPEED_MATCH_WINDOW) {
                        StartRace(entry);
                    }
                } else {
                    entry.SpeedMatchTimer = Mathf.Max(entry.SpeedMatchTimer - delta, 0);
                }
            } else {
                if (!entry.CheckpointSet
                    && _playerCar.DistanceTravelled - entry.PlayerStartDist >= RACE_DISTANCE) {
                    entry.CheckpointSet = true;

                    var checkpoints = _roadManager.GetNextCheckpoints(
                        _playerCar.RigidBody.GlobalPosition, false, 0);
                    var checkpoint = checkpoints.Skip(10).FirstOrDefault();
                    if (checkpoint == default)
                        checkpoint = checkpoints.Last();

                    GD.Print($"Placing rival race checkpoint at distance: {_playerCar.DistanceTravelled}");
                    CreateRaceCheckpoint(entry, checkpoint);
                }
            }
        }
    }

    private void SpawnRival() {
        var make = RandHelper.RandFromList(
            Enum.GetValues<CarMake>().Except([CarMake.Runner]).ToList());
        var details = make.LoadFromFile(Main.DEFAULT_GRAVITY);

        // Step each new rival further to the side so they do not overlap
        var spawnTransform = _playerCar.RigidBody.GlobalTransform;
        spawnTransform.Origin += spawnTransform.Basis.X * (3f * (_rivals.Count + 1));

        var rivalCar = new Car(details, new TrafficAiInputs(_roadManager, false), false, spawnTransform);
        GetParent().AddChild(rivalCar);

        // Decide the rival's stake
        var stake = GD.Randf() < 0.5f ? RivalStakeType.Parts : RivalStakeType.Money;
        Part wageredPart = null;
        if (stake == RivalStakeType.Parts) {
            var allParts = details.GetAllPartsInTree().ToList();
            if (allParts.Count > 0)
                wageredPart = RandHelper.RandFromList(allParts);
            else
                stake = RivalStakeType.Money; // fallback if car has no parts
        }

        var entry = new RivalEntry {
            Car = rivalCar,
            Details = details,
            Stake = stake,
            WageredPart = wageredPart,
        };
        _rivals.Add(entry);

        EmitSignal(SignalName.SpawnedRival, rivalCar);
    }

    private void StartRace(RivalEntry entry) {
        entry.RaceActive = true;
        entry.PlayerStartDist = _playerCar.DistanceTravelled;
        entry.CheckpointSet = false;
        entry.Car.ChangeInputsTo(new RacingAiInputs(_roadManager));

        EmitSignal(SignalName.RivalRaceStarted, entry.Car);
    }

    private void CreateRaceCheckpoint(RivalEntry entry, Transform3D transform) {
        entry.RaceCheckpoint = Checkpoint.AsBox(
            transform, Vector3.One * 20, new Color(1, 1, 1, 0.7f));
        AddChild(entry.RaceCheckpoint);

        entry.RaceCheckpoint.ThingEntered += node => {
            if (node.GetParent() is not Car) return;
            if (node == _playerCar.RigidBody) {
                CallDeferred(MethodName.EndRaceDeferred, entry.Car, true);
            } else if (node == entry.Car?.RigidBody) {
                CallDeferred(MethodName.EndRaceDeferred, entry.Car, false);
            }
        };
    }

    private void EndRaceDeferred(Car rivalCar, bool playerWon) {
        var entry = _rivals.FirstOrDefault(e => e.Car == rivalCar);
        if (entry != null) EndRace(entry, playerWon);
    }

    private void EndRace(RivalEntry entry, bool playerWon) {
        if (!entry.RaceActive) return;
        entry.RaceActive = false;
        entry.CheckpointSet = false;

        if (entry.RaceCheckpoint != null) {
            RemoveChild(entry.RaceCheckpoint);
            entry.RaceCheckpoint = null;
        }

        if (playerWon) {
            EmitSignal(SignalName.RivalWon, entry.Car);
        } else {
            EmitSignal(SignalName.RivalLost, entry.Car);
        }

        entry.Car.ChangeInputsTo(new StopAiInputs(_roadManager));

        int idx = _rivals.IndexOf(entry);
        CleanupEntry(entry, idx);
    }

    private void RespawnRivalNearPlayer(RivalEntry entry) {
        var t = _playerCar.RigidBody.GlobalTransform;
        t.Origin += t.Basis.X * 3f;
        entry.Car.RigidBody.GlobalTransform = t;
        entry.Car.RigidBody.LinearVelocity = _playerCar.RigidBody.LinearVelocity;
        entry.Car.RigidBody.AngularVelocity = Vector3.Zero;
    }

    private void CleanupEntry(RivalEntry entry, int index) {
        if (index >= 0 && index < _rivals.Count)
            _rivals.RemoveAt(index);
        else
            _rivals.Remove(entry);

        if (entry.RaceCheckpoint != null && IsInstanceValid(entry.RaceCheckpoint)) {
            RemoveChild(entry.RaceCheckpoint);
            entry.RaceCheckpoint = null;
        }
    }
}
