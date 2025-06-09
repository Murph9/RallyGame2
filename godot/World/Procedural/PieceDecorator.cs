using System.Linq;
using Godot;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.World.Procedural;

public interface IPieceDecorator {
    void DecoratePiece(Node3D node, WorldPiece piece, WorldPieceDir outDirection);
}

public partial class PieceDecorator : IPieceDecorator {

    private const float FENCE_HEIGHT = 1.5f;

    private readonly Node3D _treeModel;
    private readonly Node3D _fencePost;

    public PieceDecorator() {
        var fenceHeightVector = new Vector3(0, FENCE_HEIGHT, 0);
        _fencePost = DebugHelper.BoxLine(Colors.Brown, Vector3.Zero, fenceHeightVector, 0.2f);

        // TODO please load a model or make this better:
        _treeModel = new Node3D();
        _treeModel.AddChild(DebugHelper.BoxLine(Colors.SaddleBrown, Vector3.Zero, Vector3.Up * 2, 0.4f));
        _treeModel.AddChild(DebugHelper.Sphere(Colors.Green, Vector3.Up * 3, 2));
    }

    public void DecoratePiece(Node3D node, WorldPiece piece, WorldPieceDir outDirection) {
        // create some fences so stop falling off
        var edgeMax = piece.GetZMaxOffsets(outDirection).ToArray();
        var edgeMin = piece.GetZMinOffsets(outDirection).ToArray();

        var fenceHeightVector = new Vector3(0, FENCE_HEIGHT, 0);

        GenerateWalls(node, edgeMin, edgeMax, fenceHeightVector);

        GenerateSideCollision(node, edgeMin, fenceHeightVector);
        GenerateSideCollision(node, edgeMax, fenceHeightVector);

        // draw some 'trees' out of the track
        foreach (var (First, Second) in edgeMin.Zip(edgeMax)) {
            var diff = (First - Second).Normalized();

            var pos = First + diff * 2;
            var model = (Node3D)_treeModel.Duplicate();
            model.Transform = new Transform3D(model.Transform.Basis, pos);
            node.AddChild(model);

            pos = Second - diff * 2;
            model = (Node3D)_treeModel.Duplicate();
            model.Transform = new Transform3D(model.Transform.Basis, pos);
            node.AddChild(model);
        }
    }

    private void GenerateSideCollision(Node3D node, Vector3[] verts, Vector3 fenceUpVector) {
        // give the fence a static wall collision
        for (var i = 0; i < verts.Length - 1; i++) {
            var body3d = new StaticBody3D() {
                PhysicsMaterialOverride = new PhysicsMaterial() {
                    Friction = 0,
                    Bounce = 0
                }
            };
            body3d.AddChild(new CollisionShape3D() {
                Shape = new ConvexPolygonShape3D() {
                    Points = [
                        verts[i],
                        verts[i + 1],
                        verts[i] + fenceUpVector,

                        verts[i] + fenceUpVector,
                        verts[i + 1],
                        verts[i + 1] + fenceUpVector
                    ]
                }
            });
            node.AddChild(body3d);
        }
    }

    private void GenerateWalls(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax, Vector3 fenceUpVector) {
        // draw fence posts
        foreach (var edgePoint in edgeMax.Concat(edgeMin)) {
            var fence = (Node3D)_fencePost.Duplicate();
            fence.Transform = new Transform3D(fence.Transform.Basis, edgePoint + fenceUpVector * 0.5f);
            node.AddChild(fence);
        }

        // draw fence side beams
        for (var i = 0; i < edgeMax.Length - 1; i++) {
            node.AddChild(DebugHelper.BoxLine(Colors.Brown, edgeMax[i] + fenceUpVector, edgeMax[i + 1] + fenceUpVector, 0.2f));
        }
        for (var i = 0; i < edgeMin.Length - 1; i++) {
            node.AddChild(DebugHelper.BoxLine(Colors.Brown, edgeMin[i] + fenceUpVector, edgeMin[i + 1] + fenceUpVector, 0.2f));
        }
    }
}