using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace KartGame.KartSystems
{
    public class SpellProjectile : NetworkBehaviour
    {
        [Header("Movement")]
        public float speed = 24f;
        public float lifetime = 4f;

        [Header("Launch Effect")]
        public float upwardForce = 8f;
        public float forwardForce = 10f;
        public float sideForce = 0f;

        [Header("VFX")]
        public GameObject hitVFX;

        private ulong ownerClientId;
        private bool hasHit = false;

        public void SetOwner(ulong ownerId)
        {
            ownerClientId = ownerId;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                StartCoroutine(DestroyAfterLifetime());
        }

        void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
                return;

            transform.position += transform.forward * speed * Time.deltaTime;
        }

        IEnumerator DestroyAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);

            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn();
            else
                Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
                return;

            if (hasHit)
                return;

            ArcadeKart kart = other.GetComponent<ArcadeKart>();
            if (kart == null)
                kart = other.GetComponentInParent<ArcadeKart>();

            if (kart != null)
            {
                NetworkObject kartNetObj = kart.GetComponent<NetworkObject>();
                if (kartNetObj == null)
                    kartNetObj = kart.GetComponentInParent<NetworkObject>();

                if (kartNetObj != null && kartNetObj.NetworkObjectId == ownerClientId && ownerClientId != 0)
                    return;

                // Check if target kart is phased / intangible
                KartPhaseShift phaseShift = kart.GetComponent<KartPhaseShift>();
                if (phaseShift == null)
                    phaseShift = kart.GetComponentInParent<KartPhaseShift>();

                if (phaseShift != null && phaseShift.IsPhased)
                {
                    // Intangible: projectile phases right through kart without detonation
                    return;
                }

                hasHit = true;

                // Check if the target kart has an active shield
                KartShield shield = kart.GetComponent<KartShield>();
                if (shield == null)
                    shield = kart.GetComponentInParent<KartShield>();

                if (shield != null && shield.TryBlockHit())
                {
                    if (hitVFX != null)
                    {
                        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                            SpawnHitVFXClientRpc(transform.position);
                        else
                            Destroy(Instantiate(hitVFX, transform.position, Quaternion.identity), 2f);
                    }

                    if (NetworkObject != null && NetworkObject.IsSpawned)
                        NetworkObject.Despawn();
                    else
                        Destroy(gameObject);

                    return;
                }

                Vector3 launchForce =
                    transform.forward * forwardForce +
                    transform.right * sideForce +
                    Vector3.up * upwardForce;

                if (kartNetObj != null && kartNetObj.IsSpawned)
                    ApplyLaunchClientRpc(kartNetObj.NetworkObjectId, launchForce);
                else
                    kart.ApplyLaunch(launchForce);

                if (hitVFX != null)
                {
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                        SpawnHitVFXClientRpc(transform.position);
                    else
                        Destroy(Instantiate(hitVFX, transform.position, Quaternion.identity), 2f);
                }

                if (NetworkObject != null && NetworkObject.IsSpawned)
                    NetworkObject.Despawn();
                else
                    Destroy(gameObject);

                return;
            }

            // Hit environment / wall / obstacle
            hasHit = true;

            if (hitVFX != null)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    SpawnHitVFXClientRpc(transform.position);
                else
                    Destroy(Instantiate(hitVFX, transform.position, Quaternion.identity), 2f);
            }

            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn();
            else
                Destroy(gameObject);
        }

        [ClientRpc]
        void ApplyLaunchClientRpc(ulong targetNetworkObjectId, Vector3 force)
        {
            NetworkObject targetNetObj = null;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject spawnedObj))
                {
                    targetNetObj = spawnedObj;
                }
            }

            if (targetNetObj != null)
            {
                // Verify if we are the owner of this specific NetworkObject (player owns their kart, server owns AI karts)
                if (targetNetObj.IsOwner)
                {
                    ArcadeKart kart = targetNetObj.GetComponent<ArcadeKart>();
                    if (kart == null) kart = targetNetObj.GetComponentInChildren<ArcadeKart>();
                    if (kart != null)
                    {
                        kart.ApplyLaunch(force);
                    }
                }
            }
        }

        [ClientRpc]
        void SpawnHitVFXClientRpc(Vector3 position)
        {
            if (hitVFX != null)
            {
                GameObject vfx = Instantiate(hitVFX, position, Quaternion.identity);
                Destroy(vfx, 2f);
            }
        }
    }
}