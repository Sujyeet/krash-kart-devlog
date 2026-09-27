#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace KartGame.Editor
{
    /// <summary>
    /// Creates and opens the QuickDrive offline test scene.
    /// Bypasses IntroMenu / Relay / Netcode entirely.
    /// Accessible via: Krash Kart > Quick Drive Setup
    /// </summary>
    public static class QuickDriveSetup
    {
        const string k_ScenePath = "Assets/Karting/Scenes/QuickDrive.unity";
        const string k_PlayerPrefab = "Assets/Karting/Prefabs/KartClassic/KartClassic_Player_notML.prefab";
        const string k_AgentPrefab  = "Assets/Karting/Prefabs/KartClassic/KartClassic_MLAgent.prefab";
        const string k_ObjectivePrefab = "Assets/Karting/Scenes/ObjectiveLaps.prefab";

        [MenuItem("Krash Kart/Quick Drive Setup", false, 20)]
        public static void OpenOrCreate()
        {
            // Save current scene
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            bool exists = File.Exists(k_ScenePath);
            var scene = exists
                ? UnityEditor.SceneManagement.EditorSceneManager.OpenScene(k_ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single)
                : UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);

            if (!exists)
                BuildScene(scene);

            // Register in build settings if missing
            EnsureInBuildSettings(k_ScenePath);
        }

        private static void BuildScene(Scene scene)
        {
            // --- Ground plane so karts don't fall ---
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.localScale = new Vector3(20f, 1f, 20f); // 200 x 200 units
            ground.layer = LayerMask.NameToLayer("Default");
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Karting/Art/Materials/Level/Ground.mat");
            if (groundMat != null)
                ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // --- Directional Light ---
            var lightObj = new GameObject("DirectionalLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // --- GameFlowManager (offline) ---
            var gmObj = new GameObject("GameManager");
            var gfm = gmObj.AddComponent<GameFlowManager>();
            gfm.autoFindKarts = true;
            gmObj.AddComponent<ObjectiveManager>();
            gmObj.AddComponent<TimeManager>();

            // --- Objective: 3 Laps ---
            var objPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_ObjectivePrefab);
            if (objPrefab != null)
            {
                var obj = PrefabUtility.InstantiatePrefab(objPrefab) as GameObject;
                obj.name = "ObjectiveLaps";
            }
            else
            {
                var objObj = new GameObject("ObjectiveLaps");
                var laps = objObj.AddComponent<ObjectiveCompleteLaps>();
                laps.lapsToComplete = 3;
            }

            // --- Player Kart (no ML, no NetworkObject) ---
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_PlayerPrefab);
            if (playerPrefab != null)
            {
                var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
                player.name = "KartClassic_Player";
                player.transform.position = new Vector3(0f, 0.6f, 5f);
                player.transform.rotation = Quaternion.identity;
                player.tag = "Player";
                gfm.playerKart = player.GetComponent<KartGame.KartSystems.ArcadeKart>();
            }
            else
            {
                Debug.LogWarning("[QuickDriveSetup] Player kart prefab not found at: " + k_PlayerPrefab);
            }

            // --- ML Agent Kart (inference / heuristic) ---
            var agentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_AgentPrefab);
            if (agentPrefab != null)
            {
                var agent = PrefabUtility.InstantiatePrefab(agentPrefab) as GameObject;
                agent.name = "KartClassic_MLAgent";
                agent.transform.position = new Vector3(-4f, 0.6f, 5f);
                agent.transform.rotation = Quaternion.identity;

                // Strip NetworkObject/NetworkTransform if present — not needed offline
                StripNetworkComponents(agent);
            }
            else
            {
                Debug.LogWarning("[QuickDriveSetup] ML Agent kart prefab not found at: " + k_AgentPrefab);
            }

            // --- Event System ---
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Save
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, k_ScenePath);
            Debug.Log("[QuickDriveSetup] QuickDrive scene created at: " + k_ScenePath);
        }

        private static void StripNetworkComponents(GameObject go)
        {
            // Remove Unity Netcode components that require a live NetworkManager
            string[] netTypes = new[]
            {
                "Unity.Netcode.NetworkObject",
                "Unity.Netcode.Components.NetworkTransform",
                "Unity.Netcode.Components.NetworkRigidbody",
                "OwnerAuthoritativeKart",
                "NetworkAISyncAdapter"
            };

            foreach (var typeName in netTypes)
            {
                var type = System.Type.GetType(typeName + ", Unity.Netcode.Runtime")
                        ?? System.Type.GetType(typeName + ", KartGame");
                if (type == null) continue;

                var comps = go.GetComponentsInChildren(type, true);
                foreach (var c in comps)
                {
                    Undo.DestroyObjectImmediate(c);
                }
            }
        }

        private static void EnsureInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return; // Already there
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            System.Array.Copy(scenes, newScenes, scenes.Length);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[QuickDriveSetup] Added QuickDrive to Build Settings.");
        }
    }
}
#endif
