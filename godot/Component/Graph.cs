using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public partial class Graph : HBoxContainer {

	public class Dataset {
        public string GraphName { get; }
        public readonly float[] Values;
        public readonly Vector2[] GraphPoints;

        public float Min { get; private set; }
        public float Max { get; private set; }
        internal int CurrentIndex  { get; private set; }

        public Color Color { get; set; } = Colors.White;
        public bool AutoScale { get; set; } = true;


        public Dataset(string graphName, int count, float min = 0, float max = 100, bool autoScale = false) {
            GraphName = graphName;
            Min = min;
            Max = max;
            Values = new float[count];
            AutoScale = autoScale;
            GraphPoints = new Vector2[count]; // enough to fill the screen
        }

        public void Push(float value) {
            // perf, but autoscale is helpful
            if (AutoScale && (value > Max || value < Min))
            {
                Min = Mathf.Min(value, Min);
                Max = Mathf.Max(value, Max);
            }

            // don't print outside
            var val = Mathf.Clamp(value, Min, Max);

            Values[CurrentIndex] = val;
            CurrentIndex = (CurrentIndex + 1) % Values.Length;
        }
    }

    private Vector2 GraphSize { get; }
    private readonly List<Dataset> _datasets;

    public Graph(Vector2 size, IEnumerable<Dataset> datasets = null) {
        GraphSize = size;
        _datasets = (datasets ?? Array.Empty<Dataset>()).ToList();
    }

    public int DatasetCount() => _datasets.Count;

    public void AddDataset(Dataset dataset) {
        var box = GetNode<VBoxContainer>("LabelContainer");
        var l = new Label() { Text = dataset.GraphName };
        l.AddThemeColorOverride("font_color", dataset.Color);
        box.AddChild(l);
        _datasets.Add(dataset);
    }

    public void RemoveDataset(Dataset dataset) {
        var box = GetNode<VBoxContainer>("LabelContainer");
        var data = box.GetChildren().Where(x => x is Label).First(x => ((Label)x).Text == dataset.GraphName);
        box.RemoveChild(data);
        data.QueueFree();
        _datasets.Remove(dataset);
    }

    // https://github.com/WeaverDev/DebugGUIGraph/blob/master/addons/DebugGUI/Windows/GraphWindow.cs#L554

    public override void _Ready() {
        var box = new VBoxContainer() {
            Name = "LabelContainer"
        };
        AddChild(box);
        foreach (var dataset in _datasets) {
            var l = new Label() {
                Text = dataset.GraphName,
                Name = "Label"
            };
            l.AddThemeColorOverride("font_color", dataset.Color);
            box.AddChild(l);
        }
        AddChild(new ColorRect() {
            Color = Colors.Transparent, // not used for display, used for sizing the graph part
            CustomMinimumSize = GraphSize,
            Name = "GraphRect"
        });
    }

    public override void _Draw()
    {
        var colorRect = GetNode<ColorRect>("GraphRect");
        var pos = GetRect().End - GetRect().Position;
        DrawRect(new Rect2(0, 0, pos.X, pos.Y), new Color(0, 0, 0, 0.2f));

        var defaultFont = ThemeDB.FallbackFont;
        int defaultFontSize = ThemeDB.FallbackFontSize;
        var offset = new Vector2();
        foreach (var dataset in _datasets) {
            DrawString(defaultFont, colorRect.Position + new Vector2(0, defaultFontSize) + offset,
                float.Round(dataset.Max, 2).ToString(), HorizontalAlignment.Left, -1, defaultFontSize,  dataset.Color);
            DrawString(defaultFont, colorRect.Position + new Vector2(0, colorRect.Size.Y) + offset,
                float.Round(dataset.Min, 2).ToString(), HorizontalAlignment.Left, -1, defaultFontSize,  dataset.Color);

            var fraction = Mathf.InverseLerp(dataset.Min, dataset.Max, 0);
            if (fraction > 0 && fraction < 1 && Math.Abs(fraction - 0.5f) < 0.35f) {
                // add a 0 label when its inbetween min and max
                // and prevent overlapping the min/max values a little
                DrawString(defaultFont, colorRect.Position + new Vector2(3, colorRect.Size.Y * (1-fraction) + defaultFontSize/2) + offset,
                    "0", HorizontalAlignment.Left, -1, defaultFontSize,  dataset.Color);
            }

            offset += new Vector2(50, 0);
        }

        foreach (var dataset in _datasets) {
            int num = dataset.Values.Length;
            for (int i = 0; i < num; i++)
            {
                float value = dataset.Values[Mod(dataset.CurrentIndex - i - 1, dataset.Values.Length)];
                // Note flipped inverse lerp min max to account for y = down in godot UI
                dataset.GraphPoints[i] = new Vector2(
                    colorRect.Position.X + (colorRect.Size.X * ((float)(num - i) / num)), //backwards
                    colorRect.Position.Y + (Mathf.InverseLerp(dataset.Max, dataset.Min, value) * colorRect.Size.Y)
                );
            }

            DrawPolyline(dataset.GraphPoints, dataset.Color);
        }
    }

    public override void _Process(double delta) {
        QueueRedraw();
    }

    private static int Mod(int n, int m)
    {
        return ((n % m) + m) % m;
    }
}
