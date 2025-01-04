using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World;
using System;

namespace murph9.RallyGame2.godot.Component;

public partial class InfiniteRoadManager : Node3D {

    // places world pieces based on rules

    [Signal]
    public delegate void LoadedEventHandler();
    
    private readonly InfiniteWorldPieces _world;

    public InfiniteRoadManager() {
        _world = new InfiniteWorldPieces(WorldType.Simple);
    }

    public override void _Ready() {
        AddChild(_world);
    }

    public void UpdateCarPos(Vector3 pos) {
        _world.UpdateLatestPos(pos);
    }

    public Transform3D GetClosestPointTo(Vector3 pos) => _world.GetClosestPointTo(pos);
}
