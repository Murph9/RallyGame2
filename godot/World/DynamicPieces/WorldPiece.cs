using System;
using System.Collections.Generic;
using Godot;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public record WorldPiece {
    public string Name { get; init; }
    public Node3D Model { get; init; }
    public WorldPieceDir[] Directions { get; init; }
    public int Segments { get; init; }
    public float CurveAngle { get; init; }

    private readonly List<Transform3D> _subSegments = [];

    public WorldPiece(string name, Node3D model, WorldPieceDir[] directions, int segments, float curveAngle) {
        Name = name;
        Model = model;
        Directions = directions;
        Segments = segments;
        CurveAngle = curveAngle;

        _subSegments.AddRange(GenerateSubSegments(0));
    }

    public IEnumerable<Transform3D> GetSubSegments(int direction = 0) {
        return _subSegments;
    }

    private IEnumerable<Transform3D> GenerateSubSegments(int direction) {
        if (CurveAngle == 0 || Segments <= 1 || Directions.Length > 1) {
            yield return Directions[direction].Transform;
            yield break;
        }
        // TODO we'll figure out the path for intersections when its required

        // TODO the curve angle could be 0 and we still want to generate segments

        // calculate intermediate transforms
        var target = Directions[0].Transform;

        // length of chord is chord = 2*radis*sin(theta/2)
        // so radius from chord length is radius = chord/(2*sin(theta/2))
        var radius = Math.Abs(target.Origin.ToV2XZ().Length() / (2 * Mathf.Sin(Mathf.DegToRad(CurveAngle) / 2)));

        for (int i = 1; i < Segments; i++) {
            var angle = i / (float)Segments * Mathf.DegToRad(CurveAngle);
            var x = Mathf.Sin(angle) * radius;
            var z = radius - Mathf.Cos(angle) * radius;
            yield return new Transform3D(new Basis(Vector3.Up, -Mathf.Sign(target.Origin.Z) * angle), new Vector3(x, target.Origin.Y, z * Mathf.Sign(target.Origin.Z)));
        }

        yield return target;
    }
}
