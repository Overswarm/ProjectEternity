using UnityEngine;

namespace Eternity
{
    public enum Biome { Ocean, Coast, Plains, Grassland, Forest, Desert, Hills, Mountain, Tundra, Snow }

    /// <summary>Static per-biome data: look, passability, and base yields.</summary>
    public static class BiomeInfo
    {
        public struct Data
        {
            public string Name;
            public Color Color;
            public float Food;       // base food units / yr when worked
            public float Materials;  // wood/stone/ore -> city development
            public float Commerce;   // gold
            public float MoveCost;   // army pathfinding cost
            public bool Land;        // can walk / settle
        }

        static readonly Data[] Table =
        {
            new Data { Name = "Ocean",     Color = new Color(0.09f, 0.20f, 0.40f), Food = 0.0f,  Materials = 0.0f, Commerce = 0.0f,  MoveCost = 99f,  Land = false },
            new Data { Name = "Coast",     Color = new Color(0.15f, 0.40f, 0.58f), Food = 0.7f,  Materials = 0.0f, Commerce = 0.45f, MoveCost = 99f,  Land = false },
            new Data { Name = "Plains",    Color = new Color(0.76f, 0.70f, 0.42f), Food = 0.9f,  Materials = 0.25f, Commerce = 0.2f, MoveCost = 1.0f, Land = true },
            new Data { Name = "Grassland", Color = new Color(0.40f, 0.62f, 0.27f), Food = 1.3f,  Materials = 0.2f,  Commerce = 0.2f, MoveCost = 1.0f, Land = true },
            new Data { Name = "Forest",    Color = new Color(0.15f, 0.40f, 0.18f), Food = 0.6f,  Materials = 1.1f,  Commerce = 0.1f, MoveCost = 1.5f, Land = true },
            new Data { Name = "Desert",    Color = new Color(0.86f, 0.77f, 0.50f), Food = 0.15f, Materials = 0.2f,  Commerce = 0.15f,MoveCost = 1.2f, Land = true },
            new Data { Name = "Hills",     Color = new Color(0.55f, 0.48f, 0.35f), Food = 0.5f,  Materials = 1.0f,  Commerce = 0.15f,MoveCost = 1.6f, Land = true },
            new Data { Name = "Mountain",  Color = new Color(0.47f, 0.44f, 0.46f), Food = 0.1f,  Materials = 1.5f,  Commerce = 0.0f, MoveCost = 3.0f, Land = true },
            new Data { Name = "Tundra",    Color = new Color(0.56f, 0.58f, 0.54f), Food = 0.35f, Materials = 0.3f,  Commerce = 0.1f, MoveCost = 1.3f, Land = true },
            new Data { Name = "Snow",      Color = new Color(0.86f, 0.88f, 0.91f), Food = 0.05f, Materials = 0.1f,  Commerce = 0.0f, MoveCost = 2.0f, Land = true },
        };

        public static Data Get(Biome b) => Table[(int)b];
    }
}
