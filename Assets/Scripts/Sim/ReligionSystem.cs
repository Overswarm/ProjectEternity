using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Faiths found themselves in pious cities, spread along openness and proximity,
    /// and respond to state adoption/suppression. Share id -1 = folk beliefs.
    /// </summary>
    public class ReligionSystem
    {
        public readonly List<Religion> Religions = new List<Religion>();
        readonly Simulation sim;
        float spreadTimer;

        static readonly Color[] FaithColors =
        {
            new Color(0.95f, 0.85f, 0.3f), new Color(0.75f, 0.4f, 0.9f),
            new Color(0.3f, 0.85f, 0.8f), new Color(0.95f, 0.5f, 0.25f),
            new Color(0.55f, 0.75f, 0.3f),
        };

        public ReligionSystem(Simulation sim) { this.sim = sim; }

        public Religion Get(int id) => Religions[id];

        public void Tick(float dt)
        {
            TryFoundFaiths(dt);

            spreadTimer += dt;
            if (spreadTimer >= 1f)
            {
                float step = spreadTimer;
                spreadTimer = 0f;
                SpreadPass(step);
            }
        }

        void TryFoundFaiths(float dt)
        {
            if (Religions.Count >= Tuning.MaxReligions) return;
            if (sim.Year < Tuning.StartYear + 150f) return;

            foreach (var city in sim.AllCities())
            {
                bool pious = city.Pers.Piety >= Tuning.ProphetPiety
                             || city.Districts[(int)District.Temples] >= 3f;
                if (!pious || city.Population < 1500f) continue;
                int dom = city.DominantFaith(out float share);
                if (dom >= 0 && share > 0.5f) continue; // already claimed by a faith
                if (!sim.Rng.Chance(0.03f * city.Pers.Piety * dt)) continue;

                var faith = new Religion
                {
                    Id = Religions.Count,
                    Name = sim.Names.FaithName(),
                    Color = FaithColors[Religions.Count % FaithColors.Length],
                    Fervor = Mathf.Clamp01(city.Pers.Piety * 0.6f + city.Pers.Aggression * 0.4f + sim.Rng.Jitter(0.2f)),
                    HolyCityId = city.Id,
                    FoundedYear = sim.Year,
                };
                Religions.Add(faith);
                SetShare(city, faith.Id, 0.6f);

                var civ = sim.GetCiv(city.CivId);
                sim.Log.Log(sim.Year,
                    $"A prophet rises in {city.Name}: \"{faith.Name}\" is born ({faith.Temperament}).",
                    faith.Color, city.Tile.X, city.Tile.Y);
                sim.Decisions.OnFaithFounded(civ, faith, city);
                if (Religions.Count >= Tuning.MaxReligions) return;
            }
        }

        void SpreadPass(float dt)
        {
            var cities = new List<City>(sim.AllCities());

            foreach (var city in cities)
            {
                var civ = sim.GetCiv(city.CivId);

                // neighbor pressure
                foreach (var other in cities)
                {
                    if (other == city) continue;
                    float d = WorldMap.Dist(city.Tile, other.Tile);
                    if (d > Tuning.FaithRange) continue;

                    foreach (var kv in other.Faiths)
                    {
                        if (kv.Key < 0 || kv.Value < 0.25f) continue;
                        var faith = Get(kv.Key);
                        float receptivity = 0.4f + city.Pers.Openness * 0.6f + city.Pers.Piety * 0.3f;
                        if (civ.Suppressed.Contains(kv.Key)) receptivity *= 0.25f;
                        float gain = Tuning.FaithSpreadRate * dt * kv.Value * (0.5f + faith.Fervor) * receptivity
                                     * (1f - d / (Tuning.FaithRange + 2f));
                        AddShare(city, kv.Key, gain);
                    }
                }

                // state religion pressure at home
                if (civ.StateReligion >= 0)
                    AddShare(city, civ.StateReligion, Tuning.StateFaithPressure * dt);

                // suppression bleeds a faith out (and its people seethe — handled in City happiness)
                foreach (int sup in civ.Suppressed)
                    if (city.Faiths.TryGetValue(sup, out float s) && s > 0f)
                        city.Faiths[sup] = Mathf.Max(0f, s - Tuning.SuppressDecay * dt);

                Normalize(city);
            }
        }

        static void AddShare(City city, int faithId, float gain)
        {
            city.Faiths.TryGetValue(faithId, out float cur);
            city.Faiths[faithId] = cur + gain;
        }

        static void SetShare(City city, int faithId, float share)
        {
            city.Faiths[faithId] = share;
            Normalize(city);
        }

        /// <summary>Non-folk shares sum &lt;= 1; folk beliefs (-1) hold the remainder.</summary>
        static void Normalize(City city)
        {
            float sum = 0f;
            foreach (var kv in city.Faiths)
                if (kv.Key >= 0) sum += kv.Value;

            if (sum > 1f)
            {
                var keys = new List<int>(city.Faiths.Keys);
                foreach (var k in keys)
                    if (k >= 0) city.Faiths[k] /= sum;
                sum = 1f;
            }
            city.Faiths[-1] = 1f - sum;
        }

        /// <summary>Share of a faith across a whole civ, population-weighted.</summary>
        public float CivShare(Civilization civ, int faithId)
        {
            float s = 0f, w = 0f;
            foreach (var c in civ.Cities)
            {
                c.Faiths.TryGetValue(faithId, out float share);
                s += share * c.Population;
                w += c.Population;
            }
            return w > 0f ? s / w : 0f;
        }
    }
}
