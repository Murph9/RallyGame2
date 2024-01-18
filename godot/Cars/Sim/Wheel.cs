using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.Sim;

public partial class Wheel : Node3D {

    public readonly WheelDetails Details;
    public readonly Car Car;
    public readonly Vector3 RayStart;
    public readonly Vector3 RayDir;
    public readonly RayCast3D Ray;
    public Node3D WheelModel;

    // simulation values
    public bool InContact;
    public RigidBody3D ContactRigidBody;
    public float SusTravelDistance;
    public Vector3 SusForce;
    public Vector3 ContactPointGlobal;
    public Vector3 ContactNormalGlobal;
    public float SwayForce;
    public float SpringForce;
    public float Damping;

    public float SlipAngle;
    public float SlipRatio;
    public float SlipAngleLast;
    public float SlipRatioLast;
    public float RadSec;
    public double SkidFraction;
    public Vector3 GripDir;

    public Wheel(Car car, WheelDetails details, RayCast3D ray) {
        Car = car;
        Details = details;
        Ray = ray;

        var sus = car.Details.SusByWheelNum(details.id);
        RayStart = details.position + new Vector3(0, sus.maxTravel, 0);
        RayDir = new Vector3(0, -sus.TravelTotal() - details.radius, 0);
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

    public void DoRaycast(PhysicsDirectSpaceState3D physicsState, RigidBody3D carRigidBody) {
        var query = PhysicsRayQueryParameters3D.Create(Ray.GlobalPosition, Ray.GlobalPosition + Ray.TargetPosition);
        query.Exclude = new Godot.Collections.Array<Rid> { carRigidBody.GetRid() };
        var result = physicsState.IntersectRay(query);

        InContact = result.Any();
        if (!InContact) {
            ContactPointGlobal = new Vector3();
            ContactNormalGlobal = new Vector3();
            SusTravelDistance = 0;
            ContactRigidBody = null;
            return;
        }

        ContactPointGlobal = (Vector3)result["position"];
        ContactNormalGlobal = (Vector3)result["normal"];
        ContactRigidBody = result["collider"].Obj as RigidBody3D;

        var distance = Ray.GlobalPosition.DistanceTo(ContactPointGlobal);
        var maxDist = Ray.TargetPosition.Length();

        SusTravelDistance = Math.Clamp(maxDist - distance, 0, maxDist);
    }
}
