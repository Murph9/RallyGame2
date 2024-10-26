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

    private readonly PackedScene _blenderScene;
    private readonly WorldType _pieceType;
    private readonly List<WorldPiece> _pieces = [];
    private readonly List<Node3D> _placedPieces = [];
    public List<WorldPiece> Pieces => [.. _pieces];
    
    private LastPlacedDetails _lastPlacedDetails;

    public InfiniteWorldPieces(WorldType type) {
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

        _lastPlacedDetails = new LastPlacedDetails(null, Transform3D.Identity);
    }

    public override void _Ready() {
        LoadPiecesFromScene();
    }

    private void LoadPiecesFromScene() {
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
        // calc if we need to make more pieces
        while (pos.DistanceTo(_lastPlacedDetails.FinalTransform.Origin) < 40) {
            if (_placedPieces.Count >= MAX_COUNT)
                return;

            var attempts = 0;
            var piece = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
            while (!PieceValid(piece) && attempts < 10) {
                piece = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
                attempts++;
            }
            GD.Print($"Found piece {piece?.Name} in {attempts} tries");
            
            var current = _pieces[_rand.RandiRange(0, _pieces.Count - 1)];
            PlacePiece(current, _rand.RandiRange(0, current.Directions.Length - 1));
        }
    }

    private bool PieceValid(WorldPiece piece) {
        var cloneP = piece.Model.Duplicate() as Node3D;
        cloneP.Transform = new Transform3D(_lastPlacedDetails.FinalTransform.Basis, _lastPlacedDetails.FinalTransform.Origin);

        if (cloneP.GetChildren().First() is not StaticBody3D s) {
            GD.PushError("My world piece was wrong");
            return false;
        }
        
        var area3d = new Area3D();
        
        var collisionObjects = _placedPieces.SelectMany(x => x.GetAllChildrenOfType<CollisionObject3D>());
        foreach (var colls in collisionObjects) {
            area3d.AddChild(colls.Duplicate());
        }

        return !area3d.OverlapsArea(cloneP);
    }

    private void PlacePiece(WorldPiece piece, int outIndex = 0) {
        var toAdd = piece.Model.Duplicate() as Node3D;
        toAdd.Transform = new Transform3D(_lastPlacedDetails.FinalTransform.Basis, _lastPlacedDetails.FinalTransform.Origin);

        // soz can only select the first one for now
        var dir = piece.Directions.Skip(outIndex).First().Transform;

        // TODO there has to be a way to do this with inbuilt methods:
        var pos = _lastPlacedDetails.FinalTransform.Origin + _lastPlacedDetails.FinalTransform.Basis * dir.Origin;
        var rot = (_lastPlacedDetails.FinalTransform.Basis * dir.Basis).GetRotationQuaternion().Normalized();
        _lastPlacedDetails = new LastPlacedDetails(piece.Name, new Transform3D(new Basis(rot), pos));

        AddChild(toAdd);
        _placedPieces.Add(toAdd);
    }

    public static Transform3D GetSpawn() {
        return new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(90)), Vector3.Zero);
    }
}
