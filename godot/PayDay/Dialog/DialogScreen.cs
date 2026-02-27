using Godot;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay.Dialog;

/// <summary>
/// A modal CanvasLayer that steps through a list of DialogLines on click/keypress.
/// Used for the intro sequence and phone calls from the house hub.
/// </summary>
public partial class DialogScreen : CanvasLayer {

    [Signal]
    public delegate void ClosedEventHandler();

    private List<DialogLine> _lines = [];
    private int _index;

    private Label _speakerLabel;
    private Label _textLabel;
    private TextureRect _portrait;

    public override void _Ready() {
        _speakerLabel = GetNodeOrNull<Label>("Panel/VBox/SpeakerLabel");
        _textLabel = GetNodeOrNull<Label>("Panel/VBox/TextLabel");
        _portrait = GetNodeOrNull<TextureRect>("Panel/Portrait");
    }

    public void SetDialogLines(List<DialogLine> lines) {
        _lines = lines;
        _index = 0;
        ShowCurrent();
    }

    private void ShowCurrent() {
        if (_index >= _lines.Count) {
            EmitSignal(SignalName.Closed);
            return;
        }

        var line = _lines[_index];

        if (_speakerLabel != null) {
            _speakerLabel.Text = line.SpeakerName;
            _speakerLabel.AddThemeColorOverride("font_color", line.SpeakerColour);
        }

        if (_textLabel != null)
            _textLabel.Text = line.Text;

        // Portrait swap: look for res://assets/images/dialog/<speakerName>.png
        if (_portrait != null) {
            var portraitPath = $"res://assets/images/dialog/{line.SpeakerName.ToLower()}.png";
            if (ResourceLoader.Exists(portraitPath))
                _portrait.Texture = GD.Load<Texture2D>(portraitPath);
        }
    }

    public override void _Input(InputEvent @event) {
        if (!Visible) return;

        bool advance = @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
                    || @event is InputEventKey { Pressed: true };

        if (advance) {
            _index++;
            ShowCurrent();
        }
    }
}
