using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public interface IHaveParts {
    PartCategory PartCategory { get; }
    List<PartDetails> Parts { get; }
    Dictionary<string, PartLevel> PartLevels { get; set; }
    IEnumerable<PartDetails> GetAllPartsInTree();
    IEnumerable<PartResult> GetPartResultsInTree();
    /// <summary>
    /// Returns the per-field differences that upgrading <paramref name="part"/> from
    /// <paramref name="fromLevel"/> to <paramref name="toLevel"/> would produce.
    /// </summary>
    IEnumerable<PartDelta> CalcDeltaForPart(PartDetails part, PartLevel fromLevel, PartLevel toLevel);
}

public record PartResult(string Name, object Value, HigherIs HigherIsGood, IEnumerable<PartDetails> BecauseOf) {
    public string Name { get; init; } = Name;
    public object Value { get; init; } = Value;
    public HigherIs HigherIsGood { get; init; } = HigherIsGood;
    public IEnumerable<PartDetails> BecauseOf { get; init; } = BecauseOf;
}
