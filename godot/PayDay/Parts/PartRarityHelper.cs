using Godot;
using System;

namespace murph9.RallyGame2.godot.PayDay.Parts;

public static class PartRarityHelper {
    private static readonly Color[] Colours = [
        new Color("#808080"), // Poor
        new Color("#ffffff"), // Common
        new Color("#22c55e"), // Uncommon
        new Color("#3b82f6"), // Rare
        new Color("#a855f7"), // Epic
        new Color("#f97316"), // Legendary
    ];

    private static readonly string[] Names = [
        "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary"
    ];

    private static readonly string[] Exclamations = [
        "oh...", "ok i guess", "not bad!", "NICE!", "HOLY MOLY!", "LEGENDARY!!!"
    ];

    public static Color GetColour(PartRarity rarity) => Colours[(int)rarity];
    public static string GetDisplayName(PartRarity rarity) => Names[(int)rarity];
    public static string GetExclamation(PartRarity rarity) => Exclamations[(int)rarity];

    /// <summary>
    /// Weighted rarity roll. Later days push the distribution toward higher tiers.
    /// Day 1: ~90% Poor/Common. Day 10+: meaningful Rare/Epic chance.
    /// </summary>
    public static PartRarity RollRarity(int dayNumber) {
        float day = Math.Min(dayNumber, 20);

        // weights: [Poor, Common, Uncommon, Rare, Epic, Legendary]
        float legendary = day * 0.5f;           //  0 – 10 %
        float epic = day * 2.0f;           //  0 – 40 %
        float rare = day * 3.0f;           //  0 – 60 %
        float uncommon = 20f;
        float common = 30f;
        float poor = Math.Max(5f, 60f - day * 4f);

        float[] weights = [poor, common, uncommon, rare, epic, legendary];

        float total = 0f;
        foreach (var w in weights) total += w;

        float roll = (float)GD.RandRange(0.0, total);
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++) {
            cumulative += weights[i];
            if (roll < cumulative) return (PartRarity)i;
        }

        return PartRarity.Common;
    }

    /// <summary>Returns an HTML hex colour string for use in BBCode.</summary>
    public static string GetBBCodeColour(PartRarity rarity) {
        var c = GetColour(rarity);
        return $"#{(int)(c.R * 255):X2}{(int)(c.G * 255):X2}{(int)(c.B * 255):X2}";
    }
}
