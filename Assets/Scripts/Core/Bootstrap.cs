using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// Code-only entry point: press Play in ANY scene and the whole game builds
    /// itself. No prefabs, no scene wiring, no assets.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnGameStart()
        {
            Build();
        }

        public static void Build()
        {
            var root = new GameObject("ProjectEternity");
            root.AddComponent<GameController>();
        }
    }
}
