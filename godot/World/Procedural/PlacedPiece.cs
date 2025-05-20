using Godot;

namespace murph9.RallyGame2.godot.World.Procedural;

public record PlacedPiece(string Name, Transform3D FinalTransform, WorldPieceDir Dir);
