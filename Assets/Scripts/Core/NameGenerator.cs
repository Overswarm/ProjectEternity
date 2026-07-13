using System.Collections.Generic;

namespace Eternity
{
    /// <summary>Flavorful procedural names for civs, cities, religions.</summary>
    public class NameGenerator
    {
        readonly Rng rng;
        readonly HashSet<string> used = new HashSet<string>();

        static readonly string[] CivNames =
        {
            "Aurelia", "Kethran", "Voskaya", "Thalassar", "Umbra", "Perindor",
            "Shen-Kai", "Ozryn", "Mardukka", "Elowen", "Tavros", "Qadesh",
        };

        static readonly string[] CityStart =
        {
            "Ash", "Riven", "Dusk", "Storm", "Gold", "Iron", "Sun", "Moon", "Oak",
            "Salt", "Wolf", "Raven", "High", "Deep", "Whit", "Black", "Green", "Red",
            "Stone", "Frost", "Ember", "Bright", "Mira", "Kar", "Tel", "Vor", "Zan",
        };

        static readonly string[] CityEnd =
        {
            "fall", "bend", "mere", "hold", "haven", "gate", "ford", "port", "spire",
            "reach", "mark", "wick", "dale", "moor", "crest", "watch", "run", "row",
            "os", "ium", "ara", "eth", "una", "is",
        };

        static readonly string[] FaithPatterns =
        {
            "The Way of {0}", "The {0} Creed", "Cult of {0}", "The {0} Flame",
            "Children of {0}", "The Path of {0}", "{0}ism", "The Covenant of {0}",
        };

        static readonly string[] FaithRoots =
        {
            "Aruna", "Sol", "Vesh", "the Deep", "Omu", "the Twin Moons", "Kor",
            "the Silent Sky", "Yssa", "the First River", "Dawn", "Thal", "the Ember",
        };

        public NameGenerator(Rng rng) { this.rng = rng; }

        public string CivName(int index)
        {
            return CivNames[index % CivNames.Length];
        }

        public string CityName()
        {
            for (int tries = 0; tries < 50; tries++)
            {
                string n = rng.Pick(CityStart) + rng.Pick(CityEnd);
                if (used.Add(n)) return n;
            }
            return "New" + rng.Pick(CityStart) + used.Count;
        }

        public string FaithName()
        {
            for (int tries = 0; tries < 50; tries++)
            {
                string n = string.Format(rng.Pick(FaithPatterns), rng.Pick(FaithRoots));
                if (used.Add(n)) return n;
            }
            return "The Nameless Faith";
        }
    }
}
