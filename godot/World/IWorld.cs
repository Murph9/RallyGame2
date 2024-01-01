using Godot;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.World;

public interface IWorld {
    Transform3D GetSpawn();
    IEnumerable<Vector3> GetCheckpoints();
}
