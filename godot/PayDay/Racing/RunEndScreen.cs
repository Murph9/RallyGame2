using Godot;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Shown after a run timer expires. Lists money earned and all parts collected,
/// each with a colour swatch matching its rarity tier.
/// </summary>
public partial class RunEndScreen : CenterContainer {

    [Signal]
    public delegate void ReturnHomeEventHandler();

    private VBoxContainer _partsContainer;
    private Label _moneyLabel;

    public override void _Ready() {
        _partsContainer = GetNode<VBoxContainer>("Panel/VBox/PartsContainer");
        _moneyLabel = GetNode<Label>("Panel/VBox/MoneyLabel");
    }

    public void Populate(float moneyEarned, List<CollectedPart> parts) {
        _moneyLabel.Text = $"Money earned this run: ${moneyEarned:F0}";

        foreach (var child in _partsContainer.GetChildren())
            child.QueueFree();

        if (parts.Count == 0) {
            var emptyLabel = new Label { Text = "No parts collected." };
            _partsContainer.AddChild(emptyLabel);
            return;
        }

        foreach (var part in parts) {
            var row = new HBoxContainer();

            row.AddChild(new ColorRect {
                CustomMinimumSize = new Vector2(20, 20),
                Color = PartRarityHelper.GetColour(part.Rarity)
            });

            row.AddChild(new Label {
                Text = $"  [{PartRarityHelper.GetDisplayName(part.Rarity)}]  {part.Part?.Name ?? "Unknown Part"}"
            });

            _partsContainer.AddChild(row);
        }
    }

    public void ReturnHomeButton_Pressed() {
        EmitSignal(SignalName.ReturnHome);
    }
}
