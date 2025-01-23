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
    IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, bool inReverse, bool leftSideOfRoad);
}

public partial class InfiniteRoadManager : Node3D, IRoadManager {

    // places world pieces based on rules
    // does traffic and stuff

    public const int MAX_TRAFFIC_COUNT = 100;

    [Signal]
    public delegate void LoadedEventHandler();
    [Signal]
    public delegate void StopCreatedEventHandler(Transform3D transform);

    private readonly InfiniteWorldPieces _world;
    private readonly List<Car> _traffic = [];
    private readonly RandomNumberGenerator _rand = new();

    public InfiniteRoadManager() {
        _world = new InfiniteWorldPieces(WorldType.Simple2, 50);
        _world.PieceAdded += PiecePlacedListener;
    }

    public override void _Ready() {
        AddChild(_world);
    }

    public override void _Process(double delta) {
        foreach (var traffic in new List<Car>(_traffic)) {
            if (traffic.RigidBody.GlobalPosition.Y + 100 < GetNextCheckpoint(traffic.RigidBody.GlobalPosition).Origin.Y) {
                _traffic.Remove(traffic);
                RemoveChild(traffic);
            }
        }
    }

    private void PiecePlacedListener(Transform3D checkpointTransform, string name, bool queuedPiece) {
        if (_traffic.Count >= MAX_TRAFFIC_COUNT) return;

        var isReverse = _rand.Randf() > 0.5f;
        var ai = new TrafficAiInputs(this, isReverse);

        var realPosition = GetNextCheckpoint(checkpointTransform.Origin, isReverse, !isReverse);
        if (isReverse) {
            realPosition = new Transform3D(realPosition.Basis.Rotated(Vector3.Up, Mathf.Pi), realPosition.Origin);
        }
        var car = new Car(CarMake.Normal.LoadFromFile(Main.DEFAULT_GRAVITY), ai, realPosition);
        car.RigidBody.Transform = realPosition;
        car.RigidBody.LinearVelocity = realPosition.Basis * Vector3.Back * ai.TargetSpeed;

        AddChild(car);
        _traffic.Add(car);

        if (queuedPiece) {
            EmitSignal(SignalName.StopCreated, checkpointTransform);
        }
    }

    public Transform3D GetInitialSpawn() => _world.GetSpawn().Transform;

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

    public Transform3D GetNextCheckpoint(Vector3 pos, bool inReverse, bool leftSide) => GetNextCheckpoints(pos, inReverse, leftSide).First();

    public IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, bool inReverse, bool leftSide) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(checkpoints, pos, inReverse);

        var list = new List<Transform3D>();
        foreach (var index in indexes) {
            var checkpoint = checkpoints[index];
            var originOffset = leftSide ? checkpoint.LeftOffset : -checkpoint.LeftOffset;
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
        }

        var reverseOffset = inReverse ? -1 : 1;

        var finalIndex = closestIndex;
        var closestTransform = checkpoints[closestIndex];
        var checkpoint = checkpoints[closestIndex + reverseOffset];
        if (checkpoint.Transform.Origin.DistanceSquaredTo(pos) < closestTransform.Transform.Origin.DistanceSquaredTo(checkpoint.Transform.Origin)) {
            finalIndex = closestIndex + reverseOffset;
        }

        var result = Enumerable.Range(finalIndex, checkpoints.Length - finalIndex).ToArray();
        if (inReverse) {
            result = Enumerable.Range(0, finalIndex + 1).Reverse().ToArray();
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

    public void TriggerStop() {
        var piece = _world.GetStraightPiece();
        _world.QueuePiece(piece);
    }
}
