using Godot;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

public class PieceTypes {

    public static Godot.Collections.Array GenerateStraightMesh(ImportedMesh surface, Vector3 size) {
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
            verts.Add(vertices[i] + new Vector3(size.X, 0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));

            verts.Add(vertices[i + 1] + new Vector3(size.X, 0, 0));
            verts.Add(vertices[i + 1]);
            verts.Add(vertices[i] + new Vector3(size.X, 0, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();

        return surfaceArray;
    }

    public static Godot.Collections.Array GenerateHill(ImportedMesh surface, Vector3 size, int segments) {
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        List<Vector3> verts = [];
        List<Vector2> uvs = [];

        var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

        for (int j = 0; j < segments; j++) {
            var curFraction = j / (float)segments;
            var nextFraction = (j + 1) / (float)segments;
            var curHeight = new Vector3(curFraction * size.X, size.Y / 2 + Mathf.Cos(curFraction * Mathf.Pi) * -size.Y / 2, 0);
            var nextHeight = new Vector3(nextFraction * size.X, size.Y / 2 + Mathf.Cos(nextFraction * Mathf.Pi) * -size.Y / 2, 0);

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

    public static Godot.Collections.Array GenerateCurveMeshByDeg(ImportedMesh surface, Vector3 size, bool right, float degree, int segments) {
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        List<Vector3> verts = [];
        List<Vector2> uvs = [];

        var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

        var circleCenter = new Vector3(0, 0, right ? size.X : -size.X);

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

    public static Godot.Collections.Array GenerateCrossingMesh(ImportedMesh surface, Vector3 size, bool left, bool right) {
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
            verts.Add(vertices[i] + new Vector3(size.X, 0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));

            verts.Add(vertices[i + 1] + new Vector3(size.X, 0, 0));
            verts.Add(vertices[i + 1]);
            verts.Add(vertices[i] + new Vector3(size.X, 0, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
        }

        var pieceCenter = new Vector3(size.X / 2f, 0, 0);

        if (left) {
            var leftAntiRotation = new Basis(Vector3.Up, Mathf.DegToRad(90));
            // create right vertex quads
            for (var i = 0; i < vertices.Length - 1; i++) {
                verts.Add(leftAntiRotation * (vertices[i + 1] - pieceCenter) + pieceCenter);
                verts.Add(leftAntiRotation * (vertices[i] - pieceCenter) + pieceCenter);
                verts.Add(leftAntiRotation * (vertices[i] + new Vector3(size.X / 2, 0, 0) - pieceCenter) + pieceCenter);
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));

                verts.Add(leftAntiRotation * (vertices[i + 1] + new Vector3(size.X / 2, 0, 0) - pieceCenter) + pieceCenter);
                verts.Add(leftAntiRotation * (vertices[i + 1] - pieceCenter) + pieceCenter);
                verts.Add(leftAntiRotation * (vertices[i] + new Vector3(size.X / 2, 0, 0) - pieceCenter) + pieceCenter);
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1));
            }
        }

        if (right) {
            var rightAntiRotation = new Basis(Vector3.Up, Mathf.DegToRad(-90));
            for (var i = 0; i < vertices.Length - 1; i++) {
                verts.Add(rightAntiRotation * (vertices[i + 1] - pieceCenter) + pieceCenter);
                verts.Add(rightAntiRotation * (vertices[i] - pieceCenter) + pieceCenter);
                verts.Add(rightAntiRotation * (vertices[i] + new Vector3(size.X / 2, 0, 0) - pieceCenter) + pieceCenter);
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));

                verts.Add(rightAntiRotation * (vertices[i + 1] + new Vector3(size.X / 2, 0, 0) - pieceCenter) + pieceCenter);
                verts.Add(rightAntiRotation * (vertices[i + 1] - pieceCenter) + pieceCenter);
                verts.Add(rightAntiRotation * (vertices[i] + new Vector3(size.X / 2, 0, 0) - pieceCenter) + pieceCenter);
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1));
            }
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        return surfaceArray;
    }


    public static Vector3 GenerateFinalPointOfMeshCurve(Vector3 size, bool right, float degree) {
        var circleCenter = new Vector3(0, 0, right ? size.X : -size.X);
        var curAngle = new Basis(Vector3.Up, Mathf.DegToRad(right ? degree : -degree));
        return (new Vector3() - circleCenter) * curAngle + circleCenter;
    }
}
