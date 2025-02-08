using Godot;
using murph9.RallyGame2.godot.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace murph9.RallyGame2.godot.Utilities;

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
}