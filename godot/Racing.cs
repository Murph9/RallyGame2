using Godot;
using murph9.RallyGame2.godot.Cars;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Debug;
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

		int i = 0;
		foreach (var checkpoint in worldPieces.GetCheckpoints()) {
			AddChild(DebugHelper.GenerateWorldText(i.ToString(), checkpoint));
			GD.Print(checkpoint);
			i++;
		}
	}

	public override void _Process(double delta)
	{
	}

	public void _on_back_button_pressed() {
		GetTree().ChangeSceneToFile("res://main.tscn");
	}
}
