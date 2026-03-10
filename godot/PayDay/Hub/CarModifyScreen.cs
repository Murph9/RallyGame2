using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.PayDay.Hub;

/// <summary>
/// Fullscreen overlay shown when the player clicks the car in the hub.
/// Left panel: grouped collapsible tree of collected parts, organised by subsystem.
///   Each part row is draggable and can be dropped onto the car in the centre to apply it.
///   A small "Apply" button on each row is kept as an accessible alternative.
/// Centre: the live 3D car view (camera switched to "car" by HubScene before this opens).
/// A transparent Control overlay covers the centre area and acts as the DnD drop target.
/// </summary>
public partial class CarModifyScreen : CenterContainer {

    [Signal]
    public delegate void ClosedEventHandler();

    // Drag-data key used to pass a CollectedPart between drag source and drop target.
    // Shared with PartDropZone via internal visibility.
    internal const string DRAG_KEY = "collected_part_index";

    public Vector3? Center { get; set; } = null;

    // Populated in _Ready, used by the drop target
    private PayDayGlobalState _state;

    // The tree container (left panel scroll)
    private VBoxContainer _treeContainer;

    public override void _Ready() {
        _state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        if (Center.HasValue) {
            GetViewport().GetCamera3D().LookAt(Center.Value);
        }

        _treeContainer = GetNode<VBoxContainer>("HBox/Panel/VBox/ScrollContainer/TreeContainer");

        // Replace the static DropZone placeholder with a live PartDropZone instance
        var staticDropZone = GetNode<Control>("HBox/DropZone");
        var dropZone = new PartDropZone(this);
        dropZone.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill;
        dropZone.SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill;
        staticDropZone.GetParent().AddChild(dropZone);
        staticDropZone.QueueFree();

        PopulateTree();
    }

    // ─── Tree population ──────────────────────────────────────────────────────

    private void PopulateTree() {
        // Clear existing rows
        foreach (var child in _treeContainer.GetChildren())
            child.QueueFree();

        if (_state.PartInventory.Count == 0) {
            var empty = new Label { Text = "No parts in inventory." };
            _treeContainer.AddChild(empty);
            return;
        }

        // Group inventory by which subsystem owns the part
        var groups = BuildGroups(_state);

        bool anyGroup = false;
        foreach (var (groupName, items) in groups) {
            if (items.Count == 0) continue;
            anyGroup = true;
            AddGroupSection(groupName, items);
        }

        if (!anyGroup) {
            var empty = new Label { Text = "No parts in inventory." };
            _treeContainer.AddChild(empty);
        }
    }

    /// <summary>
    /// Returns ordered (groupName, [CollectedPart]) pairs, matched by subsystem ownership.
    /// </summary>
    private static List<(string Name, List<CollectedPart> Parts)> BuildGroups(PayDayGlobalState state) {
        var enginePartNames = new HashSet<string>(state.CarDetails.Engine.GetAllPartsInTree().Select(p => p.Name));
        var susPartNames = new HashSet<string>(state.CarDetails.SuspensionDetails.GetAllPartsInTree().Select(p => p.Name));
        var tractionPartNames = new HashSet<string>(state.CarDetails.TractionDetails.GetAllPartsInTree().Select(p => p.Name));

        var engine = new List<CollectedPart>();
        var chassis = new List<CollectedPart>();
        var sus = new List<CollectedPart>();
        var traction = new List<CollectedPart>();

        foreach (var cp in state.PartInventory) {
            if (cp.Part == null) continue;
            if (enginePartNames.Contains(cp.Part.Name))
                engine.Add(cp);
            else if (susPartNames.Contains(cp.Part.Name))
                sus.Add(cp);
            else if (tractionPartNames.Contains(cp.Part.Name))
                traction.Add(cp);
            else
                chassis.Add(cp);  // CarDetails.Parts: Brakes, Transmission, Nitro, Aero, etc.
        }

        return [
            ("Engine",          engine),
            ("Chassis / Aero",  chassis),
            ("Suspension",      sus),
            ("Traction",        traction),
        ];
    }

    private void AddGroupSection(string groupName, List<CollectedPart> items) {
        // ── Category header (toggle button) ──
        var headerBtn = new Button {
            Text = $"▼  {groupName}  ({items.Count})",
            Flat = true,
            SizeFlagsHorizontal = SizeFlags.Fill,
            Alignment = HorizontalAlignment.Left,
        };
        headerBtn.AddThemeColorOverride("font_color", Colors.LightGray);
        _treeContainer.AddChild(headerBtn);

        // ── Child container (collapsible) ──
        var childBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill };
        _treeContainer.AddChild(childBox);

        // Toggle collapse on header click
        headerBtn.Pressed += () => {
            childBox.Visible = !childBox.Visible;
            headerBtn.Text = (childBox.Visible ? "▼  " : "▶  ") + groupName + $"  ({items.Count})";
        };

        // ── Part rows ──
        foreach (var cp in items) {
            var part = cp.Part;
            if (part == null) continue;

            var currentLevel = _state.CarDetails.LevelOfPart(part);
            var maxLevel = part.Levels.Length - 1;
            var isMax = currentLevel >= maxLevel;

            var row = new PartRow(cp, currentLevel, maxLevel, isMax);
            row.ApplyRequested += () => ApplyPart(cp);
            childBox.AddChild(row);
        }

        // Add a small separator under each group
        childBox.AddChild(new HSeparator());
    }

    // ─── Part application ────────────────────────────────────────────────────

    /// <summary>Called by PartDropZone when a part is dropped onto the car view.</summary>
    public void ApplyPartAtIndex(int inventoryIdx) {
        if (inventoryIdx < 0 || inventoryIdx >= _state.PartInventory.Count) return;
        ApplyPart(_state.PartInventory[inventoryIdx]);
    }

    private void ApplyPart(CollectedPart cp) {
        var nextLevel = _state.CarDetails.LevelOfPart(cp.Part) + 1;
        if (nextLevel > cp.Part.Levels.Length - 1) return;

        _state.CarDetails.ApplyPartChange(cp.Part, nextLevel);
        _state.RemoveCollectedPart(cp);
        PopulateTree();
    }

    // ─── Drag source (on each PartRow) ───────────────────────────────────────

    /// <summary>
    /// Called from a PartRow when the user starts a drag. Returns drag data dict.
    /// </summary>
    public Variant MakeDragDataForPart(CollectedPart cp, Vector2 atPosition) {
        var idx = _state.PartInventory.IndexOf(cp);
        if (idx < 0) return default;

        // Build a small drag preview label
        var preview = new Label {
            Text = $"{PartRarityHelper.GetDisplayName(cp.Rarity)} {cp.Part?.Name}",
            CustomMinimumSize = new Vector2(200, 30),
        };
        preview.AddThemeColorOverride("font_color", PartRarityHelper.GetColour(cp.Rarity));
        SetDragPreview(preview);

        var dict = new Godot.Collections.Dictionary { [DRAG_KEY] = idx };
        return dict;
    }

    // ─── Close ───────────────────────────────────────────────────────────────

    public void CloseButton_Pressed() => EmitSignal(SignalName.Closed);
}

// ─── Inner helper: one draggable part row ────────────────────────────────────

/// <summary>
/// A single row in the part tree. Shows rarity swatch, icon, name, level badge,
/// and an Apply button. The whole row is draggable via Godot's Control DnD API.
/// </summary>
public partial class PartRow : HBoxContainer {

    [Signal]
    public delegate void ApplyRequestedEventHandler();

    private readonly CollectedPart _cp;

    public PartRow(CollectedPart cp, int currentLevel, int maxLevel, bool isMax) {
        _cp = cp;

        SizeFlagsHorizontal = SizeFlags.Fill;
        MouseFilter = MouseFilterEnum.Stop;

        // Rarity colour bar (narrow)
        var swatch = new ColorRect {
            CustomMinimumSize = new Vector2(6, 0),
            Color = PartRarityHelper.GetColour(cp.Rarity),
            SizeFlagsVertical = SizeFlags.Fill,
        };
        AddChild(swatch);

        // Part icon
        if (cp.Part?.IconImage != null) {
            var icon = new TextureRect {
                Texture = cp.Part.IconImage,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.FitHeightProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            AddChild(icon);
        }

        // Part name + rarity text
        var nameLabel = new Label {
            Text = $"{cp.Part?.Name ?? "Unknown"}",
            SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        nameLabel.AddThemeColorOverride("font_color", isMax ? Colors.Gray : PartRarityHelper.GetColour(cp.Rarity));
        AddChild(nameLabel);

        // Level badge
        var levelLabel = new Label {
            Text = isMax ? "MAX" : $"Lv {currentLevel}→{currentLevel + 1}",
            CustomMinimumSize = new Vector2(70, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        levelLabel.AddThemeColorOverride("font_color", isMax ? Colors.Gray : Colors.White);
        AddChild(levelLabel);

        // Apply button (accessible alternative to drag-and-drop)
        var applyBtn = new Button {
            Text = isMax ? "Max" : "Apply",
            Disabled = isMax,
            CustomMinimumSize = new Vector2(60, 0),
        };
        if (!isMax) {
            applyBtn.Pressed += () => EmitSignal(SignalName.ApplyRequested);
        }
        AddChild(applyBtn);
    }

    public override Variant _GetDragData(Vector2 atPosition) {
        if (_cp.Part == null) return default;

        // Find the CarModifyScreen ancestor to delegate preview + data creation
        var screen = FindCarModifyScreen();
        if (screen == null) return default;

        return screen.MakeDragDataForPart(_cp, atPosition);
    }

    private CarModifyScreen FindCarModifyScreen() {
        Node node = GetParent();
        while (node != null) {
            if (node is CarModifyScreen s) return s;
            node = node.GetParent();
        }
        return null;
    }
}

// ─── Drop zone: transparent Control covering the centre car area ──────────────

/// <summary>
/// Transparent Control that fills the centre area of CarModifyScreen.
/// Accepts drag data from PartRow and applies the part to the car on drop.
/// </summary>
public partial class PartDropZone : Control {

    private readonly CarModifyScreen _screen;

    public PartDropZone(CarModifyScreen screen) {
        _screen = screen;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) {
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var dict = data.As<Godot.Collections.Dictionary>();
        return dict.ContainsKey(CarModifyScreen.DRAG_KEY);
    }

    public override void _DropData(Vector2 atPosition, Variant data) {
        var dict = data.As<Godot.Collections.Dictionary>();
        var idx = dict[CarModifyScreen.DRAG_KEY].AsInt32();
        _screen.ApplyPartAtIndex(idx);
    }
}
