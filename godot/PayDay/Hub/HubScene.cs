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

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        // Spawn the live car mesh at the Car StaticBody3D's position
        if (state.CarDetails != null) {
            var carDisplay = new CarDisplayNode();
            carDisplay.Initialise(state.CarDetails);
            // Match the transform of StaticBody3D2 in HubScene.tscn
            carDisplay.Position = new Vector3(3.1767545f, 0f, 0f);
            AddChild(carDisplay);
        }

        foreach (var item in GetAllHubItems()) {
            item.InputEvent += (camera, @event, eventPosition, normal, shapeIdx) => {
                if (!_hubInteractionEnabled) {
                    return;
                }

                if (!@event.IsAction("select_world_object") || @event.IsReleased()) {
                    return;
                }

                var type = GetTypeFromNode(item);
                if (type == null) {
                    return;
                }

                switch (type) {
                    case HubItemType.Car:
                        OpenCarModifyScreen();
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
                        // Decorative items: no action beyond showing their rarity glow
                }
            };

            UpdateItemRarity(item, state);
        }
    }

    /// <summary>
    /// Call after loading the hub when parts were collected in the previous run.
    /// Shows the PartApplyScreen overlay; hub items are non-interactive until it closes.
    /// Safe to call with an empty list — it becomes a no-op.
    /// </summary>
    public void SetEveningParts(List<CollectedPart> parts) {
        if (parts == null || parts.Count == 0)
            return;

        _hubInteractionEnabled = false;

        var applyScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(PartApplyScreen))).Instantiate<PartApplyScreen>();
        applyScreen.Closed += () => {
            RemoveChild(applyScreen);
            applyScreen.QueueFree();
            _hubInteractionEnabled = true;
        };
        AddChild(applyScreen);
        applyScreen.SetParts(parts);
    }

    private void OpenCarModifyScreen() {
        _hubInteractionEnabled = false;

        var modifyScreen = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(CarModifyScreen))).Instantiate<CarModifyScreen>();
        modifyScreen.Closed += () => {
            RemoveChild(modifyScreen);
            modifyScreen.QueueFree();
            _hubInteractionEnabled = true;
        };
        modifyScreen.StartRacing += () => {
            RemoveChild(modifyScreen);
            modifyScreen.QueueFree();
            _hubInteractionEnabled = true;
            EmitSignal(SignalName.StartRacing);
        };
        AddChild(modifyScreen);
    }

    private static void UpdateItemRarity(StaticBody3D item, PayDayGlobalState state) {
        if (state.PartInventory.Count == 0) return;

        // The house glows with the best rarity part collected so far
        var best = PartRarity.Poor;
        foreach (var part in state.PartInventory) {
            if (part.Rarity > best) best = part.Rarity;
        }

        // All decorative furniture reflects the item's rarity
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
        if (node is StaticBody3D body) {
            if (Enum.TryParse(body.GetMeta("HubItemType").ToString(), out HubItemType type)) {
                return type;
            }
        }
        return null;
    }
}
