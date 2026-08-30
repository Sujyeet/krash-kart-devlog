using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace KartGame.Track
{
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    public class SplineRoadGenerator : MonoBehaviour
    {
        [Header("Road Dimensions")]
        [Range(4f, 30f)]
        public float roadWidth = 14f;

        [Range(50, 1000)]
        public int resolution = 400;

        [Header("Curbs & Borders")]
        public bool generateCurbs = true;
        [Range(0.2f, 2f)]
        public float curbWidth = 0.8f;
        [Range(0.05f, 0.8f)]
        public float curbHeight = 0.25f;
        [Range(0.1f, 2f)]
        public float skirtDepth = 0.5f;

        [Header("Texturing")]
        public float uvTileLength = 8f;
        public Material roadMaterial;

        private SplineContainer m_Container;
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;
        private MeshCollider m_MeshCollider;

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
            if (totalLength <= 0.01f) return;

            // Ensure required components
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

            if (roadMaterial != null && m_MeshRenderer.sharedMaterial != roadMaterial)
            {
                m_MeshRenderer.sharedMaterial = roadMaterial;
            }

            if (m_MeshCollider == null)
            {
                m_MeshCollider = GetComponent<MeshCollider>();
                if (m_MeshCollider == null) m_MeshCollider = gameObject.AddComponent<MeshCollider>();
            }

            int sampleCount = Mathf.Max(30, resolution);
            int crossSectionVerts = generateCurbs ? 8 : 4; // 8 vertices per ring with curbs + skirts

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float halfRoad = roadWidth * 0.5f;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                if (i == sampleCount && spline.Closed)
                {
                    t = 0f; // Seamless loop closure
                }

                SplineUtility.Evaluate(spline, t, out float3 p, out float3 tan, out float3 upVec);

                Vector3 center = (Vector3)p;
                Vector3 forward = ((Vector3)tan).normalized;
                Vector3 up = ((Vector3)upVec).normalized;
                Vector3 right = Vector3.Cross(up, forward).normalized;

                float vCoord = (t * totalLength) / uvTileLength;
                if (i == sampleCount && spline.Closed)
                {
                    vCoord = totalLength / uvTileLength;
                }

                if (generateCurbs)
                {
                    // Ring Cross-Section Profile:
                    // 0: Left Skirt Bottom
                    // 1: Left Curb Outer Top
                    // 2: Left Curb Inner Top
                    // 3: Left Road Edge (Flat)
                    // 4: Right Road Edge (Flat)
                    // 5: Right Curb Inner Top
                    // 6: Right Curb Outer Top
                    // 7: Right Skirt Bottom

                    float outerLeft = -(halfRoad + curbWidth);
                    float innerLeft = -halfRoad;
                    float innerRight = halfRoad;
                    float outerRight = halfRoad + curbWidth;

                    Vector3 v0 = center + right * outerLeft - up * skirtDepth;
                    Vector3 v1 = center + right * outerLeft + up * curbHeight;
                    Vector3 v2 = center + right * innerLeft + up * curbHeight;
                    Vector3 v3 = center + right * innerLeft;
                    Vector3 v4 = center + right * innerRight;
                    Vector3 v5 = center + right * innerRight + up * curbHeight;
                    Vector3 v6 = center + right * outerRight + up * curbHeight;
                    Vector3 v7 = center + right * outerRight - up * skirtDepth;

                    vertices.Add(v0);
                    vertices.Add(v1);
                    vertices.Add(v2);
                    vertices.Add(v3);
                    vertices.Add(v4);
                    vertices.Add(v5);
                    vertices.Add(v6);
                    vertices.Add(v7);

                    uvs.Add(new Vector2(0f, vCoord));
                    uvs.Add(new Vector2(0.05f, vCoord));
                    uvs.Add(new Vector2(0.1f, vCoord));
                    uvs.Add(new Vector2(0.15f, vCoord));
                    uvs.Add(new Vector2(0.85f, vCoord));
                    uvs.Add(new Vector2(0.9f, vCoord));
                    uvs.Add(new Vector2(0.95f, vCoord));
                    uvs.Add(new Vector2(1f, vCoord));
                }
                else
                {
                    // Simple Flat Road with Skirts
                    Vector3 v0 = center - right * halfRoad - up * skirtDepth;
                    Vector3 v1 = center - right * halfRoad;
                    Vector3 v2 = center + right * halfRoad;
                    Vector3 v3 = center + right * halfRoad - up * skirtDepth;

                    vertices.Add(v0);
                    vertices.Add(v1);
                    vertices.Add(v2);
                    vertices.Add(v3);

                    uvs.Add(new Vector2(0f, vCoord));
                    uvs.Add(new Vector2(0.05f, vCoord));
                    uvs.Add(new Vector2(0.95f, vCoord));
                    uvs.Add(new Vector2(1f, vCoord));
                }
            }

            // Build Quad Triangles across rings
            for (int i = 0; i < sampleCount; i++)
            {
                int ringStartA = i * crossSectionVerts;
                int ringStartB = (i + 1) * crossSectionVerts;

                for (int seg = 0; seg < crossSectionVerts - 1; seg++)
                {
                    int a0 = ringStartA + seg;
                    int a1 = ringStartA + seg + 1;
                    int b0 = ringStartB + seg;
                    int b1 = ringStartB + seg + 1;

                    // Quad: a0 -> a1 -> b1, a0 -> b1 -> b0
                    triangles.Add(a0);
                    triangles.Add(a1);
                    triangles.Add(b1);

                    triangles.Add(a0);
                    triangles.Add(b1);
                    triangles.Add(b0);
                }
            }

            Mesh roadMesh = new Mesh();
            roadMesh.name = "Generated_Spline_Road";
            roadMesh.SetVertices(vertices);
            roadMesh.SetUVs(0, uvs);
            roadMesh.SetTriangles(triangles, 0);
            roadMesh.RecalculateNormals();
            roadMesh.RecalculateTangents();
            roadMesh.RecalculateBounds();

            m_MeshFilter.sharedMesh = roadMesh;
            if (m_MeshCollider != null)
            {
                m_MeshCollider.sharedMesh = null;
                m_MeshCollider.sharedMesh = roadMesh;
            }
        }
    }
}
