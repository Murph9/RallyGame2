using Godot;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public partial class CameraPiecePlacementStrategy : Node3D {
    private readonly float _generationRange;

    public Transform3D NextTransform { get; set; }

    [Signal]
    public delegate void NeedPieceEventHandler();

    public CameraPiecePlacementStrategy(float generationRange) {
        _generationRange = generationRange;
    }

    public override void _PhysicsProcess(double delta) {
        var pos = GetViewport().GetCamera3D().Position;

        if (pos.DistanceTo(NextTransform.Origin) < _generationRange) {
            EmitSignal(SignalName.NeedPiece);
        }
    }
}
