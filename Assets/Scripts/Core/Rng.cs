using System;
using System.Collections.Generic;

namespace Eternity
{
    /// <summary>Seeded random helper — one instance drives worldgen + simulation.</summary>
    public class Rng
    {
        readonly Random r;
        public Rng(int seed) { r = new Random(seed); }

        public float Value => (float)r.NextDouble();
        public float Range(float min, float max) => min + (max - min) * Value;
        public int Range(int min, int maxExclusive) => r.Next(min, maxExclusive);
        public bool Chance(float p) => Value < p;
        public T Pick<T>(IReadOnlyList<T> list) => list[r.Next(list.Count)];

        /// <summary>Roughly-normal jitter around 0 with given half-width.</summary>
        public float Jitter(float halfWidth)
        {
            return (Value + Value - 1f) * halfWidth;
        }
    }
}
