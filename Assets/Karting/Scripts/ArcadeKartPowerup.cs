using KartGame.KartSystems;
using UnityEngine;
using UnityEngine.Events;

public class ArcadeKartPowerup : MonoBehaviour
{
    [Header("Boost Settings")]
    public ArcadeKart.StatPowerups boostStats = new ArcadeKart.StatPowerups
    {
        PowerUpID = "SpeedPad",
        MaxTime = 2.5f,
        ElapsedTime = 0f,
        modifiers = new ArcadeKart.Stats
        {
            TopSpeed = 4f,
            Acceleration = 3f
        }
    };

    public float cooldown = 5f;
    public bool disableGameObjectWhenActivated;
    public UnityEvent onPowerupActivated;
    public UnityEvent onPowerupFinishCooldown;

    public bool isCoolingDown { get; private set; }
    public float lastActivatedTimestamp { get; private set; } = -9999f;

    private void Update()
    {
        if (isCoolingDown && Time.time - lastActivatedTimestamp > cooldown)
        {
            isCoolingDown = false;
            onPowerupFinishCooldown.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCoolingDown) return;

        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            var kart = rb.GetComponent<ArcadeKart>();
            if (kart != null)
            {
                lastActivatedTimestamp = Time.time;
                kart.AddPowerup(new ArcadeKart.StatPowerups
                {
                    PowerUpID = boostStats.PowerUpID,
                    MaxTime = boostStats.MaxTime,
                    ElapsedTime = 0f,
                    modifiers = boostStats.modifiers
                });

                onPowerupActivated.Invoke();
                isCoolingDown = true;

                if (disableGameObjectWhenActivated)
                    gameObject.SetActive(false);
            }
        }
    }
}

