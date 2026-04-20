using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.UI;
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
public partial class CarModifyScreen : HBoxContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    public Vector3? Center { get; set; } = null;
    private readonly List<PartRow> _rows = [];

    private PayDayGlobalState _state;
    private VBoxContainer _treeContainer;
    private Button _confirmButton;
    private CarStatsUI _carStatsUI;


    public override void _Ready() {
        _state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        if (Center.HasValue)
            GetViewport().GetCamera3D().LookAt(Center.Value);

        _treeContainer = GetNode<VBoxContainer>("LeftPanel/VBox/ScrollContainer/TreeContainer");
        _confirmButton = GetNode<Button>("RightPanel/VBox/ButtonRow/ConfirmButton");
        _confirmButton.Disabled = true;

        PopulateTree();

        if (_state?.CarDetails != null) {
            var box = GetNode<Control>("RightPanel/VBox/CarStatsUI");
            _carStatsUI = new CarStatsUI();
            _carStatsUI.SetCarDetails(_state.CarDetails);
            _carStatsUI.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            box.AddChild(_carStatsUI);
        }
    }

    // ─── Tree population ──────────────────────────────────────────────────────

    private void PopulateTree() {
        foreach (var child in _treeContainer.GetChildren())
            child.QueueFree();

        if (_state.PartInventory.Count == 0) {
            _treeContainer.AddChild(new Label { Text = "No parts in inventory." });
            Refresh();
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

        Refresh();
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
            row.Updated += Refresh;

            childBox.AddChild(row);
            _rows.Add(row);
        }

        childBox.AddChild(new HSeparator());
    }

    private void Refresh() {
        _carStatsUI?.SetCarDetails(_state.CarDetails);

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
/// Shows an OptionButton dropdown to select a rarity tier and a symbol next to it indicating
/// whether the staged selection is an upgrade (▲), downgrade (▼), or unchanged (no symbol).
/// </summary>
public partial class PartRow : HBoxContainer {

    [Signal]
    public delegate void UpdatedEventHandler();

    private readonly List<PartLevel> _tierLevels = [];

    private readonly PartDetails _part;
    private readonly PartLevel _existingRarity;
    private PartLevel? _newRarity = null;

    private OptionButton _dropdown;
    private Label _changeIndicator;
    private ColorRect _colorBar;

    public void Reset() {
        _newRarity = null;
        UpdateDropdown();
    }
    public StagedPart GetResult() => _newRarity.HasValue ? new StagedPart(_part, _newRarity.Value) : null;

    public PartRow(PartDetails part, PartLevel currentRarity, IEnumerable<CollectedPart> ownedRarities) {
        _part = part;
        _existingRarity = currentRarity;

        SizeFlagsHorizontal = SizeFlags.Fill;
        MouseFilter = MouseFilterEnum.Stop;

        // Colour bar reflecting the current (equipped) rarity
        _colorBar = new ColorRect {
            CustomMinimumSize = new Vector2(6, 0),
            Color = PartLevelHelper.GetColour(currentRarity),
            SizeFlagsVertical = SizeFlags.Fill,
        };
        AddChild(_colorBar);

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

        // Tier badges — small coloured pills showing every available rarity at a glance
        var badgeRow = new HBoxContainer {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        foreach (var (level, index) in part.Levels.WithIndex()) {
            if ((PartLevel)index != PartLevel.Common && !ownedRarities.Any(x => (int)x.Rarity == index))
                continue;

            var rarity = (PartLevel)index;
            var badge = new Label {
                Text = PartLevelHelper.GetDisplayName(rarity),
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.Off,
            };
            badge.AddThemeColorOverride("font_color", PartLevelHelper.GetColour(rarity));
            badge.AddThemeFontSizeOverride("font_size", 10);
            badgeRow.AddChild(badge);

            // Separator dot between badges (not after last)
            badgeRow.AddChild(new Label {
                Text = " · ",
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.Off,
            });
        }
        // Remove the trailing separator
        var children = badgeRow.GetChildren();
        if (children.Count > 0)
            children[^1].QueueFree();
        AddChild(badgeRow);

        // Build the dropdown with owned tiers (Common always included)
        _dropdown = new OptionButton {
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
        };

        foreach (var (level, index) in part.Levels.WithIndex()) {
            if ((PartLevel)index != PartLevel.Common && !ownedRarities.Any(x => (int)x.Rarity == index))
                continue;

            var rarity = (PartLevel)index;
            var label = PartLevelHelper.GetDisplayName(rarity);
            _dropdown.AddItem(label);
            _tierLevels.Add(rarity);
        }

        // Pre-select the currently equipped tier
        var currentIdx = _tierLevels.IndexOf(currentRarity);
        if (currentIdx >= 0)
            _dropdown.Select(currentIdx);

        _dropdown.ItemSelected += OnItemSelected;
        AddChild(_dropdown);

        // Change indicator symbol shown after the dropdown
        _changeIndicator = new Label {
            Text = "",
            CustomMinimumSize = new Vector2(24, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        AddChild(_changeIndicator);

        UpdateDropdown();
    }

    private void OnItemSelected(long idx) {
        var selected = _tierLevels[(int)idx];
        _newRarity = selected == _existingRarity ? null : selected;
        UpdateDropdown();
        EmitSignal(SignalName.Updated);
    }

    private void UpdateDropdown() {
        if (_newRarity.HasValue) {
            var upgraded = (int)_newRarity.Value > (int)_existingRarity;
            _changeIndicator.Text = upgraded ? "▲" : "▼";
            _changeIndicator.AddThemeColorOverride("font_color",
                upgraded ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.3f));
            _colorBar.Color = PartLevelHelper.GetColour(_newRarity.Value);
        } else {
            _changeIndicator.Text = "";
            _colorBar.Color = PartLevelHelper.GetColour(_existingRarity);
        }

        // Sync dropdown selection back (e.g. after Reset)
        if (!_newRarity.HasValue) {
            var currentIdx = _tierLevels.IndexOf(_existingRarity);
            if (currentIdx >= 0)
                _dropdown.Select(currentIdx);
        }
    }
}
