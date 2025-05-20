using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record WorldPiece {
    public string Name { get; init; }
    public Node3D Model { get; init; }
    public WorldPieceDir[] Directions { get; init; }
    public List<Vector3> ObjectLocations { get; init; }

    public WorldPiece(string name, Node3D model, IEnumerable<WorldPieceDir> directions) {
        Name = name;
        Model = model;

        if (directions == null || !directions.Any()) {
            throw new Exception("World piece found with no directions: " + directions);
        }

        Directions = directions.ToArray();
    }
}
