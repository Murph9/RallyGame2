using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public record RelicType(string Name) {
    public static implicit operator string(RelicType r) => r?.Name;
    public static implicit operator RelicType(string s) => new(s);

    public static readonly ICollection<Type> ALL_RELIC_CLASSES;
    static RelicType() {
        var baseType = typeof(Relic);
        ALL_RELIC_CLASSES = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();
    }
}
