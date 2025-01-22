using Godot;

namespace murph9.RallyGame2.godot.Cars.Sim;

public partial class WheelSkid(Wheel w) : Node3D {

    record SkidPos(Vector3 Left, Vector3 Right);

    private readonly Wheel _wheel = w;
    private readonly MeshInstance3D[] _instances = new MeshInstance3D[400];
    private readonly ImmediateMesh _mesh = new();

    private int _instanceIndex = 0;

    private SkidPos lastPos;

    public override void _Ready() {
        var obj = new MeshInstance3D() {
            Mesh = _mesh,
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = new Color(0, 0, 0, 0.8f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha
            }
        };
        AddChild(obj);
    }

    public override void _Process(double delta) {
        var skidding = _wheel.SkidFraction > 1;
        if (!skidding) {
            lastPos = null;
        }

        var curPos = GetWheelNow(_wheel);

        if (lastPos?.Left.DistanceSquaredTo(curPos.Left) < 9 && _wheel.Car.RigidBody.LinearVelocity.LengthSquared() > 5) {
            _mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);
            _mesh.SurfaceAddVertex(lastPos.Left + new Vector3(0, 0.01f, 0));
            _mesh.SurfaceAddVertex(lastPos.Right + new Vector3(0, 0.01f, 0));
            _mesh.SurfaceAddVertex(curPos.Left + new Vector3(0, 0.01f, 0));
            _mesh.SurfaceAddVertex(curPos.Right + new Vector3(0, 0.01f, 0));
            _mesh.SurfaceEnd();
        }
        // else start a skid (which is just set lastPos)

        var count = _mesh.GetSurfaceCount();
        if (count > 255) { //MAX_MESH_SURFACES
            _mesh.ClearSurfaces(); // TODO will look weird
        }

        lastPos = curPos;
    }

    private static SkidPos GetWheelNow(Wheel w) {
        var dir = new Basis(w.ContactNormalGlobal, Mathf.DegToRad(180)) * w.Car.RigidBody.LinearVelocity.Normalized();
        var width = w.Details.Width;

        var pos = w.ContactPointGlobal;
        var left = w.ContactNormalGlobal.Cross(dir).Normalized();

        return new SkidPos(pos + left * width / 2f, pos - left * width / 2f);
    }
}
