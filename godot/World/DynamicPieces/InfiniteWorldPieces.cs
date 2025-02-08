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
    private readonly int _pieceAttemptMax;

    private readonly PackedScene _blenderScene;
    private readonly WorldType _pieceType;
    private readonly List<WorldPiece> _worldPieces = [];
    private Vector3 _trafficLeftSideOffset;

    private readonly List<Node3D> _placedPieces = [];
    private readonly List<Tuple<Transform3D, Node3D>> _checkpoints = [];
    private readonly List<WorldPiece> _queuedPieces = [];
    private LastPlacedDetails _nextTransform;
    public Transform3D NextPieceTransform => _nextTransform.FinalTransform;

    public List<string> IgnoredList { get; set; } = [];
    // ["left_45", "right_45"];

    [Signal]
    public delegate void PieceAddedEventHandler(Transform3D checkpointTransform, string pieceName, bool queuedPiece);

    public InfiniteWorldPieces(WorldType type, float generationRange = 40, int pieceAttemptMax = 10) {
        _generationRange = generationRange;
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

        // use the base location as the first checkpoint
        _checkpoints.Add(new(Transform3D.Identity, null));
    }


    public override void _Ready() {
        var scene = _blenderScene.Instantiate<Node3D>();

        // I have attempted mirroring pieces (and therefore only creating one model for each turn type)
        // The scale and rotate args of all methods only modify the transform
        // BUT we set the transform to place the piece so the above will get ignored
        // If you do figure it out all the normals and UV mappings are quite wrong
        // - You can also not move all the vertexes without a large perf hit, but it might be possible to make it from scratch i.e. another MeshInstance3D

        try {
            foreach (var c in scene.GetChildren().ToList()) {
                if (c is MeshInstance3D model) {
                    GD.Print("Loading '" + model.Name + "' as a road piece");
                    var directions = model.GetChildren()
                        .Where(x => x.GetType() == typeof(Node3D))
                        .Select(x => x as Node3D);

                    foreach (var dir in directions) {
                        c.RemoveChild(dir);
                        dir.QueueFree();
                    }

                    // attempt to read curve information from the piece, which is stored in the name of a sub node
                    float curveAngle = 0;
                    int segmentCount = 1;

                    var modelName = model.Name.ToString(); // its not a 'String'
                    if (modelName.Contains("left", StringComparison.InvariantCultureIgnoreCase) || modelName.Contains("right", StringComparison.InvariantCultureIgnoreCase)) {
                        if (modelName.Contains("90")) {
                            curveAngle = 90;
                            segmentCount = 4;
                        } else if (modelName.Contains("45")) {
                            curveAngle = 45;
                            segmentCount = 2;
                        } else {
                            GD.PrintErr($"Model name '{modelName}' doesn't contain a curve angle");
                        }
                        GD.Print($"   as {curveAngle} deg with {segmentCount} parts");
                    }

                    var directionsWithSegments = directions
                        .ToDictionary(x => x.Transform, y => y
                            .GetChildren()
                            .Where(x => x.GetType() == typeof(Node3D))
                            .Select(x => y.Transform * (x as Node3D).Transform)
                            );
                    var p = new WorldPiece(model.Name, model, directionsWithSegments, segmentCount, curveAngle);
                    _worldPieces.Add(p);
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
            GD.PrintErr("Failed to parse pieces for " + _pieceType);
            GD.PrintErr(e);
        }
        GD.Print("Loaded " + _worldPieces.Count + " pieces");
    }

    private WorldPiece PickRandom() {
        var pieceList = _worldPieces.Where(x => !IgnoredList.Contains(x.Name)).ToArray();
        return pieceList[_rand.RandiRange(0, pieceList.Length - 1)];
    }

    public override void _PhysicsProcess(double delta) {
        var pos = GetViewport().GetCamera3D().Position;
        var currentTransform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);

        // for the physics issues we can only make one piece per frame
        // while (pos.DistanceTo(_nextTransform.FinalTransform.Origin) < _distance) {

        if (pos.DistanceTo(_nextTransform.FinalTransform.Origin) >= _generationRange) {
            return;
        }

        if (_placedPieces.Count >= MAX_COUNT) {
            // keep max piece count by removing the oldest one
            var removal = _placedPieces.First();

            _placedPieces.Remove(removal);
            _checkpoints.RemoveAll(x => x.Item2 == removal);
            RemoveChild(removal);
        }

        foreach (var queuedPiece in new List<WorldPiece>(_queuedPieces)) {
            PlacePiece(queuedPiece, currentTransform, 0, true); // TODO only the first one so far

            currentTransform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);
        }
        _queuedPieces.Clear();

        var attempts = 0;
        var piece = PickRandom();
        var directionIndex = _rand.RandiRange(0, piece.Directions.Length - 1);
        while (!PieceValidSimple(piece, currentTransform, directionIndex) && attempts < _pieceAttemptMax) {
            piece = PickRandom();
            attempts++;
        }

        if (attempts > 0) {
            // GD.Print($"Found piece {piece?.Name} in {attempts} tries, at " + currentTransform);
        }

        PlacePiece(piece, currentTransform, directionIndex);
    }

    public void QueuePiece(string pieceName) {
        var piece = _worldPieces.FirstOrDefault(x => x.Name.Contains(pieceName));
        if (piece != null)
            _queuedPieces.Add(piece);
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

    private void PlacePiece(WorldPiece piece, Transform3D transform, int outIndex = 0, bool queuedPiece = false) {
        var toAdd = piece.Model.Duplicate() as Node3D;
        toAdd.Transform = new Transform3D(transform.Basis, transform.Origin);


        AddChild(toAdd);
        _placedPieces.Add(toAdd);

        GD.Print("InfinteWorldPieces: Placing piece " + piece.Name);
        var outDirection = piece.Directions.Skip(outIndex).First();

        foreach (var checkpoint in outDirection.Transforms) {
            var checkTransform = transform * checkpoint;
            _checkpoints.Add(new(checkTransform, toAdd));

            AddChild(DebugHelper.GenerateArrow(Colors.DeepPink, checkTransform, 2, 0.4f));
        }


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

    public IReadOnlyCollection<InfiniteCheckpoint> GetAllCurrentCheckpoints() {
        // rotate everything by CAR_ROTATION_OFFSET so its pointing the correct way for cars
        return _checkpoints
            .Select(x => x.Item1)
            .Append(_nextTransform.FinalTransform)
            .Select(x => new InfiniteCheckpoint(new Transform3D(CAR_ROTATION_OFFSET.Basis * x.Basis, x.Origin), x.Basis * _trafficLeftSideOffset))
            .ToList();
    }
}

public record InfiniteCheckpoint(Transform3D Transform, Vector3 LeftOffset);
