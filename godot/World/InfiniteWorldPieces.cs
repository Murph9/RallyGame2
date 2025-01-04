using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public record LastPlacedDetails(string Name, Transform3D FinalTransform);

public partial class InfiniteWorldPieces : Node3D {
    
    private const int MAX_COUNT = 100;

    // tracks the world pieces

    private readonly RandomNumberGenerator _rand = new ();
    private readonly float _distance;

    private readonly PackedScene _blenderScene;
    private readonly WorldType _pieceType;
    private readonly List<WorldPiece> _pieces = [];
    private readonly List<Node3D> _placedPieces = [];
    public List<WorldPiece> Pieces => [.. _pieces];
    
    private LastPlacedDetails _nextTransform;

    public InfiniteWorldPieces(WorldType type, float distance = 40) {
        _distance = distance;
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
                _pieces.Add(p);
            }
        } catch (Exception e) {
            GD.Print("Failed to parse pieces for " + _pieceType);
            GD.Print(e);
            return;
        }
    }

    public void UpdateLatestPos(Vector3 pos) {
        var transform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);

        // calc if we need to make more pieces
        while (pos.DistanceTo(_nextTransform.FinalTransform.Origin) < _distance) {
            if (_placedPieces.Count >= MAX_COUNT)
                return;

            var attempts = 0;
            var piece = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
            while (!PieceValid(piece, transform) && attempts < 10) {
                piece = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
                attempts++;
            }
            GD.Print($"Found piece {piece?.Name} in {attempts} tries, at " + transform);
            
            var current = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
            PlacePiece(current, transform, _rand.RandiRange(0, current.Directions.Length - 1));

            transform = new Transform3D(_nextTransform.FinalTransform.Basis, _nextTransform.FinalTransform.Origin);
        }
    }

    private bool PieceValid(WorldPiece piece, Transform3D transform) {
        var collisions = (piece.Model.Duplicate() as Node3D).GetAllChildrenOfType<CollisionShape3D>();

        if (!collisions.Any() || collisions.Count() > 1) {
            GD.PushError("My world piece was wrong");
            return false;
        }

        foreach (var collisionShape in collisions) {
            var space = GetWorld3D().DirectSpaceState;
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
        var outLocalTransform = piece.Directions.Skip(outIndex).First().Transform;

        // TODO there has to be a way to do this with inbuilt methods:
        var pos = _nextTransform.FinalTransform.Origin + _nextTransform.FinalTransform.Basis * outLocalTransform.Origin;
        var rot = (_nextTransform.FinalTransform.Basis * outLocalTransform.Basis).GetRotationQuaternion().Normalized();
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
