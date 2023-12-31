using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public class WorldPieceCircuitGenerator {
    class StackPiece {
        public WorldPieces.Piece Piece;
        public Vector3 Position = new ();
        public Quaternion Rotation = Quaternion.Identity;
        public Aabb Aabb;
    }

    private readonly RandomNumberGenerator _rand = new ();
    private readonly WorldPieces.Piece[] _pieces;

    public enum CircuitLayout {
        SimpleLoop,
        LargeCircle
    }

    public WorldPieceCircuitGenerator(ICollection<WorldPieces.Piece> pieces) {
        _pieces = pieces.ToArray();
    }

    public IEnumerable<WorldPieces.Piece> GenerateFixed(CircuitLayout layout) {
        if (layout == CircuitLayout.SimpleLoop) {
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left");
            yield return GetPieceByName("left");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left");
            yield return GetPieceByName("left");
            yield break;
        }

        if (layout == CircuitLayout.LargeCircle) {
            yield return GetPieceByName("straight");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            yield break;
        }

        yield return null;
    }
    private WorldPieces.Piece GetPieceByName(string name) {
        return _pieces.First(x => x.Name == name);
    }

    
    public IEnumerable<WorldPieces.Piece> GenerateRandomCount(WorldPieces.Piece[] pieces, int count) {
        for (int i = 0; i < count; i++) {
            yield return pieces[_rand.RandiRange(0, pieces.Length - 1)];
        }
    }

}
