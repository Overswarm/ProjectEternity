using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// The world's own drama: plagues in crowded cities, famine crises,
    /// revolt ultimatums, golden ages. Mostly a trigger layer over DecisionSystem.
    /// </summary>
    public class EventsSystem
    {
        readonly Simulation sim;
        readonly Dictionary<int, float> cityCrisisCooldown = new Dictionary<int, float>();

        public EventsSystem(Simulation sim) { this.sim = sim; }

        bool CooledDown(City c, float years)
        {
            if (cityCrisisCooldown.TryGetValue(c.Id, out float until) && sim.Year < until) return false;
            cityCrisisCooldown[c.Id] = sim.Year + years;
            return true;
        }

        public void Tick(float dt)
        {
            foreach (var civ in sim.Civs)
            {
                if (civ.Eliminated) continue;

                foreach (var city in civ.Cities)
                {
                    // ---- plague: big pre-modern cities are demographic sinks.
                    // risk rises as a city presses against its health plateau,
                    // and medicine is what finally ends the cycle.
                    float health = civ.Mods.Health;
                    float crowd = city.Population / (Tuning.PlateauPop * health * health);
                    if (crowd > 0.6f)
                    {
                        float risk = Tuning.PlagueChance * dt * (crowd - 0.6f) * 3f;
                        if (sim.Rng.Chance(risk) && CooledDown(city, 30f))
                        {
                            city.Happiness -= 14f;
                            city.Unrest += 8f;
                            sim.Log.Log(sim.Year, $"Plague breaks out in {city.Name}!",
                                new Color(0.7f, 1f, 0.4f), city.Tile.X, city.Tile.Y);
                            sim.Decisions.OnPlague(city, civ);
                        }
                    }

                    // ---- famine crisis (bad enough to reach the throne) ----
                    if (city.SurplusRatio < -0.3f && city.Population > 3000f && CooledDown(city, 25f))
                        sim.Decisions.OnFamine(city, civ);

                    // ---- revolt ultimatum before the actual revolt ----
                    if (city.Unrest > 70f && CooledDown(city, 20f))
                        sim.Decisions.OnRevoltThreat(city, civ);
                }

                // ---- golden ages ----
                bool atWar = sim.Diplo.AtWarWithAnyone(civ.Id, sim.Civs.Count);
                if (civ.Cities.Count > 0 && civ.AvgHappiness >= Tuning.GoldenAgeHappyReq && !atWar)
                    civ.HappyStreakYears += dt;
                else
                    civ.HappyStreakYears = Mathf.Max(0f, civ.HappyStreakYears - dt * 2f);

                if (civ.HappyStreakYears >= Tuning.GoldenAgeStreak && !civ.GoldenAgeActive(sim.Year))
                {
                    civ.GoldenAgeUntil = sim.Year + Tuning.GoldenAgeDuration;
                    civ.HappyStreakYears = 0f;
                    sim.Log.Log(sim.Year, $"A GOLDEN AGE dawns for {civ.Name}!",
                        new Color(1f, 0.9f, 0.35f));
                    sim.Decisions.OnGoldenAge(civ);
                }
            }
        }
    }
}
