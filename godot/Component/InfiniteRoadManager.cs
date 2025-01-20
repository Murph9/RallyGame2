using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public interface IRoadManager {
    Transform3D GetNextCheckpoint(Vector3 pos);
}

public partial class InfiniteRoadManager : Node3D, IRoadManager {

    // places world pieces based on rules
    // does traffic and stuff

    [Signal]
    public delegate void LoadedEventHandler();

    private readonly InfiniteWorldPieces _world;

    private List<Node3D> _traffic = [];

    public InfiniteRoadManager() {
        _world = new InfiniteWorldPieces(WorldType.Simple2);
    }

    public override void _Ready() {
        AddChild(_world);
    }

    public override void _Process(double delta) {
        while (_traffic.Count < 3) {
            var car = new Car(CarMake.Normal.LoadFromFile(Main.DEFAULT_GRAVITY), null, _world.NextPieceTransform, new TrafficCarAi(this));

            AddChild(car);
            _traffic.Add(car);
        }
    }

    public void UpdateCarPos(Vector3 pos) {
        _world.UpdateLatestPos(pos);
    }

    public Transform3D GetClosestPointTo(Vector3 pos) => _world.GetClosestPointTo(pos);

    public Transform3D GetNextCheckpoint(Vector3 pos) => _world.GetNextCheckpoint(pos, true);
}
