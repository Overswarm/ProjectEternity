using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// The soul of a city: six 0..1 axes that drift toward geography, national
    /// policy, and neighbors. Everything a city does autonomously flows from this.
    /// </summary>
    public class CityPersonality
    {
        public float Aggression = 0.3f;
        public float Piety = 0.4f;
        public float Openness = 0.4f;
        public float Industry = 0.35f;
        public float Commerce = 0.35f;
        public float Scholarship = 0.25f;

        public static readonly string[] AxisNames =
            { "Aggression", "Piety", "Openness", "Industry", "Commerce", "Scholarship" };

        public float Get(int i)
        {
            switch (i)
            {
                case 0: return Aggression;
                case 1: return Piety;
                case 2: return Openness;
                case 3: return Industry;
                case 4: return Commerce;
                default: return Scholarship;
            }
        }

        public void Set(int i, float v)
        {
            v = Mathf.Clamp01(v);
            switch (i)
            {
                case 0: Aggression = v; break;
                case 1: Piety = v; break;
                case 2: Openness = v; break;
                case 3: Industry = v; break;
                case 4: Commerce = v; break;
                default: Scholarship = v; break;
            }
        }

        public CityPersonality Clone() => (CityPersonality)MemberwiseClone();

        public void DriftToward(CityPersonality target, float amount)
        {
            for (int i = 0; i < 6; i++)
                Set(i, Mathf.MoveTowards(Get(i), target.Get(i), amount));
        }

        public void Nudge(int axis, float delta) => Set(axis, Get(axis) + delta);

        /// <summary>
        /// What the national law asks of a citizen, expressed in personality space —
        /// used both as a drift attractor and to measure law-vs-culture clash.
        /// </summary>
        public static CityPersonality FromPolicy(PolicySet p)
        {
            return new CityPersonality
            {
                Aggression = p.Militarism,
                Piety = p.Devotion,
                Openness = Mathf.Clamp01(p.Openness * 0.85f + (1f - p.Authority) * 0.35f - 0.1f),
                Industry = Mathf.Clamp01(0.3f + p.SpendInfra * 1.2f),
                Commerce = p.Market,
                Scholarship = Mathf.Clamp01(0.15f + p.SpendScience * 1.6f + (1f - p.Devotion) * 0.2f),
            };
        }

        /// <summary>Mean absolute distance across axes, 0..1.</summary>
        public float Distance(CityPersonality other)
        {
            float d = 0f;
            for (int i = 0; i < 6; i++) d += Mathf.Abs(Get(i) - other.Get(i));
            return d / 6f;
        }

        /// <summary>
        /// Geography writes the first draft of every city's story.
        /// Sampled from tiles around the city; recomputed when the land changes.
        /// </summary>
        public static CityPersonality GeographyAttractor(WorldMap map, Tile center, Rng rng)
        {
            int desert = 0, mountain = 0, forest = 0, water = 0, river = 0, lush = 0, total = 0;
            foreach (var t in map.InRadius(center.X, center.Y, Tuning.CityWorkRadius))
            {
                total++;
                switch (t.Biome)
                {
                    case Biome.Desert: desert++; break;
                    case Biome.Mountain:
                    case Biome.Hills: mountain++; break;
                    case Biome.Forest: forest++; break;
                    case Biome.Ocean:
                    case Biome.Coast: water++; break;
                    case Biome.Grassland: lush++; break;
                }
                if (t.River) river++;
            }
            float ft = Mathf.Max(1, total);
            float scarcity = Mathf.Clamp01((desert + mountain * 0.5f) / ft * 2.2f);
            float trade = Mathf.Clamp01((water + river) / ft * 2.5f);
            float woods = Mathf.Clamp01(forest / ft * 2.5f);
            float ore = Mathf.Clamp01(mountain / ft * 2.5f);
            float plenty = Mathf.Clamp01(lush / ft * 2.2f);

            var p = new CityPersonality
            {
                // hungry lands raid; happy valleys don't
                Aggression = Mathf.Clamp01(0.2f + scarcity * 0.55f - plenty * 0.25f),
                // the ancient world prays by default; hard lands pray harder, crossroads question
                Piety = Mathf.Clamp01(0.5f + scarcity * 0.3f - trade * 0.2f),
                Openness = Mathf.Clamp01(0.25f + trade * 0.55f - woods * 0.15f),
                Industry = Mathf.Clamp01(0.25f + ore * 0.5f + woods * 0.35f),
                Commerce = Mathf.Clamp01(0.2f + trade * 0.6f),
                Scholarship = Mathf.Clamp01(0.2f + trade * 0.25f + plenty * 0.15f),
            };
            // founders matter: a small permanent randomness
            for (int i = 0; i < 6; i++) p.Set(i, p.Get(i) + rng.Jitter(0.08f));
            return p;
        }
    }
}
