using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Owns the Simulation and the real-time clock. Years-per-second depends on the
    /// player's era (ancient ages flash by) times the chosen speed step.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public Simulation Sim { get; private set; }
        public CameraRig Rig { get; private set; }
        public WorldView View { get; private set; }
        public SelectionManager Selection { get; private set; }

        public float Speed = 1f;          // 0 (paused), 1, 2, 4, 8
        public bool AutoPauseOnDecision = true;

        int pendingSeen;

        void Awake()
        {
            int seed = System.Environment.TickCount;
            Sim = new Simulation(seed, Tuning.DefaultRivals);

            // clear any scene default camera so ours is the only one
            // (FindObjectsByType: works in 2022.3+ and Unity 6; FindObjectsOfType is deprecated in 6)
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Destroy(cam.gameObject);

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f);
            lightGo.transform.rotation = Quaternion.Euler(55f, 35f, 0f);
            lightGo.transform.SetParent(transform);
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.55f);

            Rig = new GameObject("CameraRig").AddComponent<CameraRig>();
            Rig.transform.SetParent(transform);
            Rig.Init(Tuning.WorldSize);

            View = new GameObject("WorldView").AddComponent<WorldView>();
            View.transform.SetParent(transform);
            View.Init(Sim);

            Selection = gameObject.AddComponent<SelectionManager>();
            Selection.Init(Sim, View);

            var ui = gameObject.AddComponent<GameUI>();
            ui.Init(this);
        }

        public float YearsPerSecond =>
            Tuning.EraYearsPerSecond[(int)Sim.Player.Era] * Speed;

        void Update()
        {
            HandleHotkeys();

            if (Sim.Victory.GameOver || Sim.State == SimState.Founding) return;

            // auto-pause the moment a new decision lands on the desk
            int pending = Sim.Decisions.Pending.Count;
            if (AutoPauseOnDecision && pending > pendingSeen && Speed > 0f)
                Speed = 0f;
            pendingSeen = pending;

            float yearsThisFrame = YearsPerSecond * Time.unscaledDeltaTime;
            int steps = 0;
            while (yearsThisFrame > 0f && steps < Tuning.MaxStepsPerFrame)
            {
                float dt = Mathf.Min(yearsThisFrame, Tuning.MaxSubStepYears);
                Sim.Tick(dt);
                yearsThisFrame -= dt;
                steps++;
                if (Sim.Victory.GameOver) break;
            }
        }

        void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Speed = Speed > 0f ? 0f : 1f;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Speed = 1f;
            if (Input.GetKeyDown(KeyCode.Alpha2)) Speed = 2f;
            if (Input.GetKeyDown(KeyCode.Alpha3)) Speed = 4f;
            if (Input.GetKeyDown(KeyCode.Alpha4)) Speed = 8f;
        }

        public void NewWorld()
        {
            Destroy(gameObject);
            Bootstrap.Build();
        }
    }
}
