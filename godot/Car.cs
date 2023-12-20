using Godot;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class Car : Node
{
    // no pacejka, please use http://www.racer.nl/reference/pacejka.htm
    // as a custom step function which looks like search "F1-2002" on page

    private static readonly float SUS_STIFFNESS = 10;
    private static readonly float SUS_REBOUND = 0.5f;
    private static readonly float SUS_COMPRESSION = 0.6f;
    private static readonly float MASS = 750;

    private readonly RigidBody3D _rigidBody;

    public readonly Wheel[] Wheels;

    public Car() {
        var uiScene = GD.Load<PackedScene>("res://CarUI.tscn");
        var instance = uiScene.Instantiate<CarUI>();
        instance.Car = this;
        AddChild(instance);

        var scene = GD.Load<PackedScene>("res://assets/track1_2.blend");
        var carModel = scene.Instantiate<Node3D>();
        _rigidBody = carModel.GetChildren().Single(x => x is RigidBody3D) as RigidBody3D;
        _rigidBody.Mass = MASS;
        var parent = _rigidBody.GetParent();
        parent.RemoveChild(_rigidBody); // remove the scene parent
        parent.QueueFree();
        AddChild(_rigidBody);

        Wheels = carModel.GetChildren().Where(x => x is Node3D).Select(x => {
            var node = x as Node3D;
            return new Wheel {
                Name = node.Name,
                Ray = new RayCast3D() {
                    Position = node.Position,
                    TargetPosition = new Vector3(0, -0.5f, 0)
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

            var scene = GD.Load<PackedScene>("res://assets/wheel1.blend");
            w.WheelModel = scene.Instantiate<Node3D>();
            w.WheelModel.Position = w.Ray.Position;
            
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
            ApplyDrag(w);
            w._PhysicsProcess(delta);
        }
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
        
        var rayDirectionGlobal = w.Ray.GlobalBasis * w.Ray.TargetPosition;
        var surfaceNormalFactor = hitNormalGlobal.Dot(-rayDirectionGlobal);

        var distance = w.Ray.GlobalPosition.DistanceTo(hitPositionGlobal);
        var maxDist = w.Ray.TargetPosition.Length();
        w.SusTravelFraction = Math.Clamp(maxDist - distance, 0, maxDist); // an offset
        var hitVelocity = _rigidBody.LinearVelocity + _rigidBody.AngularVelocity.Cross(hitPositionGlobal - _rigidBody.GlobalPosition); // TODO calc other model speed
        
        var relVel = hitNormalGlobal.Dot(hitVelocity);
        var damping = -SUS_REBOUND;
        if (relVel > 0) {
            damping = -SUS_COMPRESSION;
        }
        
        var totalForce = w.SusTravelFraction + damping * relVel;
        if (totalForce > 0) {
            var forceDir = SUS_STIFFNESS * _rigidBody.Mass * -rayDirectionGlobal * totalForce * surfaceNormalFactor;
            _rigidBody.ApplyForce(forceDir, hitPositionGlobal - _rigidBody.GlobalPosition);
            w.Force = forceDir;
        }
    }

    private void ApplyDrag(Wheel w)
    {
        
    }
}
