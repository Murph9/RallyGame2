using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Search;

public class El(BasicEl piece) {
    public BasicEl Piece { get; } = piece;
    public Vector3 FinalPosition { get; set; } = Vector3.Zero;
    public Quaternion FinalRotation { get; set; } = Quaternion.Identity;
    public Aabb Aabb { get; set; }

    public override string ToString() {
        return "el@" + Piece.Name;
    }

    public El Clone() {
        return new El(Piece);
    }
};

public class ElList {
    private const float AABB_BUFFER_DIFF = 0.05f;
    private readonly List<El> _elements = [];

    public ElList() { }
    public ElList(params El[] els) {
        _elements = els.ToList();
        UpdateChainFrom();
    }

    public long Count => _elements.Count;

    public void AddLast(El el) {
        _elements.Add(el);

        UpdateChainFrom(); // TODO perf
    }

    public override string ToString() {
        return "[pieces@" + string.Join(", ", _elements.Select(x => x.Piece.Name)) + "]";
    }

    private void UpdateChainFrom(int index = 0) {
        for (var i = index; i < _elements.Count; i++) {
            var parent = i - 1 < 0 ? new El(null) : _elements[i - 1];
            var el = _elements[i];

            // next position is pos + our rotation * our offset
            el.FinalPosition = parent.FinalPosition + parent.FinalRotation * el.Piece.Dir.FinalTransform.Origin;
            el.FinalRotation = parent.FinalRotation * el.Piece.Dir.FinalTransform.Basis.GetRotationQuaternion();

            // update Aabb with the extents of the current piece
            var newExtentMin = parent.FinalPosition + parent.FinalRotation * el.Piece.ExtentMin;
            var newExtentMax = parent.FinalPosition + parent.FinalRotation * el.Piece.ExtentMax;
            var size = newExtentMax - newExtentMin;
            el.Aabb = new Aabb(newExtentMin + size * AABB_BUFFER_DIFF / 2f, size * (1 - AABB_BUFFER_DIFF)).Abs(); // prevent neighbours colliding too early
        }
    }

    public IEnumerable<BasicEl> AsBasicEl() => _elements.Select(x => x.Piece);

    public ElList Clone() {
        return new ElList(_elements.Select(x => x.Clone()).ToArray());
    }

    public ElList GetRange(int start, int end) {
        return new ElList(_elements[start..end].Select(x => x.Clone()).ToArray());
    }

    public ElList ReplaceRange(int startIndex, int endIndex, ElList addIn) {
        var endingList = new List<El>();
        if (startIndex != 0) {
            // we need to splice before the new bit
            endingList.AddRange(GetRange(0, startIndex)._elements);
        }

        endingList.AddRange(addIn._elements);

        if (endIndex < _elements.Count) {
            // we need a splice after the new bit
            endingList.AddRange(GetRange(endIndex, _elements.Count)._elements);
        }

        return new ElList(endingList.Select(x => x.Clone()).ToArray());
    }
}

public class ExpandingCircuitGenerator : ICircuitGenerator {

    private readonly RandomNumberGenerator _rand;
    private readonly BasicEl[] _basicPieces;

    private readonly ElList _startingLayout;

    private readonly Dictionary<string, Vector3> _normalizedOffsets;
    private readonly Dictionary<string, Quaternion> _transforms;

    private readonly List<ElList> _replacements;

    public ExpandingCircuitGenerator(BasicEl[] pieces, ulong seed = ulong.MinValue) {
        _basicPieces = pieces;
        _rand = new RandomNumberGenerator();
        if (seed != ulong.MinValue)
            _rand.Seed = seed;

        // categorize each item into what it fits with
        // first find the most simple piece's length
        var minStraightLength = _basicPieces
            .Where(x => x.Dir.Turn == WorldPieceDir.TurnType.Straight)
            .Select(x => x.Dir.FinalTransform.Origin)
            .Min(x => x.Length());

        _normalizedOffsets = _basicPieces.ToDictionary(x => x.Name, x => x.Dir.FinalTransform.Origin / minStraightLength);
        _transforms = _basicPieces.ToDictionary(x => x.Name, x => x.Dir.FinalTransform.Basis.GetRotationQuaternion());

        var longestLeftTurn = _basicPieces.OrderByDescending(x => x.Dir.FinalTransform.Origin.LengthSquared())
            .First(x => x.Dir.Turn == WorldPieceDir.TurnType.Left);
        var normalStraight = _basicPieces.First(x => x.Dir.Turn == WorldPieceDir.TurnType.Straight && x.Dir.Vert == WorldPieceDir.VertType.Level);

        // TODO a right90 as well to randomly pick from
        var startingLoop = new BasicEl[] {
            normalStraight, normalStraight, longestLeftTurn,
            normalStraight, normalStraight, longestLeftTurn,
            normalStraight, normalStraight, longestLeftTurn,
            normalStraight, normalStraight, longestLeftTurn,
        };

        _startingLayout = new ElList();
        foreach (var b in startingLoop) {
            _startingLayout.AddLast(new El(b));
        }

        // create some replacements which are <TODO valid TODO> from the smaller pieces
        _replacements = [];

        // lets just start with some hard coded ones
        _replacements.Add(FromList("left_long_90", "left_long_90", "right_long_90"));
        _replacements.Add(FromList("straight", "straight", "cross", "left_90", "left_90", "right_long_90"));
        _replacements.Add(FromList("straight", "left_90", "straight", "left_90", "straight"));
        _replacements.Add(FromList("hill_down", "hill_up"));
        _replacements.Add(FromList("hill_up", "hill_down"));
        _replacements.Add(FromList("hill_down", "left_90", "hill_up"));
        _replacements.Add(FromList("hill_up", "left_90", "hill_down"));

        foreach (var p in _normalizedOffsets) {
            Console.WriteLine(p);
        }

        foreach (var r in _replacements) {
            Console.WriteLine(r);
            Console.WriteLine(GetNormalizedTransform3DFor(r));
        }
    }

    public IEnumerable<BasicEl> GenerateRandomLoop(int randAmount = 3, int startAmount = 8, int maxCount = 20) {
        const int iterations = 20;
        int added = 0;
        var layout = _startingLayout.Clone();

        // pick a random modification and try to fit it anywhere
        // do this until we have successfully placed N
        for (var i = 0; i < iterations; i++) {
            var current = RandHelper.RandFromList(_rand, _replacements);
            var curTransform = GetNormalizedTransform3DFor(current);

            // search the current circuit for where to put it
            for (int j = 0; j < layout.Count - 1; j++) {
                for (int k = j + 1; k < layout.Count; k++) {
                    var layoutNodes = layout.GetRange(j, k);
                    var transform = GetNormalizedTransform3DFor(layoutNodes);
                    if (transform.IsEqualApprox(curTransform)) {
                        added++;
                        layout = layout.ReplaceRange(j, k, current);
                        goto SearchDone;
                    }
                }
            }

        SearchDone: { }
        }

        Console.WriteLine("Added " + added + " pieces");
        return layout.AsBasicEl();
    }

    private BasicEl FromName(string name) => _basicPieces.Single(x => x.Name == name);
    private ElList FromList(params string[] names) {
        return new ElList(names.Select(FromName).Select(x => new El(x)).ToArray());
    }

    private Transform3D GetNormalizedTransform3DFor(ElList pieces) {
        var curPos = new Vector3();
        var curRot = Quaternion.Identity;
        foreach (var p in pieces.AsBasicEl()) {
            var origin = _normalizedOffsets[p.Name];
            var basis = _transforms[p.Name];
            curPos += curRot * origin;
            curRot *= basis;
        }

        return new Transform3D(new Basis(curRot), curPos);
    }
}
