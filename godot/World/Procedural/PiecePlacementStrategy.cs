using Godot;

namespace murph9.RallyGame2.godot.World.Procedural;

public partial class PiecePlacementStrategy : Node3D {
    public enum Type {
        Camera
    }

    private readonly float _generationRange;
    private readonly Type _type;

    public Transform3D NextTransform { get; set; }

    [Signal]
    public delegate void NeedPieceEventHandler();

    public PiecePlacementStrategy(Type type, float generationRange) {
        _type = type;
        _generationRange = generationRange;
    }

    public override void _PhysicsProcess(double delta) {
        if (_type == Type.Camera) {
            var pos = GetViewport().GetCamera3D().Position;

            if (pos.DistanceTo(NextTransform.Origin) < _generationRange) {
                EmitSignal(SignalName.NeedPiece);
            }
        }
    }
}
