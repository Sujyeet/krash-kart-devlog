#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace KartGame.Editor
{
    public static class ArcadeKartTrackBuilder
    {
        [MenuItem("Krash Kart/Generate Sample Circuit on Track 2", false, 10)]
        public static void BuildCircuit()
        {
            GameObject splineObj = GameObject.Find("Spline");
            if (splineObj == null)
            {
                splineObj = new GameObject("Spline");
                Undo.RegisterCreatedObjectUndo(splineObj, "Create Spline");
            }

            SplineContainer container = splineObj.GetComponent<SplineContainer>();
            if (container == null)
            {
                container = Undo.AddComponent<SplineContainer>(splineObj);
            }

            // Ensure SplineExtrude component exists
            SplineExtrude extrude = splineObj.GetComponent<SplineExtrude>();
            if (extrude == null)
            {
                extrude = Undo.AddComponent<SplineExtrude>(splineObj);
            }

            // Configure SplineExtrude for wide road
            extrude.Radius = 7.0f; // 14 units total road width
            extrude.SegmentsPerUnit = 2; // Smooth curve interpolation

            // Add MeshCollider if missing
            MeshCollider collider = splineObj.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<MeshCollider>(splineObj);
                collider.convex = false; // Static track collider works great non-convex when static
            }

            Spline spline = container.Spline;
            spline.Clear();

            // Set Spline object transform to ground level
            splineObj.transform.position = new Vector3(0f, 0.5f, 0f);
            splineObj.transform.rotation = Quaternion.identity;
            splineObj.transform.localScale = Vector3.one;

            // 10 knots forming a ~600 unit circuit
            var knotDefs = new (Vector3 pos, Vector3 tanIn, Vector3 tanOut, Quaternion rot)[]
            {
                // 1. Start/Finish Line
                (new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -30f), new Vector3(0f, 0f, 30f), Quaternion.identity),
                // 2. High-speed straightaway
                (new Vector3(0f, 0.5f, 100f), new Vector3(0f, 0f, -30f), new Vector3(15f, 1f, 35f), Quaternion.identity),
                // 3. Sweeping entry into banked turn
                (new Vector3(45f, 2f, 150f), new Vector3(-20f, -1f, -25f), new Vector3(35f, 1.5f, 10f), Quaternion.Euler(0, 0, 6f)),
                // 4. Apex of banked right turn (max roll & elevation)
                (new Vector3(115f, 4.5f, 140f), new Vector3(-35f, -1f, 15f), new Vector3(35f, -0.5f, -15f), Quaternion.Euler(0, 0, 12f)),
                // 5. Downhill sweep out of banked turn
                (new Vector3(160f, 2.5f, 85f), new Vector3(-25f, 1f, 30f), new Vector3(15f, -1f, -35f), Quaternion.Euler(0, 0, 6f)),
                // 6. S-Bend left entry
                (new Vector3(175f, 1f, 25f), new Vector3(5f, 0.5f, 35f), new Vector3(-25f, -0.5f, -35f), Quaternion.identity),
                // 7. S-Bend right flick
                (new Vector3(135f, 0.5f, -20f), new Vector3(25f, 0.5f, 30f), new Vector3(-30f, 0f, -30f), Quaternion.identity),
                // 8. Wide hairpin entry
                (new Vector3(65f, 0f, -40f), new Vector3(35f, 0f, 15f), new Vector3(-35f, 0.5f, -15f), Quaternion.identity),
                // 9. Hairpin apex (banked left)
                (new Vector3(-25f, 1.5f, -55f), new Vector3(35f, -0.5f, 15f), new Vector3(-35f, 0.5f, 25f), Quaternion.Euler(0, 0, -10f)),
                // 10. Return straight exit
                (new Vector3(-55f, 0.5f, -10f), new Vector3(10f, 0.5f, -35f), new Vector3(10f, -0.5f, 35f), Quaternion.Euler(0, 0, -4f))
            };

            foreach (var k in knotDefs)
            {
                var knot = new BezierKnot(
                    (float3)k.pos,
                    (float3)k.tanIn,
                    (float3)k.tanOut,
                    (quaternion)k.rot
                );
                spline.Add(knot);
            }

            spline.Closed = true;
            extrude.Rebuild();

            // Snap player kart to Start line
            GameObject kart = GameObject.Find("KartClassic_MLAgent");
            if (kart != null)
            {
                Undo.RecordObject(kart.transform, "Position Kart at Start");
                kart.transform.position = new Vector3(0f, 1.2f, 5f);
                kart.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // Snap Checkpoints to the new track circuit
            RepositionCheckpoints();

            EditorUtility.SetDirty(splineObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(splineObj.scene);

            Debug.Log($"[ArcadeKartTrackBuilder] Generated 10-knot wide arcade circuit ({spline.GetLength():F1}m length) with banked corners and S-bend!");
        }

        private static void RepositionCheckpoints()
        {
            GameObject cp0 = GameObject.Find("Checkpoint");
            if (cp0 != null)
            {
                Undo.RecordObject(cp0.transform, "Move Checkpoint 0");
                cp0.transform.position = new Vector3(0f, 1.5f, 10f); // Start/Finish
                cp0.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            GameObject cp1 = GameObject.Find("Checkpoint (1)");
            if (cp1 != null)
            {
                Undo.RecordObject(cp1.transform, "Move Checkpoint 1");
                cp1.transform.position = new Vector3(120f, 5.0f, 140f); // Banked turn apex
                cp1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            GameObject cp2 = GameObject.Find("Checkpoint (2)");
            if (cp2 != null)
            {
                Undo.RecordObject(cp2.transform, "Move Checkpoint 2");
                cp2.transform.position = new Vector3(-25f, 2.0f, -55f); // Hairpin apex
                cp2.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            }
        }
    }
}
#endif
