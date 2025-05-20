using System.Collections.Generic;
using System.Linq;

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
            yield return GetPieceByName("left_long_90");
            yield return GetPieceByName("left_long_90");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left_long_90");
            yield return GetPieceByName("left_long_90");
            yield break;

        } else if (layout == CircuitLayout.VeryLongLine) {
            for (int i = 0; i < 100; i++) {
                yield return GetPieceByName("straight");
            }
            yield return GetPieceByName("left_long_90");
            yield return GetPieceByName("left_long_90");
            for (int i = 0; i < 100; i++) {
                yield return GetPieceByName("straight");
            }
            yield return GetPieceByName("left_long_90");
            yield return GetPieceByName("left_long_90");
            yield break;
        }
    }

    private WorldPiece GetPieceByName(string name) {
        return _pieces.First(x => x.Name == name);
    }
}
