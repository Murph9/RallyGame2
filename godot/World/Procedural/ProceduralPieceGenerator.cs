using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Procedural;

public record ImportedSurface(Material Material, List<Vector3> Vertices);

class WorldTypeDetails {
    public readonly List<ImportedSurface> ImportedSurfaces = [];
    public readonly List<WorldPiece> WorldPieces = [];
    public Vector3 TrafficLeftSideOffset;
}

public class ProceduralPieceGenerator : IPieceGenerator {

    private static readonly Vector3 PIECE_SIZE = new(20, 1, 20);
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

        var zExtents = StraightSceneParser.GetZExtents(arrayMesh);

        // load in the scene to generate pieces
        worldTypeDetails.ImportedSurfaces.AddRange(StraightSceneParser.GetList(arrayMesh));

        // generate pieces using the cross section and add to the list
        // straight
        var straightObj = PieceTypes.GenerateFor(PieceTypes.GenerateStraightArrays(worldTypeDetails.ImportedSurfaces, PIECE_SIZE));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("straight", straightObj, [
            WorldPieceDir.FromTransform(new Transform3D(Basis.Identity, new Vector3(PIECE_SIZE.X, 0, 0)), 1, 0)
        ], zExtents));

        // up a little
        var hillUpObj = PieceTypes.GenerateFor(PieceTypes.GenerateHillArrays(worldTypeDetails.ImportedSurfaces, PIECE_SIZE, SEGMENTS));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("hillUp", hillUpObj, [
            WorldPieceDir.FromTransform(new Transform3D(Basis.Identity, new Vector3(PIECE_SIZE.X, PIECE_SIZE.Y, 0)), SEGMENTS / 2, 0)
        ], zExtents));

        // down a little
        var hillDownObj = PieceTypes.GenerateFor(PieceTypes.GenerateHillArrays(worldTypeDetails.ImportedSurfaces, new Vector3(1, -1, 1) * PIECE_SIZE, SEGMENTS));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("hillDown", hillDownObj, [
            WorldPieceDir.FromTransform(new Transform3D(Basis.Identity, new Vector3(PIECE_SIZE.X, -PIECE_SIZE.Y, 0)), SEGMENTS / 2, 0)
        ], zExtents));

        // aggressive little down/up
        var hillSize = new Vector3(PIECE_SIZE.X / 3f, 0.5f, 0);
        var hillUpLittleObj = PieceTypes.GenerateFor(PieceTypes.GenerateHillArrays(worldTypeDetails.ImportedSurfaces, hillSize, SEGMENTS));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("hillUpLittle", hillUpLittleObj, [
            WorldPieceDir.FromTransform(new Transform3D(Basis.Identity, hillSize), SEGMENTS / 2, 0)
        ], zExtents));
        hillSize = new Vector3(PIECE_SIZE.X / 3f, -0.5f, 0);
        var hillDownLittleObj = PieceTypes.GenerateFor(PieceTypes.GenerateHillArrays(worldTypeDetails.ImportedSurfaces, hillSize, SEGMENTS));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("hillDownLittle", hillDownLittleObj, [
            WorldPieceDir.FromTransform(new Transform3D(Basis.Identity, hillSize), SEGMENTS / 2, 0)
        ], zExtents));

        // 45 deg turns
        var right45Obj = PieceTypes.GenerateFor(PieceTypes.GenerateCurveArraysByDeg(worldTypeDetails.ImportedSurfaces, PIECE_SIZE, true, 45, SEGMENTS / 2));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("right45", right45Obj, [
            WorldPieceDir.FromTransform(new Transform3D(MyMath.RIGHT45, PieceTypes.GenerateFinalPointOfMeshCurve(PIECE_SIZE, true, 45)), SEGMENTS / 2, 45)
        ], zExtents));

        var left45Obj = PieceTypes.GenerateFor(PieceTypes.GenerateCurveArraysByDeg(worldTypeDetails.ImportedSurfaces, PIECE_SIZE, false, 45, SEGMENTS / 2));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("left45", left45Obj, [
            WorldPieceDir.FromTransform(new Transform3D(MyMath.LEFT45, PieceTypes.GenerateFinalPointOfMeshCurve(PIECE_SIZE, false, 45)), SEGMENTS / 2, 45)
        ], zExtents));

        // 90 deg
        var right90Obj = PieceTypes.GenerateFor(PieceTypes.GenerateCurveArraysByDeg(worldTypeDetails.ImportedSurfaces, PIECE_SIZE, true, 90, SEGMENTS));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("right90", right90Obj, [
            WorldPieceDir.FromTransform(new Transform3D(MyMath.RIGHT90, PieceTypes.GenerateFinalPointOfMeshCurve(PIECE_SIZE, true, 90)), SEGMENTS, 90)
        ], zExtents));

        var left90Obj = PieceTypes.GenerateFor(PieceTypes.GenerateCurveArraysByDeg(worldTypeDetails.ImportedSurfaces, PIECE_SIZE, false, 90, SEGMENTS));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("left90", left90Obj, [
            WorldPieceDir.FromTransform(new Transform3D(MyMath.LEFT90, PieceTypes.GenerateFinalPointOfMeshCurve(PIECE_SIZE, false, 90)), SEGMENTS, 90)
        ], zExtents));

        // a cross piece
        var crossObj = PieceTypes.GenerateFor(PieceTypes.GenerateCrossingArrays(worldTypeDetails.ImportedSurfaces, PIECE_SIZE * 1.5f, true, true));
        worldTypeDetails.WorldPieces.Add(new WorldPiece("cross", crossObj, [
            WorldPieceDir.FromTransform(new Transform3D(Basis.Identity, new Vector3(PIECE_SIZE.X, 0, 0) * 1.5f), SEGMENTS, 90),
            WorldPieceDir.FromTransform(new Transform3D(MyMath.RIGHT90, new Vector3(PIECE_SIZE.X/2, 0, PIECE_SIZE.Z/2) * 1.5f), SEGMENTS, 90),
            WorldPieceDir.FromTransform(new Transform3D(MyMath.LEFT90, new Vector3(PIECE_SIZE.X/2, 0, -PIECE_SIZE.Z/2) * 1.5f), SEGMENTS, 90)
        ], zExtents));

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
