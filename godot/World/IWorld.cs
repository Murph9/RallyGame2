using Godot;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.World;

public interface IWorld {
    Transform3D GetSpawn();
    IEnumerable<Transform3D> GetCheckpoints();
    IEnumerable<Curve3DPoint> GetCurve3DPoints();
}

public record Curve3DPoint(Vector3 Point, Vector3? PIn, Vector3? POut);