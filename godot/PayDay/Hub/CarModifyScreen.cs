using Godot;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay.Hub;

/// <summary>
/// Fullscreen overlay shown when the player clicks the car in the hub.
/// Left side: live 3D car view
/// Right side: scrollable list of collected parts with Apply buttons.
/// Emits Closed or StartRacing on the respective button presses.
/// </summary>
public partial class CarModifyScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    public Vector3? Center { get; set; } = null;

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        if (Center.HasValue) {
            GetViewport().GetCamera3D().LookAt(Center.Value);
        }

        PopulatePartsList(state);
    }

    private void PopulatePartsList(PayDayGlobalState state) {
        var partsList = GetNode<VBoxContainer>("HBox/Panel/VBox/ScrollContainer/PartsList");

        // Clear any existing rows (in case we refresh)
        foreach (var child in partsList.GetChildren()) {
            child.QueueFree();
        }

        if (state.PartInventory.Count == 0) {
            var empty = new Label { Text = "No parts collected yet." };
            partsList.AddChild(empty);
            return;
        }

        // Snapshot the inventory so iteration is stable while we build rows
        var inventorySnapshot = new List<CollectedPart>(state.PartInventory);

        foreach (var collectedPart in inventorySnapshot) {
            var part = collectedPart.Part;
            if (part == null) continue;

            var currentLevel = state.CarDetails.LevelOfPart(part);
            var maxLevel = part.Levels.Length - 1;

            var row = new HBoxContainer();

            // Rarity colour swatch
            var rarityLabel = new Label {
                Text = PartRarityHelper.GetDisplayName(collectedPart.Rarity),
                CustomMinimumSize = new Vector2(80, 0)
            };
            rarityLabel.AddThemeColorOverride("font_color", PartRarityHelper.GetColour(collectedPart.Rarity));
            row.AddChild(rarityLabel);

            // Part name
            var nameLabel = new Label {
                Text = part.Name,
                SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
            };
            row.AddChild(nameLabel);

            // Current level indicator
            var levelLabel = new Label {
                Text = $"Lv {currentLevel}/{maxLevel}",
                CustomMinimumSize = new Vector2(60, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            row.AddChild(levelLabel);

            // Apply button
            var applyBtn = new Button {
                Text = currentLevel >= maxLevel ? "Max" : "Apply",
                Disabled = currentLevel >= maxLevel,
                CustomMinimumSize = new Vector2(70, 0)
            };

            // Capture loop variables for the closure
            var capturedPart = collectedPart;

            applyBtn.Pressed += () => {
                var nextLevel = state.CarDetails.LevelOfPart(capturedPart.Part) + 1;
                if (nextLevel > capturedPart.Part.Levels.Length - 1) return;

                state.CarDetails.ApplyPartChange(capturedPart.Part, nextLevel);
                state.RemoveCollectedPart(capturedPart);

                // Refresh the whole list so the consumed row disappears
                PopulatePartsList(state);
            };

            row.AddChild(applyBtn);
            partsList.AddChild(row);
        }
    }

    public void ApplyButton_Pressed() => EmitSignal(SignalName.Closed);
    public void CloseButton_Pressed() => EmitSignal(SignalName.Closed);
}
