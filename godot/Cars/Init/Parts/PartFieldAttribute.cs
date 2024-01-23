using System;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PartFieldAttribute(object defaultValue, string howToApply) : Attribute {
    public object DefaultValue { get; init; } = defaultValue;
    public string HowToApply { get; init; } = howToApply;
}
