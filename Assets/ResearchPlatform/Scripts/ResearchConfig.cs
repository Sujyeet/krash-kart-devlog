using UnityEngine;
using KartGame.AI;

namespace ResearchPlatform
{
    [CreateAssetMenu(fileName = "ResearchConfig", menuName = "Research Platform/Research Config", order = 1)]
    public class ResearchConfig : ScriptableObject
    {
        [Header("Experiment Identity")]
        [Tooltip("Identifier for logging and experiment tracking")]
        public string experimentName = "EXP_001_Baseline";

        [Tooltip("Researcher or run author")]
        public string researcherName = "Sujyeet";

        [TextArea(3, 5)]
        [Tooltip("Notes or parameters describing this experiment run")]
        public string notes = "Standard experiment run via Research Launcher.";

        [Header("Agent Configuration")]
        [Tooltip("Default mode to enforce when launching AI scenes")]
        public AgentMode defaultAgentMode = AgentMode.Inferencing;

        [Header("Target Scene Names")]
        public string trainingSceneName = "KartClassic_Training";
        public string inferenceSceneName = "KartClassic_TrainingDemo";
        public string manualRunSceneName = "MainScene";
        public string physicsPlaygroundSceneName = "PhysicsPlayground";
    }
}
