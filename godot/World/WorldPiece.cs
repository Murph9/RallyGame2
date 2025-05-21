using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record WorldPiece {
    public string Name { get; init; }
    public MeshInstance3D Model { get; init; }
    public WorldPieceDir[] Directions { get; init; }

    public WorldPiece(string name, MeshInstance3D model, IEnumerable<WorldPieceDir> directions) {
        if (directions == null || !directions.Any()) {
            throw new Exception("World piece found with no directions: " + directions);
        }

        Name = name;
        Model = model;
        Directions = directions.ToArray();
    }
}
