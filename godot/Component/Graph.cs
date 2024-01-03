using Godot;

namespace murph9.RallyGame2.godot.Component;

public partial class Graph : HBoxContainer {

    private readonly string _graphName;
    private readonly Vector2 _graphSize;
    private readonly float _min;
    private readonly float _max;
    public readonly float[] Values;
    public readonly Vector2[] GraphPoints;

    public string GraphName;
    private int _currentIndex;

    public Graph(string graphName, Vector2 size, float min, float max) {
        _graphName = graphName;
        _graphSize = size;
        _min = min;
        _max = max;
        Values = new float[(int)size.X];
        GraphPoints = new Vector2[(int)size.X];
    }

    // https://github.com/WeaverDev/DebugGUIGraph/blob/master/addons/DebugGUI/Windows/GraphWindow.cs#L554

    public override void _Ready() {
        AddChild(new Label() {
            Text = _graphName,
            Name = "Label",
            LabelSettings = new LabelSettings() {
                FontColor = Colors.Blue
            }
        });
        AddChild(new ColorRect() {
            Color = Colors.Transparent, // not used for display, used for sizing the graph
            CustomMinimumSize = _graphSize,
            Size = _graphSize,
            Name = "GraphRect"
        });
    }

    public void Push(float value) {
        // don't print outside
        var val = Mathf.Clamp(value, _min, _max);

        Values[_currentIndex] = val;
        _currentIndex = (_currentIndex + 1) % Values.Length;
    }

    public override void _Draw()
    {
        var colorRect = GetNode<ColorRect>("GraphRect");
        DrawRect(GetRect(), new Color(1,1,1,0.2f));

        var defaultFont = ThemeDB.FallbackFont;
        int defaultFontSize = ThemeDB.FallbackFontSize;
        DrawString(defaultFont, colorRect.Position + new Vector2(3, defaultFontSize),
            float.Round(_max, 2).ToString(), HorizontalAlignment.Left, -1, defaultFontSize,  Colors.Blue);
        DrawString(defaultFont, colorRect.Position + new Vector2(3, colorRect.Size.Y),
            float.Round(_min, 2).ToString(), HorizontalAlignment.Left, -1, defaultFontSize,  Colors.Blue);

        int num = Values.Length;
        for (int i = 0; i < num; i++)
        {
            float value = Values[Mod(_currentIndex - i - 1, Values.Length)];
            // Note flipped inverse lerp min max to account for y = down in godot UI
            GraphPoints[i] = new Vector2(
                colorRect.Position.X + (colorRect.Size.X * ((float)(num - i) / num)), //backwards
                colorRect.Position.Y + (Mathf.InverseLerp(_max, _min, value) * colorRect.Size.Y)
            );
        }

        DrawPolyline(GraphPoints, Colors.Blue);
    }

    public override void _Process(double delta) {
        QueueRedraw();
    }

    private static int Mod(int n, int m)
    {
        return ((n % m) + m) % m;
    }
}
