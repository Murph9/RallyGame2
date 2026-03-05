using Godot;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.Extensions;
using System;
using System.Collections.Generic;

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
/// </summary>
public partial class HubScene : Node3D {

    [Signal]
    public delegate void StartRacingEventHandler();
    [Signal]
    public delegate void OpenLoanPaperworkEventHandler();
    [Signal]
    public delegate void OpenPhoneEventHandler();

    private bool _hubInteractionEnabled = true;

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

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        _tooltipPanel = GetNode<PanelContainer>("TooltipLayer/TooltipPanel");
        _tooltipLabel = GetNode<Label>("TooltipLayer/TooltipPanel/TooltipLabel");

        // Spawn the live car mesh at the Car StaticBody3D's position
        if (state.CarDetails != null) {
            var carDisplay = new CarDisplayNode();
            carDisplay.Initialise(state.CarDetails);
            carDisplay.Position = new Vector3(3.1767545f, 0f, 0f);
            AddChild(carDisplay);
        }

        foreach (var item in GetAllHubItems()) {
            var type = GetTypeFromNode(item);

            item.InputEvent += (camera, @event, eventPosition, normal, shapeIdx) => {
                if (!_hubInteractionEnabled) return;
                if (!@event.IsAction("select_world_object") || @event.IsReleased()) return;
                if (type == null) return;

                switch (type) {
                    case HubItemType.Car:
                        OpenCarModifyScreen(eventPosition);
                        break;
                    case HubItemType.Race:
                        EmitSignal(SignalName.StartRacing);
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
            if (type.HasValue && GetTooltipForType(type.Value) != null) {
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

            UpdateItemRarity(item, state);
        }

        SetupCamera();
    }

    public override void _Process(double delta) {
        // Keep the tooltip panel near the cursor each frame
        if (_tooltipPanel != null && _tooltipPanel.Visible) {
            _tooltipPanel.Position = GetViewport().GetMousePosition() + TooltipOffset;
        }
    }

    private void ShowTooltip(HubItemType type) {
        var text = GetTooltipForType(type);
        if (text == null) return;
        _tooltipLabel.Text = text;
        _tooltipPanel.Visible = true;
    }

    private void HideTooltip() {
        if (_tooltipPanel != null)
            _tooltipPanel.Visible = false;
    }

    private static string GetTooltipForType(HubItemType type) => type switch {
        HubItemType.Car => "Modify your car",
        HubItemType.Race => "Start a racing run",
        HubItemType.LoanPaperwork => "Pay down your loan",
        HubItemType.Phone => "Call a friend",
        HubItemType.Lounge => "Look at stats",
        _ => null, // decorative — no tooltip
    };

    private void ApplyHoverHighlight(StaticBody3D item) {
        var id = item.GetInstanceId();
        if (_savedMaterials.ContainsKey(id)) return; // already highlighted

        var saved = new List<(MeshInstance3D, int, Material)>();

        foreach (var mesh in item.GetAllChildrenOfType<MeshInstance3D>()) {
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

        _savedMaterials[id] = saved;
    }

    private void RemoveHoverHighlight(StaticBody3D item) {
        var id = item.GetInstanceId();
        if (!_savedMaterials.TryGetValue(id, out var saved)) return;

        foreach (var (mesh, surface, original) in saved) {
            mesh.SetSurfaceOverrideMaterial(surface, original);
        }

        _savedMaterials.Remove(id);
    }

    /// <summary>
    /// Call after loading the hub when parts were collected in the previous run.
    /// Shows the PartApplyScreen overlay; hub items are non-interactive until it closes.
    /// </summary>
    public void SetEveningParts(List<CollectedPart> parts) {
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

    private void OpenCarModifyScreen(Vector3 eventPosition) {
        _hubInteractionEnabled = false;
        HideTooltip();

        var modifyScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(CarModifyScreen))).Instantiate<CarModifyScreen>();
        modifyScreen.Center = eventPosition;
        modifyScreen.Closed += () => {
            RemoveChild(modifyScreen);
            modifyScreen.QueueFree();
            _hubInteractionEnabled = true;
            RefreshAllRarityGlows();
            SetupCamera();
        };
        AddChild(modifyScreen);
    }

    private void SetupCamera() {
        GetViewport().GetCamera3D().LookAt(new Vector3());
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

        var best = PartRarity.Poor;
        foreach (var part in state.PartInventory) {
            if (part.Rarity > best) best = part.Rarity;
        }

        var hubItem = GetTypeFromNode(item);
        if (hubItem is HubItemType.TV or HubItemType.Fridge
                      or HubItemType.Lounge or HubItemType.Lamp) {
            var meshes = item.GetAllChildrenOfType<MeshInstance3D>();
            foreach (var mesh in meshes) {
                PartRarityHelper.ApplyRarityMaterial(mesh, best);
            }
        }
    }

    private IEnumerable<StaticBody3D> GetAllHubItems() {
        foreach (var child in GetChildren()) {
            if (GetTypeFromNode(child).HasValue) {
                yield return child as StaticBody3D;
            }
        }
    }

    private static HubItemType? GetTypeFromNode(Node node) {
        if (node is StaticBody3D body && body.HasMeta("HubItemType")) {
            if (Enum.TryParse(body.GetMeta("HubItemType").ToString(), out HubItemType type)) {
                return type;
            }
        }
        return null;
    }
}
