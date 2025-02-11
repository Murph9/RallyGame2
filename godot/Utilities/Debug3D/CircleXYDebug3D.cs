using System;
using Godot;

namespace murph9.RallyGame2.godot.Utilities.Debug3D;

public partial class CircleXYDebug3D : Node3D {

    public Vector3 Center { get; set; }
    public int VertexCount { get; set; }
    public float Radius { get; set; }
    public Color Colour { get; set; }

    private ImmediateMesh mesh;
    private MeshInstance3D meshInstance;

    public override void _Ready() {
        mesh = new ImmediateMesh();
        var material = new StandardMaterial3D {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            NoDepthTest = true,
            VertexColorUseAsAlbedo = true,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        meshInstance = new MeshInstance3D {
            Mesh = mesh,
            MaterialOverride = material
        };
        AddChild(meshInstance);
    }

    public override void _Process(double delta) {
        mesh.ClearSurfaces();

        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        mesh.SurfaceSetColor(Colour);
        for (var i = 0; i < VertexCount + 1; i++) {
            var pos = Center + new Vector3(Mathf.Sin((float)i / VertexCount * Mathf.Pi * 2), 0, Mathf.Cos((float)i / VertexCount * Mathf.Pi * 2)) * Radius;
            mesh.SurfaceAddVertex(pos);
        }
        mesh.SurfaceEnd();
    }

}