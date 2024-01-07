using Godot;
using murph9.RallyGame2.godot.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace murph9.RallyGame2.godot.Utilities;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DebugGUIGraphAttribute : Attribute {
    public float Min { get; private set; }
    public float Max { get; private set; }
    public Color Color { get; private set; }
    public int Group { get; private set; }
    public bool AutoScale { get; private set; }

    public DebugGUIGraphAttribute(
        // Line color
        float r = 1,
        float g = 1,
        float b = 1,
        // Values at top/bottom of graph
        float min = 0,
        float max = 1,
        // Offset position on screen
        int group = 0,
        // Auto-adjust min/max to fit the values
        bool autoScale = true
    )
    {
        Color = new Color(r, g, b, 0.9f);
        Min = min;
        Max = max;
        Group = group;
        AutoScale = autoScale;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DebugGUITextAttribute : Attribute {
    public Color Color { get; private set; }

    public DebugGUITextAttribute(
        // Color
        float r = 1,
        float g = 1,
        float b = 1
    )
    {
        Color = new Color(r, g, b, 0.9f);
    }
}

public partial class DebugGUI : VBoxContainer {

    // from https://github.com/WeaverDev/DebugGUIGraph/blob/master/addons/DebugGUI/Windows/GraphWindow.cs
    static DebugGUI Instance;

    record GraphMapping {
        public Graph.Dataset Dataset;
        public Graph Graph;
        public Node Node;
        public FieldInfo FieldInfo;
        public PropertyInfo PropertyInfo;
        public int Group;
    }
    record TextMapping {
        public Label Label;
        public Node Node;
        public FieldInfo FieldInfo;
        public PropertyInfo PropertyInfo;
    }

    private static readonly double RESCAN_TIMER = 5; // big perf
    private double _rescanTimer = RESCAN_TIMER;

    private readonly Dictionary<Node, IList<GraphMapping>> Datasets = new ();
    private readonly Dictionary<Node, IList<TextMapping>> LabelSets = new ();

    public static void ForceReinitializeAttributes() {
        Instance.LoadAllAttributes(Instance.GetTree().Root);
    }

    public override void _Ready() {
        if (Instance != null) throw new Exception("Don't initialise DebugGUI twice");
        Instance = this;

        AddChild(new Label() {
            Text = "Debug Window:"
        });

        LoadAllAttributes(GetTree().Root);
    }

    public override void _Process(double delta) {
        CallDeferred(nameof(GetValues));
        CallDeferred(nameof(CleanupOld));

        _rescanTimer -= delta;
        if (_rescanTimer < 0) {
            _rescanTimer = RESCAN_TIMER;
            CallDeferred(nameof(ForceReinitializeAttributes));
        }
    }

    private void LoadAllAttributes(Node node) {
        foreach (var child in node.GetChildren())
            LoadAllAttributes(child);

        var nodeType = node.GetType();

        var objectProperties = nodeType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var objProp in objectProperties) {
            if (Attribute.GetCustomAttribute(objProp, typeof(DebugGUIGraphAttribute)) is DebugGUIGraphAttribute graphAttribute) {
                if (DebugHelper.IsNumeric(objProp.GetValue(node))) {
                    AddGraphMapping(node, graphAttribute, objProp, null);
                } else {
                    GD.PrintErr("Field " + objProp.Name + " probably isn't a number, so not graphing it");
                }
            }

            if (Attribute.GetCustomAttribute(objProp, typeof(DebugGUITextAttribute)) is DebugGUITextAttribute textAttribute) {
                AddTextMapping(node, textAttribute, objProp, null);
            }
        }

        var objectFields = nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var objField in objectFields) {
            if (Attribute.GetCustomAttribute(objField, typeof(DebugGUIGraphAttribute)) is DebugGUIGraphAttribute graphAttribute) {
                if (DebugHelper.IsNumeric(objField.GetValue(node))) {
                    AddGraphMapping(node, graphAttribute, null, objField);
                } else {
                    GD.PrintErr("Field " + objField.Name + " probably isn't a number, so not graphing it");
                }
            }

            if (Attribute.GetCustomAttribute(objField, typeof(DebugGUITextAttribute)) is DebugGUITextAttribute textAttribute) {
                AddTextMapping(node, textAttribute, null, objField);
            }
        }
    }

    private void AddTextMapping(Node node, DebugGUITextAttribute textAttribute, PropertyInfo prop, FieldInfo field) {
        var name = prop?.Name ?? field.Name;

        // store nodes to check if they are removed
        if (!LabelSets.ContainsKey(node))
            LabelSets.Add(node, new List<TextMapping>());
        // check we haven't already added this one
        if (LabelSets[node].Any(x => x.Node == node && x.FieldInfo == field && x.PropertyInfo == prop))
            return;

        var label = new Label();
        label.AddThemeColorOverride("font_color", textAttribute.Color);
        var mapping = new TextMapping() {
            Label = label,
            FieldInfo = field,
            PropertyInfo = prop,
            Node = node
        };
        // store fields against nodes
        LabelSets[node].Add(mapping);

        // add label to scene
        AddChild(label);
    }

    private void AddGraphMapping(Node node, DebugGUIGraphAttribute graphAttribute, PropertyInfo prop, FieldInfo field) {
        // store nodes to check if they are removed
        if (!Datasets.ContainsKey(node))
            Datasets.Add(node, new List<GraphMapping>());
        // check we haven't already added this one
        if (Datasets[node].Any(x => x.Node == node && x.FieldInfo == field && x.PropertyInfo == prop))
            return;

        var name = prop?.Name ?? field.Name;

        var dataset = new Graph.Dataset(name, graphAttribute.Min, graphAttribute.Max, graphAttribute.AutoScale) {
            Color = graphAttribute.Color
        };
        var mapping = new GraphMapping() {
            Group = graphAttribute.Group,
            Dataset = dataset,
            FieldInfo = field,
            PropertyInfo = prop,
            Node = node
        };
        // store fields against nodes
        Datasets[node].Add(mapping);

        // create the graph if required
        var graph = Datasets[node].FirstOrDefault(x => x.Group == graphAttribute.Group)?.Graph;
        if (graph == null) {
            graph = new Graph(new Vector2(200, 80));
            AddChild(graph);
        }

        // then add to scene
        graph.AddDataset(dataset);
        mapping.Graph = graph;
    }

    private void GetValues() {
        foreach (var nodeMaps in Datasets) {
            if (!IsInstanceValid(nodeMaps.Key))
                continue;

            foreach (var attr in nodeMaps.Value) {
                if (attr.FieldInfo is FieldInfo fieldInfo) {
                    float? val = Convert.ToSingle(fieldInfo.GetValue(nodeMaps.Key));
                    if (val != null)
                        attr.Dataset.Push(val.Value);
                }
                if (attr.PropertyInfo is PropertyInfo propertyInfo) {
                    float? val = Convert.ToSingle(propertyInfo.GetValue(nodeMaps.Key, null));
                    if (val != null)
                        attr.Dataset.Push(val.Value);
                }
            }
        }

        foreach (var label in LabelSets) {
            if (!IsInstanceValid(label.Key))
                continue;

            foreach (var attr in label.Value) {
                if (attr.FieldInfo is FieldInfo fieldInfo) {
                    attr.Label.Text = fieldInfo.Name + ": " + fieldInfo.GetValue(label.Key)?.ToString();
                }
                if (attr.PropertyInfo is PropertyInfo propertyInfo) {
                    attr.Label.Text = propertyInfo.Name + ": " + propertyInfo.GetValue(label.Key)?.ToString();
                }
            }
        }
    }

    private void CleanupOld() {
        // Clear out any nodes that no longer have attached nodes to the scene

        foreach (var node in Datasets.ToList()) {
            if (IsInstanceValid(node.Key))
                continue;

            foreach (var key in node.Value) {
                var graph = key.Graph;
                graph.RemoveDataset(key.Dataset);
                if (graph.DatasetCount() == 0) {
                    RemoveChild(graph);
                }
            }

            Datasets.Remove(node.Key);
        }

        foreach (var label in LabelSets.ToList()) {
            if (IsInstanceValid(label.Key))
                continue;

            foreach (var key in label.Value) {
                var realLabel = key.Label;
                RemoveChild(realLabel);
            }

            LabelSets.Remove(label.Key);
        }
    }
}
