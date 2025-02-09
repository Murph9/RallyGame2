using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.World;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public interface IRoadManager {
    Transform3D GetPassedCheckpoint(Vector3 pos);
    IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, bool inReverse = false, int positionIndex = 0);
}

public partial class InfiniteRoadManager : Node3D, IRoadManager {

    // Makes a world based on the infinite world pieces
    // does traffic and stuff

    public const int MAX_TRAFFIC_COUNT = 10;
    public const int RIVAL_MAX_COUNT = 3;
    public const float OPPONENT_SPAWN_BUFFER_DISTANCE = 250;

    [Signal]
    public delegate void LoadedEventHandler();
    [Signal]
    public delegate void ShopPlacedEventHandler(Transform3D transform);

    private readonly InfiniteWorldPieces _world;
    private readonly List<Car> _normalTraffic = [];
    private readonly List<Car> _opponents = [];
    private readonly RandomNumberGenerator _rand = new();

    public InfiniteRoadManager(int spawnDistance) {
        _world = new InfiniteWorldPieces(WorldType.Simple2, spawnDistance);
        _world.PieceAdded += PiecePlacedListener;
        _world.IgnoredList.Add("station");
    }

    public override void _Ready() {
        AddChild(_world);
    }

    public override void _Process(double delta) {
        foreach (var traffic in new List<Car>(_normalTraffic)) {
            // find cars which are greatly below their next checkpoint to kill them
            if (traffic.RigidBody.GlobalPosition.Y + 100 < GetNextCheckpoint(traffic.RigidBody.GlobalPosition).Origin.Y) {
                _normalTraffic.Remove(traffic);
                RemoveChild(traffic);
            }
        }

        foreach (var opponent in new List<Car>(_opponents)) {
            var nextOpponentCheckpoint = GetNextCheckpoint(opponent.RigidBody.GlobalPosition);
            var removeForFallingOff = opponent.RigidBody.GlobalPosition.Y + 100 < nextOpponentCheckpoint.Origin.Y;
            var removeForBeingFarBehind = false; // TODO do we just remove old roads and let them fall?
            if (removeForFallingOff || removeForBeingFarBehind) {
                _opponents.Remove(opponent);
                RemoveChild(opponent);
            }
        }

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

        // give them basic ai for now
        var ai = new StopAiInputs(this);
        var car = new Car(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY), ai, position);
        car.RigidBody.LinearVelocity = position.Basis * Vector3.Back * 10; // TODO

        AddChild(car);
        _opponents.Add(car);

        GD.Print("Spawned rival at " + position);
    }

    private void PiecePlacedListener(Transform3D checkpointTransform, string name, bool queuedPiece) {
        TryGenerateTrafficCarNow(checkpointTransform);

        if (queuedPiece) {
            EmitSignal(SignalName.ShopPlaced, checkpointTransform);
        }
    }

    private bool TryGenerateTrafficCarNow(Transform3D spawnTransform) {
        if (_normalTraffic.Count >= MAX_TRAFFIC_COUNT) return false;

        var isReverse = _rand.Randf() > 0.5f;
        var ai = new TrafficAiInputs(this, isReverse);

        var realPosition = GetNextCheckpoint(spawnTransform.Origin, isReverse, isReverse ? -1 : 1);
        if (isReverse) {
            realPosition = new Transform3D(realPosition.Basis.Rotated(Vector3.Up, Mathf.Pi), realPosition.Origin);
        }
        var car = new Car(CarMake.Normal.LoadFromFile(Main.DEFAULT_GRAVITY), ai, realPosition);
        // car.RigidBody.Transform = realPosition;
        car.RigidBody.LinearVelocity = realPosition.Basis * Vector3.Back * ai.TargetSpeed;

        AddChild(car);
        _normalTraffic.Add(car);

        return true;
    }

    public Transform3D GetInitialSpawn() => _world.GetInitialSpawn().Transform;

    public Transform3D GetPassedCheckpoint(Vector3 pos) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var index = GetClosestToPieceIndex(checkpoints, pos);
        return checkpoints[Math.Max(0, index - 1)].Transform;
    }

    public Transform3D GetNextCheckpoint(Vector3 pos) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(checkpoints, pos, false);
        return checkpoints[indexes.First()].Transform;
    }

    public Transform3D GetNextCheckpoint(Vector3 pos, bool inReverse, int positionIndex) => GetNextCheckpoints(pos, inReverse, positionIndex).First();

    public IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, bool inReverse, int positionIndex) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(checkpoints, pos, inReverse);

        var list = new List<Transform3D>();
        foreach (var index in indexes) {
            var checkpoint = checkpoints[index];
            var originOffset = checkpoint.LeftOffset * positionIndex;
            list.Add(new Transform3D(checkpoint.Transform.Basis, checkpoint.Transform.Origin + originOffset));
        }

        return list;
    }

    private static int[] GetNextCheckpointIndexes(InfiniteCheckpoint[] checkpoints, Vector3 pos, bool inReverse) {
        var closestIndex = GetClosestToPieceIndex(checkpoints, pos);

        if (inReverse) {
            if (closestIndex <= 0) {
                return [0];
            }
        } else if (closestIndex >= checkpoints.Length - 1) {
            return [closestIndex];
        } else if (closestIndex <= 1) {
            return [closestIndex + 1]; // its the next one
        }

        var reverseOffset = inReverse ? -1 : 1;

        var firstIndex = closestIndex;

        // figure out we are past the current checkpoint
        // we get fake an angle bisector of the 3 checkpoints around the closest one
        // then figure out which side we are closer to compared with the distance bewteen the checkpoints
        var curCheckpoint = checkpoints[closestIndex].Transform.Origin;
        var beforeCheckpoint = checkpoints[closestIndex + reverseOffset * -1].Transform.Origin;
        var afterCheckpoint = checkpoints[closestIndex + reverseOffset].Transform.Origin;

        var beforeDistanceDiff = beforeCheckpoint.DistanceTo(curCheckpoint) - beforeCheckpoint.DistanceTo(pos);
        var afterDistanceDiff = afterCheckpoint.DistanceTo(curCheckpoint) - afterCheckpoint.DistanceTo(pos);

        if (beforeDistanceDiff < afterDistanceDiff) {
            // this means that we have passed the closest checkpoint and need to return the next one
            firstIndex = closestIndex + reverseOffset;
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
            var currentDistance = checkpoint.Transform.Origin.DistanceSquaredTo(pos);
            if (currentDistance < closestDistance) {
                closestIndex = i;
                closestDistance = currentDistance;
            }
        }

        return closestIndex;
    }

    public void TriggerShop() {
        _world.QueuePiece("station");
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
}
