using Godot;
using murph9.RallyGame2.Car.Init;

namespace murph9.RallyGame2.godot;

public partial class Main : Node
{
    private static readonly float default_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    public override void _Ready() {
        var details = CarType.Runner.LoadCarDetails(new Vector3(0, -default_gravity, 0));
        AddChild(new Car.Car(details));
    }

    public override void _Process(double delta) {
        
    }

    public void _on_button_pressed() {
        var details = CarType.Runner.LoadCarDetails(new Vector3(0, -default_gravity, 0));
        AddChild(new Car.Car(details));
    }
}
