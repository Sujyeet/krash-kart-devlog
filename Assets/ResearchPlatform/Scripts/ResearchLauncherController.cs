using UnityEngine;
using UnityEngine.SceneManagement;
using KartGame.GameFlow;

namespace ResearchPlatform
{
    public class ResearchLauncherController : MonoBehaviour
    {
        [Header("Research Configuration")]
        [Tooltip("Active experiment configuration profile. If null, default settings are used.")]
        public ResearchConfig activeConfig;

        [Header("Status")]
        private string statusMessage = "Ready to launch experiment.";

        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;
        private GUIStyle statusStyle;
        private bool stylesInitialized = false;

        private void Start()
        {
            if (activeConfig == null)
            {
                activeConfig = ScriptableObject.CreateInstance<ResearchConfig>();
                activeConfig.experimentName = "EXP_DEFAULT_RUN";
                activeConfig.researcherName = "Sujyeet";
                activeConfig.notes = "Default runtime configuration.";
            }

            Debug.Log($"[ResearchLauncher] Platform initialized for experiment: {activeConfig.experimentName}");
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 0.98f) }
            };

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.65f, 0.72f, 0.85f) }
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(20, 20, 20, 20)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 45,
                margin = new RectOffset(0, 0, 8, 8)
            };

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 0.85f, 0.5f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            float panelWidth = 520f;
            float panelHeight = 540f;
            float left = (Screen.width - panelWidth) * 0.5f;
            float top = (Screen.height - panelHeight) * 0.5f;

            Rect panelRect = new Rect(left, top, panelWidth, panelHeight);

            GUI.Box(panelRect, "", cardStyle);
            GUILayout.BeginArea(new Rect(panelRect.x + 20, panelRect.y + 20, panelRect.width - 40, panelRect.height - 40));

            GUILayout.Label("RESEARCH_ML", headerStyle);
            GUILayout.Label("AI Experimentation Control Center", subHeaderStyle);
            GUILayout.Space(15);

            // Experiment Details Card
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"<b>Experiment ID:</b> {activeConfig.experimentName}", GUI.skin.label);
            GUILayout.Label($"<b>Researcher:</b> {activeConfig.researcherName}", GUI.skin.label);
            GUILayout.Label($"<b>Target Mode:</b> {activeConfig.defaultAgentMode}", GUI.skin.label);
            GUILayout.Label($"<b>Notes:</b> {activeConfig.notes}", GUI.skin.label);
            GUILayout.EndVertical();

            GUILayout.Space(20);
            GUILayout.Label("<b>Select Experiment Gym to Launch:</b>", GUI.skin.label);
            GUILayout.Space(10);

            // Button A: DRL Training Scene
            if (GUILayout.Button($"[A] Launch DRL Agent Training\n({activeConfig.trainingSceneName})", buttonStyle))
            {
                LaunchExperiment(activeConfig.trainingSceneName, true, true, "Launching DRL Agent Training...");
            }

            // Button B: DRL Demo / Inference Scene
            if (GUILayout.Button($"[B] Launch DRL Agent Demo / Evaluation\n({activeConfig.inferenceSceneName})", buttonStyle))
            {
                LaunchExperiment(activeConfig.inferenceSceneName, true, true, "Launching DRL Agent Evaluation...");
            }

            // Button C: Manual Player Run Scene
            if (GUILayout.Button($"[C] Launch Manual Player Benchmark\n({activeConfig.manualRunSceneName})", buttonStyle))
            {
                LaunchExperiment(activeConfig.manualRunSceneName, true, false, "Launching Manual Player Benchmark...");
            }

            // Button D: Physics Playground Scene
            if (GUILayout.Button($"[D] Launch Physics & Track Gym\n({activeConfig.physicsPlaygroundSceneName})", buttonStyle))
            {
                LaunchExperiment(activeConfig.physicsPlaygroundSceneName, true, false, "Launching Physics Playground...");
            }

            GUILayout.Space(15);

            // Legacy Game Flow Option
            if (GUILayout.Button("Original Game Flow (Intro Menu)", GUI.skin.button))
            {
                statusMessage = "Loading original game menu...";
                SceneManager.LoadScene("IntroMenu");
            }

            GUILayout.Space(10);
            GUILayout.Label(statusMessage, statusStyle);

            GUILayout.EndArea();
        }

        private void LaunchExperiment(string sceneName, bool singlePlayer, bool includeMLAgents, string status)
        {
            statusMessage = status;

            // Set global game mode flags safely without modifying existing scripts
            GameModeManager.IsSinglePlayer = singlePlayer;
            GameModeManager.IncludeMLAgents = includeMLAgents;

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.Log($"[ResearchLauncher] Loading scene '{sceneName}' (SinglePlayer={singlePlayer}, IncludeMLAgents={includeMLAgents})");
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                statusMessage = $"Error: Scene '{sceneName}' is not in Build Settings or does not exist.";
                Debug.LogError($"[ResearchLauncher] Cannot load scene '{sceneName}'. Make sure it is added to Build Settings.");
            }
        }
    }
}
