using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

public class PieceTypes {

    public static readonly Vector3 PIECE_SIZE = new(20, 1, 20);

    public static Godot.Collections.Array GenerateStraightMesh(ImportedMesh surface) {
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        List<Vector3> verts = [];
        List<Vector2> uvs = [];

        // generate the tri mesh
        var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

        // create vertex quads
        for (var i = 0; i < vertices.Length - 1; i++) {
            verts.Add(vertices[i + 1]);
            verts.Add(vertices[i]);
            verts.Add(vertices[i] + new Vector3(PIECE_SIZE.X, 0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));

            verts.Add(vertices[i + 1] + new Vector3(PIECE_SIZE.X, 0, 0));
            verts.Add(vertices[i + 1]);
            verts.Add(vertices[i] + new Vector3(PIECE_SIZE.X, 0, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();

        return surfaceArray;
    }

    public static Godot.Collections.Array GenerateHill(ImportedMesh surface, float highDiff, int segments) {
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        List<Vector3> verts = [];
        List<Vector2> uvs = [];

        var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

        for (int j = 0; j < segments; j++) {
            var curFraction = j / (float)segments;
            var nextFraction = (j + 1) / (float)segments;
            var curHeight = new Vector3(curFraction * PIECE_SIZE.X, highDiff / 2 + Mathf.Cos(curFraction * Mathf.Pi) * -highDiff / 2, 0);
            var nextHeight = new Vector3(nextFraction * PIECE_SIZE.X, highDiff / 2 + Mathf.Cos(nextFraction * Mathf.Pi) * -highDiff / 2, 0);

            for (var i = 0; i < vertices.Length - 1; i++) {
                verts.Add(vertices[i + 1] + curHeight);
                verts.Add(vertices[i] + curHeight);
                verts.Add(vertices[i] + nextHeight);
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));

                verts.Add(vertices[i + 1] + nextHeight);
                verts.Add(vertices[i + 1] + curHeight);
                verts.Add(vertices[i] + nextHeight);
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1));
            }
        }


        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        return surfaceArray;
    }

    public static Godot.Collections.Array GenerateCurveMeshByDeg(ImportedMesh surface, bool right, float degree, int segments) {
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        List<Vector3> verts = [];
        List<Vector2> uvs = [];

        var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

        var circleCenter = new Vector3(0, 0, right ? PIECE_SIZE.X : -PIECE_SIZE.X);

        // generate the tri mesh based on a circle centered to the left or right
        for (int j = 0; j < segments; j++) {
            var curAngle = new Basis(Vector3.Up, Mathf.DegToRad((right ? degree : -degree) * j / segments));
            var nextAngle = new Basis(Vector3.Up, Mathf.DegToRad((right ? degree : -degree) * (j + 1) / segments));

            for (var i = 0; i < vertices.Length - 1; i++) {
                verts.Add((vertices[i + 1] - circleCenter) * curAngle + circleCenter);
                verts.Add((vertices[i] - circleCenter) * curAngle + circleCenter);
                verts.Add((vertices[i] - circleCenter) * nextAngle + circleCenter);
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));

                verts.Add((vertices[i + 1] - circleCenter) * nextAngle + circleCenter);
                verts.Add((vertices[i + 1] - circleCenter) * curAngle + circleCenter);
                verts.Add((vertices[i] - circleCenter) * nextAngle + circleCenter);
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1));
            }
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        return surfaceArray;
    }


    public static Vector3 GenerateFinalPointOfMeshCurve(bool right, float degree) {
        var circleCenter = new Vector3(0, 0, right ? PIECE_SIZE.X : -PIECE_SIZE.X);
        var curAngle = new Basis(Vector3.Up, Mathf.DegToRad(right ? degree : -degree));
        return (new Vector3() - circleCenter) * curAngle + circleCenter;
    }
}
