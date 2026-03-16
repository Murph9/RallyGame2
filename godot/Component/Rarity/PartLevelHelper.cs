using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using System;

namespace murph9.RallyGame2.godot.Component.Rarity;

public static class PartLevelHelper {
    private static readonly Color[] Colours = [
        new Color("#b0b0b0"), // Common
        new Color("#22c55e"), // Uncommon
        new Color("#3b82f6"), // Rare
        new Color("#a855f7"), // Epic
        new Color("#f97316"), // Legendary
    ];

    private static readonly string[] Names = [
        "Common", "Uncommon", "Rare", "Epic", "Legendary"
    ];

    private static readonly string[] Exclamations = [
        "ok i guess", "not bad!", "NICE!", "HOLY MOLY!", "LEGENDARY!!!"
    ];

    public static Color GetColour(PartLevel rarity) => Colours[(int)rarity];
    public static string GetDisplayName(PartLevel rarity) => Names[(int)rarity];
    public static string GetExclamation(PartLevel rarity) => Exclamations[(int)rarity];

    /// <summary>
    /// Weighted rarity roll. Later days push the distribution toward higher tiers.
    /// Day 1: ~90% Common. Day 10+: meaningful Rare/Epic chance.
    /// </summary>
    public static PartLevel RollRarity(int dayNumber) {
        float day = Math.Min(dayNumber, 20);

        // weights: [Poor, Common, Uncommon, Rare, Epic, Legendary]
        float legendary = day * 0.5f;      //  0 – 10 %
        float epic = day * 2.0f;           //  0 – 40 %
        float rare = day * 3.0f;           //  0 – 60 %
        float uncommon = 20f;
        float common = 30f;

        float[] weights = [common, uncommon, rare, epic, legendary];

        float total = 0f;
        foreach (var w in weights) total += w;

        float roll = (float)GD.RandRange(0.0, total);
        float cumulative = 0;
        for (int i = 0; i < weights.Length; i++) {
            cumulative += weights[i];
            if (roll < cumulative)
                return (PartLevel)i;
        }

        return PartLevel.Common;
    }

    /// <summary>Returns an HTML hex colour string for use in BBCode.</summary>
    public static string GetBBCodeColour(PartLevel rarity) {
        var c = GetColour(rarity);
        return $"#{(int)(c.R * 255):X2}{(int)(c.G * 255):X2}{(int)(c.B * 255):X2}";
    }


    /// <summary>Apply the rarity glow shader as a material override on the given mesh.</summary>
    public static void ApplyRarityMaterial(MeshInstance3D mesh, PartLevel rarity) {
        var shader = GD.Load<Shader>("res://RarityGlow.gdshader");
        var mat = new ShaderMaterial {
            Shader = shader
        };
        mat.SetShaderParameter("rarity_color", GetColour(rarity));
        mat.SetShaderParameter("glow_strength", GlowStrengthForRarity(rarity));
        mesh.MaterialOverride = mat;
    }

    private static float GlowStrengthForRarity(PartLevel rarity) => rarity switch {
        PartLevel.Common => 0.05f,
        PartLevel.Uncommon => 0.15f,
        PartLevel.Rare => 0.30f,
        PartLevel.Epic => 0.50f,
        PartLevel.Legendary => 0.80f,
        _ => 0.00f,
    };

    public static float EmissionForRarity(PartLevel rarity) => rarity switch {
        PartLevel.Common => 0.35f,
        PartLevel.Uncommon => 0.55f,
        PartLevel.Rare => 0.80f,
        PartLevel.Epic => 1.20f,
        PartLevel.Legendary => 2.00f,
        _ => 0.20f,
    };
}
