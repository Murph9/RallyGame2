using Godot;

namespace murph9.RallyGame2.godot.Cars;

public partial class WheelUI : VBoxContainer
{
	public Wheel Wheel { get; set; }

	public override void _Ready() { }

	public override void _Process(double delta)
	{
		QueueRedraw();

		GetNode<Label>("Label").Text = Wheel.Details.id + " " + float.Round(Wheel.RadSec, 2);
		GetNode<ProgressBar>("ProgressBar").Value = Wheel.SusTravelDistance / Wheel.Car.Details.SusByWheelNum(Wheel.Details.id).TravelTotal();

		var gridContainer = GetNode<GridContainer>("GridContainer");
		foreach (var f in gridContainer.GetChildren()) {
			if (f is HBoxContainer)
				continue;
			gridContainer.RemoveChild(f);
			f.QueueFree();
		}

		gridContainer.AddChild(new Label() {
			Text = "Spring"
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(Wheel.SusTravelDistance, 2).ToString()
		});
		foreach (var e in Wheel.ExtraDetails) {
			gridContainer.AddChild(new Label() {
				Text = e.Key
			});
			gridContainer.AddChild(new Label() {
				Text = float.Round(e.Value, 2).ToString()
			});
		}
	}

    public override void _Draw() {
		var control = GetNode<Control>("Control");
		var dir = new Vector2(Wheel.GripDir.X, Wheel.GripDir.Z) / Wheel.Car.Details.mass;
		var start = control.Position + control.Size/2;
		DrawLine(start, start + dir * 50, Colors.Red, width: 4);
    }
}
