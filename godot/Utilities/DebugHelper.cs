using Godot;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities;

public class DebugHelper {

    public static WorldText GenerateWorldText(string text, Vector3 position) {
        var uiScene = GD.Load<PackedScene>("res://Utilities/WorldText.tscn");
        var instance = uiScene.Instantiate<WorldText>();
        instance.Position = position;
        instance.SetText(text);
        return instance;
    }

    public static bool IsNumeric(object o) {
        var numType = typeof(System.Numerics.INumber<>);
        return o.GetType().GetInterfaces().Any(iface =>
            iface.IsGenericType && (iface.GetGenericTypeDefinition() == numType));
    }

    public static Node3D GenerateArrow(Color colour, Transform3D transform, float length, float width) {
        var node = new Node3D() {
            Transform = transform
        };

        node.AddChild(BoxLine(colour, new Vector3(0, 0, 0), new Vector3(length, 0, 0)));
        node.AddChild(Box(colour, new Vector3(length, 0, 0), width));
        return node;
    }

    public static MeshInstance3D BoxLine(Color c, Vector3 start, Vector3 end, float size = 0.1f) {
        var mat = new StandardMaterial3D() {
            AlbedoColor = c
        };
        var length = (end - start).Length();

        var mesh = new BoxMesh() {
            Size = new Vector3(length, size, size),
            Material = mat
        };

        return new MeshInstance3D() {
            Transform = new Transform3D(new Basis(new Quaternion(new Vector3(1, 0, 0), (end - start).Normalized())), start.Lerp(end, 0.5f)),
            Mesh = mesh
        };
    }

    public static MeshInstance3D Box(Color c, Vector3 pos, float width = 1) {
        var mat = new StandardMaterial3D() {
            AlbedoColor = c,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        var mesh = new BoxMesh() {
            Size = new Vector3(width, width, width),
            Material = mat
        };

        return new MeshInstance3D() {
            Transform = new Transform3D(Basis.Identity, pos),
            Mesh = mesh
        };
    }

    public static MeshInstance3D Sphere(Color c, Vector3 pos, float radius) {
        var mat = new StandardMaterial3D() {
            AlbedoColor = c,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        var mesh = new SphereMesh() {
            Radius = 1,
            Height = 1,
            Material = mat
        };

        return new MeshInstance3D {
            Transform = new Transform3D(Basis.Identity, pos),
            Mesh = mesh,
            Scale = new Vector3(radius, radius, radius)
        };
    }
}
