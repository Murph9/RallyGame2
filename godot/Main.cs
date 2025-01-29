using Godot;
using murph9.RallyGame2.godot.World;

namespace murph9.RallyGame2.godot;

public partial class Main : Node {
    public static readonly Vector3 DEFAULT_GRAVITY = new(0, -(float)ProjectSettings.GetSetting("physics/3d/default_gravity"), 0);
    private int sphereCount = 0;

    private StaticWorld _world;

    private string[] _worlds;

    public override void _Ready() {
        _worlds = StaticWorld.GetList();

        _world = new StaticWorld() { };
        AddChild(_world);

        var optionButton = GetNode<OptionButton>("PanelContainer/VBoxContainer/HBoxContainer/WorldOptionButton");
        var popup = optionButton.GetPopup();

        foreach (var w in _worlds) {
            popup.AddItem(w);
        }
        optionButton.ItemSelected += (i) => {
            _world.WorldName = _worlds[i];
        };
    }

    public override void _Process(double delta) {
    }

    public void _on_button_pressed() {
        var r = new RigidBody3D();
        r.AddChild(new CollisionShape3D() {
            Shape = new SphereShape3D()
        });
        r.AddChild(new MeshInstance3D() {
            Mesh = new SphereMesh()
        });
        AddChild(r);
        sphereCount++;
    }

    public void _on_upgrade_button_pressed() {
        GetTree().ChangeSceneToFile("res://UpgradeTestMenu.tscn");
    }

    public void _on_hundredstart_button_pressed() {
        GetTree().ChangeSceneToFile("res://HundredRallyGame.tscn");
    }
    public void _on_circuitstart_button_pressed() {
        GetTree().ChangeSceneToFile("res://CircuitGame.tscn");
    }
}
