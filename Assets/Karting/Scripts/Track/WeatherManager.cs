using System;
using UnityEngine;

namespace KartGame.Track
{
    public enum WeatherType
    {
        DryNormal,
        DryHeat,
        RainLightDrizzle,
        RainHeavyPour,
        SnowVisibleTrack,
        SnowFullSnow
    }

    [Serializable]
    public struct WeatherModifiers
    {
        [Tooltip("Multiplier applied to acceleration (1.0 = 100%, 0.7 = -30%, 1.1 = +10%)")]
        public float AccelerationMultiplier;

        [Tooltip("Multiplier applied to braking (1.0 = 100%, 0.7 = -30%)")]
        public float BrakingMultiplier;

        [Tooltip("Multiplier applied to steering responsiveness (1.0 = 100%, 0.7 = -30%, 1.1 = +10%)")]
        public float SteeringMultiplier;

        [Tooltip("Multiplier applied to tire grip/traction (1.0 = 100%, 0.75 = -25%)")]
        public float GripMultiplier;

        [Tooltip("Multiplier applied to top speed")]
        public float TopSpeedMultiplier;

        public static WeatherModifiers Default => new WeatherModifiers
        {
            AccelerationMultiplier = 1.0f,
            BrakingMultiplier = 1.0f,
            SteeringMultiplier = 1.0f,
            GripMultiplier = 1.0f,
            TopSpeedMultiplier = 1.0f
        };
    }

    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }

        [Header("Active Weather")]
        [SerializeField] private WeatherType currentWeather = WeatherType.DryNormal;

        [Header("Weather Presets")]
        [SerializeField] private WeatherModifiers dryNormalPreset = new WeatherModifiers
        {
            AccelerationMultiplier = 1.0f,
            BrakingMultiplier = 1.0f,
            SteeringMultiplier = 1.0f,
            GripMultiplier = 1.0f,
            TopSpeedMultiplier = 1.0f
        };

        [SerializeField] private WeatherModifiers dryHeatPreset = new WeatherModifiers
        {
            AccelerationMultiplier = 1.10f, // +10%
            BrakingMultiplier = 0.90f,      // -10%
            SteeringMultiplier = 1.10f,     // +10%
            GripMultiplier = 0.95f,
            TopSpeedMultiplier = 1.05f
        };

        [SerializeField] private WeatherModifiers rainLightDrizzlePreset = new WeatherModifiers
        {
            AccelerationMultiplier = 0.90f, // -10%
            BrakingMultiplier = 0.90f,      // -10%
            SteeringMultiplier = 0.90f,     // -10%
            GripMultiplier = 0.85f,
            TopSpeedMultiplier = 0.95f
        };

        [SerializeField] private WeatherModifiers rainHeavyPourPreset = new WeatherModifiers
        {
            AccelerationMultiplier = 0.70f, // -30%
            BrakingMultiplier = 0.70f,      // -30%
            SteeringMultiplier = 0.70f,     // -30%
            GripMultiplier = 0.75f,
            TopSpeedMultiplier = 0.90f
        };

        [SerializeField] private WeatherModifiers snowVisibleTrackPreset = new WeatherModifiers
        {
            AccelerationMultiplier = 0.80f, // -20%
            BrakingMultiplier = 0.80f,      // -20%
            SteeringMultiplier = 0.80f,     // -20%
            GripMultiplier = 0.70f,
            TopSpeedMultiplier = 0.90f
        };

        [SerializeField] private WeatherModifiers snowFullSnowPreset = new WeatherModifiers
        {
            AccelerationMultiplier = 0.60f, // -40%
            BrakingMultiplier = 0.60f,      // -40%
            SteeringMultiplier = 0.60f,     // -40%
            GripMultiplier = 0.60f,
            TopSpeedMultiplier = 0.85f
        };

        public static event Action<WeatherType, WeatherModifiers> OnWeatherChanged;

        public WeatherType CurrentWeather => currentWeather;
        public WeatherModifiers CurrentModifiers => GetModifiers(currentWeather);

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

        public void SetWeather(WeatherType newWeather)
        {
            currentWeather = newWeather;
            WeatherModifiers modifiers = GetModifiers(newWeather);
            Debug.Log($"[WeatherManager] Weather updated to {newWeather}");
            OnWeatherChanged?.Invoke(newWeather, modifiers);
        }

        public WeatherModifiers GetModifiers(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.DryNormal: return dryNormalPreset;
                case WeatherType.DryHeat: return dryHeatPreset;
                case WeatherType.RainLightDrizzle: return rainLightDrizzlePreset;
                case WeatherType.RainHeavyPour: return rainHeavyPourPreset;
                case WeatherType.SnowVisibleTrack: return snowVisibleTrackPreset;
                case WeatherType.SnowFullSnow: return snowFullSnowPreset;
                default: return WeatherModifiers.Default;
            }
        }
    }
}
