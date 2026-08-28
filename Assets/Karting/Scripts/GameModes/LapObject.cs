using UnityEngine;
using KartGame.KartSystems;

/// <summary>
/// This class inherits from TargetObject and represents a LapObject.
/// </summary>
public class LapObject : TargetObject
{
    [Header("LapObject")]
    [Tooltip("Is this the first/last lap object?")]
    public bool finishLap;

    [HideInInspector]
    public bool lapOverNextPass;

    void Start() {
        Register();
    }
    
    void OnEnable()
    {
        lapOverNextPass = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore initial spawn collisions during countdown/scene load
        if (Time.timeSinceLevelLoad < 1.5f)
            return;

        ArcadeKart kart = other.GetComponentInParent<ArcadeKart>();
        if (kart == null) return;

        // CRITICAL FIX: Ignore AI bots (KartAgent) - decoupled check across asmdef boundaries
        if (kart.GetComponent("KartAgent") != null)
            return;

        // Verify this is a kart we own (locally controlled) in multiplayer
        var netObj = kart.GetComponent<Unity.Netcode.NetworkObject>();
        if (netObj != null && netObj.IsSpawned && !netObj.IsOwner)
            return; // Ignore other remote players' karts trigger entry

        Debug.Log($"[StartFinishLine] Lap trigger entered by local player kart '{kart.name}'!");
        Objective.OnUnregisterPickup?.Invoke(this);
    }
}
