using System.Collections;
using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Procedurally generates a glowing, semi-transparent energy bubble around a kart.
    /// Handles procedural UV sphere mesh construction, Fresnel shading, and break/fade animations.
    /// Requires zero external 3D model assets.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ShieldBubble : MonoBehaviour
    {
        [Header("Procedural Mesh Settings")]
        [SerializeField, Min(8)] private int rings = 20;
        [SerializeField, Min(8)] private int segments = 28;

        [Header("Visual Colors")]
        [SerializeField] private Color baseColor = new Color(0.15f, 0.55f, 1.0f, 0.22f);
        [SerializeField] private Color rimColor = new Color(0.45f, 0.85f, 1.0f, 0.75f);
        [SerializeField, Range(0.5f, 8f)] private float rimPower = 2.8f;
        [SerializeField, Range(0.5f, 3f)] private float glow = 1.4f;

        [Header("Animation Settings")]
        [SerializeField] private float pulseSpeed = 2.5f;
        [SerializeField] private float pulseMagnitude = 0.04f;
        [SerializeField] private float rotationSpeed = 25f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh proceduralMesh;
        private Material bubbleMaterial;
        private Vector3 baseScale = Vector3.one * 2.8f;
        private bool isExiting = false;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            GenerateBubbleMesh();
            SetupMaterial();
        }

        private void Start()
        {
            StartCoroutine(AnimateScaleIn(0.25f));
        }

        private void Update()
        {
            if (isExiting) return;

            // Subtle rotation for dynamic energy field look
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

            // Gentle breathing/pulsation
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
            transform.localScale = baseScale * pulse;
        }

        /// <summary>
        /// Adapts the bubble's base scale to comfortably encase the target kart.
        /// </summary>
        public void Initialize(ArcadeKart kart)
        {
            if (kart == null) return;

            Collider col = kart.GetComponent<Collider>();
            if (col != null)
            {
                float maxDim = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, col.bounds.extents.z);
                float calculatedScale = Mathf.Max(2.4f, maxDim * 2.3f);
                baseScale = new Vector3(calculatedScale, calculatedScale * 0.9f, calculatedScale * 1.15f);
            }
            else
            {
                baseScale = new Vector3(2.8f, 2.5f, 3.2f);
            }

            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Plays an energetic pop/dissolve sequence when absorbing an offensive projectile or mine.
        /// </summary>
        public void Break()
        {
            if (isExiting) return;
            isExiting = true;
            StopAllCoroutines();
            StartCoroutine(AnimateBreakSequence(0.25f));
        }

        /// <summary>
        /// Smoothly fades out when shield duration elapses without being struck.
        /// </summary>
        public void Despawn()
        {
            if (isExiting) return;
            isExiting = true;
            StopAllCoroutines();
            StartCoroutine(AnimateFadeOut(0.4f));
        }

        private IEnumerator AnimateScaleIn(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curve = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic
                transform.localScale = Vector3.Lerp(Vector3.zero, baseScale, curve);
                yield return null;
            }
            transform.localScale = baseScale;
        }

        private IEnumerator AnimateBreakSequence(float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 burstScale = startScale * 1.35f;

            Color initialBase = bubbleMaterial != null ? bubbleMaterial.GetColor("_BaseColor") : baseColor;
            Color initialRim = bubbleMaterial != null ? bubbleMaterial.GetColor("_RimColor") : rimColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Quick burst outward then collapse
                if (t < 0.35f)
                {
                    float burstT = t / 0.35f;
                    transform.localScale = Vector3.Lerp(startScale, burstScale, burstT);
                }
                else
                {
                    float collapseT = (t - 0.35f) / 0.65f;
                    transform.localScale = Vector3.Lerp(burstScale, Vector3.zero, collapseT);
                }

                // Rapid flash and fade
                if (bubbleMaterial != null)
                {
                    float alpha = 1f - t;
                    Color curBase = initialBase;
                    curBase.a *= alpha;
                    Color curRim = initialRim;
                    curRim.a *= alpha;

                    bubbleMaterial.SetColor("_BaseColor", curBase);
                    bubbleMaterial.SetColor("_RimColor", curRim);
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateFadeOut(float duration)
        {
            float elapsed = 0f;
            Color initialBase = bubbleMaterial != null ? bubbleMaterial.GetColor("_BaseColor") : baseColor;
            Color initialRim = bubbleMaterial != null ? bubbleMaterial.GetColor("_RimColor") : rimColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = 1f - t;

                if (bubbleMaterial != null)
                {
                    Color curBase = initialBase;
                    curBase.a *= alpha;
                    Color curRim = initialRim;
                    curRim.a *= alpha;

                    bubbleMaterial.SetColor("_BaseColor", curBase);
                    bubbleMaterial.SetColor("_RimColor", curRim);
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void GenerateBubbleMesh()
        {
            proceduralMesh = new Mesh { name = "ProceduralShieldBubble_Mesh" };

            int vertexCount = (rings + 1) * (segments + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];

            int v = 0;
            for (int ring = 0; ring <= rings; ring++)
            {
                float phi = Mathf.PI * ring / rings;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                for (int seg = 0; seg <= segments; seg++)
                {
                    float theta = 2f * Mathf.PI * seg / segments;
                    float sinTheta = Mathf.Sin(theta);
                    float cosTheta = Mathf.Cos(theta);

                    Vector3 vertex = new Vector3(sinPhi * cosTheta, cosPhi, sinPhi * sinTheta);
                    vertices[v] = vertex;
                    normals[v] = vertex.normalized;
                    uvs[v] = new Vector2((float)seg / segments, (float)ring / rings);
                    v++;
                }
            }

            int[] triangles = new int[rings * segments * 6];
            int t = 0;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int current = ring * (segments + 1) + seg;
                    int next = current + (segments + 1);

                    triangles[t++] = current;
                    triangles[t++] = next;
                    triangles[t++] = current + 1;

                    triangles[t++] = current + 1;
                    triangles[t++] = next;
                    triangles[t++] = next + 1;
                }
            }

            proceduralMesh.vertices = vertices;
            proceduralMesh.normals = normals;
            proceduralMesh.uv = uvs;
            proceduralMesh.triangles = triangles;
            proceduralMesh.RecalculateBounds();

            meshFilter.sharedMesh = proceduralMesh;
        }

        private void SetupMaterial()
        {
            Shader shader = Shader.Find("KartGame/ShieldBubble");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            bubbleMaterial = new Material(shader)
            {
                name = "ShieldBubble_RuntimeMaterial",
                enableInstancing = true
            };

            if (bubbleMaterial.HasProperty("_BaseColor"))
                bubbleMaterial.SetColor("_BaseColor", baseColor);
            if (bubbleMaterial.HasProperty("_Color"))
                bubbleMaterial.SetColor("_Color", baseColor);
            if (bubbleMaterial.HasProperty("_RimColor"))
                bubbleMaterial.SetColor("_RimColor", rimColor);
            if (bubbleMaterial.HasProperty("_RimPower"))
                bubbleMaterial.SetFloat("_RimPower", rimPower);
            if (bubbleMaterial.HasProperty("_Glow"))
                bubbleMaterial.SetFloat("_Glow", glow);

            meshRenderer.sharedMaterial = bubbleMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private void OnDestroy()
        {
            if (proceduralMesh != null)
                Destroy(proceduralMesh);

            if (bubbleMaterial != null)
                Destroy(bubbleMaterial);
        }
    }
}

