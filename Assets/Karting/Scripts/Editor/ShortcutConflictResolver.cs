#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace KartGame.Editor
{
    [InitializeOnLoad]
    public static class ShortcutConflictResolver
    {
        static ShortcutConflictResolver()
        {
            EditorApplication.delayCall += ResolveAllConflicts;
        }

        [MenuItem("Krash Kart/Resolve Shortcut Conflicts", false, 100)]
        public static void ResolveAllConflicts()
        {
            var manager = ShortcutManager.instance;
            if (manager == null) return;

            var allIds = manager.GetAvailableShortcutIds();
            int clearedCount = 0;

            foreach (var id in allIds)
            {
                // Skip our own scene switcher shortcuts
                if (id.StartsWith("Main Menu/Scenes/") || id.StartsWith("Scenes/"))
                    continue;

                var binding = manager.GetShortcutBinding(id);
                var combos = binding.keyCombinationSequence;
                if (combos == null) continue;

                foreach (var combo in combos)
                {
                    // Check if shortcut uses Alt+1 (Keypad1 or Alpha1 with Alt)
                    bool isAlt1 = combo.alt && (combo.keyCode == KeyCode.Alpha1 || combo.keyCode == KeyCode.Keypad1);
                    bool isAlt2 = combo.alt && (combo.keyCode == KeyCode.Alpha2 || combo.keyCode == KeyCode.Keypad2);
                    bool isAlt5 = combo.alt && (combo.keyCode == KeyCode.Alpha5 || combo.keyCode == KeyCode.Keypad5);
                    bool isAltC = combo.alt && combo.keyCode == KeyCode.C && id.Contains("ProBuilder");
                    bool isCtrlShiftC = combo.action && combo.shift && combo.keyCode == KeyCode.C && !id.Contains("Console");

                    if (isAlt1 || isAlt2 || isAlt5 || isAltC || isCtrlShiftC)
                    {
                        try
                        {
                            manager.RebindShortcut(id, default);
                            Debug.Log($"[ShortcutConflictResolver] Cleared conflicting shortcut: '{id}'");
                            clearedCount++;
                            break;
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[ShortcutConflictResolver] Could not clear '{id}': {ex.Message}");
                        }
                    }
                }
            }

            Debug.Log($"[ShortcutConflictResolver] Success! Unbound {clearedCount} conflicting shortcut(s). Alt+1 (IntroMenu) and Alt+2 (MainScene) are now fully dedicated.");
        }
    }
}
#endif
