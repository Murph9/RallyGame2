using System.Collections.Generic;
using Godot;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot.Cars;

public partial class Wheel : Node3D {

    public readonly WheelDetails Details;
    public Node3D WheelModel;
    public RayCast3D Ray;

    public bool InContact;
    public RigidBody3D ContactRigidBody;
    public float SusTravelDistance;
    public Vector3 SusForce;
    public Vector3 ContactPoint;
    public Vector3 ContactNormal;
    public Dictionary<string, float> ExtraDetails;

    public float SlipAngle;
    public float SlipRatio;
    public float RadSec;
    public double SkidFraction;
    public Vector3 GripDir;

    public Wheel(WheelDetails details, RayCast3D ray) {
        Details = details;
        Ray = ray;
    }

    public override void _Ready()
    {
        var scene = GD.Load<PackedScene>("res://assets/" + Details.modelName);
        WheelModel = scene.Instantiate<Node3D>();
        
        if (Details.id % 2 == 1)
            WheelModel.Rotate(Vector3.Up, Mathf.DegToRad(180));
        Position = Details.position;
        AddChild(WheelModel);
    }

    public override void _PhysicsProcess(double delta) {
        if (!InContact) {
            Position = Ray.TargetPosition + Ray.Position - Ray.TargetPosition.Normalized() * Details.radius;
        } else {
            Position = ContactPoint - Ray.TargetPosition.Normalized() * Details.radius;
        }

        WheelModel.Rotate(Vector3.Right, RadSec * (float)delta);
    }
}
