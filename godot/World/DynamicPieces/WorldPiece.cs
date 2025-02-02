using Godot;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public record WorldPiece(string Name, Node3D Model, WorldPieceDir[] Directions);
