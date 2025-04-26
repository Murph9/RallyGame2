using Godot;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Utilities;

public static class RandHelper {
    public static T RandFromList<T>(IList<T> list) {
        return list[(int)(GD.Randi() % list.Count)];
    }
    public static T RandFromList<T>(RandomNumberGenerator _rand, IList<T> list) {
        return list[(int)(_rand.Randi() % list.Count)];
    }

    public static Color GetRandColour(RandomNumberGenerator _rand) {
        return Color.FromHsv(_rand.Randf(), 0.8f, 0.8f);
    }
}
