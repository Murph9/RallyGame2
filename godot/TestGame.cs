using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.World;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class TestGame : Node3D {

    private Car _car;
    public override void _Ready() {
        var carDetails = CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY);
        _car = new Car(carDetails, null, new Transform3D(Basis.FromEuler(new Vector3(0, -Mathf.Pi / 2, 0)), new Vector3(0, .4f, 27)));
        AddChild(_car);

        var world = new StaticWorld() { WorldName = StaticWorld.GetList().First(x => x.StartsWith("roc")) };
        AddChild(world);
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("car_reset")) {
            _car.RigidBody.GlobalPosition = new Vector3(0, .4f, 27);
            _car.RigidBody.LinearVelocity = new Vector3();
        }
    }
}
