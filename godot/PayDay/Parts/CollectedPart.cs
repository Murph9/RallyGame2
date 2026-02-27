using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;

namespace murph9.RallyGame2.godot.PayDay.Parts;

/// <summary>
/// A part that has been dropped as loot (with a rarity tier).
/// Extends RefCounted so it can be passed through Godot signals as a Variant.
/// </summary>
public partial class CollectedPart : RefCounted {
    public Part Part { get; set; }
    public PartRarity Rarity { get; set; }

    public CollectedPart() { }

    public CollectedPart(Part part, PartRarity rarity) {
        Part = part;
        Rarity = rarity;
    }

    /// <summary>Apply the rarity glow shader as a material override on the given mesh.</summary>
    public static void ApplyRarityMaterial(MeshInstance3D mesh, PartRarity rarity) {
        var shader = GD.Load<Shader>("res://RarityGlow.gdshader");
        var mat = new ShaderMaterial {
            Shader = shader
        };
        mat.SetShaderParameter("rarity_color", PartRarityHelper.GetColour(rarity));
        mat.SetShaderParameter("glow_strength", GlowStrengthForRarity(rarity));
        mesh.MaterialOverride = mat;
    }

    private static float GlowStrengthForRarity(PartRarity rarity) => rarity switch {
        PartRarity.Poor => 0.00f,
        PartRarity.Common => 0.05f,
        PartRarity.Uncommon => 0.15f,
        PartRarity.Rare => 0.30f,
        PartRarity.Epic => 0.50f,
        PartRarity.Legendary => 0.80f,
        _ => 0.00f,
    };
}
