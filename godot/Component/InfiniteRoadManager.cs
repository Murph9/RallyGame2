using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.World;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Component;

public interface IRoadManager {
    Transform3D GetNextCheckpoint(Vector3 pos, bool leftSideOfRoad);
    IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, int count, bool leftSideOfRoad);
}

public partial class InfiniteRoadManager : Node3D, IRoadManager {

    // places world pieces based on rules
    // does traffic and stuff

    [Signal]
    public delegate void LoadedEventHandler();

    private readonly InfiniteWorldPieces _world;

    private List<Node3D> _traffic = [];
    private Vector3 _lastKnownPlayerPos = Vector3.Zero;

    public InfiniteRoadManager() {
        _world = new InfiniteWorldPieces(WorldType.Simple2);
    }

    public override void _Ready() {
        AddChild(_world);
    }

    public override void _Process(double delta) {
        while (_traffic.Count < 1) {
            var car = new Car(CarMake.Normal.LoadFromFile(Main.DEFAULT_GRAVITY), new TrafficAiInputs(this), new Transform3D(InfiniteWorldPieces.GetSpawn().Basis, _lastKnownPlayerPos + new Vector3(0, 0, 4)));

            AddChild(car);
            _traffic.Add(car);
        }
    }

    public void UpdateCarPos(Vector3 pos) {
        _lastKnownPlayerPos = pos;
        _world.UpdateLatestPos(pos);
    }

    public Transform3D GetClosestPointTo(Vector3 pos) => _world.GetClosestPointTo(pos);

    public Transform3D GetNextCheckpoint(Vector3 pos, bool leftSideOfRoad) => _world.GetNextCheckpoint(pos, leftSideOfRoad);

    public IReadOnlyCollection<Transform3D> GetNextCheckpoints(Vector3 pos, int count, bool leftSideOfRoad) => _world.GetNextCheckpoints(pos, count, leftSideOfRoad);
}
