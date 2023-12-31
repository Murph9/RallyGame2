using Godot;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars;

public partial class WheelUI : VBoxContainer
{
	public Car Car {get; set; }
	public Wheel Wheel {get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var w = Wheel;
		GetNode<Label>("Label").Text = w.Details.id + " " + float.Round(w.RadSec, 2);
		GetNode<ProgressBar>("ProgressBar").Value = w.SusTravelDistance / Car.Details.SusByWheelNum(w.Details.id).TravelTotal();
		GetNode<Line2D>("Line2D").Points = new Vector2[] {
			new (80, 40),
			40* new Vector2(w.GripDir.X, w.GripDir.Z) / Car.Details.mass + new Vector2(80, 40),
		};

		var gridContainer = GetNode<GridContainer>("GridContainer");
		foreach (var f in gridContainer.GetChildren()) {
			gridContainer.RemoveChild(f);
			f.QueueFree();
		}

		gridContainer.AddChild(new Label() {
			Text = "SusTravel"
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(w.SusTravelDistance, 2).ToString()
		});
		gridContainer.AddChild(new Label() {
			Text = "SlipAngle"
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(w.SlipAngle, 2).ToString()
		});
		gridContainer.AddChild(new Label() {
			Text = "SlipRatio"
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(w.SlipRatio, 2).ToString()
		});
		foreach (var e in w.ExtraDetails) {
			gridContainer.AddChild(new Label() {
				Text = e.Key
			});
			gridContainer.AddChild(new Label() {
				Text = float.Round(e.Value, 2).ToString()
			});
		}
	}
}
