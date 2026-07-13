namespace Eternity
{
    /// <summary>
    /// Every gameplay constant in one place, grouped and annotated.
    /// This file IS the balance pass surface — tweak here, not in systems.
    /// </summary>
    public static class Tuning
    {
        // ---------------- TIME ----------------
        // Years of sim time per real second at 1x speed, indexed by Era.
        // Ancient ages fly; modern ages crawl (denser decisions per year).
        public static readonly float[] EraYearsPerSecond =
            { 9f, 7f, 6f, 5f, 4f, 3f, 2.4f, 1.8f, 1.2f, 1.0f };
        public const float MaxSubStepYears = 0.25f;   // sim stability cap
        public const int MaxStepsPerFrame = 60;       // spiral-of-death guard
        public const float StartYear = -4000f;
        public const float EndYear = 2500f;           // the Long Count (Legacy victory)

        // ---------------- WORLD ----------------
        public const int WorldSize = 64;
        public const float SeaLevel = 0.34f;
        public const int RiverCount = 9;
        public const float TileHeightScale = 2.2f;    // visual elevation exaggeration

        // ---------------- CITIES ----------------
        public const float StartingPop = 800f;
        public const float MinPop = 60f;
        public const int CityWorkRadius = 2;          // tiles a city works/claims
        public const int CityMinDistance = 4;         // min spacing between cities
        public const int BaseWorkTiles = 4;           // tiles worked at tiny pop
        public const float PopPerWorkTile = 1500f;    // extra tile per this many people
        public const float FoodPerThousand = 1.0f;    // food units to feed 1000 people / yr
        public const float BasePopCap = 2500f;        // housing floor
        public const float PopCapPerDevLevel = 0.35f; // cap multiplier per total district level
        public const float BaseGrowth = 0.0022f;      // per year
        public const float SurplusGrowth = 0.005f;    // extra growth at full food surplus
        public const float StarvationRate = 0.06f;    // death rate scale in famine
        public const float PlateauPop = 5500f;        // growth halves here (×Health² lifts it)
        public const float DistrictCostBase = 8f;     // invest cost = (lvl+1)^2 * base
        public const float DeforestRate = 0.010f;     // forest stock loss / workshop level / yr

        // District yield multipliers per level
        public const float FarmYield = 0.12f;
        public const float WorkshopYield = 0.12f;
        public const float MarketYield = 0.15f;
        public const float AcademyYield = 0.12f;
        public const float TempleFaith = 0.15f;
        public const float WallDefense = 0.20f;

        // ---------------- HAPPINESS -> BEHAVIOR ----------------
        public const float HappyBase = 55f;
        public const float FoodHappy = 8f;
        public const float FamineUnhappy = 25f;
        public const float FaithUnityHappy = 8f;
        public const float SuppressedFaithUnhappy = 16f;
        public const float PolicyClashUnhappy = 22f;
        public const float PolicyAlignHappy = 5f;
        public const float WarWearyUnhappy = 25f;
        public const float RaidUnhappy = 18f;
        public const float CrowdUnhappy = 12f;
        public const float WelfareHappy = 14f;
        public const float GoldenAgeHappy = 10f;
        public const float HarshAgeUnhappy = 16f;     // pre-medicine misery, fades with Health tech
        public const float HappinessLerp = 0.35f;     // approach rate per year
        public const float EmigrateBelow = 45f;
        public const float EmigrateRate = 0.006f;     // pop fraction leaving / yr at 0 happiness margin
        public const float UnrestBelow = 40f;
        public const float UnrestRate = 0.45f;        // unrest gain / yr per point below threshold
        public const float UnrestDecay = 3.5f;        // unrest loss / yr when content
        public const float RiotAt = 55f;
        public const float RevoltAt = 100f;

        // ---------------- CULTURE DRIFT ----------------
        public const float GeoDriftRate = 0.020f;     // toward geography attractor / yr
        public const float PolicyDriftRate = 0.028f;  // toward national policy / yr
        public const float NeighborDriftRate = 0.008f;

        // ---------------- INFLUENCE (political capital) ----------------
        public const float InfluenceMax = 100f;
        public const float InfluenceRegen = 2.4f;     // per yr, scaled by legitimacy
        public const float CostPolicyPerUnit = 55f;   // influence per full slider unit moved
        public const float CostAdoptFaith = 30f;
        public const float CostSuppressFaith = 40f;
        public const float CostTolerateFaith = 10f;
        public const float CostRestrainCity = 10f;
        public const float CostFocus = 10f;
        public const float CostDeclareWar = 15f;
        public const float CostSuePeace = 20f;
        public const float CostGift = 10f;
        public const float PolicyInertia = 0.06f;     // slider drift toward target / yr

        // ---------------- MILITARY ----------------
        public const float GarrisonRatio = 0.03f;     // garrison target as fraction of pop (scaled)
        public const float GarrisonRegen = 0.25f;     // fraction of gap closed / yr
        public const float GarrisonUpkeep = 0.02f;    // gold / strength / yr
        public const float WartimeMobilization = 1.6f; // garrison surge while at war
        public const float ArmySpeed = 7f;            // tiles / yr
        public const float ArmyAttrition = 0.03f;     // strength loss / yr marching
        public const float AggressionRaidAt = 0.50f;  // city aggression needed to self-raid
        public const float RaidCooldownMin = 7f;
        public const float RaidCooldownMax = 18f;
        public const float RaidRange = 22f;           // tiles — must exceed typical inter-civ spacing
        public const float ConquerDefenseBonus = 1.6f; // sieges are hard; walls matter
        public const float ArmyGoldCost = 0.4f;       // gold per point of strength raised
        public const float RestrainYears = 25f;

        // ---------------- EXPANSION ----------------
        public const float SettlerGoldCost = 200f;
        public const float SettlerPulseChance = 0.006f; // autonomous settler waves per civ / yr (×Expansion)
        public const float SettlerPopCost = 500f;
        public const float SettlerMinCityPop = 2600f;
        public const int SettleSearchRange = 20;

        // ---------------- RELIGION ----------------
        public const int MaxReligions = 5;
        public const float ProphetPiety = 0.55f;      // city piety needed to birth a faith
        public const float FaithSpreadRate = 0.05f;   // share transfer / yr between neighbors
        public const float FaithRange = 7f;           // spread distance in tiles
        public const float StateFaithPressure = 0.03f;// extra conversion in own cities / yr
        public const float SuppressDecay = 0.05f;     // suppressed faith share loss / yr

        // ---------------- EVENTS ----------------
        public const float PlagueCrowding = 0.85f;    // pop/cap ratio where plague risk starts
        public const float PlagueChance = 0.012f;     // per crowded city per yr (pre-medicine)
        public const float GoldenAgeHappyReq = 72f;
        public const float GoldenAgeStreak = 20f;     // years above req to ignite
        public const float GoldenAgeDuration = 15f;
        public const float GoldenAgeBonus = 1.25f;    // all-yield multiplier

        // ---------------- VICTORY ----------------
        public const float DominationShare = 0.60f;   // world pop controlled
        public const float HarmonyHappy = 78f;
        public const float HarmonyYears = 120f;
        public const int HarmonyMinCities = 5;
        public const float AscensionCost = 60000f;    // knowledge for the final project

        // ---------------- AI ----------------
        public const float AIThinkInterval = 2.0f;    // sim years between AI passes
        public const int DefaultRivals = 3;
        public const int MaxCivs = 12;                // hard cap incl. rebels

        // ---------------- DECISIONS ----------------
        public const float DecisionLifetime = 18f;    // sim years before auto-resolve
        public const float IndecisionPenalty = 5f;    // influence lost when a decision expires
    }
}
