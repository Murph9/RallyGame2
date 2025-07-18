using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.DebugGUI;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars;

public partial class CarUI : Control {

    public Car Car { get; set; }

    [DebugGUIText(g: 0)]
    [DebugGUIGraph(g: 0, group: 2, min: -10, max: 150)]
    private float SusForce0 => Car.Wheels[0].SusForce.Length();
    [DebugGUIGraph(g: 0.5f, group: 2, min: -10, max: 150)]
    private float SusForce1 => Car.Wheels[1].SusForce.Length();
    [DebugGUIGraph(r: 0f, group: 2, min: -10, max: 150)]
    private float SusForce2 => Car.Wheels[2].SusForce.Length();
    [DebugGUIGraph(b: 0f, group: 2, min: -10, max: 150)]
    private float SusForce3 => Car.Wheels[3].SusForce.Length();

    private TorqueCurveGraph _torqueCurveGraph;

    private bool _debugOn;

    public override void _Ready() {
        for (int i = 0; i < Car.Wheels.Length; i++) {
            var node = GetNode<WheelUI>("VBoxContainer/HBoxContainer/WheelGridContainer/WheelUi" + i);
            node.Wheel = Car.Wheels[i];
            node.Debug = false;
        }

        GetTree().Root.SizeChanged += WindowSizeChanged;
        WindowSizeChanged();

        _torqueCurveGraph = new TorqueCurveGraph(Car.Details) {
            Name = "TorqueCurveGraph",
            Visible = false
        };
        AddChild(_torqueCurveGraph);
    }

    private void WindowSizeChanged() {
        const float REFERENCE_SIZE = (360f - 20f) / 1080f;
        var speedoRect = GetNode<ReferenceRect>("VBoxContainer/HBoxContainer/SpeedoReferenceRect");
        speedoRect.CustomMinimumSize = new Vector2(1, 1) * REFERENCE_SIZE * GetViewportRect().End.Y;
    }

    public override void _Draw() {
        const int ARC_WIDTH = 2;
        var defaultFont = ThemeDB.FallbackFont;
        int defaultFontSize = ThemeDB.FallbackFontSize;

        // update damage and fuel bars
        var damageBar = GetNode<ProgressBar>("VBoxContainer/DamageProgressBar");
        damageBar.Value = Car.Damage;

        var fuelBar = GetNode<ProgressBar>("VBoxContainer/FuelProgressBar");
        fuelBar.Value = Car.Engine.CurrentFuel / Car.Details.FuelMax;

        // update the speedo
        var speedoRect = GetNode<ReferenceRect>("VBoxContainer/HBoxContainer/SpeedoReferenceRect");
        DrawSetTransform(speedoRect.GlobalPosition);
        var middle = speedoRect.Size / 2;

        // rpm arc
        var showRPMTo = Mathf.Ceil((Car.Details.Engine.MaxRpm + 1000) / 1000) * 1000; // include the next 1000 and end on whole number
        var arcSize = speedoRect.Size.X / 2 - ARC_WIDTH / 2;
        var rotation = (1 + Car.Engine.CurRPM / 10000f) * Math.PI;
        DrawArc(middle, arcSize, (float)Math.PI, (float)rotation, 32, Colors.White, ARC_WIDTH, true);
        // rpm max rpm arc
        DrawArc(middle, arcSize, (float)((1 + Car.Details.Engine.MaxRpm / 10000f) * Math.PI), (float)((1 + showRPMTo / 10000f) * Math.PI), 32, Colors.Red, ARC_WIDTH, true);

        // rpm line(s)
        var rotationQua = new Vector2(1, 0).Rotated((float)rotation);
        DrawLine(middle, rotationQua * (arcSize * 0.9f) + middle, Colors.Red, 5);
        DrawLine(middle + rotationQua * (arcSize * 0.99f), middle + rotationQua * (arcSize * 1.01f), Colors.Red, 5);

        // rpm numbers
        var numOffset = new Vector2(-defaultFontSize / 3, defaultFontSize / 2f);
        for (int i = 0; i <= showRPMTo / 1000; i++) {
            rotation = (1 + i / 10f) * Math.PI;
            rotationQua = new Vector2(1, 0).Rotated((float)rotation);

            DrawString(defaultFont, middle + rotationQua * (arcSize - defaultFontSize * 2) + numOffset, i.ToString(), HorizontalAlignment.Left);
            DrawLine(middle + rotationQua * (arcSize - defaultFontSize), middle + rotationQua * arcSize, Colors.White, 2);
        }

        // show gear
        DrawString(defaultFont, new Vector2(30, speedoRect.Size.Y - 20), Car.Engine.CurGear.ToString(), width: -1, fontSize: defaultFontSize * 4);

        // show speed
        var speed = MyMath.MsToKmh(Car.RigidBody.LinearVelocity.Length());
        var speedStr = float.Round(speed, 0).ToString();
        var width = speedoRect.Size.X * 0.75f;
        DrawString(defaultFont, new Vector2(0, speedoRect.Size.Y - defaultFontSize - 10), speedStr, HorizontalAlignment.Right, width, fontSize: defaultFontSize * 3);

        // render control bars
        var controlBarX = middle.X + 10f;
        const int CONTROL_BAR_WIDTH = 50;
        const int CONTROL_BAR_HEIGHT = 10;
        DrawRect(new Rect2(controlBarX, middle.Y, CONTROL_BAR_WIDTH, CONTROL_BAR_HEIGHT), Colors.Blue * 0.2f);
        DrawRect(new Rect2(controlBarX, middle.Y, CONTROL_BAR_WIDTH * Car.Inputs.AccelCur, CONTROL_BAR_HEIGHT), Colors.Blue * 0.7f);

        DrawRect(new Rect2(controlBarX, middle.Y + CONTROL_BAR_HEIGHT, CONTROL_BAR_WIDTH, CONTROL_BAR_HEIGHT), Colors.Red * 0.2f);
        DrawRect(new Rect2(controlBarX, middle.Y + CONTROL_BAR_HEIGHT, CONTROL_BAR_WIDTH * Car.Inputs.BrakingCur, CONTROL_BAR_HEIGHT), Colors.Red * 0.7f);

        DrawRect(new Rect2(controlBarX, middle.Y + CONTROL_BAR_HEIGHT * 2, CONTROL_BAR_WIDTH, CONTROL_BAR_HEIGHT), Colors.White * 0.2f);
        DrawRect(new Rect2(controlBarX + CONTROL_BAR_WIDTH / 2 - CONTROL_BAR_HEIGHT / 2 + (CONTROL_BAR_WIDTH - CONTROL_BAR_HEIGHT) * -Car.Inputs.Steering,
            middle.Y + CONTROL_BAR_HEIGHT * 2, CONTROL_BAR_HEIGHT, CONTROL_BAR_HEIGHT), Colors.White * 0.7f);

        // debug numbers
        if (_debugOn) {
            // rpm
            var rpmStr = Car.Engine.CurRPM.ToString() + " rpm";
            DrawString(defaultFont, new Vector2(2, 2 + defaultFontSize), rpmStr, width: -1, fontSize: defaultFontSize);

            // engine stuff
            var engineTorqueStr = (int)Car.Engine.CurrentTorque + " Nm";
            DrawString(defaultFont, new Vector2(2, 2 + defaultFontSize * 2), engineTorqueStr, width: -1, fontSize: defaultFontSize);
            var enginekWStr = (int)EngineDetails.TorqueToKw(Car.Engine.CurrentTorque, Car.Engine.CurRPM) + " kW";
            DrawString(defaultFont, new Vector2(2, 2 + defaultFontSize * 3), enginekWStr, width: -1, fontSize: defaultFontSize);

            // position and physics stuff
            var pos = Car.RigidBody.GlobalPosition;
            var rot = Car.RigidBody.GlobalRotation;

            var vel = Car.RigidBody.LinearVelocity;
            var ang = Car.RigidBody.AngularVelocity;
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 1), "pos: " + pos.ToRoundedString(), width: -1, fontSize: defaultFontSize);
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 2), "rot: " + rot.ToRoundedString(), width: -1, fontSize: defaultFontSize);
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 3), "vel: " + vel.ToRoundedString(), width: -1, fontSize: defaultFontSize);
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 4), "aug: " + ang.ToRoundedString(), width: -1, fontSize: defaultFontSize);

            // fuel
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 6), $"fuel: {float.Round(Car.Engine.CurrentFuel, 2)} / {Car.Details.FuelMax} L", width: -1, fontSize: defaultFontSize);
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 7), $"fuel rate ({Car.Details.Engine.FuelByRpmRate:G2}): {Car.Engine.CurrentFuelRate:G3} L/s", width: -1, fontSize: defaultFontSize);

            // tyre wear
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 9), $"Tyre Wear Rate: {string.Join(",", Car.Wheels.Select(x => x.Details.TyreWearRate))}", width: -1, fontSize: defaultFontSize);
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 10), $"Tyre Wear: {string.Join(",", Car.Wheels.Select(x => x.TyreWear))}", width: -1, fontSize: defaultFontSize);

            // damage
            DrawString(defaultFont, new Vector2(2, -2 - defaultFontSize * 11), $"Damage %: {Car.Damage}", width: -1, fontSize: defaultFontSize);
        }
    }

    public override void _Process(double delta) {
        if (Car == null) return;

        QueueRedraw();

        var wheels = GetNode<GridContainer>("VBoxContainer/HBoxContainer/WheelGridContainer");

        // force the torque outputs to the bottom left
        var torqueGraph = GetNode<TorqueCurveGraph>("TorqueCurveGraph");
        torqueGraph.Position = new Vector2(0, GetViewportRect().End.Y - torqueGraph.Size.Y);

        if (Input.IsActionJustPressed("toggle_wheel_telemetry")) {
            _torqueCurveGraph.Visible = !_torqueCurveGraph.Visible;
            _debugOn = _torqueCurveGraph.Visible;
            foreach (var wheelUi in wheels.GetAllChildrenOfType<WheelUI>()) {
                wheelUi.Debug = _debugOn;
            }
        }
    }

    private static string V3toStr(Vector3 v) {
        return $"({float.Round(v.X, 2)}, {float.Round(v.Y, 2)}, {float.Round(v.Z, 2)})";
    }
}
