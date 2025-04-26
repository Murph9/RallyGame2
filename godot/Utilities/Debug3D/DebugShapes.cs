using Godot;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Utilities.Debug3D;

class DebugThing(Node3D node, double timeAlive) {
    public Node3D Node = node;
    public double TimeAlive = timeAlive;
}

public partial class DebugShapes : Node3D {

    private const double MAX_AGE = 10;

    public static DebugShapes INSTANCE { get; private set; }
    private static readonly Dictionary<string, DebugThing> _thingSet = [];

    public DebugShapes() {
        if (INSTANCE != null) throw new Exception("DebugShapes can't be initialized twice");
        INSTANCE = this;
    }

    public override void _Process(double delta) {
        var markedKeys = new List<string>();
        foreach (var thing in _thingSet) {
            thing.Value.TimeAlive += delta;
            if (thing.Value.TimeAlive > MAX_AGE) {
                markedKeys.Add(thing.Key);
            }
        }

        foreach (var markedKey in markedKeys) {
            RemoveChild(_thingSet[markedKey].Node);
            _thingSet.Remove(markedKey);
        }
    }

    public void Add(string key, Node3D obj) {
        if (_thingSet.TryGetValue(key, out DebugThing? value)) {
            RemoveChild(value.Node);
        }
        _thingSet[key] = new DebugThing(obj, 0);
        AddChild(obj);
    }

    public void AddLineDebug3D(string key, Vector3 start, Vector3 end, Color colour) {
        LineDebug3D lineDebug;
        if (_thingSet.TryGetValue(key, out DebugThing? value)) {
            lineDebug = value.Node as LineDebug3D;
        } else {
            lineDebug = new LineDebug3D();
            _thingSet[key] = new DebugThing(lineDebug, 0);
            AddChild(_thingSet[key].Node);
        }
        lineDebug.Start = start;
        lineDebug.End = end;
        lineDebug.Colour = colour;
    }

    public void AddCircleXYDebug3D(string key, Vector3 center, float radius, int vertexCount, Color colour) {
        CircleXYDebug3D circleDebug;
        if (_thingSet.TryGetValue(key, out DebugThing? value)) {
            circleDebug = value.Node as CircleXYDebug3D;
        } else {
            circleDebug = new CircleXYDebug3D();
            _thingSet[key] = new DebugThing(circleDebug, 0);
            AddChild(_thingSet[key].Node);
        }
        circleDebug.Center = center;
        circleDebug.Radius = radius;
        circleDebug.VertexCount = vertexCount;
        circleDebug.Colour = colour;
    }

    public void Remove(string key) {
        if (_thingSet.TryGetValue(key, out DebugThing? value)) {
            RemoveChild(value.Node);
        }
    }
}
