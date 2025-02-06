using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public record WorldPieceDir(ICollection<Transform3D> Transforms, WorldPieceDir.TurnType Turn, WorldPieceDir.OffsetType Offset, WorldPieceDir.VertType Vert) {
    private static Basis LEFT90 = new(new Vector3(0, 1, 0), Mathf.DegToRad(90));
    private static Basis RIGHT90 = new(new Vector3(0, 1, 0), Mathf.DegToRad(-90));

    private static Basis LEFT45 = new(new Vector3(0, 1, 0), Mathf.DegToRad(45));
    private static Basis RIGHT45 = new(new Vector3(0, 1, 0), Mathf.DegToRad(-45));

    public Transform3D FinalTransform => Transforms.Last();

    public static WorldPieceDir FromTransform(Transform3D finalTransform, IEnumerable<Transform3D> subTransforms) => WithTransforms(finalTransform, subTransforms);

    public static WorldPieceDir FromTransform(Transform3D finalTransform, int segmentCount, float curveAngle) {
        var subTransforms = GenerateSubSegments(finalTransform, segmentCount, curveAngle);
        return WithTransforms(finalTransform, subTransforms);
    }

    private static WorldPieceDir WithTransforms(Transform3D finalTransform, IEnumerable<Transform3D> subTransforms = null) {
        // TODO normalize the rotation a little (to like closest 15' or something)
        // normalize the transform
        var t = new Transform3D(finalTransform.Basis, finalTransform.Origin);

        var turn = TurnType.Straight;
        var offset = OffsetType.None;
        var vert = VertType.Level;

        if (t.Basis.IsEqualApprox(Basis.Identity) && Math.Abs(t.Origin.X) > 0 && Math.Abs(t.Origin.Z) > 0) // going in both flat directions
            offset = t.Origin.Z > 0 ? OffsetType.OffsetRight : OffsetType.OffsetLeft; // TODO amounts

        if (Math.Abs(t.Origin.Y) > 0) // a change in elevation
            vert = t.Origin.Y > 0 ? VertType.Up : VertType.Down;

        if (t.Basis.IsEqualApprox(LEFT90) || t.Basis.IsEqualApprox(LEFT45))
            turn = TurnType.Left;
        else if (t.Basis.IsEqualApprox(RIGHT90) || t.Basis.IsEqualApprox(RIGHT45))
            turn = TurnType.Right;

        subTransforms ??= [];
        return new WorldPieceDir(subTransforms.Append(finalTransform).ToArray(), turn, offset, vert);
    }

    private static IList<Transform3D> GenerateSubSegments(Transform3D target, int segments, float curveAngle) {
        if (curveAngle == 0 || segments <= 1) {
            return [target];
        }

        // TODO the curve angle could be 0 and we still want to generate segments

        // calculate intermediate transforms
        // length of chord is chord = 2*radis*sin(theta/2)
        // so radius from chord length is radius = chord/(2*sin(theta/2))
        var radius = Math.Abs(target.Origin.ToV2XZ().Length() / (2 * Mathf.Sin(Mathf.DegToRad(curveAngle) / 2)));
        var result = new List<Transform3D>();

        for (int i = 1; i < segments; i++) {
            var angle = i / (float)segments * Mathf.DegToRad(curveAngle);
            var x = Mathf.Sin(angle) * radius;
            var z = radius - Mathf.Cos(angle) * radius;
            result.Add(new Transform3D(new Basis(Vector3.Up, -Mathf.Sign(target.Origin.Z) * angle), new Vector3(x, target.Origin.Y, z * Mathf.Sign(target.Origin.Z))));
        }

        return result;
    }

    public enum TurnType {
        Straight, Left, Right
    }
    public enum VertType {
        Level, Down, Up
    }
    public enum OffsetType {
        None, OffsetLeft, OffsetRight
    }
}
