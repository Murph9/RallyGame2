using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace murph9.RallyGame2.godot.Cars.Init;

public class Part {
    public string Name { get; set; }
    public string Color { get; set; }
    [JsonIgnore]
    public Color PartColour { get; set; }
    public int CurrentLevel { get; set; }
    public double[] LevelCost { get; set; }

    public Dictionary<string, double>[] Levels { get; set; }
    public Dictionary<string, double> GetLevel() => Levels[CurrentLevel];

    public Part() {
        PartColour = Godot.Color.FromHtml(Color);
    }

    public Dictionary<string, double> GetValues(IEnumerable<string> propNames) {
        return GetLevel().Where(x => propNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }

    public void Validate() {
        if (string.IsNullOrWhiteSpace(Name))
            throw new Exception("No name set for part with levels " + Levels.Length);
        if (CurrentLevel < 0 || CurrentLevel > Levels.Length - 1)
            throw new Exception($"Part {Name}: Current level is wrong ({CurrentLevel})");
        if (LevelCost.Length != Levels.Length)
            throw new Exception($"Part {Name}: Level {Levels.Length} has different amount to LevelCost {LevelCost.Length}");
        if (!Godot.Color.HtmlIsValid(Color))
            throw new Exception($"Part {Name}: no colour set");
    }
}

public interface IHaveParts {
    List<Part> Parts { get; }
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PartFieldAttribute : Attribute {
    public object? DefaultValue { get; init; }
    public PartFieldAttribute(object? defaultValue) {
        DefaultValue = defaultValue;
    }
}

public class PartReader {
    private readonly IHaveParts _self;
    private readonly Dictionary<FieldInfo, object?> _fields;
    public PartReader(IHaveParts self) {
        _self = self;
        _fields = new Dictionary<FieldInfo, object?>();
        foreach (var field in _self.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (Attribute.GetCustomAttribute(field, typeof(PartFieldAttribute)) is PartFieldAttribute partFieldAttribute) {
                _fields.Add(field, partFieldAttribute.DefaultValue);
            }
        }
    }

    public void Init() {
        foreach (var part in _self.Parts) {
            part.Validate();
        }

        foreach (var field in _fields) {
            field.Key.SetValue(_self, field.Value);
        }
    }

    public IEnumerable<FieldInfo> GetFields() => _fields.Keys;

    public Dictionary<string, double> ResultAsDict() {
        return GetFields().ToDictionary(x => x.Name, x => {
            if (x.FieldType == typeof(double))
                return (double)x.GetValue(_self);
            if (x.FieldType == typeof(int))
                return (int)x.GetValue(_self);

            return double.MinValue;
        });
    }

    public Dictionary<string, List<Part>> GetValueCauses() {
        var dict = ResultAsDict().ToDictionary(x => x.Key, x => new List<Part>());

        foreach (var part in _self.Parts) {
            var partValues = part.GetLevel();

            foreach (var field in GetFields()) {
                if (partValues.ContainsKey(field.Name))
                    dict[field.Name].Add(part);
            }
        }

        return dict;
    }

    public bool AreAllSet() {
        foreach (var field in _fields)
            if (field.Key.GetValue(_self) == field.Value)
                return false;

        return true;
    }
}
