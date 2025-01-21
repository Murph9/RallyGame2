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
            if (traffic.RigidBody.GlobalPosition.Y < GetNextCheckpoint(traffic.RigidBody.GlobalPosition).Origin.Y) {
                _traffic.Remove(traffic);
                RemoveChild(traffic);
            }
        }
    }

    private void PiecePlacedListener(Transform3D checkpointTransform) {
        if (_traffic.Count >= 10) return;

        var car = new Car(CarMake.Normal.LoadFromFile(Main.DEFAULT_GRAVITY), new TrafficAiInputs(this), checkpointTransform);
        car.RigidBody.Transform = checkpointTransform;

        AddChild(car);
        _traffic.Add(car);
    }

    public Transform3D GetInitialSpawn() => _world.GetSpawn();

    public Transform3D GetLastCheckpoint(Vector3 pos) {
        var pieces = _world.GetAllCurrentCheckpoints().ToArray();
        var index = GetClosestToPieceIndex(pieces, pos);
        GD.Print(index);
        return pieces[Math.Max(0, index - 1)];
    }

    public Transform3D GetNextCheckpoint(Vector3 pos) {
        var pieces = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(pieces, pos);
        return pieces[indexes.First()];
    }

    public Transform3D GetNextCheckpoint(Vector3 pos, bool leftSide) => GetNextCheckpoints(pos, 1, leftSide).First();

    public IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, int count, bool leftSide) {
        var pieces = _world.GetAllCurrentCheckpoints().ToArray();
        var indexes = GetNextCheckpointIndexes(pieces, pos);

        var list = new List<Transform3D>();
        foreach (var index in indexes) {
            var transform = pieces[index];
            // yes it looks like the left side is wrong here
            var originOffset = transform.Basis * (leftSide ? -_world.TrafficLeftSideOffset : _world.TrafficLeftSideOffset);
            list.Add(new Transform3D(transform.Basis, transform.Origin + originOffset));
        }
        if (list.Count < 1) {
            GD.Print("No checkpoints found");
        }
        return list;
    }

    private static int[] GetNextCheckpointIndexes(Transform3D[] pieces, Vector3 pos) {
        var closestIndex = GetClosestToPieceIndex(pieces, pos);

        if (closestIndex >= pieces.Length - 1) {
            return [closestIndex];
        } else if (closestIndex >= pieces.Length - 2) {
            return [closestIndex, closestIndex + 1];
        }

        var finalIndex = closestIndex;
        var closestTransform = pieces[closestIndex];
        var nextTransform = pieces[closestIndex + 1];
        if (nextTransform.Origin.DistanceSquaredTo(pos) < closestTransform.Origin.DistanceSquaredTo(nextTransform.Origin)) {
            finalIndex = closestIndex + 1;
        }

        return Enumerable.Range(finalIndex, pieces.Length - 1 - finalIndex).ToArray();
    }

    private static int GetClosestToPieceIndex(Transform3D[] list, Vector3 pos) {
        if (list.Length < 1) {
            return 0;
        }

        var closestDistance = float.MaxValue;
        var closestIndex = -1;

        for (int i = 0; i < list.Length; i++) {
            var point = list[i];
            var currentDistance = point.Origin.DistanceSquaredTo(pos);
            if (currentDistance < closestDistance) {
                closestIndex = i;
                closestDistance = currentDistance;
            }
        }

        return closestIndex;
    }
}
