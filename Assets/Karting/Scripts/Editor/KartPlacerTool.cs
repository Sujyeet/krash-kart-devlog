#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace KartGame.Editor
{
    /// <summary>
    /// Scene View kart placement tool.
    /// Click anywhere on a mesh/collider surface in the Scene View to place the selected kart prefab.
    /// The kart is placed at the exact surface contact point + half-height offset so it sits ON the mesh,
    /// not inside it. Uses Physics.Raycast against all colliders in the scene.
    /// Accessible via: Krash Kart > Kart Placer Tool
    /// </summary>
    public class KartPlacerTool : EditorWindow
    {
        // --- Settings ---
        private GameObject m_KartPrefab;
        private float m_HeightOffset = 0.6f;
        private bool m_AlignToNormal = false;
        private bool m_PlacingActive = false;
        private Color m_PreviewColor = new Color(0f, 1f, 0.5f, 0.4f);

        // --- Runtime state ---
        private Vector3 m_PreviewPos;
        private Quaternion m_PreviewRot;
        private bool m_HasPreview;
        private GameObject m_PreviewInstance;

        [MenuItem("Krash Kart/Kart Placer Tool", false, 21)]
        public static void Open()
        {
            var win = GetWindow<KartPlacerTool>("Kart Placer");
            win.minSize = new Vector2(280, 220);
            win.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            DestroyPreview();
            m_PlacingActive = false;
        }

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Kart Placer Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click on any surface in the Scene View to place the kart precisely on the mesh.", MessageType.Info);

            GUILayout.Space(6);
            m_KartPrefab = (GameObject)EditorGUILayout.ObjectField("Kart Prefab", m_KartPrefab, typeof(GameObject), false);

            GUILayout.Space(4);
            m_HeightOffset = EditorGUILayout.Slider("Height Offset", m_HeightOffset, 0f, 2f);
            m_AlignToNormal = EditorGUILayout.Toggle("Align to Surface Normal", m_AlignToNormal);

            GUILayout.Space(10);

            GUI.color = m_PlacingActive ? new Color(0.4f, 1f, 0.4f) : Color.white;
            string btnLabel = m_PlacingActive ? "● PLACING ACTIVE — Click in Scene View" : "Start Placing";
            if (GUILayout.Button(btnLabel, GUILayout.Height(34)))
            {
                if (m_KartPrefab == null)
                {
                    // Try auto-detect common kart prefabs
                    m_KartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Karting/Prefabs/KartClassic/KartClassic_Player_notML.prefab");
                    if (m_KartPrefab == null)
                    {
                        EditorUtility.DisplayDialog("No Prefab", "Assign a Kart Prefab first.", "OK");
                        return;
                    }
                }
                m_PlacingActive = !m_PlacingActive;
                if (!m_PlacingActive) DestroyPreview();
            }
            GUI.color = Color.white;

            if (m_PlacingActive)
            {
                GUILayout.Space(4);
                EditorGUILayout.HelpBox("Press ESC to cancel.", MessageType.None);
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!m_PlacingActive || m_KartPrefab == null)
            {
                DestroyPreview();
                return;
            }

            Event e = Event.current;

            // Cancel on Escape
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                m_PlacingActive = false;
                DestroyPreview();
                Repaint();
                e.Use();
                return;
            }

            // Suppress default scene view selection while placing
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Raycast mouse against scene geometry
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            m_HasPreview = Physics.Raycast(ray, out RaycastHit hit, 1000f);

            if (m_HasPreview)
            {
                m_PreviewPos = hit.point + Vector3.up * m_HeightOffset;

                if (m_AlignToNormal)
                    m_PreviewRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                else
                    m_PreviewRot = Quaternion.identity;

                // Draw ghost preview handle
                Handles.color = m_PreviewColor;
                Handles.SphereHandleCap(0, m_PreviewPos, Quaternion.identity, 0.8f, EventType.Repaint);
                Handles.DrawDottedLine(hit.point, m_PreviewPos, 4f);
                Handles.Label(m_PreviewPos + Vector3.up * 1.2f,
                    $"Place {m_KartPrefab.name}\n({m_PreviewPos.x:F1}, {m_PreviewPos.y:F1}, {m_PreviewPos.z:F1})");

                // Update ghost mesh preview
                UpdatePreviewInstance();

                sceneView.Repaint();
            }

            // Left click = place
            if (e.type == EventType.MouseDown && e.button == 0 && m_HasPreview)
            {
                PlaceKart();
                e.Use();
            }

            // Right click = cancel
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                m_PlacingActive = false;
                DestroyPreview();
                Repaint();
                e.Use();
            }
        }

        private void UpdatePreviewInstance()
        {
            if (m_PreviewInstance == null)
            {
                m_PreviewInstance = (GameObject)PrefabUtility.InstantiatePrefab(m_KartPrefab);
                m_PreviewInstance.name = "__KartPlacerPreview__";
                m_PreviewInstance.hideFlags = HideFlags.HideAndDontSave;

                // Disable all colliders and scripts so preview is inert
                foreach (var col in m_PreviewInstance.GetComponentsInChildren<Collider>())
                    col.enabled = false;
                foreach (var rb in m_PreviewInstance.GetComponentsInChildren<Rigidbody>())
                {
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                }
                foreach (var mono in m_PreviewInstance.GetComponentsInChildren<MonoBehaviour>())
                    mono.enabled = false;
            }

            m_PreviewInstance.transform.position = m_PreviewPos;
            m_PreviewInstance.transform.rotation = m_PreviewRot;
        }

        private void PlaceKart()
        {
            DestroyPreview();

            var placed = (GameObject)PrefabUtility.InstantiatePrefab(m_KartPrefab);
            placed.transform.position = m_PreviewPos;
            placed.transform.rotation = m_PreviewRot;
            placed.name = m_KartPrefab.name;

            Undo.RegisterCreatedObjectUndo(placed, "Place Kart");
            Selection.activeGameObject = placed;

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(placed.scene);

            Debug.Log($"[KartPlacerTool] Placed '{placed.name}' at {m_PreviewPos}");
        }

        private void DestroyPreview()
        {
            if (m_PreviewInstance != null)
            {
                DestroyImmediate(m_PreviewInstance);
                m_PreviewInstance = null;
            }
        }
    }
}
#endif
