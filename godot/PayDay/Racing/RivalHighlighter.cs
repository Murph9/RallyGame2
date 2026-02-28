using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Attaches directly to a rival car and provides multiple in-world visual cues:
///  1. Rarity-coloured emission glow applied to all car body meshes.
///  2. A pulsing 3D billboard label ("★ RIVAL ★") floating above the car.
///  3. A rarity-coloured overhead ring drawn with ImmediateMesh lines.
/// Call Init() before adding to the scene tree.
/// </summary>
public partial class RivalHighlighter : Node3D {

    private const float LABEL_HEIGHT = 2.8f;
    private const float RING_HEIGHT = 2.2f;
    private const float RING_RADIUS = 1.8f;
    private const float PULSE_SPEED = 2.0f;

    private Car _rival;
    private PartRarity _rarity;
    private Label3D _label;
    private MeshInstance3D _ringMesh;
    private float _pulseTimer;

    public void Init(Car rival, PartRarity rarity) {
        _rival = rival;
        _rarity = rarity;
    }

    public override void _Ready() {
        var color = PartRarityHelper.GetColour(_rarity);
        var rarityName = PartRarityHelper.GetDisplayName(_rarity);

        // 1. Apply rarity emission glow to all car body mesh surfaces.
        //    We duplicate each StandardMaterial3D surface and enable emission.
        float emission = EmissionForRarity(_rarity);
        foreach (var mesh in _rival.RigidBody.GetAllChildrenOfType<MeshInstance3D>()) {
            if (mesh.Mesh == null || mesh.Mesh.GetSurfaceCount() == 0) continue;
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++) {
                var activeMat = mesh.GetActiveMaterial(i);
                if (activeMat is not StandardMaterial3D baseMat) continue;
                var newMat = (StandardMaterial3D)baseMat.Duplicate();
                newMat.EmissionEnabled = true;
                newMat.Emission = color;
                newMat.EmissionEnergyMultiplier = emission;
                mesh.SetSurfaceOverrideMaterial(i, newMat);
            }
        }

        // 2. Floating billboard label attached to the rival's RigidBody.
        _label = new Label3D {
            Text = $"★ RIVAL ★\n{rarityName.ToUpper()}",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            PixelSize = 0.004f,
            FontSize = 64,
            OutlineSize = 8,
            Modulate = color,
            OutlineModulate = new Color(0f, 0f, 0f, 1f),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _rival.RigidBody.AddChild(_label);
        _label.Position = Vector3.Up * LABEL_HEIGHT;

        // 3. Overhead ring (ImmediateMesh circle) drawn in the rarity colour.
        _ringMesh = BuildRingMesh(color);
        _rival.RigidBody.AddChild(_ringMesh);
        _ringMesh.Position = Vector3.Up * RING_HEIGHT;
    }

    public override void _Process(double delta) {
        if (!IsInstanceValid(_label)) return;

        // Pulse the label and ring opacity
        _pulseTimer += (float)delta * PULSE_SPEED;
        float alpha = 0.55f + 0.45f * Mathf.Sin(_pulseTimer);

        var c = _label.Modulate;
        _label.Modulate = new Color(c.R, c.G, c.B, alpha);

        if (IsInstanceValid(_ringMesh)) {
            var rc = _ringMesh.GetActiveMaterial(0);
            if (rc is StandardMaterial3D sm)
                sm.AlbedoColor = new Color(sm.AlbedoColor.R, sm.AlbedoColor.G, sm.AlbedoColor.B, alpha);
        }
    }

    public override void _ExitTree() {
        if (IsInstanceValid(_label)) _label.QueueFree();
        if (IsInstanceValid(_ringMesh)) _ringMesh.QueueFree();
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static float EmissionForRarity(PartRarity rarity) => rarity switch {
        PartRarity.Poor => 0.20f,
        PartRarity.Common => 0.35f,
        PartRarity.Uncommon => 0.55f,
        PartRarity.Rare => 0.80f,
        PartRarity.Epic => 1.20f,
        PartRarity.Legendary => 2.00f,
        _ => 0.20f,
    };

    private static MeshInstance3D BuildRingMesh(Color color) {
        const int SEGMENTS = 32;
        var immesh = new ImmediateMesh();
        immesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        for (int i = 0; i <= SEGMENTS; i++) {
            float angle = Mathf.Tau * i / SEGMENTS;
            immesh.SurfaceAddVertex(new Vector3(Mathf.Cos(angle) * RING_RADIUS, 0, Mathf.Sin(angle) * RING_RADIUS));
        }
        immesh.SurfaceEnd();

        var mat = new StandardMaterial3D {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            NoDepthTest = true,
        };
        immesh.SurfaceSetMaterial(0, mat);

        return new MeshInstance3D { Mesh = immesh };
    }
}
