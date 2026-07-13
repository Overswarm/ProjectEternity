using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    public enum District { Farms, Workshops, Markets, Academies, Temples, Walls }

    public struct HappyFactor
    {
        public string Name;
        public float Value;
        public HappyFactor(string n, float v) { Name = n; Value = v; }
    }

    /// <summary>
    /// Cities are the protagonists. They grow, invest, drift, pray, riot, and raid
    /// on their own. The player only ever applies pressure.
    /// </summary>
    public class City
    {
        public int Id;
        public string Name;
        public int CivId;
        public Tile Tile;
        public float Founded;

        public float Population;
        public float Happiness = 55f;
        public float Unrest;
        public float Garrison = 10f;

        public CityPersonality Pers;
        public CityPersonality GeoAttractor;

        public readonly float[] Districts = new float[6];

        // religion shares; key -1 = folk beliefs. Kept normalized by ReligionSystem.
        public readonly Dictionary<int, float> Faiths = new Dictionary<int, float> { { -1, 1f } };

        public bool SelfRule;               // granted autonomy (revolt concession)
        public float RestrainedUntil = float.MinValue;
        public float RecentRaid;            // 0..1, decays; misery + wall pressure
        public float RaidCooldown = 10f;
        public bool Razed;                  // marked for removal

        // cached per-tick outputs (read by UI)
        public float FoodYield, MatYield, GoldYield, SciYield, FoodNeed, PopCap;
        public float SurplusRatio;
        public string SpecName = "Young Settlement";
        public readonly List<HappyFactor> HappyBreakdown = new List<HappyFactor>();

        List<Tile> worked;                  // best worked tiles, refreshed on a timer
        float workRefresh, neighborDrift = 5f, geoRefresh = 20f;
        float emigrated;                    // cumulative, for sparse logging

        public float TotalDistricts
        {
            get { float s = 0f; for (int i = 0; i < 6; i++) s += Districts[i]; return s; }
        }

        public float Defense(Civilization civ) =>
            Garrison * (1f + Districts[(int)District.Walls] * Tuning.WallDefense) * civ.Mods.Military;

        public int DominantFaith(out float share)
        {
            int best = -1; share = 0f;
            foreach (var kv in Faiths)
                if (kv.Value > share) { share = kv.Value; best = kv.Key; }
            return best;
        }

        public void Tick(Simulation sim, Civilization civ, float dt)
        {
            if (Razed) return;
            RefreshWorkedTiles(sim, dt);

            float golden = civ.GoldenAgeActive(sim.Year) ? Tuning.GoldenAgeBonus : 1f;
            var mods = civ.Mods;

            // ---- yields from worked land ----
            float sumFood = 0f, sumMat = 0f, sumCom = 0f, riverPower = 0f;
            foreach (var t in worked)
            {
                sumFood += t.Food; sumMat += t.Materials; sumCom += t.Commerce;
                riverPower += t.RiverIndustry;
            }
            bool industrialRivers = civ.Era >= Era.Industrial;

            float specFood = SpecName == "Breadbasket" ? 1.1f : 1f;
            float specInd = SpecName == "Forge City" ? 1.1f : 1f;
            float specGold = (SpecName == "Free Port" || SpecName == "Market City") ? 1.1f : 1f;
            float specSci = SpecName == "Academy City" ? 1.1f : 1f;

            FoodYield = sumFood * (1f + Districts[(int)District.Farms] * Tuning.FarmYield) * mods.Food * golden * specFood;
            FoodNeed = Population / 1000f * Tuning.FoodPerThousand;
            SurplusRatio = (FoodYield - FoodNeed) / Mathf.Max(FoodNeed, 0.2f);

            MatYield = (sumMat + Population / 1000f * (0.4f + Pers.Industry) + (industrialRivers ? riverPower : 0f))
                       * (1f + Districts[(int)District.Workshops] * Tuning.WorkshopYield) * mods.Industry * golden * specInd;

            GoldYield = (sumCom + Population / 1000f * (0.25f + Pers.Commerce * 1.1f))
                        * (1f + Districts[(int)District.Markets] * Tuning.MarketYield) * mods.Commerce
                        * (0.8f + 0.4f * civ.Policies.Openness) * golden * specGold;

            int domFaith = DominantFaith(out float domShare);
            float fervor = domFaith >= 0 ? sim.Religions.Get(domFaith).Fervor : 0.2f;
            float faithSciDrag = 1f - fervor * Pers.Piety * 0.25f;

            SciYield = Population / 1000f * (0.12f + Pers.Scholarship * 0.6f)
                       * (1f + Districts[(int)District.Academies] * Tuning.AcademyYield) * mods.Science
                       * (0.85f + 0.3f * civ.Policies.Openness) * faithSciDrag * golden * specSci;

            civ.Gold += GoldYield * dt * (SelfRule ? 0.5f : 1f);
            civ.Knowledge += SciYield * dt;

            // ---- district investment (the city builds itself) ----
            InvestDistricts(civ, dt);

            // ---- population ----
            PopCap = Tuning.BasePopCap * (1f + TotalDistricts * Tuning.PopCapPerDevLevel) * mods.PopCap;
            float happyFactor = 0.5f + Happiness / 100f;
            float growth;
            if (SurplusRatio >= 0f)
            {
                growth = Tuning.BaseGrowth * mods.Health * happyFactor
                         + Mathf.Clamp01(SurplusRatio) * Tuning.SurplusGrowth;
                // the ancient ceiling: big cities are disease pits until medicine
                // raises the asymptote — this is what makes eras take eras
                float ceiling = Tuning.PlateauPop * mods.Health * mods.Health;
                growth *= Mathf.Max(0f, 1f - Population / ceiling);
            }
            else
                growth = SurplusRatio * Tuning.StarvationRate; // famine deaths
            if (Population > PopCap)
                growth -= (Population / PopCap - 1f) * 0.08f;
            Population = Mathf.Max(Tuning.MinPop, Population * (1f + growth * dt));

            // ---- happiness (computed as legible contributions) ----
            UpdateHappiness(sim, civ, domFaith, domShare, fervor, dt);

            // ---- behavior from mood ----
            Emigration(sim, dt);
            UnrestAndRevolt(sim, civ, dt);

            // ---- garrison (soldiers cost gold; unpaid soldiers go home) ----
            bool mobilized = sim.Diplo.AtWarWithAnyone(civ.Id, sim.Civs.Count);
            float gtarget = Population * Tuning.GarrisonRatio
                            * (0.4f + civ.Policies.Militarism)
                            * (0.5f + civ.Policies.SpendMilitary * 2f) * mods.Military
                            * (mobilized ? Tuning.WartimeMobilization : 1f);
            Garrison += (gtarget - Garrison) * Mathf.Min(1f, Tuning.GarrisonRegen * dt);
            civ.Gold -= Garrison * Tuning.GarrisonUpkeep * dt;
            if (civ.Gold < 0f) { civ.Gold = 0f; Garrison *= 1f - 0.05f * dt; }

            // ---- culture drift: geography, law, feedback loops ----
            Pers.DriftToward(GeoAttractor, Tuning.GeoDriftRate * dt);
            Pers.DriftToward(CityPersonality.FromPolicy(civ.Policies),
                             Tuning.PolicyDriftRate * dt * (0.5f + civ.Policies.Authority * 0.8f));
            if (Districts[(int)District.Temples] > 2f) Pers.Nudge(1, 0.006f * dt);
            if (Districts[(int)District.Academies] > 2f) { Pers.Nudge(5, 0.006f * dt); Pers.Nudge(1, -0.003f * dt); }

            NeighborDrift(sim, dt);
            Deforest(sim, dt);
            ClaimBorders(sim);
            AutonomousRaid(sim, civ, dt);

            RecentRaid = Mathf.Max(0f, RecentRaid - 0.12f * dt);
            UpdateSpecialization();
        }

        void RefreshWorkedTiles(Simulation sim, float dt)
        {
            workRefresh -= dt;
            if (worked != null && workRefresh > 0f) return;
            workRefresh = 10f;

            var all = sim.World.InRadius(Tile.X, Tile.Y, Tuning.CityWorkRadius);
            all.RemoveAll(t => t.Biome == Biome.Ocean);
            all.Sort((a, b) =>
                (b.Food * 1.2f + b.Materials + b.Commerce).CompareTo(a.Food * 1.2f + a.Materials + a.Commerce));
            int n = Mathf.Min(all.Count, Tuning.BaseWorkTiles + (int)(Population / Tuning.PopPerWorkTile));
            worked = all.GetRange(0, n);

            geoRefresh -= 10f;
            if (geoRefresh <= 0f)
            {
                geoRefresh = 20f;
                // the land changed (deforestation etc.) -> geography attractor shifts
                GeoAttractor = CityPersonality.GeographyAttractor(sim.World, Tile, sim.Rng);
            }
        }

        void InvestDistricts(Civilization civ, float dt)
        {
            float invest = MatYield * dt * (0.7f + civ.Policies.SpendInfra * 1.5f);
            float hunger = SurplusRatio < 0.3f ? (0.3f - SurplusRatio) * 3f : 0f;
            float threat = RecentRaid * 1.5f;

            float[] w =
            {
                0.55f + hunger,                                                  // Farms
                0.30f + Pers.Industry,                                           // Workshops
                0.20f + Pers.Commerce,                                           // Markets
                0.15f + Pers.Scholarship * 0.8f + civ.Policies.SpendScience * 0.8f, // Academies
                0.10f + Pers.Piety * 0.8f + civ.Policies.Devotion * 0.4f,        // Temples
                0.10f + Pers.Aggression * 0.4f + threat + civ.Policies.SpendMilitary * 0.6f, // Walls
            };
            float sum = 0f;
            for (int i = 0; i < 6; i++) sum += w[i];
            for (int i = 0; i < 6; i++)
            {
                float lvl = Districts[i];
                float cost = (lvl + 1f) * (lvl + 1f) * Tuning.DistrictCostBase;
                Districts[i] = lvl + invest * (w[i] / sum) / cost;
            }
        }

        void UpdateHappiness(Simulation sim, Civilization civ, int domFaith, float domShare, float fervor, float dt)
        {
            HappyBreakdown.Clear();
            void F(string n, float v) { if (Mathf.Abs(v) > 0.05f) HappyBreakdown.Add(new HappyFactor(n, v)); }

            float target = Tuning.HappyBase;
            F("Base", Tuning.HappyBase);

            float food = SurplusRatio >= 0f
                ? Mathf.Min(SurplusRatio, 1f) * Tuning.FoodHappy
                : -Tuning.FamineUnhappy * Mathf.Min(1f, -SurplusRatio * 2f);
            target += food; F(SurplusRatio >= 0 ? "Full bellies" : "FAMINE", food);

            // life before medicine is short and hard; fades as Health tech compounds
            float harsh = -Tuning.HarshAgeUnhappy * Mathf.Clamp01(1.6f - civ.Mods.Health);
            target += harsh; F("A harsh age", harsh);

            float faith = domFaith >= 0
                ? domShare * (0.5f + fervor) * Tuning.FaithUnityHappy
                : 2f;
            target += faith; F("Faith unity", faith);

            float suppressed = 0f;
            foreach (var kv in Faiths)
                if (kv.Key >= 0 && civ.Suppressed.Contains(kv.Key) && kv.Value > 0.1f)
                    suppressed -= kv.Value * 2f * Tuning.SuppressedFaithUnhappy;
            if (suppressed != 0f) { target += suppressed; F("Persecuted faithful", suppressed); }

            float clash = Pers.Distance(CityPersonality.FromPolicy(civ.Policies)) - 0.25f;
            float law = clash < 0f
                ? -clash / 0.25f * Tuning.PolicyAlignHappy
                : -clash / 0.5f * Tuning.PolicyClashUnhappy;
            target += law; F(clash < 0 ? "The law fits us" : "The law chafes", law);

            if (civ.WarWeariness > 0.02f)
            { float v = -civ.WarWeariness * Tuning.WarWearyUnhappy; target += v; F("War weariness", v); }

            if (RecentRaid > 0.02f)
            { float v = -RecentRaid * Tuning.RaidUnhappy; target += v; F("Raided!", v); }

            if (Population > PopCap * 0.9f)
            {
                float v = -Mathf.Clamp01(Population / PopCap - 0.9f) * 2f * Tuning.CrowdUnhappy;
                target += v; F("Overcrowding", v);
            }

            float welfare = (civ.Policies.SpendWelfare - 0.25f) * 2f * Tuning.WelfareHappy;
            target += welfare; F(welfare >= 0 ? "Welfare" : "Neglect", welfare);

            if (civ.GoldenAgeActive(sim.Year)) { target += Tuning.GoldenAgeHappy; F("GOLDEN AGE", Tuning.GoldenAgeHappy); }
            if (civ.Mods.Happiness != 0f) { target += civ.Mods.Happiness; F("Modern comforts", civ.Mods.Happiness); }

            float temples = Districts[(int)District.Temples];
            if (temples > 0.5f && Pers.Piety > 0.3f)
            { float v = Mathf.Min(5f, temples * 0.8f); target += v; F("Temple comfort", v); }

            if (SelfRule) { target += 4f; F("Self rule", 4f); }

            target = Mathf.Clamp(target, 0f, 100f);
            Happiness += (target - Happiness) * Mathf.Min(1f, Tuning.HappinessLerp * dt);
        }

        void Emigration(Simulation sim, float dt)
        {
            if (Happiness >= Tuning.EmigrateBelow) return;
            float frac = (Tuning.EmigrateBelow - Happiness) / Tuning.EmigrateBelow * Tuning.EmigrateRate;
            float movers = Population * frac * dt;
            if (movers < 0.5f) return;

            City best = null; float bestScore = Happiness + 8f;
            foreach (var c in sim.AllCities())
            {
                if (c == this || c.Razed) continue;
                if (WorldMap.Dist(c.Tile, Tile) > 14f) continue;
                if (c.Happiness > bestScore) { bestScore = c.Happiness; best = c; }
            }
            Population -= movers;
            if (best != null)
            {
                best.Population += movers * 0.85f;
                emigrated += movers;
                if (emigrated > 400f)
                {
                    emigrated = 0f;
                    sim.Log.Log(sim.Year, $"People are abandoning {Name} for {best.Name}.",
                        new Color(1f, 0.75f, 0.4f), Tile.X, Tile.Y);
                }
            }
        }

        void UnrestAndRevolt(Simulation sim, Civilization civ, float dt)
        {
            if (Happiness < Tuning.UnrestBelow)
                Unrest += (Tuning.UnrestBelow - Happiness) * Tuning.UnrestRate * dt;
            else
                Unrest -= Tuning.UnrestDecay * dt * (1f + civ.Policies.Authority); // order suppresses unrest
            Unrest = Mathf.Clamp(Unrest, 0f, 110f);

            if (Unrest > Tuning.RiotAt && sim.Rng.Chance(0.4f * dt))
            {
                int i = sim.Rng.Range(0, 6);
                if (Districts[i] >= 1f)
                {
                    Districts[i] -= 1f;
                    sim.Log.Log(sim.Year, $"Riots in {Name}! The {(District)i} district burns.",
                        new Color(1f, 0.45f, 0.3f), Tile.X, Tile.Y);
                }
                Unrest -= 20f;
            }

            if (Unrest >= Tuning.RevoltAt)
            {
                Unrest = 40f;
                sim.TriggerRevolt(this);
            }
        }

        void NeighborDrift(Simulation sim, float dt)
        {
            neighborDrift -= dt;
            if (neighborDrift > 0f) return;
            neighborDrift = 5f;

            var avg = new CityPersonality();
            for (int i = 0; i < 6; i++) avg.Set(i, 0f);
            int n = 0;
            foreach (var c in sim.AllCities())
            {
                if (c == this || WorldMap.Dist(c.Tile, Tile) > 8f) continue;
                for (int i = 0; i < 6; i++) avg.Set(i, avg.Get(i) + c.Pers.Get(i));
                n++;
            }
            if (n == 0) return;
            for (int i = 0; i < 6; i++) avg.Set(i, avg.Get(i) / n);
            Pers.DriftToward(avg, Tuning.NeighborDriftRate * 5f * (0.5f + Pers.Openness));
        }

        void Deforest(Simulation sim, float dt)
        {
            float shops = Districts[(int)District.Workshops];
            if (shops < 1.5f || worked == null) return;
            foreach (var t in worked)
            {
                if (t.Biome != Biome.Forest) continue;
                t.ForestStock -= Tuning.DeforestRate * shops * dt;
                if (t.ForestStock <= 0f)
                {
                    t.Biome = t.Moisture > 0.4f ? Biome.Grassland : Biome.Plains;
                    t.ForestStock = 0f;
                    sim.WorldDirty = true;
                    workRefresh = 0f; // re-evaluate the land
                    sim.Log.Log(sim.Year, $"The old woods around {Name} have fallen silent — cleared for timber and fields.",
                        new Color(0.8f, 0.7f, 0.45f), t.X, t.Y);
                }
            }
        }

        void ClaimBorders(Simulation sim)
        {
            int r = 1 + (Population > 4000f ? 1 : 0) + (Population > 16000f ? 1 : 0);
            foreach (var t in sim.World.InRadius(Tile.X, Tile.Y, r))
            {
                if (t.OwnerCiv == -1 && t.Land)
                {
                    t.OwnerCiv = CivId;
                    sim.WorldDirty = true;
                }
            }
        }

        void AutonomousRaid(Simulation sim, Civilization civ, float dt)
        {
            RaidCooldown -= dt;
            if (RaidCooldown > 0f) return;
            RaidCooldown = sim.Rng.Range(Tuning.RaidCooldownMin, Tuning.RaidCooldownMax);

            if (Pers.Aggression < Tuning.AggressionRaidAt) return;
            if (civ.Stance == MilitaryStance.Defensive) return;
            if (sim.Year < RestrainedUntil) return;
            if (Garrison < 25f) return;

            bool hungry = SurplusRatio < 0.35f;
            float chance = (Pers.Aggression - 0.4f) * (hungry ? 1.6f : 0.6f)
                           * (civ.Stance == MilitaryStance.Aggressive ? 1.4f : 1f);
            if (!sim.Rng.Chance(chance)) return;

            // find prey: a weaker foreign city in reach (prefer those we're at war with)
            City prey = null; float preyScore = float.MinValue;
            foreach (var c in sim.AllCities())
            {
                if (c.CivId == CivId || c.Razed) continue;
                float d = WorldMap.Dist(c.Tile, Tile);
                if (d > Tuning.RaidRange) continue;
                var theirCiv = sim.GetCiv(c.CivId);
                float score = (c.GoldYield + c.FoodYield) * 2f - c.Defense(theirCiv) * 0.5f - d
                              + (sim.Diplo.AtWar(CivId, c.CivId) ? 30f : 0f);
                if (c.Defense(theirCiv) > Garrison * 1.1f) continue;
                if (score > preyScore) { preyScore = score; prey = c; }
            }
            if (prey == null) return;

            sim.Military.LaunchArmy(this, prey, Mission.Raid, Garrison * 0.55f, autonomous: true);
        }

        void UpdateSpecialization()
        {
            int best = 0; float bestLvl = Districts[0];
            for (int i = 1; i < 6; i++)
                if (Districts[i] > bestLvl) { bestLvl = Districts[i]; best = i; }

            if (bestLvl < 2f) { SpecName = "Young Settlement"; return; }
            switch ((District)best)
            {
                case District.Farms: SpecName = "Breadbasket"; break;
                case District.Workshops: SpecName = "Forge City"; break;
                case District.Markets:
                    SpecName = (Tile.River || NearWater()) ? "Free Port" : "Market City"; break;
                case District.Academies: SpecName = "Academy City"; break;
                case District.Temples: SpecName = "Holy City"; break;
                default: SpecName = "Garrison Town"; break;
            }
        }

        bool NearWater()
        {
            if (worked == null) return false;
            foreach (var t in worked)
                if (t.Biome == Biome.Coast) return true;
            return false;
        }
    }
}
