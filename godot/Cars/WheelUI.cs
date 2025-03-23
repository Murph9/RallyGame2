using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.Cars;

public partial class WheelUI : VBoxContainer {
    public Wheel Wheel { get; set; }

    public bool _debug;
    public bool Debug {
        get { return _debug; }
        set {
            _debug = value;
            var control = GetNode<VBoxContainer>("DebugVBoxContainer");
            if (control != null)
                control.Visible = value;
        }
    }

    public override void _Ready() { }

    public override void _Process(double delta) {
        QueueRedraw();

        GetNode<Label>("WheelLabel").Text = GetTypeNameFromId(Wheel.Details.Id);
        GetNode<ProgressBar>("HBoxContainer/SuspensionTravel").Value = Wheel.SusTravelDistance / Wheel.Car.Details.SusByWheelNum(Wheel.Details.Id).TravelTotal();

        var gridContainer = GetNode<GridContainer>("DebugVBoxContainer/GridContainer");
        foreach (var f in gridContainer.GetChildren()) {
            if (f is HBoxContainer)
                continue;
            gridContainer.RemoveChild(f);
            f.QueueFree();
        }
        // TODO perf
        gridContainer.AddChild(new Label() {
            Text = "Sus Dist."
        });
        gridContainer.AddChild(new Label() {
            Text = float.Round(Wheel.SusTravelDistance, 2).ToString()
        });
        gridContainer.AddChild(new Label() {
            Text = "Tyre Life"
        });
        gridContainer.AddChild(new Label() {
            Text = $"{float.Round(Wheel.TyreWear, 2) * 100}%"
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
        var control = GetNode<Control>("HBoxContainer/Control");
        var controlCenter = control.Position + control.Size / 2;

        // draw suspension square
        var rectSize = new Vector2(1, 1) * (float)Mathf.Clamp(Mathf.Sqrt(Wheel.SkidFraction), 0, 2) * 50f;
        DrawTextureRect(new GradientTexture1D() {
            Gradient = new Gradient() {
                Colors = [ColourHelper.TyreWearOnGreenToRed(Wheel.TyreWear)]
            }
        }, new Rect2(controlCenter - rectSize / 2, rectSize), false);

        var dir = new Vector2(Wheel.GripDir.X, Wheel.GripDir.Z).Normalized();
        DrawLine(controlCenter, controlCenter + dir * control.Size / 2, ColourHelper.SkidOnRGBScale((float)Wheel.SkidFraction), width: 4);

        var defaultFont = ThemeDB.FallbackFont;
        int defaultFontSize = ThemeDB.FallbackFontSize;
        var tyreWearPercentage = $"{float.Round(Wheel.TyreWear, 2) * 100}%";
        var textSize = defaultFont.GetStringSize(tyreWearPercentage);
        DrawString(defaultFont, controlCenter + new Vector2(-textSize.X / 2f, textSize.Y / 2f), tyreWearPercentage, fontSize: defaultFontSize);
    }

    public static string GetTypeNameFromId(int id) => id switch {
        0 => "Front Left",
        1 => "Front Right",
        2 => "Rear Left",
        3 => "Rear Right",
        _ => "",
    };
}
