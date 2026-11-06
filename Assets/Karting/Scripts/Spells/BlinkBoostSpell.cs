using UnityEngine;
using Unity.Netcode;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Utility spell: Short-range forward teleportation with safe geometry sweep and momentum preservation.
    /// Spawns procedural departure flash, afterimage ghost, warp streak ribbon, and arrival shockwave.
    /// </summary>
    public class BlinkBoostSpell : BaseSpell
    {
        [Header("Blink Kinematics")]
        [Tooltip("Forward teleport distance in world units.")]
        public float blinkDistance = 14f;

        [Tooltip("Additional forward speed boost applied upon emerging.")]
        public float postBlinkSpeedBoost = 6f;

        [Tooltip("Duration of the momentum power-up after blink.")]
        public float boostDuration = 1.8f;

        [Header("Collision Safety")]
        [Tooltip("Radius of the sphere sweep used to check obstacles in the path.")]
        public float collisionRadius = 0.9f;

        [Tooltip("Skin margin to keep away from walls and guard rails.")]
        public float skinMargin = 0.5f;

        [Tooltip("Layer mask representing solid barriers, walls, and track boundaries.")]
        public LayerMask obstacleMask = ~0; // Default all layers

        protected override void OnCast(ArcadeKart kart)
        {
            if (kart == null) return;

            Vector3 startPos = kart.transform.position;
            Vector3 forwardDir = kart.transform.forward;
            Quaternion kartRot = kart.transform.rotation;

            // 1. Safe collision sweep: never phase through walls or get stuck inside geometry
            Vector3 targetPos = ComputeSafeDestination(startPos, forwardDir, kart);

            // 2. Departure procedural visuals (flash + afterimage ghost)
            BlinkBoostVisuals.SpawnDepartureFlash(this, startPos + (Vector3.up * 0.4f));
            BlinkBoostVisuals.SpawnGhostAfterImage(this, kart, startPos, kartRot);

            // 3. Teleport execution (safe for Rigidbody and Netcode)
            TeleportKart(kart, targetPos);

            // 4. Arrival procedural visuals (warp streak ribbon + arrival shockwave)
            BlinkBoostVisuals.SpawnWarpStreak(this, startPos + (Vector3.up * 0.4f), targetPos + (Vector3.up * 0.4f));
            BlinkBoostVisuals.SpawnArrivalShockwave(this, targetPos);

            // 5. Momentum boost upon emergence
            ApplyPostBlinkMomentum(kart, forwardDir);

            Debug.Log($"[BlinkBoostSpell] Teleported {kart.name} forward safely by {Vector3.Distance(startPos, targetPos):F1}m.");
        }

        private Vector3 ComputeSafeDestination(Vector3 start, Vector3 direction, ArcadeKart kart)
        {
            float maxDistance = blinkDistance;
            Vector3 sweepOrigin = start + (Vector3.up * 0.5f);

            // Raycast / SphereCast forward to detect track barriers and walls
            RaycastHit hit;
            if (Physics.SphereCast(sweepOrigin, collisionRadius, direction, out hit, maxDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // Ensure hit object is not the kart itself or its children
                if (!hit.transform.IsChildOf(kart.transform))
                {
                    float safeDist = Mathf.Max(0.5f, hit.distance - skinMargin);
                    Vector3 clampedDest = start + (direction * safeDist);
                    return clampedDest;
                }
            }

            // Raycast down from intended destination to match track surface elevation
            Vector3 rawDest = start + (direction * maxDistance);
            if (Physics.Raycast(rawDest + (Vector3.up * 2.0f), Vector3.down, out RaycastHit groundHit, 5.0f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                rawDest.y = groundHit.point.y + 0.1f;
            }

            return rawDest;
        }

        private void TeleportKart(ArcadeKart kart, Vector3 targetPos)
        {
            Rigidbody rb = kart.Rigidbody;
            if (rb != null)
            {
                rb.position = targetPos;
            }
            kart.transform.position = targetPos;
        }

        private void ApplyPostBlinkMomentum(ArcadeKart kart, Vector3 forwardDir)
        {
            Rigidbody rb = kart.Rigidbody;
            if (rb != null)
            {
                // Preserve forward velocity and eliminate unwanted lateral drift
                float currentFwdSpeed = Mathf.Max(0f, Vector3.Dot(rb.velocity, forwardDir));
                rb.velocity = forwardDir * (currentFwdSpeed + postBlinkSpeedBoost);
            }

            // Apply ArcadeKart StatPowerup to smoothly reward the player with higher top speed
            var boostPowerup = new ArcadeKart.StatPowerups
            {
                PowerUpID = "BlinkBoostSurge",
                MaxTime = boostDuration,
                ElapsedTime = 0f,
                modifiers = new ArcadeKart.Stats
                {
                    TopSpeed = postBlinkSpeedBoost,
                    Acceleration = 4.0f
                }
            };
            kart.AddPowerup(boostPowerup);
        }
    }
}

