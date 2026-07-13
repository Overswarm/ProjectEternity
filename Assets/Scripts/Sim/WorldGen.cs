using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Procedural continent: fBm heightmap with radial falloff, latitude temperature,
    /// moisture noise, carved rivers, biome table, per-tile fertility jitter.
    /// </summary>
    public static class WorldGen
    {
        public static WorldMap Generate(int size, Rng rng)
        {
            var map = new WorldMap(size, size);
            float ox1 = rng.Range(0f, 999f), oy1 = rng.Range(0f, 999f);
            float ox2 = rng.Range(0f, 999f), oy2 = rng.Range(0f, 999f);

            // --- elevation & climate fields ---
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var t = map.Get(x, y);
                    float nx = (float)x / size, ny = (float)y / size;

                    float h = Fbm(ox1 + nx * 3.4f, oy1 + ny * 3.4f, 4);
                    // radial falloff -> one continent with ragged coasts
                    float dx = nx - 0.5f, dy = ny - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // 0 center, ~1 corner-ish
                    h = h * 0.9f + 0.25f - d * d * 0.55f;
                    t.Elevation = Mathf.Clamp01(h);

                    // latitude: poles at north/south map edges
                    float lat = Mathf.Abs(ny - 0.5f) * 2f;
                    t.Temperature = Mathf.Clamp01(1f - lat * 1.1f - Mathf.Max(0f, t.Elevation - 0.55f) * 0.9f
                                                  + Fbm(ox2 + nx * 5f, oy2 + ny * 5f, 2) * 0.15f - 0.05f);
                    t.Moisture = Mathf.Clamp01(Fbm(ox2 + nx * 4.2f, oy2 + ny * 4.2f, 3) * 1.15f - 0.05f);
                    t.Fertility = 0.8f + rng.Value * 0.4f;
                }
            }

            CarveRivers(map, rng);

            // --- biomes ---
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var t = map.Get(x, y);
                    t.Biome = PickBiome(t);
                    if (t.Biome == Biome.Forest) t.ForestStock = 1f;
                }
            }
            return map;
        }

        static Biome PickBiome(Tile t)
        {
            float sea = Tuning.SeaLevel;
            if (t.Elevation < sea - 0.05f) return Biome.Ocean;
            if (t.Elevation < sea) return Biome.Coast;
            if (t.Elevation > 0.82f) return Biome.Mountain;
            if (t.Temperature < 0.14f) return Biome.Snow;
            if (t.Temperature < 0.28f) return Biome.Tundra;
            if (t.Elevation > 0.68f) return Biome.Hills;
            float moist = t.Moisture + (t.River ? 0.18f : 0f);
            if (moist > 0.62f && t.Temperature > 0.35f) return Biome.Forest;
            if (moist > 0.44f) return Biome.Grassland;
            if (moist > 0.27f) return Biome.Plains;
            return Biome.Desert;
        }

        static void CarveRivers(WorldMap map, Rng rng)
        {
            for (int r = 0; r < Tuning.RiverCount; r++)
            {
                // source: a random high tile
                Tile src = null;
                for (int tries = 0; tries < 200; tries++)
                {
                    var c = map.Get(rng.Range(0, map.W), rng.Range(0, map.H));
                    if (c.Elevation > 0.62f && !c.River) { src = c; break; }
                }
                if (src == null) continue;

                var cur = src;
                for (int step = 0; step < 300; step++)
                {
                    cur.River = true;
                    // moisten the valley
                    foreach (var n in map.Neighbors8(cur))
                        n.Moisture = Mathf.Min(1f, n.Moisture + 0.06f);

                    // flow to the lowest neighbor; carve through flats
                    Tile low = null;
                    float lowE = float.MaxValue;
                    foreach (var n in map.Neighbors4(cur))
                    {
                        if (n.River && n != src) { low = n; break; } // merge into existing river
                        if (n.Elevation < lowE) { lowE = n.Elevation; low = n; }
                    }
                    if (low == null) break;
                    if (low.River) break;                      // merged
                    if (low.Elevation < Tuning.SeaLevel) break; // reached the sea
                    if (low.Elevation >= cur.Elevation)
                        low.Elevation = cur.Elevation - 0.004f; // carve
                    cur = low;
                }
            }
        }

        static float Fbm(float x, float y, int octaves)
        {
            float v = 0f, amp = 0.55f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                v += Mathf.PerlinNoise(x * freq, y * freq) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2.1f;
            }
            return v / norm;
        }
    }
}
