using Godot;
using murph9.RallyGame2.Car.Init;

namespace murph9.RallyGame2.godot;

public partial class Wheel : Node3D {

    public readonly WheelDetails Details;
    public Node3D WheelModel;
    public RayCast3D Ray;

    public bool InContact;
    public RigidBody3D ContactRigidBody;
    public float SusTravelFraction;
    public Vector3 SusForce;
    public Vector3 ContactPoint;

    public float SlipAngle;
    public float SlipRatio;
    public float RadSec;
    public double SkidFraction;
    public Vector3 GripDir;

    public Wheel(WheelDetails details) {
        Details = details;
    }

    public override void _Process(double delta) {
        if (!InContact) {
            WheelModel.Position = Ray.TargetPosition + Ray.Position - Ray.TargetPosition.Normalized() * 0.4f;
        } else {
            WheelModel.Position = ContactPoint - Ray.TargetPosition.Normalized() * 0.4f;
        }
    }

    public override void _PhysicsProcess(double delta) {}
}
