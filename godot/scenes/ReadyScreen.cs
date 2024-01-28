using Godot;
using System;

namespace murph9.RallyGame2.godot.scenes;

public partial class ReadyScreen : Control, IScene {
	[Signal]
    public delegate void ClosedEventHandler();

	public override void _Ready() { }

	public override void _Process(double delta) { }
}
