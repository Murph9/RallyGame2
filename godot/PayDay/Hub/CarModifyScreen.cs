using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.PayDay.Hub;

/// <summary>
/// Fullscreen overlay shown when the player clicks the car in the hub.
/// Left panel: grouped collapsible tree of collected parts, organised by subsystem.
///   Each part row shows one button per owned rarity variant (only applicable ones shown).
///   Staged changes are not committed until the player presses Confirm.
///   Parts apply at the level matching their rarity tier.
///   Poor parts (level 0) can always downgrade/reset a part back to default.
///   Other tiers are blocked if their target level is not above the current level.
/// Centre: the live 3D car view (camera switched to "car" by HubScene before this opens).
/// </summary>
public partial class CarModifyScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    public Vector3? Center { get; set; } = null;
    private readonly List<PartRow> _rows = [];

    private PayDayGlobalState _state;
    private VBoxContainer _treeContainer;
    private Button _confirmButton;


    public override void _Ready() {
        _state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        if (Center.HasValue)
            GetViewport().GetCamera3D().LookAt(Center.Value);

        _treeContainer = GetNode<VBoxContainer>("HBox/Panel/VBox/ScrollContainer/TreeContainer");
        _confirmButton = GetNode<Button>("HBox/Panel/VBox/ConfirmButton");
        _confirmButton.Disabled = true;

        PopulateTree();
    }

    // ─── Tree population ──────────────────────────────────────────────────────

    private void PopulateTree() {
        foreach (var child in _treeContainer.GetChildren())
            child.QueueFree();

        if (_state.PartInventory.Count == 0) {
            _treeContainer.AddChild(new Label { Text = "No parts in inventory." });
            RefreshConfirmButton();
            return;
        }

        var partsByCategory = _state.CarDetails.GetAllPartsInTree().GroupBy(x => _state.CarDetails.GetPartCategory(x));

        bool anyGroup = false;
        foreach (var group in partsByCategory) {
            if (!group.Any())
                continue;

            anyGroup = true;
            AddGroupSection(group.Key.ToString(), [.. group]);
        }

        if (!anyGroup)
            _treeContainer.AddChild(new Label { Text = "No parts in inventory." });

        RefreshConfirmButton();
    }

    private void AddGroupSection(string groupName, List<PartDetails> parts) {
        var headerBtn = new Button {
            Text = $"▼  {groupName}  ({parts.Count})",
            Flat = true,
            SizeFlagsHorizontal = SizeFlags.Fill,
            Alignment = HorizontalAlignment.Left,
        };
        headerBtn.AddThemeColorOverride("font_color", Colors.LightGray);
        _treeContainer.AddChild(headerBtn);

        var childBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill };
        _treeContainer.AddChild(childBox);

        headerBtn.Pressed += () => {
            childBox.Visible = !childBox.Visible;
            headerBtn.Text = (childBox.Visible ? "▼  " : "▶  ") + groupName + $"  ({parts.Count})";
        };

        foreach (var part in parts) {
            var currentLevel = _state.CarDetails.LevelOfPart(part);
            var row = new PartRow(part, currentLevel, _state.PartInventory.Where(x => x.Part.Code == part.Code));
            row.Updated += RefreshConfirmButton;

            childBox.AddChild(row);
            _rows.Add(row);
        }

        childBox.AddChild(new HSeparator());
    }

    private void RefreshConfirmButton() {
        if (_confirmButton == null)
            return;

        var countChanged = _rows.Count(x => x.GetResult()?.Rarity != null);
        _confirmButton.Disabled = countChanged == 0;
        _confirmButton.Text = countChanged > 0
            ? $"Confirm ({countChanged} change{(countChanged == 1 ? "" : "s")})"
            : "Confirm";
    }

    public void ConfirmButton_Pressed() {
        foreach (var row in _rows) {
            var newRarity = row.GetResult();
            if (newRarity != null)
                _state.CarDetails.ApplyPartChange(newRarity.Part, newRarity.Rarity);
            row.Reset();
        }
        EmitSignal(SignalName.Closed);
    }

    public void CloseButton_Pressed() {
        foreach (var row in _rows)
            row.Reset();
        EmitSignal(SignalName.Closed);
    }
}


public partial class StagedPart(PartDetails part, PartLevel rarity) : RefCounted {
    public PartDetails Part { get; } = part;
    public PartLevel Rarity { get; } = rarity;

    public override bool Equals(object? obj) {
        if (obj == null) return false;
        if (obj is not StagedPart sp) return false;
        return sp.Part == Part && sp.Rarity == Rarity;
    }
    public override int GetHashCode() => 79 * Part.GetHashCode() + 41 * Rarity.GetHashCode();
}

/// <summary>
/// A single row in the part tree representing one part across all owned rarity variants.
/// The staged tier shows ✕ and current is ✓ instead of its name.
/// </summary>
public partial class PartRow : HBoxContainer {

    [Signal]
    public delegate void UpdatedEventHandler();

    private readonly List<(PartLevel, Button)> _tiers = [];

    private readonly PartDetails _part;
    private readonly PartLevel _existingRarity;
    private PartLevel? _newRarity = null;

    public void Reset() => _newRarity = null;
    public StagedPart GetResult() => _newRarity.HasValue ? new StagedPart(_part, _newRarity.Value) : null;

    public PartRow(PartDetails part, PartLevel currentRarity, IEnumerable<CollectedPart> ownedRarities) {
        _part = part;
        _existingRarity = currentRarity;

        SizeFlagsHorizontal = SizeFlags.Fill;
        MouseFilter = MouseFilterEnum.Stop;

        // Colour of the current rarity
        AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(6, 0),
            Color = PartLevelHelper.GetColour(currentRarity),
            SizeFlagsVertical = SizeFlags.Fill,
        });

        if (part?.IconImage != null) {
            AddChild(new TextureRect {
                Texture = part.IconImage,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            });
        }

        // Part name
        AddChild(new Label {
            Text = part.Name ?? "Unknown",
            SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
            VerticalAlignment = VerticalAlignment.Center,
        });

        foreach (var (level, index) in part.Levels.WithIndex()) {
            // always show common, but and only show owned tiers
            if ((PartLevel)index != PartLevel.Common && !ownedRarities.Any(x => (int)x.Rarity == index))
                continue;

            var rarity = (PartLevel)index;

            var btn = new Button {
                CustomMinimumSize = new Vector2(80, 0)
            };
            btn.AddThemeColorOverride("font_color", PartLevelHelper.GetColour(rarity));
            btn.Pressed += () => ButtonPressed(rarity);

            AddChild(btn);
            _tiers.Add(new(rarity, btn));
        }
        UpdateButtons();
    }

    private void ButtonPressed(PartLevel rarity) {
        if (_existingRarity == rarity) {
            _newRarity = null;
        } else {
            _newRarity = rarity;
        }
        UpdateButtons();
    }

    private void UpdateButtons() {
        foreach (var tier in _tiers) {
            var btn = tier.Item2;
            if (tier.Item1 == _newRarity) {
                btn.Text = "✓";
            } else if (tier.Item1 == _existingRarity) {
                btn.Text = "Owned";
            } else {
                btn.Text = PartLevelHelper.GetDisplayName(tier.Item1);
            }
        }

        EmitSignal(SignalName.Updated);
    }
}
