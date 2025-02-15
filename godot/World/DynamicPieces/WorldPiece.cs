using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public record WorldPiece {
    public string Name { get; init; }
    public Node3D Model { get; init; }
    public WorldPieceDir[] Directions { get; init; }

    private WorldPiece(string name, Node3D model, Dictionary<Transform3D, IEnumerable<Transform3D>> directions) : this(name, model, directions, 1, 0) { }
    private WorldPiece(string name, Node3D model, Dictionary<Transform3D, IEnumerable<Transform3D>> directions, int segments, float curveAngle) {
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

    public static WorldPiece LoadFrom(MeshInstance3D model) {
        GD.Print("Loading '" + model.Name + "' as a road piece");
        var directions = model.GetChildren()
            .Where(x => x.GetType() == typeof(Node3D))
            .Select(x => x as Node3D);

        foreach (var dir in directions) {
            model.RemoveChild(dir);
            dir.QueueFree();
        }

        // attempt to read curve information from the piece, which is stored in the name of a sub node
        float curveAngle = 0;
        int segmentCount = 1;

        var modelName = model.Name.ToString(); // its not a 'String'
        if (modelName.Contains("left", StringComparison.InvariantCultureIgnoreCase) || modelName.Contains("right", StringComparison.InvariantCultureIgnoreCase)) {
            if (modelName.Contains("90")) {
                curveAngle = 90;
                segmentCount = 4;
            } else if (modelName.Contains("45")) {
                curveAngle = 45;
                segmentCount = 2;
            } else {
                GD.PrintErr($"Model name '{modelName}' doesn't contain a curve angle");
            }
            GD.Print($"   as {curveAngle} deg with {segmentCount} parts");
        }

        var directionsWithSegments = directions
            .ToDictionary(x => x.Transform, y => y
                .GetChildren()
                .Where(x => x.GetType() == typeof(Node3D))
                .Select(x => y.Transform * (x as Node3D).Transform)
                );

        return new WorldPiece(model.Name, model, directionsWithSegments, segmentCount, curveAngle);
    }
}
