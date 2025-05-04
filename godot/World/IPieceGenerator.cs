using Godot;
using murph9.RallyGame2.godot.World.DynamicPieces;

namespace murph9.RallyGame2.godot.World;

public interface IPieceGenerator {
    void UpdatePieceType(WorldType type);
    (WorldPiece, int) Next(Transform3D currentTransform, RandomNumberGenerator rand);
}
