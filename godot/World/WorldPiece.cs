using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record WorldPiece {
    public string Name { get; init; }
    public MeshInstance3D Model { get; init; }
    public WorldPieceDir[] Directions { get; init; }
    private PieceZExtents _pieceZExtents;

    public WorldPiece(string name, MeshInstance3D model, IEnumerable<WorldPieceDir> directions, PieceZExtents zExtents) {
        if (directions == null || !directions.Any()) {
            throw new Exception("World piece found with no directions: " + directions);
        }

        Name = name;
        Model = model;
        Directions = directions.ToArray();
        _pieceZExtents = zExtents;
    }

    public IEnumerable<Vector3> GetZMinOffsets(WorldPieceDir dir) {
        yield return _pieceZExtents.MinZPos;
        foreach (var d in dir.Transforms) {
            yield return d * _pieceZExtents.MinZPos;
        }
    }

    public IEnumerable<Vector3> GetZMaxOffsets(WorldPieceDir dir) {
        yield return _pieceZExtents.MaxZPos;
        foreach (var d in dir.Transforms) {
            yield return d * _pieceZExtents.MaxZPos;
        }
    }
}
