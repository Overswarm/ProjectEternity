using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    public class DecisionOption
    {
        public string Label;
        public string Detail;
        public Action<Simulation, Civilization> Apply;
        public Func<AIPersonality, float> AIScore; // null -> 0.5
    }

    public class Decision
    {
        public string Key;
        public string Title;
        public string Desc;
        public int CivId;
        public float Expires;
        public int DefaultIndex;
        public readonly List<DecisionOption> Options = new List<DecisionOption>();
    }

    /// <summary>
    /// The crises on your desk. Generated from real simulation state; AI leaders
    /// answer the same dilemmas from their own temperament; ignored decisions
    /// resolve themselves and cost you legitimacy.
    /// </summary>
    public class DecisionSystem
    {
        readonly Simulation sim;
        public readonly List<Decision> Pending = new List<Decision>(); // player's desk
        readonly HashSet<string> fired = new HashSet<string>();        // once-only keys

        static readonly Color DecColor = new Color(0.95f, 0.9f, 0.5f);

        public DecisionSystem(Simulation sim) { this.sim = sim; }

        public void Tick(float dt)
        {
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                var d = Pending[i];
                if (sim.Year >= d.Expires)
                {
                    var civ = sim.GetCiv(d.CivId);
                    d.Options[d.DefaultIndex].Apply(sim, civ);
                    civ.Influence = Mathf.Max(0f, civ.Influence - Tuning.IndecisionPenalty);
                    sim.Log.Log(sim.Year, $"Indecision: \"{d.Title}\" resolved itself ({d.Options[d.DefaultIndex].Label}).",
                        new Color(0.8f, 0.6f, 0.4f));
                    Pending.RemoveAt(i);
                }
            }
        }

        public void Choose(Decision d, int optionIndex)
        {
            var civ = sim.GetCiv(d.CivId);
            d.Options[optionIndex].Apply(sim, civ);
            Pending.Remove(d);
        }

        void Push(Civilization civ, Decision d)
        {
            d.CivId = civ.Id;
            d.Expires = sim.Year + Tuning.DecisionLifetime;

            if (civ.AI != null)
            {
                int best = d.DefaultIndex; float bestScore = float.MinValue;
                for (int i = 0; i < d.Options.Count; i++)
                {
                    float s = d.Options[i].AIScore?.Invoke(civ.AI) ?? 0.5f;
                    s += sim.Rng.Jitter(0.1f);
                    if (s > bestScore) { bestScore = s; best = i; }
                }
                d.Options[best].Apply(sim, civ);
                return;
            }
            Pending.Add(d);
            sim.Log.Log(sim.Year, $"A decision awaits the throne: {d.Title}", DecColor);
        }

        bool Once(string key)
        {
            if (fired.Contains(key)) return false;
            fired.Add(key);
            return true;
        }

        // ------------------------------------------------------------------
        //  TRIGGER HOOKS (called by other systems)
        // ------------------------------------------------------------------

        public void OnFaithFounded(Civilization civ, Religion faith, City city)
        {
            var d = new Decision
            {
                Key = "faith" + faith.Id,
                Title = $"A New Faith: {faith.Name}",
                Desc = $"A prophet in {city.Name} preaches {faith.Name} ({faith.Temperament}). " +
                       "The temples fill. The old priests mutter. What is the throne's word?",
                DefaultIndex = 1,
            };
            d.Options.Add(new DecisionOption
            {
                Label = "Embrace it as the state faith",
                Detail = "Unity among believers; resentment among the rest. Devout cities calm; science slows where fervor runs hot.",
                Apply = (s, c) => { c.StateReligion = faith.Id; c.Suppressed.Remove(faith.Id);
                    s.Log.Log(s.Year, $"{c.Name} adopts {faith.Name} as the faith of the realm.", faith.Color); },
                AIScore = ai => ai.Zeal,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Tolerate it",
                Detail = "Let faiths compete. More friction, more questions — and questions breed discovery.",
                Apply = (s, c) => { foreach (var ct in c.Cities) ct.Pers.Nudge(5, 0.02f); },
                AIScore = ai => 0.6f - ai.Zeal * 0.3f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Suppress it",
                Detail = "Stamp it out before it spreads. Its faithful will not forgive you.",
                Apply = (s, c) => { c.Suppressed.Add(faith.Id);
                    s.Log.Log(s.Year, $"{c.Name} outlaws {faith.Name}. Its faithful seethe.", new Color(1f, 0.5f, 0.4f)); },
                AIScore = ai => ai.Aggression * 0.5f + (ai.Zeal > 0.6f ? 0.4f : 0f),
            });
            Push(civ, d);
        }

        public void OnFirstContact(Civilization civ, Civilization other)
        {
            if (!Once($"contact{civ.Id}-{other.Id}")) return;
            var d = new Decision
            {
                Key = "contact",
                Title = $"First Contact: {other.Name}",
                Desc = $"Scouts return with word of another people — {other.Name}. How do we greet them?",
                DefaultIndex = 1,
            };
            d.Options.Add(new DecisionOption
            {
                Label = "Open arms",
                Detail = "Trade and trust. (+20 relations)",
                Apply = (s, c) => s.Diplo.ChangeRelation(c.Id, other.Id, 20f),
                AIScore = ai => 0.7f - ai.Aggression * 0.5f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Watch them warily",
                Detail = "No promises, no provocations.",
                Apply = (s, c) => { },
            });
            d.Options.Add(new DecisionOption
            {
                Label = "A show of force",
                Detail = "Let them fear us. (-15 relations, your border cities take heart)",
                Apply = (s, c) => { s.Diplo.ChangeRelation(c.Id, other.Id, -15f);
                    foreach (var ct in c.Cities) ct.Pers.Nudge(0, 0.02f); },
                AIScore = ai => ai.Aggression,
            });
            Push(civ, d);
        }

        public void OnTechDiscovered(Civilization civ, TechDef tech)
        {
            if (tech.Name == "Iron Working" && Once($"iron{civ.Id}"))
            {
                var d = new Decision
                {
                    Key = "iron",
                    Title = "The Iron Question",
                    Desc = "Iron blades in every smithy. The generals ask: what shape shall our army take?",
                    DefaultIndex = 2,
                };
                d.Options.Add(new DecisionOption
                {
                    Label = "A professional army",
                    Detail = "Costly, disciplined. (-100 gold, garrisons +25%)",
                    Apply = (s, c) => { c.Gold -= 100f; foreach (var ct in c.Cities) ct.Garrison *= 1.25f; },
                    AIScore = ai => 0.4f + ai.Aggression * 0.3f,
                });
                d.Options.Add(new DecisionOption
                {
                    Label = "Conscription by decree",
                    Detail = "Everyone serves. (garrisons +40%, unrest +10, society hardens)",
                    Apply = (s, c) => { foreach (var ct in c.Cities) { ct.Garrison *= 1.4f; ct.Unrest += 10f; }
                        c.PolicyTarget.Authority = Mathf.Clamp01(c.PolicyTarget.Authority + 0.1f); },
                    AIScore = ai => ai.Aggression,
                });
                d.Options.Add(new DecisionOption
                {
                    Label = "Keep the militia tradition",
                    Detail = "Farmers with spears, same as always.",
                    Apply = (s, c) => { },
                    AIScore = ai => 0.5f - ai.Aggression * 0.3f,
                });
                Push(civ, d);
            }

            if (tech.Name == "Factory System" && Once($"smoke{civ.Id}"))
            {
                var d = new Decision
                {
                    Key = "smoke",
                    Title = "The Smoke Question",
                    Desc = "Chimneys blacken the sky over your cities. Physicians protest; industrialists shrug. Regulate the factories?",
                    DefaultIndex = 1,
                };
                d.Options.Add(new DecisionOption
                {
                    Label = "Regulate them",
                    Detail = "Cleaner cities, slower forges. (-100 gold, industry culture cools, +5 happiness)",
                    Apply = (s, c) => { c.Gold -= 100f;
                        foreach (var ct in c.Cities) { ct.Pers.Nudge(3, -0.05f); ct.Happiness += 5f; } },
                    AIScore = ai => ai.Scholarship,
                });
                d.Options.Add(new DecisionOption
                {
                    Label = "Let them burn",
                    Detail = "Progress has a price. (industry culture rises, -5 happiness, smog deaths)",
                    Apply = (s, c) => { foreach (var ct in c.Cities)
                        { ct.Pers.Nudge(3, 0.05f); ct.Happiness -= 5f; ct.Population *= 0.985f; } },
                    AIScore = ai => 0.4f + ai.Aggression * 0.2f,
                });
                Push(civ, d);
            }
        }

        public void OnPlague(City city, Civilization civ)
        {
            var d = new Decision
            {
                Key = "plague",
                Title = $"Plague in {city.Name}",
                Desc = $"Sickness spreads through the crowded streets of {city.Name} ({Mathf.RoundToInt(city.Population)} souls). The people look to the throne.",
                DefaultIndex = 0,
            };
            d.Options.Add(new DecisionOption
            {
                Label = "Quarantine the city",
                Detail = "Seal the gates. Fewer deaths, strangled trade, simmering anger. (-7% pop, -30 gold, +unrest)",
                Apply = (s, c) => { city.Population *= 0.93f; c.Gold -= 30f; city.Unrest += 8f; },
                AIScore = ai => 0.5f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Prayers and processions",
                Detail = "Comfort in faith. More deaths, but the people feel held. (-12% pop, piety rises, +5 happiness)",
                Apply = (s, c) => { city.Population *= 0.88f; city.Pers.Nudge(1, 0.08f); city.Happiness += 5f; },
                AIScore = ai => ai.Zeal,
            });
            if (civ.Era >= Era.Medieval)
                d.Options.Add(new DecisionOption
                {
                    Label = "Send the physicians",
                    Detail = "Science against death. (-3% pop, -80 gold)",
                    Apply = (s, c) => { city.Population *= 0.97f; c.Gold -= 80f; },
                    AIScore = ai => ai.Scholarship + 0.2f,
                });
            Push(civ, d);
        }

        public void OnFamine(City city, Civilization civ)
        {
            var d = new Decision
            {
                Key = "famine",
                Title = $"Famine in {city.Name}",
                Desc = $"The granaries of {city.Name} are empty and the fields give nothing. Hunger walks the streets.",
                DefaultIndex = 0,
            };
            d.Options.Add(new DecisionOption
            {
                Label = "Import grain",
                Detail = "Empty the treasury, fill the bellies. (-60 gold, city steadied)",
                Apply = (s, c) => { c.Gold -= 60f; city.Happiness += 10f; city.Unrest -= 10f; },
                AIScore = ai => 0.6f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Let the market provide",
                Detail = "Speculators feast; some people don't. (+20 gold, -5% pop, +unrest, commerce culture rises)",
                Apply = (s, c) => { c.Gold += 20f; city.Population *= 0.95f; city.Unrest += 10f; city.Pers.Nudge(4, 0.05f); },
                AIScore = ai => 0.3f + (1f - ai.Zeal) * 0.2f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Ration by decree",
                Detail = "Order over plenty. (-2% pop, society hardens)",
                Apply = (s, c) => { city.Population *= 0.98f;
                    c.PolicyTarget.Authority = Mathf.Clamp01(c.PolicyTarget.Authority + 0.05f); },
                AIScore = ai => ai.Aggression * 0.4f + 0.3f,
            });
            Push(civ, d);
        }

        public void OnRevoltThreat(City city, Civilization civ)
        {
            var d = new Decision
            {
                Key = "revolt",
                Title = $"Ultimatum from {city.Name}",
                Desc = $"{city.Name} is on the edge of open revolt. Its people demand relief — or independence.",
                DefaultIndex = 0,
            };
            d.Options.Add(new DecisionOption
            {
                Label = "Make concessions",
                Detail = "Gold and grain buy calm. (-80 gold, unrest -30)",
                Apply = (s, c) => { c.Gold -= 80f; city.Unrest -= 30f; city.Happiness += 8f; },
                AIScore = ai => 0.5f - ai.Aggression * 0.2f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Crack down",
                Detail = "Send the garrison in. Order restored, hearts hardened. (unrest -40, happiness -10, aggression rises)",
                Apply = (s, c) => { city.Unrest -= 40f; city.Happiness -= 10f; city.Pers.Nudge(0, 0.06f);
                    c.PolicyTarget.Authority = Mathf.Clamp01(c.PolicyTarget.Authority + 0.05f); },
                AIScore = ai => ai.Aggression,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Grant self-rule",
                Detail = "A freer city pays half taxes but keeps the peace. (permanent)",
                Apply = (s, c) => { city.SelfRule = true; city.Unrest -= 50f;
                    s.Log.Log(s.Year, $"{city.Name} is granted self-rule.", new Color(0.7f, 0.9f, 1f)); },
                AIScore = ai => 0.3f,
            });
            Push(civ, d);
        }

        public void OnWarDeclaredOnYou(Civilization civ, Civilization aggressor)
        {
            var d = new Decision
            {
                Key = "warRally",
                Title = $"War! {aggressor.Name} attacks!",
                Desc = $"{aggressor.Name} has declared war upon us. The court waits for your word.",
                DefaultIndex = 2,
            };
            d.Options.Add(new DecisionOption
            {
                Label = "Rally the banners",
                Detail = "Garrisons +20%, war weariness eased.",
                Apply = (s, c) => { foreach (var ct in c.Cities) ct.Garrison *= 1.2f;
                    c.WarWeariness = Mathf.Max(0f, c.WarWeariness - 0.15f); },
                AIScore = ai => ai.Aggression + 0.2f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Seek terms at once",
                Detail = "Pay 100 gold tribute for peace.",
                Apply = (s, c) => { c.Gold -= 100f; aggressor.Gold += 100f; s.MakePeace(c, aggressor); },
                AIScore = ai => 0.4f - ai.Aggression * 0.4f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Grim resolve",
                Detail = "Endure. No special measures.",
                Apply = (s, c) => { },
            });
            Push(civ, d);
        }

        public void OnGoldenAge(Civilization civ)
        {
            var d = new Decision
            {
                Key = "golden",
                Title = "A Golden Age Dawns",
                Desc = "Contentment, plenty, and confidence — your people flourish. How shall the age be spent?",
                DefaultIndex = 0,
            };
            d.Options.Add(new DecisionOption
            {
                Label = "A great festival",
                Detail = "(-100 gold, +6 happiness everywhere)",
                Apply = (s, c) => { c.Gold -= 100f; foreach (var ct in c.Cities) ct.Happiness += 6f; },
                AIScore = ai => ai.Zeal * 0.5f + 0.3f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Great works",
                Detail = "Monuments for the ages. (+30 Legacy)",
                Apply = (s, c) => c.Legacy += 30f,
                AIScore = ai => 0.5f,
            });
            d.Options.Add(new DecisionOption
            {
                Label = "Store the surplus",
                Detail = "(+120 gold)",
                Apply = (s, c) => c.Gold += 120f,
                AIScore = ai => 0.4f,
            });
            Push(civ, d);
        }
    }
}
