using Godot;
using murph9.RallyGame2.godot.Cars;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot;

public partial class Main : Node
{
    private static readonly float default_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private int sphereCount = 0;

    public override void _Ready() {
        var details = CarType.Runner.LoadCarDetails(new Vector3(0, -default_gravity, 0));
        AddChild(new Car(details));
    }

    public override void _Process(double delta) {
        var label = GetNode<Label>("VBoxContainer/Label");
        label.Text = $"Count: {sphereCount}";
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
}
