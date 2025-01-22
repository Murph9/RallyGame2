using Godot;
using System;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public record WorldPieceDir(Transform3D Transform, WorldPieceDir.TurnType Turn, WorldPieceDir.OffsetType Offset, WorldPieceDir.VertType Vert)
{
    private static Basis LEFT90 = new(new Vector3(0, 1, 0), Mathf.DegToRad(90));
    private static Basis RIGHT90 = new(new Vector3(0, 1, 0), Mathf.DegToRad(-90));

    private static Basis LEFT45 = new(new Vector3(0, 1, 0), Mathf.DegToRad(45));
    private static Basis RIGHT45 = new(new Vector3(0, 1, 0), Mathf.DegToRad(-45));

    public static WorldPieceDir FromTransform3D(Transform3D transform)
    {
        // TODO normalize the rotation a little (to like closest 15' or something)
        // normalize the transform
        var t = new Transform3D(transform.Basis, transform.Origin);

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

        return new WorldPieceDir(t, turn, offset, vert);
    }

    public enum TurnType
    {
        Straight, Left, Right
    }
    public enum VertType
    {
        Level, Down, Up
    }
    public enum OffsetType
    {
        None, OffsetLeft, OffsetRight
    }
}
