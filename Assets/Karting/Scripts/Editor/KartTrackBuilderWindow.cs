#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using KartGame.Track;

namespace KartGame.Editor
{
    /// <summary>
    /// Interactive Editor Window to design, modify, and build clean spline tracks with 1-click tools
    /// and a full Procedural RNG Track Generator.
    /// Accessible via menu: Krash Kart > Track Builder Window
    /// </summary>
    public class KartTrackBuilderWindow : EditorWindow
    {
        private SplineContainer targetSplineContainer;
        private SplineRoadGenerator targetRoadGenerator;

        private Vector2 scrollPos;
        private int autoCheckpointCount = 3;
        private float flattenHeight = 0.5f;

        // RNG Track Generator Settings
        private ProceduralRngTrackGenerator.TrackGenSettings rngSettings = new ProceduralRngTrackGenerator.TrackGenSettings();
        private bool showRngSection = true;
        private bool showManualSection = true;
        private bool showStyleSection = true;

        [MenuItem("Krash Kart/Track Builder Window", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<KartTrackBuilderWindow>("Track Builder");
            window.minSize = new Vector2(400, 620);
            window.Show();
        }

        private void OnEnable()
        {
            FindActiveTrackObjects();
        }

        private void OnFocus()
        {
            FindActiveTrackObjects();
        }

        private void FindActiveTrackObjects()
        {
            if (targetSplineContainer == null)
            {
                GameObject splineObj = GameObject.Find("Spline");
                if (splineObj != null)
                {
                    targetSplineContainer = splineObj.GetComponent<SplineContainer>();
                    targetRoadGenerator = splineObj.GetComponent<SplineRoadGenerator>();
                }
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🏎️ Krash Kart — Track Builder & RNG Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generate infinite procedural RNG tracks (curves, hills, ramps, overpasses) with pinch-free mesh extrusion.", MessageType.Info);
            EditorGUILayout.Space(6);

            // 1. TARGET SELECTION
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Track Target", EditorStyles.boldLabel);
            targetSplineContainer = (SplineContainer)EditorGUILayout.ObjectField("Spline Container", targetSplineContainer, typeof(SplineContainer), true);

            if (targetSplineContainer != null)
            {
                if (targetRoadGenerator == null)
                {
                    targetRoadGenerator = targetSplineContainer.GetComponent<SplineRoadGenerator>();
                }
                targetRoadGenerator = (SplineRoadGenerator)EditorGUILayout.ObjectField("Road Generator", targetRoadGenerator, typeof(SplineRoadGenerator), true);
            }
            else
            {
                if (GUILayout.Button("Create / Find 'Spline' Track Object", GUILayout.Height(28)))
                {
                    ArcadeKartTrackBuilder.BuildCircuit();
                    FindActiveTrackObjects();
                }
            }
            EditorGUILayout.EndVertical();

            if (targetSplineContainer == null || targetSplineContainer.Spline == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            Spline spline = targetSplineContainer.Spline;
            float totalLength = spline.GetLength();

            // 2. LIVE TRACK METRICS
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📏 Current Track: {totalLength:F1}m  |  Knots: {spline.Count}  |  Loop: {spline.Closed}", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndVertical();

            // 3. PROCEDURAL RNG TRACK GENERATOR SECTION
            EditorGUILayout.Space(6);
            showRngSection = EditorGUILayout.BeginFoldoutHeaderGroup(showRngSection, "🎲 Procedural RNG Track Generator");
            if (showRngSection)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Seed Control
                EditorGUILayout.BeginHorizontal();
                rngSettings.seed = EditorGUILayout.IntField("RNG Seed", rngSettings.seed);
                if (GUILayout.Button("🎲 Randomize", GUILayout.Width(100)))
                {
                    rngSettings.seed = UnityEngine.Random.Range(1000, 999999);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Dimensions & Layout", EditorStyles.boldLabel);
                rngSettings.targetLength = EditorGUILayout.Slider("Track Length (m)", rngSettings.targetLength, 250f, 2500f);
                rngSettings.roadWidth = EditorGUILayout.Slider("Road Width (m)", rngSettings.roadWidth, 6f, 28f);
                rngSettings.curveCount = EditorGUILayout.IntSlider("Number of Curves / Knots", rngSettings.curveCount, 6, 30);
                rngSettings.curvatureIntensity = EditorGUILayout.Slider("Curvature / Sharpness", rngSettings.curvatureIntensity, 0.1f, 1.0f);
                rngSettings.trackIrregularity = EditorGUILayout.Slider("Track Irregularity / Variety", rngSettings.trackIrregularity, 0.1f, 0.9f);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Elevation & 3D Topography", EditorStyles.boldLabel);
                rngSettings.elevationMode = (ProceduralRngTrackGenerator.ElevationMode)EditorGUILayout.EnumPopup("Elevation Mode", rngSettings.elevationMode);
                if (rngSettings.elevationMode != ProceduralRngTrackGenerator.ElevationMode.Flat)
                {
                    EditorGUI.indentLevel++;
                    rngSettings.hillAmplitude = EditorGUILayout.Slider("Hill Amplitude (m)", rngSettings.hillAmplitude, 2f, 35f);
                    rngSettings.hillFrequency = EditorGUILayout.IntSlider("Hill Frequency", rngSettings.hillFrequency, 1, 6);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Ramps, Swirls & Overpasses", EditorStyles.boldLabel);
                rngSettings.rampCount = EditorGUILayout.IntSlider("Jump Ramps", rngSettings.rampCount, 0, 4);
                if (rngSettings.rampCount > 0)
                {
                    EditorGUI.indentLevel++;
                    rngSettings.rampHeight = EditorGUILayout.Slider("Ramp Kick Height (m)", rngSettings.rampHeight, 2f, 10f);
                    EditorGUI.indentLevel--;
                }

                rngSettings.overpassCount = EditorGUILayout.IntSlider("3D Overpass Spirals", rngSettings.overpassCount, 0, 2);
                rngSettings.autoBanking = EditorGUILayout.Toggle("Auto-Bank Corners", rngSettings.autoBanking);
                if (rngSettings.autoBanking)
                {
                    EditorGUI.indentLevel++;
                    rngSettings.maxBankAngle = EditorGUILayout.Slider("Max Bank Angle", rngSettings.maxBankAngle, 4f, 25f);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(8);
                GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
                if (GUILayout.Button("⚡ Generate Procedural RNG Track Now", GUILayout.Height(36)))
                {
                    GenerateRngTrack();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 4. MANUAL PRESETS & UTILITIES
            EditorGUILayout.Space(6);
            showManualSection = EditorGUILayout.BeginFoldoutHeaderGroup(showManualSection, "🛠️ Presets & Track Utilities");
            if (showManualSection)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Circuit Presets", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Grand Prix (10 Knots)")) ArcadeKartTrackBuilder.BuildCircuit();
                if (GUILayout.Button("Speedway Oval")) BuildOvalPreset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Figure-8 Overpass")) BuildFigureEightPreset();
                if (GUILayout.Button("Smooth Circle")) BuildCirclePreset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Race Helpers", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                autoCheckpointCount = EditorGUILayout.IntSlider("Checkpoints", autoCheckpointCount, 2, 8);
                if (GUILayout.Button("Distribute", GUILayout.Width(110)))
                {
                    DistributeCheckpointsAlongSpline(autoCheckpointCount);
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Snap Kart to Start Line", GUILayout.Height(24)))
                {
                    SnapKartToStart();
                }

                if (GUILayout.Button("Smooth All Knot Tangents (Pinch Fix)", GUILayout.Height(24)))
                {
                    SmoothSplineTangents();
                }

                EditorGUILayout.BeginHorizontal();
                flattenHeight = EditorGUILayout.FloatField("Flatten Y Height", flattenHeight);
                if (GUILayout.Button("Flatten Track", GUILayout.Width(110)))
                {
                    FlattenTrackElevation(flattenHeight);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 5. ROAD DIMENSIONS & VISUALS
            if (targetRoadGenerator != null)
            {
                EditorGUILayout.Space(6);
                showStyleSection = EditorGUILayout.BeginFoldoutHeaderGroup(showStyleSection, "🎨 Road Dimensions & Materials");
                if (showStyleSection)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUI.BeginChangeCheck();

                    targetRoadGenerator.roadWidth = EditorGUILayout.Slider("Road Width (m)", targetRoadGenerator.roadWidth, 4f, 35f);
                    targetRoadGenerator.resolution = EditorGUILayout.IntSlider("Resolution", targetRoadGenerator.resolution, 50, 1500);
                    targetRoadGenerator.generateCurbs = EditorGUILayout.Toggle("Generate Curbs", targetRoadGenerator.generateCurbs);

                    if (targetRoadGenerator.generateCurbs)
                    {
                        EditorGUI.indentLevel++;
                        targetRoadGenerator.curbWidth = EditorGUILayout.Slider("Curb Width", targetRoadGenerator.curbWidth, 0.2f, 3f);
                        targetRoadGenerator.curbHeight = EditorGUILayout.Slider("Curb Height", targetRoadGenerator.curbHeight, 0.05f, 1f);
                        targetRoadGenerator.skirtDepth = EditorGUILayout.Slider("Skirt Depth", targetRoadGenerator.skirtDepth, 0.1f, 4f);
                        EditorGUI.indentLevel--;
                    }

                    targetRoadGenerator.uvTileLength = EditorGUILayout.Slider("UV Tile Length (m)", targetRoadGenerator.uvTileLength, 2f, 30f);

                    EditorGUILayout.Space(4);
                    targetRoadGenerator.roadMaterial = (Material)EditorGUILayout.ObjectField("Road Material", targetRoadGenerator.roadMaterial, typeof(Material), false);
                    targetRoadGenerator.curbMaterial = (Material)EditorGUILayout.ObjectField("Curb Material", targetRoadGenerator.curbMaterial, typeof(Material), false);
                    targetRoadGenerator.skirtMaterial = (Material)EditorGUILayout.ObjectField("Skirt Material", targetRoadGenerator.skirtMaterial, typeof(Material), false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(targetRoadGenerator);
                        RebuildMesh();
                    }

                    if (GUILayout.Button("Rebuild Road Mesh Now", GUILayout.Height(28)))
                    {
                        RebuildMesh();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.EndScrollView();
        }

        private void GenerateRngTrack()
        {
            Undo.RecordObject(targetSplineContainer, "Generate RNG Track");

            // Apply road width to generator
            if (targetRoadGenerator != null)
            {
                targetRoadGenerator.roadWidth = rngSettings.roadWidth;
                EditorUtility.SetDirty(targetRoadGenerator);
            }

            // Generate spline knots
            ProceduralRngTrackGenerator.GenerateTrack(targetSplineContainer, rngSettings);

            // Rebuild mesh with pinch-free RMF frames
            RebuildMesh();

            // Auto-reposition checkpoints and kart
            DistributeCheckpointsAlongSpline(autoCheckpointCount);
            SnapKartToStart();

            EditorUtility.SetDirty(targetSplineContainer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetSplineContainer.gameObject.scene);

            Debug.Log($"[KartTrackBuilderWindow] Successfully generated procedural RNG Track (Seed: {rngSettings.seed}, Length: {targetSplineContainer.Spline.GetLength():F1}m, Knots: {targetSplineContainer.Spline.Count})!");
        }

        private void RebuildMesh()
        {
            if (targetRoadGenerator != null)
            {
                targetRoadGenerator.GenerateRoadMesh();
                EditorUtility.SetDirty(targetRoadGenerator);
            }
        }

        private void BuildOvalPreset()
        {
            Spline spline = targetSplineContainer.Spline;
            Undo.RecordObject(targetSplineContainer, "Build Oval Preset");
            spline.Clear();

            spline.Add(new BezierKnot(new float3(0f, 0.5f, 0f), new float3(0f, 0f, -30f), new float3(0f, 0f, 30f)));
            spline.Add(new BezierKnot(new float3(0f, 1.5f, 120f), new float3(0f, 0f, -30f), new float3(30f, 0.5f, 25f)));
            spline.Add(new BezierKnot(new float3(80f, 4.0f, 140f), new float3(-30f, 0f, 10f), new float3(30f, 0f, -10f), quaternion.Euler(0, 0, math.radians(12f))));
            spline.Add(new BezierKnot(new float3(120f, 1.5f, 60f), new float3(0f, 0f, 30f), new float3(0f, 0f, -30f)));
            spline.Add(new BezierKnot(new float3(120f, 0.5f, -60f), new float3(0f, 0f, 30f), new float3(-30f, -0.5f, -25f)));
            spline.Add(new BezierKnot(new float3(40f, 3.0f, -80f), new float3(30f, 0f, -10f), new float3(-30f, 0f, 10f), quaternion.Euler(0, 0, math.radians(-12f))));

            spline.Closed = true;
            RebuildMesh();
            DistributeCheckpointsAlongSpline(autoCheckpointCount);
            SnapKartToStart();
        }

        private void BuildFigureEightPreset()
        {
            Spline spline = targetSplineContainer.Spline;
            Undo.RecordObject(targetSplineContainer, "Build Figure-8 Preset");
            spline.Clear();

            spline.Add(new BezierKnot(new float3(0f, 0.5f, 0f), new float3(-20f, 0f, -20f), new float3(20f, 0f, 20f)));
            spline.Add(new BezierKnot(new float3(60f, 1.5f, 70f), new float3(-25f, 0f, -10f), new float3(25f, 0f, 10f)));
            spline.Add(new BezierKnot(new float3(100f, 2f, 0f), new float3(20f, 0f, 20f), new float3(-20f, 0f, -20f)));
            spline.Add(new BezierKnot(new float3(0f, 8f, -10f), new float3(20f, 0.5f, 15f), new float3(-20f, -0.5f, -15f))); // 8m overpass
            spline.Add(new BezierKnot(new float3(-60f, 2f, -70f), new float3(25f, 0f, 10f), new float3(-25f, 0f, -10f)));
            spline.Add(new BezierKnot(new float3(-100f, 1f, 0f), new float3(-20f, 0f, -20f), new float3(20f, 0f, 20f)));

            spline.Closed = true;
            RebuildMesh();
            DistributeCheckpointsAlongSpline(autoCheckpointCount);
            SnapKartToStart();
        }

        private void BuildCirclePreset()
        {
            Spline spline = targetSplineContainer.Spline;
            Undo.RecordObject(targetSplineContainer, "Build Circle Preset");
            spline.Clear();

            int count = 8;
            float radius = 70f;
            for (int i = 0; i < count; i++)
            {
                float angle = (float)i / count * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;

                Vector3 tangent = new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle)) * (radius * 0.35f);
                spline.Add(new BezierKnot(new float3(x, 0.5f, z), (float3)(-tangent), (float3)tangent));
            }

            spline.Closed = true;
            RebuildMesh();
            DistributeCheckpointsAlongSpline(autoCheckpointCount);
            SnapKartToStart();
        }

        private void DistributeCheckpointsAlongSpline(int count)
        {
            Spline spline = targetSplineContainer.Spline;
            if (spline == null || spline.Count < 2) return;

            string[] cpNames = new string[] { "Checkpoint", "Checkpoint (1)", "Checkpoint (2)", "Checkpoint (3)", "Checkpoint (4)", "Checkpoint (5)", "Checkpoint (6)", "Checkpoint (7)" };

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                SplineUtility.Evaluate(spline, t, out float3 p, out float3 tan, out float3 upVec);

                Vector3 pos = (Vector3)p + Vector3.up * 1.5f;
                Vector3 forward = ((Vector3)tan).normalized;
                Quaternion rot = forward != Vector3.zero ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;

                string targetName = i < cpNames.Length ? cpNames[i] : $"Checkpoint ({i})";
                GameObject cp = GameObject.Find(targetName);
                if (cp != null)
                {
                    Undo.RecordObject(cp.transform, $"Distribute Checkpoint {i}");
                    cp.transform.position = pos;
                    cp.transform.rotation = rot;
                    EditorUtility.SetDirty(cp);
                }
            }

            Debug.Log($"[KartTrackBuilderWindow] Auto-distributed {count} checkpoints evenly along spline circuit!");
        }

        private void SnapKartToStart()
        {
            Spline spline = targetSplineContainer.Spline;
            if (spline == null || spline.Count < 1) return;

            SplineUtility.Evaluate(spline, 0f, out float3 p, out float3 tan, out float3 upVec);
            Vector3 startPos = (Vector3)p + Vector3.up * 1.2f + ((Vector3)tan).normalized * 5f;
            Quaternion startRot = Quaternion.LookRotation(((Vector3)tan).normalized, Vector3.up);

            GameObject kart = GameObject.Find("KartClassic_MLAgent");
            if (kart == null) kart = GameObject.Find("KartClassic_Player");

            if (kart != null)
            {
                Undo.RecordObject(kart.transform, "Snap Kart to Start Line");
                kart.transform.position = startPos;
                kart.transform.rotation = startRot;
                EditorUtility.SetDirty(kart);
                Debug.Log($"[KartTrackBuilderWindow] Snapped kart '{kart.name}' to start line at {startPos}.");
            }
        }

        private void SmoothSplineTangents()
        {
            Spline spline = targetSplineContainer.Spline;
            Undo.RecordObject(targetSplineContainer, "Smooth Spline Tangents");

            for (int i = 0; i < spline.Count; i++)
            {
                BezierKnot prev = spline[(i - 1 + spline.Count) % spline.Count];
                BezierKnot curr = spline[i];
                BezierKnot next = spline[(i + 1) % spline.Count];

                float3 dir = math.normalize(next.Position - prev.Position);
                float distPrev = math.distance(curr.Position, prev.Position);
                float distNext = math.distance(curr.Position, next.Position);

                float3 tanOut = dir * (distNext * 0.33f);
                float3 tanIn = -dir * (distPrev * 0.33f);

                curr.TangentIn = tanIn;
                curr.TangentOut = tanOut;
                spline[i] = curr;
            }

            RebuildMesh();
            Debug.Log("[KartTrackBuilderWindow] Smoothed all knot tangents across the circuit!");
        }

        private void FlattenTrackElevation(float height)
        {
            Spline spline = targetSplineContainer.Spline;
            Undo.RecordObject(targetSplineContainer, "Flatten Track Elevation");

            for (int i = 0; i < spline.Count; i++)
            {
                BezierKnot k = spline[i];
                float3 pos = k.Position;
                pos.y = height;
                k.Position = pos;

                float3 tin = k.TangentIn;
                tin.y = 0f;
                k.TangentIn = tin;

                float3 tout = k.TangentOut;
                tout.y = 0f;
                k.TangentOut = tout;

                k.Rotation = quaternion.identity;
                spline[i] = k;
            }

            RebuildMesh();
            Debug.Log($"[KartTrackBuilderWindow] Flattened entire track to Y = {height:F1}m.");
        }
    }
}
#endif
