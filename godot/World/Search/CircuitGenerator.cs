using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.World.Search;

public class CircuitGenerator(BasicEl[] pieces) {
    private readonly RandomNumberGenerator _rand = new ();
    private readonly BasicEl[] _pieces = pieces;

    public IEnumerable<BasicEl> GenerateRandomLoop(int randAmount = 3, int startAmount = 8, int maxCount = 20) {
        // always start with a straight piece
        var last = new SearchPiece(null, _pieces.First(x => x.Name == "straight"));

        const string JUSTPLACE = "justplace";
        const string MOVEHOME = "movehome";
        const string CLOSELOOP = "closeloop";

        var state = JUSTPLACE;

        while (true) {
            var nextNeighbours = GeneratePieces(last);
            if (nextNeighbours.Length < 1) {
                last = last.Parent;
                continue;
                // reset to the last piece if there is no solution
                // TODO needs to stop getting in loops or picking a piece
            }

            if (state == JUSTPLACE) {
                last = nextNeighbours[_rand.RandiRange(0, nextNeighbours.Length - 1)];
                if (last.LengthToRoot > randAmount) {
                    Console.WriteLine(GetNamesOfPath(last) + " @" + last.FinalPosition);
                    state = MOVEHOME;
                }
            } else if (state == MOVEHOME) {
                // generally move home with a weighting to pieces that move closer to home
                var nexts = nextNeighbours.OrderBy(x => x.F).ToArray();
                var index = 0; // weight pieces to the closest
                while (_rand.Randf() > 0.5f)
                    index++;

                last = nexts[Mathf.Min(nexts.Length - 1, index)];
                if (last.LengthToRoot >= startAmount) {
                    Console.WriteLine(GetNamesOfPath(last) + " @" + last.FinalPosition);
                    state = CLOSELOOP;
                }
            } else if (state == CLOSELOOP) {
                var finish = GetFinalPieces(last, maxCount);
                return finish.GetParentPath().Select(x => x.Piece);
            } else {
                throw new Exception("Unknown state " + state);
            }
        }
    }


    private SearchPiece GetFinalPieces(SearchPiece last, int maxCount) {
        var queue = new PriorityQueue<SearchPiece, double>();
        var seen = new HashSet<SearchPiece> {
            last
        };

        foreach (var c in GeneratePieces(last, true)) {
            queue.Enqueue(c, c.F);
        }
        while (queue.Count < 1) {
            last = last.Parent;
            foreach (var c in GeneratePieces(last, true)) {
                queue.Enqueue(c, c.F);
            }
        }

        while (queue.Count > 0) {
            var q = queue.Dequeue();
            if (q.F < 1 && q.FinalRotation.IsEqualApprox(Quaternion.Identity)) {
                return q;
            }

            if (queue.Count % 1000 == 0) {
                Console.WriteLine(GetNamesOfPath(q));
            }

            foreach (var o in GeneratePieces(q, true)) {
                if (q.LengthToRoot > maxCount) continue;
                if (q.F < 1 && !q.FinalRotation.IsEqualApprox(Quaternion.Identity))
                    continue; // please ignore anything that touches the goal but doesn't face the right way

                if (!seen.Contains(o)) {
                    queue.Enqueue(o, o.F);
                    seen.Add(o);
                }
            }
        }

        return null;
    }

    private BasicEl[] GetValidPiecesForPath(Dictionary<WorldPieceDirType, int> partTypes) {
        // add pieces that can help get back to 0
        var piecesList = _pieces.Where(x => x.Dir.Type == WorldPieceDirType.Straight).ToList();

        var lefts = partTypes.GetValueOrDefault(WorldPieceDirType.Left90);
        var rights = partTypes.GetValueOrDefault(WorldPieceDirType.Right90);
        if (lefts >= rights) {
            // then we probably want to only add lefts
            // = left is favoured incase they are the same
            piecesList.AddRange(_pieces.Where(x => x.Dir.Type == WorldPieceDirType.Left90));
        } else {
            // then we probably want to only add rights only
            piecesList.AddRange(_pieces.Where(x => x.Dir.Type == WorldPieceDirType.Right90));
        }

        // TODO chicanes need to be swapped back so they align again

        var ups = partTypes.GetValueOrDefault(WorldPieceDirType.Up);
        var downs = partTypes.GetValueOrDefault(WorldPieceDirType.Down);
        if (ups > downs) {
            piecesList.AddRange(_pieces.Where(x => x.Dir.Type == WorldPieceDirType.Down));
        } else if (ups < downs) {
            piecesList.AddRange(_pieces.Where(x => x.Dir.Type == WorldPieceDirType.Up));
        }

        return piecesList.ToArray();
    }


    private SearchPiece[] GeneratePieces(SearchPiece top, bool filterPieces = false) {
        var pieces = filterPieces ? GetValidPiecesForPath(top.GetTypesOfPath()) : _pieces;

        var currentPath = top.GetParentPath();

        var list = new List<SearchPiece>();
        foreach (var p in pieces) {
            var newP = new SearchPiece(top, p);
            var types = newP.GetTypesOfPath();
            var offsets = types.GetValueOrDefault(WorldPieceDirType.OffsetLeft) + types.GetValueOrDefault(WorldPieceDirType.OffsetRight);
            newP.H = offsets * 100 + Math.Abs(top.FinalPosition.Y) + top.FinalRotation.AngleTo(Quaternion.Identity) * 10;

            // any collisions don't allow them
            var any = currentPath.Any(x => x.Aabb.Intersects(newP.Aabb));
            if (!any) {
                list.Add(newP);
            }
        }

        return list.ToArray();
    }

    private static string GetNamesOfPath(SearchPiece end) {
        if (end.Parent == null)
            return end.Piece.Name;
        return GetNamesOfPath(end.Parent) +  " " + end.Piece.Name;
    }
}
