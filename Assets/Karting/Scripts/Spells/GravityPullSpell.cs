using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Gravity Pull offensive spell. Creates an accretion vortex disk centered on the caster,
    /// pulling opposing karts within range inwards over time with smooth force falloff.
    /// Opponents currently phased (KartPhaseShift) or shielded (KartShield) are unaffected.
    /// </summary>
    public class GravityPullSpell : BaseSpell
    {
        [Header("Gravity Pull Settings")]
        [Tooltip("Effective attraction radius in world units.")]
        public float pullRadius = 16.0f;

        [Tooltip("Duration of the active gravity singularity in seconds.")]
        public float pullDuration = 2.5f;

        [Tooltip("Max inward acceleration force applied at the event horizon.")]
        public float pullStrength = 38.0f;

        [Tooltip("Upward lift stabilization damping factor to prevent karts flipping.")]
        public float verticalDamping = 0.5f;

        protected override void OnCast(ArcadeKart casterKart)
        {
            if (casterKart == null) return;

            // Spawn procedural neon concentric rings & converging streamers
            GravityPullVisuals.StartVisuals(casterKart, casterKart.transform, pullRadius, pullDuration);

            // Start the physics attraction loop
            casterKart.StartCoroutine(ExecuteGravityPull(casterKart));
        }

        private IEnumerator ExecuteGravityPull(ArcadeKart caster)
        {
            float elapsed = 0f;
            Collider[] overlapBuffer = new Collider[32];
            HashSet<Rigidbody> processedRigidbodies = new HashSet<Rigidbody>();

            while (elapsed < pullDuration && caster != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / pullDuration;

                // Pulsing intensity: smooth ease-in and ease-out
                float pulse = Mathf.Sin(progress * Mathf.PI);

                // Query nearby colliders
                int hitCount = Physics.OverlapSphereNonAlloc(caster.transform.position, pullRadius, overlapBuffer);
                processedRigidbodies.Clear();

                for (int i = 0; i < hitCount; i++)
                {
                    Collider col = overlapBuffer[i];
                    if (col == null) continue;

                    Rigidbody targetRb = col.attachedRigidbody;
                    if (targetRb == null || targetRb == caster.Rigidbody || processedRigidbodies.Contains(targetRb))
                        continue;

                    processedRigidbodies.Add(targetRb);

                    Transform targetRoot = targetRb.transform;
                    ArcadeKart targetKart = targetRoot.GetComponent<ArcadeKart>() ?? targetRoot.GetComponentInParent<ArcadeKart>();

                    // Opponent check: ignore non-karts or unaffected karts
                    if (targetKart == null || targetKart == caster)
                        continue;

                    // Immunity checks: Phase Shift or Shield active
                    KartPhaseShift phase = targetKart.GetComponent<KartPhaseShift>();
                    if (phase != null && phase.IsPhased)
                        continue;

                    KartShield shield = targetKart.GetComponent<KartShield>();
                    if (shield != null && shield.HasShield)
                        continue;

                    // Inward vector calculation towards caster center
                    Vector3 displacement = caster.transform.position - targetRb.position;
                    float distance = displacement.magnitude;
                    if (distance < 0.3f) continue; // Singularity core deadzone

                    Vector3 inwardDirection = displacement / distance;
                    // Dampen vertical pull to keep target karts grounded
                    inwardDirection.y *= verticalDamping;

                    // Falloff: maximum force closer to center, smooth linear drop to boundary
                    float falloff = 1.0f - Mathf.Clamp01(distance / pullRadius);
                    float currentForce = pullStrength * falloff * pulse;

                    targetRb.AddForce(inwardDirection.normalized * currentForce, ForceMode.Acceleration);
                }

                yield return null;
            }
        }
    }
}

