using Godot;
using murph9.RallyGame2.godot.World.Search;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public partial class DynamicWorldPieces : Node3D, IWorld {

    private readonly PackedScene BlenderScene;
    private readonly WorldType pieceType;
    private readonly List<WorldPiece> _pieces = [];
    private readonly List<Node3D> _placedPieces = [];
    private List<PlacedPiece> _pieceOrder;

    public List<WorldPiece> Pieces => [.. _pieces];

    public DynamicWorldPieces(WorldType type) {
        pieceType = type;
        BlenderScene = GD.Load<PackedScene>("res://assets/worldPieces/" + pieceType.ToString().ToLower() + ".blend");

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

                var p = WorldPiece.LoadFrom(c as MeshInstance3D);
                _pieces.Add(p);
            }
        } catch (Exception e) {
            GD.Print("Failed to parse pieces for " + pieceType);
            GD.Print(e);
            return;
        }

        try {
            var c2s = new List<BasicEl>();
            foreach (var p in _pieces) {
                var aabb = p.Model.GlobalTransform * ((MeshInstance3D)p.Model).GetAabb();
                c2s.Add(new BasicEl(p.Name, p.Directions[0], aabb.Position, aabb.End));
            }
            var pieces = new ExpandingCircuitGenerator(c2s.ToArray()).GenerateRandomLoop().ToList();
            _pieceOrder = [];

            var curPos = new Vector3();
            var curRot = Quaternion.Identity;
            foreach (var piece in pieces) {
                var p = _pieces.Single(x => x.Name == piece.Name);

                var toAdd = p.Model.Duplicate() as Node3D;
                toAdd.Transform = new Transform3D(new Basis(curRot), curPos);

                // soz can only select the first one for now
                var dir = p.Directions.First().FinalTransform;
                curPos += curRot * dir.Origin;
                curRot *= dir.Basis.GetRotationQuaternion();
                AddChild(toAdd);

                _placedPieces.Add(toAdd);
                _pieceOrder.Add(new PlacedPiece(p.Name, new Transform3D(new Basis(curRot), curPos), piece.Dir));
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

    public IEnumerable<Curve3DPoint> GetCurve3DPoints() {
        // we assume every piece that has a rotation has a perfect circle path
        // and use the circle bezier formula: d = r*4*(Mathf.Sqrt(2)-1)/3
        var radiusCalc = 4 * (Math.Sqrt(2) - 1) / 3f;

        var pointList = new List<Vector3?>();

        var lastOffset = Vector3.Zero;
        var lastBasis = Basis.Identity;
        for (var i = 0; i < _pieceOrder.Count; i++) {
            var cur = _pieceOrder[i];

            // get the point
            pointList.Add(lastOffset);
            var localOffset = cur.Dir.FinalTransform.Origin;

            if (cur.Dir.Turn == WorldPieceDir.TurnType.Straight) {
                pointList.Add(null);
                pointList.Add(null);
            } else {
                var curveOffset = localOffset.X * (float)radiusCalc; // offset is radius with everything being a circle

                // get the next point a 'bit' of 'curve' away
                pointList.Add(lastBasis * new Vector3(curveOffset, 0, 0));

                // get the next point a 'bit' of 'curve' from the end
                pointList.Add(cur.FinalTransform.Basis * new Vector3(-curveOffset, 0, 0));
            }

            // next loop
            lastOffset = cur.FinalTransform.Origin;
            lastBasis = cur.FinalTransform.Basis;
        }

        yield return new Curve3DPoint(pointList[0].Value, null, pointList[1]);

        for (var i = 3; i < pointList.Count - 1; i += 3) {
            yield return new Curve3DPoint(pointList[i].Value, pointList[i - 1], pointList[i + 1]);
        }

        yield return new Curve3DPoint(pointList[0].Value, pointList[^1], null);
    }
}
