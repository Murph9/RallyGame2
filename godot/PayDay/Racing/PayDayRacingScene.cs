using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.PayDay.Parts;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Wraps InfiniteRoadManager + player Car for a Pay Day racing run.
/// </summary>
public partial class PayDayRacingScene : Node3D {

    [Signal]
    public delegate void RivalRaceStartedEventHandler(Car rival);
    [Signal]
    public delegate void RivalWonEventHandler(CollectedPart reward);
    [Signal]
    public delegate void RivalWonMoneyEventHandler();
    [Signal]
    public delegate void RivalLostEventHandler();

    private Car _car;
    private InfiniteRoadManager _roadManager;
    private PaydayRivalEncounterManager _rivalManager;

    public InfiniteRoadManager RoadManager => _roadManager;

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        // Build road first so we can get the correct spawn transform
        _roadManager = new InfiniteRoadManager(300, World.Procedural.WorldType.Simple2, 150);
        AddChild(_roadManager);
        GetNode<GlobalState>("/root/GlobalState").RoadManager = _roadManager;

        var spawnTransform = _roadManager.GetInitialSpawn();
        _car = new Car(state.CarDetails, null, true, spawnTransform);
        _car.RigidBody.Translate(new Vector3(0, 0, 8));
        AddChild(_car);

        // create the rival placer
        _rivalManager = new PaydayRivalEncounterManager();
        AddChild(_rivalManager);
        _rivalManager.Init(_roadManager, _car, state.DayNumber);
        _rivalManager.RivalRaceStarted += (rival) => EmitSignal(SignalName.RivalRaceStarted, rival);
        _rivalManager.RivalWonReward += (reward) => EmitSignal(SignalName.RivalWon, reward);
        _rivalManager.RivalWonMoney += () => EmitSignal(SignalName.RivalWonMoney);
        _rivalManager.RivalLost += (_rival) => EmitSignal(SignalName.RivalLost);
    }

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
    public void SetPaused(bool paused) {
        _car.SetActive(!paused);
        _roadManager.SetPaused(paused);
    }
}
