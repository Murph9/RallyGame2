using Godot;

namespace murph9.RallyGame2.godot.Utilities.Debug3D;

public partial class LineDebug3D : Node3D {

    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
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

        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        mesh.SurfaceSetColor(Colour);
        mesh.SurfaceAddVertex(Start);
        mesh.SurfaceAddVertex(End);
        mesh.SurfaceEnd();
    }

}