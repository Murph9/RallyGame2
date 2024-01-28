using System.Linq;
using Godot;

namespace murph9.RallyGame2.godot.scenes;

public partial class ReadyScreen : CenterContainer, IScene {
	[Signal]
    public delegate void ClosedEventHandler();

	public override void _Ready() {
		// show:
		// - basic car stats
		// - the goal in laps
		// - rewards

		// maybe or later:
		// - the last goal and difference from it

		var main = new VBoxContainer();
		AddChild(main);

		var state = GetNode<GlobalState>("/root/GlobalState");

		main.AddChild(new Label() {
			Text = $"Round {state.RoundResults.Count() + 1} Goal: {state.SecondsToWin()} sec"
		});
		main.AddChild(new Label() {
			Text = "Rewards: $500 + random part lol"
		});

		var b = new Button() {
			Text = "Start"
		};
		b.Pressed += () => EmitSignal(SignalName.Closed);
		main.AddChild(b);
	}

	public override void _Process(double delta) { }
}
