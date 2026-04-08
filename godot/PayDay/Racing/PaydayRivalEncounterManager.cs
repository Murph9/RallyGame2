using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Component.Racing;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.Component.Traffic;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// PayDay-specific traffic manager. Inherits all car spawning, despawning and
/// pausing from TrafficManager and treats every car in the traffic population
/// as a potential rival.
///
/// When any traffic car comes within RIVAL_UI_DISTANCE of the player it is
/// assigned a stake + rarity and gets a RivalHighlighter and RivalIndicatorUI.
/// The speed-match → race trigger logic then runs per-frame for each tracked car.
/// </summary>
public partial class PaydayRivalEncounterManager : TrafficManager {

    [Signal]
    public delegate void SpawnedRivalEventHandler(Car rival);
    [Signal]
    public delegate void RivalRaceStartedEventHandler(Car rival);
    [Signal]
    public delegate void RivalWonEventHandler(Car rival);
    [Signal]
    public delegate void RivalLostEventHandler(Car rival);
    [Signal]
    public delegate void RivalWonRewardEventHandler(CollectedPart reward);
    [Signal]
    public delegate void RivalWonMoneyEventHandler();

    private const float RIVAL_UI_DISTANCE = 50f;       // proximity gate for HUD attachment
    private const float RACE_TRIGGER_DISTANCE = 6f;
    private const float SPEED_MATCH_DIFF_MS = 2f;
    private const float SPEED_MATCH_WINDOW = 2.5f;
    private const float RACE_DISTANCE = 500f;

    private class PerRivalState {
        public PartLevel Rarity;
        public RivalStakeType Stake;
        public PartDetails WageredPart;
        public RivalHighlighter Highlighter;
        public RivalIndicatorUI IndicatorUI;
        // Race state (ported from RivalEntry)
        public bool RaceActive;
        public float PlayerStartDist;
        public bool CheckpointSet;
        public double SpeedMatchTimer;
        public Checkpoint RaceCheckpoint;
    }

    private readonly Dictionary<Car, PerRivalState> _rivalStates = [];
    // Cars in this list are actively racing; their index IS their sidebar slot.
    private readonly List<Car> _racingQueue = [];
    private Car _playerCar;
    private int _dayNumber;

    public PaydayRivalEncounterManager() : base() { }

    public void Init(Car playerCar, int dayNumber) {
        _playerCar = playerCar;
        _dayNumber = dayNumber;
    }

    // ── Main update ───────────────────────────────────────────────────────────

    public override void _Process(double delta) {
        // Let TrafficManager handle all spawn / despawn / removal logic first.
        base._Process(delta);

        if (_playerCar == null) return;

        // Collect every car currently alive in the traffic population.
        var allTraffic = _normalTraffic.Concat(_opponents);

        // 1. Attach UI to any car that has just entered rival range.
        foreach (var car in allTraffic) {
            if (_rivalStates.ContainsKey(car)) continue;

            float dist = car.RigidBody.GlobalPosition
                .DistanceTo(_playerCar.RigidBody.GlobalPosition);
            if (dist > RIVAL_UI_DISTANCE) continue;

            AttachRivalState(car);
        }

        // 2. Remove state for any car the base class has already despawned.
        foreach (var car in _rivalStates.Keys.ToList()) {
            if (!_normalTraffic.Contains(car) && !_opponents.Contains(car))
                RemoveRivalState(car, wasRace: false);
        }

        // 3. Per-rival race logic + UI update.
        foreach (var (rival, state) in _rivalStates) {
            if (!IsInstanceValid(rival?.RigidBody)) continue;

            // Only respawn cars mid-race — non-racing traffic should vanish naturally
            // when the base class removes them (handled by the stale-state cleanup above).
            if (state.RaceActive) {
                var nextCheckpointY = _manager
                    .GetNextCheckpoint(rival.RigidBody.GlobalPosition, false, 0).Origin.Y;
                if (rival.RigidBody.GlobalPosition.Y + 20f < nextCheckpointY)
                    RespawnRivalNearPlayer(rival);
            }

            if (!state.RaceActive) {
                float d = rival.RigidBody.GlobalPosition
                    .DistanceTo(_playerCar.RigidBody.GlobalPosition);
                float speedDiff = (rival.RigidBody.LinearVelocity
                    - _playerCar.RigidBody.LinearVelocity).Length();

                if (d < RACE_TRIGGER_DISTANCE && speedDiff < SPEED_MATCH_DIFF_MS) {
                    state.SpeedMatchTimer += delta;
                    if (state.SpeedMatchTimer >= SPEED_MATCH_WINDOW)
                        StartRace(rival, state);
                } else {
                    state.SpeedMatchTimer = Mathf.Max(state.SpeedMatchTimer - delta, 0);
                }

                state.IndicatorUI?.UpdateSpeedMatchProgress(
                    state.SpeedMatchTimer / SPEED_MATCH_WINDOW);
            } else {
                // Place finish checkpoint once the player has driven RACE_DISTANCE.
                if (!state.CheckpointSet
                    && _playerCar.DistanceTravelled - state.PlayerStartDist >= RACE_DISTANCE) {
                    state.CheckpointSet = true;

                    var checkpoints = _manager.GetNextCheckpoints(
                        _playerCar.RigidBody.GlobalPosition, false, 0);
                    var checkpoint = checkpoints.Skip(10).FirstOrDefault();
                    if (checkpoint == default) checkpoint = checkpoints.Last();

                    CreateRaceCheckpoint(rival, state, checkpoint);
                }

                state.IndicatorUI?.UpdateRaceProgress(
                    RaceDistanceDrivenFor(state), RACE_DISTANCE,
                    RaceCheckpointDistanceFor(state));
            }
        }
    }

    // ── Rival lifecycle ───────────────────────────────────────────────────────

    private void AttachRivalState(Car car) {
        // Skip cars driving the other way — they can't be raced.
        if (car.Inputs is TrafficAiInputs trafficAi && trafficAi.InReverse)
            return;

        // Retrieve part details from the player car to assign a stake.
        var details = _playerCar.Details;
        var stake = GD.Randf() < 0.5f ? RivalStakeType.Parts : RivalStakeType.Money;
        PartDetails wageredPart = null;
        if (stake == RivalStakeType.Parts && details != null) {
            var allParts = details.GetAllPartsInTree().ToList();
            if (allParts.Count > 0)
                wageredPart = RandHelper.RandFromList(allParts);
            else
                stake = RivalStakeType.Money;
        }

        var rarity = PartLevelHelper.RollRarity(_dayNumber);
        string wageredPartName = stake == RivalStakeType.Parts
            ? (wageredPart?.Name ?? "Part") : "$$$";

        var state = new PerRivalState {
            Rarity = rarity,
            Stake = stake,
            WageredPart = wageredPart,
        };

        var indicatorUI = new RivalIndicatorUI();
        indicatorUI.Init(_playerCar, car, rarity, stake, wageredPartName);
        AddChild(indicatorUI);
        state.IndicatorUI = indicatorUI;

        _rivalStates[car] = state;
        EmitSignal(SignalName.SpawnedRival, car);
    }

    private void RemoveRivalState(Car car, bool wasRace) {
        if (!_rivalStates.TryGetValue(car, out var state)) return;

        _racingQueue.Remove(car);
        UpdateRacingSlots();

        state.Highlighter?.QueueFree();
        state.IndicatorUI?.QueueFree();

        if (state.RaceCheckpoint != null && IsInstanceValid(state.RaceCheckpoint)) {
            RemoveChild(state.RaceCheckpoint);
            state.RaceCheckpoint = null;
        }

        _rivalStates.Remove(car);
    }

    // ── Race logic ────────────────────────────────────────────────────────────

    /// <summary>Reassigns sidebar slot indices after any queue change.</summary>
    private void UpdateRacingSlots() {
        for (int i = 0; i < _racingQueue.Count; i++) {
            if (_rivalStates.TryGetValue(_racingQueue[i], out var s))
                s.IndicatorUI?.SetSlotIndex(i);
        }
    }

    private void StartRace(Car rival, PerRivalState state) {
        state.RaceActive = true;
        state.PlayerStartDist = _playerCar.DistanceTravelled;
        state.CheckpointSet = false;
        rival.ChangeInputsTo(new RacingAiInputs(_manager));

        _racingQueue.Add(rival);
        UpdateRacingSlots();

        // Attach world-space glow + billboard now that the race is confirmed.
        var highlighter = new RivalHighlighter();
        highlighter.Init(rival, state.Rarity);
        rival.AddChild(highlighter);
        state.Highlighter = highlighter;

        state.IndicatorUI?.SetRaceActive(true);
        EmitSignal(SignalName.RivalRaceStarted, rival);
    }

    private void CreateRaceCheckpoint(Car rival, PerRivalState state, Transform3D transform) {
        state.RaceCheckpoint = Checkpoint.AsBox(
            transform, Vector3.One * 20, new Color(1, 1, 1, 0.7f));
        AddChild(state.RaceCheckpoint);

        state.RaceCheckpoint.ThingEntered += node => {
            if (node.GetParent() is not Car) return;
            if (node == _playerCar.RigidBody)
                CallDeferred(MethodName.EndRaceDeferred, rival, true);
            else if (node == rival.RigidBody)
                CallDeferred(MethodName.EndRaceDeferred, rival, false);
        };
    }

    private void EndRaceDeferred(Car rival, bool playerWon) {
        if (_rivalStates.TryGetValue(rival, out var state))
            EndRace(rival, state, playerWon);
    }

    private void EndRace(Car rival, PerRivalState state, bool playerWon) {
        if (!state.RaceActive) return;
        state.RaceActive = false;
        state.CheckpointSet = false;

        _racingQueue.Remove(rival);
        UpdateRacingSlots();

        if (state.RaceCheckpoint != null) {
            RemoveChild(state.RaceCheckpoint);
            state.RaceCheckpoint = null;
        }

        if (playerWon) {
            if (state.Stake == RivalStakeType.Parts && state.WageredPart != null) {
                var reward = new CollectedPart(state.WageredPart, state.Rarity);
                EmitSignal(SignalName.RivalWonReward, reward);
            } else {
                EmitSignal(SignalName.RivalWonMoney);
            }
            EmitSignal(SignalName.RivalWon, rival);
        } else {
            EmitSignal(SignalName.RivalLost, rival);
        }

        rival.ChangeInputsTo(new StopAiInputs(_manager));

        state.Highlighter?.QueueFree();
        state.IndicatorUI?.QueueFree();
        _rivalStates.Remove(rival);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RespawnRivalNearPlayer(Car rival) {
        // Place the rival a few checkpoints ahead of the player so the catch-up
        // teleport is never visible (they appear in front, not beside the player).
        // Each racing rival skips to a different checkpoint so they don't stack.
        // positionIndex = 1 keeps them in the forward traffic lane.
        int slot = _racingQueue.IndexOf(rival);
        int skip = 1 + slot; // slot 0 → 1 ahead, slot 1 → 2 ahead, etc.
        var checkpoints = _manager.GetNextCheckpoints(
            _playerCar.RigidBody.GlobalPosition, false, 1);
        var target = checkpoints.Skip(skip).FirstOrDefault();
        if (target == default) target = checkpoints.LastOrDefault();
        if (target == default) return;

        rival.RigidBody.GlobalTransform = target;
        rival.RigidBody.LinearVelocity = _playerCar.RigidBody.LinearVelocity;
        rival.RigidBody.AngularVelocity = Vector3.Zero;
    }

    private float RaceDistanceDrivenFor(PerRivalState state) =>
        state.RaceActive ? _playerCar.DistanceTravelled - state.PlayerStartDist : 0f;

    private float RaceCheckpointDistanceFor(PerRivalState state) =>
        state.CheckpointSet && IsInstanceValid(state.RaceCheckpoint)
            ? _playerCar.RigidBody.GlobalPosition.DistanceTo(state.RaceCheckpoint.GlobalPosition)
            : -1f;
}
