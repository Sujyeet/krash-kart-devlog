using System.Collections;
using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Procedural visual generator for the Blink Boost spell.
    /// Spawns departure energy flash, afterimage ghost mesh, procedural warp streak ribbon,
    /// and arrival shockwave ring without requiring any imported 3D models or textures.
    /// </summary>
    public static class BlinkBoostVisuals
    {
        private static Shader warpShader;

        private static Shader GetWarpShader()
        {
            if (warpShader == null)
            {
                warpShader = Shader.Find("KartGame/BlinkWarp") 
                    ?? Shader.Find("KartGame/ShieldBubble") 
                    ?? Shader.Find("Sprites/Default");
            }
            return warpShader;
        }

        private static Material CreateWarpMaterial(Color color, Color secondaryColor, float glow, float alpha)
        {
            Material mat = new Material(GetWarpShader())
            {
                hideFlags = HideFlags.DontSave
            };
            mat.SetColor("_Color", color);
            mat.SetColor("_SecondaryColor", secondaryColor);
            mat.SetFloat("_Glow", glow);
            mat.SetFloat("_Alpha", alpha);
            return mat;
        }

        /// <summary>
        /// Spawns the departure flash at the origin point.
        /// </summary>
        public static void SpawnDepartureFlash(MonoBehaviour runner, Vector3 position)
        {
            GameObject flashObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flashObj.name = "Blink_DepartureFlash";
            flashObj.transform.position = position;
            Object.Destroy(flashObj.GetComponent<Collider>());

            Material mat = CreateWarpMaterial(new Color(0.1f, 0.9f, 1f), new Color(0.9f, 0.3f, 1f), 2.5f, 1f);
            MeshRenderer mr = flashObj.GetComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            runner.StartCoroutine(AnimateFlash(flashObj, mat, 0.2f, 2.2f));
        }

        /// <summary>
        /// Spawns an afterimage ghost of the kart at departure.
        /// </summary>
        public static void SpawnGhostAfterImage(MonoBehaviour runner, ArcadeKart kart, Vector3 position, Quaternion rotation)
        {
            GameObject ghostObj = new GameObject("Blink_AfterImageGhost");
            ghostObj.transform.position = position;
            ghostObj.transform.rotation = rotation;

            Material ghostMat = CreateWarpMaterial(new Color(0.2f, 0.8f, 1f, 0.6f), new Color(0.8f, 0.2f, 1f, 0.6f), 1.8f, 0.6f);

            // Copy meshes from kart renderers if present, otherwise create procedural capsule
            MeshFilter[] kartFilters = kart.GetComponentsInChildren<MeshFilter>();
            bool copiedMesh = false;
            if (kartFilters != null && kartFilters.Length > 0)
            {
                foreach (MeshFilter mf in kartFilters)
                {
                    if (mf.sharedMesh == null) continue;
                    GameObject childGhost = new GameObject("GhostPart");
                    childGhost.transform.SetParent(ghostObj.transform, false);
                    childGhost.transform.position = mf.transform.position;
                    childGhost.transform.rotation = mf.transform.rotation;
                    childGhost.transform.localScale = mf.transform.lossyScale;

                    MeshFilter childMf = childGhost.AddComponent<MeshFilter>();
                    childMf.sharedMesh = mf.sharedMesh;
                    MeshRenderer childMr = childGhost.AddComponent<MeshRenderer>();
                    childMr.material = ghostMat;
                    childMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    childMr.receiveShadows = false;
                    copiedMesh = true;
                }
            }

            if (!copiedMesh)
            {
                GameObject fallbackCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallbackCapsule.transform.SetParent(ghostObj.transform, false);
                fallbackCapsule.transform.localScale = new Vector3(1.4f, 0.8f, 2.2f);
                Object.Destroy(fallbackCapsule.GetComponent<Collider>());
                MeshRenderer mr = fallbackCapsule.GetComponent<MeshRenderer>();
                mr.material = ghostMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            runner.StartCoroutine(AnimateGhost(ghostObj, ghostMat, 0.45f));
        }

        /// <summary>
        /// Spawns a procedural warp streak ribbon connecting the start and end positions.
        /// </summary>
        public static void SpawnWarpStreak(MonoBehaviour runner, Vector3 start, Vector3 end)
        {
            GameObject streakObj = new GameObject("Blink_WarpStreak");
            MeshFilter mf = streakObj.AddComponent<MeshFilter>();
            MeshRenderer mr = streakObj.AddComponent<MeshRenderer>();

            Material streakMat = CreateWarpMaterial(new Color(0.0f, 0.95f, 1f), new Color(0.95f, 0.35f, 1f), 3.0f, 1.0f);
            mr.material = streakMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Mesh ribbonMesh = GenerateStreakRibbon(start, end, 16, 0.75f);
            mf.sharedMesh = ribbonMesh;

            runner.StartCoroutine(AnimateStreak(streakObj, streakMat, 0.35f));
        }

        /// <summary>
        /// Spawns an arrival shockwave ring at destination.
        /// </summary>
        public static void SpawnArrivalShockwave(MonoBehaviour runner, Vector3 position)
        {
            GameObject shockObj = new GameObject("Blink_ArrivalShockwave");
            shockObj.transform.position = position + (Vector3.up * 0.15f);
            shockObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Horizontal plane

            MeshFilter mf = shockObj.AddComponent<MeshFilter>();
            MeshRenderer mr = shockObj.AddComponent<MeshRenderer>();

            Material shockMat = CreateWarpMaterial(new Color(0.2f, 1.0f, 0.9f), new Color(0.9f, 0.2f, 1.0f), 2.5f, 1.0f);
            mr.material = shockMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            mf.sharedMesh = GenerateRingMesh(32, 0.3f);

            runner.StartCoroutine(AnimateShockwave(shockObj, shockMat, 0.4f, 4.5f));
        }

        #region Procedural Mesh Generators

        private static Mesh GenerateStreakRibbon(Vector3 start, Vector3 end, int segments, float width)
        {
            Vector3 diff = end - start;
            float length = diff.magnitude;
            if (length < 0.01f) return new Mesh();

            Vector3 dir = diff.normalized;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.Cross(dir, Vector3.right);
            side.Normalize();

            int vertexCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 center = Vector3.Lerp(start, end, t);
                // Sine taper at endpoints
                float w = width * Mathf.Sin(t * Mathf.PI);

                vertices[i * 2] = center - (side * w);
                vertices[i * 2 + 1] = center + (side * w);
                uvs[i * 2] = new Vector2(0f, t);
                uvs[i * 2 + 1] = new Vector2(1f, t);
            }

            int tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int c = i * 2;
                int n = (i + 1) * 2;

                triangles[tIdx++] = c;
                triangles[tIdx++] = n;
                triangles[tIdx++] = c + 1;

                triangles[tIdx++] = c + 1;
                triangles[tIdx++] = n;
                triangles[tIdx++] = n + 1;
            }

            Mesh mesh = new Mesh
            {
                name = "ProceduralStreakRibbon",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh GenerateRingMesh(int segments, float thickness)
        {
            int vertexCount = segments * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segments * 6];

            float step = (Mathf.PI * 2f) / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * step;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float innerR = 1f - thickness;
                float outerR = 1f;

                vertices[i * 2] = new Vector3(cos * innerR, sin * innerR, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * outerR, sin * outerR, 0f);

                float u = (float)i / segments;
                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
            }

            int tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int c = i * 2;
                int n = ((i + 1) % segments) * 2;

                triangles[tIdx++] = c;
                triangles[tIdx++] = n;
                triangles[tIdx++] = c + 1;

                triangles[tIdx++] = c + 1;
                triangles[tIdx++] = n;
                triangles[tIdx++] = n + 1;
            }

            Mesh mesh = new Mesh
            {
                name = "ProceduralRingMesh",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        #endregion

        #region Animation Coroutines

        private static IEnumerator AnimateFlash(GameObject obj, Material mat, float duration, float maxScale)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.2f;
            Vector3 targetScale = Vector3.one * maxScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (obj != null)
                {
                    obj.transform.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Sin(t * Mathf.PI * 0.5f));
                }

                if (mat != null)
                {
                    mat.SetFloat("_Alpha", 1f - t);
                    mat.SetFloat("_AnimProgress", t);
                }

                yield return null;
            }

            if (obj != null) Object.Destroy(obj);
            if (mat != null) Object.Destroy(mat);
        }

        private static IEnumerator AnimateGhost(GameObject obj, Material mat, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (mat != null)
                {
                    mat.SetFloat("_Alpha", (1f - t) * 0.6f);
                    mat.SetFloat("_AnimProgress", t);
                }

                yield return null;
            }

            if (obj != null) Object.Destroy(obj);
            if (mat != null) Object.Destroy(mat);
        }

        private static IEnumerator AnimateStreak(GameObject obj, Material mat, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (mat != null)
                {
                    mat.SetFloat("_Alpha", 1f - (t * t));
                    mat.SetFloat("_AnimProgress", t);
                }

                yield return null;
            }

            if (obj != null) Object.Destroy(obj);
            if (mat != null) Object.Destroy(mat);
        }

        private static IEnumerator AnimateShockwave(GameObject obj, Material mat, float duration, float maxRadius)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = new Vector3(maxRadius, maxRadius, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                if (obj != null)
                {
                    obj.transform.localScale = Vector3.Lerp(startScale, targetScale, easeOut);
                }

                if (mat != null)
                {
                    mat.SetFloat("_Alpha", 1f - t);
                    mat.SetFloat("_AnimProgress", t);
                }

                yield return null;
            }

            if (obj != null) Object.Destroy(obj);
            if (mat != null) Object.Destroy(mat);
        }

        #endregion
    }
}

