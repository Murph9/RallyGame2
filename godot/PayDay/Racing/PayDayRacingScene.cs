using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.PayDay.Parts;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Wraps InfiniteRoadManager + player Car for a Pay Day racing run.
/// Mirrors HundredRacingScene but drives from PayDayGlobalState.
/// </summary>
public partial class PayDayRacingScene : Node3D {

    [Signal]
    public delegate void RivalRaceStartedEventHandler(Car rival);
    [Signal]
    public delegate void RivalWonEventHandler(CollectedPart reward);
    [Signal]
    public delegate void RivalLostEventHandler();

    public Vector3 PlayerCarPos => _car.RigidBody.GlobalPosition;
    public Vector3 PlayerCarLinearVelocity => _car.RigidBody.LinearVelocity;
    public float PlayerDistanceTravelled => _car.DistanceTravelled;

    private Car _car;
    private InfiniteRoadManager _roadManager;
    private PaydayRivalEncounterManager _rivalManager;

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        // Build road first so we can get the correct spawn transform
        _roadManager = new InfiniteRoadManager(300, World.Procedural.WorldType.Simple2);
        AddChild(_roadManager);
        GetNode<GlobalState>("/root/GlobalState").RoadManager = _roadManager;

        var spawnTransform = _roadManager.GetInitialSpawn();
        _car = new Car(state.CarDetails, null, true, spawnTransform);
        AddChild(_car);

        // Rival encounter manager sits as a sibling node
        _rivalManager = new PaydayRivalEncounterManager();
        AddChild(_rivalManager);
        _rivalManager.Init(_roadManager, _car, state.DayNumber);
        _rivalManager.RivalRaceStarted += (rival) => EmitSignal(SignalName.RivalRaceStarted, rival);
        _rivalManager.RivalWonReward += (reward) => EmitSignal(SignalName.RivalWon, reward);
        _rivalManager.RivalLost += () => EmitSignal(SignalName.RivalLost);
    }

    /// <summary>Hot-swap the car with new details (e.g. after applying a part).</summary>
    public void ReplaceCarWithState(CarDetails newDetails) {
        Callable.From(() => {
            var newCar = _car.CloneWithNewDetails(newDetails);
            RemoveChild(_car);
            _car.QueueFree();
            _car = newCar;
            AddChild(_car);
        }).CallDeferred();
    }

    public void ResetCarTo(Transform3D transform) => _car.ResetCarTo(transform);
    public void SetActive(bool active) => _car.SetActive(active);
    public bool IsMainCar(Node3D node) => _car.RigidBody == node;
    public void SetPaused(bool paused) {
        _car.SetActive(!paused);
        _roadManager.SetPaused(paused);
    }
}
