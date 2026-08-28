#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KartGame.Editor
{
    public class QuickSceneSwitcher : EditorWindow
    {
        private const string IntroScenePath = "Assets/Karting/Scenes/IntroMenu.unity";
        private const string MainScenePath = "Assets/Karting/Scenes/MainScene.unity";
        private const string WinScenePath = "Assets/Karting/Scenes/WinScene.unity";
        private const string LoseScenePath = "Assets/Karting/Scenes/LoseScene.unity";

        [MenuItem("Scenes/Quick Scene Toolbar", false, 1)]
        [MenuItem("Krash Kart/Quick Scene Toolbar", false, 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<QuickSceneSwitcher>("Scenes Toolbar");
            window.minSize = new Vector2(240, 160);
            window.Show();
        }

        [MenuItem("Scenes/1. IntroMenu %1", false, 20)]
        public static void SwitchToIntroMenu() => LoadScene(IntroScenePath);

        [MenuItem("Scenes/2. MainScene %2", false, 21)]
        public static void SwitchToMainScene() => LoadScene(MainScenePath);

        [MenuItem("Scenes/3. WinScene %3", false, 22)]
        public static void SwitchToWinScene() => LoadScene(WinScenePath);

        [MenuItem("Scenes/4. LoseScene %4", false, 23)]
        public static void SwitchToLoseScene() => LoadScene(LoseScenePath);

        private void OnGUI()
        {
            string currentPath = SceneManager.GetActiveScene().path;

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Quick Scene Switcher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use top menu 'Scenes', shortcuts (Ctrl+1, Ctrl+2), or the buttons below.", MessageType.Info);
            GUILayout.Space(6);

            DrawSceneButton("IntroMenu (Lobby)", IntroScenePath, currentPath);
            DrawSceneButton("MainScene (Game Track)", MainScenePath, currentPath);
            DrawSceneButton("WinScene", WinScenePath, currentPath);
            DrawSceneButton("LoseScene", LoseScenePath, currentPath);
        }

        private void DrawSceneButton(string label, string path, string currentPath)
        {
            bool isCurrent = currentPath == path;
            GUI.backgroundColor = isCurrent ? new Color(0.4f, 0.9f, 0.4f) : Color.white;

            if (GUILayout.Button(isCurrent ? $"[ACTIVE] {label}" : label, GUILayout.Height(26)))
            {
                if (!isCurrent)
                {
                    LoadScene(path);
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private static void LoadScene(string path)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[QuickSceneSwitcher] Cannot switch scene while in Play Mode. Stop play mode first.");
                return;
            }

            if (SceneManager.GetActiveScene().path == path)
            {
                Debug.Log($"[QuickSceneSwitcher] Already in {path}");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Debug.Log($"[QuickSceneSwitcher] Loaded: {path}");
            }
        }
    }
}
#endif
