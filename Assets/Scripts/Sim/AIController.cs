using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Rival leaders: they steer their own societies with the same levers the
    /// player has — policy targets, stances, focus, settlers, wars, peace.
    /// </summary>
    public static class AIController
    {
        public static void Tick(Simulation sim, Civilization civ)
        {
            if (civ.AI == null || civ.Eliminated || civ.Cities.Count == 0) return;
            if (sim.Year < civ.NextThink) return;
            civ.NextThink = sim.Year + Tuning.AIThinkInterval;
            var ai = civ.AI;

            // ---- policy targets from temperament ----
            var t = civ.PolicyTarget;
            t.Militarism = Mathf.Clamp01(ai.Aggression * 0.9f);
            t.Devotion = Mathf.Clamp01(ai.Zeal * 0.9f);
            t.Authority = Mathf.Clamp01(0.3f + ai.Aggression * 0.35f + ai.Zeal * 0.15f);
            t.Expansion = Mathf.Clamp01(ai.ExpansionDrive);
            t.Openness = Mathf.Clamp01(0.65f - ai.Zeal * 0.3f + ai.Scholarship * 0.2f);
            t.Market = Mathf.Clamp01(0.4f + ai.Scholarship * 0.2f);
            t.SpendMilitary = 0.2f + ai.Aggression * 0.25f;
            t.SpendScience = 0.15f + ai.Scholarship * 0.3f;
            t.SpendInfra = 0.25f;
            t.SpendWelfare = 0.2f;
            t.NormalizeSpending();

            civ.Stance = ai.Aggression > 0.6f ? MilitaryStance.Aggressive
                       : ai.Aggression < 0.3f ? MilitaryStance.Defensive
                       : MilitaryStance.Balanced;

            civ.Focus = ai.Scholarship > 0.6f ? TechTag.Science
                      : ai.Aggression > 0.6f ? TechTag.Military
                      : (TechTag?)null;

            // ---- adopt a homegrown faith if zealous ----
            if (civ.StateReligion < 0 && ai.Zeal > 0.55f)
            {
                foreach (var r in sim.Religions.Religions)
                    if (sim.Religions.CivShare(civ, r.Id) > 0.4f) { civ.StateReligion = r.Id; break; }
            }

            // ---- unhappy empire? ease off ----
            if (civ.AvgHappiness < 40f)
            {
                t.SpendWelfare += 0.15f;
                t.NormalizeSpending();
            }

            // ---- wealth becomes development (AI spends its treasury) ----
            int invested = 0;
            while (civ.Gold > 400f && invested < 3 && civ.Cities.Count > 0)
            {
                City poorest = null; float low = float.MaxValue;
                foreach (var c in civ.Cities)
                    if (c.TotalDistricts < low) { low = c.TotalDistricts; poorest = c; }
                int d = ai.Aggression > 0.6f ? (int)District.Walls
                      : ai.Scholarship > 0.6f ? (int)District.Academies
                      : ai.Zeal > 0.6f ? (int)District.Temples
                      : (int)District.Markets;
                if (sim.Rng.Chance(0.5f)) d = sim.Rng.Range(0, 6);
                float cost = 40f + poorest.Districts[d] * 30f;
                if (civ.Gold < cost) break;
                civ.Gold -= cost;
                poorest.Districts[d] += 1f;
                invested++;
            }

            // ---- wars ----
            float myStr = sim.Military.TotalStrength(civ);
            foreach (var other in sim.Civs)
            {
                if (other == civ || other.Eliminated || other.Cities.Count == 0) continue;
                if (!sim.Diplo.HasMet(civ.Id, other.Id)) continue;

                bool atWar = sim.Diplo.AtWar(civ.Id, other.Id);
                float theirStr = sim.Military.TotalStrength(other);

                if (!atWar)
                {
                    float rel = sim.Diplo.Relation(civ.Id, other.Id);
                    if (rel < -25f && myStr > theirStr * 1.25f && civ.WarWeariness < 0.25f
                        && sim.Rng.Chance(ai.Aggression * 0.35f))
                        sim.DeclareWar(civ, other, "old grudges");
                    // predation: the strong don't need a grudge, only an opening
                    // (but nobody bothers crushing stone-age villages without cause)
                    else if (rel < 10f && myStr > theirStr * 1.8f && civ.WarWeariness < 0.15f
                             && (other.Era >= Era.Iron || rel < -25f)
                             && sim.Rng.Chance(ai.Aggression * ai.Aggression * 0.08f))
                        sim.DeclareWar(civ, other, "conquest, plain and simple");
                }
                else
                {
                    // press the attack
                    if (myStr > theirStr * 1.1f && civ.WarWeariness < 0.6f)
                        TryConquest(sim, civ, other);

                    // or beg for peace
                    if (civ.WarWeariness > 0.6f || myStr < theirStr * 0.5f)
                    {
                        float tribute = Mathf.Min(civ.Gold * 0.3f, 80f);
                        civ.Gold -= tribute;
                        other.Gold += tribute;
                        sim.MakePeace(civ, other);
                        sim.Log.Log(sim.Year, $"{civ.Name} sues for peace with {other.Name}, paying {Mathf.RoundToInt(tribute)} gold.",
                            new Color(0.6f, 0.85f, 0.6f));
                    }
                }
            }
        }

        static void TryConquest(Simulation sim, Civilization civ, Civilization enemy)
        {
            // strongest garrison marches on the enemy's weakest reachable city
            City from = null;
            foreach (var c in civ.Cities)
                if (from == null || c.Garrison > from.Garrison) from = c;
            if (from == null || from.Garrison < 60f) return;

            City target = null; float best = float.MaxValue;
            foreach (var c in enemy.Cities)
            {
                float d = WorldMap.Dist(from.Tile, c.Tile);
                if (d > 24f) continue;
                float score = c.Defense(enemy) + d * 2f;
                if (score < best) { best = score; target = c; }
            }
            if (target == null) return;
            if (target.Defense(enemy) * Tuning.ConquerDefenseBonus > from.Garrison * 0.7f * civ.Mods.Military) return;

            sim.Military.LaunchArmy(from, target, Mission.Conquer, from.Garrison * 0.75f);
        }
    }
}
