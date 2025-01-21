using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public interface IRoadManager {
    Transform3D GetLastCheckpoint(Vector3 pos);
    Transform3D GetNextCheckpoint(Vector3 pos, bool leftSideOfRoad);
    IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, int count, bool leftSideOfRoad);
}

public partial class InfiniteRoadManager : Node3D, IRoadManager {

    // places world pieces based on rules
    // does traffic and stuff

    [Signal]
    public delegate void LoadedEventHandler();

    private readonly InfiniteWorldPieces _world;
    private readonly List<Car> _traffic = [];

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

    private void PiecePlacedListener(Transform3D checkpointTransform) {
        if (_traffic.Count >= 10) return;

        var ai = new TrafficAiInputs(this);
        var car = new Car(CarMake.Normal.LoadFromFile(Main.DEFAULT_GRAVITY), ai, checkpointTransform);
        car.RigidBody.Transform = checkpointTransform;
        car.RigidBody.LinearVelocity = checkpointTransform.Basis * Vector3.Back * ai.TargetSpeed;

        AddChild(car);
        _traffic.Add(car);
    }

    public Transform3D GetInitialSpawn() => _world.GetSpawn().Transform;

    public Transform3D GetLastCheckpoint(Vector3 pos) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var index = GetClosestToPieceIndex(checkpoints, pos);
        GD.Print(index);
        return checkpoints[Math.Max(0, index - 1)].Transform;
    }

    public Transform3D GetNextCheckpoint(Vector3 pos) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(checkpoints, pos);
        return checkpoints[indexes.First()].Transform;
    }

    public Transform3D GetNextCheckpoint(Vector3 pos, bool leftSide) => GetNextCheckpoints(pos, 1, leftSide).First();

    public IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, int count, bool leftSide) {
        var checkpoints = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(checkpoints, pos);

        var list = new List<Transform3D>();
        foreach (var index in indexes) {
            var checkpoint = checkpoints[index];
            // yes it looks like the left side is wrong here
            var originOffset = leftSide ? checkpoint.LeftOffset : -checkpoint.LeftOffset;
            list.Add(new Transform3D(checkpoint.Transform.Basis, checkpoint.Transform.Origin + originOffset));
        }
        if (list.Count < 1) {
            GD.Print("No checkpoints found");
        }
        return list;
    }

    private static int[] GetNextCheckpointIndexes(InfiniteCheckpoint[] checkpoints, Vector3 pos) {
        var closestIndex = GetClosestToPieceIndex(checkpoints, pos);

        if (closestIndex >= checkpoints.Length - 1) {
            return [closestIndex];
        }

        var finalIndex = closestIndex;
        var closestTransform = checkpoints[closestIndex];
        var checkpoint = checkpoints[closestIndex + 1];
        if (checkpoint.Transform.Origin.DistanceSquaredTo(pos) < closestTransform.Transform.Origin.DistanceSquaredTo(checkpoint.Transform.Origin)) {
            finalIndex = closestIndex + 1;
        }

        return Enumerable.Range(finalIndex, checkpoints.Length - finalIndex).ToArray();
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
}
