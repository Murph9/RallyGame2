using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.Init.Part;

public class Part {
    public string Name { get; set; }
    public int CurrentLevel { get; set; }
    public Dictionary<string, double>[] Levels { get; set; }
    public Dictionary<string, double> GetLevel() => Levels[CurrentLevel];
    public Dictionary<string, double> GetValues(IEnumerable<string> propNames) {
        return GetLevel().Where(x => propNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }
}
