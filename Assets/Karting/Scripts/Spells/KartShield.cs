using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Component attached to a kart that manages its active shield state.
    /// Blocks exactly one offensive hit (projectile, trap mine, or hex), absorbs the effect,
    /// and triggers visual bubble dissolution and feedback.
    /// Operates with server authority in Netcode multiplayer, with offline single-player fallback.
    /// </summary>
    public class KartShield : NetworkBehaviour
    {
        [Header("Bubble Visuals")]
        [Tooltip("Optional prefab with ShieldBubble component. If left null, a procedural bubble is generated at runtime.")]
        public ShieldBubble customBubblePrefab;
        [SerializeField] private Vector3 bubbleCenterOffset = new Vector3(0f, 0.6f, 0f);

        [Header("Feedback Effects")]
        [SerializeField] private GameObject breakVFX;
        [SerializeField] private AudioClip breakSFX;

        // Multiplayer networked state
        public NetworkVariable<bool> IsActive = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Offline single-player state
        private bool isLocalActive = false;

        private ShieldBubble currentBubble;
        private ArcadeKart kart;
        private Coroutine expirationCoroutine;

        public bool HasShield
        {
            get
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)
                    return IsActive.Value;
                return isLocalActive;
            }
        }

        private void Awake()
        {
            kart = GetComponent<ArcadeKart>();
            if (kart == null)
                kart = GetComponentInParent<ArcadeKart>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            IsActive.OnValueChanged += OnShieldNetworkStateChanged;
            if (IsActive.Value && currentBubble == null)
            {
                CreateAndAttachBubble();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            IsActive.OnValueChanged -= OnShieldNetworkStateChanged;
            if (currentBubble != null)
            {
                Destroy(currentBubble.gameObject);
                currentBubble = null;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (currentBubble != null)
            {
                Destroy(currentBubble.gameObject);
                currentBubble = null;
            }
        }

        private void OnShieldNetworkStateChanged(bool previous, bool current)
        {
            if (current && currentBubble == null)
            {
                CreateAndAttachBubble();
            }
            else if (!current && currentBubble != null)
            {
                currentBubble.Despawn();
                currentBubble = null;
            }
        }

        /// <summary>
        /// Activates the shield on this kart for the specified duration.
        /// </summary>
        public void ActivateShield(float duration)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)
            {
                if (IsServer)
                {
                    ApplyServerShieldActivation(duration);
                }
                else
                {
                    ActivateShieldServerRpc(duration);
                }
            }
            else
            {
                // Single-player offline execution or fallback if not spawned over network
                ApplyLocalShieldActivation(duration);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ActivateShieldServerRpc(float duration)
        {
            ApplyServerShieldActivation(duration);
        }

        private void ApplyServerShieldActivation(float duration)
        {
            IsActive.Value = true;

            if (expirationCoroutine != null)
                StopCoroutine(expirationCoroutine);
            expirationCoroutine = StartCoroutine(ServerExpirationRoutine(duration));

            SpawnBubbleClientRpc();
        }

        private void ApplyLocalShieldActivation(float duration)
        {
            isLocalActive = true;

            if (expirationCoroutine != null)
                StopCoroutine(expirationCoroutine);
            expirationCoroutine = StartCoroutine(LocalExpirationRoutine(duration));

            if (currentBubble != null)
                Destroy(currentBubble.gameObject);

            CreateAndAttachBubble();
        }

        /// <summary>
        /// Attempts to block an incoming hostile spell/hazard.
        /// Returns true if the shield absorbed the impact (and was consumed).
        /// </summary>
        public bool TryBlockHit()
        {
            if (!HasShield)
                return false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)
            {
                if (!IsServer)
                    return false;

                // Server consumes shield
                IsActive.Value = false;
                if (expirationCoroutine != null)
                {
                    StopCoroutine(expirationCoroutine);
                    expirationCoroutine = null;
                }

                BreakShieldClientRpc();
                return true;
            }
            else
            {
                // Offline consumption
                isLocalActive = false;
                if (expirationCoroutine != null)
                {
                    StopCoroutine(expirationCoroutine);
                    expirationCoroutine = null;
                }

                BreakShieldLocal();
                return true;
            }
        }

        private IEnumerator ServerExpirationRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (IsActive.Value)
            {
                IsActive.Value = false;
                DespawnBubbleClientRpc();
            }
            expirationCoroutine = null;
        }

        private IEnumerator LocalExpirationRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (isLocalActive)
            {
                isLocalActive = false;
                if (currentBubble != null)
                {
                    currentBubble.Despawn();
                    currentBubble = null;
                }
            }
            expirationCoroutine = null;
        }

        [ClientRpc]
        private void SpawnBubbleClientRpc()
        {
            if (currentBubble != null)
                Destroy(currentBubble.gameObject);

            CreateAndAttachBubble();
        }

        [ClientRpc]
        private void BreakShieldClientRpc()
        {
            BreakShieldLocal();
        }

        [ClientRpc]
        private void DespawnBubbleClientRpc()
        {
            if (currentBubble != null)
            {
                currentBubble.Despawn();
                currentBubble = null;
            }
        }

        private void BreakShieldLocal()
        {
            if (currentBubble != null)
            {
                currentBubble.Break();
                currentBubble = null;
            }

            if (breakVFX != null)
            {
                GameObject vfx = Instantiate(breakVFX, transform.position + bubbleCenterOffset, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            if (breakSFX != null)
            {
                AudioSource.PlayClipAtPoint(breakSFX, transform.position + bubbleCenterOffset);
            }
        }

        private void CreateAndAttachBubble()
        {
            if (customBubblePrefab != null)
            {
                currentBubble = Instantiate(customBubblePrefab, transform);
            }
            else
            {
                GameObject bubbleObj = new GameObject("ShieldBubble_Procedural");
                bubbleObj.transform.SetParent(transform, false);
                currentBubble = bubbleObj.AddComponent<ShieldBubble>();
            }

            currentBubble.transform.localPosition = bubbleCenterOffset;
            currentBubble.transform.localRotation = Quaternion.identity;
            currentBubble.Initialize(kart);
        }
    }
}

