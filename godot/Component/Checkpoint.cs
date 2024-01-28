using Godot;

namespace murph9.RallyGame2.godot.Component;

public partial class Checkpoint : Area3D {

    public static Checkpoint AsBox(Vector3 position, Vector3 size, Color color) => new (position,
        new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = size
            },
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = color,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha
            }
        },
        new CollisionShape3D() {
            Shape = new BoxShape3D() {
                Size = size
            }
        }
    );

    [Signal]
    public delegate void ThingEnteredEventHandler(Node3D node);

    private Checkpoint(Vector3 position, MeshInstance3D visual, CollisionShape3D collision) {
        Position = position;
        AddChild(visual);
        AddChild(collision);

        Monitoring = true;
        BodyEntered += OnCheckpointBodyEntered;
    }

    private void OnCheckpointBodyEntered(Node3D node) {
        EmitSignal(SignalName.ThingEntered, node);
    }
}
