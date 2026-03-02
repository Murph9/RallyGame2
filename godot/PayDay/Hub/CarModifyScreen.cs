using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.PayDay.Hub;

/// <summary>
/// Fullscreen overlay shown when the player clicks the car in the hub.
/// Left side: live 3D car view via SubViewport.
/// Right side: scrollable list of collected parts with Apply buttons.
/// Emits Closed or StartRacing on the respective button presses.
/// </summary>
public partial class CarModifyScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();
    [Signal]
    public delegate void StartRacingEventHandler();

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        // ── 3D car view ─────────────────────────────────────────────────────
        var subViewport = GetNode<SubViewport>("Panel/HBox/SubViewportContainer/SubViewport");
        var carDisplay = new CarDisplayNode();
        carDisplay.Initialise(state.CarDetails);
        subViewport.AddChild(carDisplay);

        // ── Parts list ───────────────────────────────────────────────────────
        PopulatePartsList(state);
    }

    private void PopulatePartsList(PayDayGlobalState state) {
        var partsList = GetNode<VBoxContainer>("Panel/HBox/VBox/ScrollContainer/PartsList");

        // Clear any existing rows (in case we refresh)
        foreach (var child in partsList.GetChildren()) {
            child.QueueFree();
        }

        if (state.PartInventory.Count == 0) {
            var empty = new Label { Text = "No parts collected yet." };
            partsList.AddChild(empty);
            return;
        }

        foreach (var collectedPart in state.PartInventory) {
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
            var capturedPart = part;
            var capturedLevelLabel = levelLabel;
            var capturedApplyBtn = applyBtn;
            var capturedMaxLevel = maxLevel;

            applyBtn.Pressed += () => {
                var nextLevel = state.CarDetails.LevelOfPart(capturedPart) + 1;
                if (nextLevel > capturedMaxLevel) return;

                state.CarDetails.ApplyPartChange(capturedPart, nextLevel);

                var newLevel = state.CarDetails.LevelOfPart(capturedPart);
                capturedLevelLabel.Text = $"Lv {newLevel}/{capturedMaxLevel}";
                if (newLevel >= capturedMaxLevel) {
                    capturedApplyBtn.Text = "Max";
                    capturedApplyBtn.Disabled = true;
                }
            };

            row.AddChild(applyBtn);
            partsList.AddChild(row);
        }
    }

    public void CloseButton_Pressed() => EmitSignal(SignalName.Closed);
    public void StartRaceButton_Pressed() => EmitSignal(SignalName.StartRacing);
}
