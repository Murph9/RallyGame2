using Godot;
using murph9.RallyGame2.godot.Component.Rarity;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay.Parts;

/// <summary>
/// Shown for each newly collected part after a run.
/// Displays a "scratch card" style reveal with growing rarity glow,
/// comedic exclamation text, and a click-to-continue flow.
/// </summary>
public partial class PartApplyScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    private Queue<CollectedPart> _queue = new();
    private CollectedPart _current;

    private Label _rarityLabel;
    private Label _partNameLabel;
    private Label _exclamationLabel;
    private Panel _glowPanel;
    private Button _nextButton;

    public override void _Ready() {
        _rarityLabel = GetNode<Label>("Panel/VBox/RarityLabel");
        _partNameLabel = GetNode<Label>("Panel/VBox/PartNameLabel");
        _exclamationLabel = GetNode<Label>("Panel/VBox/ExclamationLabel");
        _glowPanel = GetNode<Panel>("Panel/GlowPanel");
        _nextButton = GetNode<Button>("Panel/VBox/NextButton");
    }

    public void SetParts(List<CollectedPart> parts) {
        _queue.Clear();
        foreach (var p in parts)
            _queue.Enqueue(p);
        ShowNext();
    }

    private void ShowNext() {
        if (_queue.Count == 0) {
            EmitSignal(SignalName.Closed);
            return;
        }

        _current = _queue.Dequeue();

        var rarityColour = PartRarityHelper.GetColour(_current.Rarity);
        var rarityName = PartRarityHelper.GetDisplayName(_current.Rarity);
        var exclamation = PartRarityHelper.GetExclamation(_current.Rarity);
        var partName = _current.Part?.Name ?? "PART";

        if (_rarityLabel != null) {
            _rarityLabel.Text = rarityName.ToUpper();
            _rarityLabel.AddThemeColorOverride("font_color", rarityColour);
        }

        if (_partNameLabel != null)
            _partNameLabel.Text = partName;

        if (_exclamationLabel != null) {
            _exclamationLabel.Text = $"{exclamation} {rarityName.ToUpper()} {partName.ToUpper()}!";
            _exclamationLabel.AddThemeColorOverride("font_color", rarityColour);
        }

        if (_glowPanel != null) {
            var style = new StyleBoxFlat {
                BgColor = rarityColour with { A = 0.2f },
                BorderColor = rarityColour
            };
            style.SetBorderWidthAll(3);
            _glowPanel.AddThemeStyleboxOverride("panel", style);
        }

        if (_nextButton != null)
            _nextButton.Text = _queue.Count > 0 ? "Next Part" : "Done";
    }

    public void NextButton_Pressed() => ShowNext();
}
