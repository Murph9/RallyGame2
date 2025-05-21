using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record WorldPiece {
    public string Name { get; init; }
    public MeshInstance3D Model { get; init; }
    public WorldPieceDir[] Directions { get; init; }

    private readonly (Vector3, Vector3) _zOffsets;

    public WorldPiece(string name, MeshInstance3D model, IEnumerable<WorldPieceDir> directions, (Vector3, Vector3) zOffsets) {
        if (directions == null || !directions.Any()) {
            throw new Exception("World piece found with no directions: " + directions);
        }

        Name = name;
        Model = model;
        Directions = directions.ToArray();
        _zOffsets = zOffsets;
    }

    public IEnumerable<Vector3> GetZMinOffsets(WorldPieceDir dir) {
        yield return _zOffsets.Item1;
        foreach (var d in dir.Transforms) {
            yield return d * _zOffsets.Item1;
        }
    }

    public IEnumerable<Vector3> GetZMaxOffsets(WorldPieceDir dir) {
        yield return _zOffsets.Item2;
        foreach (var d in dir.Transforms) {
            yield return d * _zOffsets.Item2;
        }
    }
}
