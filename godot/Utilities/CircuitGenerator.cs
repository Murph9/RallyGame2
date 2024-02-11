using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities;

// TODO allow more than one offset
public record BasicEl(string Name, Transform3D AddedOffset, Vector3 ExtentMin, Vector3 ExtentMax);

public record SearchPiece {
    public BasicEl Piece { get; init; }
    public Vector3 Position { get; init; }
    public Quaternion Rotation { get; init; }
    public Aabb Aabb { get; init; }
    public double G { get; set; } = double.MaxValue;
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
            // current position is parent pos + parent's rotation * parent's offset
            Position = parent.Position + parent.Rotation * parent.Piece.AddedOffset.Origin;
            Rotation = parent.Rotation * parent.Piece.AddedOffset.Basis.GetRotationQuaternion();
        }

        var newExtentMin = Position + Rotation * piece.ExtentMin;
        var newExtentMax = Position + Rotation * piece.ExtentMax;
        var size = newExtentMax - newExtentMin;
        Aabb = new Aabb(newExtentMin + size * 0.025f, size * 0.95f).Abs(); // prevent neighbours colliding too early

        G = (Position + Rotation * piece.AddedOffset.Origin).Length();
    }

    public double F => G + H;

    public void GetParentPath(List<SearchPiece> list) {
        Parent?.GetParentPath(list);
        list.Add(this);
    }
}

public class CircuitGenerator(BasicEl[] pieces) {
    private readonly RandomNumberGenerator _rand = new ();
    private readonly BasicEl[] _pieces = pieces;

    public IEnumerable<BasicEl> GenerateRandomLoop(int startAmount, int minCount = 8) {
        var closed = new List<SearchPiece>();
        var open = new PriorityQueue<SearchPiece, double>();

        // generate a few to seed the generation (at least 1)
        var last = new SearchPiece(null, _pieces.First(x => x.Name == "straight")) {
            G = 0,
            H = 0
        };
        closed.Add(last);

        for (int i = 0; i < Math.Max(0, startAmount - 1); i++) {
            var nexts = GeneratePieces(last);
            if (nexts.Length == 0)
                continue;
            last = nexts[_rand.RandiRange(0, nexts.Length - 1)];
            closed.Add(last);
        }

        // add the first neighbours for the last enqueued item
        foreach (var c in GeneratePieces(last)) {
            open.Enqueue(c, c.G);
        }
        if (open.Count < 1) {
            // the last piece blocked the track, so reset it and start at the last one
            closed.Remove(closed.Last());
            last = closed.Last();
        }

        while (open.Count > 0) {
            var q = open.Dequeue();

            var options = GeneratePieces(q);

            foreach (var o in options) {
                if (o.F < 1 && o.LengthToRoot >= minCount) {
                    var currentPath = new List<SearchPiece>();
                    o.GetParentPath(currentPath);

                    return currentPath.Select(x => x.Piece);
                }

                open.Enqueue(o, o.F);
            }

            closed.Add(q);
        }

        throw new Exception("No circuit found for " + open.Count);
    }

    private SearchPiece[] GeneratePieces(SearchPiece top) {
        var currentPath = new List<SearchPiece>();
        top.GetParentPath(currentPath);

        var list = new List<SearchPiece>();
        foreach (var p in _pieces) {
            var newP = new SearchPiece(top, p);

            // any collisions don't allow them
            var any = currentPath.Any(x => x.Aabb.Intersects(newP.Aabb));
            if (!any) {
                list.Add(newP);
            }
        }

        return list.ToArray();
    }
}
