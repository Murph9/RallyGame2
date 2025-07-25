using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World;
using murph9.RallyGame2.godot.World.Procedural;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public interface IRoadManager {
    Transform3D GetPassedCheckpoint(Vector3 pos);
    IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, bool inReverse = false, int positionIndex = 0);
    float CurrentRoadWidth { get; }
}

public partial class InfiniteRoadManager : Node3D, IRoadManager {

    // Makes a world based on the infinite world pieces
    // does traffic and stuff

    public const int MAX_TRAFFIC_COUNT = 10;
    public const int RIVAL_MAX_COUNT = 3;
    public const float OPPONENT_SPAWN_BUFFER_DISTANCE = 250;
    public const float TRAFFIC_SPAWN_BUFFER_DISTANCE = 25;
    public const float TRAFFIC_SPAWN_PLAYER_DISTANCE = 100;

    [Signal]
    public delegate void LoadedEventHandler();
    [Signal]
    public delegate void RoadNextPointEventHandler(float totalDistance, Transform3D transform);

    private readonly InfiniteWorldPieces _world;
    private readonly List<Car> _normalTraffic = [];
    private readonly List<Car> _opponents = [];
    private readonly RandomNumberGenerator _rand = new();

    public WorldType CurrentWorldType => _world.CurrentWorldType;
    public int PiecesPlaced { get; private set; }
    public float CurrentRoadWidth { get; private set; }

    public InfiniteRoadManager(int spawnDistance, WorldType initialWorldType) {
        UpdateWorldType(initialWorldType);

        var strat = new PiecePlacementStrategy(PiecePlacementStrategy.Type.Camera, spawnDistance);

        _world = new InfiniteWorldPieces(new ProceduralPieceGenerator(initialWorldType), strat, new PieceDecorator());
        _world.PieceAdded += PiecePlacedListener;
        _world.SetIgnoredPieces(["station"]);

        CurrentRoadWidth = _world.GetRoadWidth();
    }

    public override void _Ready() {
        AddChild(_world);
    }

    public void UpdateWorldType(WorldType name) {
        GD.Print("World Type set to " + name);
        if (_world == null) return;

        _world.UpdateWorldType(name);

        CurrentRoadWidth = _world.GetRoadWidth();
    }

    public static IEnumerable<WorldType> GetWorldTypes() {
        return (WorldType[])Enum.GetValues(typeof(WorldType));
    }

    public void StopPlacingRoadAfter(double distance) {
        _world.LimitPlacingAfterDistance(distance);
    }

    public override void _Process(double delta) {
        // calculate the player pos
        var cameraPos = GetViewport().GetCamera3D().Position;

        foreach (var traffic in new List<Car>(_normalTraffic)) {
            // find cars which are greatly below their next checkpoint to kill them
            if (traffic.RigidBody.GlobalPosition.Y + 100 < GetNextCheckpoint(traffic.RigidBody.GlobalPosition).Origin.Y) {
                _normalTraffic.Remove(traffic);
                RemoveChild(traffic);

            } else if ((traffic.RigidBody.GlobalPosition - cameraPos).Length() > 350) {
                // remove any cars too far away
                _normalTraffic.Remove(traffic);
                RemoveChild(traffic);
            }
        }

        foreach (var opponent in new List<Car>(_opponents)) {
            var nextOpponentCheckpoint = GetNextCheckpoint(opponent.RigidBody.GlobalPosition);
            var removeForFallingOff = opponent.RigidBody.GlobalPosition.Y + 100 < nextOpponentCheckpoint.Origin.Y;
            var removeForBeingFarBehind = (opponent.RigidBody.GlobalPosition - cameraPos).Length() > 250;
            if (removeForFallingOff || removeForBeingFarBehind) {
                _opponents.Remove(opponent);
                RemoveChild(opponent);
            }
        }

        TrySpawnTraffic();
        TrySpawnOpponent();
    }

    private void TrySpawnOpponent() {
        // attempt to generate opponents
        if (_opponents.Count >= RIVAL_MAX_COUNT)
            return;

        var cameraPos = GetViewport().GetCamera3D().Position;

        var nextPieces = GetNextCheckpoints(cameraPos, false, 0);
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
        var ai = new TrafficAiInputs(this, false);
        ai.TargetSpeedMs += 10; // a little more than the default AI
        var car = new Car(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY), ai, false, position, RandHelper.GetRandColour(_rand));
        car.RigidBody.LinearVelocity = position.Basis * Vector3.Back * 10; // TODO

        AddChild(car);
        _opponents.Add(car);
    }

    private void PiecePlacedListener(Transform3D checkpointTransform) {
        PiecesPlaced++;

        EmitSignal(SignalName.RoadNextPoint, _world.TotalDistanceFromCheckpoint(checkpointTransform.Origin), checkpointTransform);
    }

    private bool TrySpawnTraffic() {
        if (_normalTraffic.Count >= MAX_TRAFFIC_COUNT) return false;

        var cameraPos = GetViewport().GetCamera3D().Position;

        var nextPieces = GetNextCheckpoints(cameraPos, false, 0);
        // attempt to spawn far from the player
        var aFewRoadPositionsAway = nextPieces.FirstOrDefault(x => x.Origin.DistanceTo(cameraPos) > TRAFFIC_SPAWN_PLAYER_DISTANCE);

        var isReverse = _rand.Randf() > 0.5f;
        var ai = new TrafficAiInputs(this, isReverse);

        var realPosition = GetNextCheckpoint(aFewRoadPositionsAway.Origin, isReverse, isReverse ? -1 : 1);
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

    public Transform3D GetInitialSpawn() => _world.GetInitialSpawn().StartTransform;

    public Transform3D GetPassedCheckpoint(Vector3 pos) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var index = GetClosestToPieceIndex(checkpoints, pos);
        return checkpoints[Math.Max(0, index - 1)].StartTransform;
    }

    public Transform3D GetNextCheckpoint(Vector3 pos) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(checkpoints, pos, false);
        return checkpoints[indexes.First()].StartTransform;
    }

    public Transform3D GetNextCheckpoint(Vector3 pos, bool inReverse, int positionIndex) => GetNextCheckpoints(pos, inReverse, positionIndex).First();

    public IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, bool inReverse, int positionIndex) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(checkpoints, pos, inReverse);

        var list = new List<Transform3D>();
        foreach (var index in indexes) {
            var checkpoint = checkpoints[index];
            var originOffset = checkpoint.LeftOffset * positionIndex;
            list.Add(new Transform3D(checkpoint.StartTransform.Basis, checkpoint.StartTransform.Origin + originOffset));
        }

        return list;
    }

    private static int[] GetNextCheckpointIndexes(InfiniteCheckpoint[] checkpoints, Vector3 pos, bool inReverse) {
        var closestIndex = GetClosestToPieceIndex(checkpoints, pos);

        var offset = inReverse ? -1 : 1;
        // obvious bounds:
        if (closestIndex + offset <= 1) // too close to the start
            return [Math.Min(1, checkpoints.Length - 1)];
        if (closestIndex + offset >= checkpoints.Length - 1) //too close to the end
            return [checkpoints.Length - 1];

        var firstIndex = closestIndex;

        // figure out we are past the current checkpoint
        // we get fake an angle bisector of the 3 checkpoints around the closest one
        // then figure out which side we are closer to compared with the distance bewteen the checkpoints
        var curCheckpoint = checkpoints[closestIndex].StartTransform.Origin;
        var beforeCheckpoint = checkpoints[closestIndex + offset * -1].StartTransform.Origin;
        var afterCheckpoint = checkpoints[closestIndex + offset].StartTransform.Origin;

        var beforeDistanceDiff = beforeCheckpoint.DistanceTo(curCheckpoint) - beforeCheckpoint.DistanceTo(pos);
        var afterDistanceDiff = afterCheckpoint.DistanceTo(curCheckpoint) - afterCheckpoint.DistanceTo(pos);

        if (beforeDistanceDiff < afterDistanceDiff) {
            // this means that we have passed the closest checkpoint and need to return the next one
            firstIndex = closestIndex + offset;
        }

        var result = Enumerable.Range(firstIndex, checkpoints.Length - firstIndex).ToArray();
        if (inReverse) {
            result = Enumerable.Range(0, firstIndex + 1).Reverse().ToArray();
        }
        if (result.Length < 1) {
            GD.PushError("No checkpoints in result");
        }

        return result;
    }

    private static int GetClosestToPieceIndex(InfiniteCheckpoint[] checkpoints, Vector3 pos) {
        if (checkpoints.Length < 1) {
            return 0;
        }

        var closestDistance = float.MaxValue;
        var closestIndex = -1;

        for (int i = 0; i < checkpoints.Length; i++) {
            var checkpoint = checkpoints[i];
            var currentDistance = checkpoint.StartTransform.Origin.DistanceSquaredTo(pos);
            if (currentDistance < closestDistance) {
                closestIndex = i;
                closestDistance = currentDistance;
            }
        }

        return closestIndex;
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

    public void SetPaused(bool paused) {
        foreach (var car in _normalTraffic) {
            car.SetActive(!paused);
        }
        foreach (var car in _opponents) {
            car.SetActive(!paused);
        }
    }

    public float TotalDistanceFromCheckpoint(Vector3 position) {
        return _world.TotalDistanceFromCheckpoint(position);
    }
}
