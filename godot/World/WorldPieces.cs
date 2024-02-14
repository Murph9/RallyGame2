using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record WorldPiece(string Name, WorldPieceDir[] Directions, Node3D Model);

public record WorldPieceDir(Transform3D Offset, WorldPieceDirType Type) {
    private static Basis LEFT90 = new (new Vector3(0, 1, 0), Mathf.DegToRad(90));
    private static Basis RIGHT90 = new (new Vector3(0, 1, 0), Mathf.DegToRad(-90));

    public static WorldPieceDir FromTransform3D(Transform3D transform) {
        // TODO allow multiple choices
        var type = WorldPieceDirType.Straight;

        if (Math.Abs(transform.Origin.X) > 0 && Math.Abs(transform.Origin.Z) > 0) // going in both flat directions
            type = transform.Origin.Z > 0 ? WorldPieceDirType.OffsetRight : WorldPieceDirType.OffsetLeft; // TODO amounts

        if (Math.Abs(transform.Origin.Y) > 0) // a change in elevation
            type = transform.Origin.Y > 0 ? WorldPieceDirType.Up : WorldPieceDirType.Down;

        if (transform.Basis.IsEqualApprox(LEFT90))
            type = WorldPieceDirType.Left90;
        else if (transform.Basis.IsEqualApprox(RIGHT90))
            type = WorldPieceDirType.Right90;

        return new WorldPieceDir(transform, type);
    }

    public static WorldPieceDirType GetOppositeDir(WorldPieceDirType dir) => dir switch {
        WorldPieceDirType.Up => WorldPieceDirType.Down,
        WorldPieceDirType.Down => WorldPieceDirType.Up,
        WorldPieceDirType.Left90 => WorldPieceDirType.Right90,
        WorldPieceDirType.Right90 => WorldPieceDirType.Left90,
        WorldPieceDirType.OffsetLeft => WorldPieceDirType.OffsetRight,
        WorldPieceDirType.OffsetRight => WorldPieceDirType.OffsetLeft,
        WorldPieceDirType.Straight => WorldPieceDirType.Straight,
        _ => WorldPieceDirType.Straight,
    };
}

public enum WorldPieceDirType {
    Straight, Left90, Right90, OffsetLeft, OffsetRight, Down, Up
}

public partial class WorldPieces : Node3D, IWorld {

    private readonly PackedScene BlenderScene;
    private readonly string pieceName;
    private readonly List<WorldPiece> _pieces = [];
    private readonly List<Node3D> _placedPieces = [];

    public List<WorldPiece> Pieces => [.. _pieces];

    public WorldPieces(string name) {
        pieceName = name;
        BlenderScene = GD.Load<PackedScene>("res://assets/worldPieces/" + name + ".blend");

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
        var scene = BlenderScene.Instantiate<Node3D>();

        try {
            foreach (var c in scene.GetChildren().ToList()) {
                scene.RemoveChild(c);

                var directions = c.GetChildren().Where(x => x.GetType() == typeof(Node3D)).Select(x => x as Node3D);

                var p = new WorldPiece(c.Name, directions.Select(x => WorldPieceDir.FromTransform3D(x.Transform)).ToArray(), c as Node3D);

                foreach (var dir in directions) {
                    c.RemoveChild(dir);
                    dir.QueueFree();
                }

                _pieces.Add(p);
            }
        } catch (Exception e) {
            GD.Print("Failed to parse pieces for " + pieceName);
            GD.Print(e);
            return;
        }

        try {
            var c2s = new List<BasicEl>();
            foreach (var p in _pieces) {
                var aabb = p.Model.GlobalTransform * ((MeshInstance3D)p.Model).GetAabb();
                c2s.Add(new BasicEl(p.Name, p.Directions[0], aabb.Position, aabb.End));
            }
            var pieceNames = new CircuitGenerator(c2s.ToArray()).GenerateRandomLoop().ToArray();
            var pieces = pieceNames.Select(x => _pieces.Single(y => y.Name == x.Name));

            var curPos = new Vector3();
            var curRot = Quaternion.Identity;
            foreach (var p in pieces) {
                var toAdd = p.Model.Duplicate() as Node3D;
                toAdd.Transform = new Transform3D(new Basis(curRot), curPos);

                // soz can only select the first one for now
                var dir = p.Directions.First().Offset;
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

    public IEnumerable<Transform3D> GetCheckpoints() {
        return _placedPieces.Select(x => x.Transform);
    }
}
