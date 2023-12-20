using Godot;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class Car : Node
{
    // no pacejka, please use http://www.racer.nl/reference/pacejka.htm
    // as a custom step function which looks like search "F1-2002" on page

    private static readonly float SUS_STIFFNESS = 20;
    private static readonly float SUS_REBOUND = 0.2f;
    private static readonly float SUS_COMPRESSION = 0.4f;
    private static readonly float MASS = 750;

    class Wheel {
        public Node3D WheelModel;
        public RayCast3D Ray;
    }

    private readonly RigidBody3D _rigidBody;

    private readonly Wheel[] _wheels;

    public Car() {
        var scene = GD.Load<PackedScene>("res://assets/track1_2.blend");
        var carModel = scene.Instantiate<Node3D>();
        _rigidBody = carModel.GetChildren().Single(x => x is RigidBody3D) as RigidBody3D;
        _rigidBody.Mass = MASS;
        _rigidBody.GetParent().RemoveChild(_rigidBody); // remove the scene parent
        AddChild(_rigidBody);

        _wheels = carModel.GetChildren().Where(x => x is Node3D).Select(x => {
            var node = x as Node3D;
            return new Wheel {
                Ray = new RayCast3D() {
                    Position = node.Position,
                    TargetPosition = new Vector3(0, -1f, 0)
                }
            };
        }).ToArray();
        
        _rigidBody.Position = new Vector3(1, 5, 1);
        _rigidBody.Rotate(Vector3.Up, Mathf.DegToRad(135));
    }

    public override void _Ready() {
        // all 4 wheels now
        foreach (var w in _wheels) {
            _rigidBody.AddChild(w.Ray);

            var scene = GD.Load<PackedScene>("res://assets/wheel1.blend");
            w.WheelModel = scene.Instantiate<Node3D>();
            w.WheelModel.Position = w.Ray.Position;
            
            _rigidBody.AddChild(w.WheelModel);
        }
    }

    public override void _Process(double delta) {
        GetViewport().GetCamera3D().LookAt(_rigidBody.GlobalPosition);
    }
    
    public override void _PhysicsProcess(double delta) {
        foreach (var w in _wheels) {
            ApplySuspension(w);
        }
        foreach (var w in _wheels) {
            ApplyDrag(w);
        }
    }

    private void ApplySuspension(Wheel w)
    {
        var hitPositionGlobal = w.Ray.GetCollisionPoint();
        var hitNormalGlobal = w.Ray.GetCollisionNormal();
        if (!w.Ray.IsColliding()) {
            w.WheelModel.Position = w.Ray.TargetPosition + w.Ray.Position - w.Ray.TargetPosition.Normalized() * 0.4f;
            return;
        }
        
        w.WheelModel.Position = _rigidBody.ToLocal(hitPositionGlobal) - w.Ray.TargetPosition.Normalized() * 0.4f;

        var rayDirectionGlobal = w.Ray.GlobalBasis * w.Ray.TargetPosition;
        var surfaceNormalFactor = hitNormalGlobal.Dot(-rayDirectionGlobal);

        var distance = w.Ray.GlobalPosition.DistanceTo(hitPositionGlobal);
        var force = Math.Max(0, 1 - distance); // an offset
        var hitVelocity = _rigidBody.LinearVelocity + _rigidBody.AngularVelocity.Cross(hitPositionGlobal - _rigidBody.GlobalPosition); // TODO calc other model speed
        
        var relVel = hitNormalGlobal.Dot(hitVelocity);
        var damping = -SUS_REBOUND;
        if (relVel > 0) {
            damping = -SUS_COMPRESSION;
        }
        
        var totalForce = force + damping * relVel;
        if (totalForce > 0) {
            var forceDir = -rayDirectionGlobal * totalForce * surfaceNormalFactor;
            _rigidBody.ApplyForce(SUS_STIFFNESS * _rigidBody.Mass * forceDir, hitPositionGlobal - _rigidBody.GlobalPosition);
        }
    }

    private void ApplyDrag(Wheel w)
    {
        
    }
}
