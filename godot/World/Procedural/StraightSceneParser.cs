using Godot;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

public class StraightSceneParser {
    public static IEnumerable<ImportedSurface> GetList(ArrayMesh arrayMesh) {
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
                yield return new ImportedSurface(material, vertexGroup.ToList());
            }
        }
    }
}
