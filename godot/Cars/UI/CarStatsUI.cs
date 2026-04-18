// New file
using Godot;
using murph9.RallyGame2.godot.PayDay;

namespace murph9.RallyGame2.godot.Cars.UI;

  public partial class CarStatsUI : Control {

    private PayDayGlobalState _state;
    private double accel;
    private double topSpeed;
    private double handling;
    private double braking;

    public override void _Notification(int what) {
        if (what == NotificationLayoutChanged)
            _carStatsVBox?.CustomMinimumSize = new Vector2(0, 0);
    }

    private VBoxContainer _carStatsVBox;

    public override void _Ready() {
        _state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        Refresh();
    }

    private void ComputeStats() {
        if (_state.CarDetails == null) {
            accel = 0;
            topSpeed = 0;
            handling = 0;
            braking = 0;
            return;
        }

        float maxTorque = (float)_state.CarDetails.Engine.MaxTorque().Item1;
        float maxKw = (float)_state.CarDetails.Engine.MaxKw().Item1;
        float drag = Mathf.Abs(_state.CarDetails.QuadraticDrag(new Vector3(27f, 0f, 0f)).X);

        var mass = _state.CarDetails.TotalMass;
        accel = mass > 0 ? ClampToRange(SkewLog(maxTorque / mass, -1f, 0.75f), 0, 1) : 0;
        topSpeed = drag > 0 ? ClampToRange(SkewLog(maxKw / drag, -2f, 10f), 0, 1) : 0;

        var longGrip = _state.CarDetails.TractionDetails.LongGripMax;
        var handlingRaw = (float)(_state.CarDetails.TractionDetails.LatGripMax / longGrip);
        handling = ClampToRange(SkewLog(handlingRaw, -1f, 1f), 0, 1);

        var brakingRaw = _state.CarDetails.BrakeMaxTorque * longGrip / mass;
        braking = mass > 0 ? ClampToRange(SkewLog(brakingRaw, 0f, 1f), 0, 1) : 0;
    }

    private static double ClampToRange(double value, double min, double max) {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return min;
        return Mathf.Clamp(value, min, max);
    }

    private static double SkewLog(double value, double preMin, double preMax) => SkewLog(value, preMin, preMax, 0, 1);
    private static double SkewLog(double value, double preMin, double preMax, double min, double max) {
        var mx = Mathf.Log(value - preMin) / Mathf.Log(preMax - preMin);
        return mx * (max - min) + min;
    }

    public void Refresh() {
        ComputeStats();
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
        AddFlagRow(vbox, "Nitro", _state.CarDetails.NitroEnabled);
        AddFlagRow(vbox, "Turbo", _state.CarDetails.Engine.TurboAirMult > 1);
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
