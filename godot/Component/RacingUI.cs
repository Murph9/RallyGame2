using Godot;
using murph9.RallyGame2.godot.scenes;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public partial class RacingUI : Control
{
	public RacingScreen Racing { get; set; }

	public override void _Ready() {}

	public override void _Process(double delta) {
		var state = GetNode<GlobalState>("/root/GlobalState");
		var secToWin = state.SecondsToWin();

		GetNode<Label>("GridContainer/LapLabel").Text = $"Lap: {Racing.CurrentLap} ({Racing.CurrentCheckpoint})";
		GetNode<Label>("GridContainer/TimeLabel").Text = double.Round(Racing.LapTimer, 3) + "\n"
				+ string.Join('\n', Racing.LapTimes.Select(x => double.Round(x, 1)));
		GetNode<Label>("GridContainer/TargetLabel").Text = "Target: " + Math.Round(secToWin, 2) + " sec";
		GetNode<Label>("GridContainer/RemainingLabel").Text = "Remaining: " + Math.Round(Math.Max(0, secToWin - Racing.LapTimer), 2);

		var cam = GetViewport().GetCamera3D();
		var positions = Racing.GetCarAndCheckpointPos();
		GetNode<Line2D>("CheckpointLine2D").Points = [
			cam.UnprojectPosition(positions.Item1 + new Vector3(0, 0.3f, 0)),
			cam.UnprojectPosition(positions.Item2 + new Vector3(0, 0.3f, 0))
		];

		// center top middle
		var uiBox = GetNode<GridContainer>("GridContainer");
		uiBox.Position = new Vector2(GetViewportRect().End.X/2 - uiBox.Size.X/2, 0);
	}

	public void _on_back_button_pressed() {
		Racing.Exit();
	}
}
