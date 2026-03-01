using Godot;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay.Hub;

/// <summary>
/// The 3D house hub scene that serves as the menu between runs.
/// Each HouseItem child emits Clicked; this node routes those to game-level signals.
/// Decorative furniture glows with the best rarity part in the player's inventory.
/// </summary>
public partial class HubScene : Node3D {

    [Signal]
    public delegate void StartRacingEventHandler();
    [Signal]
    public delegate void OpenLoanPaperworkEventHandler();
    [Signal]
    public delegate void OpenPhoneEventHandler();

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        foreach (var item in GetAllHubItems()) {
            item.InputEvent += (camera, @event, eventPosition, normal, shapeIdx) => {
                if (!@event.IsAction("select_world_object") || @event.IsReleased()) {
                    return;
                }

                var type = GetTypeFromNode(item);
                if (type == null) {
                    return;
                }

                switch (type) {
                    case HubItemType.Car:
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
