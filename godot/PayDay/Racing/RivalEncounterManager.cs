using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.PayDay.Parts;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Manages rival car encounters during a racing run.
/// Periodically spawns a rival alongside the player. When close enough and
/// at matching speed, a race is triggered. The rival's rarity hint indicates
/// how hard they are and what reward is on offer.
/// </summary>
public partial class RivalEncounterManager : Node {

    [Signal]
    public delegate void RivalWonEventHandler(CollectedPart reward);
    [Signal]
    public delegate void RivalLostEventHandler();

    private const float RIVAL_SPAWN_INTERVAL = 20f;  // seconds between spawn attempts
    private const float RACE_TRIGGER_DISTANCE = 10f;  // metres — must be this close
    private const float RACE_DISTANCE = 500f; // metres of race length
    private const float SPEED_MATCH_WINDOW = 3f;   // seconds both must hold matching speed
    private const float SPEED_MATCH_DIFF_MS = 5f;   // m/s tolerance for speed match

    private InfiniteRoadManager _roadManager;
    private Car _playerCar;
    private int _dayNumber;

    private float _spawnTimer;
    private Car _currentRival;
    private PartRarity _currentRivalRarity;

    private float _playerStartDist;
    private float _rivalStartDist;
    private bool _raceActive;
    private double _speedMatchTimer;

    public void Init(InfiniteRoadManager roadManager, Car playerCar, int dayNumber) {
        _roadManager = roadManager;
        _playerCar = playerCar;
        _dayNumber = dayNumber;
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
        _currentRivalRarity = PartRarityHelper.RollRarity(_dayNumber);

        // Pick a random car make; higher rarity rivals use faster CarMakes in future tuning
        int makeCount = System.Enum.GetValues<CarMake>().Length;
        var make = (CarMake)GD.RandRange(0, makeCount - 1);
        var details = make.LoadFromFile(Main.DEFAULT_GRAVITY);

        // Spawn offset to the side of the player
        var spawnTransform = _playerCar.RigidBody.GlobalTransform;
        spawnTransform.Origin += spawnTransform.Basis.X * 3f;

        _currentRival = new Car(details, null, false, spawnTransform);
        _currentRival.ChangeInputsTo(new TrafficAiInputs(_roadManager, false));
        GetParent().AddChild(_currentRival);

        _speedMatchTimer = 0;
        _raceActive = false;
    }

    private void StartRace() {
        _raceActive = true;
        _playerStartDist = _playerCar.DistanceTravelled;
        _rivalStartDist = _currentRival.DistanceTravelled;
        _currentRival.ChangeInputsTo(new RacingAiInputs(_roadManager));
    }

    private void EndRace(bool playerWon) {
        _raceActive = false;

        if (playerWon) {
            var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
            var reward = PartDropTable.Generate(state.CarDetails, _dayNumber);
            // Override the rolled rarity with the advertised rival rarity
            var finalReward = new CollectedPart(reward.Part, _currentRivalRarity);
            EmitSignal(SignalName.RivalWon, finalReward);
        } else {
            EmitSignal(SignalName.RivalLost);
        }

        _currentRival.ChangeInputsTo(new StopAiInputs(_roadManager));
        _currentRival = null;
    }
}
