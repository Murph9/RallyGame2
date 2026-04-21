using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.UI;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.Utilities;
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
            var row = new PartRow(part, currentLevel, _state.PartInventory.Where(x => x.Part.Code == part.Code),
                (from, to) => _state.CarDetails.CalcDeltaForPart(part, from, to));
            row.Updated += Refresh;

            childBox.AddChild(row);
            _rows.Add(row);
        }

        childBox.AddChild(new HSeparator());
    }

    private void Refresh() {
        var stagedPublicStats = ComputeStagedPublicStats();
        _carStatsUI?.SetCarDetails(_state.CarDetails, stagedPublicStats);

        if (_confirmButton == null)
            return;

        var countChanged = _rows.Count(x => x.GetResult()?.Rarity != null);
        _confirmButton.Disabled = countChanged == 0;
        _confirmButton.Text = countChanged > 0
            ? $"Confirm ({countChanged} change{(countChanged == 1 ? "" : "s")})"
            : "Confirm";
    }

    /// <summary>
    /// Temporarily applies all staged part changes to CarDetails, reads the resulting
    /// public stats, then reverts — so the live car state is never permanently mutated.
    /// Returns null when there are no staged changes.
    /// </summary>
    private SimpleCarStats ComputeStagedPublicStats() {
        var staged = _rows.Select(r => r.GetResult()).Where(r => r != null).ToList();
        if (staged.Count == 0)
            return null;

        // Save current levels so we can revert
        var saved = staged.Select(sp => (sp.Part, CurrentLevel: _state.CarDetails.LevelOfPart(sp.Part))).ToList();

        try {
            // Apply staged changes
            foreach (var sp in staged)
                _state.CarDetails.ApplyPartChange(sp.Part, sp.Rarity);

            return CarStatsCalculator.ComputeSimpleStats(_state.CarDetails);
        } finally {
            // Always revert, even if an exception occurs
            foreach (var (part, level) in saved)
                _state.CarDetails.ApplyPartChange(part, level);
        }
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
/// When a change is staged, a stat-delta panel is shown below comparing the current and new values.
/// </summary>
public partial class PartRow : VBoxContainer {

    [Signal]
    public delegate void UpdatedEventHandler();

    private readonly List<PartLevel> _tierLevels = [];

    private readonly PartDetails _part;
    private readonly PartLevel _existingRarity;
    private readonly Func<PartLevel, PartLevel, IEnumerable<PartDelta>> _calcDelta;
    private PartLevel? _newRarity = null;

    private OptionButton _dropdown;
    private Label _changeIndicator;
    private ColorRect _colorBar;
    private RichTextLabel _deltaLabel;

    public void Reset() {
        _newRarity = null;
        UpdateDropdown();
    }
    public StagedPart GetResult() => _newRarity.HasValue ? new StagedPart(_part, _newRarity.Value) : null;

    public PartRow(PartDetails part, PartLevel currentRarity, IEnumerable<CollectedPart> ownedRarities, Func<PartLevel, PartLevel, IEnumerable<PartDelta>> calcDelta) {
        _part = part;
        _existingRarity = currentRarity;
        _calcDelta = calcDelta;

        SizeFlagsHorizontal = SizeFlags.Fill;
        MouseFilter = MouseFilterEnum.Stop;

        // ── Main row ─────────────────────────────────────────────────────────
        var mainRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill };
        AddChild(mainRow);

        // Colour bar reflecting the current (equipped) rarity
        _colorBar = new ColorRect {
            CustomMinimumSize = new Vector2(6, 0),
            Color = PartLevelHelper.GetColour(currentRarity),
            SizeFlagsVertical = SizeFlags.Fill,
        };
        mainRow.AddChild(_colorBar);

        if (part?.IconImage != null) {
            mainRow.AddChild(new TextureRect {
                Texture = part.IconImage,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            });
        }

        // Part name
        mainRow.AddChild(new Label {
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
        mainRow.AddChild(badgeRow);

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
        mainRow.AddChild(_dropdown);

        // Change indicator symbol shown after the dropdown
        _changeIndicator = new Label {
            Text = "",
            CustomMinimumSize = new Vector2(24, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        mainRow.AddChild(_changeIndicator);

        // ── Delta panel (hidden until a change is staged) ─────────────────────
        _deltaLabel = new RichTextLabel {
            BbcodeEnabled = true,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.Off,
            SizeFlagsHorizontal = SizeFlags.Fill,
            Visible = false,
        };
        _deltaLabel.AddThemeFontSizeOverride("normal_font_size", 11);
        _deltaLabel.AddThemeFontSizeOverride("bold_font_size", 11);
        AddChild(_deltaLabel);

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
            UpdateDeltaPanel(_existingRarity, _newRarity.Value);
        } else {
            _changeIndicator.Text = "";
            _colorBar.Color = PartLevelHelper.GetColour(_existingRarity);
            _deltaLabel.Visible = false;
        }

        // Sync dropdown selection back (e.g. after Reset)
        if (!_newRarity.HasValue) {
            var currentIdx = _tierLevels.IndexOf(_existingRarity);
            if (currentIdx >= 0)
                _dropdown.Select(currentIdx);
        }
    }

    private void UpdateDeltaPanel(PartLevel fromLevel, PartLevel toLevel) {
        _deltaLabel.Clear();

        var deltas = _calcDelta?.Invoke(fromLevel, toLevel).ToList();
        if (deltas == null || deltas.Count == 0) {
            _deltaLabel.Visible = false;
            return;
        }

        _deltaLabel.PushIndent(1);
        foreach (var delta in deltas) {
            var fromStr = delta.FromValue != null ? GodotClassHelper.ToStringWithRounding(delta.FromValue, 2) : "-";
            var toStr = delta.ToValue != null ? GodotClassHelper.ToStringWithRounding(delta.ToValue, 2) : "-";

            bool isImprovement = delta.HigherIs == HigherIs.Good
                ? (dynamic)delta.ToValue > (dynamic)delta.FromValue
                : delta.HigherIs == HigherIs.Bad
                    ? (dynamic)delta.ToValue < (dynamic)delta.FromValue
                    : true;

            var arrowColour = isImprovement ? "green" : "orange";
            var arrow = isImprovement ? "▲" : "▼";

            _deltaLabel.AppendText($"[color=gray]{delta.FieldName}:[/color]  " +
                                   $"[color=lightblue]{fromStr}[/color]  " +
                                   $"[color={arrowColour}]{arrow} {toStr}[/color]\n");
        }
        _deltaLabel.Pop(); // indent

        _deltaLabel.Visible = true;
    }
}
