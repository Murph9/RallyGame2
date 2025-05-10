using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

record ImportedMesh(Material Material, List<Vector3> Vertices);

public class ProceduralPieceGenerator : IPieceGenerator {

    private readonly List<ImportedMesh> _importedCrossSection = [];
    private readonly List<WorldPiece> _worldPieces = [];

    public Vector3 TrafficLeftSideOffset { get; private set; }
    private WorldType _type;

    public List<string> IgnoredList { get; init; } = [];

    public ProceduralPieceGenerator(WorldType type) {
        _type = type;
        UpdatePieceType(type);
    }

    public void UpdatePieceType(WorldType type) {
        // testing reading data from a blender file
        var packedScene = GD.Load<PackedScene>("res://assets/worldPieces/" + _type.ToString().ToLowerInvariant() + ".blend");
        var scene = packedScene.Instantiate<Node3D>();
        var model = scene.GetAllChildrenOfType<MeshInstance3D>().Single(x => x.Name == "straight");
        var arrayMesh = model.GetMesh() as ArrayMesh;

        foreach (var c in scene.GetAllChildrenOfType<Node3D>()) {
            if (c.Name == "TrafficLeftSide") {
                GD.Print("Loading " + c.Name + " as a traffic offset value");
                TrafficLeftSideOffset = c.Transform.Origin;
            }
        }

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

            // calculate which vertexes are connected to other vertexes
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
                    break;
                }
            }

            // then map the index groups back to vertex groups
            foreach (var vertexGroup in indicesGroups.Select(x => x.Where(y => outVertMap.ContainsKey(y)).Select(y => outVertMap[y]))) {
                var temp = new ImportedMesh(material, vertexGroup.ToList());

                _importedCrossSection.Add(temp);
            }
        }

        // generate pieces using the cross section

        const float STRAIGHT_LENGTH = 10;
        var straight = new ArrayMesh();
        var s = new SurfaceTool();
        foreach (var material in _importedCrossSection) {
            var surfaceArrays = GenerateStraightMesh(material, "straight" + material.Material.ResourceName, STRAIGHT_LENGTH);
            s.CreateFromArrays(surfaceArrays);
            s.GenerateNormals();
            s.GenerateTangents();
            surfaceArrays = s.CommitToArrays();

            straight.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrays);

            var index = straight.GetSurfaceCount() - 1;
            straight.SurfaceSetMaterial(index, material.Material);

        }

        ResourceSaver.Save(straight, "res://temp/assets/straight.tres", ResourceSaver.SaverFlags.Compress);

        var straightObj = new MeshInstance3D() {
            Mesh = straight
        };

        var body3d = new StaticBody3D();
        body3d.AddChild(new CollisionShape3D() {
            Shape = straight.CreateTrimeshShape()
        });
        straightObj.AddChild(body3d);

        _worldPieces.Add(new WorldPiece("straight", straightObj, new Dictionary<Transform3D, IEnumerable<Transform3D>>() {
            {new Transform3D(Basis.Identity, new Vector3(STRAIGHT_LENGTH, 0, 0)), []}
        }, 1, 0));
    }

    public (WorldPiece, int) Next(Transform3D currentTransform, RandomNumberGenerator rand) {
        // TODO IgnoredList
        return (_worldPieces[0], 0);
    }

    private static Godot.Collections.Array GenerateStraightMesh(ImportedMesh surface, string name, float length) {
        // generate new mesh
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        List<Vector3> verts = [];
        List<int> indices = [];

        // generate the quads
        var vertices = surface.Vertices.OrderBy(x => x.Z).ToArray();
        for (var i = 0; i < 2; i++) {
            foreach (var v in vertices) {
                verts.Add(v + new Vector3(length * i, 0, 0));
            }
        }
        // calc indices by column/row
        for (var i = 0; i < verts.Count; i += vertices.Length) {
            // draw the row, ignoring the last vertex column
            for (var j = 0; j < vertices.Length - 1; j++) {
                // triangle 1
                indices.Add(i + j + 1);
                indices.Add(i + j + vertices.Length);
                indices.Add(i + j);
                // triangle 2
                indices.Add(i + j + vertices.Length);
                indices.Add(i + j + 1);
                indices.Add(i + j + vertices.Length + 1);
            }
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        return surfaceArray;
    }
}
