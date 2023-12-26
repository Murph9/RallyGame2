using Godot;
using murph9.RallyGame2.Car.Init;

namespace murph9.RallyGame2.godot;

public partial class Main : Node
{
    public override void _Ready() {
        var grav = new Vector3(0, -9.81f, 0);

        var details = CarType.Runner.LoadCarDetails(grav);
        AddChild(new Car(details));
    }

    public override void _Process(double delta) {
        
    }

    public void _on_button_pressed() {
        var details = CarType.Runner.LoadCarDetails(new Vector3(0, -9.81f, 0));
        AddChild(new Car(details));
    }
}
