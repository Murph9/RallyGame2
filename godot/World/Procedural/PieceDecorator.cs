using System.Linq;
using Godot;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.World.Procedural;

public interface IPieceDecorator {
    void DecoratePiece(Node3D node, WorldPiece piece, WorldPieceDir outDirection);
}

public partial class PieceDecorator : IPieceDecorator {

    private readonly MeshInstance3D _treeModel;

    public PieceDecorator() {
        // TODO please load a model or make this better:
        _treeModel = DebugHelper.BoxLine(Colors.Green, Vector3.Zero, Vector3.Up * 2);
    }

    public void DecoratePiece(Node3D node, WorldPiece piece, WorldPieceDir outDirection) {
        GenerateWalls(node, piece, outDirection);
    }

    private void GenerateWalls(Node3D node, WorldPiece piece, WorldPieceDir outDirection) {
        // create some fences so stop falling off
        var edgeMax = piece.GetZMaxOffsets(outDirection).ToArray();
        var edgeMin = piece.GetZMinOffsets(outDirection).ToArray();
        const float fenceHeight = 1.5f;
        var fenceHeightVector = new Vector3(0, fenceHeight, 0);

        // draw some 'trees' out of the track
        foreach (var (First, Second) in edgeMin.Zip(edgeMax)) {
            var diff = (First - Second).Normalized();

            var pos = First + diff * 2;
            var model = (MeshInstance3D)_treeModel.Duplicate();
            model.Transform = new Transform3D(model.Transform.Basis, pos);
            node.AddChild(model);

            pos = Second - diff * 2;
            model = (MeshInstance3D)_treeModel.Duplicate();
            model.Transform = new Transform3D(model.Transform.Basis, pos);
            node.AddChild(model);
        }

        // draw fence posts
        foreach (var edgePoint in edgeMax.Concat(edgeMin)) {
            node.AddChild(DebugHelper.BoxLine(Colors.Brown, edgePoint, edgePoint + fenceHeightVector, 0.2f));
        }

        for (var i = 0; i < edgeMax.Length - 1; i++) {
            // draw fence side beams
            node.AddChild(DebugHelper.BoxLine(Colors.Brown, edgeMax[i] + fenceHeightVector, edgeMax[i + 1] + fenceHeightVector, 0.2f));

            // give collision
            var body3d = new StaticBody3D() {
                PhysicsMaterialOverride = new PhysicsMaterial() {
                    Friction = 0,
                    Bounce = 0
                }
            };
            body3d.AddChild(new CollisionShape3D() {
                Shape = new ConvexPolygonShape3D() {
                    Points = [
                        edgeMax[i],
                        edgeMax[i + 1],
                        edgeMax[i] + fenceHeightVector,

                        edgeMax[i] + fenceHeightVector,
                        edgeMax[i + 1],
                        edgeMax[i + 1] + fenceHeightVector
                    ]
                }
            });
            node.AddChild(body3d);
        }
        for (var i = 0; i < edgeMin.Length - 1; i++) {
            // draw fence side beams
            node.AddChild(DebugHelper.BoxLine(Colors.Brown, edgeMin[i] + fenceHeightVector, edgeMin[i + 1] + fenceHeightVector, 0.2f));

            // give collision
            var body3d = new StaticBody3D() {
                PhysicsMaterialOverride = new PhysicsMaterial() {
                    Friction = 0,
                    Bounce = 0
                }
            };
            body3d.AddChild(new CollisionShape3D() {
                Shape = new ConvexPolygonShape3D() {
                    Points = [
                        edgeMin[i],
                        edgeMin[i + 1],
                        edgeMin[i] + fenceHeightVector,

                        edgeMin[i] + fenceHeightVector,
                        edgeMin[i + 1],
                        edgeMin[i + 1] + fenceHeightVector
                    ]
                }
            });

            node.AddChild(body3d);
        }
    }
}