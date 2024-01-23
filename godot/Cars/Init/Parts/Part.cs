using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public class Part {
    public string Name { get; set; }
    public string Color { get; set; }
    [JsonIgnore]
    public Color PartColour { get; set; }
    public int CurrentLevel { get; set; }
    public double[] LevelCost { get; set; }

    public Dictionary<string, object>[] Levels { get; set; }

    public Part() {
        PartColour = Godot.Color.FromHtml(Color);
    }

    public Dictionary<string, object> GetLevel() => GetAllValues()[CurrentLevel];
    public Dictionary<string, object>[] GetAllValues() => Levels;

    public void Validate(IEnumerable<FieldInfo> allFields) {
        if (string.IsNullOrWhiteSpace(Name))
            throw new Exception("No name set for part with levels " + Levels.Length);
        if (CurrentLevel < 0 || CurrentLevel > Levels.Length - 1)
            throw new Exception($"Part {Name}: Current level is wrong ({CurrentLevel})");
        if (LevelCost.Length != Levels.Length)
            throw new Exception($"Part {Name}: Level {Levels.Length} has different amount to LevelCost {LevelCost.Length}");
        if (!Godot.Color.HtmlIsValid(Color))
            throw new Exception($"Part {Name}: no colour set");

        foreach (var props in Levels) {
            foreach (var field in props) {
                if (!allFields.Any(x => x.Name == field.Key)) {
                    throw new Exception($"Part {Name} applies prop '{field.Key}' but it doesn't exist to set");
                }
            }
        }
    }
}
