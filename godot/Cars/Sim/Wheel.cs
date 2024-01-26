using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System;

namespace murph9.RallyGame2.godot.Cars.Sim;

public partial class Wheel : Node3D {

    public readonly WheelDetails Details;
    private readonly WheelSkid _skid;

    public readonly Car Car;
    public readonly Vector3 RayDir;
    private Vector3 RayDirInGlobal => GlobalBasis * RayDir;

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
    public float SlipAngleLast;
    public float SlipRatioLast;
    public float RadSec;
    public double SkidFraction;
    public Vector3 GripDir;

    public double TractionControlTimeOut;

    public Wheel(Car car, WheelDetails details) {
        Car = car;
        Details = details;
        _skid = new WheelSkid(this);

        var sus = car.Details.SusByWheelNum(details.Id);
        RayDir = new Vector3(0, -sus.TravelTotal() - details.Radius, 0);
    }

    public override void _Ready() {
        var scene = GD.Load<PackedScene>("res://assets/" + Details.ModelName);
        WheelModel = scene.Instantiate<Node3D>();
        WheelModel.Rotate(Vector3.Up, Details.Id % 2 == 1 ? Mathf.Pi : 0);
        AddChild(WheelModel);

        var sus = Car.Details.SusByWheelNum(Details.Id);
        Position = Details.Position + new Vector3(0, sus.MaxTravel, 0);

        Car.AddChild(_skid);
    }

    public override void _Process(double delta) {
        WheelModel.Position = RayDirInGlobal - RayDirInGlobal.Normalized() * (Details.Radius + SusTravelDistance);
        WheelModel.Rotate(Vector3.Right, RadSec * (float)delta);
    }

    public void DoRaycast(PhysicsDirectSpaceState3D physicsState, RigidBody3D carRigidBody) {
        var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, GlobalPosition + RayDirInGlobal);
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
        var maxDist = RayDir.Length();

        SusTravelDistance = Math.Clamp(maxDist - distance, 0, maxDist);
    }
}
