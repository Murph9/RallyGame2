using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.Component.Traffic;

public interface ITrafficManager {
    void SetPaused(bool paused);
    Car GetClosestOpponent(Vector3 pos);
}

public partial class TrafficManager : Node3D, ITrafficManager {

    public const int MAX_TRAFFIC_COUNT = 10;
    public const int RIVAL_MAX_COUNT = 3;
    public const float OPPONENT_SPAWN_BUFFER_DISTANCE = 250;
    public const float TRAFFIC_SPAWN_BUFFER_DISTANCE = 25;
    public const float TRAFFIC_SPAWN_PLAYER_DISTANCE = 100;


    protected readonly List<Car> _normalTraffic = [];
    protected readonly List<Car> _opponents = [];
    protected IRoadManager _manager;

    private readonly RandomNumberGenerator _rand = new();
    private bool _paused;

    public TrafficManager(IRoadManager manager) {
        _manager = manager;
    }

    /// <summary>
    /// Parameterless constructor for subclasses constructed before the road
    /// manager exists. Call <see cref="SetRoadManager"/> before the node
    /// enters the scene tree.
    /// </summary>
    protected TrafficManager() { }

    /// <summary>
    /// When false, TrySpawnOpponent() is skipped and existing opponents are not
    /// removed by the base _Process loop. Set to false by subclasses that manage
    /// the opponent population themselves (e.g. PaydayRivalEncounterManager).
    /// </summary>
    public bool SpawnOpponents { get; set; } = true;

    public void SetRoadManager(IRoadManager manager) {
        _manager = manager;
    }

    public void SetPaused(bool paused) {
        _paused = paused;
        foreach (var car in _normalTraffic) {
            car.SetActive(!paused);
        }
        foreach (var car in _opponents) {
            car.SetActive(!paused);
        }
    }

    public Car GetClosestOpponent(Vector3 pos) {
        if (_opponents.Count <= 0) return null;

        var closestOpponent = _opponents.First();
        foreach (var opp in _opponents.Skip(1)) {
            if (opp.RigidBody.GlobalPosition.DistanceSquaredTo(pos) < closestOpponent.RigidBody.GlobalPosition.DistanceSquaredTo(pos)) {
                closestOpponent = opp;
            }
        }

        return closestOpponent;
    }

    public override void _Process(double delta) {
        if (_manager == null) throw new Exception("Road manager not set for traffic manager");
        if (_paused) return;

        var cameraPos = GetViewport().GetCamera3D().Position;

        foreach (var traffic in new List<Car>(_normalTraffic)) {
            // find cars which are greatly below their next checkpoint to kill them
            if (traffic.RigidBody.GlobalPosition.Y + 100 < _manager.GetNextCheckpoint(traffic.RigidBody.GlobalPosition).Origin.Y) {
                _normalTraffic.Remove(traffic);
                RemoveChild(traffic);

            } else if ((traffic.RigidBody.GlobalPosition - cameraPos).Length() > 350) {
                // remove any cars too far away
                _normalTraffic.Remove(traffic);
                RemoveChild(traffic);
            }
        }

        if (SpawnOpponents) {
            foreach (var opponent in new List<Car>(_opponents)) {
                var nextOpponentCheckpoint = _manager.GetNextCheckpoint(opponent.RigidBody.GlobalPosition);
                var removeForFallingOff = opponent.RigidBody.GlobalPosition.Y + 100 < nextOpponentCheckpoint.Origin.Y;
                var removeForBeingFarBehind = (opponent.RigidBody.GlobalPosition - cameraPos).Length() > 250;
                if (removeForFallingOff || removeForBeingFarBehind) {
                    _opponents.Remove(opponent);
                    RemoveChild(opponent);
                }
            }
        }

        TrySpawnTraffic();
        if (SpawnOpponents)
            TrySpawnOpponent();
    }


    private bool TrySpawnTraffic() {
        if (_normalTraffic.Count >= MAX_TRAFFIC_COUNT) return false;

        var cameraPos = GetViewport().GetCamera3D().Position;

        var nextPieces = _manager.GetNextCheckpoints(cameraPos, false, 0);
        // attempt to spawn far from the player
        var aFewRoadPositionsAway = nextPieces.FirstOrDefault(x => x.Origin.DistanceTo(cameraPos) > TRAFFIC_SPAWN_PLAYER_DISTANCE);

        var isReverse = _rand.Randf() > 0.5f;
        var ai = new TrafficAiInputs(_manager, isReverse);

        var realPosition = _manager.GetNextCheckpoint(aFewRoadPositionsAway.Origin, isReverse, isReverse ? -1 : 1);
        if (isReverse) {
            realPosition = new Transform3D(realPosition.Basis.Rotated(Vector3.Up, Mathf.Pi), realPosition.Origin);
        }

        // don't spawn them too close to the player
        if (realPosition.Origin.DistanceTo(cameraPos) < TRAFFIC_SPAWN_BUFFER_DISTANCE) {
            return false;
        }

        // check if its too close to an existing traffic car or the player
        foreach (var opp in _normalTraffic) {
            if (realPosition.Origin.DistanceTo(opp.RigidBody.GlobalPosition) < TRAFFIC_SPAWN_BUFFER_DISTANCE) {
                return false;
            }
        }

        var carMake = RandHelper.RandFromList(Enum.GetValues<CarMake>().Except([CarMake.Runner]).ToList());

        var car = new Car(carMake.LoadFromFile(Main.DEFAULT_GRAVITY), ai, false, realPosition, RandHelper.GetRandColour(_rand));
        car.RigidBody.LinearVelocity = realPosition.Basis * Vector3.Back * ai.TargetSpeedMs;

        AddChild(car);
        _normalTraffic.Add(car);

        return true;
    }

    private void TrySpawnOpponent() {
        // attempt to generate opponents
        if (_opponents.Count >= RIVAL_MAX_COUNT)
            return;

        var cameraPos = GetViewport().GetCamera3D().Position;

        var nextPieces = _manager.GetNextCheckpoints(cameraPos, false, 0);
        // don't spawn them too close to the player
        var position = nextPieces.Skip(10).FirstOrDefault();
        if (position == default || position == nextPieces.Last())
            // avoid using the last checkpoint position
            position = nextPieces.Reverse().Skip(1).FirstOrDefault();

        if (position == default)
            return;

        // don't spawn them too close to each other
        foreach (var opp in _opponents) {
            if (position.Origin.DistanceTo(opp.RigidBody.GlobalPosition) < OPPONENT_SPAWN_BUFFER_DISTANCE) {
                return;
            }
        }

        // make sure they don't spawn in the ground
        position.Origin += new Vector3(0, 0.5f, 0);

        // give them basic ai for now
        var ai = new TrafficAiInputs(_manager, false);
        ai.TargetSpeedMs += 10; // a little more than the default AI
        var car = new Car(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY), ai, false, position, RandHelper.GetRandColour(_rand));
        car.RigidBody.LinearVelocity = position.Basis * Vector3.Back * 10; // TODO

        AddChild(car);
        _opponents.Add(car);
    }
}
