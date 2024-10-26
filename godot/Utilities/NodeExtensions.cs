using System.Collections.Generic;
using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public static class NodeExtensions {

    public static IEnumerable<T> GetAllChildrenOfType<T>(this Node node) where T : Node {
        foreach (var child in node.GetChildren()) {
            if (child is T ct) {
                yield return ct;
            }
            foreach (var t in GetAllChildrenOfType<T>(child))
                yield return t;
        }
    }
}
