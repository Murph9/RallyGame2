using Godot;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities;

public static class RandHelper {
    public static T RandFromList<T>(ICollection<T> list) {
        return list.ElementAt((int)(GD.Randi() % list.Count));
    }

    public static T RandFromList<T>(RandomNumberGenerator _rand, ICollection<T> list) {
        return list.ElementAt((int)(_rand.Randi() % list.Count));
    }

    public static Color GetRandColour(RandomNumberGenerator _rand) {
        return Color.FromHsv(_rand.Randf(), 0.8f, 0.8f);
    }
}
