using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

public class PieceTypes {

    public record PieceSurfaceResult(List<(Material, Godot.Collections.Array)> Surfaces);

    public static MeshInstance3D GenerateFor(PieceSurfaceResult pieceMeshes) {
        ArrayMesh arrayMesh = new();

        foreach (var surface in pieceMeshes.Surfaces) {
            var s = new SurfaceTool();
            s.CreateFromArrays(surface.Item2);
            s.GenerateNormals();
            s.GenerateTangents();

            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, s.CommitToArrays());

            var index = arrayMesh.GetSurfaceCount() - 1;
            arrayMesh.SurfaceSetMaterial(index, surface.Item1);
        }

        var meshObj = new MeshInstance3D() {
            Mesh = arrayMesh
        };

        var body3d = new StaticBody3D();
        body3d.AddChild(new CollisionShape3D() {
            Shape = arrayMesh.CreateTrimeshShape()
        });
        meshObj.AddChild(body3d);

        return meshObj;
    }

    public static PieceSurfaceResult GenerateStraightArrays(List<ImportedSurface> surfaces, Vector3 size) {
        var arrays = new List<(Material, Godot.Collections.Array)>();

        var totalLength = new Vector3(size.X, 0, 0);
        foreach (var surface in surfaces) {
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
                verts.Add(vertices[i] + totalLength);
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));

                verts.Add(vertices[i + 1] + totalLength);
                verts.Add(vertices[i + 1]);
                verts.Add(vertices[i] + totalLength);
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1));
            }

            surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();

            arrays.Add(new(surface.Material, surfaceArray));
        }

        return new PieceSurfaceResult(arrays);
    }

    public static PieceSurfaceResult GenerateHillArrays(List<ImportedSurface> surfaces, Vector3 size, int segments) {
        var arrays = new List<(Material, Godot.Collections.Array)>();

        foreach (var surface in surfaces) {
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

            arrays.Add(new(surface.Material, surfaceArray));
        }

        return new PieceSurfaceResult(arrays);
    }

    public static PieceSurfaceResult GenerateCurveArraysByDeg(List<ImportedSurface> surfaces, Vector3 size, bool right, float degree, int segments) {
        var arrays = new List<(Material, Godot.Collections.Array)>();

        var circleCenter = new Vector3(0, 0, right ? size.X : -size.X);
        foreach (var surface in surfaces) {
            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);
            List<Vector3> verts = [];
            List<Vector2> uvs = [];

            var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

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

            arrays.Add(new(surface.Material, surfaceArray));
        }

        return new PieceSurfaceResult(arrays);
    }

    public static PieceSurfaceResult GenerateCrossingArrays(List<ImportedSurface> surfaces, Vector3 size, bool left, bool right) {
        var arrays = new List<(Material, Godot.Collections.Array)>();

        var length = new Vector3(size.X, 0, 0);
        var largeOffset = length * 0.75f;
        var smallOffset = length * 0.25f;

        List<ImportedSurface> darkestSurfaces = [];
        foreach (var surface in surfaces) {
            if (darkestSurfaces.Count == 0) {
                darkestSurfaces.Add(surface);
            } else if (surface.Material is StandardMaterial3D) {
                var selfColor = (surface.Material as StandardMaterial3D).AlbedoColor;
                var currentColor = (darkestSurfaces.First().Material as StandardMaterial3D).AlbedoColor;
                if (selfColor.Luminance < currentColor.Luminance) {
                    darkestSurfaces.Clear();
                    darkestSurfaces.Add(surface);
                } else if (selfColor.Luminance == currentColor.Luminance) {
                    darkestSurfaces.Add(surface);
                }
            }

            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);
            List<Vector3> verts = [];
            List<Vector2> uvs = [];

            // generate the tri mesh
            var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

            // create vertex quads
            for (var i = 0; i < vertices.Length - 1; i++) {
                // close side
                verts.Add(vertices[i + 1]);
                verts.Add(vertices[i]);
                verts.Add(vertices[i] + smallOffset);
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));

                verts.Add(vertices[i + 1] + smallOffset);
                verts.Add(vertices[i + 1]);
                verts.Add(vertices[i] + smallOffset);
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1));

                // far side
                verts.Add(vertices[i + 1] + largeOffset);
                verts.Add(vertices[i] + largeOffset);
                verts.Add(vertices[i] + length);
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));

                verts.Add(vertices[i + 1] + length);
                verts.Add(vertices[i + 1] + largeOffset);
                verts.Add(vertices[i] + length);
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
                    verts.Add(leftAntiRotation * (vertices[i] + smallOffset - pieceCenter) + pieceCenter);
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(0, 1));

                    verts.Add(leftAntiRotation * (vertices[i + 1] + smallOffset - pieceCenter) + pieceCenter);
                    verts.Add(leftAntiRotation * (vertices[i + 1] - pieceCenter) + pieceCenter);
                    verts.Add(leftAntiRotation * (vertices[i] + smallOffset - pieceCenter) + pieceCenter);
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
                    verts.Add(rightAntiRotation * (vertices[i] + smallOffset - pieceCenter) + pieceCenter);
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(0, 1));

                    verts.Add(rightAntiRotation * (vertices[i + 1] + smallOffset - pieceCenter) + pieceCenter);
                    verts.Add(rightAntiRotation * (vertices[i + 1] - pieceCenter) + pieceCenter);
                    verts.Add(rightAntiRotation * (vertices[i] + smallOffset - pieceCenter) + pieceCenter);
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 1));
                }
            }


            surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();

            arrays.Add(new(surface.Material, surfaceArray));
        }

        if (darkestSurfaces != null && darkestSurfaces.Count != 0) {
            Godot.Collections.Array surfaceArray = [];
            surfaceArray.Resize((int)Mesh.ArrayType.Max);
            List<Vector3> verts = [];
            List<Vector2> uvs = [];

            // generate the tri mesh
            var vertices = darkestSurfaces.SelectMany(x => x.Vertices).OrderBy(x => x.Z).ToArray();
            // and the center, which we will do in the darkest color

            var vertexXLow = vertices.First();
            var vertexXHigh = vertices.Last();
            verts.Add(vertexXHigh);
            verts.Add(vertexXLow);
            verts.Add(vertexXLow + length);
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));

            verts.Add(vertexXHigh + length);
            verts.Add(vertexXHigh);
            verts.Add(vertexXLow + length);
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));

            surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();

            arrays.Add(new(darkestSurfaces.First().Material, surfaceArray)); // TODO
        }

        return new PieceSurfaceResult(arrays);
    }


    public static Vector3 GenerateFinalPointOfMeshCurve(Vector3 size, bool right, float degree) {
        var circleCenter = new Vector3(0, 0, right ? size.X : -size.X);
        var curAngle = new Basis(Vector3.Up, Mathf.DegToRad(right ? degree : -degree));
        return (new Vector3() - circleCenter) * curAngle + circleCenter;
    }
}
