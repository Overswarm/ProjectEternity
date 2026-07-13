using System;
using System.Collections.Generic;

namespace Eternity
{
    public enum Era
    {
        Neolithic, Bronze, Iron, Classical, Medieval,
        Renaissance, Industrial, Modern, Information, Transcendent
    }

    public enum TechTag { Military, Economy, Science, Faith, Civic }

    /// <summary>Compounded national multipliers from every discovery known.</summary>
    public class TechMods
    {
        public float Food = 1f, Industry = 1f, Commerce = 1f, Science = 1f;
        public float Health = 1f, Military = 1f, PopCap = 1f;
        public float Happiness = 0f;   // flat add
        public bool AscensionDone = false;
    }

    public class TechDef
    {
        public int Id;
        public string Name;
        public Era Era;
        public TechTag Tag;
        public float Cost;
        public string Effect;              // human-readable, shown in UI
        public Action<TechMods> Apply;
    }

    public static class TechCatalog
    {
        public static readonly List<TechDef> All = new List<TechDef>();
        public const int TechsPerEra = 5;

        public static readonly string[] EraNames =
        {
            "Neolithic Age", "Bronze Age", "Iron Age", "Classical Age", "Medieval Age",
            "Renaissance", "Industrial Age", "Modern Age", "Information Age", "Transcendence"
        };

        static void Add(string name, Era era, TechTag tag, float cost, string effect, Action<TechMods> apply)
        {
            All.Add(new TechDef { Id = All.Count, Name = name, Era = era, Tag = tag, Cost = cost, Effect = effect, Apply = apply });
        }

        static TechCatalog()
        {
            // -------- Neolithic --------
            Add("Agriculture", Era.Neolithic, TechTag.Economy, 300, "+15% food", m => m.Food *= 1.15f);
            Add("Animal Husbandry", Era.Neolithic, TechTag.Economy, 300, "+10% food, +5% industry", m => { m.Food *= 1.10f; m.Industry *= 1.05f; });
            Add("Pottery", Era.Neolithic, TechTag.Civic, 300, "+15% housing", m => m.PopCap *= 1.15f);
            Add("Toolmaking", Era.Neolithic, TechTag.Economy, 300, "+12% industry", m => m.Industry *= 1.12f);
            Add("Oral Tradition", Era.Neolithic, TechTag.Faith, 300, "+15% knowledge", m => m.Science *= 1.15f);

            // -------- Bronze --------
            Add("Bronze Working", Era.Bronze, TechTag.Military, 800, "+15% military, +8% industry", m => { m.Military *= 1.15f; m.Industry *= 1.08f; });
            Add("Writing", Era.Bronze, TechTag.Science, 800, "+20% knowledge", m => m.Science *= 1.20f);
            Add("Masonry", Era.Bronze, TechTag.Civic, 800, "+15% housing, +5% military", m => { m.PopCap *= 1.15f; m.Military *= 1.05f; });
            Add("Sailing", Era.Bronze, TechTag.Economy, 800, "+15% commerce", m => m.Commerce *= 1.15f);
            Add("The Wheel", Era.Bronze, TechTag.Economy, 800, "+10% commerce, +8% industry", m => { m.Commerce *= 1.10f; m.Industry *= 1.08f; });

            // -------- Iron --------
            Add("Iron Working", Era.Iron, TechTag.Military, 1800, "+20% military", m => m.Military *= 1.20f);
            Add("Mathematics", Era.Iron, TechTag.Science, 1800, "+15% knowledge, +5% industry", m => { m.Science *= 1.15f; m.Industry *= 1.05f; });
            Add("Currency", Era.Iron, TechTag.Economy, 1800, "+20% commerce", m => m.Commerce *= 1.20f);
            Add("Irrigation", Era.Iron, TechTag.Economy, 1800, "+15% food", m => m.Food *= 1.15f);
            Add("Construction", Era.Iron, TechTag.Civic, 1800, "+20% housing", m => m.PopCap *= 1.20f);

            // -------- Classical --------
            Add("Philosophy", Era.Classical, TechTag.Science, 3600, "+20% knowledge", m => m.Science *= 1.20f);
            Add("Roads", Era.Classical, TechTag.Economy, 3600, "+15% commerce, +5% military", m => { m.Commerce *= 1.15f; m.Military *= 1.05f; });
            Add("Aqueducts", Era.Classical, TechTag.Civic, 3600, "+15% health, +10% housing", m => { m.Health *= 1.15f; m.PopCap *= 1.10f; });
            Add("Drama & Poetry", Era.Classical, TechTag.Faith, 3600, "+3 happiness", m => m.Happiness += 3f);
            Add("Military Discipline", Era.Classical, TechTag.Military, 3600, "+15% military", m => m.Military *= 1.15f);

            // -------- Medieval --------
            Add("Feudal Levy", Era.Medieval, TechTag.Military, 6500, "+15% military", m => m.Military *= 1.15f);
            Add("Guilds", Era.Medieval, TechTag.Economy, 6500, "+12% commerce, +12% industry", m => { m.Commerce *= 1.12f; m.Industry *= 1.12f; });
            Add("Universities", Era.Medieval, TechTag.Science, 6500, "+25% knowledge", m => m.Science *= 1.25f);
            Add("Crop Rotation", Era.Medieval, TechTag.Economy, 6500, "+18% food", m => m.Food *= 1.18f);
            Add("Physicians", Era.Medieval, TechTag.Civic, 6500, "+15% health", m => m.Health *= 1.15f);

            // -------- Renaissance --------
            Add("Printing Press", Era.Renaissance, TechTag.Science, 10000, "+25% knowledge", m => m.Science *= 1.25f);
            Add("Banking", Era.Renaissance, TechTag.Economy, 10000, "+20% commerce", m => m.Commerce *= 1.20f);
            Add("Gunpowder", Era.Renaissance, TechTag.Military, 10000, "+25% military", m => m.Military *= 1.25f);
            Add("Astronomy", Era.Renaissance, TechTag.Science, 10000, "+15% knowledge, +5% commerce", m => { m.Science *= 1.15f; m.Commerce *= 1.05f; });
            Add("Grand Architecture", Era.Renaissance, TechTag.Civic, 10000, "+15% housing, +2 happiness", m => { m.PopCap *= 1.15f; m.Happiness += 2f; });

            // -------- Industrial --------
            Add("Steam Power", Era.Industrial, TechTag.Economy, 14000, "+25% industry", m => m.Industry *= 1.25f);
            Add("Factory System", Era.Industrial, TechTag.Economy, 14000, "+20% industry, +10% commerce", m => { m.Industry *= 1.20f; m.Commerce *= 1.10f; });
            Add("Railroads", Era.Industrial, TechTag.Economy, 14000, "+20% commerce", m => m.Commerce *= 1.20f);
            Add("Vaccination", Era.Industrial, TechTag.Civic, 14000, "+25% health", m => m.Health *= 1.25f);
            Add("Rifling", Era.Industrial, TechTag.Military, 14000, "+25% military", m => m.Military *= 1.25f);

            // -------- Modern --------
            Add("Electricity", Era.Modern, TechTag.Economy, 18000, "+15% industry, +15% commerce", m => { m.Industry *= 1.15f; m.Commerce *= 1.15f; });
            Add("Mass Media", Era.Modern, TechTag.Civic, 18000, "+3 happiness", m => m.Happiness += 3f);
            Add("Modern Medicine", Era.Modern, TechTag.Science, 18000, "+30% health", m => m.Health *= 1.30f);
            Add("Combustion", Era.Modern, TechTag.Military, 18000, "+20% military, +10% industry", m => { m.Military *= 1.20f; m.Industry *= 1.10f; });
            Add("Universal Education", Era.Modern, TechTag.Science, 18000, "+30% knowledge", m => m.Science *= 1.30f);

            // -------- Information --------
            Add("Computers", Era.Information, TechTag.Science, 24000, "+35% knowledge", m => m.Science *= 1.35f);
            Add("Automation", Era.Information, TechTag.Economy, 24000, "+30% industry", m => m.Industry *= 1.30f);
            Add("Global Trade", Era.Information, TechTag.Economy, 24000, "+30% commerce", m => m.Commerce *= 1.30f);
            Add("Genetics", Era.Information, TechTag.Science, 24000, "+20% health, +15% food", m => { m.Health *= 1.20f; m.Food *= 1.15f; });
            Add("Networked Society", Era.Information, TechTag.Civic, 24000, "+15% knowledge, +2 happiness", m => { m.Science *= 1.15f; m.Happiness += 2f; });

            // -------- Transcendent (the Ascension victory project) --------
            Add("Ascension Project", Era.Transcendent, TechTag.Science, Tuning.AscensionCost,
                "VICTORY: your civilization transcends", m => m.AscensionDone = true);
        }

        /// <summary>Era is a function of how much a society knows.</summary>
        public static Era EraOf(int techsKnown)
        {
            int idx = techsKnown / TechsPerEra;
            if (idx > (int)Era.Transcendent) idx = (int)Era.Transcendent;
            return (Era)idx;
        }
    }
}
