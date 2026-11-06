using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace KartGame.KartSystems
{
    public class SpellSystem : MonoBehaviour
    {
        [Header("Spell Slots")]
        public BaseSpell spellSlot1; // I
        public BaseSpell spellSlot2; // J
        public BaseSpell spellSlot3; // K
        public BaseSpell spellSlot4; // L
        public BaseSpell spellSlotSpace; // Space (Phase Shift / Quick Defensive)
        public BaseSpell spellSlotTab; // Tab (Gravity Pull / Crowd Control)

        [Header("HUD - Slot 1 (I)")]
        public Image slot1CooldownFill;
        public TextMeshProUGUI slot1KeyLabel;

        [Header("HUD - Slot 2 (J)")]
        public Image slot2CooldownFill;
        public TextMeshProUGUI slot2KeyLabel;

        [Header("HUD - Slot 3 (K)")]
        public Image slot3CooldownFill;
        public TextMeshProUGUI slot3KeyLabel;

        [Header("HUD - Slot 4 (L)")]
        public Image slot4CooldownFill;
        public TextMeshProUGUI slot4KeyLabel;

        [Header("HUD - Space Slot")]
        public Image spaceCooldownFill;
        public TextMeshProUGUI spaceKeyLabel;

        [Header("HUD - Tab Slot")]
        public Image tabCooldownFill;
        public TextMeshProUGUI tabKeyLabel;

        private ArcadeKart kart;
        private NetworkObject networkObject;

        void Awake()
        {
            kart = GetComponent<ArcadeKart>();
            networkObject = GetComponent<NetworkObject>();
        }

        void Start()
        {
            if (slot1KeyLabel) slot1KeyLabel.text = "I";
            if (slot2KeyLabel) slot2KeyLabel.text = "J";
            if (slot3KeyLabel) slot3KeyLabel.text = "K";
            if (slot4KeyLabel) slot4KeyLabel.text = "L";
            if (spaceKeyLabel) spaceKeyLabel.text = "SPACE";
            if (tabKeyLabel) tabKeyLabel.text = "TAB";
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;

            if (networkObject != null && networkObject.IsSpawned && !networkObject.IsOwner)
                return;

            if (Input.GetKeyDown(KeyCode.I))
                TryCastSpell(spellSlot1);

            if (Input.GetKeyDown(KeyCode.J))
                TryCastSpell(spellSlot2);

            if (Input.GetKeyDown(KeyCode.K))
                TryCastSpell(spellSlot3);

            if (Input.GetKeyDown(KeyCode.L))
                TryCastSpell(spellSlot4);

            if (Input.GetKeyDown(KeyCode.Space))
                TryCastSpell(spellSlotSpace);

            if (Input.GetKeyDown(KeyCode.Tab))
                TryCastSpell(spellSlotTab);

            UpdateCooldownUI(spellSlot1, slot1CooldownFill);
            UpdateCooldownUI(spellSlot2, slot2CooldownFill);
            UpdateCooldownUI(spellSlot3, slot3CooldownFill);
            UpdateCooldownUI(spellSlot4, slot4CooldownFill);
            UpdateCooldownUI(spellSlotSpace, spaceCooldownFill);
            UpdateCooldownUI(spellSlotTab, tabCooldownFill);
        }

        void TryCastSpell(BaseSpell spell)
        {
            if (spell == null) return;
            if (!spell.IsReady()) return;

            spell.Cast(kart);
        }

        void UpdateCooldownUI(BaseSpell spell, Image fillImage)
        {
            if (fillImage == null) return;
            
            if (spell == null)
            {
                fillImage.fillAmount = 0f; // Force clear if no spell assigned
                return;
            }

            fillImage.fillAmount = 1f - spell.GetCooldownPercent();
        }
    }
}