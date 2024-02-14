using System.Collections.Generic;

namespace murph9.RallyGame2.godot.World.Search;

public interface ICircuitGenerator {
    IEnumerable<BasicEl> GenerateRandomLoop(int randAmount = 3, int startAmount = 8, int maxCount = 20);
}
