using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    /// <summary>AI leader temperament — used for policy, war, and decision choices.</summary>
    public class AIPersonality
    {
        public float Aggression;   // war, raids, conquest
        public float ExpansionDrive;
        public float Scholarship;
        public float Zeal;         // religion, tradition
    }

    public class Civilization
    {
        public int Id;
        public string Name;
        public Color Color;
        public bool IsPlayer;
        public bool IsRebel;
        public bool Eliminated;

        // Law: actual (what society lives under) drifts toward target (what you enacted).
        public PolicySet Policies = new PolicySet();
        public PolicySet PolicyTarget = new PolicySet();
        public MilitaryStance Stance = MilitaryStance.Balanced;

        public float Influence = 50f;
        public float Gold = 100f;
        public float Knowledge;

        public readonly HashSet<int> Techs = new HashSet<int>();
        public TechDef Researching;
        public TechMods Mods = new TechMods();
        public TechTag? Focus;

        public readonly List<City> Cities = new List<City>();

        public int StateReligion = -1;
        public readonly HashSet<int> Suppressed = new HashSet<int>();

        public float WarWeariness;          // 0..1
        public float GoldenAgeUntil = float.MinValue;
        public float HappyStreakYears;      // consecutive years above golden-age bar
        public float HarmonyStreakYears;    // consecutive years qualifying for Harmony victory
        public float Legacy;                // Legacy victory score, accrues forever

        public AIPersonality AI;            // null for the player
        public float NextThink = float.MinValue;  // game years are negative until 1 AD!

        public Era Era => TechCatalog.EraOf(Techs.Count);
        public bool GoldenAgeActive(float year) => year < GoldenAgeUntil;

        public float TotalPop
        {
            get { float s = 0f; foreach (var c in Cities) s += c.Population; return s; }
        }

        public float AvgHappiness
        {
            get
            {
                if (Cities.Count == 0) return 0f;
                float s = 0f, w = 0f;
                foreach (var c in Cities) { s += c.Happiness * c.Population; w += c.Population; }
                return w > 0f ? s / w : 0f;
            }
        }

        public void RecomputeMods()
        {
            Mods = new TechMods();
            foreach (var t in TechCatalog.All)
                if (Techs.Contains(t.Id))
                    t.Apply(Mods);
        }

        public bool SpendInfluence(float cost)
        {
            if (Influence < cost) return false;
            Influence -= cost;
            return true;
        }
    }
}
