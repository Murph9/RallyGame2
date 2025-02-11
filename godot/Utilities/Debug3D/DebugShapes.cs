using Godot;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Utilities.Debug3D;

public partial class DebugShapes : Node3D {

    public static DebugShapes INSTANCE { get; private set; }
    private static readonly Dictionary<string, Node3D> _thingSet = [];


    public DebugShapes() {
        if (INSTANCE != null) throw new Exception("DebugShapes can't be initialized twice");
        INSTANCE = this;
    }

    public void Add(string key, Node3D obj) {
        if (_thingSet.TryGetValue(key, out Node3D? value)) {
            RemoveChild(value);
        }
        _thingSet[key] = obj;
        AddChild(obj);
    }

    public void AddLineDebug3D(string key, Vector3 start, Vector3 end, Color colour) {
        LineDebug3D lineDebug;
        if (_thingSet.TryGetValue(key, out Node3D? value)) {
            lineDebug = value as LineDebug3D;
        } else {
            lineDebug = new LineDebug3D();
            _thingSet[key] = lineDebug;
            AddChild(_thingSet[key]);
        }
        lineDebug.Start = start;
        lineDebug.End = end;
        lineDebug.Colour = colour;
    }

    public void AddCircleXYDebug3D(string key, Vector3 center, float radius, int vertexCount, Color colour) {
        CircleXYDebug3D circleDebug;
        if (_thingSet.TryGetValue(key, out Node3D? value)) {
            circleDebug = value as CircleXYDebug3D;
        } else {
            circleDebug = new CircleXYDebug3D();
            _thingSet[key] = circleDebug;
            AddChild(_thingSet[key]);
        }
        circleDebug.Center = center;
        circleDebug.Radius = radius;
        circleDebug.VertexCount = vertexCount;
        circleDebug.Colour = colour;
    }

    public void Remove(string key) {
        if (_thingSet.TryGetValue(key, out Node3D? value)) {
            RemoveChild(value);
        }
    }
}
