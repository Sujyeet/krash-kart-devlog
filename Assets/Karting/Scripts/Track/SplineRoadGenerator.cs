using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace KartGame.Track
{
    /// <summary>
    /// Generates a clean, modular 3D road mesh with curbs and skirts along a Unity Spline.
    /// Uses Rotation-Minimizing Frames (RMF) to prevent mesh pinching, twists, and self-intersections.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    public class SplineRoadGenerator : MonoBehaviour
    {
        [Header("Road Dimensions")]
        [Tooltip("Width of the drivable road surface in meters.")]
        [Range(4f, 35f)]
        public float roadWidth = 14f;

        [Tooltip("Mesh segment count along the spline path.")]
        [Range(50, 2000)]
        public int resolution = 500;

        [Header("Curbs & Edges")]
        [Tooltip("Generate raised racing curbs along track edges.")]
        public bool generateCurbs = true;

        [Tooltip("Width of each curb border.")]
        [Range(0.2f, 3f)]
        public float curbWidth = 1.0f;

        [Tooltip("Height of the curb step above road level.")]
        [Range(0.05f, 1.0f)]
        public float curbHeight = 0.22f;

        [Tooltip("Depth of ground skirts preventing floating track artifacts.")]
        [Range(0.1f, 5f)]
        public float skirtDepth = 1.0f;

        [Header("Materials (Multi-Submesh)")]
        [Tooltip("Main drivable asphalt road material.")]
        public Material roadMaterial;

        [Tooltip("Racing curb border material (e.g. Red/White curb).")]
        public Material curbMaterial;

        [Tooltip("Outer ground skirt material.")]
        public Material skirtMaterial;

        [Header("Texturing & UV")]
        [Tooltip("Length in meters for one full texture tile repeat along the track.")]
        public float uvTileLength = 8f;

        [Tooltip("Number of lateral UV texture repeats across the road width.")]
        public float uvAcrossRepeat = 1f;

        // Cached components
        private SplineContainer m_Container;
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;
        private MeshCollider m_MeshCollider;

        private struct OrientedPoint
        {
            public Vector3 position;
            public Vector3 forward;
            public Vector3 up;
            public Vector3 right;
            public float vCoord;
        }

        private void OnEnable()
        {
            m_Container = GetComponent<SplineContainer>();
            Spline.Changed += OnSplineChanged;
            GenerateRoadMesh();
        }

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) GenerateRoadMesh();
            };
#endif
        }

        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (m_Container != null && m_Container.Spline == spline)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) GenerateRoadMesh();
                };
#else
                GenerateRoadMesh();
#endif
            }
        }

        [ContextMenu("Rebuild Road Mesh")]
        public void GenerateRoadMesh()
        {
            if (m_Container == null)
                m_Container = GetComponent<SplineContainer>();

            if (m_Container == null || m_Container.Spline == null || m_Container.Spline.Count < 2)
                return;

            Spline spline = m_Container.Spline;
            float totalLength = spline.GetLength();
            if (totalLength <= 0.05f) return;

            // Ensure cached components
            if (m_MeshFilter == null)
            {
                m_MeshFilter = GetComponent<MeshFilter>();
                if (m_MeshFilter == null) m_MeshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (m_MeshRenderer == null)
            {
                m_MeshRenderer = GetComponent<MeshRenderer>();
                if (m_MeshRenderer == null) m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (m_MeshCollider == null)
            {
                m_MeshCollider = GetComponent<MeshCollider>();
                if (m_MeshCollider == null)
                {
                    m_MeshCollider = gameObject.AddComponent<MeshCollider>();
                    m_MeshCollider.convex = false;
                }
            }

            AssignMaterials();

            int sampleCount = Mathf.Max(30, resolution);

            // 1. Build Smooth Rotation-Minimizing Frames (RMF) along the spline
            OrientedPoint[] frames = ComputeRotationMinimizingFrames(spline, sampleCount, totalLength);

            // 2. Construct Quad Strips with Clean Split Normals
            List<Vector3> roadVerts = new List<Vector3>(sampleCount * 2 + 2);
            List<Vector2> roadUVs = new List<Vector2>(sampleCount * 2 + 2);
            List<int> roadTriangles = new List<int>(sampleCount * 6);

            List<Vector3> curbVerts = new List<Vector3>(sampleCount * 4 + 4);
            List<Vector2> curbUVs = new List<Vector2>(sampleCount * 4 + 4);
            List<int> curbTriangles = new List<int>(sampleCount * 12);

            List<Vector3> skirtVerts = new List<Vector3>(sampleCount * 4 + 4);
            List<Vector2> skirtUVs = new List<Vector2>(sampleCount * 4 + 4);
            List<int> skirtTriangles = new List<int>(sampleCount * 12);

            float halfRoad = roadWidth * 0.5f;

            for (int i = 0; i <= sampleCount; i++)
            {
                OrientedPoint pt = frames[i];
                Vector3 center = pt.position;
                Vector3 right = pt.right;
                Vector3 up = pt.up;
                float vCoord = pt.vCoord;

                // --- Drivable Road Surface ---
                Vector3 roadL = center - right * halfRoad;
                Vector3 roadR = center + right * halfRoad;

                roadVerts.Add(roadL);
                roadVerts.Add(roadR);
                roadUVs.Add(new Vector2(0f, vCoord));
                roadUVs.Add(new Vector2(uvAcrossRepeat, vCoord));

                // --- Curbs & Skirts ---
                if (generateCurbs)
                {
                    Vector3 lCurbInner = center - right * halfRoad + up * curbHeight;
                    Vector3 lCurbOuter = center - right * (halfRoad + curbWidth) + up * curbHeight;
                    Vector3 rCurbInner = center + right * halfRoad + up * curbHeight;
                    Vector3 rCurbOuter = center + right * (halfRoad + curbWidth) + up * curbHeight;

                    curbVerts.Add(lCurbOuter);
                    curbVerts.Add(lCurbInner);
                    curbVerts.Add(rCurbInner);
                    curbVerts.Add(rCurbOuter);

                    curbUVs.Add(new Vector2(0f, vCoord));
                    curbUVs.Add(new Vector2(1f, vCoord));
                    curbUVs.Add(new Vector2(0f, vCoord));
                    curbUVs.Add(new Vector2(1f, vCoord));

                    // Skirts (downwards from curb outer edge to prevent gaps in terrain)
                    Vector3 lSkirtBottom = lCurbOuter - up * skirtDepth;
                    Vector3 rSkirtBottom = rCurbOuter - up * skirtDepth;

                    skirtVerts.Add(lSkirtBottom);
                    skirtVerts.Add(lCurbOuter);
                    skirtVerts.Add(rCurbOuter);
                    skirtVerts.Add(rSkirtBottom);

                    skirtUVs.Add(new Vector2(0f, vCoord));
                    skirtUVs.Add(new Vector2(1f, vCoord));
                    skirtUVs.Add(new Vector2(0f, vCoord));
                    skirtUVs.Add(new Vector2(1f, vCoord));
                }
                else
                {
                    Vector3 lSkirt = roadL - up * skirtDepth;
                    Vector3 rSkirt = roadR - up * skirtDepth;

                    skirtVerts.Add(lSkirt);
                    skirtVerts.Add(roadL);
                    skirtVerts.Add(roadR);
                    skirtVerts.Add(rSkirt);

                    skirtUVs.Add(new Vector2(0f, vCoord));
                    skirtUVs.Add(new Vector2(1f, vCoord));
                    skirtUVs.Add(new Vector2(0f, vCoord));
                    skirtUVs.Add(new Vector2(1f, vCoord));
                }
            }

            // 3. Connect Quad Triangles
            for (int i = 0; i < sampleCount; i++)
            {
                // Road Triangles
                int r0 = i * 2;
                int r1 = r0 + 1;
                int r2 = (i + 1) * 2;
                int r3 = r2 + 1;

                roadTriangles.Add(r0);
                roadTriangles.Add(r2);
                roadTriangles.Add(r1);

                roadTriangles.Add(r1);
                roadTriangles.Add(r2);
                roadTriangles.Add(r3);

                if (generateCurbs)
                {
                    // Left Curb Quad
                    int c0 = i * 4;
                    int c1 = c0 + 1;
                    int c2 = (i + 1) * 4;
                    int c3 = c2 + 1;

                    curbTriangles.Add(c0);
                    curbTriangles.Add(c2);
                    curbTriangles.Add(c1);

                    curbTriangles.Add(c1);
                    curbTriangles.Add(c2);
                    curbTriangles.Add(c3);

                    // Right Curb Quad
                    int rc0 = i * 4 + 2;
                    int rc1 = rc0 + 1;
                    int rc2 = (i + 1) * 4 + 2;
                    int rc3 = rc2 + 1;

                    curbTriangles.Add(rc0);
                    curbTriangles.Add(rc2);
                    curbTriangles.Add(rc1);

                    curbTriangles.Add(rc1);
                    curbTriangles.Add(rc2);
                    curbTriangles.Add(rc3);
                }

                // Left Skirt Quad
                int s0 = i * 4;
                int s1 = s0 + 1;
                int s2 = (i + 1) * 4;
                int s3 = s2 + 1;

                skirtTriangles.Add(s0);
                skirtTriangles.Add(s2);
                skirtTriangles.Add(s1);

                skirtTriangles.Add(s1);
                skirtTriangles.Add(s2);
                skirtTriangles.Add(s3);

                // Right Skirt Quad
                int rs0 = i * 4 + 2;
                int rs1 = rs0 + 1;
                int rs2 = (i + 1) * 4 + 2;
                int rs3 = rs2 + 1;

                skirtTriangles.Add(rs0);
                skirtTriangles.Add(rs2);
                skirtTriangles.Add(rs1);

                skirtTriangles.Add(rs1);
                skirtTriangles.Add(rs2);
                skirtTriangles.Add(rs3);
            }

            // 4. Combine into unified Submesh structure
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "CleanSplineRoad_Mesh";

            List<Vector3> allVerts = new List<Vector3>();
            List<Vector2> allUVs = new List<Vector2>();

            allVerts.AddRange(roadVerts);
            allUVs.AddRange(roadUVs);

            int curbOffset = allVerts.Count;
            if (generateCurbs)
            {
                allVerts.AddRange(curbVerts);
                allUVs.AddRange(curbUVs);
                for (int t = 0; t < curbTriangles.Count; t++)
                {
                    curbTriangles[t] += curbOffset;
                }
            }

            int skirtOffset = allVerts.Count;
            allVerts.AddRange(skirtVerts);
            allUVs.AddRange(skirtUVs);
            for (int t = 0; t < skirtTriangles.Count; t++)
            {
                skirtTriangles[t] += skirtOffset;
            }

            combinedMesh.SetVertices(allVerts);
            combinedMesh.SetUVs(0, allUVs);

            bool hasCurbs = generateCurbs && curbMaterial != null;
            bool hasSkirts = skirtMaterial != null;

            if (hasCurbs && hasSkirts)
            {
                combinedMesh.subMeshCount = 3;
                combinedMesh.SetTriangles(roadTriangles, 0);
                combinedMesh.SetTriangles(curbTriangles, 1);
                combinedMesh.SetTriangles(skirtTriangles, 2);
            }
            else if (hasCurbs)
            {
                combinedMesh.subMeshCount = 2;
                combinedMesh.SetTriangles(roadTriangles, 0);
                curbTriangles.AddRange(skirtTriangles);
                combinedMesh.SetTriangles(curbTriangles, 1);
            }
            else if (hasSkirts)
            {
                combinedMesh.subMeshCount = 2;
                combinedMesh.SetTriangles(roadTriangles, 0);
                combinedMesh.SetTriangles(skirtTriangles, 1);
            }
            else
            {
                List<int> unifiedTris = new List<int>(roadTriangles);
                if (generateCurbs) unifiedTris.AddRange(curbTriangles);
                unifiedTris.AddRange(skirtTriangles);

                combinedMesh.subMeshCount = 1;
                combinedMesh.SetTriangles(unifiedTris, 0);
            }

            combinedMesh.RecalculateNormals();
            combinedMesh.RecalculateTangents();
            combinedMesh.RecalculateBounds();

            m_MeshFilter.sharedMesh = combinedMesh;

            if (m_MeshCollider != null)
            {
                m_MeshCollider.sharedMesh = null;
                m_MeshCollider.sharedMesh = combinedMesh;
            }
        }

        /// <summary>
        /// Computes Rotation-Minimizing Frames (RMF) via Double Reflection method.
        /// Prevents sudden 180° frame flips, pinching, and self-intersections.
        /// </summary>
        private OrientedPoint[] ComputeRotationMinimizingFrames(Spline spline, int sampleCount, float totalLength)
        {
            OrientedPoint[] frames = new OrientedPoint[sampleCount + 1];

            // Sample raw positions and tangents
            for (int i = 0; i <= sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                if (i == sampleCount && spline.Closed) t = 0f;

                SplineUtility.Evaluate(spline, t, out float3 p, out float3 tan, out float3 upVec);

                Vector3 pos = (Vector3)p;
                Vector3 forward = ((Vector3)tan).normalized;
                if (forward == Vector3.zero) forward = Vector3.forward;

                float v = (t * totalLength) / uvTileLength;
                if (i == sampleCount && spline.Closed) v = totalLength / uvTileLength;

                frames[i] = new OrientedPoint
                {
                    position = pos,
                    forward = forward,
                    vCoord = v
                };
            }

            // Establish initial reference frame at i = 0
            Vector3 initForward = frames[0].forward;
            Vector3 initUp = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(initForward, initUp)) > 0.95f)
            {
                initUp = Vector3.forward;
            }
            Vector3 initRight = Vector3.Cross(initUp, initForward).normalized;
            initUp = Vector3.Cross(initForward, initRight).normalized;

            frames[0].right = initRight;
            frames[0].up = initUp;

            // Propagate frames using Parallel Transport (Double Reflection RMF)
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 x0 = frames[i].position;
                Vector3 t0 = frames[i].forward;
                Vector3 r0 = frames[i].right;

                Vector3 x1 = frames[i + 1].position;
                Vector3 t1 = frames[i + 1].forward;

                Vector3 v1 = x1 - x0;
                float c1 = Vector3.Dot(v1, v1);

                Vector3 rL = r0;
                Vector3 tL = t0;

                if (c1 > 1e-6f)
                {
                    rL = r0 - (2f / c1) * Vector3.Dot(v1, r0) * v1;
                    tL = t0 - (2f / c1) * Vector3.Dot(v1, t0) * v1;
                }

                Vector3 v2 = t1 - tL;
                float c2 = Vector3.Dot(v2, v2);

                Vector3 r1 = rL;
                if (c2 > 1e-6f)
                {
                    r1 = rL - (2f / c2) * Vector3.Dot(v2, rL) * v2;
                }

                r1 = (r1 - Vector3.Dot(r1, t1) * t1).normalized;
                Vector3 u1 = Vector3.Cross(t1, r1).normalized;

                // Ground-bias upright alignment to keep road horizontal where possible
                if (Mathf.Abs(u1.y) > 0.3f && u1.y < 0f)
                {
                    u1 = -u1;
                    r1 = -r1;
                }

                frames[i + 1].right = r1;
                frames[i + 1].up = u1;
            }

            // For closed loops, distribute any accumulated holonomy twist across all frames
            if (spline.Closed && sampleCount > 1)
            {
                Vector3 endRight = frames[sampleCount].right;
                Vector3 startRight = frames[0].right;
                Vector3 forward0 = frames[0].forward;

                float signedAngle = Vector3.SignedAngle(endRight, startRight, forward0);

                for (int i = 1; i <= sampleCount; i++)
                {
                    float angleFraction = signedAngle * ((float)i / sampleCount);
                    Quaternion correction = Quaternion.AngleAxis(angleFraction, frames[i].forward);
                    frames[i].right = (correction * frames[i].right).normalized;
                    frames[i].up = Vector3.Cross(frames[i].forward, frames[i].right).normalized;
                }
            }

            return frames;
        }

        private void AssignMaterials()
        {
            List<Material> mats = new List<Material>();

            if (roadMaterial != null)
            {
                mats.Add(roadMaterial);
            }
            else
            {
                roadMaterial = m_MeshRenderer.sharedMaterial;
                if (roadMaterial != null) mats.Add(roadMaterial);
            }

            if (generateCurbs && curbMaterial != null)
            {
                mats.Add(curbMaterial);
            }

            if (skirtMaterial != null)
            {
                mats.Add(skirtMaterial);
            }

            if (mats.Count > 0)
            {
                m_MeshRenderer.sharedMaterials = mats.ToArray();
            }
        }
    }
}
