using System.Collections.Generic;
using Godot;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot.Cars;

public partial class Wheel : Node3D {

    public readonly WheelDetails Details;
    public readonly Car Car;
    public Node3D WheelModel;
    public RayCast3D Ray;

    // simulation values
    public bool InContact;
    public RigidBody3D ContactRigidBody;
    public float SusTravelDistance;
    public Vector3 SusForce;
    public Vector3 ContactPoint;
    public Vector3 ContactNormal;
    public float SwayForce;
    public float SpringForce;
    public float Damping;

    public float SlipAngle;
    public float SlipRatio;
    public float RadSec;
    public double SkidFraction;
    public Vector3 GripDir;

    public Dictionary<string, float> ExtraDetails;

    public Wheel(Car car, WheelDetails details, RayCast3D ray) {
        Car = car;
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

    public override void _Process(double delta) {
        Position = Ray.Position + Ray.TargetPosition - Ray.TargetPosition.Normalized() * (Details.radius + SusTravelDistance);
        WheelModel.Rotate(Vector3.Right, RadSec * (float)delta);
    }
}
