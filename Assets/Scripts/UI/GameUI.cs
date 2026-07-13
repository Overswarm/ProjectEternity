using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// The whole interface in immediate mode (code-only, art-free).
    /// Top bar · left tabbed panel · right inspector · bottom chronicle ·
    /// decision modals · founding flow · army targeting · game over.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static bool PointerOverUI;

        GameController gc;
        Simulation Sim => gc.Sim;
        Civilization Me => Sim.Player;

        enum Tab { None, Empire, Faith, Diplomacy, Victory }
        Tab tab = Tab.Empire;

        PolicySet uiPolicy;             // editing buffer for policy targets
        Vector2 leftScroll, rightScroll;

        City targetingFrom;             // raise-army flow
        City targetingChosen;

        GUIStyle head, small, wrap, mini;
        bool stylesBuilt;

        public void Init(GameController controller)
        {
            gc = controller;
            gc.Selection.OnCityClicked += OnCityClicked;
            uiPolicy = Me.PolicyTarget.Clone();
        }

        void OnCityClicked(City city)
        {
            if (targetingFrom == null || city == null) return;
            if (city.CivId == Me.Id || city == targetingFrom) return;
            targetingChosen = city;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                targetingFrom = null;
                targetingChosen = null;
            }
        }

        void BuildStyles()
        {
            if (stylesBuilt) return;
            stylesBuilt = true;
            head = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            mini = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            wrap = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
        }

        // ==================================================================
        void OnGUI()
        {
            if (gc == null || Sim == null) return;
            BuildStyles();

            float W = Screen.width, H = Screen.height;
            var over = false;

            var topRect = new Rect(0, 0, W, 30);
            var tabsRect = new Rect(4, 34, 326, 26);
            var leftRect = new Rect(4, 62, 326, H - 62 - 140);
            var rightRect = new Rect(W - 334, 34, 330, H - 34 - 140);
            var logRect = new Rect(4, H - 132, W - 8, 128);

            over |= DrawTopBar(topRect);
            over |= DrawTabs(tabsRect);
            if (tab != Tab.None) { DrawLeftPanel(leftRect); over |= leftRect.Contains(Event.current.mousePosition); }
            over |= DrawRightPanel(rightRect);
            over |= DrawChronicle(logRect);
            over |= DrawDecisionModal(W, H);
            over |= DrawTargetingUI(W, H);
            DrawFoundingBanner(W);
            over |= DrawGameOver(W, H);
            DrawTooltip(W, H);

            if (Event.current.type == EventType.Repaint)
                PointerOverUI = over;
        }

        // ------------------------------------------------------------------
        bool DrawTopBar(Rect r)
        {
            GUI.Box(r, "");
            GUILayout.BeginArea(r);
            GUILayout.BeginHorizontal();

            string era = TechCatalog.EraNames[(int)Me.Era];
            GUILayout.Label($"  {EventLog.FormatYear(Sim.Year)}  ·  {era}", head, GUILayout.Width(260));

            GUILayout.Label($"Gold {Mathf.RoundToInt(Me.Gold)}", small, GUILayout.Width(80));
            string research = Me.Researching != null
                ? $"{Me.Researching.Name} {Mathf.RoundToInt(Mathf.Clamp01(Me.Knowledge / Me.Researching.Cost) * 100f)}%"
                : "—";
            GUILayout.Label($"Research: {research}", small, GUILayout.Width(190));
            GUILayout.Label($"Influence {Mathf.RoundToInt(Me.Influence)}", small, GUILayout.Width(95));
            GUILayout.Label($"Pop {WorldView.FormatPop(Me.TotalPop)}", small, GUILayout.Width(80));
            GUILayout.Label($"Cities {Me.Cities.Count}", small, GUILayout.Width(60));
            if (Me.GoldenAgeActive(Sim.Year))
            {
                GUI.color = new Color(1f, 0.9f, 0.35f);
                GUILayout.Label("GOLDEN AGE", small, GUILayout.Width(90));
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            void SpeedBtn(string label, float v)
            {
                bool active = Mathf.Approximately(gc.Speed, v);
                GUI.color = active ? new Color(0.6f, 1f, 0.6f) : Color.white;
                if (GUILayout.Button(label, GUILayout.Width(38))) gc.Speed = v;
                GUI.color = Color.white;
            }
            SpeedBtn("❚❚", 0f); SpeedBtn("1x", 1f); SpeedBtn("2x", 2f); SpeedBtn("4x", 4f); SpeedBtn("8x", 8f);
            GUILayout.Space(6);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return r.Contains(Event.current.mousePosition);
        }

        // ------------------------------------------------------------------
        bool DrawTabs(Rect r)
        {
            GUILayout.BeginArea(r);
            GUILayout.BeginHorizontal();
            void TabBtn(string label, Tab t)
            {
                GUI.color = tab == t ? new Color(0.7f, 0.9f, 1f) : Color.white;
                if (GUILayout.Button(label))
                {
                    tab = tab == t ? Tab.None : t;
                    if (tab == Tab.Empire) uiPolicy = Me.PolicyTarget.Clone();
                }
                GUI.color = Color.white;
            }
            TabBtn("Empire", Tab.Empire);
            TabBtn("Faith", Tab.Faith);
            TabBtn("Diplomacy", Tab.Diplomacy);
            TabBtn("Victory", Tab.Victory);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return r.Contains(Event.current.mousePosition);
        }

        void DrawLeftPanel(Rect r)
        {
            GUI.Box(r, "");
            GUILayout.BeginArea(new Rect(r.x + 6, r.y + 4, r.width - 12, r.height - 8));
            leftScroll = GUILayout.BeginScrollView(leftScroll);
            switch (tab)
            {
                case Tab.Empire: DrawEmpireTab(); break;
                case Tab.Faith: DrawFaithTab(); break;
                case Tab.Diplomacy: DrawDiplomacyTab(); break;
                case Tab.Victory: DrawVictoryTab(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------
        void DrawEmpireTab()
        {
            GUILayout.Label($"{Me.Name} — the Eternal Throne", head);
            GUILayout.Label($"Legitimacy flows from happiness (avg {Mathf.RoundToInt(Me.AvgHappiness)}). " +
                            "Influence regenerates faster when your people believe in you.", mini);
            Bar(Me.Influence / Tuning.InfluenceMax, new Color(0.6f, 0.8f, 1f),
                $"Influence {Mathf.RoundToInt(Me.Influence)} / {Tuning.InfluenceMax}");
            GUILayout.Space(6);

            // ---- stance ----
            GUILayout.Label("Military stance (gates autonomous raiding)", small);
            GUILayout.BeginHorizontal();
            foreach (MilitaryStance st in System.Enum.GetValues(typeof(MilitaryStance)))
            {
                GUI.color = Me.Stance == st ? new Color(0.7f, 1f, 0.7f) : Color.white;
                if (GUILayout.Button(st.ToString())) Me.Stance = st;
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // ---- policy sliders ----
            GUILayout.Label("Law of the land (society drifts toward what you enact)", small);
            string[] lows = { "Liberty", "Secular", "Pacifist", "Consolidate", "Closed", "Collectivist" };
            string[] highs = { "Authority", "Theocratic", "Warlike", "Expand", "Open", "Free Market" };
            for (int i = 0; i < 6; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(lows[i], mini, GUILayout.Width(70));
                uiPolicy.SetAxis(i, GUILayout.HorizontalSlider(uiPolicy.GetAxis(i), 0f, 1f, GUILayout.Width(120)));
                GUILayout.Label(highs[i], mini, GUILayout.Width(75));
                GUILayout.Label($"now {Me.Policies.GetAxis(i):0.00}", mini);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("National spending", small);
            uiPolicy.SpendMilitary = SpendSlider("Military", uiPolicy.SpendMilitary);
            uiPolicy.SpendInfra = SpendSlider("Infrastructure", uiPolicy.SpendInfra);
            uiPolicy.SpendScience = SpendSlider("Science", uiPolicy.SpendScience);
            uiPolicy.SpendWelfare = SpendSlider("Welfare", uiPolicy.SpendWelfare);

            float cost = uiPolicy.TotalDelta(Me.PolicyTarget) * Tuning.CostPolicyPerUnit;
            GUILayout.BeginHorizontal();
            GUI.enabled = cost > 0.5f && Me.Influence >= cost;
            if (GUILayout.Button($"Enact ({Mathf.CeilToInt(cost)} influence)"))
            {
                if (Me.SpendInfluence(cost))
                {
                    Me.PolicyTarget = uiPolicy.Clone();
                    Me.PolicyTarget.NormalizeSpending();
                    uiPolicy = Me.PolicyTarget.Clone();
                    Sim.Log.Log(Sim.Year, "The throne enacts new law. Society will take years to bend.",
                        new Color(0.8f, 0.9f, 1f));
                }
            }
            GUI.enabled = true;
            if (GUILayout.Button("Reset")) uiPolicy = Me.PolicyTarget.Clone();
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // ---- research focus ----
            GUILayout.Label($"Research focus ({Tuning.CostFocus} influence to change)", small);
            GUILayout.BeginHorizontal();
            void FocusBtn(string label, TechTag? f)
            {
                bool active = Me.Focus.HasValue == f.HasValue && (!f.HasValue || Me.Focus.Value == f.Value);
                GUI.color = active ? new Color(0.7f, 1f, 0.7f) : Color.white;
                if (GUILayout.Button(label, mini2Btn) && !active && Me.SpendInfluence(Tuning.CostFocus))
                {
                    Me.Focus = f;
                    Me.Researching = null; // re-pick with new weighting
                }
                GUI.color = Color.white;
            }
            FocusBtn("None", null);
            FocusBtn("Mil", TechTag.Military);
            FocusBtn("Econ", TechTag.Economy);
            FocusBtn("Sci", TechTag.Science);
            FocusBtn("Faith", TechTag.Faith);
            FocusBtn("Civic", TechTag.Civic);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            gc.AutoPauseOnDecision = GUILayout.Toggle(gc.AutoPauseOnDecision, " Pause when a decision arrives");
            GUILayout.Label($"War weariness: {Mathf.RoundToInt(Me.WarWeariness * 100)}%", mini);
        }

        GUIStyle mini2Btn => GUI.skin.button;

        float SpendSlider(string label, float v)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, mini, GUILayout.Width(90));
            v = GUILayout.HorizontalSlider(v, 0.05f, 0.7f, GUILayout.Width(140));
            GUILayout.Label($"{v:0.00}", mini);
            GUILayout.EndHorizontal();
            return v;
        }

        // ------------------------------------------------------------------
        void DrawFaithTab()
        {
            GUILayout.Label("Faiths of the World", head);
            if (Sim.Religions.Religions.Count == 0)
            {
                GUILayout.Label("No prophets yet. Pious cities will birth faiths in time.", wrap);
                return;
            }
            foreach (var faith in Sim.Religions.Religions)
            {
                float share = Sim.Religions.CivShare(Me, faith.Id);
                GUI.color = faith.Color;
                GUILayout.Label($"{faith.Name}", head);
                GUI.color = Color.white;
                string status = Me.StateReligion == faith.Id ? "STATE FAITH"
                              : Me.Suppressed.Contains(faith.Id) ? "SUPPRESSED" : "tolerated";
                GUILayout.Label($"{faith.Temperament} · {Mathf.RoundToInt(share * 100)}% of your people · {status}", mini);
                Bar(share, faith.Color, null);

                GUILayout.BeginHorizontal();
                if (Me.StateReligion != faith.Id &&
                    GUILayout.Button($"Adopt ({Tuning.CostAdoptFaith})") &&
                    Me.SpendInfluence(Tuning.CostAdoptFaith))
                {
                    Me.StateReligion = faith.Id;
                    Me.Suppressed.Remove(faith.Id);
                    Sim.Log.Log(Sim.Year, $"{Me.Name} adopts {faith.Name} as the faith of the realm.", faith.Color);
                }
                if (!Me.Suppressed.Contains(faith.Id) &&
                    GUILayout.Button($"Suppress ({Tuning.CostSuppressFaith})") &&
                    Me.SpendInfluence(Tuning.CostSuppressFaith))
                {
                    Me.Suppressed.Add(faith.Id);
                    if (Me.StateReligion == faith.Id) Me.StateReligion = -1;
                    Sim.Log.Log(Sim.Year, $"{Me.Name} outlaws {faith.Name}. Its faithful seethe.",
                        new Color(1f, 0.5f, 0.4f));
                }
                if ((Me.Suppressed.Contains(faith.Id) || Me.StateReligion == faith.Id) &&
                    GUILayout.Button($"Tolerate ({Tuning.CostTolerateFaith})") &&
                    Me.SpendInfluence(Tuning.CostTolerateFaith))
                {
                    Me.Suppressed.Remove(faith.Id);
                    if (Me.StateReligion == faith.Id) Me.StateReligion = -1;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
            }
        }

        // ------------------------------------------------------------------
        void DrawDiplomacyTab()
        {
            GUILayout.Label("The Other Thrones", head);
            bool any = false;
            foreach (var other in Sim.Civs)
            {
                if (other == Me || other.Eliminated) continue;
                if (!Sim.Diplo.HasMet(Me.Id, other.Id)) continue;
                any = true;

                GUI.color = other.Color;
                GUILayout.Label($"{other.Name}{(other.IsRebel ? " (rebels)" : "")}", head);
                GUI.color = Color.white;

                float rel = Sim.Diplo.Relation(Me.Id, other.Id);
                bool war = Sim.Diplo.AtWar(Me.Id, other.Id);
                GUILayout.Label($"{TechCatalog.EraNames[(int)other.Era]} · pop {WorldView.FormatPop(other.TotalPop)} · " +
                                $"{other.Cities.Count} cities · relations {Mathf.RoundToInt(rel)}" +
                                (war ? " · AT WAR" : ""), mini);
                Bar((rel + 100f) / 200f, war ? new Color(1f, 0.35f, 0.3f) : new Color(0.55f, 0.8f, 0.55f), null);

                GUILayout.BeginHorizontal();
                if (!war && GUILayout.Button($"Declare War ({Tuning.CostDeclareWar})") &&
                    Me.SpendInfluence(Tuning.CostDeclareWar))
                    Sim.DeclareWar(Me, other, "the throne wills it");
                if (war && GUILayout.Button($"Sue for Peace ({Tuning.CostSuePeace} + 80g)") &&
                    Me.Gold >= 80f && Me.SpendInfluence(Tuning.CostSuePeace))
                {
                    Me.Gold -= 80f; other.Gold += 80f;
                    Sim.MakePeace(Me, other);
                }
                if (!war && GUILayout.Button($"Send Gift ({Tuning.CostGift} + 50g)") &&
                    Me.Gold >= 50f && Me.SpendInfluence(Tuning.CostGift))
                {
                    Me.Gold -= 50f; other.Gold += 50f;
                    Sim.Diplo.ChangeRelation(Me.Id, other.Id, 15f);
                    Sim.Log.Log(Sim.Year, $"A caravan of gifts leaves for {other.Name}.", new Color(0.8f, 0.9f, 0.8f));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
            }
            if (!any) GUILayout.Label("You have met no other peoples. The world feels empty... for now.", wrap);
        }

        // ------------------------------------------------------------------
        void DrawVictoryTab()
        {
            GUILayout.Label("Four Ways to Be Remembered", head);
            var v = Sim.Victory;

            GUILayout.Label($"Dominion — control {Mathf.RoundToInt(Tuning.DominationShare * 100)}% of humanity", small);
            Bar(v.DominionProgress(Me), new Color(1f, 0.5f, 0.4f), null);

            GUILayout.Label("Ascension — complete the final project", small);
            Bar(v.AscensionProgress(Me), new Color(0.55f, 0.75f, 1f), null);

            GUILayout.Label($"Harmony — {Tuning.HarmonyYears:0} years of contentment, {Tuning.HarmonyMinCities}+ cities, at peace", small);
            Bar(v.HarmonyProgress(Me), new Color(0.55f, 0.9f, 0.6f), null);

            GUILayout.Label($"Legacy — highest score when the Long Count ends ({EventLog.FormatYear(Tuning.EndYear)})", small);
            GUILayout.Space(4);
            GUILayout.Label("Legacy standings:", small);
            foreach (var civ in Sim.Civs)
            {
                if (civ.Eliminated || civ.IsRebel) continue;
                GUI.color = civ.Color;
                GUILayout.Label($"  {civ.Name}: {Mathf.RoundToInt(civ.Legacy)}" +
                                (Sim.Diplo.HasMet(Me.Id, civ.Id) || civ == Me ? "" : " (unknown)"), small);
                GUI.color = Color.white;
            }
            GUILayout.Space(6);
            GUILayout.Label("Rivals' best progress:", small);
            foreach (var civ in Sim.Civs)
            {
                if (civ == Me || civ.Eliminated || civ.IsRebel || !Sim.Diplo.HasMet(Me.Id, civ.Id)) continue;
                float best = Mathf.Max(v.DominionProgress(civ), v.AscensionProgress(civ), v.HarmonyProgress(civ));
                GUI.color = civ.Color;
                GUILayout.Label($"  {civ.Name}", mini);
                GUI.color = Color.white;
                Bar(best, civ.Color, null);
            }
        }

        // ------------------------------------------------------------------
        bool DrawRightPanel(Rect r)
        {
            var sel = gc.Selection;
            var city = sel.SelectedCity;
            var tile = sel.SelectedTile;
            if (city == null && tile == null) return false;

            GUI.Box(r, "");
            GUILayout.BeginArea(new Rect(r.x + 6, r.y + 4, r.width - 12, r.height - 8));
            rightScroll = GUILayout.BeginScrollView(rightScroll);

            if (city != null) DrawCityPanel(city);
            else DrawTilePanel(tile);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return r.Contains(Event.current.mousePosition);
        }

        void DrawTilePanel(Tile t)
        {
            GUILayout.Label($"{BiomeInfo.Get(t.Biome).Name}{(t.River ? " · River" : "")}", head);
            GUILayout.Label($"Food {t.Food:0.0} · Materials {t.Materials:0.0} · Commerce {t.Commerce:0.0}", small);
            if (t.Biome == Biome.Forest)
                GUILayout.Label($"Timber stock: {Mathf.RoundToInt(t.ForestStock * 100)}%", small);
            if (t.OwnerCiv >= 0)
            {
                var owner = Sim.GetCiv(t.OwnerCiv);
                GUI.color = owner.Color;
                GUILayout.Label($"Territory of {owner.Name}", small);
                GUI.color = Color.white;
            }

            if (Sim.State == SimState.Founding && t.Land)
            {
                float score = Sim.Settlement.ScoreSite(t, Me);
                if (score > float.MinValue)
                {
                    GUILayout.Space(8);
                    GUILayout.Label($"Site quality: {score:0.0}", small);
                    GUILayout.Label("Geography writes your people's first draft: deserts breed raiders, " +
                                    "rivers breed merchants, mountains breed smiths.", mini);
                    GUI.color = new Color(1f, 0.95f, 0.5f);
                    if (GUILayout.Button("⚑  FOUND YOUR CAPITAL HERE"))
                    {
                        Sim.FoundCity(Me, t);
                        gc.Speed = 1f;
                        gc.Selection.Deselect();
                    }
                    GUI.color = Color.white;
                }
                else GUILayout.Label("Cannot settle here.", small);
            }
        }

        void DrawCityPanel(City city)
        {
            var owner = Sim.GetCiv(city.CivId);
            GUI.color = owner.Color;
            GUILayout.Label($"{city.Name} — {city.SpecName}", head);
            GUI.color = Color.white;
            GUILayout.Label($"{owner.Name}{(city.SelfRule ? " · self-rule" : "")} · founded {EventLog.FormatYear(city.Founded)}", mini);

            GUILayout.Label($"Population {WorldView.FormatPop(city.Population)} / cap {WorldView.FormatPop(city.PopCap)}", small);
            Bar(Mathf.Clamp01(city.Happiness / 100f),
                city.Happiness > 60 ? new Color(0.5f, 0.9f, 0.5f) :
                city.Happiness > 40 ? new Color(0.95f, 0.85f, 0.4f) : new Color(1f, 0.45f, 0.35f),
                $"Happiness {Mathf.RoundToInt(city.Happiness)}");
            if (city.Unrest > 5f)
                Bar(Mathf.Clamp01(city.Unrest / 100f), new Color(1f, 0.4f, 0.3f), $"Unrest {Mathf.RoundToInt(city.Unrest)}");

            GUILayout.Label($"Food {city.FoodYield:0.0} (need {city.FoodNeed:0.0}) · Prod {city.MatYield:0.0} · " +
                            $"Gold {city.GoldYield:0.0} · Sci {city.SciYield:0.0}", mini);
            GUILayout.Label($"Garrison {Mathf.RoundToInt(city.Garrison)}", small);

            // ---- personality ----
            GUILayout.Space(4);
            GUILayout.Label("Character of the people", small);
            for (int i = 0; i < 6; i++)
                Bar(city.Pers.Get(i), new Color(0.7f, 0.7f, 0.9f), CityPersonality.AxisNames[i], 12);

            // ---- districts ----
            GUILayout.Space(4);
            GUILayout.Label("Districts (the city builds these itself)", small);
            for (int i = 0; i < 6; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{(District)i} {city.Districts[i]:0.0}", mini, GUILayout.Width(110));
                if (city.CivId == Me.Id)
                {
                    float cost = 40f + city.Districts[i] * 30f;
                    if (GUILayout.Button($"Invest {Mathf.CeilToInt(cost)}g", GUILayout.Width(90)) && Me.Gold >= cost)
                    {
                        Me.Gold -= cost;
                        city.Districts[i] += 1f;
                    }
                }
                GUILayout.EndHorizontal();
            }

            // ---- faiths ----
            if (city.Faiths.Count > 1)
            {
                GUILayout.Space(4);
                GUILayout.Label("Faiths", small);
                foreach (var kv in city.Faiths)
                {
                    if (kv.Value < 0.03f) continue;
                    string name = kv.Key < 0 ? "Folk beliefs" : Sim.Religions.Get(kv.Key).Name;
                    Color c = kv.Key < 0 ? new Color(0.65f, 0.6f, 0.5f) : Sim.Religions.Get(kv.Key).Color;
                    Bar(kv.Value, c, $"{name} {Mathf.RoundToInt(kv.Value * 100)}%", 12);
                }
            }

            // ---- why are they (un)happy ----
            GUILayout.Space(4);
            GUILayout.Label("Why the mood:", small);
            foreach (var f in city.HappyBreakdown)
            {
                GUI.color = f.Value >= 0 ? new Color(0.7f, 0.9f, 0.7f) : new Color(1f, 0.6f, 0.55f);
                GUILayout.Label($"  {(f.Value >= 0 ? "+" : "")}{f.Value:0.#}  {f.Name}", mini);
                GUI.color = Color.white;
            }

            // ---- player actions ----
            if (city.CivId == Me.Id)
            {
                GUILayout.Space(6);
                GUILayout.Label("Acts of the throne", small);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Festival (50g)") && Me.Gold >= 50f)
                {
                    Me.Gold -= 50f;
                    city.Happiness += 6f;
                    Sim.Log.Log(Sim.Year, $"A festival fills the streets of {city.Name}.",
                        new Color(1f, 0.9f, 0.6f), city.Tile.X, city.Tile.Y);
                }
                if (city.Garrison >= 25f && GUILayout.Button("Raise Army"))
                {
                    targetingFrom = city;
                    targetingChosen = null;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"Settlers ({Tuning.SettlerGoldCost}g)"))
                    Sim.Settlement.TrySpawnSettler(Me, manual: true, from: city);
                if (city.Pers.Aggression > Tuning.AggressionRaidAt && Sim.Year >= city.RestrainedUntil &&
                    GUILayout.Button($"Restrain raiders ({Tuning.CostRestrainCity} inf)") &&
                    Me.SpendInfluence(Tuning.CostRestrainCity))
                {
                    city.RestrainedUntil = Sim.Year + Tuning.RestrainYears;
                    Sim.Log.Log(Sim.Year, $"The throne reins in the warbands of {city.Name} — for a generation.",
                        new Color(0.8f, 0.85f, 1f));
                }
                GUILayout.EndHorizontal();
                if (Sim.Year < city.RestrainedUntil)
                    GUILayout.Label($"Raiders restrained until {EventLog.FormatYear(city.RestrainedUntil)}", mini);
            }
        }

        // ------------------------------------------------------------------
        bool DrawChronicle(Rect r)
        {
            GUI.Box(r, "");
            GUILayout.BeginArea(new Rect(r.x + 6, r.y + 2, r.width - 12, r.height - 4));
            var entries = Sim.Log.Entries;
            int show = Mathf.Min(7, entries.Count);
            for (int i = entries.Count - show; i < entries.Count; i++)
            {
                var e = entries[i];
                GUILayout.BeginHorizontal();
                GUI.color = e.Color;
                if (GUILayout.Button($"{EventLog.FormatYear(e.Year)} — {e.Text}", mini) && e.HasLocation)
                    gc.Rig.JumpTo(e.TileX, e.TileY);
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
            return r.Contains(Event.current.mousePosition);
        }

        // ------------------------------------------------------------------
        bool DrawDecisionModal(float W, float H)
        {
            if (Sim.Decisions.Pending.Count == 0) return false;
            var d = Sim.Decisions.Pending[0];

            var r = new Rect(W / 2f - 260, H / 2f - 190, 520, 380);
            GUI.Box(r, "");
            GUI.Box(r, ""); // double for opacity
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));

            GUI.color = new Color(1f, 0.95f, 0.6f);
            GUILayout.Label(d.Title, head);
            GUI.color = Color.white;
            GUILayout.Label(d.Desc, wrap);
            GUILayout.Label($"(Left unanswered, resolves by itself in {Mathf.Max(0, Mathf.RoundToInt(d.Expires - Sim.Year))} years " +
                            $"and costs you {Tuning.IndecisionPenalty} influence.)", mini);
            GUILayout.Space(8);

            for (int i = 0; i < d.Options.Count; i++)
            {
                var opt = d.Options[i];
                if (GUILayout.Button(opt.Label))
                    Sim.Decisions.Choose(d, i);
                GUILayout.Label("   " + opt.Detail, mini);
                GUILayout.Space(4);
            }
            GUILayout.EndArea();
            return r.Contains(Event.current.mousePosition) || true; // modal blocks the world
        }

        // ------------------------------------------------------------------
        bool DrawTargetingUI(float W, float H)
        {
            if (targetingFrom == null) return false;

            if (targetingChosen == null)
            {
                var r = new Rect(W / 2f - 240, 34, 480, 44);
                GUI.Box(r, "");
                GUI.color = new Color(1f, 0.8f, 0.5f);
                GUI.Label(new Rect(r.x + 10, r.y + 4, r.width - 20, 36),
                    $"Army of {targetingFrom.Name} ({Mathf.RoundToInt(targetingFrom.Garrison * 0.6f)} strong) — " +
                    "click an enemy city to target it. Esc cancels.");
                GUI.color = Color.white;
                return r.Contains(Event.current.mousePosition);
            }

            var m = new Rect(W / 2f - 200, H / 2f - 80, 400, 160);
            GUI.Box(m, "");
            GUILayout.BeginArea(new Rect(m.x + 12, m.y + 8, m.width - 24, m.height - 16));
            var enemyCiv = Sim.GetCiv(targetingChosen.CivId);
            GUILayout.Label($"March on {targetingChosen.Name} ({enemyCiv.Name})", head);
            bool atWar = Sim.Diplo.AtWar(Me.Id, targetingChosen.CivId);
            GUILayout.Label(atWar ? "You are at war." : "You are at peace — conquest will DECLARE WAR.", small);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Raid (steal & burn)"))
            {
                Sim.Military.LaunchArmy(targetingFrom, targetingChosen, Mission.Raid, targetingFrom.Garrison * 0.6f);
                targetingFrom = null; targetingChosen = null;
            }
            if (GUILayout.Button("Conquer (take the city)"))
            {
                Sim.Military.LaunchArmy(targetingFrom, targetingChosen, Mission.Conquer, targetingFrom.Garrison * 0.6f);
                targetingFrom = null; targetingChosen = null;
            }
            if (GUILayout.Button("Cancel")) { targetingFrom = null; targetingChosen = null; }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return true;
        }

        // ------------------------------------------------------------------
        void DrawFoundingBanner(float W)
        {
            if (Sim.State != SimState.Founding) return;
            var r = new Rect(W / 2f - 300, 40, 600, 54);
            GUI.Box(r, "");
            GUI.color = new Color(1f, 0.95f, 0.6f);
            GUI.Label(new Rect(r.x + 12, r.y + 4, r.width - 24, 46),
                "CHOOSE YOUR HOMELAND — click a tile, inspect it, then found your capital.\n" +
                "The land will shape who your people become.", small);
            GUI.color = Color.white;
        }

        // ------------------------------------------------------------------
        bool DrawGameOver(float W, float H)
        {
            if (!Sim.Victory.GameOver) return false;
            var v = Sim.Victory;
            var r = new Rect(W / 2f - 280, H / 2f - 140, 560, 280);
            GUI.Box(r, ""); GUI.Box(r, ""); GUI.Box(r, "");
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 14, r.width - 32, r.height - 28));

            bool playerWon = v.Winner == Me;
            GUI.color = playerWon ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.5f, 0.4f);
            GUILayout.Label(playerWon ? $"VICTORY — {v.VictoryType}" :
                v.Winner == null ? "DEFEAT" : $"{v.Winner.Name} WINS — {v.VictoryType}", head);
            GUI.color = Color.white;
            GUILayout.Label(v.Message, wrap);
            GUILayout.Space(8);
            GUILayout.Label($"The world stands at {EventLog.FormatYear(Sim.Year)}. Your legacy: {Mathf.RoundToInt(Me.Legacy)}.", small);
            GUILayout.Space(12);
            if (GUILayout.Button("Begin a New World"))
                gc.NewWorld();
            GUILayout.EndArea();
            return true;
        }

        // ------------------------------------------------------------------
        void DrawTooltip(float W, float H)
        {
            var t = gc.Selection.HoverTile;
            if (t == null || PointerOverUI) return;
            string txt = $"{BiomeInfo.Get(t.Biome).Name}{(t.River ? " · River" : "")}";
            if (t.City != null) txt = $"{t.City.Name} · {txt}";
            else if (t.OwnerCiv >= 0) txt += $" · {Sim.GetCiv(t.OwnerCiv).Name}";
            var size = small.CalcSize(new GUIContent(txt));
            var r = new Rect(Input.mousePosition.x + 16, H - Input.mousePosition.y + 12, size.x + 12, 22);
            GUI.Box(r, "");
            GUI.Label(new Rect(r.x + 6, r.y + 2, size.x, 18), txt, small);
        }

        // ------------------------------------------------------------------
        void Bar(float frac, Color color, string label, int height = 16)
        {
            var r = GUILayoutUtility.GetRect(120, height, GUILayout.ExpandWidth(true));
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (!string.IsNullOrEmpty(label))
                GUI.Label(new Rect(r.x + 4, r.y - 1, r.width - 8, r.height + 2), label, mini);
        }
    }
}
