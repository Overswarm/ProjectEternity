using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Four ways to be remembered: Dominion, Ascension, Harmony, Legacy.
    /// Defeat: lose your last city.
    /// </summary>
    public class VictorySystem
    {
        readonly Simulation sim;
        float checkTimer;

        public bool GameOver;
        public Civilization Winner;
        public string VictoryType = "";
        public string Message = "";

        public VictorySystem(Simulation sim) { this.sim = sim; }

        public void Tick(float dt)
        {
            if (GameOver) return;

            // continuous accrual
            foreach (var civ in sim.Civs)
            {
                if (civ.Eliminated) continue;
                civ.Legacy += dt * (civ.TotalPop / 1000f * 0.05f
                                    + civ.Techs.Count * 0.02f
                                    + civ.Cities.Count * 0.05f
                                    + (civ.GoldenAgeActive(sim.Year) ? 0.5f : 0f));

                bool atWar = sim.Diplo.AtWarWithAnyone(civ.Id, sim.Civs.Count);
                bool harmonious = civ.Cities.Count >= Tuning.HarmonyMinCities
                                  && civ.AvgHappiness >= Tuning.HarmonyHappy && !atWar
                                  && civ.Era >= Era.Medieval;    // harmony is an achievement, not a lucky garden
                if (harmonious)
                    foreach (var c in civ.Cities)               // harmony means NOBODY is miserable —
                        if (c.Happiness < 60f) { harmonious = false; break; } // one plagued city breaks the streak
                civ.HarmonyStreakYears = harmonious ? civ.HarmonyStreakYears + dt : 0f;
            }

            checkTimer += dt;
            if (checkTimer < 1f) return;
            checkTimer = 0f;

            // ---- player defeat ----
            var player = sim.Player;
            if (player.Eliminated)
            {
                Declare(null, "Defeat",
                    "Your last city has fallen. Your people scatter into the pages of other nations' histories.");
                return;
            }

            float worldPop = 0f;
            foreach (var civ in sim.Civs) if (!civ.Eliminated) worldPop += civ.TotalPop;

            foreach (var civ in sim.Civs)
            {
                if (civ.Eliminated || civ.IsRebel) continue;

                if (worldPop > 0f && civ.TotalPop / worldPop >= Tuning.DominationShare && sim.Civs.Count > 1)
                { Declare(civ, "Dominion", $"{civ.Name} rules {Mathf.RoundToInt(civ.TotalPop / worldPop * 100f)}% of humanity."); return; }

                if (civ.Mods.AscensionDone)
                { Declare(civ, "Ascension", $"{civ.Name} completes the Ascension Project and steps beyond history."); return; }

                if (civ.HarmonyStreakYears >= Tuning.HarmonyYears)
                { Declare(civ, "Harmony", $"A century of contentment: {civ.Name} has built the good life, and kept it."); return; }
            }

            // ---- the Long Count ends: Legacy judges everyone ----
            if (sim.Year >= Tuning.EndYear)
            {
                Civilization best = null;
                foreach (var civ in sim.Civs)
                    if (!civ.Eliminated && (best == null || civ.Legacy > best.Legacy)) best = civ;
                Declare(best, "Legacy",
                    best == null ? "History ends with no one left to read it."
                                 : $"The Long Count ends. History remembers {best.Name} above all others (Legacy {Mathf.RoundToInt(best.Legacy)}).");
            }
        }

        void Declare(Civilization winner, string type, string message)
        {
            GameOver = true;
            Winner = winner;
            VictoryType = type;
            Message = message;
            sim.Log.Log(sim.Year, message, new Color(1f, 0.9f, 0.3f));
        }

        // ---- progress readouts for the UI ----
        public float DominionProgress(Civilization civ)
        {
            float worldPop = 0f;
            foreach (var c in sim.Civs) if (!c.Eliminated) worldPop += c.TotalPop;
            return worldPop > 0f ? Mathf.Clamp01(civ.TotalPop / worldPop / Tuning.DominationShare) : 0f;
        }

        public float AscensionProgress(Civilization civ)
        {
            float baseTechs = Mathf.Clamp01(civ.Techs.Count / (float)(TechCatalog.All.Count - 1));
            if (civ.Researching != null && civ.Researching.Era == Era.Transcendent)
                baseTechs = Mathf.Max(baseTechs, 0.9f + 0.1f * Mathf.Clamp01(civ.Knowledge / Tuning.AscensionCost));
            return civ.Mods.AscensionDone ? 1f : baseTechs;
        }

        public float HarmonyProgress(Civilization civ) =>
            Mathf.Clamp01(civ.HarmonyStreakYears / Tuning.HarmonyYears);
    }
}
