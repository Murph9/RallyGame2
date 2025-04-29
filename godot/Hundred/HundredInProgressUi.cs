using Godot;
using murph9.RallyGame2.godot.Utilities;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredInProgressUi : VBoxContainer {

    public List<HundredInProgressItem> Items { get; init; } = [];

    public HundredInProgressItem Add() {
        var item = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(HundredInProgressItem))).Instantiate<HundredInProgressItem>();
        Items.Add(item);
        AddChild(item);
        return item;
    }

    public void Remove(HundredInProgressItem item) {
        if (Items.Remove(item)) {
            RemoveChild(item);
        }
    }
}
