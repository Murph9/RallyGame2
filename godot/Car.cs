using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot;

public partial class Car : RigidBody3D
{
    private static float SUS_DAMPING = 0.2f;
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
        public Vector3 Pos;
        public MeshInstance3D Model;
        public MeshInstance3D SusRay;
        public MeshInstance3D SusForce;
    }

    private readonly Wheel[] _wheels;

    public Car() {
        _wheels = new Wheel[] {
            new () { Pos = new Vector3(1, 0, -2) },
            new () { Pos = new Vector3(1, 0, 2) },
            new () { Pos = new Vector3(-1, 0, 2) },
            new () { Pos = new Vector3(-1, 0, -2) }
        };

        Mass = MASS;
        Position = new Vector3(1, 1, 1);
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
            w.Model = new MeshInstance3D() {
                Position = w.Pos,
                Mesh = new SphereMesh() {
                    Radius = 0.2f,
                    Height = 0.2f*2,
                    Material = new StandardMaterial3D() {
                        AlbedoColor = Colors.Yellow
                    }
                },
            };
            AddChild(w.Model);
            
            w.SusRay = BoxLine(Colors.Black, Vector3.Up, Vector3.Down);
            AddChild(w.SusRay);

            w.SusForce = BoxLine(Colors.Blue, Vector3.Up, Vector3.Down);
            AddChild(w.SusForce);
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
        var sphere = (SphereMesh)w.Model.Mesh;

        var t = GlobalTransform;
        var globalPos = ToGlobal(w.Pos);
        var globalDownRay = -t.Basis.Y; // down
        
        var from = globalPos;
        var to = globalPos + globalDownRay * 2;
        RotateBoxLineFor(w.SusRay, ToLocal(from), ToLocal(to));
        
        var space = GetWorld3D().DirectSpaceState;
        var col = space.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to));
        if (!col.TryGetValue("position", out var position) || position.VariantType != Variant.Type.Vector3) {
            // default to the full extension
            w.Model.Position = ToLocal(to);
            sphere.Radius = 0.1f;
            sphere.Height = 0.2f;
            return;
        }

        var normal = (Vector3)col["normal"];
        if (normal.LengthSquared() == 0) {
            sphere.Radius = 0.1f;
            sphere.Height = 0.2f;
            return;
        }

        // set nothing first
        RotateBoxLineFor(w.SusForce, w.Model.Position, w.Model.Position);

        var angle = normal.Dot(-globalDownRay);

        var posV3 = (Vector3)position;
        w.Model.Position = ToLocal(posV3);

        var distance = from.DistanceTo(posV3);
        var force = 2 - distance;
        var pointVelocity = LinearVelocity + AngularVelocity.Cross(ToGlobal(w.Model.Position) - GlobalPosition);
        
        var damping = -SUS_DAMPING * normal.Dot(pointVelocity);
        
        if (force + damping > 0) {
            sphere.Radius = (force + damping)/5 + 0.1f;
            sphere.Height = (force + damping)/2.5f + 0.2f;
            var forceDir = -globalDownRay * (force + damping) * angle;
            ApplyForce(5 * Mass * forceDir, ToGlobal(w.Model.Position) - GlobalPosition);
            
            RotateBoxLineFor(w.SusForce, w.Model.Position, w.Model.Position + (forceDir * t.Basis));
        } else {
            sphere.Radius = 0.1f;
            sphere.Height = 0.2f;
        }
    }

    private void ApplyDrag(Wheel w)
    {
        
    }
}
