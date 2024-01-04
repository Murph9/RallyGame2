using Godot;
using murph9.RallyGame2.godot.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace murph9.RallyGame2.godot.Debug;

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

public partial class DebugGUI : VBoxContainer {

    // from https://github.com/WeaverDev/DebugGUIGraph/blob/master/addons/DebugGUI/Windows/GraphWindow.cs
    static DebugGUI Instance;

    record Mapping {
        public Graph.Dataset Dataset;
        public Graph Graph;
        public Node Node;
        public FieldInfo FieldInfo;
        public PropertyInfo PropertyInfo;
    }

    private static readonly double RESCAN_TIMER = 5; // big perf
    private double _rescanTimer = RESCAN_TIMER;
    private readonly List<Graph> graphs = new();
    private readonly Dictionary<Node, IList<Mapping>> Datasets = new ();

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
        CallDeferred(nameof(GetGraphValues));
        CallDeferred(nameof(CleanupOldGraphs));

        _rescanTimer -= delta;
        if (_rescanTimer < 0) {
            _rescanTimer = RESCAN_TIMER;
            CallDeferred(nameof(ForceReinitializeAttributes));
        }
        //Position = new Vector2(GetViewportRect().Size.X - Size.X, 0);
    }

    private void LoadAllAttributes(Node node) {
        foreach (var child in node.GetChildren())
            LoadAllAttributes(child);

        var nodeType = node.GetType();

        var objectProperties = nodeType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var objProp in objectProperties) {
            if (Attribute.GetCustomAttribute(objProp, typeof(DebugGUIGraphAttribute)) is not DebugGUIGraphAttribute graphAttribute)
                continue;

            if (!DebugHelper.IsNumeric(objProp.GetValue(node))) {
                GD.PrintErr("Field " + objProp.Name + " probably isn't a number, so not graphing it");
                continue;
            }

            AddMapping(node, graphAttribute, objProp, null);
        }

        var objectFields = nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var objField in objectFields) {
            if (Attribute.GetCustomAttribute(objField, typeof(DebugGUIGraphAttribute)) is not DebugGUIGraphAttribute graphAttribute)
                continue;

            if (!DebugHelper.IsNumeric(objField.GetValue(node))) {
                GD.PrintErr("Field " + objField.Name + " probably isn't a number, so not graphing it");
                continue;
            }

            AddMapping(node, graphAttribute, null, objField);
        }
    }

    private void AddMapping(Node node, DebugGUIGraphAttribute graphAttribute, PropertyInfo prop, FieldInfo field) {
        var name = prop?.Name ?? field.Name;

        var dataset = new Graph.Dataset(name, graphAttribute.Min, graphAttribute.Max, graphAttribute.AutoScale) {
            Color = graphAttribute.Color
        };
        var mapping = new Mapping() {
            Dataset = dataset,
            FieldInfo = field,
            PropertyInfo = prop,
            Node = node
        };

        // store nodes to check if they are removed
        if (!Datasets.ContainsKey(node))
            Datasets.Add(node, new List<Mapping>());
        // check we haven't already added this one
        if (Datasets[node].Any(x => x.Node == node && x.FieldInfo == field && x.PropertyInfo == prop))
            return;
        // store fields against nodes
        Datasets[node].Add(mapping);

        // create the graph if required
        var graph = graphs.FirstOrDefault(x => x.Group == graphAttribute.Group);
        if (graph == null) {
            graph = new Graph(new Vector2(200, 80), graphAttribute.Group);
            graphs.Add(graph);
            AddChild(graph);
        }

        graph.AddDataset(dataset);
        mapping.Graph = graph;
    }

    private void GetGraphValues() {
        foreach (var nodeMaps in Datasets)
        {
            if (!IsInstanceValid(nodeMaps.Key))
                continue;

            foreach (var attr in nodeMaps.Value) {
                if (attr.FieldInfo is FieldInfo fieldInfo) {
                    float? val = Convert.ToSingle(fieldInfo.GetValue(nodeMaps.Key));
                    if (val != null)
                        attr.Dataset.Push(val.Value);
                }
                if (attr.PropertyInfo is PropertyInfo propertyInfo)
                {
                    float? val = Convert.ToSingle(propertyInfo.GetValue(nodeMaps.Key, null));
                    if (val != null)
                        attr.Dataset.Push(val.Value);
                }
            }
        }
    }

    private void CleanupOldGraphs() {
        // Clear out any graphs that no longer have attached nodes to the scene
        foreach (var node in Datasets.ToList())
        {
            if (IsInstanceValid(node.Key))
                continue;

            foreach (var key in node.Value)
            {
                var graph = key.Graph;
                graph.RemoveDataset(key.Dataset);
                if (graph.DatasetCount() == 0) {
                    RemoveChild(graph);
                    graphs.Remove(graph);
                }
            }

            Datasets.Remove(node.Key);
        }
    }
}
