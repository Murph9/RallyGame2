using Godot;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class UpgradeScreen : CenterContainer, IScene {

	[Signal]
    public delegate void ClosedEventHandler();

	private readonly RandomNumberGenerator _rand = new ();

	public override void _Ready() {

		var main = new VBoxContainer();
		AddChild(main);

		main.AddChild(new Label() {
			Text = "Choose an upgrade"
		});

		main.AddChild(new Label() {
			Text = "Just kidding you get a random one"
		});

		var b = new Button() {
			Text = "Random"
		};
		b.Pressed += () => {
			var state = GetNode<GlobalState>("/root/GlobalState");
			var allParts = state.CarDetails.GetAllPartsInTree().Where(x => x.CurrentLevel < x.Levels.Length - 1).ToArray();
			var part = allParts[GD.Randi() % allParts.Length];
			part.CurrentLevel++;
			Console.WriteLine(part.Name + " is now level " + part.CurrentLevel);
			state.PartsUpgraded.Add(part);
			state.CarDetails.LoadSelf(Main.DEFAULT_GRAVITY);

			EmitSignal(SignalName.Closed);
		};
		main.AddChild(b);
	}

	public override void _Process(double delta) { }
}
