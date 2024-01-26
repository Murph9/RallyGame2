using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

public class PartReader {
    record FieldProps(FieldInfo Field, object DefaultValue, string Action, HigherIs HigherIs);

    public const string APPLY_SET = "apply_set";
    public const string APPLY_MIN = "apply_min";
    public const string APPLY_ADD = "apply_add";

    private readonly IHaveParts _self;
    private readonly ICollection<FieldProps> _fieldProps = [];
    public PartReader(IHaveParts self) {
        _self = self;
        foreach (var field in _self.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (Attribute.GetCustomAttribute(field, typeof(PartFieldAttribute)) is PartFieldAttribute partFieldAttribute) {
                _fieldProps.Add(new FieldProps(field, partFieldAttribute.DefaultValue, partFieldAttribute.HowToApply, partFieldAttribute.HigherIs));
            }
        }
    }

    public string ValidateAndSetFields() {
        var fieldInfos = _fieldProps.Select(x => x.Field);
        foreach (var part in _self.Parts) {
            part.Validate(fieldInfos);
        }

        // set defaults
        foreach (var fieldProp in _fieldProps) {
            fieldProp.Field.SetValue(_self, fieldProp.DefaultValue);
        }

        // then apply given method of setting them
        foreach (var part in _self.Parts) {
            var partValues = part.GetLevel();

            foreach (var fieldProp in _fieldProps) {
                var field = fieldProp.Field;

                if (!partValues.TryGetValue(field.Name, out object partValue))
                    continue;

                var jsonPartValue = (JsonElement)partValue;

                switch (fieldProp.Action) {
                    case APPLY_SET: ApplySet(field, _self, jsonPartValue); break;
                    case APPLY_MIN: ApplyMin(field, _self, jsonPartValue); break;
                    case APPLY_ADD: ApplyAdd(field, _self, jsonPartValue); break;
                    default:
                        throw new Exception("Unknown field action type: " + fieldProp.Action);
                }
            }
        }

        foreach (var fieldProp in _fieldProps)
            if (fieldProp.Field.GetValue(_self) == fieldProp.DefaultValue)
                return $"Field {fieldProp.Field.Name} was still on the default value of {fieldProp.DefaultValue}";

        return null;
    }

    public IEnumerable<PartResult> GetResults() {
        foreach (var fieldProp in _fieldProps) {
            var li = new List<Part>();
            foreach (var part in _self.Parts) {
                var partValues = part.GetLevel();
                if (partValues.ContainsKey(fieldProp.Field.Name))
                    li.Add(part);
            }
            yield return new PartResult(fieldProp.Field.Name, fieldProp.Field.GetValue(_self), fieldProp.HigherIs, li);
        }
    }

    private static void ApplySet(FieldInfo field, object self, JsonElement jsonPartValue) {
        if (field.FieldType == typeof(bool))
            field.SetValue(self, jsonPartValue.GetBoolean());
        else if (field.FieldType == typeof(int))
            field.SetValue(self, jsonPartValue.GetInt32());
        else if (field.FieldType == typeof(float))
            field.SetValue(self, jsonPartValue.GetSingle());
        else if (field.FieldType == typeof(double))
            field.SetValue(self, jsonPartValue.GetDouble());
        else if (field.FieldType == typeof(float[])) {
            var array = jsonPartValue.GetArrayLength();
            field.SetValue(self, jsonPartValue.Clone().EnumerateArray().Select(x => (float)x.GetDouble()).ToArray());
        } else {
            throw new Exception($"Unsupported field type: {field.FieldType} with APPLY_SET");
        }
    }

    private static void ApplyMin(FieldInfo field, object self, JsonElement jsonPartValue) {
        var currentValue = field.GetValue(self);
        if (field.FieldType == typeof(int))
            field.SetValue(self, Mathf.Min((int)currentValue, jsonPartValue.GetInt32()));
        else if (field.FieldType == typeof(float))
            field.SetValue(self, Mathf.Min((float)currentValue, (float)jsonPartValue.GetDouble()));
        else if (field.FieldType == typeof(double))
            field.SetValue(self, Mathf.Min((double)currentValue, jsonPartValue.GetDouble()));
        else {
            throw new Exception($"Unsupported field type: {field.FieldType} with APPLY_MIN");
        }
    }

    private static void ApplyAdd(FieldInfo field, object self, JsonElement jsonPartValue) {
        if (field.FieldType == typeof(int))
            field.SetValue(self, (int)field.GetValue(self) + jsonPartValue.GetInt32());
        else if (field.FieldType == typeof(float))
            field.SetValue(self, (float)field.GetValue(self) + jsonPartValue.GetSingle());
        else if (field.FieldType == typeof(double))
            field.SetValue(self, (double)field.GetValue(self) + jsonPartValue.GetDouble());
        else {
            throw new Exception($"Unsupported field type: {field.FieldType} with APPLY_ADD");
        }
    }
}
