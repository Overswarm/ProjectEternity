using UnityEngine;

namespace Eternity
{
    /// <summary>Pairwise relations, war state, and first-contact tracking.</summary>
    public class Diplomacy
    {
        readonly float[,] rel = new float[Tuning.MaxCivs, Tuning.MaxCivs];
        readonly bool[,] war = new bool[Tuning.MaxCivs, Tuning.MaxCivs];
        readonly bool[,] met = new bool[Tuning.MaxCivs, Tuning.MaxCivs];

        public float Relation(int a, int b) => rel[a, b];
        public bool AtWar(int a, int b) => a != b && war[a, b];
        public bool HasMet(int a, int b) => a != b && met[a, b];

        public void ChangeRelation(int a, int b, float delta)
        {
            rel[a, b] = Mathf.Clamp(rel[a, b] + delta, -100f, 100f);
            rel[b, a] = Mathf.Clamp(rel[b, a] + delta, -100f, 100f);
        }

        public void DriftRelation(int a, int b, float target, float dt)
        {
            float step = Mathf.Min(1f, 0.05f * dt);
            rel[a, b] += (target - rel[a, b]) * step;
            rel[b, a] += (target - rel[b, a]) * step;
        }

        public void SetWar(int a, int b, bool atWar) { war[a, b] = atWar; war[b, a] = atWar; }
        public void SetMet(int a, int b) { met[a, b] = true; met[b, a] = true; }

        public bool AtWarWithAnyone(int a, int civCount)
        {
            for (int i = 0; i < civCount; i++)
                if (i != a && war[a, i]) return true;
            return false;
        }

        public int WarCount(int a, int civCount)
        {
            int n = 0;
            for (int i = 0; i < civCount; i++)
                if (i != a && war[a, i]) n++;
            return n;
        }
    }
}
