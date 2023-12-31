using Godot;
using murph9.RallyGame2.godot.Cars;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.World;
using System;

namespace murph9.RallyGame2.godot;

public partial class Racing : Node3D
{
	public override void _Ready()
	{
		var worldPieces = new SimpleWorldPieces();
        AddChild(worldPieces);

        var details = CarType.Runner.LoadCarDetails(Main.DEFAULT_GRAVITY);
        AddChild(new Car(details, worldPieces.GetSpawn()));
	}

	public override void _Process(double delta)
	{
	}

	public void _on_back_button_pressed() {
		GetTree().ChangeSceneToFile("res://main.tscn");
	}
}
