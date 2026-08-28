using UnityEngine;

namespace KartGame.KartSystems
{
    [CreateAssetMenu(fileName = "NewKartData", menuName = "Krash Kart/Kart Data", order = 1)]
    public class KartDataSO : ScriptableObject
    {
        [Header("Kart Identity")]
        public string kartName = "Standard Kart";
        [TextArea(2, 4)]
        public string description = "A balanced kart with standard handling and acceleration.";

        [Header("Core Attributes")]
        [Tooltip("Base movement stats defining handling, speed, acceleration, braking, and mass.")]
        public ArcadeKart.Stats stats = new ArcadeKart.Stats
        {
            TopSpeed = 16f,
            Acceleration = 5f,
            AccelerationCurve = 0.7f,
            Braking = 10f,
            ReverseAcceleration = 4f,
            ReverseSpeed = 8f,
            Steer = 5.5f,
            CoastingDrag = 1.5f,
            Grip = 0.80f,
            AddedGravity = 12f,
            Weight = 1000f
        };

        [Header("Drift Configuration")]
        [Range(0.01f, 1.0f)] public float driftGrip = 0.50f;
        [Range(0.0f, 10.0f)] public float driftAdditionalSteer = 3.5f;
        [Range(0.5f, 0.99f)] public float driftSpeedFraction = 0.85f;
    }
}
