using Godot;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class ResultsScreen : CenterContainer, IScene {
	[Signal]
    public delegate void ClosedEventHandler();
	[Signal]
    public delegate void RestartEventHandler();

	public override void _Ready() {
		var state = GetNode<GlobalState>("/root/GlobalState");

		var root = GetNode<PanelContainer>("PanelContainer");

		var main = new VBoxContainer();
		root.AddChild(main);

		var lastResult = state.RoundResults.Last();
		if (lastResult.Time > state.SecondsToWin(-1)) {
			main.AddChild(new Label() {
				Text = $"You Failed.\nWith a time of {Math.Round(lastResult.Time, 2)} sec. You did not meet the goal of {Math.Round(state.SecondsToWin(-1), 2)} sec"
			});
			var bExit = new Button() {
				Text = "Restart"
			};
			bExit.Pressed += () => EmitSignal(SignalName.Restart);
			main.AddChild(bExit);
			return;
		}

		main.AddChild(new Label() {
			Text = $"Well Done, your time was {Math.Round(state.RoundResults.Last().Time, 2)}\nYou beat the target time of " + Math.Round(state.SecondsToWin(-1), 2) + " sec, nice"
		});

		var b = new Button() {
			Text = "Continue"
		};
		b.Pressed += () => EmitSignal(SignalName.Closed);
		main.AddChild(b);
	}

	public override void _Process(double delta) { }
}
