using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public record WorldPiece {
    public string Name { get; init; }
    public Node3D Model { get; init; }
    public WorldPieceDir[] Directions { get; init; }

    public WorldPiece(string name, Node3D model, Dictionary<Transform3D, IEnumerable<Transform3D>> directions) : this(name, model, directions, 1, 0) { }
    public WorldPiece(string name, Node3D model, Dictionary<Transform3D, IEnumerable<Transform3D>> directions, int segments, float curveAngle) {
        Name = name;
        Model = model;

        if (directions.Any(x => x.Value.Any())) {
            // use the given positions
            Directions = directions.Select(x => WorldPieceDir.FromTransform(x.Key, x.Value)).ToArray();
        } else {
            // generate using the given segments and curveAngle
            Directions = directions.Select(x => WorldPieceDir.FromTransform(x.Key, segments, curveAngle)).ToArray();
        }
    }
}
