using System.Collections.Generic;
using UnityEngine;
using KartGame.KartSystems;

namespace KartGame.Spells
{
    public class PowerupDraftManager : MonoBehaviour
    {
        public static PowerupDraftManager Instance { get; private set; }

        [Header("Available Power-ups Catalog")]
        [Tooltip("List of all available spell/power-up prefabs in the game.")]
        [SerializeField] private List<BaseSpell> registeredSpells = new List<BaseSpell>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Returns all registered spells implemented in the game.
        /// </summary>
        public IReadOnlyList<BaseSpell> GetAllSpells()
        {
            return registeredSpells;
        }

        /// <summary>
        /// Generates a randomized list of unique spell options for a draft selection screen.
        /// </summary>
        public List<BaseSpell> GenerateDraftOptions(int count)
        {
            List<BaseSpell> options = new List<BaseSpell>();
            if (registeredSpells == null || registeredSpells.Count == 0)
                return options;

            List<BaseSpell> pool = new List<BaseSpell>(registeredSpells);
            int optionsCount = Mathf.Min(count, pool.Count);

            for (int i = 0; i < optionsCount; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                options.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex);
            }

            return options;
        }

        /// <summary>
        /// Equips a selected spell prefab into the target kart's SpellSystem slot.
        /// </summary>
        public bool EquipSpellToKart(ArcadeKart kart, BaseSpell spellPrefab, int slotIndex)
        {
            if (kart == null || spellPrefab == null) return false;

            SpellSystem spellSystem = kart.GetComponent<SpellSystem>();
            if (spellSystem == null) return false;

            // Instantiate spell as a child of the kart
            BaseSpell newSpellInstance = Instantiate(spellPrefab, kart.transform);
            
            switch (slotIndex)
            {
                case 1:
                    if (spellSystem.spellSlot1 != null) Destroy(spellSystem.spellSlot1.gameObject);
                    spellSystem.spellSlot1 = newSpellInstance;
                    break;
                case 2:
                    if (spellSystem.spellSlot2 != null) Destroy(spellSystem.spellSlot2.gameObject);
                    spellSystem.spellSlot2 = newSpellInstance;
                    break;
                case 3:
                    if (spellSystem.spellSlot3 != null) Destroy(spellSystem.spellSlot3.gameObject);
                    spellSystem.spellSlot3 = newSpellInstance;
                    break;
                case 4:
                    if (spellSystem.spellSlot4 != null) Destroy(spellSystem.spellSlot4.gameObject);
                    spellSystem.spellSlot4 = newSpellInstance;
                    break;
                default:
                    Debug.LogWarning($"[PowerupDraftManager] Invalid slot index {slotIndex}. Must be 1-4.");
                    Destroy(newSpellInstance.gameObject);
                    return false;
            }

            Debug.Log($"[PowerupDraftManager] Equipped {spellPrefab.spellName} to slot {slotIndex} on {kart.name}");
            return true;
        }
    }
}
