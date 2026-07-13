using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Marching stacks, raids, sieges, conquest — and the fallout (relations, wars,
    /// resentment). Raids can be ordered by leaders or launched by cities themselves.
    /// </summary>
    public class MilitarySystem
    {
        readonly Simulation sim;
        public MilitarySystem(Simulation sim) { this.sim = sim; }

        static readonly Color WarColor = new Color(1f, 0.35f, 0.3f);

        public Army LaunchArmy(City from, City target, Mission mission, float strength, bool autonomous = false)
        {
            if (strength < 5f || from.Razed || target.Razed) return null;
            var path = sim.World.FindPath(from.Tile, target.Tile);
            if (path == null) return null;

            var civ = sim.GetCiv(from.CivId);
            from.Garrison = Mathf.Max(0f, from.Garrison - strength);

            var army = new Army
            {
                Id = sim.NextArmyId++,
                CivId = from.CivId,
                Strength = strength,
                Mission = mission,
                TargetCity = target,
                HomeCity = from,
                Autonomous = autonomous,
                Path = path,
                PathIndex = 0,
                Pos = new Vector2(from.Tile.X, from.Tile.Y),
            };
            sim.Armies.Add(army);

            if (mission == Mission.Conquer && !sim.Diplo.AtWar(from.CivId, target.CivId))
                sim.DeclareWar(civ, sim.GetCiv(target.CivId), "an invasion");

            if (autonomous)
                sim.Log.Log(sim.Year,
                    $"The warlords of {from.Name}, hungry and proud, ride against {target.Name} — nobody asked the throne.",
                    WarColor, from.Tile.X, from.Tile.Y);
            else
                sim.Log.Log(sim.Year,
                    $"An army of {civ.Name} marches from {from.Name} to {(mission == Mission.Raid ? "raid" : "conquer")} {target.Name}.",
                    WarColor, from.Tile.X, from.Tile.Y);
            return army;
        }

        public void Tick(float dt)
        {
            for (int i = sim.Armies.Count - 1; i >= 0; i--)
            {
                var a = sim.Armies[i];
                if (a.Dead) { sim.Armies.RemoveAt(i); continue; }

                a.Strength -= a.Strength * Tuning.ArmyAttrition * dt;
                if (a.Strength < 3f && a.Mission != Mission.Settle)
                { a.Dead = true; sim.Armies.RemoveAt(i); continue; }

                // retarget sanity: target razed or now friendly
                if ((a.Mission == Mission.Raid || a.Mission == Mission.Conquer) &&
                    (a.TargetCity == null || a.TargetCity.Razed || a.TargetCity.CivId == a.CivId))
                    ConvertToReturn(a);

                MoveAlongPath(a, dt);
            }
        }

        void MoveAlongPath(Army a, float dt)
        {
            if (a.Path == null || a.PathIndex >= a.Path.Count) { Arrive(a); return; }

            float budget = Tuning.ArmySpeed * dt;
            while (budget > 0f && a.PathIndex < a.Path.Count)
            {
                var next = a.Path[a.PathIndex];
                var target = new Vector2(next.X, next.Y);
                float d = Vector2.Distance(a.Pos, target);
                float cost = BiomeInfo.Get(next.Biome).MoveCost;
                float reach = budget / Mathf.Max(0.2f, cost);
                if (reach >= d)
                {
                    a.Pos = target;
                    budget -= d * cost;
                    a.PathIndex++;
                }
                else
                {
                    a.Pos = Vector2.MoveTowards(a.Pos, target, reach);
                    budget = 0f;
                }
            }
            if (a.PathIndex >= a.Path.Count) Arrive(a);
        }

        void ConvertToReturn(Army a)
        {
            a.Mission = Mission.Return;
            a.TargetCity = null;
            var cur = a.CurrentTile(sim.World);
            if (a.HomeCity != null && !a.HomeCity.Razed && a.HomeCity.CivId == a.CivId && cur != null)
                a.Path = sim.World.FindPath(cur, a.HomeCity.Tile);
            else
                a.Path = null;
            a.PathIndex = 0;
            if (a.Path == null) a.Dead = true; // scattered
        }

        void Arrive(Army a)
        {
            switch (a.Mission)
            {
                case Mission.Raid: ResolveRaid(a); break;
                case Mission.Conquer: ResolveConquest(a); break;
                case Mission.Return:
                    if (a.HomeCity != null && !a.HomeCity.Razed && a.HomeCity.CivId == a.CivId)
                        a.HomeCity.Garrison += a.Strength;
                    a.Dead = true;
                    break;
                case Mission.Settle:
                    sim.Settlement.ArriveSettler(a);
                    break;
            }
        }

        void ResolveRaid(Army a)
        {
            var city = a.TargetCity;
            var attCiv = sim.GetCiv(a.CivId);
            var defCiv = sim.GetCiv(city.CivId);

            float att = a.Strength * attCiv.Mods.Military * sim.Rng.Range(0.8f, 1.2f);
            float def = city.Defense(defCiv) * sim.Rng.Range(0.8f, 1.2f);

            sim.Diplo.ChangeRelation(a.CivId, city.CivId, -25f);

            if (att > def)
            {
                float steal = Mathf.Min(defCiv.Gold * 0.25f, 20f + att * 0.4f);
                defCiv.Gold -= steal;
                attCiv.Gold += steal;
                int d = sim.Rng.Range(0, 6);
                if (city.Districts[d] >= 1f) city.Districts[d] -= 1f;
                city.Population *= 0.96f;
                city.Garrison *= 0.7f;
                city.RecentRaid = 1f;

                sim.Log.Log(sim.Year,
                    $"{city.Name} is sacked by raiders from {a.HomeCity?.Name ?? attCiv.Name}! {Mathf.RoundToInt(steal)} gold carried off.",
                    WarColor, city.Tile.X, city.Tile.Y);

                a.Strength *= 0.7f;
                ConvertToReturn(a);
            }
            else
            {
                city.Garrison *= 0.8f;
                sim.Log.Log(sim.Year,
                    $"The walls of {city.Name} hold — the raiders are cut down.",
                    new Color(0.6f, 0.85f, 0.6f), city.Tile.X, city.Tile.Y);
                a.Dead = true;
            }
        }

        void ResolveConquest(Army a)
        {
            var city = a.TargetCity;
            var attCiv = sim.GetCiv(a.CivId);
            var defCiv = sim.GetCiv(city.CivId);

            float att = a.Strength * attCiv.Mods.Military * sim.Rng.Range(0.8f, 1.2f);
            float def = city.Defense(defCiv) * Tuning.ConquerDefenseBonus * sim.Rng.Range(0.8f, 1.2f);

            if (att > def)
            {
                TransferCity(city, defCiv, attCiv);
                city.Garrison = a.Strength * 0.5f;
                city.Population *= 0.9f;
                city.Unrest = 70f;                              // occupation breeds rebellion
                city.Happiness = Mathf.Min(city.Happiness, 25f);
                city.RecentRaid = 1f;
                attCiv.WarWeariness = Mathf.Min(1f, attCiv.WarWeariness + 0.15f); // conquest exhausts the conqueror
                a.Dead = true;

                sim.Log.Log(sim.Year,
                    $"{city.Name} falls! The banners of {attCiv.Name} fly over its walls.",
                    WarColor, city.Tile.X, city.Tile.Y);
                sim.CheckElimination(defCiv);
            }
            else
            {
                city.Garrison *= 0.75f;
                a.Dead = true;
                sim.Log.Log(sim.Year,
                    $"The siege of {city.Name} is broken; {attCiv.Name}'s host is destroyed.",
                    new Color(0.6f, 0.85f, 0.6f), city.Tile.X, city.Tile.Y);
            }
        }

        public void TransferCity(City city, Civilization from, Civilization to)
        {
            from.Cities.Remove(city);
            to.Cities.Add(city);
            city.CivId = to.Id;
            city.SelfRule = false;

            foreach (var t in sim.World.InRadius(city.Tile.X, city.Tile.Y, Tuning.CityWorkRadius))
                if (t.OwnerCiv == from.Id) t.OwnerCiv = to.Id;
            sim.WorldDirty = true;
        }

        public float TotalStrength(Civilization civ)
        {
            float s = 0f;
            foreach (var c in civ.Cities) s += c.Garrison;
            foreach (var a in sim.Armies) if (a.CivId == civ.Id && a.Mission != Mission.Settle) s += a.Strength;
            return s * civ.Mods.Military;
        }
    }
}
