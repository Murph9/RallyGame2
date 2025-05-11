using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

public record ImportedMesh(Material Material, List<Vector3> Vertices);

class WorldTypeDetails {
    public readonly List<ImportedMesh> ImportedCrossSection = [];
    public readonly List<WorldPiece> WorldPieces = [];
    public Vector3 TrafficLeftSideOffset;
}

public class ProceduralPieceGenerator : IPieceGenerator {

    private static readonly Vector3 PIECE_SIZE = new(20, 2, 20);
    private static readonly int PIECE_ATTEMPT_COUNT = 3;
    private static readonly int SEGMENTS = 4;

    private readonly Dictionary<WorldType, WorldTypeDetails> _types = [];
    private WorldType _currentType;

    public List<string> IgnoredList { get; init; } = [];
    public Vector3 TrafficLeftSideOffset => _types[_currentType].TrafficLeftSideOffset;

    public ProceduralPieceGenerator(WorldType type) {
        UpdatePieceType(type);
    }

    public void UpdatePieceType(WorldType type) {
        _currentType = type;

        if (_types.ContainsKey(type)) {
            return; // already loaded
        }

        var worldTypeDetails = new WorldTypeDetails();

        // reading cross section data from the blender files
        var packedScene = GD.Load<PackedScene>("res://assets/worldPieces/" + _currentType.ToString().ToLowerInvariant() + ".blend");
        var scene = packedScene.Instantiate<Node3D>();
        var model = scene.GetAllChildrenOfType<MeshInstance3D>().Single(x => x.Name == "straight");
        var arrayMesh = model.GetMesh() as ArrayMesh;

        foreach (var c in scene.GetAllChildrenOfType<Node3D>()) {
            if (c.Name == "TrafficLeftSide") {
                GD.Print("Loading " + c.Name + " as a traffic offset value");
                worldTypeDetails.TrafficLeftSideOffset = c.Transform.Origin;
            }
        }

        // scenes are split by material, so we need to get all the vertex groups
        for (var i = 0; i < arrayMesh.GetSurfaceCount(); i++) {
            var material = arrayMesh.SurfaceGetMaterial(i);
            var arrays = arrayMesh.SurfaceGetArrays(i);

            var vertices = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            var indices = ((int[])arrays[(int)Mesh.ArrayType.Index]).ToList();

            // calculate if you get from an index to every other
            var connections = new Dictionary<int, HashSet<int>>();
            for (int k = 0; k < indices.Count; k += 3) {
                var ind = indices[k];
                var ind1 = indices[k + 1];
                var ind2 = indices[k + 2];

                if (!connections.TryGetValue(ind, out HashSet<int>? value1))
                    connections.Add(ind, [ind1, ind2]);
                else {
                    value1.Add(ind1);
                    value1.Add(ind2);
                }
                if (!connections.TryGetValue(ind1, out HashSet<int>? value2))
                    connections.Add(ind1, [ind, ind2]);
                else {
                    value2.Add(ind);
                    value2.Add(ind2);
                }
                if (!connections.TryGetValue(ind2, out HashSet<int>? value3))
                    connections.Add(ind2, [ind1, ind]);
                else {
                    value3.Add(ind1);
                    value3.Add(ind);
                }
            }

            // store only vertices we care about on the x axis
            var outVertMap = new Dictionary<int, Vector3>();

            // calculate which vertexes are connected to other vertexes, so we join them correctly in the pieces
            var indicesGroups = new List<List<int>>();
            while (true) {
                var foundIndices = new List<int>();
                var queue = new Queue<int>();
                var start = connections.First().Key;
                queue.Enqueue(start);
                foundIndices.Add(start);

                while (queue.Count != 0) {
                    var curIndice = queue.Dequeue();

                    // map index to vert for later use
                    if (Mathf.IsZeroApprox(vertices[curIndice].X)) {
                        // export the x = 0 vertices which are connected into groups
                        outVertMap.Add(curIndice, vertices[curIndice]);
                    }

                    // enqueue all neighbours
                    foreach (var n in connections[curIndice]) {
                        if (!foundIndices.Contains(n)) {
                            foundIndices.Add(n);
                            queue.Enqueue(n);
                        }
                    }
                    connections.Remove(curIndice);
                }

                // these connected indices are done
                indicesGroups.Add(foundIndices);

                if (connections.Count == 0) {
                    break; // all connections in this group are completed
                }
            }

            // then map the index groups back to vertex groups
            foreach (var vertexGroup in indicesGroups.Select(x => x.Where(y => outVertMap.ContainsKey(y)).Select(y => outVertMap[y]))) {
                var temp = new ImportedMesh(material, vertexGroup.ToList());

                worldTypeDetails.ImportedCrossSection.Add(temp);
            }
        }

        // generate pieces using the cross section and add to the list
        var straightObj = GenerateFor(worldTypeDetails.ImportedCrossSection, GenerateStraightMesh);
        worldTypeDetails.WorldPieces.Add(new WorldPiece("straight", straightObj, new Dictionary<Transform3D, IEnumerable<Transform3D>>() {
            { new Transform3D(Basis.Identity, new Vector3(PIECE_SIZE.X, 0, 0)), [] }
        }, 1, 0));

        var rightObj = GenerateFor(worldTypeDetails.ImportedCrossSection, (surface) => Generate90DegMesh(surface, true));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("right", rightObj, new Dictionary<Transform3D, IEnumerable<Transform3D>>() {
            { new Transform3D(MyMath.RIGHT90, new Vector3(PIECE_SIZE.X, 0, PIECE_SIZE.Z)), [] }
        }, SEGMENTS, 90));

        var leftObj = GenerateFor(worldTypeDetails.ImportedCrossSection, (surface) => Generate90DegMesh(surface, false));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("left", leftObj, new Dictionary<Transform3D, IEnumerable<Transform3D>>() {
            { new Transform3D(MyMath.LEFT90, new Vector3(PIECE_SIZE.X, 0, -PIECE_SIZE.Z)), [] }
        }, SEGMENTS, 90));

        _types.Add(type, worldTypeDetails);
    }

    public (WorldPiece, int) Next(Transform3D currentTransform, RandomNumberGenerator rand) {
        var attempts = 0;
        var piece = PickRandom(rand);
        var directionIndex = rand.RandiRange(0, piece.Directions.Length - 1);
        while (!PieceValid(piece, currentTransform, directionIndex) && attempts < PIECE_ATTEMPT_COUNT) {
            piece = PickRandom(rand);
            directionIndex = rand.RandiRange(0, piece.Directions.Length - 1);
            attempts++;
        }

        return (piece, directionIndex);
    }

    private WorldPiece PickRandom(RandomNumberGenerator rand) {
        var pieceList = _types[_currentType].WorldPieces.Where(x => !IgnoredList.Contains(x.Name)).ToArray();
        return RandHelper.RandFromList(rand, pieceList);
    }

    private static MeshInstance3D GenerateFor(List<ImportedMesh> importedCrossSection, Func<ImportedMesh, Godot.Collections.Array> func) {
        ArrayMesh arrayMesh = new();
        var s = new SurfaceTool();
        foreach (var surface in importedCrossSection) {
            var surfaceArrays = func(surface);
            s.CreateFromArrays(surfaceArrays);
            s.GenerateNormals();
            s.GenerateTangents();
            surfaceArrays = s.CommitToArrays();

            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrays);

            var index = arrayMesh.GetSurfaceCount() - 1;
            arrayMesh.SurfaceSetMaterial(index, surface.Material);
        }

        //ResourceSaver.Save(arrayMesh, resourceName, ResourceSaver.SaverFlags.Compress);

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

    private static Godot.Collections.Array GenerateStraightMesh(ImportedMesh surface) {
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

    private static Godot.Collections.Array Generate90DegMesh(ImportedMesh surface, bool right) {
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        List<Vector3> verts = [];
        List<Vector2> uvs = [];

        var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();

        var circleCenter = new Vector3(0, 0, right ? PIECE_SIZE.X : -PIECE_SIZE.X);

        // generate the tri mesh based on a circle centered to the left or right
        for (int j = 0; j < SEGMENTS; j++) {
            var curAngle = new Basis(Vector3.Up, Mathf.DegToRad((right ? 90 : -90) * j / (float)SEGMENTS));
            var nextAngle = new Basis(Vector3.Up, Mathf.DegToRad((right ? 90 : -90) * (j + 1) / (float)SEGMENTS));

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

    private static bool PieceValid(WorldPiece piece, Transform3D transform, int outIndex) {
        var outDirection = piece.Directions.Skip(outIndex).First();
        var rot = (transform.Basis * outDirection.FinalTransform.Basis).GetRotationQuaternion().Normalized();
        var angle = rot.AngleTo(Quaternion.Identity);
        if (angle > Math.PI / 2f) {
            return false;
        }
        return true;
    }
}
