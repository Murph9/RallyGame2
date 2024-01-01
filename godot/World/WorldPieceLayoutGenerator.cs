using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World;

public class WorldPieceLayoutGenerator {

    private readonly RandomNumberGenerator _rand = new ();
    private readonly WorldPieces.Piece[] _pieces;

    public enum CircuitLayout {
        SimpleLoop,
        LargeCircle,
        Random
    }

    public WorldPieceLayoutGenerator(ICollection<WorldPieces.Piece> pieces) {
        _pieces = pieces.ToArray();
    }

    public IEnumerable<WorldPieces.Piece> GenerateFixed(CircuitLayout layout) {
        if (layout == CircuitLayout.SimpleLoop) {
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left");
            yield return GetPieceByName("left");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left");
            yield return GetPieceByName("left");
            yield break;
        }

        if (layout == CircuitLayout.LargeCircle) {
            yield return GetPieceByName("straight");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("straight");
            yield return GetPieceByName("left_long");
            yield return GetPieceByName("left_long");
            yield break;
        }

        if (layout == CircuitLayout.Random) {
            foreach (var l in GenerateRandomLoop()) {
                yield return l;
            }
        }
    }
    private WorldPieces.Piece GetPieceByName(string name) {
        return _pieces.First(x => x.Name == name);
    }

    public IEnumerable<WorldPieces.Piece> GenerateRandomCount(WorldPieces.Piece[] pieces, int count) {
        for (int i = 0; i < count; i++) {
            yield return pieces[_rand.RandiRange(0, pieces.Length - 1)];
        }
    }


    const float MAX_DISTANCE = 150;
    class StackPiece {
        public WorldPieces.Piece Piece;
        public Vector3 Position = new ();
        public Quaternion Rotation = Quaternion.Identity;
        public Aabb Aabb;
    }
    private IEnumerable<WorldPieces.Piece> GenerateRandomLoop() {

        // TODO this should probably generate a few and then try and get back home
        var stack = new Stack<StackPiece>();

        while (true) {
            var piece = GenerateNextPiece(stack);
            if (piece == null) {
                stack.Pop();
                continue;
            }

            StackPiece last;
            if (stack.Any())
                last = stack.Peek();
            else
                last = new StackPiece();

            // soz can only select the first direction one for now (it might have to be its own piece entry to work)
            var dir = piece.Directions.First();

            var newPos = last.Position + last.Rotation * dir.Origin;
            var newRot = last.Rotation * dir.Basis.GetRotationQuaternion();

            var newNode = piece.Node.Duplicate() as MeshInstance3D;
            newNode.Transform = new Transform3D(new Basis(newRot), newPos);
            stack.Push(new StackPiece() {
                Position = newPos,
                Rotation = newRot,
                Piece = new WorldPieces.Piece() {
                    Node = newNode,
                    Directions = piece.Directions,
                    Name = piece.Name
                },
                Aabb = newNode.GetAabb().Grow(-0.1f)
            });

            if (stack.Peek().Position.Length() < 1) {
                Console.WriteLine("Did it complete?");
                break;
            }
            Console.WriteLine(stack.Peek().Position.Length());
        }
        return stack.Select(x => x.Piece);
    }

    private WorldPieces.Piece GenerateNextPiece(Stack<StackPiece> stack) {
        var ourPieces = _pieces.Where(x => x.Name == "left").ToArray();
        Shuffle(ourPieces);

        if (stack.Count > 0) {
            var curPos = stack.Peek().Position;
            if (curPos.Length() > MAX_DISTANCE)
                ourPieces = ourPieces.OrderBy(x => (x.Directions.First().Origin + curPos).Length()).ToArray();
        }

        if (!stack.Any())
            return ourPieces.First();

        var last = stack.Peek();

        foreach (var p in ourPieces) {
            if (DoesItFit(p, stack, last.Position, last.Rotation)) {
                return p;
            }
        }

        return null;
    }

    private bool DoesItFit(WorldPieces.Piece piece, IEnumerable<StackPiece> pieces, Vector3 location, Quaternion direction) {
        var aabb = (piece.Node as MeshInstance3D).GetAabb();
        return pieces.All(x => !x.Aabb.Intersects(aabb));
    }

    private void Shuffle<T> (T[] array)
    {
        var n = array.Length;
        while (n > 1)
        {
            int k = _rand.RandiRange(0, n-- + -1);
            (array[k], array[n]) = (array[n], array[k]);
        }
    }
}
