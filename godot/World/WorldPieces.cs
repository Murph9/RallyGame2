using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using murph9.RallyGame2.godot.Debug;

namespace murph9.RallyGame2.godot.World;

public partial class WorldPieces : Node3D, IWorld {

    private const float COUNT = 9;

    private static PackedScene SCENE;
    private readonly Dictionary<Detail, Node3D> _pieces = new();

    private readonly RandomNumberGenerator rand;

    private Vector3 currentPosition = new ();
    private Quaternion currentRotation = Quaternion.Identity;

    record Detail {
        public string Name;
        public Transform3D[] Directions;
    }

    public WorldPieces(string name) {
        SCENE ??= GD.Load<PackedScene>("res://assets/worldPieces/" + name + ".blend");

        rand = new RandomNumberGenerator();

        // generate a starting box for now, we'll need it to stay still
        var boxBody = new StaticBody3D();
        boxBody.AddChild(new CollisionShape3D() {
            Shape = new BoxShape3D() {
                Size = new Vector3(30, 1, 30)
            }
        });
        boxBody.AddChild(new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = new Vector3(30, 1, 30)
            },
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = Colors.Blue
            }
        });
        boxBody.Position = new Vector3(0, -0.5f, 0);
        AddChild(boxBody);
    }

    public override void _Ready() {
        var scene = SCENE.Instantiate<Node3D>();

        try {
            foreach (var c in scene.GetChildren().ToList()) {
                scene.RemoveChild(c);

                var directions = c.GetChildren().Where(x => x.GetType() == typeof(Node3D)).Select(x => x as Node3D);
                var d = new Detail() {
                    Name = c.Name,
                    Directions = directions.Select(x => x.Transform).ToArray()
                };
                foreach (var dir in directions)
                    c.RemoveChild(dir);
                
                _pieces.Add(d, c as Node3D);
            }

            GeneratePieces();
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    private void GeneratePieces() {
        var pieces = _pieces.ToArray();
        for (int i = 0; i < COUNT; i++) {
            bool added = false;
            int attempts = 10;
            while (attempts > 0) {
                var piece = pieces[rand.RandiRange(0, pieces.Length - 1)];
                if (DoesItFit(piece, currentPosition, currentRotation)) {
                    var toAdd = piece.Value.Duplicate() as Node3D;
                    toAdd.Transform = new Transform3D(new Basis(currentRotation), currentPosition);
                    AddChild(toAdd);
                    added = true;
                    
                    AddChild(DebugHelper.GenerateWorldText(piece.Value.Name, currentPosition + new Vector3(0, 1, 0)));

                    // soz can only select the first one for now
                    var dir = piece.Key.Directions.First();
                    Console.WriteLine(piece.Value.Name + " is adding " + dir.Origin + " as " + currentRotation * dir.Origin + " and " + dir.Basis.GetEuler());
                    currentPosition += currentRotation * dir.Origin;
                    currentRotation *= dir.Basis.GetRotationQuaternion();
                    Console.WriteLine("Making " + currentPosition + " " + currentRotation.GetEuler());
                    break;
                }
            }
            if (!added) {
                break; // TODO aaaaaaa
            }
        }
    }

    private bool DoesItFit(KeyValuePair<Detail, Node3D> piece, Vector3 location, Quaternion direction) {
        // var col = piece.Value.GetNode<CollisionShape3D>("");
        // CollisionShape3D.
        return true; // LOL
    }

    public Transform3D GetSpawn() {
        return new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
    }
}

