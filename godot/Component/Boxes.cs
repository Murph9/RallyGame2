using Godot;

namespace murph9.RallyGame2.godot.Component;


public partial class Boxes : Node3D {

    // a debug class for testing where some boxes exist

    public override void _Ready() {
        var boxes = new Vector3[] {
            new (0.9975586f, -0.99999f, -19), new (38.00244f, 1.99999f, 38),
            new (1, -0.99999f, -61),          new (38, 1.99999f, 42.00244f),
        };

        for (var i = 0 ; i < boxes.Length; i+=2) {
            var boxMesh = new MeshInstance3D() {
                Mesh = new BoxMesh() {
                    Size = boxes[i+1]
                },
                Position = boxes[i] + boxes[i+1]/2,
                MaterialOverride = new StandardMaterial3D() {
                    AlbedoColor = new Color(0, 1, 1, 0.2f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha
                }
            };
            AddChild(boxMesh);
        }
    }
}