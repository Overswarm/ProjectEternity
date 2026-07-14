#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Eternity.EditorTools
{
    /// <summary>
    /// One-click standalone builds. Adds a "Project Eternity" menu to the editor
    /// menu bar. Because the whole game bootstraps itself at runtime
    /// (see Bootstrap.cs), any scene works — we just ship the (near-empty) Main.
    /// Output lands in a Builds/ folder beside the Assets/ folder.
    /// </summary>
    public static class BuildTool
    {
        const string Scene = "Assets/Scenes/Main.unity";

        [MenuItem("Project Eternity/Build/Windows (64-bit)")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "ProjectEternity.exe");

        [MenuItem("Project Eternity/Build/macOS")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "ProjectEternity.app");

        [MenuItem("Project Eternity/Build/Linux (64-bit)")]
        public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "ProjectEternity.x86_64");

        [MenuItem("Project Eternity/Build/WebGL (shareable link)")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "");

        [MenuItem("Project Eternity/Open Builds Folder")]
        public static void OpenBuildsFolder()
        {
            string dir = BuildsRoot();
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        static string BuildsRoot() =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");

        static void Build(BuildTarget target, string exeName)
        {
            // Ship standalone builds windowed so testers can close the window
            // (there's no in-game quit button yet). WebGL is always windowed.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

            string outDir = Path.Combine(BuildsRoot(), target.ToString());
            Directory.CreateDirectory(outDir);

            // WebGL builds into a folder; standalone builds name an executable.
            string location = string.IsNullOrEmpty(exeName) ? outDir : Path.Combine(outDir, exeName);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = location,
                target = target,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Project Eternity] Build succeeded ({summary.totalSize / (1024 * 1024)} MB) → {location}");
                EditorUtility.RevealInFinder(location);
            }
            else
            {
                Debug.LogError($"[Project Eternity] Build {summary.result}. See the Console for details.");
            }
        }
    }
}
#endif
