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

    private readonly Queue<CollectedPart> _queue = new();

    private CollectedPart _current;

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

        var rarityLabel = GetNode<Label>("Panel/VBox/RarityLabel");
        rarityLabel.Text = rarityName.ToUpper();
        rarityLabel.AddThemeColorOverride("font_color", rarityColour);

        var partNameLabel = GetNode<Label>("Panel/VBox/PartNameLabel");
        partNameLabel.Text = partName;

        var exclamationLabel = GetNode<Label>("Panel/VBox/ExclamationLabel");
        exclamationLabel.Text = $"{exclamation} {rarityName.ToUpper()} {partName.ToUpper()}!";
        exclamationLabel.AddThemeColorOverride("font_color", rarityColour);

        var style = new StyleBoxFlat {
            BgColor = rarityColour with { A = 0.2f },
            BorderColor = rarityColour
        };
        style.SetBorderWidthAll(3);
        var glowPanel = GetNode<Panel>("Panel/GlowPanel");
        glowPanel.AddThemeStyleboxOverride("panel", style);

        var nextButton = GetNode<Button>("Panel/VBox/NextButton");
        nextButton.Text = _queue.Count > 0 ? "Next Part" : "Done";
    }

    public void NextButton_Pressed() => ShowNext();
}
