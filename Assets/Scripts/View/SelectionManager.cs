using UnityEngine;

namespace Eternity
{
    /// <summary>Raycast picking: tiles, cities, and hover for tooltips.</summary>
    public class SelectionManager : MonoBehaviour
    {
        Simulation sim;
        WorldView view;

        public Tile SelectedTile { get; private set; }
        public City SelectedCity { get; private set; }
        public Tile HoverTile { get; private set; }

        /// <summary>Fired on any left-click that lands on a city (used for army targeting).</summary>
        public System.Action<City> OnCityClicked;

        public void Init(Simulation sim, WorldView view)
        {
            this.sim = sim;
            this.view = view;
        }

        void Update()
        {
            if (sim == null || Camera.main == null) return;

            HoverTile = null;
            if (!GameUI.PointerOverUI)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    var cityRef = hit.collider.GetComponent<CityRef>();
                    var tileRef = hit.collider.GetComponent<TileRef>();

                    if (cityRef != null) HoverTile = cityRef.City.Tile;
                    else if (tileRef != null) HoverTile = sim.World.Get(tileRef.X, tileRef.Y);

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (cityRef != null) Select(cityRef.City.Tile, cityRef.City);
                        else if (tileRef != null)
                        {
                            var t = sim.World.Get(tileRef.X, tileRef.Y);
                            Select(t, t.City);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                Select(null, null);
        }

        void Select(Tile tile, City city)
        {
            SelectedTile = tile;
            SelectedCity = city != null && !city.Razed ? city : null;
            view.SetSelection(tile);
            if (SelectedCity != null)
                OnCityClicked?.Invoke(SelectedCity);
        }

        public void Deselect() => Select(null, null);
    }
}
