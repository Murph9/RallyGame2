using System;
using System.Linq;
using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;

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
    private const float RACE_TRIGGER_DISTANCE = 10f;  // metres — must be this close
    private const float RACE_DISTANCE = 500f; // metres of race length
    protected const float SPEED_MATCH_WINDOW = 3f;   // seconds both must hold matching speed
    private const float SPEED_MATCH_DIFF_MS = 5f;   // m/s tolerance for speed match

    protected InfiniteRoadManager _roadManager;
    protected Car _playerCar;

    protected float _spawnTimer;
    protected Car _currentRival;

    protected float _playerStartDist;
    protected float _rivalStartDist;
    protected bool _raceActive;
    protected double _speedMatchTimer;

    public void Init(InfiniteRoadManager roadManager, Car playerCar) {
        _roadManager = roadManager;
        _playerCar = playerCar;
        _spawnTimer = RIVAL_SPAWN_INTERVAL;
    }

    public override void _PhysicsProcess(double delta) {
        if (_roadManager == null || _playerCar == null) return;

        if (_currentRival == null) {
            _spawnTimer -= (float)delta;
            if (_spawnTimer <= 0) {
                SpawnRival();
                _spawnTimer = RIVAL_SPAWN_INTERVAL;
            }
            return;
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
            float playerDist = _playerCar.DistanceTravelled - _playerStartDist;
            float rivalDist = _currentRival.DistanceTravelled - _rivalStartDist;

            if (playerDist >= RACE_DISTANCE || rivalDist >= RACE_DISTANCE) {
                EndRace(playerWon: playerDist >= rivalDist);
            }
        }
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
        _rivalStartDist = _currentRival.DistanceTravelled;
        _currentRival.ChangeInputsTo(new RacingAiInputs(_roadManager));

        EmitSignal(SignalName.RivalRaceStarted, _currentRival);
    }

    private void EndRace(bool playerWon) {
        _raceActive = false;

        if (playerWon) {
            EmitSignal(SignalName.RivalWon);
        } else {
            EmitSignal(SignalName.RivalLost);
        }

        _currentRival.ChangeInputsTo(new StopAiInputs(_roadManager));
        _currentRival = null;
    }
}
