using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public partial class WorldText : Node3D {

    private string _preReadyText = null;

    public void SetText(string text) {
        if (!IsNodeReady()) {
            _preReadyText = text;
        } else {
            _label.Text = text;
        }
    }
    public string GetText() {
        if (!IsNodeReady())
            return _preReadyText;
        return _label.Text;
    }

    private Label _label;

    public override void _Ready() {
        _label = GetNode<Label>("SubViewport/Label");

        if (_preReadyText != null) {
            _label.Text = _preReadyText;
            _preReadyText = null;
        }
    }
}
