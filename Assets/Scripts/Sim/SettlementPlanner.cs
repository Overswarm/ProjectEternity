using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Where civilizations grow: site scoring, autonomous settler waves driven by
    /// Expansion policy (yes, for the player too — you steer, society moves).
    /// </summary>
    public class SettlementPlanner
    {
        readonly Simulation sim;
        public SettlementPlanner(Simulation sim) { this.sim = sim; }

        public float ScoreSite(Tile tile, Civilization civ)
        {
            if (tile == null || tile.Water || tile.City != null) return float.MinValue;
            if (tile.OwnerCiv != -1 && tile.OwnerCiv != civ.Id) return float.MinValue;
            foreach (var c in sim.AllCities())
                if (WorldMap.Dist(c.Tile, tile) < Tuning.CityMinDistance) return float.MinValue;

            float score = 0f;
            foreach (var t in sim.World.InRadius(tile.X, tile.Y, Tuning.CityWorkRadius))
            {
                score += t.Food * 1.3f + t.Materials + t.Commerce;
                if (t.River) score += 0.5f;
            }
            return score;
        }

        public Tile FindBestSite(Civilization civ)
        {
            Tile best = null; float bestScore = 8f; // don't settle garbage
            foreach (var c in civ.Cities)
            {
                var area = sim.World.InRadius(c.Tile.X, c.Tile.Y, Tuning.SettleSearchRange);
                foreach (var t in area)
                {
                    float d = WorldMap.Dist(c.Tile, t);
                    if (d < Tuning.CityMinDistance) continue;
                    float s = ScoreSite(t, civ);
                    if (s == float.MinValue) continue;
                    s -= d * 0.4f; // near-ish is better
                    if (s > bestScore) { bestScore = s; best = t; }
                }
            }
            return best;
        }

        /// <summary>Autonomous expansion pulse, called for every living civ.</summary>
        public void Tick(float dt)
        {
            foreach (var civ in sim.Civs)
            {
                if (civ.Eliminated || civ.Cities.Count == 0 || civ.IsRebel) continue;
                float drive = civ.Policies.Expansion;
                if (drive < 0.3f) continue;
                if (civ.Gold < Tuning.SettlerGoldCost) continue;
                float pulse = drive * Tuning.SettlerPulseChance
                              * Mathf.Min(2.5f, 1f + civ.Cities.Count * 0.2f);
                if (!sim.Rng.Chance(pulse * dt)) continue;
                TrySpawnSettler(civ, manual: false);
            }
        }

        public bool TrySpawnSettler(Civilization civ, bool manual, City from = null)
        {
            // pick the source: fullest city that can spare people
            if (from == null)
            {
                float best = Tuning.SettlerMinCityPop;
                foreach (var c in civ.Cities)
                    if (c.Population > best) { best = c.Population; from = c; }
            }
            if (from == null || from.Population < Tuning.SettlerMinCityPop) return false;
            if (civ.Gold < Tuning.SettlerGoldCost) return false;

            var site = FindBestSite(civ);
            if (site == null) return false;
            var path = sim.World.FindPath(from.Tile, site);
            if (path == null) return false;

            civ.Gold -= Tuning.SettlerGoldCost;
            from.Population -= Tuning.SettlerPopCost;

            var settler = new Army
            {
                Id = sim.NextArmyId++,
                CivId = civ.Id,
                Strength = 15f,
                Mission = Mission.Settle,
                HomeCity = from,
                TargetTile = site,
                Path = path,
                PathIndex = 0,
                Pos = new Vector2(from.Tile.X, from.Tile.Y),
            };
            sim.Armies.Add(settler);
            sim.Log.Log(sim.Year,
                manual
                    ? $"Settlers set out from {from.Name} at the throne's urging."
                    : $"Restless families leave {from.Name} in search of new land.",
                new Color(0.85f, 0.85f, 0.7f), from.Tile.X, from.Tile.Y);
            return true;
        }

        public void ArriveSettler(Army a)
        {
            a.Dead = true;
            var civ = sim.GetCiv(a.CivId);
            var tile = a.TargetTile;

            if (ScoreSite(tile, civ) == float.MinValue)
            {
                // someone claimed it first; look nearby
                Tile alt = null; float best = float.MinValue;
                foreach (var t in sim.World.InRadius(tile.X, tile.Y, 4))
                {
                    float s = ScoreSite(t, civ);
                    if (s > best) { best = s; alt = t; }
                }
                if (alt == null || best == float.MinValue)
                {
                    if (a.HomeCity != null && !a.HomeCity.Razed)
                        a.HomeCity.Population += Tuning.SettlerPopCost * 0.8f;
                    return;
                }
                tile = alt;
            }
            sim.FoundCity(civ, tile);
        }
    }
}
