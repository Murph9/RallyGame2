using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System;

namespace murph9.RallyGame2.godot.Cars.Sim;

public partial class Wheel : Node3D {

    public readonly WheelDetails Details;

    public readonly Car Car;
    public readonly Vector3 RayDirLocal;

    public Node3D WheelModel;

    // simulation values
    public bool InContact;
    public RigidBody3D ContactRigidBody;
    public Node3D ContactNode;
    public float SusTravelDistance;
    public Vector3 SusForce;
    public Vector3 ContactPointGlobal;
    public Vector3 ContactNormalGlobal;
    public float SwayForce;
    public float SpringForce;
    public float Damping;

    public float SlipAngle;
    public float SlipRatio;
    public float RadSec;
    public double SkidFraction;

    public double TractionControlTimeOut;
    public double ABSControlTimeOut;

    // calculation results
    public Vector3 AppliedForces;
    public Vector3 GripDir => AppliedForces / (float)Car.Details.TotalMass;

    public Wheel(Car car, WheelDetails details) {
        Car = car;
        Details = details;

        var sus = car.Details.SusByWheelNum(details.Id);
        RayDirLocal = new Vector3(0, -sus.TravelTotal() - details.Radius, 0);
    }

    public override void _Ready() {
        var scene = GD.Load<PackedScene>("res://assets/car/" + Details.ModelName);
        WheelModel = scene.Instantiate<Node3D>();
        // rotate the wheel for each side of the car
        WheelModel.Rotate(Vector3.Up, Details.Id % 2 == 1 ? Mathf.Pi : 0);
        AddChild(WheelModel);

        var sus = Car.Details.SusByWheelNum(Details.Id);
        Position = Details.Position + new Vector3(0, sus.MaxTravel, 0);
    }

    public override void _Process(double delta) {
        // rotate the model based on the rad/sec of the wheel
        WheelModel.Position = RayDirLocal - RayDirLocal.Normalized() * (Details.Radius + SusTravelDistance);
        WheelModel.Rotate(Vector3.Right, RadSec * (float)delta);
    }

    public void DoRaycast(PhysicsDirectSpaceState3D physicsState, RigidBody3D carRigidBody) {
        var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, GlobalPosition + carRigidBody.GlobalBasis * RayDirLocal);
        query.Exclude = [carRigidBody.GetRid()];
        var result = physicsState.IntersectRay(query);

        InContact = result.Count > 0;
        if (!InContact) {
            ContactPointGlobal = new Vector3();
            ContactNormalGlobal = new Vector3();
            SusTravelDistance = 0;
            ContactRigidBody = null;
            ContactNode = null;
            return;
        }

        ContactPointGlobal = (Vector3)result["position"];
        ContactNormalGlobal = (Vector3)result["normal"];
        ContactRigidBody = result["collider"].Obj as RigidBody3D;
        ContactNode = result["collider"].Obj as Node3D;

        var distance = GlobalPosition.DistanceTo(ContactPointGlobal);
        var maxDist = RayDirLocal.Length();

        SusTravelDistance = Math.Clamp(maxDist - distance, 0, maxDist);
    }
}
