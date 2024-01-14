using Godot;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.Init.Part;

public class Part {
    public string Name { get; set; }
    public string Color { get; set; }
    [JsonIgnore]
    public Color PartColour { get; set; }
    public int CurrentLevel { get; set; }
    public double[] LevelCost { get; set; }

    public Dictionary<string, double>[] Levels { get; set; }
    public Dictionary<string, double> GetLevel() => Levels[CurrentLevel];
    public Dictionary<string, double> GetValues(IEnumerable<string> propNames) {
        return GetLevel().Where(x => propNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }

    public void Validate() {
        if (string.IsNullOrWhiteSpace(Name))
            throw new System.Exception("No name set for part with levels " + Levels.Length);
        if (CurrentLevel < 0 || CurrentLevel > Levels.Length - 1)
            throw new System.Exception($"Part {Name}: Current level is wrong ({CurrentLevel})");
        if (LevelCost.Length != Levels.Length)
            throw new System.Exception($"Part {Name}: Level {Levels.Length} has different amount to LevelCost {LevelCost.Length}");
        if (!Godot.Color.HtmlIsValid(Color))
            throw new System.Exception($"Part {Name}: no colour set");
    }
}
