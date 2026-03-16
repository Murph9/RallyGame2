using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public class PartDetails {
    public string Name { get; set; }
    public string Color { get; set; }
    public PartLevel CurrentLevel { get; set; }
    public double[] LevelCost { get; set; }
    public string Icon { get; set; }
    [JsonIgnore]
    public Texture2D IconImage { get; private set; }

    public Dictionary<string, object>[] Levels { get; set; }

    public Dictionary<string, object> GetLevel() => GetAllValues()[(int)CurrentLevel];
    public Dictionary<string, object>[] GetAllValues() => Levels;

    public void Validate(IEnumerable<FieldInfo> allFields) {
        if (string.IsNullOrWhiteSpace(Name))
            throw new Exception("No name set for part with levels " + Levels.Length);
        if (LevelCost.Length != Levels.Length)
            throw new Exception($"Part {Name}: Level {Levels.Length} has different amount to LevelCost {LevelCost.Length}");
        if (!Godot.Color.HtmlIsValid(Color))
            throw new Exception($"Part {Name}: no colour set");
        if (string.IsNullOrWhiteSpace(Icon))
            throw new Exception("Every part needs an icon png set");

        IconImage = ResourceLoader.Load<Texture2D>("res://assets/images/upgrades/" + Icon);
        IconImage ??= ResourceLoader.Load<Texture2D>("res://icon.svg");

        foreach (var props in Levels) {
            foreach (var field in props) {
                if (!allFields.Any(x => x.Name == field.Key)) {
                    throw new Exception($"Part {Name} applies prop '{field.Key}' but it doesn't exist to set");
                }
            }
        }
    }
}
