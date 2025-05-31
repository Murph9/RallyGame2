using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public class MeshHelper {

    public static (Vector3, Vector3) GetBoxExtents(Vector3[] vertices) {
        var minX = float.MaxValue;
        var minZ = float.MaxValue;
        var minY = float.MaxValue;

        var maxX = float.MinValue;
        var maxZ = float.MinValue;
        var maxY = float.MinValue;
        foreach (var v in vertices) {
            minX = Mathf.Min(v.X, minX);
            minY = Mathf.Min(v.Y, minY);
            minZ = Mathf.Min(v.Z, minZ);

            maxX = Mathf.Max(v.X, maxX);
            maxY = Mathf.Max(v.Y, maxY);
            maxZ = Mathf.Max(v.Z, maxZ);
        }

        return (new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
    }
}
