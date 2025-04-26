using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.DynamicPieces;

public record LastPlacedDetails(string Name, Transform3D FinalTransform);

// tracks the world pieces
public partial class InfiniteWorldPieces : Node3D {

    private const int MAX_COUNT = 100;
    private static readonly Transform3D CAR_ROTATION_OFFSET = new(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);

    private readonly RandomNumberGenerator _rand = new();
    private readonly float _generationRange;

    private InfinitePieceGenerator _pieceGen;

    private readonly List<Node3D> _placedPieces = [];
    private readonly List<Tuple<Transform3D, Node3D, float>> _checkpoints = [];
    private readonly List<WorldPiece> _queuedPieces = [];
    private LastPlacedDetails _nextTransform;

    [Signal]
    public delegate void PieceAddedEventHandler(Transform3D checkpointTransform, string pieceName, bool queuedPiece);

    public InfiniteWorldPieces(WorldType type, float generationRange = 40, int pieceAttemptMax = 10) {
        _generationRange = generationRange;

        _pieceGen = new InfinitePieceGenerator(type, pieceAttemptMax);

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

        _nextTransform = new LastPlacedDetails(null, Transform3D.Identity);

        // use the base location as the first checkpoint
        _checkpoints.Add(new(Transform3D.Identity, null, 0));
    }

    public void UpdateWorldType(WorldType type) {
        _pieceGen.UpdatePieceType(type);
    }

    public override void _PhysicsProcess(double delta) {
        var pos = GetViewport().GetCamera3D().Position;
        var currentTransform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);

        if (_placedPieces.Count >= MAX_COUNT) {
            // keep max piece count by removing the oldest one
            var removal = _placedPieces.First();

            _placedPieces.Remove(removal);
            _checkpoints.RemoveAll(x => x.Item2 == removal);
            RemoveChild(removal);
        }

        foreach (var queuedPiece in new List<WorldPiece>(_queuedPieces)) {
            PlacePiece(queuedPiece, currentTransform, _rand.RandiRange(0, queuedPiece.Directions.Length - 1), true);

            currentTransform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);
        }
        _queuedPieces.Clear();

        if (pos.DistanceTo(_nextTransform.FinalTransform.Origin) >= _generationRange) {
            return;
        }

        var (piece, directionIndex) = _pieceGen.Next(currentTransform, _rand);
        PlacePiece(piece, currentTransform, directionIndex);
    }

    public void QueuePiece(string pieceName) {
        var piece = _pieceGen.WorldPieces.FirstOrDefault(x => x.Name.Contains(pieceName));
        if (piece != null)
            _queuedPieces.Add(piece);
    }


    private void PlacePiece(WorldPiece piece, Transform3D transform, int outIndex = 0, bool queuedPiece = false) {
        var toAdd = piece.Model.Duplicate() as Node3D;
        toAdd.Transform = new Transform3D(transform.Basis, transform.Origin);

        AddChild(toAdd);
        _placedPieces.Add(toAdd);

        // GD.Print("InfinteWorldPieces: Placing piece " + piece.Name);
        var outDirection = piece.Directions.Skip(outIndex).First();

        var checkpointDistance = _checkpoints.Last().Item3;
        checkpointDistance += outDirection.FinalTransform.Origin.Length();

        foreach (var checkpoint in outDirection.Transforms) {
            var checkTransform = transform * checkpoint;
            _checkpoints.Add(new(checkTransform, toAdd, checkpointDistance));

            AddChild(DebugHelper.GenerateArrow(Colors.DeepPink, checkTransform, 2, 0.4f));
        }

        PlaceObjectsForPiece(toAdd, piece);

        // TODO there has to be a way to do this with inbuilt methods:
        var pos = _nextTransform.FinalTransform.Origin + _nextTransform.FinalTransform.Basis * outDirection.FinalTransform.Origin;
        var rot = (_nextTransform.FinalTransform.Basis * outDirection.FinalTransform.Basis).GetRotationQuaternion().Normalized();
        _nextTransform = new LastPlacedDetails(piece.Name, new Transform3D(new Basis(rot), pos));

        // the transform is expected to be in the direction of travel here
        EmitSignal(SignalName.PieceAdded, transform, piece.Name, queuedPiece);
    }

    public InfiniteCheckpoint GetInitialSpawn() {
        if (_placedPieces == null || _placedPieces.Count() == 0) {
            return new InfiniteCheckpoint(CAR_ROTATION_OFFSET, Vector3.Zero);
        }

        return GetAllCurrentCheckpoints().First();
    }

    public void SetIgnoredPieces(IEnumerable<string> pieceNames) {
        _pieceGen.IgnoredList.Clear();
        _pieceGen.IgnoredList.AddRange(pieceNames);
    }

    public IReadOnlyCollection<InfiniteCheckpoint> GetAllCurrentCheckpoints() {
        // rotate everything by CAR_ROTATION_OFFSET so its pointing the correct way for cars
        return _checkpoints
            .Select(x => x.Item1)
            .Append(_nextTransform.FinalTransform)
            .Select(x => new InfiniteCheckpoint(new Transform3D(CAR_ROTATION_OFFSET.Basis * x.Basis, x.Origin), x.Basis * _pieceGen.TrafficLeftSideOffset))
            .ToList();
    }

    public float TotalDistanceFromCheckpoint(Vector3 position) {
        var checkpointTuple = _checkpoints.SingleOrDefault(x => x.Item1.Origin.IsEqualApprox(position));
        if (checkpointTuple != null) {
            return checkpointTuple.Item3;
        }
        return -1;
    }

    private void PlaceObjectsForPiece(Node3D root, WorldPiece piece) {
        foreach (var objLocation in piece.ObjectLocations) {
            var mat = new StandardMaterial3D() {
                AlbedoColor = Colors.GreenYellow
            };

            var mesh = new BoxMesh() {
                Size = new Vector3(_rand.RandfRange(1.5f, 8), _rand.RandfRange(1.5f, 8), _rand.RandfRange(1.5f, 8)),
                Material = mat
            };

            var instance = new MeshInstance3D() {
                Transform = new Transform3D(Basis.Identity, objLocation),
                Mesh = mesh
            };
            root.AddChild(instance);
        }
    }
}

public record InfiniteCheckpoint(Transform3D Transform, Vector3 LeftOffset);
