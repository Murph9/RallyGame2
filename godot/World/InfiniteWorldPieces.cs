using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.Procedural;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

// tracks the world pieces
public partial class InfiniteWorldPieces : Node3D, IWorld {

    record PrivateCheckpoint(Transform3D Transform3D, Node3D Node, float Distance);

    private const int REMOVE_PIECES_BEHIND_CAMERA_DISTANCE = 50;
    private static readonly Transform3D CAR_ROTATION_OFFSET = new(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
    private readonly Transform3D _spawnPoint = Transform3D.Identity;

    private readonly RandomNumberGenerator _rand = new();

    private readonly IPieceGenerator _pieceGen;
    private readonly PiecePlacementStrategy _placementStrategy;
    private readonly IPieceDecorator _pieceDecorator;

    private readonly List<Node3D> _placedPieces = [];
    private readonly List<PrivateCheckpoint> _checkpoints = [];
    private InfiniteCheckpoint _nextTransform;

    private double _pieceDistanceLimit;

    public WorldType CurrentWorldType => _pieceGen.CurrentWorldType;


    [Signal]
    public delegate void PieceAddedEventHandler(Transform3D checkpointTransform);

    public InfiniteWorldPieces(IPieceGenerator pieceGenerator, PiecePlacementStrategy strategy, IPieceDecorator pieceDecorator) {
        _pieceGen = pieceGenerator;
        _placementStrategy = strategy;
        _placementStrategy.NeedPiece += GeneratePiece;

        _pieceDecorator = pieceDecorator;

        // generate a starting box so we don't spawn in the void
        var boxBody = new StaticBody3D();
        boxBody.AddChild(new CollisionShape3D() {
            Shape = new BoxShape3D() {
                Size = new Vector3(10, 1, 10)
            }
        });
        boxBody.AddChild(new MeshInstance3D() {
            Mesh = new BoxMesh() {
                Size = new Vector3(10, 1, 10)
            },
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = Colors.Blue
            }
        });
        boxBody.Position = new Vector3(0, -0.501f, 0);
        AddChild(boxBody);

        _nextTransform = new InfiniteCheckpoint(null, Transform3D.Identity, Transform3D.Identity, Vector3.Zero);
    }

    public override void _Ready() {
        AddChild(_placementStrategy);
    }

    public void UpdateWorldType(WorldType type) {
        _pieceGen.UpdatePieceType(type);
    }

    public void LimitPlacingAfterDistance(double distance) {
        _pieceDistanceLimit = distance;
    }

    public override void _PhysicsProcess(double delta) {
        // attempt to remove the oldest piece
        var firstPiece = _placedPieces.FirstOrDefault();
        var cameraPos = GetViewport().GetCamera3D().GlobalPosition;
        if (firstPiece != null && firstPiece.GlobalPosition.DistanceTo(cameraPos) > REMOVE_PIECES_BEHIND_CAMERA_DISTANCE) {
            _placedPieces.Remove(firstPiece);
            _checkpoints.RemoveAll(x => x.Node == firstPiece);
            RemoveChild(firstPiece);
        }

        // update placementStrategy position for math
        var currentTransform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);
        _placementStrategy.NextTransform = currentTransform;
    }

    private void GeneratePiece() {
        if (_pieceDistanceLimit > 0 && _checkpoints.Last().Distance > _pieceDistanceLimit) {
            return; // no more generating
        }

        var currentTransform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);

        var (piece, directionIndex) = _pieceGen.Next(currentTransform, _rand);
        PlacePiece(piece, currentTransform, directionIndex);
    }

    private void PlacePiece(WorldPiece piece, Transform3D transform, int outIndex = 0, bool queuedPiece = false) {
        var toAdd = piece.Model.Duplicate() as Node3D;
        toAdd.Transform = new Transform3D(transform.Basis, transform.Origin);

        AddChild(toAdd);
        _placedPieces.Add(toAdd);

        var outDirection = piece.Directions.Skip(outIndex).First();

        var checkpointDistance = 0f;
        if (_checkpoints.Count > 0) {
            checkpointDistance = _checkpoints.Last().Distance;
            checkpointDistance += outDirection.FinalTransform.Origin.Length();
        }

        foreach (var checkpoint in outDirection.Transforms) {
            var checkTransform = transform * checkpoint;
            _checkpoints.Add(new(checkTransform, toAdd, checkpointDistance));

            toAdd.AddChild(DebugHelper.GenerateArrow(Colors.DeepPink, checkpoint, 2, 0.4f));
        }

        _pieceDecorator.DecoratePiece(toAdd, piece, outDirection);

        _nextTransform = new InfiniteCheckpoint(piece.Name, _nextTransform.FinalTransform, _nextTransform.FinalTransform * outDirection.FinalTransform, Vector3.Zero);

        // the transform is expected to be in the direction of travel here
        EmitSignal(SignalName.PieceAdded, transform);
    }

    public InfiniteCheckpoint GetInitialSpawn() {
        if (_checkpoints.Count == 0) {
            return CheckPointToInfiniteCheckpoint(_spawnPoint);
        }

        return GetAllCurrentCheckpoints().First();
    }

    public void SetIgnoredPieces(IEnumerable<string> pieceNames) {
        _pieceGen.IgnoredList.Clear();
        _pieceGen.IgnoredList.AddRange(pieceNames);
    }

    public IEnumerable<InfiniteCheckpoint> GetAllCurrentCheckpoints() {
        if (_checkpoints.Count == 0) {
            return [CheckPointToInfiniteCheckpoint(_spawnPoint)];
        }

        return _checkpoints
            .Select(x => x.Transform3D)
            .Append(_nextTransform.FinalTransform)
            .Select(CheckPointToInfiniteCheckpoint)
            .ToList();
    }

    private InfiniteCheckpoint CheckPointToInfiniteCheckpoint(Transform3D transform) {
        // rotate everything by CAR_ROTATION_OFFSET so its pointing the correct way for cars' models
        return new InfiniteCheckpoint(null, new Transform3D(CAR_ROTATION_OFFSET.Basis * transform.Basis, transform.Origin), new Transform3D(CAR_ROTATION_OFFSET.Basis * transform.Basis, transform.Origin), transform.Basis * _pieceGen.TrafficLeftSideOffset);
    }

    public float TotalDistanceFromCheckpoint(Vector3 position) {
        var checkpointTuple = _checkpoints.FirstOrDefault(x => x.Transform3D.Origin.IsEqualApprox(position));
        if (checkpointTuple != null) {
            return checkpointTuple.Distance;
        }
        return -1;
    }
}
