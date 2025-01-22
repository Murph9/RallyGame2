using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.World.DynamicPieces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record LastPlacedDetails(string Name, Transform3D FinalTransform);

// tracks the world pieces
public partial class InfiniteWorldPieces : Node3D {

    [Signal]
    public delegate void PieceAddedEventHandler(Transform3D checkpointTransform);

    private const int MAX_COUNT = 100;
    private static readonly Transform3D STARTING_OFFSET = new(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
    private readonly IReadOnlyCollection<string> EXCLUDED_LIST = [];
    // private readonly IReadOnlyCollection<string> EXCLUDED_LIST = // debugging
    // ["hill_up", "right_chicane", "left_chicane", "hill_down", "right_long", "left_long", "left_sharp", "right_sharp", "cross"];

    private readonly RandomNumberGenerator _rand = new();
    private readonly float _distance;
    private readonly int _pieceAttemptMax;

    private readonly PackedScene _blenderScene;
    private readonly WorldType _pieceType;
    private readonly List<WorldPiece> _pieces = [];
    private readonly List<Node3D> _placedPieces = [];
    private Vector3 _trafficLeftSideOffset;

    private LastPlacedDetails _nextTransform;
    public Transform3D NextPieceTransform => _nextTransform.FinalTransform;

    public InfiniteWorldPieces(WorldType type, float distance = 40, int pieceAttemptMax = 10) {
        _distance = distance;
        _pieceAttemptMax = pieceAttemptMax;

        _pieceType = type;
        _blenderScene = GD.Load<PackedScene>("res://assets/worldPieces/" + _pieceType.ToString().ToLower() + ".blend");

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
    }

    public override void _Ready() {
        var scene = _blenderScene.Instantiate<Node3D>();

        try {
            foreach (var c in scene.GetChildren().ToList()) {
                if (c is MeshInstance3D model) {
                    GD.Print("Loading " + model.Name + " as a road piece");
                    var directions = model.GetChildren().Where(x => x.GetType() == typeof(Node3D)).Select(x => x as Node3D);

                    foreach (var dir in directions) {
                        c.RemoveChild(dir);
                        dir.QueueFree();
                    }

                    var p = new WorldPiece(model.Name, directions.Select(x => WorldPieceDir.FromTransform3D(x.Transform)).ToArray(), model);

                    if (!EXCLUDED_LIST.Any(x => x == model.Name))
                        _pieces.Add(p);
                } else if (c.GetType() == typeof(Node3D)) {
                    var node = c as Node3D;
                    if (node.Name == "TrafficLeftSide") {
                        GD.Print("Loading " + node.Name + " as a traffic offset value");
                        _trafficLeftSideOffset = node.Transform.Origin;
                    }
                }
                scene.RemoveChild(c);
            }

            if (_trafficLeftSideOffset == Vector3.Zero) {
                throw new Exception("Traffic offset not set for model type " + _pieceType);
            }

        } catch (Exception e) {
            GD.Print("Failed to parse pieces for " + _pieceType);
            GD.Print(e);
            return;
        }
        GD.Print("Loaded " + _pieces.Count + " pieces");
    }

    public override void _PhysicsProcess(double delta) {
        var pos = GetViewport().GetCamera3D().Position;
        var currentTransform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);

        // for the physics issues we can only make one piece per frame
        // while (pos.DistanceTo(_nextTransform.FinalTransform.Origin) < _distance) {

        if (pos.DistanceTo(_nextTransform.FinalTransform.Origin) >= _distance) {
            return;
        }

        if (_placedPieces.Count >= MAX_COUNT) {
            // keep max piece count by removing the first one
            var removal = _placedPieces.First();

            _placedPieces.Remove(removal);
            RemoveChild(removal);
        }

        var attempts = 0;
        var piece = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
        var directionIndex = _rand.RandiRange(0, piece.Directions.Length - 1);
        while (!PieceValidSimple(piece, currentTransform, directionIndex) && attempts < _pieceAttemptMax) {
            piece = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
            attempts++;
        }

        if (attempts > 0) {
            // GD.Print($"Found piece {piece?.Name} in {attempts} tries, at " + currentTransform);
        }

        PlacePiece(piece, currentTransform, directionIndex);
    }

    private static bool PieceValidSimple(WorldPiece piece, Transform3D transform, int outIndex) {
        var outDirection = piece.Directions.Skip(outIndex).First();
        var rot = (transform.Basis * outDirection.Transform.Basis).GetRotationQuaternion().Normalized();
        var angle = rot.AngleTo(Quaternion.Identity);
        if (angle > Math.PI * 1 / 2f) {
            return false;
        }
        return true;
    }

    private bool PieceValidPhysicsCollision(WorldPiece piece, Transform3D transform) {
        var space = GetWorld3D().DirectSpaceState;
        var collisionShapes = piece.Model.GetAllChildrenOfType<CollisionShape3D>();

        if (!collisionShapes.Any() || collisionShapes.Count() > 1) {
            GD.PushError("My world piece was wrong: " + collisionShapes);
            return false;
        }

        foreach (var collisionShape in collisionShapes) {
            var physicsParams = new PhysicsShapeQueryParameters3D {
                Shape = collisionShape.Shape,
                Transform = transform
            };
            var result = space.IntersectShape(physicsParams);

            if (result.Count > 0) {
                return false;
            }
        }

        return true;
    }

    private void PlacePiece(WorldPiece piece, Transform3D transform, int outIndex = 0) {
        var toAdd = piece.Model.Duplicate() as Node3D;
        toAdd.Transform = new Transform3D(transform.Basis, transform.Origin);

        // soz can only select the first one for now
        var outDirection = piece.Directions.Skip(outIndex).First();

        // TODO there has to be a way to do this with inbuilt methods:
        var pos = _nextTransform.FinalTransform.Origin + _nextTransform.FinalTransform.Basis * outDirection.Transform.Origin;
        var rot = (_nextTransform.FinalTransform.Basis * outDirection.Transform.Basis).GetRotationQuaternion().Normalized();
        _nextTransform = new LastPlacedDetails(piece.Name, new Transform3D(new Basis(rot), pos));

        AddChild(toAdd);
        _placedPieces.Add(toAdd);

        // the transform is expected to be in the direction of travel here
        EmitSignal(SignalName.PieceAdded, new Transform3D(STARTING_OFFSET.Basis * transform.Basis, transform.Origin));
    }

    public InfiniteCheckpoint GetSpawn() {
        if (_placedPieces == null || _placedPieces.Count() == 0) {
            return new InfiniteCheckpoint(STARTING_OFFSET, Vector3.Zero);
        }

        return GetAllCurrentCheckpoints().First();
    }

    public IReadOnlyCollection<InfiniteCheckpoint> GetAllCurrentCheckpoints() {
        return _placedPieces
            .Select(x => x.GlobalTransform)
            .Append(_nextTransform.FinalTransform)
            .Select(x => new InfiniteCheckpoint(new Transform3D(STARTING_OFFSET.Basis * x.Basis, x.Origin), x.Basis * _trafficLeftSideOffset))
            .ToList();
    }
}

public record InfiniteCheckpoint(Transform3D Transform, Vector3 LeftOffset);
