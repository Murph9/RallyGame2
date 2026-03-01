using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

public interface IPieceDecorator {
    void DecoratePiece(Node3D node, WorldPiece piece, WorldPieceDir outDirection, Transform3D pieceTransform, IReadOnlyList<Vector3> allRoadPoints);
}

public partial class PieceDecorator : IPieceDecorator {

    private const float FENCE_HEIGHT = 1.5f;
    private const float BUILDING_SIDE_OFFSET = 3f;   // gap between road edge and nearest building face
    private const float FIELD_SIDE_OFFSET = 5f;       // gap between road edge and nearest field edge
    private const float OVERPASS_HEIGHT = 5f;
    private const float OVERPASS_THICKNESS = 0.8f;
    private const float GROUND_WIDTH = 60f;           // how far the ground strip extends from the road edge

    private enum ZoneType { Trees, Buildings, Courtyard, Field, Overpass }

    private readonly Node3D _fencePost;

    public PieceDecorator() {
        _fencePost = ObjectHelper.BoxLine(Colors.Brown, Vector3.Zero, new Vector3(0, FENCE_HEIGHT, 0), 0.2f);
    }

    public void DecoratePiece(Node3D node, WorldPiece piece, WorldPieceDir outDirection, Transform3D pieceTransform, IReadOnlyList<Vector3> allRoadPoints) {
        var edgeMax = piece.GetZMaxOffsets(outDirection).ToArray();
        var edgeMin = piece.GetZMinOffsets(outDirection).ToArray();
        var fenceHeightVector = new Vector3(0, FENCE_HEIGHT, 0);

        // Always: ground strips so the void isn't visible, barriers, collision
        GenerateGroundStrips(node, edgeMin, edgeMax);
        GenerateWalls(node, edgeMin, edgeMax, fenceHeightVector);
        GenerateSideCollision(node, edgeMin, fenceHeightVector);
        GenerateSideCollision(node, edgeMax, fenceHeightVector);

        // The checkpoints for THIS piece are its sub-transforms applied to pieceTransform.
        // These are the "owned" road points used in Voronoi comparisons.
        var myPoints = outDirection.Transforms
            .Select(t => (pieceTransform * t).Origin)
            .ToList();
        if (myPoints.Count == 0)
            myPoints.Add(pieceTransform.Origin);

        // Per-piece deterministic seed so decoration is stable across frames
        var rand = new RandomNumberGenerator();
        rand.Seed = (ulong)Math.Abs((long)(pieceTransform.Origin.X * 1009) + (long)(pieceTransform.Origin.Z * 997));

        var isStraight = outDirection.Turn == WorldPieceDir.TurnType.Straight;

        // On curves, only safe close-in trees. On straights, pick a richer zone.
        ZoneType zone;
        if (!isStraight) {
            zone = ZoneType.Trees;
        } else {
            zone = rand.RandiRange(0, 9) switch {
                0 or 1 => ZoneType.Field,
                2 or 3 => ZoneType.Courtyard,
                4 => ZoneType.Overpass,
                _ => ZoneType.Buildings
            };
        }

        switch (zone) {
            case ZoneType.Trees:
                PlaceTrees(node, edgeMin, edgeMax);
                break;
            case ZoneType.Buildings:
                PlaceBuildingRow(node, edgeMin, edgeMax, pieceTransform, myPoints, allRoadPoints, rand);
                break;
            case ZoneType.Courtyard:
                PlaceCourtyard(node, edgeMin, edgeMax, pieceTransform, myPoints, allRoadPoints, rand);
                break;
            case ZoneType.Field:
                PlaceField(node, edgeMin, edgeMax, pieceTransform, myPoints, allRoadPoints, rand);
                break;
            case ZoneType.Overpass:
                PlaceOverpass(node, edgeMin, edgeMax, pieceTransform, myPoints, allRoadPoints);
                PlaceTrees(node, edgeMin, edgeMax);
                break;
        }
    }

    // -------------------------------------------------------------------
    // Voronoi ownership test
    // -------------------------------------------------------------------

    /// <summary>
    /// Returns true if <paramref name="localPos"/> (in toAdd-local space) lies within the
    /// Voronoi cell of this piece — i.e. one of <paramref name="myPoints"/> is the nearest
    /// road checkpoint in the XZ plane.
    /// </summary>
    private static bool IsOwned(Vector3 localPos, Transform3D pieceTransform, List<Vector3> myPoints, IReadOnlyList<Vector3> allPoints) {
        var world = pieceTransform * localPos;
        var flat = new Vector2(world.X, world.Z);

        // Find the global minimum distance across all road checkpoints
        var minAll = float.MaxValue;
        foreach (var pt in allPoints) {
            var d = flat.DistanceTo(new Vector2(pt.X, pt.Z));
            if (d < minAll) minAll = d;
        }

        // Owned if one of our own points achieves (or ties) that minimum
        const float tolerance = 0.5f;
        foreach (var pt in myPoints) {
            if (flat.DistanceTo(new Vector2(pt.X, pt.Z)) <= minAll + tolerance)
                return true;
        }
        return false;
    }

    // -------------------------------------------------------------------
    // Zone: Trees  (close-in, always safe, no Voronoi check needed)
    // -------------------------------------------------------------------

    private static void PlaceTrees(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax) {
        foreach (var (eMin, eMax) in edgeMin.Zip(edgeMax)) {
            var diff = (eMin - eMax).Normalized();
            PlaceTree(node, eMin + diff * 2f);
            PlaceTree(node, eMax - diff * 2f);
        }
    }

    private static void PlaceTree(Node3D node, Vector3 pos) {
        node.AddChild(ObjectHelper.BoxLine(Colors.SaddleBrown, pos, pos + Vector3.Up * 2f, 0.4f));
        node.AddChild(ObjectHelper.Sphere(Colors.Green, pos + Vector3.Up * 3f, 2f));
    }

    // -------------------------------------------------------------------
    // Zone: Buildings
    // -------------------------------------------------------------------

    private static void PlaceBuildingRow(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax,
        Transform3D pieceTransform, List<Vector3> myPoints, IReadOnlyList<Vector3> allRoadPoints,
        RandomNumberGenerator rand) {
        for (var i = 0; i < edgeMin.Length - 1; i++) {
            PlaceBuildingSegment(node, edgeMin, edgeMax, i, true, pieceTransform, myPoints, allRoadPoints, rand);
            PlaceBuildingSegment(node, edgeMin, edgeMax, i, false, pieceTransform, myPoints, allRoadPoints, rand);
        }
    }

    private static void PlaceBuildingSegment(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax, int i, bool minSide,
        Transform3D pieceTransform, List<Vector3> myPoints, IReadOnlyList<Vector3> allRoadPoints,
        RandomNumberGenerator rand) {
        var edgePt = minSide ? edgeMin[i] : edgeMax[i];
        var edgePtNext = minSide ? edgeMin[i + 1] : edgeMax[i + 1];
        var opposite = minSide ? edgeMax[i] : edgeMin[i];

        var outward = (edgePt - opposite).Normalized();
        var segDir = (edgePtNext - edgePt).Normalized();
        var segLength = (edgePtNext - edgePt).Length();
        var mid = edgePt.Lerp(edgePtNext, 0.5f);

        var height = rand.RandfRange(3f, 10f);
        var depth = rand.RandfRange(4f, 10f);

        // Centre of building face, in toAdd-local space
        var buildingCenter = mid + outward * (BUILDING_SIDE_OFFSET + depth * 0.5f) + Vector3.Up * (height * 0.5f);

        if (!IsOwned(buildingCenter, pieceTransform, myPoints, allRoadPoints)) return;

        var color = PickBuildingColor(rand);
        var basis = BuildBasis(segDir);
        var boxSize = new Vector3(segLength * 0.9f, height, depth);

        node.AddChild(new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = boxSize,
                Material = new StandardMaterial3D() { AlbedoColor = color }
            },
            Transform = new Transform3D(basis, buildingCenter)
        });

        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D() {
            Shape = new BoxShape3D() { Size = boxSize },
            Transform = new Transform3D(basis, buildingCenter)
        });
        node.AddChild(body);
    }

    // -------------------------------------------------------------------
    // Zone: Courtyard  (buildings bookending an open area with props)
    // -------------------------------------------------------------------

    private static void PlaceCourtyard(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax,
        Transform3D pieceTransform, List<Vector3> myPoints, IReadOnlyList<Vector3> allRoadPoints,
        RandomNumberGenerator rand) {
        var numSegments = edgeMin.Length - 1;
        for (var i = 0; i < numSegments; i++) {
            var isGap = i > 0 && i < numSegments - 1;
            if (isGap) {
                // Place a prop in the courtyard on each side
                foreach (var minSide in new[] { true, false }) {
                    var edgePt = minSide ? edgeMin[i] : edgeMax[i];
                    var opposite = minSide ? edgeMax[i] : edgeMin[i];
                    var outward = (edgePt - opposite).Normalized();
                    var propPos = edgePt + outward * (BUILDING_SIDE_OFFSET + 3f);
                    if (IsOwned(propPos, pieceTransform, myPoints, allRoadPoints))
                        PlaceCourtyardProp(node, propPos, rand);
                }
            } else {
                PlaceBuildingSegment(node, edgeMin, edgeMax, i, true, pieceTransform, myPoints, allRoadPoints, rand);
                PlaceBuildingSegment(node, edgeMin, edgeMax, i, false, pieceTransform, myPoints, allRoadPoints, rand);
            }
        }
    }

    private static void PlaceCourtyardProp(Node3D node, Vector3 pos, RandomNumberGenerator rand) {
        switch (rand.RandiRange(0, 2)) {
            case 0: // fountain / column
                node.AddChild(ObjectHelper.BoxLine(Colors.LightGray, pos, pos + Vector3.Up * 2f, 0.5f));
                node.AddChild(ObjectHelper.Sphere(new Color(0.5f, 0.7f, 1f, 0.8f), pos + Vector3.Up * 2.5f, 0.8f));
                break;
            case 1: // tree cluster
                PlaceTree(node, pos);
                PlaceTree(node, pos + new Vector3(2.5f, 0f, 0f));
                break;
            case 2: // low wall cross
                var h = 0.5f;
                node.AddChild(ObjectHelper.BoxLine(Colors.SlateGray, pos + new Vector3(-2f, h, 0f), pos + new Vector3(2f, h, 0f), 0.3f));
                node.AddChild(ObjectHelper.BoxLine(Colors.SlateGray, pos + new Vector3(0f, h, -2f), pos + new Vector3(0f, h, 2f), 0.3f));
                break;
        }
    }

    // -------------------------------------------------------------------
    // Zone: Field  (flat coloured planes — grain or grass)
    // -------------------------------------------------------------------

    private static void PlaceField(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax,
        Transform3D pieceTransform, List<Vector3> myPoints, IReadOnlyList<Vector3> allRoadPoints,
        RandomNumberGenerator rand) {
        for (var i = 0; i < edgeMin.Length - 1; i++) {
            PlaceFieldSegment(node, edgeMin, edgeMax, i, true, pieceTransform, myPoints, allRoadPoints, rand);
            PlaceFieldSegment(node, edgeMin, edgeMax, i, false, pieceTransform, myPoints, allRoadPoints, rand);
        }
    }

    private static void PlaceFieldSegment(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax, int i, bool minSide,
        Transform3D pieceTransform, List<Vector3> myPoints, IReadOnlyList<Vector3> allRoadPoints,
        RandomNumberGenerator rand) {
        var edgePt = minSide ? edgeMin[i] : edgeMax[i];
        var edgePtNext = minSide ? edgeMin[i + 1] : edgeMax[i + 1];
        var opposite = minSide ? edgeMax[i] : edgeMin[i];

        var outward = (edgePt - opposite).Normalized();
        var segDir = (edgePtNext - edgePt).Normalized();
        var segLength = (edgePtNext - edgePt).Length();
        var mid = edgePt.Lerp(edgePtNext, 0.5f);

        var fieldWidth = rand.RandfRange(10f, 25f);
        var fieldCenter = mid + outward * (FIELD_SIDE_OFFSET + fieldWidth * 0.5f);

        if (!IsOwned(fieldCenter, pieceTransform, myPoints, allRoadPoints)) return;

        var color = rand.RandiRange(0, 1) == 0
            ? new Color(0.8f, 0.75f, 0.3f)   // grain / crop
            : Colors.ForestGreen;             // grass

        node.AddChild(new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = new Vector3(segLength, 0.1f, fieldWidth),
                Material = new StandardMaterial3D() { AlbedoColor = color }
            },
            Transform = new Transform3D(BuildBasis(segDir), fieldCenter)
        });

        // Thin fence-line separating field from road
        node.AddChild(ObjectHelper.BoxLine(Colors.Peru,
            edgePt + outward * FIELD_SIDE_OFFSET,
            edgePtNext + outward * FIELD_SIDE_OFFSET, 0.15f));
    }

    // -------------------------------------------------------------------
    // Zone: Overpass  (beam spanning the road + support pillars)
    // -------------------------------------------------------------------

    private static void PlaceOverpass(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax,
        Transform3D pieceTransform, List<Vector3> myPoints, IReadOnlyList<Vector3> allRoadPoints) {
        var midIdx = edgeMin.Length / 2;
        var spanMin = edgeMin[midIdx];
        var spanMax = edgeMax[midIdx];
        var midFlat = new Vector3(spanMin.Lerp(spanMax, 0.5f).X, 0f, spanMin.Lerp(spanMax, 0.5f).Z);

        if (!IsOwned(midFlat, pieceTransform, myPoints, allRoadPoints)) return;

        var topMin = spanMin + Vector3.Up * OVERPASS_HEIGHT;
        var topMax = spanMax + Vector3.Up * OVERPASS_HEIGHT;

        // Horizontal beam
        node.AddChild(ObjectHelper.BoxLine(Colors.DarkGray, topMin, topMax, OVERPASS_THICKNESS));

        // Support pillars
        node.AddChild(ObjectHelper.BoxLine(Colors.Gray, spanMin, topMin, OVERPASS_THICKNESS));
        node.AddChild(ObjectHelper.BoxLine(Colors.Gray, spanMax, topMax, OVERPASS_THICKNESS));

        // Collision for the beam (cars can drive under, things can drive over)
        var spanDir = (spanMax - spanMin).Normalized();
        var spanLen = (spanMax - spanMin).Length();
        var beamCenter = topMin.Lerp(topMax, 0.5f);
        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D() {
            Shape = new BoxShape3D() { Size = new Vector3(spanLen, OVERPASS_THICKNESS, OVERPASS_THICKNESS) },
            Transform = new Transform3D(BuildBasis(spanDir), beamCenter)
        });
        node.AddChild(body);
    }

    // -------------------------------------------------------------------
    // Ground strips
    // -------------------------------------------------------------------

    /// <summary>
    /// Builds a flat quad-strip of <see cref="GROUND_WIDTH"/> extending outward from each road edge.
    /// Each segment between consecutive edge points becomes one quad so the strip follows curves.
    /// A matching <see cref="StaticBody3D"/> is added so objects/cars don't fall through.
    /// </summary>
    private static void GenerateGroundStrips(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax) {
        GenerateGroundStrip(node, edgeMin, edgeMax, minSide: true);
        GenerateGroundStrip(node, edgeMin, edgeMax, minSide: false);
    }

    private static void GenerateGroundStrip(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax, bool minSide) {
        var edge = minSide ? edgeMin : edgeMax;
        var opposite = minSide ? edgeMax : edgeMin;

        var groundMat = new StandardMaterial3D() {
            AlbedoColor = new Color(0.32f, 0.26f, 0.18f), // dirt brown
            VertexColorUseAsAlbedo = false,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetMaterial(groundMat);

        for (var i = 0; i < edge.Length - 1; i++) {
            var a = edge[i];
            var b = edge[i + 1];
            var opp = opposite[i];

            // Outward direction in XZ, perpendicular to the edge segment
            var segDir = (b - a).Normalized();
            var outward = (a - opp).Normalized();
            // Make outward purely horizontal and truly perpendicular to seg
            outward = new Vector3(outward.X, 0f, outward.Z).Normalized();
            if (outward.IsZeroApprox())
                outward = segDir.Cross(Vector3.Up).Normalized();

            // Four corners of the ground quad, clamped to Y=0
            var v0 = new Vector3(a.X, 0f, a.Z);
            var v1 = new Vector3(b.X, 0f, b.Z);
            var v2 = new Vector3(a.X + outward.X * GROUND_WIDTH, 0f, a.Z + outward.Z * GROUND_WIDTH);
            var v3 = new Vector3(b.X + outward.X * GROUND_WIDTH, 0f, b.Z + outward.Z * GROUND_WIDTH);

            var normal = Vector3.Up;

            // Triangle 1
            st.SetNormal(normal); st.AddVertex(v0);
            st.SetNormal(normal); st.AddVertex(v2);
            st.SetNormal(normal); st.AddVertex(v1);
            // Triangle 2
            st.SetNormal(normal); st.AddVertex(v1);
            st.SetNormal(normal); st.AddVertex(v2);
            st.SetNormal(normal); st.AddVertex(v3);
        }

        var mesh = st.Commit();
        node.AddChild(new MeshInstance3D() { Mesh = mesh });

        // Matching static collision so it's solid underfoot
        var body = new StaticBody3D();
        var shape = new ConcavePolygonShape3D();
        shape.SetFaces(mesh.GetFaces());
        body.AddChild(new CollisionShape3D() { Shape = shape });
        node.AddChild(body);
    }

    // -------------------------------------------------------------------
    // Barriers (always placed)
    // -------------------------------------------------------------------

    private void GenerateSideCollision(Node3D node, Vector3[] verts, Vector3 fenceUpVector) {
        for (var i = 0; i < verts.Length - 1; i++) {
            var body3d = new StaticBody3D() {
                PhysicsMaterialOverride = new PhysicsMaterial() { Friction = 0, Bounce = 0 }
            };
            body3d.AddChild(new CollisionShape3D() {
                Shape = new ConvexPolygonShape3D() {
                    Points = [
                        verts[i],
                        verts[i + 1],
                        verts[i] + fenceUpVector,
                        verts[i] + fenceUpVector,
                        verts[i + 1],
                        verts[i + 1] + fenceUpVector
                    ]
                }
            });
            node.AddChild(body3d);
        }
    }

    private void GenerateWalls(Node3D node, Vector3[] edgeMin, Vector3[] edgeMax, Vector3 fenceUpVector) {
        foreach (var edgePoint in edgeMax.Concat(edgeMin)) {
            var fence = (Node3D)_fencePost.Duplicate();
            fence.Transform = new Transform3D(fence.Transform.Basis, edgePoint + fenceUpVector * 0.5f);
            node.AddChild(fence);
        }
        for (var i = 0; i < edgeMax.Length - 1; i++)
            node.AddChild(ObjectHelper.BoxLine(Colors.Brown, edgeMax[i] + fenceUpVector, edgeMax[i + 1] + fenceUpVector, 0.2f));
        for (var i = 0; i < edgeMin.Length - 1; i++)
            node.AddChild(ObjectHelper.BoxLine(Colors.Brown, edgeMin[i] + fenceUpVector, edgeMin[i + 1] + fenceUpVector, 0.2f));
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    /// <summary>Builds an orthogonal basis whose X axis aligns with <paramref name="forward"/>.</summary>
    private static Basis BuildBasis(Vector3 forward) {
        if (forward.IsZeroApprox()) return Basis.Identity;
        var right = forward.Normalized();
        var up = Vector3.Up;
        // Re-orthogonalise in case forward is nearly vertical
        var side = right.Cross(up).Normalized();
        if (side.IsZeroApprox()) side = Vector3.Right;
        up = side.Cross(right).Normalized();
        return new Basis(right, up, side);
    }

    private static Color PickBuildingColor(RandomNumberGenerator rand) => rand.RandiRange(0, 3) switch {
        0 => Colors.SlateGray,
        1 => new Color(0.7f, 0.35f, 0.2f),  // brick red
        2 => Colors.WhiteSmoke,
        _ => Colors.DarkKhaki
    };
}