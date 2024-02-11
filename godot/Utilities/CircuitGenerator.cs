using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities;

// TODO allow more than one offset
public record BasicEl(string Name, Transform3D AddedOffset, Vector3 ExtentMin, Vector3 ExtentMax);

public record SearchPiece {
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
        Aabb = new Aabb(newExtentMin + size * AABB_BUFFER_DIFF/2f, size * (1 - AABB_BUFFER_DIFF)).Abs(); // prevent neighbours colliding too early

        // next position is pos + our rotation * our offset
        FinalPosition = Position + Rotation * piece.AddedOffset.Origin;
        FinalRotation = Rotation * piece.AddedOffset.Basis.GetRotationQuaternion();
    }

    public double G => FinalPosition.Length();

    public double F => G + H;

    public void GetParentPath(List<SearchPiece> list) {
        Parent?.GetParentPath(list);
        list.Add(this);
    }
}

public class CircuitGenerator(BasicEl[] pieces) {
    private readonly RandomNumberGenerator _rand = new ();
    private readonly BasicEl[] _pieces = pieces;

    public IEnumerable<BasicEl> GenerateRandomLoop(int startAmount, int minCount = 8, int maxCount = 20) {
        // generate a few to seed the generation (at least 1)
        var last = new SearchPiece(null, _pieces.First(x => x.Name == "straight")) {
            H = 0
        };

        while (true) {
            var nexts = GeneratePieces(last).OrderBy(x => x.F).ToArray();

            if (nexts.Length < 1) {
                last = last.Parent;
                continue;
            }

            if (last.LengthToRoot >= startAmount) {
                // break here so we know there are continuations
                break;
            }

            var index = 0; // weight pieces to the closest
            while (_rand.Randf() > 0.3f)
                index++;

            last = nexts[Mathf.Min(nexts.Length - 1, index)];
        }

        return CompleteLoop(last, minCount, maxCount);
    }

    private IEnumerable<BasicEl> CompleteLoop(SearchPiece last, int minCount, int maxCount) {
        var closed = new List<SearchPiece>();
        var open = new PriorityQueue<SearchPiece, double>();

        // add the first neighbours for the last enqueued item
        foreach (var c in GeneratePieces(last)) {
            open.Enqueue(c, c.G);
        }

        if (open.Count < 1) {
            // the last piece blocked the track, so reset it and start at the one before it
            closed.Remove(closed.Last());
            last = closed.Last();
        }

        while (open.Count > 0) {
            var q = open.Dequeue();

            var options = GeneratePieces(q);

            foreach (var o in options) {
                if (o.F < 1 && o.LengthToRoot >= minCount) {
                    var outputPath = new List<SearchPiece>();
                    o.GetParentPath(outputPath);

                    return outputPath.Select(x => x.Piece);
                }

                if (o.LengthToRoot <= maxCount)
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
