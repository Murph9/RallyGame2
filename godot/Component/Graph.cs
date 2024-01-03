using Godot;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Component;

public partial class Graph : HBoxContainer {

	public class Dataset {
        public string GraphName { get; }
        public float Min { get; }
        public float Max { get; }
        internal int CurrentIndex  { get; private set; }

        public Color Color { get; set; } = Colors.Blue;

        public readonly float[] Values;
        public readonly Vector2[] GraphPoints;

        public Dataset(string graphName, float min, float max) {
            GraphName = graphName;
            Min = min;
            Max = max;
            Values = new float[1090];
            GraphPoints = new Vector2[1090]; // enough to fill the screen
        }

        public void Push(float value) {
            // don't print outside
            var val = Mathf.Clamp(value, Min, Max);

            Values[CurrentIndex] = val;
            CurrentIndex = (CurrentIndex + 1) % Values.Length;
        }
    }

    private readonly Vector2 _graphSize;

    public List<Dataset> Datasets { get; set; }

    public Graph(Vector2 size, IEnumerable<Dataset> datasets) {
        _graphSize = size;
        Datasets = datasets.ToList();
    }

    // https://github.com/WeaverDev/DebugGUIGraph/blob/master/addons/DebugGUI/Windows/GraphWindow.cs#L554

    public override void _Ready() {
        var box = new VBoxContainer();
        AddChild(box);
        foreach (var dataset in Datasets) {
            box.AddChild(new Label() {
                Text = dataset.GraphName,
                Name = "Label",
                LabelSettings = new LabelSettings() {
                    FontColor = dataset.Color
                }
            });
        }
        AddChild(new ColorRect() {
            Color = Colors.Transparent, // not used for display, used for sizing the graph
            CustomMinimumSize = _graphSize,
            Size = _graphSize,
            Name = "GraphRect"
        });
    }

    public override void _Draw()
    {
        var colorRect = GetNode<ColorRect>("GraphRect");
        DrawRect(GetRect(), new Color(1,1,1,0.2f));

        var defaultFont = ThemeDB.FallbackFont;
        int defaultFontSize = ThemeDB.FallbackFontSize;
        var offset = new Vector2();
        foreach (var dataset in Datasets) {
            DrawString(defaultFont, colorRect.Position + new Vector2(3, defaultFontSize) + offset,
                float.Round(dataset.Max, 2).ToString(), HorizontalAlignment.Left, -1, defaultFontSize,  dataset.Color);
            DrawString(defaultFont, colorRect.Position + new Vector2(3, colorRect.Size.Y) + offset,
                float.Round(dataset.Min, 2).ToString(), HorizontalAlignment.Left, -1, defaultFontSize,  dataset.Color);
            offset += new Vector2(50, 0);
        }

        foreach (var dataset in Datasets) {
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
