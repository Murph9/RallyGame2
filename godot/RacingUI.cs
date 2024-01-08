using Godot;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class RacingUI : Control
{
	public Racing Racing { get; set; }

	public override void _Ready() {}

	public override void _Process(double delta) {
		GetNode<Label>("VBoxContainer/GridContainer/LapLabel").Text = $"Lap: {Racing.CurrentLap} ({Racing.CurrentCheckpoint})";
		GetNode<Label>("VBoxContainer/GridContainer/TimeLabel").Text = double.Round(Racing.LapTimer, 3) + "\n"
				+ string.Join('\n', Racing.LapTimes.Select(x => double.Round(x, 1)));

		var cam = GetViewport().GetCamera3D();
		var positions = Racing.GetCarAndCheckpointPos();
		GetNode<Line2D>("CheckpointLine2D").Points = new Vector2[] {
			cam.UnprojectPosition(positions.Item1 + new Vector3(0, 0.3f, 0)),
			cam.UnprojectPosition(positions.Item2 + new Vector3(0, 0.3f, 0))
		};

		// center top middle
		var uiBox = GetNode<GridContainer>("VBoxContainer/GridContainer");
		uiBox.Position = new Vector2(GetViewportRect().End.X/2 - uiBox.Size.X/2, 0);
	}

	public void _on_back_button_pressed() {
		Racing.Exit();
	}
}
