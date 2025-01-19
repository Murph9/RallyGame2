using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record LastPlacedDetails(string Name, Transform3D FinalTransform);

// tracks the world pieces
public partial class InfiniteWorldPieces : Node3D {
    
    private const int MAX_COUNT = 100;
    private readonly IReadOnlyCollection<string> EXCLUDED_LIST = [];
    // private readonly IReadOnlyCollection<string> EXCLUDED_LIST = // debugging
        // ["hill_up", "right_chicane", "left_chicane", "hill_down", "right_long", "left_long", "left_sharp", "right_sharp", "cross"];

    private readonly RandomNumberGenerator _rand = new ();
    private readonly float _distance;
    private readonly int _pieceAttemptMax;

    private readonly PackedScene _blenderScene;
    private readonly WorldType _pieceType;
    private readonly List<WorldPiece> _pieces = [];
    private readonly List<Node3D> _placedPieces = [];
    public List<WorldPiece> Pieces => [.. _pieces];
    
    private LastPlacedDetails _nextTransform;

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
                scene.RemoveChild(c);

                var model = c as Node3D;
                var directions = model.GetChildren().Where(x => x.GetType() == typeof(Node3D)).Select(x => x as Node3D);
                foreach (var dir in directions) {
                    c.RemoveChild(dir);
                    dir.QueueFree();
                }

                var p = new WorldPiece(model.Name, directions.Select(x => WorldPieceDir.FromTransform3D(x.Transform)).ToArray(), model);

                if (!EXCLUDED_LIST.Any(x => x == model.Name))
                    _pieces.Add(p);
            }
        } catch (Exception e) {
            GD.Print("Failed to parse pieces for " + _pieceType);
            GD.Print(e);
            return;
        }
        GD.Print("Loaded " + _pieces.Count + " pieces");
    }

    public void UpdateLatestPos(Vector3 pos) {
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
            GD.Print($"Found piece {piece?.Name} in {attempts} tries, at " + currentTransform);
            // GD.Print(piece.Directions[directionIndex].Offset + " " + piece.Directions[directionIndex].Turn  + " " + piece.Directions[directionIndex].Vert);
        }

        PlacePiece(piece, currentTransform, directionIndex);
    }

    private bool PieceValidSimple(WorldPiece piece, Transform3D transform, int outIndex) {
        var outDirection = piece.Directions.Skip(outIndex).First();
        var rot = (transform.Basis * outDirection.Transform.Basis).GetRotationQuaternion().Normalized();
        var angle = rot.AngleTo(Quaternion.Identity);
        GD.Print(angle);
        if (angle > Math.PI * 1/2f) {
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
    }

    public static Transform3D GetSpawn() {
        return new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
    }

    public Transform3D GetClosestPointTo(Vector3 pos) {
        if (_placedPieces.Count < 1) {
            return new Transform3D(GetSpawn().Basis, pos);
        }

        var closestTransform = _placedPieces.FirstOrDefault().GlobalTransform;
        var distance = closestTransform.Origin.DistanceSquaredTo(pos);
        
        foreach (var point in _placedPieces) {
            var currentDistance = point.GlobalTransform.Origin.DistanceSquaredTo(pos);
            if (currentDistance < distance) {
                closestTransform = point.GlobalTransform;
                distance = currentDistance;
            }
        }

        return new Transform3D(GetSpawn().Basis * closestTransform.Basis, closestTransform.Origin);
    }
}
