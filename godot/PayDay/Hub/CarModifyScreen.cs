using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
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

    private PayDayGlobalState _state;
    private VBoxContainer _treeContainer;
    private Button _confirmButton;

    // Staged changes: part name → (CollectedPart, targetLevel).
    // Keyed by part name so staging the same part twice just overwrites.
    private readonly Dictionary<string, (CollectedPart Cp, PartLevel TargetLevel)> _staged = [];

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

        var groups = BuildGroups(_state);
        bool anyGroup = false;
        foreach (var (groupName, items) in groups) {
            if (items.Count == 0) continue;
            anyGroup = true;
            AddGroupSection(groupName, items);
        }

        if (!anyGroup)
            _treeContainer.AddChild(new Label { Text = "No parts in inventory." });

        RefreshConfirmButton();
    }

    /// <summary>
    /// Returns groups where each entry is a list of CollectedPart lists grouped by part name.
    /// Within each group, variants are ordered by descending rarity so the best shows first.
    /// </summary>
    private static List<(string Name, List<List<CollectedPart>> PartGroups)> BuildGroups(PayDayGlobalState state) {
        var engineNames = new HashSet<string>(state.CarDetails.Engine.GetAllPartsInTree().Select(p => p.Name));
        var susNames = new HashSet<string>(state.CarDetails.SuspensionDetails.GetAllPartsInTree().Select(p => p.Name));
        var tractionNames = new HashSet<string>(state.CarDetails.TractionDetails.GetAllPartsInTree().Select(p => p.Name));

        var engine = new List<CollectedPart>();
        var chassis = new List<CollectedPart>();
        var sus = new List<CollectedPart>();
        var traction = new List<CollectedPart>();

        foreach (var cp in state.PartInventory) {
            if (cp.Part == null) continue;
            if (engineNames.Contains(cp.Part.Name)) engine.Add(cp);
            else if (susNames.Contains(cp.Part.Name)) sus.Add(cp);
            else if (tractionNames.Contains(cp.Part.Name)) traction.Add(cp);
            else chassis.Add(cp);
        }

        return [
            ("Engine",         GroupByPartName(engine)),
            ("Chassis / Aero", GroupByPartName(chassis)),
            ("Suspension",     GroupByPartName(sus)),
            ("Traction",       GroupByPartName(traction)),
        ];
    }

    private static List<List<CollectedPart>> GroupByPartName(List<CollectedPart> parts) =>
        parts
            .GroupBy(cp => cp.Part.Name)
            .Select(g => g.OrderByDescending(cp => cp.Rarity).ToList())
            .ToList();

    private void AddGroupSection(string groupName, List<List<CollectedPart>> partGroups) {
        int partCount = partGroups.Count;
        var headerBtn = new Button {
            Text = $"▼  {groupName}  ({partCount})",
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
            headerBtn.Text = (childBox.Visible ? "▼  " : "▶  ") + groupName + $"  ({partCount})";
        };

        foreach (var group in partGroups) {
            if (group.Count == 0) continue;
            var currentLevel = _state.CarDetails.LevelOfPart(group[0].Part);
            var maxLevel = group[0].Part.Levels.Length - 1;
            var stagedCp = _staged.TryGetValue(group[0].Part.Name, out var s) ? s.Cp : null;

            var row = new PartRow(group, currentLevel, maxLevel, stagedCp);
            row.StageRequested += (cp) => StageChange(cp, row);
            row.UnstageRequested += (cp) => UnstageChange(cp, row);
            childBox.AddChild(row);
        }

        childBox.AddChild(new HSeparator());
    }

    // ─── Staging ─────────────────────────────────────────────────────────────

    private void StageChange(CollectedPart cp, PartRow row) {
        var currentLevel = _state.CarDetails.LevelOfPart(cp.Part);
        var maxLevel = cp.Part.Levels.Length - 1;
        var targetLevel = Math.Min((int)cp.Rarity, maxLevel);

        bool canApply = targetLevel == 0 ? currentLevel > 0 : targetLevel > (int)currentLevel;
        if (!canApply) return;

        _staged[cp.Part.Name] = (cp, (PartLevel)targetLevel);
        row.MarkStaged(cp);
        RefreshConfirmButton();
    }

    private void UnstageChange(CollectedPart cp, PartRow row) {
        _staged.Remove(cp.Part.Name);
        row.MarkUnstaged(cp);
        RefreshConfirmButton();
    }

    private void RefreshConfirmButton() {
        if (_confirmButton == null) return;
        _confirmButton.Disabled = _staged.Count == 0;
        _confirmButton.Text = _staged.Count > 0
            ? $"Confirm ({_staged.Count} change{(_staged.Count == 1 ? "" : "s")})"
            : "Confirm";
    }

    // ─── Confirm / Close ─────────────────────────────────────────────────────

    public void ConfirmButton_Pressed() {
        foreach (var (_, (cp, targetLevel)) in _staged) {
            _state.CarDetails.ApplyPartChange(cp.Part, targetLevel);
            _state.RemoveCollectedPart(cp);
        }
        _staged.Clear();
        PopulateTree();
    }

    public void CloseButton_Pressed() {
        _staged.Clear();
        EmitSignal(SignalName.Closed);
    }
}

// ─── Part row ─────────────────────────────────────────────────────────────────

/// <summary>
/// A single row in the part tree representing one part across all owned rarity variants.
/// Layout: [colour bar] [icon] [part name] [Common] [Rare] [Epic] ...
/// Only applicable tier buttons are shown. The staged tier shows ✕ instead of its name.
/// </summary>
public partial class PartRow : HBoxContainer {

    [Signal]
    public delegate void StageRequestedEventHandler(CollectedPart cp);

    [Signal]
    public delegate void UnstageRequestedEventHandler(CollectedPart cp);

    // One entry per variant: the button and its named press handler (for clean -=).
    private readonly record struct TierEntry(CollectedPart Cp, Button Btn, Action Handler);
    private readonly List<TierEntry> _tierEntries = [];

    public PartRow(List<CollectedPart> variants, PartLevel currentLevel, int maxLevel, CollectedPart stagedCp) {
        SizeFlagsHorizontal = SizeFlags.Fill;
        MouseFilter = MouseFilterEnum.Stop;

        // ── Rarity colour bar (colour of the highest-rarity owned variant) ───
        AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(6, 0),
            Color = PartLevelHelper.GetColour(variants[0].Rarity),
            SizeFlagsVertical = SizeFlags.Fill,
        });

        // ── Part icon ────────────────────────────────────────────────────────
        var part = variants[0].Part;
        if (part?.IconImage != null) {
            AddChild(new TextureRect {
                Texture = part.IconImage,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            });
        }

        // ── Part name ────────────────────────────────────────────────────────
        AddChild(new Label {
            Text = part?.Name ?? "Unknown",
            SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
            VerticalAlignment = VerticalAlignment.Center,
        });

        // ── Per-tier buttons ─────────────────────────────────────────────────
        // One button per applicable variant, ordered best-first (variants already sorted desc).
        // Inapplicable variants are hidden entirely.
        foreach (var cp in variants) {
            int targetLevel = Math.Min((int)cp.Rarity, maxLevel);
            bool applicable = targetLevel == 0 ? currentLevel > 0 : targetLevel > (int)currentLevel;

            bool isStaged = stagedCp != null && ReferenceEquals(stagedCp, cp);

            // Hidden if not applicable and not staged (staged always visible so ✕ is reachable).
            if (!applicable && !isStaged) continue;

            var btn = new Button {
                CustomMinimumSize = new Vector2(80, 0),
            };
            btn.AddThemeColorOverride("font_color", PartLevelHelper.GetColour(cp.Rarity));

            Action handler;
            if (isStaged) {
                btn.Text = "✕";
                handler = () => EmitSignal(SignalName.UnstageRequested, cp);
            } else {
                btn.Text = PartLevelHelper.GetDisplayName(cp.Rarity);
                handler = () => EmitSignal(SignalName.StageRequested, cp);
            }
            btn.Pressed += handler;

            AddChild(btn);
            _tierEntries.Add(new TierEntry(cp, btn, handler));
        }
    }

    // ─── Called by CarModifyScreen ────────────────────────────────────────────

    /// <summary>Switches the staged variant's button to ✕ / UnstageRequested.</summary>
    public void MarkStaged(CollectedPart cp) {
        var entry = _tierEntries.FirstOrDefault(e => ReferenceEquals(e.Cp, cp));
        if (entry.Btn == null) return;

        entry.Btn.Pressed -= entry.Handler;
        Action newHandler = () => EmitSignal(SignalName.UnstageRequested, cp);
        entry.Btn.Text = "✕";
        entry.Btn.Pressed += newHandler;

        // Update stored handler so MarkUnstaged can remove it cleanly.
        int idx = _tierEntries.IndexOf(entry);
        _tierEntries[idx] = entry with { Handler = newHandler };
    }

    /// <summary>Restores the previously staged variant's button to its rarity name.</summary>
    public void MarkUnstaged(CollectedPart cp) {
        var entry = _tierEntries.FirstOrDefault(e => ReferenceEquals(e.Cp, cp));
        if (entry.Btn == null) return;

        entry.Btn.Pressed -= entry.Handler;
        Action newHandler = () => EmitSignal(SignalName.StageRequested, cp);
        entry.Btn.Text = PartLevelHelper.GetDisplayName(cp.Rarity);
        entry.Btn.Pressed += newHandler;

        int idx = _tierEntries.IndexOf(entry);
        _tierEntries[idx] = entry with { Handler = newHandler };
    }
}
