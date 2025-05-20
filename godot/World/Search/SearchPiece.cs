using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Search;

// TODO allow more than one offset
public record BasicEl(string Name, WorldPieceDir Dir, Vector3 ExtentMin, Vector3 ExtentMax);

public class SearchPiece {
    private const float AABB_BUFFER_DIFF = 0.05f;

    public BasicEl Piece { get; init; }
    public Vector3 Position { get; init; }
    public Quaternion Rotation { get; init; }
    public Aabb Aabb { get; init; }
    public Vector3 FinalPosition { get; init; }
    public Quaternion FinalRotation { get; init; }
    public double H { get; set; }

    public SearchPiece Parent { get; init; }
    public int LengthToRoot => (Parent?.LengthToRoot ?? 0) + 1;

    public SearchPiece(SearchPiece parent, BasicEl piece) {
        Parent = parent;
        Piece = piece;
        if (parent == null) {
            Position = new Vector3();
            Rotation = Quaternion.Identity;
        } else {
            Position = parent.FinalPosition;
            Rotation = parent.FinalRotation;
        }

        var newExtentMin = Position + Rotation * piece.ExtentMin;
        var newExtentMax = Position + Rotation * piece.ExtentMax;
        var size = newExtentMax - newExtentMin;
        Aabb = new Aabb(newExtentMin + size * AABB_BUFFER_DIFF / 2f, size * (1 - AABB_BUFFER_DIFF)).Abs(); // prevent neighbours colliding too early

        // next position is pos + our rotation * our offset
        FinalPosition = Position + Rotation * piece.Dir.FinalTransform.Origin;
        FinalRotation = Rotation * piece.Dir.FinalTransform.Basis.GetRotationQuaternion();
    }

    public double G => Math.Abs(FinalPosition.X) + Math.Abs(FinalPosition.Y) + Math.Abs(FinalPosition.Z); // manhattan distance

    public double F => G + H;

    public IEnumerable<SearchPiece> GetParentPath() {
        if (Parent != null)
            foreach (var p in Parent.GetParentPath())
                yield return p;
        yield return this;
    }

    public Dictionary<WorldPieceDir.TurnType, int> GetTurnsOfPath() {
        return GetParentPath().GroupBy(x => x.Piece.Dir.Turn).ToDictionary(x => x.Key, x => x.Count());
    }
    public Dictionary<WorldPieceDir.OffsetType, int> GetOffsetsOfPath() {
        return GetParentPath().GroupBy(x => x.Piece.Dir.Offset).ToDictionary(x => x.Key, x => x.Count());
    }
    public Dictionary<WorldPieceDir.VertType, int> GetVertsOfPath() {
        return GetParentPath().GroupBy(x => x.Piece.Dir.Vert).ToDictionary(x => x.Key, x => x.Count());
    }

    public override int GetHashCode() {
        int hash = 37;
        foreach (var foo in GetParentPath().Select(x => x.Piece.Name)) {
            hash = hash * 31 + foo.GetHashCode();
        }
        return hash;
    }

    public override bool Equals(object obj) {
        if (obj == null || GetType() != obj.GetType()) {
            return false;
        }
        return GetHashCode() == obj.GetHashCode(); // the sequence of pieces is perfect
    }
}
