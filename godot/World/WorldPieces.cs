using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace murph9.RallyGame2.godot.World;

public partial class WorldPieces : Node3D, IWorld {

    private readonly PackedScene SCENE;
    private readonly string pieceName;
    private readonly List<Piece> _pieces = [];
    private readonly List<Node3D> _placedPieces = [];

    public record Piece {
        public string Name;
        public Transform3D[] Directions;
        public Node3D Node;
    }

    public WorldPieces(string name) {
        pieceName = name;
        SCENE = GD.Load<PackedScene>("res://assets/worldPieces/" + name + ".blend");

        // generate a starting box so we don't spawn in the void
        var boxBody = new StaticBody3D();
        boxBody.AddChild(new CollisionShape3D() {
            Shape = new BoxShape3D() {
                Size = new Vector3(10, 1, 10)
            }
        });
        boxBody.AddChild(new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = new Vector3(10, 1, 10)
            },
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = Colors.Blue
            }
        });
        boxBody.Position = new Vector3(0, -0.501f, 0);
        AddChild(boxBody);
    }

    public override void _Ready() {
        var scene = SCENE.Instantiate<Node3D>();

        try {
            foreach (var c in scene.GetChildren().ToList()) {
                scene.RemoveChild(c);

                var directions = c.GetChildren().Where(x => x.GetType() == typeof(Node3D)).Select(x => x as Node3D);

                var p = new Piece() {
                    Name = c.Name,
                    Directions = directions.Select(x => x.Transform).ToArray(),
                    Node = c as Node3D
                };
                foreach (var dir in directions)
                    c.RemoveChild(dir);

                _pieces.Add(p);
            }
        } catch (Exception e) {
            GD.Print("Failed to parse pieces for " + pieceName);
            GD.Print(e);
            return;
        }

        try {
            var w = new WorldPieceLayoutGenerator(_pieces);
            var pieces = w.GenerateFixed(WorldPieceLayoutGenerator.CircuitLayout.SimpleLoop);

            var curPos = new Vector3();
            var curRot = Quaternion.Identity;
            foreach (var p in pieces) {
                var toAdd = p.Node.Duplicate() as Node3D;
                toAdd.Transform = new Transform3D(new Basis(curRot), curPos);

                // soz can only select the first one for now
                var dir = p.Directions.First();
                curPos += curRot * dir.Origin;
                curRot *= dir.Basis.GetRotationQuaternion();
                AddChild(toAdd);

                _placedPieces.Add(toAdd);
            }

        } catch (Exception e) {
            GD.Print("Failed to generate world piece location");
            GD.Print(e);
        }
    }

    public Transform3D GetSpawn() {
        return new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
    }

    public IEnumerable<Vector3> GetCheckpoints() {
        return _placedPieces.Select(x => x.Position);
    }
}
