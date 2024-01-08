using Godot;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities.DebugGUI;
using System;

namespace murph9.RallyGame2.godot.Cars;

public partial class CarUI : Control {

    public Car Car { get; set; }

    [DebugGUIText(g:0)]
    [DebugGUIGraph(g:0)]
    private float RadSec0 => Car.Wheels[0].RadSec;
    [DebugGUIText(g:0.5f)]
    [DebugGUIGraph(g:0.5f)]
    private float RadSec1 => Car.Wheels[1].RadSec;

    public override void _Ready() {
        for (int i = 0; i < Car.Wheels.Length; i++) {
            var node = GetNode<WheelUI>("WheelGridContainer/WheelUi" + i);
            node.Wheel = Car.Wheels[i];
        }

        GetTree().Root.SizeChanged += WindowSizeChanged;
        WindowSizeChanged();

        var graph = new Graph(new Vector2(200, 200)) {
            Name = "TorqueGraph"
        };
        AddChild(graph);
        var dataset = new Graph.Dataset("Torque", 200, autoScale: true) {
            Color = Colors.Green
        };
        for (int i = 0; i < 200; i++) {
            dataset.Push((float)Car.Details.Engine.CalcTorqueFor(i*50));
        }
        graph.AddDataset(dataset);

        dataset = new Graph.Dataset("kW", 200, autoScale: true) {
            Color = Colors.Aqua
        };
        for (int i = 0; i < 200; i++) {
            dataset.Push((float)Car.Details.Engine.CalcKwFor(i*50));
        }
        graph.AddDataset(dataset);
    }

    private void WindowSizeChanged() {
        const float REFERENCE_SIZE = (360f - 20f)/1080f;
        var speedoRect = GetNode<ReferenceRect>("SpeedoReferenceRect");
        speedoRect.CustomMinimumSize = new Vector2(1,1) * REFERENCE_SIZE * GetViewportRect().End.Y;
    }

    public override void _Draw() {
        const int ARC_WIDTH = 2;
        var defaultFont = ThemeDB.FallbackFont;
        int defaultFontSize = ThemeDB.FallbackFontSize;

        var speedoRect = GetNode<ReferenceRect>("SpeedoReferenceRect");
        DrawSetTransform(speedoRect.GlobalPosition);
        var middle = speedoRect.Size / 2;

        // rpm arc
        var arcSize = speedoRect.Size.X / 2 - ARC_WIDTH / 2;
        var rotation = (1 + Car.Engine.CurRPM/10000f)*Math.PI;
        DrawArc(middle, arcSize, (float)Math.PI, (float)rotation, 32, Colors.White, ARC_WIDTH, true);
        // rpm max rpm arc
        DrawArc(middle, arcSize, (float)((1 + Car.Details.Engine.MaxRpm/10000f)*Math.PI), (float)Math.PI*2, 32, Colors.Red, ARC_WIDTH, true);

        // rpm line(s)
        var rotationQua = new Vector2(1, 0).Rotated((float)rotation);
        DrawLine(middle, rotationQua * (arcSize * 0.9f) + middle, Colors.Red, 5);
        DrawLine(middle + rotationQua * (arcSize * 0.99f), middle + rotationQua * (arcSize * 1.01f), Colors.Red, 5);

        // rpm numbers
        var numOffset = new Vector2(-defaultFontSize / 3, defaultFontSize / 2f);
        for (int i = 0; i <= 10; i++) {
            rotation = (1 + i/10f) * Math.PI;
            rotationQua = new Vector2(1, 0).Rotated((float)rotation);

            DrawString(defaultFont, middle + rotationQua * (arcSize - defaultFontSize*2) + numOffset, i.ToString(), HorizontalAlignment.Left);
            DrawLine(middle + rotationQua * (arcSize - defaultFontSize), middle + rotationQua * arcSize, Colors.White, 2);
        }

        // rpm debug number
        var rpmStr = Car.Engine.CurRPM.ToString();
        DrawString(defaultFont, new Vector2(2, 2 + defaultFontSize), rpmStr, width: -1, fontSize: defaultFontSize);

        // show gear
        DrawString(defaultFont, new Vector2(30, speedoRect.Size.Y - 20), Car.Engine.CurGear.ToString(), width: -1, fontSize: defaultFontSize * 4);

        // show speed
        var speed = Car.RigidBody.LinearVelocity.Length() * 3.6f; // m/s -> km/h
        var speedStr = float.Round(speed, 0).ToString();
        var width = speedoRect.Size.X * 0.75f;
        DrawString(defaultFont, new Vector2(0, speedoRect.Size.Y - defaultFontSize - 10), speedStr, HorizontalAlignment.Right, width, fontSize: defaultFontSize * 3);

        // render control bars
        var controlBarX = middle.X + 10f;
        const int CONTROL_BAR_WIDTH = 50;
        const int CONTROL_BAR_HEIGHT = 10;
        DrawRect(new Rect2(controlBarX, middle.Y, CONTROL_BAR_WIDTH, CONTROL_BAR_HEIGHT), Colors.Blue * 0.2f);
        DrawRect(new Rect2(controlBarX, middle.Y, CONTROL_BAR_WIDTH * Car.AccelCur, CONTROL_BAR_HEIGHT), Colors.Blue * 0.7f);

        DrawRect(new Rect2(controlBarX, middle.Y + CONTROL_BAR_HEIGHT, CONTROL_BAR_WIDTH, CONTROL_BAR_HEIGHT), Colors.Red * 0.2f);
        DrawRect(new Rect2(controlBarX, middle.Y + CONTROL_BAR_HEIGHT, CONTROL_BAR_WIDTH * Car.BrakingCur, CONTROL_BAR_HEIGHT), Colors.Red * 0.7f);

        DrawRect(new Rect2(controlBarX, middle.Y + CONTROL_BAR_HEIGHT*2, CONTROL_BAR_WIDTH, CONTROL_BAR_HEIGHT), Colors.White * 0.2f);
        DrawRect(new Rect2(controlBarX + CONTROL_BAR_WIDTH/2 - CONTROL_BAR_HEIGHT/2 + (CONTROL_BAR_WIDTH - CONTROL_BAR_HEIGHT) * -Car.Steering,
            middle.Y + CONTROL_BAR_HEIGHT * 2, CONTROL_BAR_HEIGHT, CONTROL_BAR_HEIGHT), Colors.White * 0.7f);
    }

    public override void _Process(double delta) {
        if (Car == null) return;

        QueueRedraw();

        // force the wheel outputs to the top left
        var wheels = GetNode<GridContainer>("WheelGridContainer");
        wheels.Position = new Vector2(GetViewportRect().End.X - wheels.Size.X, 0);

        // force the speedo to the bottom right
        var speedoRect = GetNode<ReferenceRect>("SpeedoReferenceRect");
        speedoRect.Position = GetViewportRect().End - speedoRect.Size;

        // force the wheel outputs to the bottom left
        var torqueGraph = GetNode<Graph>("TorqueGraph");
        torqueGraph.Position = new Vector2(0, GetViewportRect().End.Y - torqueGraph.Size.Y);
    }

    private static string V3toStr(Vector3 v) {
        return $"({float.Round(v.X, 2)}, {float.Round(v.Y, 2)}, {float.Round(v.Z, 2)})";
    }
}
