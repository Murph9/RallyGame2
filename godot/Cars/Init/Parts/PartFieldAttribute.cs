using System;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public enum HigherIs {
    Good,
    Neutral,
    Bad
}

public enum DefaultIs {
    NotOkay,
    Okay
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PartFieldAttribute(object defaultValue, string howToApply, HigherIs higherIsGood = HigherIs.Good, DefaultIs defaultIs = DefaultIs.NotOkay) : Attribute {
    public object DefaultValue { get; init; } = defaultValue;
    public string HowToApply { get; init; } = howToApply;
    public HigherIs HigherIs { get; init;} = higherIsGood;
    public DefaultIs DefaultIs { get; init; } = defaultIs;
}
