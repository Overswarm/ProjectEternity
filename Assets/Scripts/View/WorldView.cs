using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    public class TileRef : MonoBehaviour { public int X, Y; }
    public class CityRef : MonoBehaviour { public City City; }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    /// <summary>
    /// Renders the whole simulation with primitives: height-scaled tile boxes,
    /// river strips, growing city cylinders, marching army cubes.
    /// </summary>
    public class WorldView : MonoBehaviour
    {
        Simulation sim;
        Material baseMat;
        Font uiFont;

        Renderer[,] tileRenderers;
        MaterialPropertyBlock mpb;

        readonly Dictionary<City, GameObject> cityMarkers = new Dictionary<City, GameObject>();
        readonly Dictionary<City, TextMesh> cityLabels = new Dictionary<City, TextMesh>();
        readonly Dictionary<City, Renderer> cityBodies = new Dictionary<City, Renderer>();
        readonly Dictionary<Army, GameObject> armyMarkers = new Dictionary<Army, GameObject>();

        GameObject selectionRing;
        float refreshTimer;

        static readonly int ColorId = Shader.PropertyToID("_Color");

        public void Init(Simulation sim)
        {
            this.sim = sim;
            mpb = new MaterialPropertyBlock();

            var shader = Shader.Find("Standard");
            baseMat = new Material(shader) { enableInstancing = true };
            baseMat.SetFloat("_Glossiness", 0.05f);

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildTiles();
            BuildSelectionRing();
            RefreshTileColors();
        }

        public float TileTopY(Tile t)
        {
            if (t.Water) return 0.28f;
            return 0.3f + (t.Elevation - Tuning.SeaLevel) * Tuning.TileHeightScale;
        }

        void BuildTiles()
        {
            var parent = new GameObject("Tiles").transform;
            parent.SetParent(transform);
            tileRenderers = new Renderer[sim.World.W, sim.World.H];

            for (int x = 0; x < sim.World.W; x++)
                for (int y = 0; y < sim.World.H; y++)
                {
                    var t = sim.World.Get(x, y);
                    float top = TileTopY(t);

                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"T{x}_{y}";
                    go.transform.SetParent(parent);
                    go.transform.position = new Vector3(x, top * 0.5f, y);
                    go.transform.localScale = new Vector3(1f, Mathf.Max(0.05f, top), 1f);
                    var r = go.GetComponent<Renderer>();
                    r.sharedMaterial = baseMat;
                    tileRenderers[x, y] = r;
                    var tr = go.AddComponent<TileRef>();
                    tr.X = x; tr.Y = y;

                    if (t.River && t.Land)
                    {
                        var river = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(river.GetComponent<Collider>());
                        river.transform.SetParent(go.transform.parent);
                        river.transform.position = new Vector3(x, top + 0.02f, y);
                        river.transform.localScale = new Vector3(0.35f, 0.04f, 1.0f);
                        var rr = river.GetComponent<Renderer>();
                        rr.sharedMaterial = baseMat;
                        var b = new MaterialPropertyBlock();
                        b.SetColor(ColorId, new Color(0.25f, 0.55f, 0.95f));
                        rr.SetPropertyBlock(b);
                    }
                }
        }

        void BuildSelectionRing()
        {
            selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(selectionRing.GetComponent<Collider>());
            selectionRing.transform.localScale = new Vector3(1.15f, 0.06f, 1.15f);
            var r = selectionRing.GetComponent<Renderer>();
            r.sharedMaterial = baseMat;
            var b = new MaterialPropertyBlock();
            b.SetColor(ColorId, new Color(1f, 0.95f, 0.4f));
            r.SetPropertyBlock(b);
            selectionRing.SetActive(false);
        }

        public void SetSelection(Tile t)
        {
            if (t == null) { selectionRing.SetActive(false); return; }
            selectionRing.SetActive(true);
            selectionRing.transform.position = new Vector3(t.X, TileTopY(t) + 0.03f, t.Y);
        }

        void Update()
        {
            if (sim == null) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (sim.WorldDirty || refreshTimer <= 0f)
            {
                refreshTimer = 0.5f;
                sim.WorldDirty = false;
                RefreshTileColors();
            }

            SyncCities();
            SyncArmies();
        }

        void RefreshTileColors()
        {
            for (int x = 0; x < sim.World.W; x++)
                for (int y = 0; y < sim.World.H; y++)
                {
                    var t = sim.World.Get(x, y);
                    var c = BiomeInfo.Get(t.Biome).Color;
                    if (t.OwnerCiv >= 0 && t.Land)
                        c = Color.Lerp(c, sim.GetCiv(t.OwnerCiv).Color, 0.32f);
                    mpb.SetColor(ColorId, c);
                    tileRenderers[x, y].SetPropertyBlock(mpb);

                    // land may have changed height class (deforestation keeps height, fine)
                }
        }

        // ---------------- cities ----------------
        void SyncCities()
        {
            // add new
            foreach (var city in sim.AllCities())
                if (!cityMarkers.ContainsKey(city))
                    CreateCityMarker(city);

            // update / remove
            var dead = new List<City>();
            foreach (var kv in cityMarkers)
            {
                var city = kv.Key;
                if (city.Razed || !StillExists(city)) { dead.Add(city); continue; }

                float h = 0.5f + Mathf.Log10(Mathf.Max(1f, city.Population / 100f)) * 0.7f;
                var body = cityBodies[city];
                body.transform.localScale = new Vector3(0.55f, h * 0.5f, 0.55f);
                body.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);

                mpb.SetColor(ColorId, sim.GetCiv(city.CivId).Color);
                body.SetPropertyBlock(mpb);

                var label = cityLabels[city];
                label.transform.localPosition = new Vector3(0f, h + 0.8f, 0f);
                label.text = $"{city.Name}\n{FormatPop(city.Population)} · {city.SpecName}";
                label.color = city.Happiness < 40f ? new Color(1f, 0.6f, 0.5f) : Color.white;
            }
            foreach (var c in dead)
            {
                Destroy(cityMarkers[c]);
                cityMarkers.Remove(c);
                cityLabels.Remove(c);
                cityBodies.Remove(c);
            }
        }

        bool StillExists(City city)
        {
            var civ = sim.GetCiv(city.CivId);
            return civ.Cities.Contains(city);
        }

        void CreateCityMarker(City city)
        {
            var root = new GameObject("City_" + city.Name);
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(city.Tile.X, TileTopY(city.Tile), city.Tile.Y);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.5f, 0.55f);
            body.GetComponent<Renderer>().sharedMaterial = baseMat;
            var cref = body.AddComponent<CityRef>();
            cref.City = city;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.font = uiFont;
            tm.GetComponent<MeshRenderer>().sharedMaterial = uiFont.material;
            tm.characterSize = 0.12f;
            tm.fontSize = 40;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            labelGo.AddComponent<Billboard>();

            cityMarkers[city] = root;
            cityLabels[city] = tm;
            cityBodies[city] = body.GetComponent<Renderer>();
        }

        // ---------------- armies ----------------
        void SyncArmies()
        {
            foreach (var army in sim.Armies)
            {
                if (!armyMarkers.TryGetValue(army, out var go))
                {
                    go = GameObject.CreatePrimitive(
                        army.Mission == Mission.Settle ? PrimitiveType.Sphere : PrimitiveType.Cube);
                    Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(transform);
                    go.GetComponent<Renderer>().sharedMaterial = baseMat;
                    armyMarkers[army] = go;
                }

                float s = army.Mission == Mission.Settle
                    ? 0.3f
                    : Mathf.Clamp(0.25f + army.Strength / 300f, 0.25f, 0.7f);
                go.transform.localScale = Vector3.one * s;

                var tile = army.CurrentTile(sim.World);
                float y = tile != null ? TileTopY(tile) : 0.4f;
                go.transform.position = new Vector3(army.Pos.x, y + s * 0.5f + 0.05f, army.Pos.y);

                var col = army.Mission == Mission.Settle
                    ? Color.white
                    : Color.Lerp(sim.GetCiv(army.CivId).Color, Color.black, 0.15f);
                mpb.SetColor(ColorId, col);
                go.GetComponent<Renderer>().SetPropertyBlock(mpb);
            }

            var gone = new List<Army>();
            foreach (var kv in armyMarkers)
                if (kv.Key.Dead || !sim.Armies.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var a in gone)
            {
                Destroy(armyMarkers[a]);
                armyMarkers.Remove(a);
            }
        }

        public static string FormatPop(float pop)
        {
            if (pop >= 1_000_000f) return (pop / 1_000_000f).ToString("0.0") + "M";
            if (pop >= 1_000f) return (pop / 1_000f).ToString("0.0") + "k";
            return Mathf.RoundToInt(pop).ToString();
        }
    }
}
