using Godot;
using murph9.RallyGame2.godot.Cars.Sim;

// using murph9.RallyGame2.godot.Cars.Sim;

using murph9.RallyGame2.godot.Component.Traffic;
using murph9.RallyGame2.godot.World;
using murph9.RallyGame2.godot.World.Procedural;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public interface IRoadManager {
    Transform3D GetPassedCheckpoint(Vector3 pos);
    Transform3D GetNextCheckpoint(Vector3 pos, bool inReverse = false, int positionIndex = 0);
    IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, bool inReverse = false, int positionIndex = 0);
    float CurrentRoadWidth { get; }

}

public partial class InfiniteRoadManager : Node3D, IRoadManager {

    // Makes a world based on the infinite world pieces

    [Signal]
    public delegate void LoadedEventHandler();
    [Signal]
    public delegate void RoadNextPointEventHandler(float totalDistance, Transform3D transform);

    private readonly InfiniteWorldPieces _world;
    private readonly TrafficManager _trafficManager;
    private readonly RandomNumberGenerator _rand = new();
    private bool _paused;

    public WorldType CurrentWorldType => _world.CurrentWorldType;
    public int PiecesPlaced { get; private set; }
    public float CurrentRoadWidth { get; private set; }

    public InfiniteRoadManager(float spawnDistance, WorldType initialWorldType, float removePieceDistance) {
        UpdateWorldType(initialWorldType);

        var strat = new PiecePlacementStrategy(PiecePlacementStrategy.Type.Camera, spawnDistance);

        _world = new InfiniteWorldPieces(new ProceduralPieceGenerator(initialWorldType), strat, new PieceDecorator(), removePieceDistance);
        _world.PieceAdded += PiecePlacedListener;
        _world.SetIgnoredPieces(["station"]);

        CurrentRoadWidth = _world.GetRoadWidth();

        _trafficManager = new TrafficManager(this);
        AddChild(_trafficManager);
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

    private void PiecePlacedListener(Transform3D checkpointTransform) {
        PiecesPlaced++;

        EmitSignal(SignalName.RoadNextPoint, _world.TotalDistanceFromCheckpoint(checkpointTransform.Origin), checkpointTransform);
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

    public Car GetClosestOpponent(Vector3 pos) => _trafficManager.GetClosestOpponent(pos);

    public void SetPaused(bool paused) {
        _paused = paused;
        _trafficManager.SetPaused(paused);
    }

    public float TotalDistanceFromCheckpoint(Vector3 position) {
        return _world.TotalDistanceFromCheckpoint(position);
    }
}
