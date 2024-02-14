using Godot;
using murph9.RallyGame2.godot.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities;

// TODO allow more than one offset
public record BasicEl(string Name, WorldPieceDir Dir, Vector3 ExtentMin, Vector3 ExtentMax);

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
        FinalPosition = Position + Rotation * piece.Dir.Offset.Origin;
        FinalRotation = Rotation * piece.Dir.Offset.Basis.GetRotationQuaternion();
    }

    public double G => FinalPosition.Length() + Piece.Dir.Offset.Origin.Length() * Mathf.Abs(FinalRotation.GetEuler().X  - Math.PI);

    public double F => G + H;

    public void GetParentPath(List<SearchPiece> list) {
        Parent?.GetParentPath(list);
        list.Add(this);
    }
}

public class CircuitGenerator(BasicEl[] pieces) {
    private readonly RandomNumberGenerator _rand = new ();
    private readonly BasicEl[] _pieces = pieces;

    public IEnumerable<BasicEl> GenerateRandomLoop(int randAmount = 3, int startAmount = 8, int maxCount = 20) {
        // always start with a straight piece
        var last = new SearchPiece(null, _pieces.First(x => x.Name == "straight")) {
            H = 0
        };

        const string JUSTPLACE = "justplace";
        const string MOVEHOME = "movehome";
        const string CLOSELOOP = "closeloop";

        var state = JUSTPLACE;

        while (true) {
            var nextNeighbours = GeneratePieces(last);
            if (nextNeighbours.Length < 1) {
                last = last.Parent;
                continue;
                // reset to the last piece if there is no solution
                // needs to stop getting in loops or picking a piece
            }

            if (state == JUSTPLACE) {
                last = nextNeighbours[_rand.RandiRange(0, nextNeighbours.Length - 1)];
                if (last.LengthToRoot > randAmount) {
                    Console.WriteLine(GetNamesOfPath(last) + " @" + last.FinalPosition);
                    state = MOVEHOME;
                }
            } else if (state == MOVEHOME) {
                // generally move home with a weighting to pieces that move closer to home
                var nexts = nextNeighbours.OrderBy(x => x.F).ToArray();
                var index = 0; // weight pieces to the closest
                while (_rand.Randf() > 0.5f)
                    index++;

                last = nexts[Mathf.Min(nexts.Length - 1, index)];
                if (last.LengthToRoot >= startAmount) {
                    Console.WriteLine(GetNamesOfPath(last) + " @" + last.FinalPosition);
                    state = CLOSELOOP;
                }
            } else if (state == CLOSELOOP) {
                // GetFinalPieces(last, maxCount);

                // move home as fast as possible
                var result = CompleteLoop(last, maxCount);
                Console.WriteLine(string.Join(",", result.Select(x => x.Name)) + " @: " + result.Count());
                return result;
            } else {
                throw new Exception("Unknown state " + state);
            }
        }

        // TODO change heuristic to piece count?
        // non-uniform sizes don't help
    }

    private IEnumerable<BasicEl> CompleteLoop(SearchPiece last, int maxCount) {
        // TODO this breadth first search is good unless its like >= 3 away :(
        // we probably need to have a better heuristic in A* (not sure about the 'angle' requirement though)
        var queue = new Queue<SearchPiece>();

        // add the first neighbours for the last enqueued item
        foreach (var c in GeneratePieces(last)) {
            queue.Enqueue(c);
        }

        while (queue.Count > 0) {
            var q = queue.Dequeue();
            if (q.F < 1 && q.FinalRotation.IsEqualApprox(Quaternion.Identity)) {
                var outputPath = new List<SearchPiece>();
                q.GetParentPath(outputPath);

                return outputPath.Select(x => x.Piece);
            }

            foreach (var o in GeneratePieces(q)) {
                if (q.LengthToRoot < maxCount) {
                    if (q.F < 1 && !q.FinalRotation.IsEqualApprox(Quaternion.Identity))
                        continue; // please ignore anything that touches the goal but doesn't face the right way
                    queue.Enqueue(o);
                }
            }
        }

        throw new Exception("No circuit found for " + queue.Count);
    }

    private SearchPiece[] GeneratePieces(SearchPiece top, BasicEl[] pieces = null) {
        var currentPath = new List<SearchPiece>();
        top.GetParentPath(currentPath);

        var list = new List<SearchPiece>();
        foreach (var p in pieces ?? _pieces) {
            var newP = new SearchPiece(top, p);

            // any collisions don't allow them
            var any = currentPath.Any(x => x.Aabb.Intersects(newP.Aabb));
            if (!any) {
                list.Add(newP);
            }
        }

        return list.ToArray();
    }

    private static string GetNamesOfPath(SearchPiece end) {
        if (end.Parent == null)
            return end.Piece.Name;
        return GetNamesOfPath(end.Parent) +  " " + end.Piece.Name;
    }
}
