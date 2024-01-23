using System;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public enum HigherIs {
    Good,
    Neutral,
    Bad
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PartFieldAttribute(object defaultValue, string howToApply, HigherIs higherIsGood = HigherIs.Good) : Attribute {
    public object DefaultValue { get; init; } = defaultValue;
    public string HowToApply { get; init; } = howToApply;
    public HigherIs HigherIsGood { get; init;} = higherIsGood;
}
