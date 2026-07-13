using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    public enum SimState { Founding, Running }

    /// <summary>
    /// The whole living world: map, civs, systems, and the master tick.
    /// Pure C# — no scene dependencies; views poll this.
    /// </summary>
    public class Simulation
    {
        public readonly WorldMap World;
        public readonly List<Civilization> Civs = new List<Civilization>();
        public readonly List<Army> Armies = new List<Army>();
        public readonly Diplomacy Diplo = new Diplomacy();
        public readonly EventLog Log = new EventLog();
        public readonly Rng Rng;
        public readonly NameGenerator Names;

        public readonly ReligionSystem Religions;
        public readonly MilitarySystem Military;
        public readonly SettlementPlanner Settlement;
        public readonly EventsSystem Events;
        public readonly DecisionSystem Decisions;
        public readonly VictorySystem Victory;

        public float Year = Tuning.StartYear;
        public SimState State = SimState.Founding;
        public bool WorldDirty = true;   // view refresh flag (ownership/biome changed)

        public int NextCityId;
        public int NextArmyId;

        float meetTimer, diploTimer;

        public Civilization Player => Civs[0];

        static readonly Color[] CivColors =
        {
            new Color(0.30f, 0.55f, 1.00f),  // player: blue
            new Color(0.90f, 0.30f, 0.30f),
            new Color(0.70f, 0.40f, 0.90f),
            new Color(0.95f, 0.60f, 0.20f),
            new Color(0.30f, 0.80f, 0.72f),
            new Color(0.85f, 0.75f, 0.30f),
        };

        public Simulation(int seed, int rivals)
        {
            Rng = new Rng(seed);
            Names = new NameGenerator(Rng);
            World = WorldGen.Generate(Tuning.WorldSize, Rng);

            Religions = new ReligionSystem(this);
            Military = new MilitarySystem(this);
            Settlement = new SettlementPlanner(this);
            Events = new EventsSystem(this);
            Decisions = new DecisionSystem(this);
            Victory = new VictorySystem(this);

            // ---- create civs ----
            for (int i = 0; i <= rivals; i++)
            {
                var civ = new Civilization
                {
                    Id = i,
                    Name = Names.CivName(i),
                    Color = CivColors[i % CivColors.Length],
                    IsPlayer = i == 0,
                };
                if (i > 0)
                {
                    civ.AI = new AIPersonality
                    {
                        Aggression = Rng.Range(0.15f, 0.9f),
                        ExpansionDrive = Rng.Range(0.3f, 0.9f),
                        Scholarship = Rng.Range(0.2f, 0.85f),
                        Zeal = Rng.Range(0.1f, 0.9f),
                    };
                }
                Civs.Add(civ);
            }

            // ---- seat the rivals; the player chooses their own homeland ----
            var sites = PickStartSites(rivals);
            for (int i = 0; i < rivals && i < sites.Count; i++)
                FoundCity(Civs[i + 1], sites[i]);

            Log.Log(Year, "Choose the homeland of your people. Geography is destiny — for a while.",
                new Color(0.9f, 0.9f, 1f));
        }

        List<Tile> PickStartSites(int count)
        {
            var candidates = new List<Tile>();
            for (int x = 2; x < World.W - 2; x++)
                for (int y = 2; y < World.H - 2; y++)
                {
                    var t = World.Get(x, y);
                    if (t.Land && t.Biome != Biome.Mountain && t.Biome != Biome.Snow)
                        candidates.Add(t);
                }
            candidates.Sort((a, b) => SiteRawScore(b).CompareTo(SiteRawScore(a)));

            var picked = new List<Tile>();
            foreach (var t in candidates)
            {
                bool ok = true;
                foreach (var p in picked)
                    if (WorldMap.Dist(p, t) < 18f) { ok = false; break; }
                if (!ok) continue;
                // keep the very best land unclaimed so the player has good options too
                if (picked.Count == 0 && candidates.IndexOf(t) < 40 && Rng.Chance(0.5f)) continue;
                picked.Add(t);
                if (picked.Count >= count) break;
            }
            return picked;
        }

        float SiteRawScore(Tile tile)
        {
            float s = 0f;
            foreach (var t in World.InRadius(tile.X, tile.Y, Tuning.CityWorkRadius))
            {
                s += t.Food * 1.3f + t.Materials + t.Commerce;
                if (t.River) s += 0.5f;
            }
            return s;
        }

        public Civilization GetCiv(int id) => Civs[id];

        public IEnumerable<City> AllCities()
        {
            foreach (var civ in Civs)
                foreach (var c in civ.Cities)
                    if (!c.Razed) yield return c;
        }

        // ------------------------------------------------------------------
        //  MASTER TICK
        // ------------------------------------------------------------------
        public void Tick(float dt)
        {
            if (Victory.GameOver || State == SimState.Founding) return;
            Year += dt;

            for (int ci = 0; ci < Civs.Count; ci++)   // index loop: rebels can be born mid-tick
            {
                var civ = Civs[ci];
                if (civ.Eliminated) continue;

                civ.Policies.MoveToward(civ.PolicyTarget, dt);

                float legitimacy = Mathf.Clamp(civ.AvgHappiness / 60f, 0.2f, 1.5f);
                civ.Influence = Mathf.Min(Tuning.InfluenceMax,
                    civ.Influence + Tuning.InfluenceRegen * legitimacy * dt);

                int wars = Diplo.WarCount(civ.Id, Civs.Count);
                if (wars > 0)
                    civ.WarWeariness = Mathf.Min(1f, civ.WarWeariness + 0.025f * (1f + wars * 0.3f) * dt);
                else
                    civ.WarWeariness = Mathf.Max(0f, civ.WarWeariness - 0.08f * dt);

                TickResearch(civ);

                var snapshot = civ.Cities.ToArray();  // cities can revolt/transfer mid-tick
                foreach (var city in snapshot)
                    if (city.CivId == civ.Id && !city.Razed)
                        city.Tick(this, civ, dt);

                AIController.Tick(this, civ);
            }

            Military.Tick(dt);
            Religions.Tick(dt);
            Events.Tick(dt);
            Decisions.Tick(dt);
            Settlement.Tick(dt);
            TickMeetings(dt);
            TickDiplomacyDrift(dt);
            Victory.Tick(dt);
        }

        void TickResearch(Civilization civ)
        {
            if (civ.Researching == null)
            {
                int eraIdx = civ.Techs.Count / TechCatalog.TechsPerEra;
                TechDef best = null;
                float bestKey = float.MaxValue;
                foreach (var t in TechCatalog.All)
                {
                    if (civ.Techs.Contains(t.Id) || (int)t.Era > eraIdx) continue;
                    float key = t.Cost - (civ.Focus.HasValue && t.Tag == civ.Focus.Value ? 100000f : 0f);
                    if (key < bestKey) { bestKey = key; best = t; }
                }
                civ.Researching = best;
            }

            var cur = civ.Researching;
            if (cur == null || civ.Knowledge < cur.Cost) return;

            civ.Knowledge -= cur.Cost;
            var oldEra = civ.Era;
            civ.Techs.Add(cur.Id);
            civ.RecomputeMods();
            civ.Researching = null;

            if (civ.IsPlayer)
                Log.Log(Year, $"Discovery: {cur.Name} ({cur.Effect})", new Color(0.5f, 0.8f, 1f));

            if (civ.Era != oldEra)
                Log.Log(Year, $"{civ.Name} enters the {TechCatalog.EraNames[(int)civ.Era]}!",
                    civ.IsPlayer ? new Color(0.6f, 0.9f, 1f) : new Color(0.7f, 0.7f, 0.8f));

            Decisions.OnTechDiscovered(civ, cur);
        }

        void TickMeetings(float dt)
        {
            meetTimer += dt;
            if (meetTimer < 2f) return;
            meetTimer = 0f;

            for (int a = 0; a < Civs.Count; a++)
                for (int b = a + 1; b < Civs.Count; b++)
                {
                    if (Diplo.HasMet(a, b) || Civs[a].Eliminated || Civs[b].Eliminated) continue;
                    bool near = false;
                    foreach (var ca in Civs[a].Cities)
                    {
                        foreach (var cb in Civs[b].Cities)
                            if (WorldMap.Dist(ca.Tile, cb.Tile) < 12f) { near = true; break; }
                        if (near) break;
                    }
                    if (!near) continue;

                    Diplo.SetMet(a, b);
                    Log.Log(Year, $"{Civs[a].Name} and {Civs[b].Name} meet for the first time.",
                        new Color(0.85f, 0.85f, 1f));
                    Decisions.OnFirstContact(Civs[a], Civs[b]);
                    Decisions.OnFirstContact(Civs[b], Civs[a]);
                }
        }

        void TickDiplomacyDrift(float dt)
        {
            diploTimer += dt;
            if (diploTimer < 1f) return;
            float step = diploTimer;
            diploTimer = 0f;

            for (int a = 0; a < Civs.Count; a++)
                for (int b = a + 1; b < Civs.Count; b++)
                {
                    if (!Diplo.HasMet(a, b) || Diplo.AtWar(a, b)) continue;
                    var ca = Civs[a]; var cb = Civs[b];
                    if (ca.Eliminated || cb.Eliminated) continue;

                    float target = -5f;
                    if (ca.StateReligion >= 0 && ca.StateReligion == cb.StateReligion) target += 20f;
                    target += (ca.Policies.Openness + cb.Policies.Openness) * 10f - 10f;

                    int friction = 0;
                    foreach (var x in ca.Cities)
                        foreach (var y in cb.Cities)
                            if (WorldMap.Dist(x.Tile, y.Tile) < 8f) friction++;
                    target -= Mathf.Min(45f, friction * 9f);

                    Diplo.DriftRelation(a, b, target, step);
                }
        }

        // ------------------------------------------------------------------
        //  WORLD-CHANGING ACTS
        // ------------------------------------------------------------------

        public City FoundCity(Civilization civ, Tile tile, City culturalParent = null)
        {
            var geo = CityPersonality.GeographyAttractor(World, tile, Rng);
            var pers = geo.Clone();
            if (culturalParent != null)
                pers.DriftToward(culturalParent.Pers, 0.25f);   // settlers carry their culture
            pers.DriftToward(CityPersonality.FromPolicy(civ.Policies), 0.15f);

            var city = new City
            {
                Id = NextCityId++,
                Name = Names.CityName(),
                CivId = civ.Id,
                Tile = tile,
                Founded = Year,
                Population = Tuning.StartingPop,
                Pers = pers,
                GeoAttractor = geo,
            };
            civ.Cities.Add(city);
            tile.City = city;
            foreach (var t in World.InRadius(tile.X, tile.Y, 1))
                if (t.OwnerCiv == -1 && t.Land) t.OwnerCiv = civ.Id;
            WorldDirty = true;

            if (civ.IsPlayer && State == SimState.Founding)
            {
                State = SimState.Running;
                Log.Log(Year, $"{city.Name} is founded — the first fire of {civ.Name} is lit. Eons begin.",
                    new Color(1f, 0.95f, 0.6f), tile.X, tile.Y);
            }
            else
            {
                Log.Log(Year, $"{civ.Name} founds the city of {city.Name}.",
                    civ.Color, tile.X, tile.Y);
            }
            return city;
        }

        public void TriggerRevolt(City city)
        {
            var oldCiv = GetCiv(city.CivId);

            if (Civs.Count < Tuning.MaxCivs)
            {
                var rebel = new Civilization
                {
                    Id = Civs.Count,
                    Name = "Free " + city.Name,
                    Color = new Color(0.62f, 0.62f, 0.62f),
                    IsRebel = true,
                    AI = new AIPersonality { Aggression = 0.35f, ExpansionDrive = 0.1f, Scholarship = 0.4f, Zeal = 0.4f },
                    PolicyTarget = oldCiv.Policies.Clone(),
                    Policies = oldCiv.Policies.Clone(),
                };
                rebel.PolicyTarget.Authority = 0.2f;   // rebels want liberty
                Civs.Add(rebel);

                Military.TransferCity(city, oldCiv, rebel);
                city.Happiness = 55f;
                Diplo.SetMet(oldCiv.Id, rebel.Id);
                Diplo.SetWar(oldCiv.Id, rebel.Id, true);
                Diplo.ChangeRelation(oldCiv.Id, rebel.Id, -60f);

                Log.Log(Year, $"REVOLT! {city.Name} casts off {oldCiv.Name} and declares itself free!",
                    new Color(1f, 0.4f, 0.3f), city.Tile.X, city.Tile.Y);
            }
            else
            {
                // no room for another nation; the city defects to the best-aligned rival
                Civilization best = null; float bestDist = float.MaxValue;
                foreach (var c in Civs)
                {
                    if (c == oldCiv || c.Eliminated || c.Cities.Count == 0) continue;
                    float d = city.Pers.Distance(CityPersonality.FromPolicy(c.Policies));
                    if (d < bestDist) { bestDist = d; best = c; }
                }
                if (best == null) { city.Unrest = 60f; return; }
                Military.TransferCity(city, oldCiv, best);
                city.Happiness = 50f;
                Diplo.ChangeRelation(oldCiv.Id, best.Id, -40f);
                Log.Log(Year, $"{city.Name} defects from {oldCiv.Name} to {best.Name}!",
                    new Color(1f, 0.4f, 0.3f), city.Tile.X, city.Tile.Y);
            }
            CheckElimination(oldCiv);
        }

        public void DeclareWar(Civilization a, Civilization b, string reason)
        {
            if (Diplo.AtWar(a.Id, b.Id)) return;
            Diplo.SetMet(a.Id, b.Id);
            Diplo.SetWar(a.Id, b.Id, true);
            Diplo.ChangeRelation(a.Id, b.Id, -40f);
            Log.Log(Year, $"WAR! {a.Name} declares war on {b.Name} — {reason}.",
                new Color(1f, 0.3f, 0.25f));
            if (b.IsPlayer) Decisions.OnWarDeclaredOnYou(b, a);
        }

        public void MakePeace(Civilization a, Civilization b)
        {
            if (!Diplo.AtWar(a.Id, b.Id)) return;
            Diplo.SetWar(a.Id, b.Id, false);
            a.WarWeariness *= 0.5f;
            b.WarWeariness *= 0.5f;
            Diplo.ChangeRelation(a.Id, b.Id, 25f);
            Log.Log(Year, $"Peace between {a.Name} and {b.Name}.", new Color(0.6f, 0.9f, 0.6f));
        }

        public void CheckElimination(Civilization civ)
        {
            if (civ.Eliminated || civ.Cities.Count > 0) return;
            foreach (var a in Armies)
                if (a.CivId == civ.Id && a.Mission == Mission.Settle && !a.Dead) return;

            civ.Eliminated = true;
            for (int x = 0; x < World.W; x++)
                for (int y = 0; y < World.H; y++)
                    if (World.Get(x, y).OwnerCiv == civ.Id) World.Get(x, y).OwnerCiv = -1;
            WorldDirty = true;
            Log.Log(Year, $"The story of {civ.Name} ends. Its name passes into legend.",
                new Color(0.8f, 0.8f, 0.9f));
        }
    }
}
