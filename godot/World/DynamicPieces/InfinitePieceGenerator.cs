using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.Search;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public class InfinitePieceGenerator {
    private readonly int _pieceAttemptMax;
    private WorldType _pieceType;

    private PackedScene _blenderScene;
    public Vector3 TrafficLeftSideOffset { get; private set; }

    private readonly List<WorldPiece> _worldPieces = [];
    public IReadOnlyCollection<WorldPiece> WorldPieces => _worldPieces;

    public List<string> IgnoredList { get; init; } = [];
    // ["left_45", "right_45"];

    public InfinitePieceGenerator(WorldType type, int pieceAttemptMax) {
        _pieceAttemptMax = pieceAttemptMax;
        UpdatePieceType(type);
    }

    public void UpdatePieceType(WorldType type) {
        _pieceType = type;
        _blenderScene = GD.Load<PackedScene>("res://assets/worldPieces/" + _pieceType.ToString().ToLower() + ".blend");

        _worldPieces.Clear();
        var scene = _blenderScene.Instantiate<Node3D>();

        // I have attempted mirroring pieces (and therefore only creating one model for each turn type)
        // The scale and rotate args of all methods only modify the transform
        // BUT we set the transform to place the piece so the above will get ignored
        // If you do figure it out all the normals and UV mappings are quite wrong
        // - You can also not move all the vertexes without a large perf hit, but it might be possible to make it from scratch i.e. another MeshInstance3D

        try {
            foreach (var c in scene.GetChildren().ToList()) {
                if (c is MeshInstance3D model) {
                    _worldPieces.Add(WorldPiece.LoadFrom(model));
                } else if (c is Node3D node) {
                    if (node.Name == "TrafficLeftSide") {
                        GD.Print("Loading " + node.Name + " as a traffic offset value");
                        TrafficLeftSideOffset = node.Transform.Origin;
                    }
                }
                scene.RemoveChild(c);
            }

            if (TrafficLeftSideOffset == Vector3.Zero) {
                throw new Exception("Traffic offset not set for model type " + _pieceType);
            }

        } catch (Exception e) {
            GD.PrintErr("Failed to parse pieces for " + _pieceType);
            GD.PrintErr(e);
        }
        GD.Print("Loaded " + _worldPieces.Count + " pieces");
    }

    public (WorldPiece, int) Next(Transform3D currentTransform, RandomNumberGenerator rand) {
        var attempts = 0;
        var piece = PickRandom(rand);
        var directionIndex = rand.RandiRange(0, piece.Directions.Length - 1);
        while (!PieceValidSimple(piece, currentTransform, directionIndex) && attempts < _pieceAttemptMax) {
            piece = PickRandom(rand);
            directionIndex = rand.RandiRange(0, piece.Directions.Length - 1);
            attempts++;
        }

        if (attempts > 0) {
            // GD.Print($"Found piece {piece?.Name} in {attempts} tries, at " + currentTransform);
        }

        return (piece, directionIndex);
    }

    private WorldPiece PickRandom(RandomNumberGenerator rand) {
        var pieceList = _worldPieces.Where(x => !IgnoredList.Contains(x.Name)).ToArray();
        return RandHelper.RandFromList(rand, pieceList);
    }


    private static bool PieceValidSimple(WorldPiece piece, Transform3D transform, int outIndex) {
        var outDirection = piece.Directions.Skip(outIndex).First();
        var rot = (transform.Basis * outDirection.FinalTransform.Basis).GetRotationQuaternion().Normalized();
        var angle = rot.AngleTo(Quaternion.Identity);
        if (angle > Math.PI * 1 / 2f) {
            return false;
        }
        return true;
    }
}
