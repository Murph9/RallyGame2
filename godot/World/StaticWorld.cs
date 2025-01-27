using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public partial class StaticWorld : Node3D, IWorld {

    public static string[] GetList() {
        var dir = DirAccess.Open("res://assets/staticWorld");
        return dir.GetFiles().Where(x => x.EndsWith(".blend")).ToArray();
    }

    private bool _changed = false;
    private string _worldName;
    public string WorldName {
        get {
            return _worldName;
        }
        set {
            _worldName = value;
            _changed = true;
        }
    }
    private Node3D _scene;

    public override void _Ready() {
        Load();
    }

    private void Load() {
        if (_scene != null) {
            RemoveChild(_scene);
            _scene.QueueFree();
        }
        if (_worldName == null)
            return;

        if (!_worldName.EndsWith(".blend")) {
            _worldName += ".blend";
        }
        var packedScene = GD.Load<PackedScene>("res://assets/staticWorld/" + _worldName);
        _scene = packedScene.Instantiate<Node3D>();
        AddChild(_scene);
    }

    public override void _Process(double delta) {
        if (_changed) {
            _changed = false;
            Load();
        }
    }

    public Transform3D GetSpawn() {
        return Transform3D.Identity;
    }

    public IEnumerable<Transform3D> GetCheckpoints() {
        return Array.Empty<Transform3D>();
    }

    public IEnumerable<Curve3DPoint> GetCurve3DPoints() {
        return Array.Empty<Curve3DPoint>();
    }
}
