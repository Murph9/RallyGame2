using Godot;
using murph9.RallyGame2.Car.Init;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class Car : Node
{
    // no pacejka, please use http://www.racer.nl/reference/pacejka.htm
    // as a custom step function which looks like search "F1-2002" on page

    private readonly RigidBody3D _rigidBody;
    private readonly CarDetails _details;

    public readonly Wheel[] Wheels;

    public Car(CarDetails details) {
        _details = details;

        var uiScene = GD.Load<PackedScene>("res://CarUI.tscn");
        var instance = uiScene.Instantiate<CarUI>();
        instance.Car = this;
        AddChild(instance);

        var scene = GD.Load<PackedScene>("res://assets/" + _details.carModel);
        var carModel = scene.Instantiate<Node3D>();
        _rigidBody = carModel.GetChildren().Single(x => x is RigidBody3D) as RigidBody3D;
        _rigidBody.Mass = _details.mass;
        var parent = _rigidBody.GetParent();
        parent.RemoveChild(_rigidBody); // remove the scene parent
        parent.QueueFree();
        AddChild(_rigidBody);

        Wheels = _details.wheelData.Select(x => {
            var sus = _details.SusByWheelNum(x.i);
            return new Wheel(x) {
                Ray = new RayCast3D() {
                    Position = x.position + new Vector3(0, sus.max_travel, 0),
                    TargetPosition = new Vector3(0, -sus.TravelTotal() - x.radius, 0)
                }
            };
        }).ToArray();
        
        _rigidBody.Position = new Vector3(1, 5, 1);
        _rigidBody.Rotate(Vector3.Up, Mathf.DegToRad(135));
    }

    public override void _Ready() {
        // all 4 wheels now
        foreach (var w in Wheels) {
            _rigidBody.AddChild(w.Ray);

            var scene = GD.Load<PackedScene>("res://assets/" + w.Details.modelName);
            w.WheelModel = scene.Instantiate<Node3D>();
            if (w.Details.i % 2 == 1)
                w.WheelModel.Rotate(Vector3.Up, Mathf.DegToRad(180));
            w.WheelModel.Position = w.Details.position;
            
            _rigidBody.AddChild(w.WheelModel);
        }
    }

    public override void _Process(double delta) {
        GetViewport().GetCamera3D().LookAt(_rigidBody.GlobalPosition);

        foreach (var w in Wheels) {
            w._Process(delta);
        }
    }
    
    public override void _PhysicsProcess(double delta) {
        foreach (var w in Wheels) {
            ApplySuspension(w);
            ApplyTraction(w);
            ApplyDrag(w);
            w._PhysicsProcess(delta);
        }
        ApplyCentralDrag();
    }

    private void ApplySuspension(Wheel w)
    {
        var hitPositionGlobal = w.Ray.GetCollisionPoint();
        var hitNormalGlobal = w.Ray.GetCollisionNormal();

        w.InContact = w.Ray.IsColliding();
        if (!w.Ray.IsColliding()) {
            return;
        }
        w.ContactPoint = _rigidBody.ToLocal(hitPositionGlobal);
        
        var distance = w.Ray.GlobalPosition.DistanceTo(hitPositionGlobal);
        var maxDist = w.Ray.TargetPosition.Length();
        w.SusTravelFraction = Math.Clamp(maxDist - distance, 0, maxDist); // an offset
        
        // TODO calc other thing velocity
        var hitVelocity = _rigidBody.LinearVelocity + _rigidBody.AngularVelocity.Cross(hitPositionGlobal - _rigidBody.GlobalPosition);
        
        var relVel = hitNormalGlobal.Dot(hitVelocity);
        var susDetails = _details.SusByWheelNum(0);
        var damping = -susDetails.Relax() * relVel;
        if (relVel > 0) {
            damping = -susDetails.Compression() * relVel;
        }
        
        var totalForce = w.SusTravelFraction * susDetails.stiffness + damping;
        if (totalForce > 0) {
            // sin of angle to surface
            var rayDirectionGlobal = w.Ray.GlobalBasis * w.Ray.TargetPosition.Normalized();
            var surfaceNormalFactor = hitNormalGlobal.Dot(-rayDirectionGlobal);
            
            var forceDir = 1000 * -rayDirectionGlobal * totalForce * surfaceNormalFactor;
            _rigidBody.ApplyForce(forceDir, hitPositionGlobal - _rigidBody.GlobalPosition);
            w.Force = forceDir;
        }
    }

    private void ApplyTraction(Wheel w) {

    }
    private void ApplyDrag(Wheel w)
    {
        
    }

    private void ApplyCentralDrag() {
        
    }
}
