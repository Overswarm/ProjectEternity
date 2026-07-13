using UnityEngine;

namespace Eternity
{
    public class Tile
    {
        public int X, Y;
        public Biome Biome;
        public float Elevation;    // 0..1 (sea level at Tuning.SeaLevel)
        public float Moisture;
        public float Temperature;
        public bool River;
        public float Fertility = 1f;   // per-tile jitter on yields
        public float ForestStock = 0f; // remaining timber; forests can be consumed

        public int OwnerCiv = -1;
        public City City;              // city standing on this tile, if any

        public bool Land => BiomeInfo.Get(Biome).Land;
        public bool Water => !Land;

        public float Food => (BiomeInfo.Get(Biome).Food + (River ? 0.5f : 0f)) * Fertility;
        public float Materials => BiomeInfo.Get(Biome).Materials * Fertility;
        public float Commerce => BiomeInfo.Get(Biome).Commerce + (River ? 0.4f : 0f);

        /// <summary>Industrial-era water power: rivers become engines.</summary>
        public float RiverIndustry => River ? 0.8f : 0f;

        public Vector2Int Pos => new Vector2Int(X, Y);
    }
}
