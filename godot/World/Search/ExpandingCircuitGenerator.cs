using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Search;

// TODO just use a linkedlist management class instead of dealing with nodes only

public record ExpandPiece {
    private const float AABB_BUFFER_DIFF = 0.05f;
    public BasicEl Piece { get; init; }

    public ExpandPiece Parent { get; private set; }
    public List<ExpandPiece> Children { get; init; } = [];

    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Aabb Aabb { get; private set; }
    public Vector3 FinalPosition { get; private set; }
    public Quaternion FinalRotation { get; private set; }

    public ExpandPiece(BasicEl piece, ExpandPiece parent) {
        Piece = piece;
        SetParent(parent);
    }

    public int LengthToRoot => (Parent?.LengthToRoot ?? 0) + 1;
    public ExpandPiece LastChild() {
        if (Children.Count != 0) return Children.Single();
        return this;
    }
    
    public ExpandPiece FirstParent() => Parent is null ? this : Parent.FirstParent();
    public IEnumerable<ExpandPiece> GetParents() {
        if (Parent != null)
            foreach (var p in Parent.GetParents())
                yield return p;
        yield return this;
    }

    public void SetParent(ExpandPiece parent) {
        // remove self from Parent
        Parent?.Children.Remove(this);
        // update parent with self
        Parent = parent;
        Parent?.Children.Add(this);

        if (Parent == null) {
            Position = new Vector3();
            Rotation = Quaternion.Identity;
        } else {
            Position = Parent.FinalPosition;
            Rotation = Parent.FinalRotation;
        }

        // update Aabb with the extents of the current piece
        var newExtentMin = Position + Rotation * Piece.ExtentMin;
        var newExtentMax = Position + Rotation * Piece.ExtentMax;
        var size = newExtentMax - newExtentMin;
        Aabb = new Aabb(newExtentMin + size * AABB_BUFFER_DIFF/2f, size * (1 - AABB_BUFFER_DIFF)).Abs(); // prevent neighbours colliding too early

        // next position is pos + our rotation * our offset
        FinalPosition = Position + Rotation * Piece.Dir.Transform.Origin;
        FinalRotation = Rotation * Piece.Dir.Transform.Basis.GetRotationQuaternion();

        // then update the chain
        foreach (var child in Children.ToArray()) {
            child.SetParent(this);
        }
    }

    public IEnumerable<BasicEl> GetTreeList() {
        if (Parent != null)
            foreach (var p in Parent.GetTreeList())
                yield return p;
        yield return Piece;
    }

    public ExpandPiece CloneTreeList() {
        var treeList = GetTreeList();
        ExpandPiece last = null;
        foreach (var b in treeList) {
            var cur = new ExpandPiece(b, last);
            last = cur;
        }
        return last;
    }

    public Dictionary<WorldPieceDir.TurnType, int> GetTurnsOfPath() {
        return GetTreeList().GroupBy(x => x.Dir.Turn).ToDictionary(x => x.Key, x => x.Count());
    }
    public Dictionary<WorldPieceDir.OffsetType, int> GetOffsetsOfPath() {
        return GetTreeList().GroupBy(x => x.Dir.Offset).ToDictionary(x => x.Key, x => x.Count());
    }
    public Dictionary<WorldPieceDir.VertType, int> GetVertsOfPath() {
        return GetTreeList().GroupBy(x => x.Dir.Vert).ToDictionary(x => x.Key, x => x.Count());
    }
}


public class ExpandingCircuitGenerator : ICircuitGenerator {

    private readonly RandomNumberGenerator _rand;
    private readonly BasicEl[] _basicPieces;

    private readonly ExpandPiece _startingLayout;

    private readonly Dictionary<string, Vector3> _normalizedOffsets;
    private readonly Dictionary<string, Quaternion> _transforms;

    private readonly List<ExpandPiece> _replacements;

    public ExpandingCircuitGenerator(BasicEl[] pieces, ulong seed = ulong.MinValue) {
        _basicPieces = pieces;
        _rand = new RandomNumberGenerator();
        if (seed != ulong.MinValue)
            _rand.Seed = seed;

        // categorize each item into what it fits with
        // first find the most simple piece's length
        var minStraightLength = _basicPieces
            .Where(x => x.Dir.Turn == WorldPieceDir.TurnType.Straight)
            .Select(x => x.Dir.Transform.Origin)
            .Min(x => x.Length());

        _normalizedOffsets = _basicPieces.ToDictionary(x => x.Name, x => x.Dir.Transform.Origin / minStraightLength);
        _transforms = _basicPieces.ToDictionary(x => x.Name, x => x.Dir.Transform.Basis.GetRotationQuaternion());

        var longestLeftTurn = _basicPieces.OrderByDescending(x => x.Dir.Transform.Origin.LengthSquared())
            .First(x => x.Dir.Turn == WorldPieceDir.TurnType.Left90);
        var normalStraight = _basicPieces.First(x => x.Dir.Turn == WorldPieceDir.TurnType.Straight && x.Dir.Vert == WorldPieceDir.VertType.Level);

        // TODO a right90 as well to randomly pick from
        var startingLoop = new BasicEl[] {
            normalStraight, normalStraight, longestLeftTurn,
            normalStraight, normalStraight, longestLeftTurn,
            normalStraight, normalStraight, longestLeftTurn,
            normalStraight, normalStraight, longestLeftTurn,
        };
        
        _startingLayout = null;
        foreach (var b in startingLoop) {
            var cur = new ExpandPiece(b, _startingLayout);
            _startingLayout = cur;
        }

        // create some replacements which are <TODO valid TODO> from the smaller pieces
        _replacements = [];

        // lets just start with some hard coded ones
        _replacements.Add(FromList("straight"));
        _replacements.Add(FromList("left_long", "left_long", "right_long"));
        _replacements.Add(FromList("straight", "straight", "cross", "left", "left", "right_long"));
        _replacements.Add(FromList("straight", "left", "straight", "left", "straight"));
        _replacements.Add(FromList("hill_down", "hill_up"));
        _replacements.Add(FromList("hill_down", "left", "hill_up"));
        _replacements.Add(FromList("hill_up", "left", "hill_down"));

        foreach (var p in _normalizedOffsets) {
            Console.WriteLine(p);
        }

        foreach (var r in _replacements) {
            Console.WriteLine(string.Join(", ", r.GetTreeList().Select(x => x.Name)));
            Console.WriteLine(GetNormalizedTransform3DFor(r.GetTreeList()));
        }
    }

    public IEnumerable<BasicEl> GenerateRandomLoop(int randAmount = 3, int startAmount = 8, int maxCount = 20) {
        const int iterations = 20;
        int added = 0;
        var layout = _startingLayout.CloneTreeList();

        // pick a random modification and try to fit it anywhere
        // do this until we have successfully placed N
        for (var i = 0; i < iterations; i++) {
            var current = _replacements[_rand.RandiRange(0, _replacements.Count - 1)];
            var curTransform = GetNormalizedTransform3DFor(current.GetTreeList());

            var layoutPieces = layout.GetParents().ToArray();
            // search the current circuit for where to put it
            for (int j = 0; j < layoutPieces.Length - 1; j++) {
                for (int k = j + 1; k < layoutPieces.Length; k++) {
                    var layoutNodes = layoutPieces.Skip(k).Take(k).Select(x => x.Piece);
                    var transform = GetNormalizedTransform3DFor(layoutNodes);
                    if (transform.IsEqualApprox(curTransform)) {
                        Console.WriteLine("yay, placed: " + string.Join(", ", current.GetTreeList().Select(x => x.Name)) + "@" + GetNormalizedTransform3DFor(current.GetTreeList()));
                        added++;
                        layout = MixTogether(layoutPieces[j], layoutPieces[k], current);
                        goto SearchDone;
                    }
                }
            }

            SearchDone: Console.WriteLine("Done");
        }

        Console.WriteLine("Added " + added + " pieces");
        return layout.GetTreeList();
    }

    private static ExpandPiece MixTogether(ExpandPiece baseStart, ExpandPiece baseEnd, ExpandPiece segmentEnd) {
        // clone segment first
        segmentEnd = segmentEnd.CloneTreeList();

        var segmentStart = segmentEnd.FirstParent();

        // if the base start is the first element ignore, else merge
        var newStart = segmentStart;
        if (baseStart.Parent is not null) {
            var p = baseStart.Parent;
            baseStart.SetParent(null);
            segmentStart.SetParent(p);
            newStart = baseStart.FirstParent();
        }

        // if the base end is the last element ignore, else merge the remaining pieces
        foreach (var c in baseEnd.Children.ToArray()) {
            c.SetParent(segmentEnd);
        }

        return newStart.LastChild();
    }


    private BasicEl FromName(string name) => _basicPieces.Single(x => x.Name == name);
    private ExpandPiece FromList(params string[] names) {
        return FromList(names.Select(FromName).ToArray());
    }

    private ExpandPiece FromList(params BasicEl[] list) {
        ExpandPiece last = null;
        foreach (var b in list) {
            var cur = new ExpandPiece(b, last);
            last = cur;
        }
        return last;
    }

    private Transform3D GetNormalizedTransform3DFor(IEnumerable<BasicEl> pieces) {
        var curPos = new Vector3();
        var curRot = Quaternion.Identity;
        foreach (var p in pieces) {
            var origin = _normalizedOffsets[p.Name];
            var basis = _transforms[p.Name];
            curPos += curRot * origin;
            curRot *= basis;
        }

        return new Transform3D(new Basis(curRot), curPos);
    }
}
