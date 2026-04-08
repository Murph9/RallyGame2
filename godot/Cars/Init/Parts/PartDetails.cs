using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public partial class PartDetails {
    public string Code { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public double[] LevelCost { get; set; }
    public string Icon { get; set; }
    [JsonIgnore]
    public Texture2D IconImage { get; private set; }

    public Dictionary<string, object>[] Levels { get; set; }

    public Dictionary<string, object> GetLevel(PartLevel level) => GetAllValues()[(int)level];
    public Dictionary<string, object>[] GetAllValues() => Levels;

    public void Validate(IEnumerable<FieldInfo> allFields) {
        if (string.IsNullOrWhiteSpace(Code))
            throw new Exception("No code set for part with levels " + Levels.Length);
        if (string.IsNullOrWhiteSpace(Name))
            throw new Exception($"Part '{Code}': Name not populated (missing registry entry?).");
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
