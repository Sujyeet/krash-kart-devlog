using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Tracks the intangible phased state of the kart.
    /// Handles procedural material swapping to holographic wireframe and restores original materials.
    /// While IsPhased is true, all offensive spells (projectiles, mines, hexes) pass through harmlessly.
    /// Supports Netcode multiplayer synchronization and single-player fallback.
    /// </summary>
    public class KartPhaseShift : NetworkBehaviour
    {
        public NetworkVariable<bool> NetworkIsPhased = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private bool localIsPhased = false;
        private Coroutine activePhaseCoroutine;
        private Shader phaseShader;
        private Material phaseMaterialInstance;

        // Renderer material snapshot for safe restoration
        private readonly List<Renderer> targetRenderers = new List<Renderer>();
        private readonly List<Material[]> originalMaterials = new List<Material[]>();
        private readonly List<Collider> ignoredKartColliders = new List<Collider>();
        private Collider[] myColliders;

        public bool IsPhased
        {
            get
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)
                    return NetworkIsPhased.Value;
                return localIsPhased;
            }
        }

        private void Awake()
        {
            CacheOriginalRenderers();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkIsPhased.OnValueChanged += OnPhaseNetworkStateChanged;

            if (NetworkIsPhased.Value)
            {
                ApplyWireframeHologram();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            NetworkIsPhased.OnValueChanged -= OnPhaseNetworkStateChanged;
            RestoreOriginalMaterials();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RestoreOriginalMaterials();
        }

        private void OnPhaseNetworkStateChanged(bool previous, bool current)
        {
            if (current)
                ApplyWireframeHologram();
            else
                RestoreOriginalMaterials();
        }

        private void CacheOriginalRenderers()
        {
            targetRenderers.Clear();
            originalMaterials.Clear();

            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in rends)
            {
                // Skip UI, particles, or rear view camera objects
                if (r is TrailRenderer || r is ParticleSystemRenderer) continue;
                if (r.gameObject.name.Contains("ShieldBubble") || r.gameObject.name.Contains("Camera")) continue;

                targetRenderers.Add(r);
                originalMaterials.Add(r.sharedMaterials);
            }
        }

        /// <summary>
        /// Activates Phase Shift on this kart for the specified duration.
        /// </summary>
        public void ActivatePhaseShift(float duration)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)
            {
                if (IsServer)
                {
                    ApplyServerPhase(duration);
                }
                else
                {
                    ActivatePhaseServerRpc(duration);
                }
            }
            else
            {
                ApplyLocalPhase(duration);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ActivatePhaseServerRpc(float duration)
        {
            ApplyServerPhase(duration);
        }

        private void ApplyServerPhase(float duration)
        {
            NetworkIsPhased.Value = true;

            if (activePhaseCoroutine != null)
                StopCoroutine(activePhaseCoroutine);
            activePhaseCoroutine = StartCoroutine(ServerPhaseTimer(duration));

            ApplyPhaseVisualClientRpc();
        }

        private void ApplyLocalPhase(float duration)
        {
            localIsPhased = true;

            if (activePhaseCoroutine != null)
                StopCoroutine(activePhaseCoroutine);
            activePhaseCoroutine = StartCoroutine(LocalPhaseTimer(duration));

            ApplyWireframeHologram();
        }

        private IEnumerator ServerPhaseTimer(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (NetworkIsPhased.Value)
            {
                NetworkIsPhased.Value = false;
                RemovePhaseVisualClientRpc();
            }
            activePhaseCoroutine = null;
        }

        private IEnumerator LocalPhaseTimer(float duration)
        {
            yield return new WaitForSeconds(duration);
            localIsPhased = false;
            RestoreOriginalMaterials();
            activePhaseCoroutine = null;
        }

        [ClientRpc]
        private void ApplyPhaseVisualClientRpc()
        {
            ApplyWireframeHologram();
        }

        [ClientRpc]
        private void RemovePhaseVisualClientRpc()
        {
            RestoreOriginalMaterials();
        }

        private Shader GetPhaseShader()
        {
            if (phaseShader == null)
            {
                phaseShader = Shader.Find("KartGame/PhaseShiftWireframe")
                    ?? Shader.Find("KartGame/BlinkWarp")
                    ?? Shader.Find("KartGame/ShieldBubble");
            }
            return phaseShader;
        }

        private void ApplyWireframeHologram()
        {
            SetKartPhasingCollisions(true);

            if (targetRenderers.Count == 0)
                CacheOriginalRenderers();

            if (phaseMaterialInstance == null)
            {
                phaseMaterialInstance = new Material(GetPhaseShader())
                {
                    hideFlags = HideFlags.DontSave
                };
                phaseMaterialInstance.SetColor("_GridColor", new Color(0.0f, 0.85f, 1.0f, 1.0f));
                phaseMaterialInstance.SetColor("_GlowColor", new Color(0.1f, 0.5f, 1.0f, 1.0f));
                phaseMaterialInstance.SetFloat("_GlowIntensity", 2.5f);
                phaseMaterialInstance.SetFloat("_Alpha", 0.9f);
            }

            for (int i = 0; i < targetRenderers.Count; i++)
            {
                if (targetRenderers[i] == null) continue;

                int slotCount = targetRenderers[i].sharedMaterials.Length;
                Material[] wireframeMats = new Material[slotCount];
                for (int j = 0; j < slotCount; j++)
                {
                    wireframeMats[j] = phaseMaterialInstance;
                }
                targetRenderers[i].materials = wireframeMats;
            }
        }

        private void RestoreOriginalMaterials()
        {
            SetKartPhasingCollisions(false);

            for (int i = 0; i < targetRenderers.Count; i++)
            {
                if (targetRenderers[i] != null && i < originalMaterials.Count && originalMaterials[i] != null)
                {
                    targetRenderers[i].sharedMaterials = originalMaterials[i];
                }
            }

            if (phaseMaterialInstance != null)
            {
                Destroy(phaseMaterialInstance);
                phaseMaterialInstance = null;
            }
        }

        private void SetKartPhasingCollisions(bool ignore)
        {
            if (myColliders == null || myColliders.Length == 0)
            {
                myColliders = GetComponentsInChildren<Collider>(true);
            }

            if (ignore)
            {
                ignoredKartColliders.Clear();
                ArcadeKart[] allKarts = FindObjectsOfType<ArcadeKart>();
                for (int k = 0; k < allKarts.Length; k++)
                {
                    if (allKarts[k] == null || allKarts[k].gameObject == gameObject) continue;

                    Collider[] otherColliders = allKarts[k].GetComponentsInChildren<Collider>(true);
                    for (int o = 0; o < otherColliders.Length; o++)
                    {
                        Collider otherCol = otherColliders[o];
                        if (otherCol == null || otherCol.isTrigger) continue;

                        for (int m = 0; m < myColliders.Length; m++)
                        {
                            Collider myCol = myColliders[m];
                            if (myCol == null || myCol.isTrigger) continue;

                            Physics.IgnoreCollision(myCol, otherCol, true);
                        }

                        if (!ignoredKartColliders.Contains(otherCol))
                        {
                            ignoredKartColliders.Add(otherCol);
                        }
                    }
                }
            }
            else
            {
                if (myColliders != null)
                {
                    for (int o = 0; o < ignoredKartColliders.Count; o++)
                    {
                        Collider otherCol = ignoredKartColliders[o];
                        if (otherCol == null) continue;

                        for (int m = 0; m < myColliders.Length; m++)
                        {
                            Collider myCol = myColliders[m];
                            if (myCol == null) continue;

                            Physics.IgnoreCollision(myCol, otherCol, false);
                        }
                    }
                }
                ignoredKartColliders.Clear();
            }
        }
    }
}

