using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    public class WorldMap
    {
        public readonly int W, H;
        public readonly Tile[,] Tiles;

        public WorldMap(int w, int h)
        {
            W = w; H = h;
            Tiles = new Tile[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    Tiles[x, y] = new Tile { X = x, Y = y };
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public Tile Get(int x, int y) => Tiles[x, y];
        public Tile GetSafe(int x, int y) => InBounds(x, y) ? Tiles[x, y] : null;

        static readonly int[] DX4 = { 1, -1, 0, 0 };
        static readonly int[] DY4 = { 0, 0, 1, -1 };

        public IEnumerable<Tile> Neighbors4(Tile t)
        {
            for (int i = 0; i < 4; i++)
            {
                var n = GetSafe(t.X + DX4[i], t.Y + DY4[i]);
                if (n != null) yield return n;
            }
        }

        public IEnumerable<Tile> Neighbors8(Tile t)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = GetSafe(t.X + dx, t.Y + dy);
                    if (n != null) yield return n;
                }
        }

        /// <summary>All tiles within Chebyshev radius r (includes center).</summary>
        public List<Tile> InRadius(int cx, int cy, int r)
        {
            var list = new List<Tile>();
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    var t = GetSafe(cx + dx, cy + dy);
                    if (t != null) list.Add(t);
                }
            return list;
        }

        public static float Dist(Tile a, Tile b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// A* over land tiles (armies and settlers walk; water blocks).
        /// Returns null when unreachable. Grid is small; a simple open list suffices.
        /// </summary>
        public List<Tile> FindPath(Tile start, Tile goal)
        {
            if (start == null || goal == null || start.Water || goal.Water) return null;
            var open = new List<Tile> { start };
            var cameFrom = new Dictionary<Tile, Tile>();
            var gScore = new Dictionary<Tile, float> { [start] = 0f };
            var fScore = new Dictionary<Tile, float> { [start] = Dist(start, goal) };
            var closed = new HashSet<Tile>();

            int guard = W * H * 4;
            while (open.Count > 0 && guard-- > 0)
            {
                // extract lowest fScore
                Tile cur = open[0];
                float best = fScore[cur];
                for (int i = 1; i < open.Count; i++)
                {
                    float f = fScore[open[i]];
                    if (f < best) { best = f; cur = open[i]; }
                }
                if (cur == goal)
                {
                    var path = new List<Tile> { cur };
                    while (cameFrom.TryGetValue(cur, out var prev)) { cur = prev; path.Add(cur); }
                    path.Reverse();
                    return path;
                }
                open.Remove(cur);
                closed.Add(cur);

                foreach (var n in Neighbors8(cur))
                {
                    if (n.Water || closed.Contains(n)) continue;
                    float step = BiomeInfo.Get(n.Biome).MoveCost;
                    bool diag = n.X != cur.X && n.Y != cur.Y;
                    if (diag) step *= 1.4f;
                    float g = gScore[cur] + step;
                    if (!gScore.TryGetValue(n, out var old) || g < old)
                    {
                        cameFrom[n] = cur;
                        gScore[n] = g;
                        fScore[n] = g + Dist(n, goal);
                        if (!open.Contains(n)) open.Add(n);
                    }
                }
            }
            return null;
        }
    }
}
