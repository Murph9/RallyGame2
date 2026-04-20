using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.PayDay.Hub;

/// <summary>
/// The 3D house hub scene that serves as the menu between runs.
/// Each HouseItem child emits Clicked; this node routes those to game-level signals.
/// Interactive items highlight on hover and show a tooltip label near the cursor.
/// Decorative furniture glows with the best rarity part in the player's inventory.
/// Clicking the Car item opens CarModifyScreen where parts can be applied and racing started.
/// Clicking the Race item starts a racing run immediately.
/// When parts were collected during the previous run, SetEveningParts() shows the
/// PartApplyScreen as an overlay before hub interaction is enabled.
/// The Race item is locked until the player has reviewed their loan (LoanPaperwork).
/// </summary>
public partial class HubScene : Node3D {

    [Signal]
    public delegate void StartRacingEventHandler();
    [Signal]
    public delegate void OpenLoanPaperworkEventHandler();
    [Signal]
    public delegate void OpenPhoneEventHandler();

    private bool _hubInteractionEnabled = true;

    // Loan must be reviewed before the player can race again
    private bool _loanReviewed = false;
    private float _lastRunMoney = 0f;

    // Reference to the LoanPaperwork body so we can apply/remove urgency glow
    private StaticBody3D _loanPaperworkBody;

    // Tooltip nodes (resolved in _Ready)
    private PanelContainer _tooltipPanel;
    private Label _tooltipLabel;

    // Per-item: original material overrides saved so hover tint can be reverted.
    // Key: StaticBody3D instance id, Value: list of (MeshInstance3D, surface index, original Material)
    private readonly Dictionary<ulong, List<(MeshInstance3D mesh, int surface, Material original)>> _savedMaterials = [];

    // Hover colour applied to interactive items
    private static readonly Color HoverColour = new(1f, 1f, 1f, 1f);
    private const float HoverEmission = 0.4f;
    private static readonly Vector2 TooltipOffset = new(14f, 14f);

    // Urgency glow colour for the loan paperwork when not yet reviewed
    private static readonly Color LoanUrgentColour = new(1f, 0.3f, 0.1f, 1f);
    private const float LoanUrgentEmission = 0.6f;

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        _tooltipPanel = GetNode<PanelContainer>("TooltipLayer/TooltipPanel");
        _tooltipLabel = GetNode<Label>("TooltipLayer/TooltipPanel/TooltipLabel");

        var packedScene = GD.Load<PackedScene>("res://assets/house.blend");
        var scene = packedScene.Instantiate<Node3D>();
        AddChild(scene);

        var addedHubItems = new List<HubItemType>();

        foreach (var obj in scene.GetChildren()) {
            if (Enum.TryParse(obj.Name, true, out HubItemType type)) {
                var staticBody = obj.GetAllChildrenOfType<StaticBody3D>().First();
                staticBody.SetMeta(nameof(HubItemType), type.ToString());
                addedHubItems.Add(type);
            }
        }
        // Spawn the live car mesh at the Car StaticBody3D's position
        if (state.CarDetails != null) {
            var carDisplay = new CarDisplayNode();
            carDisplay.Initialise(state.CarDetails);
            AddChild(carDisplay);
        }

        // force main camera to be used
        UseCamera("main");

        foreach (var item in GetAllHubItems()) {
            UpdateItemRarity(item, state);

            var type = GetTypeFromNode(item);
            if (type is null)
                continue;

            // Track the LoanPaperwork body so we can control its urgency glow
            if (type == HubItemType.LoanPaperwork) {
                _loanPaperworkBody = item;
            }

            item.InputEvent += (camera, @event, eventPosition, normal, shapeIdx) => {
                if (!_hubInteractionEnabled)
                    return;
                if (!@event.IsAction("select_world_object") || @event.IsReleased())
                    return;

                switch (type) {
                    case HubItemType.Car:
                        OpenCarModifyScreen(eventPosition);
                        break;
                    case HubItemType.Race:
                        if (!_loanReviewed) {
                            // Flash the tooltip warning instead of starting race
                            ShowLockedRaceTooltip();
                        } else {
                            EmitSignal(SignalName.StartRacing);
                        }
                        break;
                    case HubItemType.LoanPaperwork:
                        EmitSignal(SignalName.OpenLoanPaperwork);
                        break;
                    case HubItemType.Phone:
                        EmitSignal(SignalName.OpenPhone);
                        break;
                }
            };

            // Only interactive items get hover effects
            if (type.HasValue) {
                item.MouseEntered += () => {
                    if (!_hubInteractionEnabled) return;
                    ApplyHoverHighlight(item);
                    ShowTooltip(type.Value);
                };
                item.MouseExited += () => {
                    RemoveHoverHighlight(item);
                    HideTooltip();
                };
            }
        }

        // Apply urgency glow to loan paperwork at start (before loan is reviewed)
        UpdateLoanUrgencyGlow();
    }

    public override void _Process(double delta) {
        // Keep the tooltip panel near the cursor each frame
        if (_tooltipPanel != null && _tooltipPanel.Visible) {
            _tooltipPanel.Position = GetViewport().GetMousePosition() + TooltipOffset;
        }
    }

    private void ShowTooltip(HubItemType type) {
        var text = GetTooltipForType(type);
        if (text == null)
            return;
        _tooltipLabel.Text = text;
        _tooltipPanel.Visible = true;
    }

    private void ShowLockedRaceTooltip() {
        _tooltipLabel.Text = "Review your loan first! (click the paperwork)";
        _tooltipPanel.Visible = true;
    }

    private void HideTooltip() {
        if (_tooltipPanel != null)
            _tooltipPanel.Visible = false;
    }

    private string GetTooltipForType(HubItemType type) => type switch {
        HubItemType.Car => "Modify your car",
        HubItemType.Race => _loanReviewed ? "Start a racing run" : "[LOCKED] Review your loan first!",
        HubItemType.LoanPaperwork => _loanReviewed ? "Pay down your loan" : "! REVIEW YOUR LOAN (required before racing)",
        HubItemType.Phone => "Call a friend",
        HubItemType.Lounge => "Look at stats",
        _ => null, // decorative — no tooltip
    };

    private void ApplyHoverHighlight(StaticBody3D item) {
        var id = item.GetInstanceId();
        if (_savedMaterials.ContainsKey(id)) return; // already highlighted

        var saved = new List<(MeshInstance3D, int, Material)>();

        var children = item.GetAllChildrenOfType<MeshInstance3D>();

        if (children.Any()) {
            foreach (var mesh in children) {
                for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++) {
                    saved.Add((mesh, i, mesh.GetSurfaceOverrideMaterial(i)));

                    var hoverMat = new StandardMaterial3D {
                        AlbedoColor = HoverColour,
                        EmissionEnabled = true,
                        Emission = HoverColour,
                        EmissionEnergyMultiplier = HoverEmission
                    };
                    mesh.SetSurfaceOverrideMaterial(i, hoverMat);
                }
            }
        } else {
            // attempt to look up the tree
            var node = item.GetParent();
            while (node != null) {
                if (node is MeshInstance3D mesh) {
                    for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++) {
                        saved.Add((mesh, i, mesh.GetSurfaceOverrideMaterial(i)));

                        var hoverMat = new StandardMaterial3D {
                            AlbedoColor = HoverColour,
                            EmissionEnabled = true,
                            Emission = HoverColour,
                            EmissionEnergyMultiplier = HoverEmission
                        };
                        mesh.SetSurfaceOverrideMaterial(i, hoverMat);
                    }
                }

                node = node.GetParent();
            }
        }

        _savedMaterials[id] = saved;
    }

    private void RemoveHoverHighlight(StaticBody3D item) {
        var id = item.GetInstanceId();
        if (!_savedMaterials.TryGetValue(id, out var saved))
            return;

        foreach (var (mesh, surface, original) in saved) {
            mesh.SetSurfaceOverrideMaterial(surface, original);
        }

        _savedMaterials.Remove(id);
    }

    /// <summary>
    /// Call after loading the hub when returning from a run.
    /// If parts were collected, shows the PartApplyScreen overlay first.
    /// Marks the loan as not-yet-reviewed so the Race item is locked until
    /// the player visits LoanPaperwork.
    /// </summary>
    public void SetEveningParts(List<CollectedPart> parts, float runMoney = 0f) {
        _lastRunMoney = runMoney;
        _loanReviewed = false;
        UpdateLoanUrgencyGlow();

        if (parts == null || parts.Count == 0)
            return;

        _hubInteractionEnabled = false;
        HideTooltip();

        var applyScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(PartApplyScreen))).Instantiate<PartApplyScreen>();
        applyScreen.Closed += () => {
            RemoveChild(applyScreen);
            applyScreen.QueueFree();
            _hubInteractionEnabled = true;
            RefreshAllRarityGlows();
        };
        AddChild(applyScreen);
        applyScreen.SetParts(parts);
    }

    /// <summary>
    /// Called when the player has opened and dismissed the LoanPaperwork / DayEnd screen.
    /// Unlocks the Race item and removes the urgency glow.
    /// </summary>
    public void MarkLoanReviewed() {
        _loanReviewed = true;
        UpdateLoanUrgencyGlow();
    }

    /// <summary>The money earned in the last run, passed to DayEndScreen for context.</summary>
    public float LastRunMoney => _lastRunMoney;

    /// <summary>
    /// Applies or removes the orange urgency glow on the LoanPaperwork mesh
    /// depending on whether the loan has been reviewed yet.
    /// </summary>
    private void UpdateLoanUrgencyGlow() {
        if (_loanPaperworkBody == null)
            return;

        var meshes = _loanPaperworkBody.GetAllChildrenOfType<MeshInstance3D>();
        foreach (var mesh in meshes) {
            if (_loanReviewed) {
                // Restore default — clear override
                for (var i = 0; i < mesh.Mesh?.GetSurfaceCount(); i++) {
                    mesh.SetSurfaceOverrideMaterial(i, null);
                }
            } else {
                // Orange urgency pulse
                for (var i = 0; i < mesh.Mesh?.GetSurfaceCount(); i++) {
                    var urgentMat = new StandardMaterial3D {
                        AlbedoColor = LoanUrgentColour,
                        EmissionEnabled = true,
                        Emission = LoanUrgentColour,
                        EmissionEnergyMultiplier = LoanUrgentEmission
                    };
                    mesh.SetSurfaceOverrideMaterial(i, urgentMat);
                }
            }
        }
    }

    private void OpenCarModifyScreen(Vector3 eventPosition) {
        _hubInteractionEnabled = false;
        HideTooltip();
        UseCamera("car");

        var modifyScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(CarModifyScreen))).Instantiate<CarModifyScreen>();
        modifyScreen.Center = eventPosition;

        var layer = new CanvasLayer { Layer = 9 };
        modifyScreen.Closed += () => {
            RemoveChild(layer);
            layer.QueueFree();
            _hubInteractionEnabled = true;
            RefreshAllRarityGlows();
            UseCamera("main");
        };
        layer.AddChild(modifyScreen);
        AddChild(layer);
    }

    /// <summary>Recomputes the rarity-glow material on all decorative hub items.</summary>
    public void RefreshAllRarityGlows() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        foreach (var item in GetAllHubItems()) {
            UpdateItemRarity(item, state);
        }
    }

    private static void UpdateItemRarity(StaticBody3D item, PayDayGlobalState state) {
        if (state.PartInventory.Count == 0) return;

        var best = PartLevel.Common;
        foreach (var part in state.PartInventory) {
            if (part.Rarity > best) best = part.Rarity;
        }

        var hubItem = GetTypeFromNode(item);
        if (hubItem is HubItemType.TV or HubItemType.Fridge
                      or HubItemType.Lounge or HubItemType.Lamp) {
            var meshes = item.GetAllChildrenOfType<MeshInstance3D>();
            foreach (var mesh in meshes) {
                PartLevelHelper.ApplyRarityMaterial(mesh, best);
            }
        }
    }

    private IEnumerable<StaticBody3D> GetAllHubItems() {
        foreach (var child in this.GetAllChildrenOfType<StaticBody3D>()) {
            if (GetTypeFromNode(child).HasValue) {
                yield return child;
            }
        }
    }

    private static HubItemType? GetTypeFromNode(StaticBody3D body) {
        if (!body.HasMeta(nameof(HubItemType))) {
            return null;
        }

        if (Enum.TryParse(body.GetMeta(nameof(HubItemType)).ToString(), out HubItemType type)) {
            return type;
        }
        return null;
    }

    private void UseCamera(string name) {
        foreach (var obj in this.GetAllChildrenOfType<Camera3D>()) {
            if (obj.Name.ToString().Contains(name)) {
                obj.Current = true;
                return;
            }
        }
    }
}
