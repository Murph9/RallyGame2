using Godot;
using System;

namespace murph9.RallyGame2.godot;

public partial class Car : RigidBody3D
{
    // no pacejka use http://www.racer.nl/reference/pacejka.htm
    // a custom step function which looks like search "F1-2002" on page

    private static float SUS_REBOUND = 0.2f;
    private static float SUS_COMPRESSION = 0.4f;
    private static float MASS = 10;

    public static void RotateBoxLineFor(MeshInstance3D mesh, Vector3 start, Vector3 end) {
        var length = (end - start).Length();
        
        mesh.Transform = new Transform3D(new Basis(new Quaternion(new Vector3(1,0,0), (end-start).Normalized())), start.Lerp(end, 0.5f));
        var box = mesh.Mesh as BoxMesh;
        box.Size = new Vector3(length, 0.1f, 0.1f);
    }

    public static MeshInstance3D BoxLine(Color c, Vector3 start, Vector3 end) {
        var mat = new StandardMaterial3D() {
            AlbedoColor = c
        };
        var length = (end - start).Length();
        
        var mesh = new BoxMesh() {
            Size = new Vector3(length, 0.1f, 0.1f),
            Material = mat
        };
        var meshObj = new MeshInstance3D() {
            Transform = new Transform3D(new Basis(new Quaternion(new Vector3(1,0,0), (end-start).Normalized())), start.Lerp(end, 0.5f)),
            Mesh = mesh
        };

        return meshObj;
    }

    class Wheel {
        public MeshInstance3D Model;
        public RayCast3D Ray;
    }

    private readonly Wheel[] _wheels;

    public Car() {
        _wheels = new Wheel[] {
            new () { Ray = new RayCast3D() {
                Position = new Vector3(1, 0, -2),
                TargetPosition = new Vector3(0, -2, 0)
            } },
            new () { Ray = new RayCast3D() {
                Position = new Vector3(1, 0, 2),
                TargetPosition = new Vector3(0, -2, 0)
            }  },
            new () { Ray = new RayCast3D() {
                Position = new Vector3(-1, 0, 2),
                TargetPosition = new Vector3(0, -2, 0)
            }  },
            new () { Ray = new RayCast3D() {
                Position = new Vector3(-1, 0, -2),
                TargetPosition = new Vector3(0, -2, 0)
            }  }
        };

        Mass = MASS;
        Position = new Vector3(1, 5, 1);
        Rotate(Vector3.Up, Mathf.DegToRad(135));
    }

    public override void _Ready() {
        AddChild(new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = new Vector3(1.8f, 1, 3.8f),
                Material = new StandardMaterial3D() {
                    AlbedoColor = Colors.AliceBlue
                }
            }
        });

        AddChild(new CollisionShape3D() {
            Shape = new BoxShape3D() {
                Size = new Vector3(1.8f, 1, 3.8f),
            }
        });

        // all 4 wheels now
        foreach (var w in _wheels) {
            AddChild(w.Ray);
            w.Model = new MeshInstance3D() {
                Position = w.Ray.Position,
                Mesh = new SphereMesh() {
                    Radius = 0.2f,
                    Height = 0.2f*2,
                    Material = new StandardMaterial3D() {
                        AlbedoColor = Colors.Yellow
                    }
                },
            };
            AddChild(w.Model);
        }
    }

    public override void _Process(double delta) {
        
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
        var sphereObj = (SphereMesh)w.Model.Mesh;
        
        var hitPositionGlobal = w.Ray.GetCollisionPoint();
        var hitNormalGlobal = w.Ray.GetCollisionNormal();
        if (hitPositionGlobal.LengthSquared() == 0) {
            // get the end of the ray TODO check
            w.Model.Position = w.Model.ToLocal(w.Ray.ToGlobal(w.Ray.TargetPosition));
            sphereObj.Radius = 0.1f;
            sphereObj.Height = 0.2f;
            return;
        }
        
        w.Model.Position = ToLocal(hitPositionGlobal);

        var rayDirectionGlobal = w.Ray.GlobalBasis * w.Ray.TargetPosition;
        var surfaceNormalFactor = hitNormalGlobal.Dot(-rayDirectionGlobal);

        var distance = w.Ray.GlobalPosition.DistanceTo(hitPositionGlobal);
        var force = Math.Max(0, 1 - distance); // an offset
        var hitVelocity = LinearVelocity + AngularVelocity.Cross(hitPositionGlobal - GlobalPosition); // TODO calc other model speed
        
        var relVel = hitNormalGlobal.Dot(hitVelocity);
        var damping = -SUS_REBOUND * relVel;
        if (relVel > 0) {
            damping = -SUS_COMPRESSION * relVel;
        }
        
        if (force + damping > 0) {
            sphereObj.Radius = (force + damping)/5 + 0.1f;
            sphereObj.Height = (force + damping)/2.5f + 0.2f;
            var forceDir = -rayDirectionGlobal * (force + damping) * surfaceNormalFactor;
            ApplyForce(5 * Mass * forceDir, ToGlobal(w.Model.Position) - GlobalPosition);
        } else {
            sphereObj.Radius = 0.1f;
            sphereObj.Height = 0.2f;
        }
    }

    private void ApplyDrag(Wheel w)
    {
        
    }
}
