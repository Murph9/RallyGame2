using System;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public record RelicType(string Name) {
    public static readonly RelicType BOUNCY = typeof(BouncyRelic).Name;
    public static readonly RelicType JUMP = typeof(JumpRelic).Name;
    public static readonly RelicType BIGFAN = typeof(BigFanRelic).Name;
    public static readonly RelicType FUELREDUCE = typeof(FuelReductionRelic).Name;
    public static readonly RelicType TYREWEARREDUCE = typeof(TyreWearReductionRelic).Name;

    public static implicit operator string(RelicType r) => r?.Name;
    public static implicit operator RelicType(string s) => new(s);

    public static readonly Type[] ALL_RELIC_CLASSES;
    public static readonly RelicType[] ALL_RELIC_TYPES;
    static RelicType() {
        var baseType = typeof(Relic);
        ALL_RELIC_CLASSES = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract).ToArray();

        ALL_RELIC_TYPES = typeof(RelicType)
                            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                            .Where(f => f.FieldType == typeof(RelicType))
                            .Select(x => (RelicType)x.GetValue(null))
                            .ToArray();
    }
}
