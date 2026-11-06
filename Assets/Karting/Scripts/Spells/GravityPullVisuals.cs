using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Procedural visual generator for the Gravity Pull spell.
    /// Creates clean, high-visibility contracting neon gravity rings, converging inward vector rays,
    /// and a compact energetic singularity pulse ring without any large solid occluding quads or external assets.
    /// Fully compatible with Unity Universal Render Pipeline (URP).
    /// </summary>
    public static class GravityPullVisuals
    {
        private static Shader additiveShader;

        private static Shader GetAdditiveShader()
        {
            if (additiveShader == null)
            {
                additiveShader = Shader.Find("KartGame/GravityPullAdditive")
                    ?? Shader.Find("KartGame/BlinkWarp")
                    ?? Shader.Find("Sprites/Default");
            }
            return additiveShader;
        }

        private static Material CreateAdditiveMaterial(Color color)
        {
            Material mat = new Material(GetAdditiveShader())
            {
                hideFlags = HideFlags.DontSave
            };
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            return mat;
        }

        /// <summary>
        /// Spawns and manages the complete Gravity Pull visual sequence on the caster kart.
        /// </summary>
        public static Coroutine StartVisuals(MonoBehaviour runner, Transform casterTransform, float maxRadius, float duration)
        {
            if (runner == null || casterTransform == null) return null;
            return runner.StartCoroutine(AnimateGravityPull(casterTransform, maxRadius, duration));
        }

        private static IEnumerator AnimateGravityPull(Transform caster, float maxRadius, float duration)
        {
            GameObject vfxRoot = new GameObject("GravityPull_VFX");
            vfxRoot.transform.SetParent(caster, false);
            vfxRoot.transform.localPosition = new Vector3(0f, 0.08f, 0f); // Just above track surface
            vfxRoot.transform.localRotation = Quaternion.identity;

            List<Material> createdMaterials = new List<Material>();
            List<GameObject> spawnedObjects = new List<GameObject>();

            // Shared neon materials: Bright cyan for outer field, electric violet for core
            Color outerColor = new Color(0.0f, 0.9f, 1.0f, 0.85f);
            Color innerColor = new Color(0.65f, 0.2f, 1.0f, 0.95f);
            Color rayColor = new Color(0.1f, 0.8f, 1.0f, 0.65f);

            Material ringMaterial = CreateAdditiveMaterial(outerColor);
            Material coreMaterial = CreateAdditiveMaterial(innerColor);
            Material rayMaterial = CreateAdditiveMaterial(rayColor);

            createdMaterials.Add(ringMaterial);
            createdMaterials.Add(coreMaterial);
            createdMaterials.Add(rayMaterial);

            // 1. Concentric contracting rings (4 rings cycling inward)
            const int ringCount = 4;
            const int ringSegments = 48;
            LineRenderer[] rings = new LineRenderer[ringCount];
            float[] ringOffsets = new float[ringCount];

            for (int r = 0; r < ringCount; r++)
            {
                GameObject ringObj = new GameObject($"GravityRing_{r}");
                ringObj.transform.SetParent(vfxRoot.transform, false);
                ringObj.transform.localPosition = Vector3.zero;

                LineRenderer lr = ringObj.AddComponent<LineRenderer>();
                lr.material = ringMaterial;
                lr.positionCount = ringSegments + 1;
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.startWidth = 0.14f;
                lr.endWidth = 0.14f;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;

                // Base unit circle coordinates on XZ plane
                for (int s = 0; s <= ringSegments; s++)
                {
                    float angle = ((float)s / ringSegments) * Mathf.PI * 2f;
                    lr.SetPosition(s, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
                }

                rings[r] = lr;
                ringOffsets[r] = (float)r / ringCount; // Staggered phase
                spawnedObjects.Add(ringObj);
            }

            // 2. Converging inward radial stream rays (6 rays pointing inward)
            const int rayCount = 6;
            LineRenderer[] rays = new LineRenderer[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                GameObject rayObj = new GameObject($"GravityRay_{i}");
                rayObj.transform.SetParent(vfxRoot.transform, false);
                rayObj.transform.localPosition = Vector3.zero;

                LineRenderer rlr = rayObj.AddComponent<LineRenderer>();
                rlr.material = rayMaterial;
                rlr.positionCount = 2;
                rlr.useWorldSpace = false;
                rlr.startWidth = 0.18f;
                rlr.endWidth = 0.04f; // Tapers inward towards center
                rlr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rlr.receiveShadows = false;

                rays[i] = rlr;
                spawnedObjects.Add(rayObj);
            }

            // 3. Compact singularity pulse core ring
            GameObject coreObj = new GameObject("GravityCore_Pulse");
            coreObj.transform.SetParent(vfxRoot.transform, false);
            coreObj.transform.localPosition = Vector3.zero;

            LineRenderer coreLr = coreObj.AddComponent<LineRenderer>();
            coreLr.material = coreMaterial;
            coreLr.positionCount = ringSegments + 1;
            coreLr.useWorldSpace = false;
            coreLr.loop = true;
            coreLr.startWidth = 0.22f;
            coreLr.endWidth = 0.22f;
            coreLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coreLr.receiveShadows = false;

            for (int s = 0; s <= ringSegments; s++)
            {
                float angle = ((float)s / ringSegments) * Mathf.PI * 2f;
                coreLr.SetPosition(s, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
            spawnedObjects.Add(coreObj);

            // Animation Loop
            float elapsed = 0f;
            float waveSpeed = 1.35f; // Inward contraction wave frequency

            while (elapsed < duration && caster != null)
            {
                elapsed += Time.deltaTime;
                float overallProgress = Mathf.Clamp01(elapsed / duration);

                // Smooth bell envelope for entrance and exit
                float globalEnvelope = Mathf.Sin(overallProgress * Mathf.PI);

                // Animate concentric contracting rings
                for (int r = 0; r < ringCount; r++)
                {
                    if (rings[r] == null) continue;

                    // Progress cycling from 0 (outer max radius) to 1 (inner core)
                    float cycle = Mathf.Repeat(elapsed * waveSpeed + ringOffsets[r], 1f);

                    // Radius contracts inward with an ease-in curve
                    float currentRadius = Mathf.Lerp(maxRadius, 0.6f, cycle * cycle);
                    rings[r].transform.localScale = Vector3.one * currentRadius;

                    // Smooth alpha fade: transparent at outer spawn, bright at mid, fades at core
                    float alpha = Mathf.Sin(cycle * Mathf.PI) * globalEnvelope;
                    Color dynamicRingColor = Color.Lerp(outerColor, innerColor, cycle);
                    dynamicRingColor.a *= alpha;

                    rings[r].startColor = dynamicRingColor;
                    rings[r].endColor = dynamicRingColor;
                    rings[r].startWidth = Mathf.Lerp(0.12f, 0.24f, cycle) * globalEnvelope;
                    rings[r].endWidth = rings[r].startWidth;
                }

                // Animate converging radial streamer rays
                for (int i = 0; i < rayCount; i++)
                {
                    if (rays[i] == null) continue;

                    float baseAngle = (i * (Mathf.PI * 2f / rayCount)) + (elapsed * 0.4f); // Gentle rotation
                    Vector3 dir = new Vector3(Mathf.Cos(baseAngle), 0f, Mathf.Sin(baseAngle));

                    // Pulses streaming inward along ray
                    float rayPulse = Mathf.Repeat(elapsed * 2.5f + ((float)i / rayCount), 1f);
                    float outerDist = Mathf.Lerp(maxRadius * 0.95f, 2.5f, rayPulse);
                    float innerDist = Mathf.Max(0.5f, outerDist - 4.0f);

                    rays[i].SetPosition(0, dir * outerDist);
                    rays[i].SetPosition(1, dir * innerDist);

                    Color dynamicRayColor = rayColor;
                    dynamicRayColor.a = globalEnvelope * (1f - rayPulse * 0.5f);
                    rays[i].startColor = dynamicRayColor;
                    rays[i].endColor = new Color(innerColor.r, innerColor.g, innerColor.b, dynamicRayColor.a * 0.8f);
                }

                // Animate singularity core pulse
                if (coreLr != null)
                {
                    float coreScale = 1.3f + 0.35f * Mathf.Sin(elapsed * 10f);
                    coreObj.transform.localScale = Vector3.one * coreScale;
                    Color dynamicCore = innerColor;
                    dynamicCore.a *= (0.7f + 0.3f * Mathf.Sin(elapsed * 12f)) * globalEnvelope;
                    coreLr.startColor = dynamicCore;
                    coreLr.endColor = dynamicCore;
                }

                yield return null;
            }

            // Cleanup all procedural resources
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                    Object.Destroy(spawnedObjects[i]);
            }
            spawnedObjects.Clear();

            if (vfxRoot != null)
            {
                Object.Destroy(vfxRoot);
            }

            for (int i = 0; i < createdMaterials.Count; i++)
            {
                if (createdMaterials[i] != null)
                    Object.Destroy(createdMaterials[i]);
            }
            createdMaterials.Clear();
        }
    }
}

