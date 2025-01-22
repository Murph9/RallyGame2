using System.Collections.Generic;
using System.Linq;
using murph9.RallyGame2.godot.World.DynamicPieces;

namespace murph9.RallyGame2.godot.World;

public class FixedPieceLayoutGenerator(ICollection<WorldPiece> pieces) {

    private readonly WorldPiece[] _pieces = [.. pieces];

    public enum CircuitLayout {
        SimpleLoop,
        LargeCircle,
        VeryLongLine
    }

    public IEnumerable<WorldPiece> Generate(CircuitLayout layout) {
        if (layout == CircuitLayout.SimpleLoop) {
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left");
            yield return GetPieceByName("left");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left");
            yield return GetPieceByName("left");

        } else if (layout == CircuitLayout.LargeCircle) {
            yield return GetPieceByName("straight");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            yield break;

        } else if (layout == CircuitLayout.VeryLongLine) {
            for (int i = 0; i < 100; i++) {
                yield return GetPieceByName("straight");
            }
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            for (int i = 0; i < 100; i++) {
                yield return GetPieceByName("straight");
            }
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            yield break;
        }
    }

    private WorldPiece GetPieceByName(string name) {
        return _pieces.First(x => x.Name == name);
    }
}
