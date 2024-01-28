using Godot;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class ResultsScreen : CenterContainer, IScene {
	[Signal]
    public delegate void ClosedEventHandler();
	[Signal]
    public delegate void QuitEventHandler();

	public override void _Ready() {
		var state = GetNode<GlobalState>("/root/GlobalState");

		var main = new VBoxContainer();
		AddChild(main);

		var lastResult = state.RoundResults.Last();
		if (lastResult.Time > state.SecondsToWin(-1)) {
			main.AddChild(new Label() {
				Text = $"You Failed.\nWith a time of {lastResult.Time} sec you did not meet the goal of {state.SecondsToWin(-1)} sec"
			});
			var bExit = new Button() {
				Text = "Restart"
			};
			bExit.Pressed += () => EmitSignal(SignalName.Closed);
			main.AddChild(bExit);
			return;
		}

		main.AddChild(new Label() {
			Text = "Well Done\nYou beat the target time of " + state.SecondsToWin(-1) + " sec, nice"
		});

		var b = new Button() {
			Text = "Continue"
		};
		b.Pressed += () => EmitSignal(SignalName.Closed);
		main.AddChild(b);
	}

	public override void _Process(double delta) { }
}
