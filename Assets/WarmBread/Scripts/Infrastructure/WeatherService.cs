using System;
using UnityEngine;

namespace WarmBread
{
    public sealed class WeatherService : MonoBehaviour
    {
        [Serializable]
        private struct WeatherPreset
        {
            public string id;
            public Color ambientColor;
            [Range(0f, 1f)] public float fogDensity;
            [Range(0f, 2f)] public float lightIntensity;
        }

        [SerializeField] private WeatherPreset[] presets;
        [SerializeField] private string defaultWeather = "overcast";
        [SerializeField] private Light sun;
        [SerializeField] private ParticleSystem precipitation;
        [SerializeField] private float transitionSeconds = 3f;

        public string CurrentWeatherId { get; private set; }
        public event Action<string> Changed;

        private void Awake() { SetWeather(defaultWeather, true); }

        public void SetWeather(string weatherId, bool instant = false)
        {
            if (string.IsNullOrEmpty(weatherId)) return;
            var preset = FindPreset(weatherId);
            if (preset.id == null) preset = FindPreset(defaultWeather);
            if (preset.id == null) return;

            CurrentWeatherId = preset.id;
            RenderSettings.ambientLight = preset.ambientColor;
            RenderSettings.fog = preset.fogDensity > 0f;
            RenderSettings.fogDensity = preset.fogDensity;
            if (sun != null) sun.intensity = preset.lightIntensity;
            if (precipitation != null) precipitation.gameObject.SetActive(preset.fogDensity > 0.015f);
            GameEventBus.Publish(new WeatherChanged(CurrentWeatherId));
            Changed?.Invoke(CurrentWeatherId);
        }

        private WeatherPreset FindPreset(string id)
        {
            if (presets != null)
                foreach (var preset in presets)
                    if (preset.id == id) return preset;
            return default;
        }
    }
}
