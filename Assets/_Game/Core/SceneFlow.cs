using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarrotClash
{
    /// <summary>
    /// Central, static scene-transition helper (Tech Architecture scene structure). Loads the
    /// MainMenu, Gameplay (with its additive GameplayUI scene), and Tutorial scenes, and returns
    /// to the menu. All loads are async with a simple re-entrancy guard so overlapping requests
    /// (e.g. double-click) cannot stack scene loads.
    /// </summary>
    public static class SceneFlow
    {
        static bool isLoading;

        /// <summary>True while a transition is in flight; UI should disable navigation buttons.</summary>
        public static bool IsLoading => isLoading;

        /// <summary>Load the main menu (single scene, replaces everything).</summary>
        public static void LoadMainMenu() => Run(LoadSingle(GameConstants.SceneMainMenu));

        /// <summary>Load the gameplay map then additively load the HUD/GameplayUI scene on top.</summary>
        public static void LoadGameplay() => Run(LoadGameplayRoutine());

        /// <summary>Load the offline tutorial scene.</summary>
        public static void LoadTutorial() => Run(LoadSingle(GameConstants.SceneTutorial));

        /// <summary>Tear down gameplay and return to the main menu.</summary>
        public static void ReturnToMenu() => Run(LoadSingle(GameConstants.SceneMainMenu));

        // ----- Internals -----

        // Coroutines need a host MonoBehaviour; spin up a hidden persistent runner.
        static SceneFlowRunner runner;

        static void Run(IEnumerator routine)
        {
            if (isLoading) return;
            EnsureRunner();
            runner.StartCoroutine(Guarded(routine));
        }

        static IEnumerator Guarded(IEnumerator routine)
        {
            isLoading = true;
            yield return routine;
            isLoading = false;
        }

        static void EnsureRunner()
        {
            if (runner != null) return;
            var go = new GameObject("[SceneFlowRunner]");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<SceneFlowRunner>();
        }

        static IEnumerator LoadSingle(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null) yield break;
            while (!op.isDone) yield return null;
        }

        static IEnumerator LoadGameplayRoutine()
        {
            AsyncOperation main = SceneManager.LoadSceneAsync(GameConstants.SceneGameplay, LoadSceneMode.Single);
            if (main != null)
            {
                while (!main.isDone) yield return null;
            }

            AsyncOperation ui = SceneManager.LoadSceneAsync(GameConstants.SceneGameplayUI, LoadSceneMode.Additive);
            if (ui != null)
            {
                while (!ui.isDone) yield return null;
            }
        }

        /// <summary>Hidden persistent coroutine host for static scene transitions.</summary>
        sealed class SceneFlowRunner : MonoBehaviour { }
    }
}
