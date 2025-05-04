using Godot;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.World;

public interface IWorld {
    InfiniteCheckpoint GetInitialSpawn();
    IEnumerable<InfiniteCheckpoint> GetAllCurrentCheckpoints();
    IEnumerable<Curve3DPoint> GetCurve3DPoints();
}

public record Curve3DPoint(Vector3 Point, Vector3? PIn, Vector3? POut);