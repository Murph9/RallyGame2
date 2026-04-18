using Godot;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot.Cars.UI;

public partial class CarStatsUI : Control {

    private CarDetails _carDetails;
    private double accel;
    private double topSpeed;
    private double handling;
    private double braking;

    public void SetCarDetails(CarDetails carDetails) {
        _carDetails = carDetails;
        Refresh();
    }

    public void Refresh() {
        var stats = CarStatsCalculator.ComputeStats(_carDetails);
        accel = stats.Acceleration;
        topSpeed = stats.TopSpeed;
        handling = stats.Handling;
        braking = stats.Braking;
        BuildUI();
    }

    private void BuildUI() {
        // Remove existing UI elements before building new ones
        foreach (var child in GetChildren())
            child.QueueFree();

        var vbox = new VBoxContainer();
        AddChild(vbox);
        AddStatRow(vbox, "Acceleration", accel);
        AddStatRow(vbox, "Top Speed", topSpeed);
        AddStatRow(vbox, "Handling", handling);
        AddStatRow(vbox, "Braking", braking);
        AddFlagRow(vbox, "Nitro", _carDetails.NitroEnabled);
        AddFlagRow(vbox, "Turbo", _carDetails.Engine.TurboAirMult > 1);
    }

    private static void AddStatRow(VBoxContainer parent, string labelText, double value) {
        var hbox = new HBoxContainer();
        var label = new Label { Text = labelText };
        var bar = new ProgressBar {
            MinValue = 0,
            MaxValue = 100,
            Value = Mathf.Clamp(value * 100, 0, 100),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(200, 0)
        };
        hbox.AddChild(label);
        hbox.AddChild(bar);
        parent.AddChild(hbox);
    }

    private static void AddFlagRow(VBoxContainer parent, string labelText, bool flag) {
        var hbox = new HBoxContainer();
        var label = new Label { Text = labelText };
        var flagLabel = new Label { Text = flag ? "Yes" : "No" };
        hbox.AddChild(label);
        hbox.AddChild(flagLabel);
        parent.AddChild(hbox);
    }
}
