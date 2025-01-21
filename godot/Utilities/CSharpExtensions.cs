using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities;

public static class CSharpExtensions {
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
       => self.Select((item, index) => (item, index));
}
