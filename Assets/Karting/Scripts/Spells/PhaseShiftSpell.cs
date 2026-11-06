using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Defensive spell that temporarily phases the kart out of normal space into an intangible wireframe hologram.
    /// While phased, offensive spells (projectiles, mines, hexes) pass through harmlessly.
    /// </summary>
    public class PhaseShiftSpell : BaseSpell
    {
        [Header("Phase Shift Settings")]
        [Tooltip("Duration in seconds the kart remains phased / intangible.")]
        public float phaseDuration = 1.5f;

        protected override void OnCast(ArcadeKart kart)
        {
            if (kart == null) return;

            KartPhaseShift phaseShift = kart.GetComponent<KartPhaseShift>();
            if (phaseShift == null)
                phaseShift = kart.GetComponentInParent<KartPhaseShift>();

            if (phaseShift == null)
            {
                phaseShift = kart.gameObject.AddComponent<KartPhaseShift>();
            }

            phaseShift.ActivatePhaseShift(phaseDuration);
            Debug.Log($"[PhaseShiftSpell] Phase Shift activated on {kart.name} for {phaseDuration} seconds.");
        }
    }
}

