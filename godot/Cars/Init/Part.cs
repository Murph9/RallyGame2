using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init;

public class Part {
    public string Name { get; set; }
    public string Color { get; set; }
    [JsonIgnore]
    public Color PartColour { get; set; }
    public int CurrentLevel { get; set; }
    public double[] LevelCost { get; set; }

    // TODO do not let JSONElement out of this class
    public Dictionary<string, JsonElement>[] Levels { get; set; }
    public Dictionary<string, JsonElement> GetLevel() => Levels[CurrentLevel];

    public Part() {
        PartColour = Godot.Color.FromHtml(Color);
    }

    public Dictionary<string, JsonElement> GetValues(IEnumerable<string> propNames) {
        return GetLevel().Where(x => propNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }

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

public interface IHaveParts {
    List<Part> Parts { get; }
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PartFieldAttribute(object defaultValue, string howToApply) : Attribute {
    public object DefaultValue { get; init; } = defaultValue;
    public string HowToApply { get; init; } = howToApply;
}

public class PartReader {
    record FieldProps(object? DefaultValue, string Action);

    public const string APPLY_SET = "apply_set";
    public const string APPLY_MIN = "apply_min";
    public const string APPLY_ADD = "apply_add";

    private readonly IHaveParts _self;
    private readonly Dictionary<FieldInfo, FieldProps> _fields = [];
    public PartReader(IHaveParts self) {
        _self = self;
        foreach (var field in _self.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (Attribute.GetCustomAttribute(field, typeof(PartFieldAttribute)) is PartFieldAttribute partFieldAttribute) {
                _fields.Add(field, new FieldProps(partFieldAttribute.DefaultValue, partFieldAttribute.HowToApply));
            }
        }
    }

    public bool ValidateAndSetFields() {
        foreach (var part in _self.Parts) {
            part.Validate(_fields.Keys);
        }

        // set defaults
        foreach (var field in _fields) {
            field.Key.SetValue(_self, field.Value.DefaultValue);
        }

        // then apply given method of setting them
        foreach (var part in _self.Parts) {
            var partValues = part.GetLevel();

            foreach (var fieldEntry in _fields) {
                var field = fieldEntry.Key;

                if (!partValues.TryGetValue(field.Name, out JsonElement value))
                    continue;

                var currentValue = field.GetValue(_self);

                if (fieldEntry.Value.Action == APPLY_SET) {
                    if (field.FieldType == typeof(bool))
                        field.SetValue(_self, value.GetBoolean());
                    else if (field.FieldType == typeof(int))
                        field.SetValue(_self, value.GetDouble());
                    else if (field.FieldType == typeof(float))
                        field.SetValue(_self, (float)value.GetDouble());
                    else if (field.FieldType == typeof(double))
                        field.SetValue(_self, value.GetInt32());
                    else if (field.FieldType == typeof(float[])) {
                        var array = value.GetArrayLength();
                        field.SetValue(_self, value.Clone().EnumerateArray().Select(x => (float)x.GetDouble()).ToArray());
                    }

                } else if (fieldEntry.Value.Action == APPLY_MIN) {
                    if (field.FieldType == typeof(int))
                        field.SetValue(_self, Mathf.Min((int)currentValue, value.GetInt32()));
                    else if (field.FieldType == typeof(float))
                        field.SetValue(_self, Mathf.Min((float)currentValue, (float)value.GetDouble()));
                    else if (field.FieldType == typeof(double))
                        field.SetValue(_self, Mathf.Min((double)currentValue, value.GetDouble()));
                    else {
                        throw new Exception($"Unsupported option: {field.FieldType} with APPLY_MIN");
                    }

                } else if (fieldEntry.Value.Action == APPLY_ADD) {
                    if (field.FieldType == typeof(int))
                        field.SetValue(_self, (int)currentValue + value.GetInt32());
                    else if (field.FieldType == typeof(float))
                        field.SetValue(_self, (float)currentValue + (float)value.GetDouble());
                    else if (field.FieldType == typeof(double))
                        field.SetValue(_self, (double)currentValue + value.GetDouble());
                    else {
                        throw new Exception($"Unsupported option: {field.FieldType} with APPLY_ADD");
                    }
                }
            }
        }

        return AreAllSet();
    }

    public IEnumerable<FieldInfo> GetFields() => _fields.Keys;

    public Dictionary<string, object> ResultAsDict() {
        return GetFields().ToDictionary(x => x.Name, x => x.GetValue(_self));
    }

    public Dictionary<string, List<Part>> GetValueCauses() {
        var dict = GetFields().ToDictionary(x => x.Name, x => new List<Part>());

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
            if (field.Key.GetValue(_self) == field.Value.DefaultValue)
                return false;

        return true;
    }
}
