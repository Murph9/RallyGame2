using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

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

                if (!partValues.TryGetValue(field.Name, out object value))
                    continue;

                var jsonValue = (JsonElement)value;

                var currentValue = field.GetValue(_self);

                if (fieldEntry.Value.Action == APPLY_SET) {
                    if (field.FieldType == typeof(bool))
                        field.SetValue(_self, jsonValue.GetBoolean());
                    else if (field.FieldType == typeof(int))
                        field.SetValue(_self, jsonValue.GetDouble());
                    else if (field.FieldType == typeof(float))
                        field.SetValue(_self, (float)jsonValue.GetDouble());
                    else if (field.FieldType == typeof(double))
                        field.SetValue(_self, jsonValue.GetInt32());
                    else if (field.FieldType == typeof(float[])) {
                        var array = jsonValue.GetArrayLength();
                        field.SetValue(_self, jsonValue.Clone().EnumerateArray().Select(x => (float)x.GetDouble()).ToArray());
                    }

                } else if (fieldEntry.Value.Action == APPLY_MIN) {
                    if (field.FieldType == typeof(int))
                        field.SetValue(_self, Mathf.Min((int)currentValue, jsonValue.GetInt32()));
                    else if (field.FieldType == typeof(float))
                        field.SetValue(_self, Mathf.Min((float)currentValue, (float)jsonValue.GetDouble()));
                    else if (field.FieldType == typeof(double))
                        field.SetValue(_self, Mathf.Min((double)currentValue, jsonValue.GetDouble()));
                    else {
                        throw new Exception($"Unsupported option: {field.FieldType} with APPLY_MIN");
                    }

                } else if (fieldEntry.Value.Action == APPLY_ADD) {
                    if (field.FieldType == typeof(int))
                        field.SetValue(_self, (int)currentValue + jsonValue.GetInt32());
                    else if (field.FieldType == typeof(float))
                        field.SetValue(_self, (float)currentValue + (float)jsonValue.GetDouble());
                    else if (field.FieldType == typeof(double))
                        field.SetValue(_self, (double)currentValue + jsonValue.GetDouble());
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
