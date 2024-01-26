using Godot;

namespace murph9.RallyGame2.godot.Cars.Sim;

public partial class WheelSkid(Wheel w) : Node3D {

    record SkidPos(Vector3 Left, Vector3 Right);

    private readonly Wheel _wheel = w;
    private readonly MeshInstance3D[] _instances = new MeshInstance3D[400];
    private int _instanceIndex = 0;

    private SkidPos lastPos;

    public override void _Ready() {
        base._Ready();
    }

    public override void _Process(double delta) {
        base._Process(delta);

        var skidding = _wheel.SkidFraction > 1;
        if (!skidding) {
            lastPos = null;
        }

        var curPos = GetWheelNow(_wheel);

        if (lastPos?.Left.DistanceSquaredTo(curPos.Left) < 9 && _wheel.Car.RigidBody.LinearVelocity.LengthSquared() > 5) {
            var quad = new ImmediateMesh();
            quad.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);
            quad.SurfaceAddVertex(lastPos.Left + new Vector3(0, 0.01f, 0));
            quad.SurfaceAddVertex(lastPos.Right + new Vector3(0, 0.01f, 0));
            quad.SurfaceAddVertex(curPos.Left + new Vector3(0, 0.01f, 0));
            quad.SurfaceAddVertex(curPos.Right + new Vector3(0, 0.01f, 0));
            quad.SurfaceEnd();

            var obj = new MeshInstance3D() {
                Mesh = quad,
                MaterialOverride = new StandardMaterial3D() {
                    AlbedoColor = new Color(0, 0, 0, 0.8f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha
                }
            };
            AddChild(obj);

            if (_instances[_instanceIndex] != null) {
                RemoveChild(_instances[_instanceIndex]);
                _instances[_instanceIndex].QueueFree();
            }
            _instances[_instanceIndex] = obj;
            _instanceIndex = (_instanceIndex + 1) % _instances.Length;
        }
        // else start a skid (which is just set lastPos)

        lastPos = curPos;
    }

    private static SkidPos GetWheelNow(Wheel w) {
        var dir = new Basis(w.ContactNormalGlobal, Mathf.DegToRad(180)) * w.Car.RigidBody.LinearVelocity.Normalized();
        var width = w.Details.width;

        var pos = w.ContactPointGlobal;
        var left = w.ContactNormalGlobal.Cross(dir).Normalized();

        return new SkidPos(pos + left * width/2f, pos - left * width/2f);
    }
}
