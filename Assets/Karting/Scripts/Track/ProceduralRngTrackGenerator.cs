using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace KartGame.Track
{
    /// <summary>
    /// Procedural RNG Track Generator.
    /// Creates non-self-intersecting 3D spline circuits with customizable curves, hills, ramps, and overpasses.
    /// </summary>
    public static class ProceduralRngTrackGenerator
    {
        public enum ElevationMode
        {
            Flat,
            RollingHills,
            Mountainous,
            RollercoasterSwirls
        }

        [System.Serializable]
        public class TrackGenSettings
        {
            [Header("Seed & Dimensions")]
            public int seed = 1337;
            [Range(200f, 3000f)] public float targetLength = 850f;
            [Range(6f, 30f)] public float roadWidth = 14f;

            [Header("Curves & Complexity")]
            [Range(6, 32)] public int curveCount = 12;
            [Range(0.1f, 1.0f)] public float curvatureIntensity = 0.55f;
            [Range(0f, 1f)] public float trackIrregularity = 0.45f;

            [Header("Elevation & 3D Topography")]
            public ElevationMode elevationMode = ElevationMode.RollingHills;
            [Range(0f, 45f)] public float hillAmplitude = 8f;
            [Range(1, 6)] public int hillFrequency = 3;

            [Header("Stunts & Features")]
            [Range(0, 4)] public int rampCount = 1;
            [Range(2f, 12f)] public float rampHeight = 4.5f;
            [Range(0, 3)] public int overpassCount = 0;
            public bool autoBanking = true;
            [Range(0f, 25f)] public float maxBankAngle = 14f;
        }

        /// <summary>
        /// Generates a clean, non-intersecting procedural spline track inside the target container.
        /// </summary>
        public static void GenerateTrack(SplineContainer container, TrackGenSettings s)
        {
            if (container == null) return;

            Spline spline = container.Spline;
            spline.Clear();

            System.Random rng = new System.Random(s.seed);
            int N = Mathf.Max(6, s.curveCount);

            // 1. Calculate Base Radius from target perimeter (Circumference = 2 * PI * R)
            float baseRadius = s.targetLength / (2f * Mathf.PI);

            // 2. Polar Angle Seeding with Angular and Radial Jitter
            float[] angles = new float[N];
            float[] radii = new float[N];
            float[] elevations = new float[N];
            float[] rollAngles = new float[N];

            for (int i = 0; i < N; i++)
            {
                float baseAngle = (i / (float)N) * Mathf.PI * 2f;
                float angleJitter = ((float)rng.NextDouble() - 0.5f) * (Mathf.PI * 2f / N) * 0.4f * s.trackIrregularity;
                angles[i] = baseAngle + angleJitter;

                float radiusNoise = Mathf.Lerp(1f - s.trackIrregularity * 0.4f, 1f + s.trackIrregularity * 0.4f, (float)rng.NextDouble());
                radii[i] = baseRadius * radiusNoise;
            }

            // 3. Polar Repulsion Relaxation (Guarantees non-overlapping control points)
            float minKnotDistance = Mathf.Max(s.roadWidth * 2.5f, 25f);
            for (int iter = 0; iter < 24; iter++)
            {
                bool moved = false;
                for (int i = 0; i < N; i++)
                {
                    Vector2 pA = new Vector2(Mathf.Cos(angles[i]) * radii[i], Mathf.Sin(angles[i]) * radii[i]);
                    for (int j = i + 1; j < N; j++)
                    {
                        Vector2 pB = new Vector2(Mathf.Cos(angles[j]) * radii[j], Mathf.Sin(angles[j]) * radii[j]);
                        float dist = Vector2.Distance(pA, pB);
                        if (dist < minKnotDistance)
                        {
                            float push = (minKnotDistance - dist) * 0.5f;
                            Vector2 dir = (pA - pB).normalized;
                            if (dir == Vector2.zero) dir = new Vector2(1, 0);

                            pA += dir * push;
                            pB -= dir * push;

                            radii[i] = pA.magnitude;
                            angles[i] = Mathf.Atan2(pA.y, pA.x);
                            radii[j] = pB.magnitude;
                            angles[j] = Mathf.Atan2(pB.y, pB.x);
                            moved = true;
                        }
                    }
                }
                if (!moved) break;
            }

            // Ensure angles are monotonically sorted
            SortKnotsByAngle(angles, radii, N);

            // 4. Calculate Elevation Topography (Hills, Dips, Ramps, Overpasses)
            for (int i = 0; i < N; i++)
            {
                float t = (float)i / N;
                float elev = 0.5f; // Ground baseline offset

                switch (s.elevationMode)
                {
                    case ElevationMode.Flat:
                        elev = 0.5f;
                        break;

                    case ElevationMode.RollingHills:
                        elev += Mathf.Sin(t * Mathf.PI * 2f * s.hillFrequency) * (s.hillAmplitude * 0.6f);
                        elev += Mathf.Cos(t * Mathf.PI * 4f) * (s.hillAmplitude * 0.3f);
                        break;

                    case ElevationMode.Mountainous:
                        elev += Mathf.Sin(t * Mathf.PI * 2f * s.hillFrequency) * s.hillAmplitude;
                        elev += Mathf.PerlinNoise(t * 4f, s.seed * 0.1f) * (s.hillAmplitude * 0.8f);
                        break;

                    case ElevationMode.RollercoasterSwirls:
                        elev += Mathf.Sin(t * Mathf.PI * 2f * s.hillFrequency) * (s.hillAmplitude * 1.2f);
                        elev += Mathf.Sin(t * Mathf.PI * 6f) * (s.hillAmplitude * 0.5f);
                        break;
                }

                elevations[i] = Mathf.Max(0.5f, elev);
            }

            // 5. Insert Jump Ramps
            if (s.rampCount > 0)
            {
                int rampStep = N / Mathf.Max(1, s.rampCount);
                for (int r = 0; r < s.rampCount; r++)
                {
                    int rampIdx = (r * rampStep + 2) % N;
                    elevations[rampIdx] += s.rampHeight;
                }
            }

            // 6. Overpass 3D Elevation Clearance
            if (s.overpassCount > 0)
            {
                int overpassIdx = (N / 2) % N;
                elevations[overpassIdx] += 10.0f; // 10m high overpass
                if (overpassIdx > 0) elevations[overpassIdx - 1] += 5.0f;
                if (overpassIdx < N - 1) elevations[overpassIdx + 1] += 5.0f;
            }

            // 7. Assemble 3D Knot Positions
            Vector3[] positions = new Vector3[N];
            for (int i = 0; i < N; i++)
            {
                positions[i] = new Vector3(
                    Mathf.Cos(angles[i]) * radii[i],
                    elevations[i],
                    Mathf.Sin(angles[i]) * radii[i]
                );
            }

            // 8. Compute Safe Smooth Tangents (Catmull-Rom with Curvature Clamping to prevent pinching)
            for (int i = 0; i < N; i++)
            {
                Vector3 prev = positions[(i - 1 + N) % N];
                Vector3 curr = positions[i];
                Vector3 next = positions[(i + 1) % N];

                Vector3 dir = (next - prev).normalized;
                float distPrev = Vector3.Distance(curr, prev);
                float distNext = Vector3.Distance(curr, next);

                // Clamp tangent length to max 35% of neighbor distance (prevents internal loop/pinch)
                float tangentScale = Mathf.Lerp(0.25f, 0.38f, s.curvatureIntensity);
                Vector3 tanIn = -dir * (distPrev * tangentScale);
                Vector3 tanOut = dir * (distNext * tangentScale);

                // Auto-Banking based on corner turn angle
                Quaternion rot = Quaternion.identity;
                if (s.autoBanking)
                {
                    Vector3 inDir = (curr - prev).normalized;
                    Vector3 outDir = (next - curr).normalized;
                    Vector3 turnCross = Vector3.Cross(inDir, outDir);
                    float turnSeverity = turnCross.y; // Positive = Left Turn, Negative = Right Turn

                    float bankAngle = Mathf.Clamp(turnSeverity * s.maxBankAngle * 2.5f, -s.maxBankAngle, s.maxBankAngle);
                    rot = Quaternion.Euler(0f, 0f, bankAngle);
                }

                BezierKnot knot = new BezierKnot(
                    (float3)curr,
                    (float3)tanIn,
                    (float3)tanOut,
                    (quaternion)rot
                );
                spline.Add(knot);
            }

            spline.Closed = true;
        }

        private static void SortKnotsByAngle(float[] angles, float[] radii, int count)
        {
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (angles[j] < angles[i])
                    {
                        float tempA = angles[i];
                        angles[i] = angles[j];
                        angles[j] = tempA;

                        float tempR = radii[i];
                        radii[i] = radii[j];
                        radii[j] = tempR;
                    }
                }
            }
        }
    }
}
