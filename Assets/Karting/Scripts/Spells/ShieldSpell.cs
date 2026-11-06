using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Defensive spell that deploys a protective energy barrier over the kart.
    /// Blocks exactly 1 hostile spell or hazard (such as a projectile or trap mine).
    /// </summary>
    public class ShieldSpell : BaseSpell
    {
        [Header("Shield Spell Settings")]
        [Tooltip("Duration in seconds the shield remains active before dissipating if unhit.")]
        public float shieldDuration = 6f;
        [Tooltip("Optional custom shield VFX prefab. If null, procedural bubble is used.")]
        public GameObject customShieldVFXPrefab;

        protected override void OnCast(ArcadeKart kart)
        {
            if (kart == null)
            {
                Debug.LogWarning("[ShieldSpell] Cast attempted with null kart reference.");
                return;
            }

            KartShield shield = kart.GetComponent<KartShield>();
            if (shield == null)
                shield = kart.GetComponentInParent<KartShield>();

            if (shield == null)
            {
                Debug.LogWarning($"[ShieldSpell] KartShield component not found on {kart.name}. Adding local fallback.");
                shield = kart.gameObject.AddComponent<KartShield>();
            }

            shield.ActivateShield(shieldDuration);
            Debug.Log($"[ShieldSpell] Energy shield activated on {kart.name} for {shieldDuration} seconds.");
        }
    }
}

