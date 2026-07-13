using UnityEngine;

namespace Eternity
{
    public enum MilitaryStance { Defensive, Balanced, Aggressive }

    /// <summary>
    /// The national character as law. Player sets targets; society drifts toward
    /// them slowly (inertia), and cities that clash with the law get unhappy.
    /// All axes 0..1.
    /// </summary>
    public class PolicySet
    {
        public float Authority = 0.5f;   // liberty <-> authoritarian
        public float Devotion = 0.5f;    // secular <-> theocratic
        public float Militarism = 0.4f;  // pacifist <-> warlike
        public float Expansion = 0.5f;   // consolidate <-> manifest destiny
        public float Openness = 0.5f;    // closed <-> open borders/trade
        public float Market = 0.5f;      // collectivist <-> free market

        // National spending allocation (normalized to sum 1).
        public float SpendMilitary = 0.25f;
        public float SpendInfra = 0.25f;
        public float SpendScience = 0.25f;
        public float SpendWelfare = 0.25f;

        public static readonly string[] AxisNames =
            { "Authority", "Devotion", "Militarism", "Expansion", "Openness", "Market" };

        public float GetAxis(int i)
        {
            switch (i)
            {
                case 0: return Authority;
                case 1: return Devotion;
                case 2: return Militarism;
                case 3: return Expansion;
                case 4: return Openness;
                default: return Market;
            }
        }

        public void SetAxis(int i, float v)
        {
            v = Mathf.Clamp01(v);
            switch (i)
            {
                case 0: Authority = v; break;
                case 1: Devotion = v; break;
                case 2: Militarism = v; break;
                case 3: Expansion = v; break;
                case 4: Openness = v; break;
                default: Market = v; break;
            }
        }

        public PolicySet Clone()
        {
            return (PolicySet)MemberwiseClone();
        }

        public float TotalDelta(PolicySet other)
        {
            float d = 0f;
            for (int i = 0; i < 6; i++) d += Mathf.Abs(GetAxis(i) - other.GetAxis(i));
            return d;
        }

        /// <summary>Societal inertia: actual law drifts toward the enacted target.</summary>
        public void MoveToward(PolicySet target, float dt)
        {
            float step = Tuning.PolicyInertia * dt;
            for (int i = 0; i < 6; i++)
                SetAxis(i, Mathf.MoveTowards(GetAxis(i), target.GetAxis(i), step));
            SpendMilitary = Mathf.MoveTowards(SpendMilitary, target.SpendMilitary, step);
            SpendInfra = Mathf.MoveTowards(SpendInfra, target.SpendInfra, step);
            SpendScience = Mathf.MoveTowards(SpendScience, target.SpendScience, step);
            SpendWelfare = Mathf.MoveTowards(SpendWelfare, target.SpendWelfare, step);
        }

        public void NormalizeSpending()
        {
            float sum = SpendMilitary + SpendInfra + SpendScience + SpendWelfare;
            if (sum <= 0.001f) { SpendMilitary = SpendInfra = SpendScience = SpendWelfare = 0.25f; return; }
            SpendMilitary /= sum; SpendInfra /= sum; SpendScience /= sum; SpendWelfare /= sum;
        }
    }
}
