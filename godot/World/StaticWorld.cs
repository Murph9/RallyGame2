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

    public InfiniteCheckpoint GetInitialSpawn() {
        return new InfiniteCheckpoint(null, Transform3D.Identity, Transform3D.Identity, Vector3.Zero);
    }

    public IEnumerable<InfiniteCheckpoint> GetAllCurrentCheckpoints() {
        return Array.Empty<InfiniteCheckpoint>();
    }
}
