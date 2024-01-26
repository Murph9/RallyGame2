using Godot;
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Cars;

public partial class WheelUI : VBoxContainer
{
	public Wheel Wheel { get; set; }

	public override void _Ready() { }

	public override void _Process(double delta)
	{
		QueueRedraw();

		GetNode<Label>("Label").Text = Wheel.Details.Id + " " + float.Round(Wheel.RadSec, 2);
		GetNode<ProgressBar>("ProgressBar").Value = Wheel.SusTravelDistance / Wheel.Car.Details.SusByWheelNum(Wheel.Details.Id).TravelTotal();

		var gridContainer = GetNode<GridContainer>("GridContainer");
		foreach (var f in gridContainer.GetChildren()) {
			if (f is HBoxContainer)
				continue;
			gridContainer.RemoveChild(f);
			f.QueueFree();
		}

		gridContainer.AddChild(new Label() {
			Text = "Sus Dist."
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(Wheel.SusTravelDistance, 2).ToString()
		});
		gridContainer.AddChild(new Label() {
			Text = "SwayForce"
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(Wheel.SwayForce, 2).ToString()
		});
		gridContainer.AddChild(new Label() {
			Text = "SpringForce"
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(Wheel.SpringForce, 2).ToString()
		});
		gridContainer.AddChild(new Label() {
			Text = "Damping"
		});
		gridContainer.AddChild(new Label() {
			Text = float.Round(Wheel.Damping, 2).ToString()
		});
	}

    public override void _Draw() {
		var control = GetNode<Control>("Control");
		var dir = new Vector2(Wheel.GripDir.X, Wheel.GripDir.Z) / Car.TRACTION_MAX_LAT;
		var start = control.Position + control.Size/2;
		DrawLine(start, start + dir * control.Size/2, SkidOnRGBScale((float)Wheel.SkidFraction), width: 4);
    }

    public static Color SkidOnRGBScale(float skidFraction) {
        // 0 is white, 0.333f is green, 0.666f is red, 1 is blue
        skidFraction = Mathf.Clamp(Mathf.Abs(skidFraction/3f), 0, 1);

        if (skidFraction < 1f/3f)
            return LerpColor(skidFraction*3, Colors.White, Colors.Green);
        else if (skidFraction < 2f/3f)
            return LerpColor((skidFraction - 1f/3f) * 3, Colors.Green, Colors.Red);
        return LerpColor((skidFraction - 2f/3f)*3, Colors.Red, Colors.Blue);
    }

    public static Color LerpColor(float value, Color a, Color b) {
        return new Color(a).Lerp(b, value);
    }
}
