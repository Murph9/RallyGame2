using Godot;

namespace murph9.RallyGame2.godot.World;

public record InfiniteCheckpoint(string Name, Transform3D StartTransform, Transform3D FinalTransform, Vector3 LeftOffset);
