using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public interface IHaveParts {
    List<Part> Parts { get; }
    IEnumerable<PartResult> GetResults();
}

public record PartResult(string Name, object Value, HigherIs HigherIsGood, IEnumerable<Part> BecauseOf) {
    public string Name { get; init; } = Name;
    public object Value { get; init; } = Value;
    public HigherIs HigherIsGood { get; init; } = HigherIsGood;
    public IEnumerable<Part> BecauseOf { get; init; } = BecauseOf;
}
