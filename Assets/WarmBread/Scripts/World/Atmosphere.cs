using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WarmBread
{
    public sealed class Atmosphere : MonoBehaviour
    {
        private const string VolumeKey = "WarmBread.Audio.MasterVolume";

        private GameSession session;
        private Light sun;
        private Camera viewCamera;
        private AudioSource ambience;
        private AudioSource radio;
        private AudioSource effects;
        private AudioClip ambienceClip;
        private AudioClip radioClip;
        private AudioClip bellClip;
        private Material rainMaterial;
        private GameObject rainObject;
        private ParticleSystem rainParticles;
        private WindZone windZone;
        private int station;
        private int appliedDay = -1;
        private float teaCooldown;
        private float weatherSunMultiplier = 1f;
        private float targetFogDensity = .012f;
        private Color daylightFog = new Color(.43f, .49f, .5f);
        private Color nightFog = new Color(.14f, .19f, .23f);

        public string RadioName => station == 0
            ? "ТИХАЯ ВОЛНА"
            : station == 1
                ? "ВЕЧЕРНИЙ ДВОР"
                : "ВЫКЛЮЧЕНО";

        public float Volume { get; private set; } = .55f;
        public string WeatherLabel => session != null && session.CurrentPlan != null
            ? session.CurrentPlan.WeatherLabel
            : "переменчивая погода";

        public void Initialize(GameSession game, Light daylight, Camera camera)
        {
            session = game;
            sun = daylight;
            viewCamera = camera;

            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .55f));
            AudioListener.volume = Volume;

            if (viewCamera != null)
            {
                viewCamera.backgroundColor = new Color(.43f, .49f, .5f);
                viewCamera.clearFlags = CameraClearFlags.SolidColor;
                viewCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            ambience.spatialBlend = 0f;
            ambience.volume = .15f;
            ambienceClip = Noise();
            ambience.clip = ambienceClip;
            ambience.Play();

            radio = gameObject.AddComponent<AudioSource>();
            radio.loop = true;
            radio.spatialBlend = 0f;
            radio.volume = .07f;
            radioClip = Melody(0);
            radio.clip = radioClip;
            radio.Play();

            effects = gameObject.AddComponent<AudioSource>();
            effects.spatialBlend = 0f;
            effects.volume = .12f;
            bellClip = Bell();

            EventBus.Chime += Chime;
            Rain();
            CreateWind();
            ApplyWeather(true);
        }

        private void OnDestroy()
        {
            EventBus.Chime -= Chime;
            AudioListener.pause = false;
            PlayerPrefs.Save();

            if (ambienceClip != null) Destroy(ambienceClip);
            if (radioClip != null) Destroy(radioClip);
            if (bellClip != null) Destroy(bellClip);
            if (rainMaterial != null) Destroy(rainMaterial);
            if (rainObject != null) Destroy(rainObject);
        }

        public void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value);
            AudioListener.volume = Volume;
            PlayerPrefs.SetFloat(VolumeKey, Volume);
        }

        public void NextStation()
        {
            if (radio == null) return;

            station = (station + 1) % 3;
            radio.Stop();

            if (radioClip != null)
            {
                Destroy(radioClip);
                radioClip = null;
            }

            if (station < 2)
            {
                radioClip = Melody(station);
                radio.clip = radioClip;
                radio.Play();
            }
            else
            {
                radio.clip = null;
            }

            EventBus.Say("Радио • " + RadioName);
        }

        public void Tea()
        {
            if (Time.time < teaCooldown)
            {
                EventBus.Say("Чай ещё слишком горячий. Посмотрим в окно.");
                return;
            }

            teaCooldown = Time.time + 90f;
            EventBus.Say("Чай с сахаром. За стеклом пахнет дождём. Всё будет хорошо.");
            Chime(.6f);
        }

        private void Chime(float pitch)
        {
            if (effects == null || bellClip == null) return;
            effects.pitch = Mathf.Clamp(pitch, .25f, 3f);
            effects.PlayOneShot(bellClip);
        }

        private void Update()
        {
            if (session == null) return;
            if (appliedDay != session.Day) ApplyWeather(false);

            var daylight = Mathf.Sin(Mathf.InverseLerp(5f, 22f, session.Hour) * Mathf.PI);
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(.065f, .75f * weatherSunMultiplier, daylight);
                sun.transform.rotation = Quaternion.Euler(10f + daylight * 24f, -35f, 0f);
            }

            var fogColor = Color.Lerp(nightFog, daylightFog, daylight);
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, Time.deltaTime * .35f);
            if (viewCamera != null) viewCamera.backgroundColor = fogColor;
        }

        private void ApplyWeather(bool force)
        {
            if (session == null || session.CurrentPlan == null) return;
            if (!force && appliedDay == session.Day) return;
            appliedDay = session.Day;

            var emissionRate = 0f;
            var startSpeed = 8f;
            var wind = .1f;
            weatherSunMultiplier = 1f;
            targetFogDensity = .01f;
            daylightFog = new Color(.43f, .49f, .5f);
            nightFog = new Color(.14f, .19f, .23f);

            switch (session.CurrentPlan.Weather)
            {
                case WeatherKind.Rain:
                    emissionRate = 1150f;
                    startSpeed = 12f;
                    wind = .7f;
                    weatherSunMultiplier = .55f;
                    targetFogDensity = .017f;
                    daylightFog = new Color(.35f, .42f, .45f);
                    if (ambience != null) ambience.volume = .22f;
                    break;
                case WeatherKind.Fog:
                    emissionRate = 85f;
                    startSpeed = 6f;
                    wind = .08f;
                    weatherSunMultiplier = .42f;
                    targetFogDensity = .029f;
                    daylightFog = new Color(.55f, .58f, .56f);
                    nightFog = new Color(.24f, .27f, .26f);
                    if (ambience != null) ambience.volume = .09f;
                    break;
                case WeatherKind.Clear:
                    emissionRate = 0f;
                    wind = .12f;
                    weatherSunMultiplier = 1.18f;
                    targetFogDensity = .0045f;
                    daylightFog = new Color(.52f, .62f, .67f);
                    if (ambience != null) ambience.volume = .065f;
                    break;
                case WeatherKind.Wind:
                    emissionRate = 220f;
                    startSpeed = 14f;
                    wind = 1.25f;
                    weatherSunMultiplier = .82f;
                    targetFogDensity = .009f;
                    daylightFog = new Color(.42f, .48f, .48f);
                    if (ambience != null) ambience.volume = .14f;
                    break;
                default:
                    emissionRate = 520f;
                    startSpeed = 9f;
                    wind = .35f;
                    weatherSunMultiplier = .82f;
                    targetFogDensity = .012f;
                    if (ambience != null) ambience.volume = .15f;
                    break;
            }

            if (rainParticles != null)
            {
                var main = rainParticles.main;
                main.startSpeed = startSpeed;
                main.startLifetime = Mathf.Lerp(1.45f, .9f, Mathf.InverseLerp(6f, 14f, startSpeed));
                main.startColor = session.CurrentPlan.Weather == WeatherKind.Fog
                    ? new Color(.72f, .76f, .73f, .13f)
                    : new Color(.6f, .72f, .75f, .34f);

                var emission = rainParticles.emission;
                emission.rateOverTime = emissionRate;
                if (emissionRate > 0f && !rainParticles.isPlaying) rainParticles.Play();
                if (emissionRate <= 0f && rainParticles.isPlaying) rainParticles.Stop();
            }

            if (windZone != null)
            {
                windZone.windMain = wind;
                windZone.windTurbulence = wind * .35f;
                windZone.windPulseMagnitude = wind * .25f;
                windZone.windPulseFrequency = .22f;
            }

            RenderSettings.fog = targetFogDensity > 0f;
            EventBus.Refresh();
        }

        private void CreateWind()
        {
            var windObject = new GameObject("Ветер между домами");
            windObject.transform.SetParent(transform, false);
            windObject.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            windZone = windObject.AddComponent<WindZone>();
            windZone.mode = WindZoneMode.Directional;
            windZone.radius = 50f;
        }

        private void Rain()
        {
            rainObject = new GameObject("Осадки за окном");
            rainObject.transform.SetParent(transform, false);
            rainObject.transform.position = new Vector3(0f, 9f, 9f);

            rainParticles = rainObject.AddComponent<ParticleSystem>();
            rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = rainParticles.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 9f;
            main.startSize = .012f;
            main.maxParticles = 2200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.6f, .72f, .75f, .32f);

            var shape = rainParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(42f, 14f, .1f);

            rainObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var emission = rainParticles.emission;
            emission.rateOverTime = 520f;

            var renderer = rainParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 6f;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                rainMaterial = new Material(shader)
                {
                    name = "WarmBread Rain",
                    hideFlags = HideFlags.DontSave
                };

                if (rainMaterial.HasProperty("_BaseColor"))
                {
                    rainMaterial.SetColor("_BaseColor", new Color(.5f, .65f, .7f, .3f));
                }
                else if (rainMaterial.HasProperty("_Color"))
                {
                    rainMaterial.SetColor("_Color", new Color(.5f, .65f, .7f, .3f));
                }

                renderer.sharedMaterial = rainMaterial;
            }

            rainParticles.Play();
        }

        private static AudioClip Noise()
        {
            const int rate = 22050;
            var data = new float[rate * 6];
            var random = new System.Random(421);
            var previous = 0f;

            for (var index = 0; index < data.Length; index++)
            {
                previous = Mathf.Lerp(previous, (float)random.NextDouble() * 2f - 1f, .17f);
                data[index] = previous * .36f;
            }

            var clip = AudioClip.Create("Дождь • синтез", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Melody(int variation)
        {
            const int rate = 22050;
            const float beat = .6f;

            int[] notes = variation == 0
                ? new[] { 57, 60, 64, 67, 64, 60, 55, 60, 59, 62, 65, 69, 65, 62, 55, 59 }
                : new[] { 52, 55, 59, 62, 59, 55, 50, 55, 48, 52, 55, 60, 55, 52, 47, 50 };

            var data = new float[(int)(rate * beat * notes.Length)];
            for (var index = 0; index < data.Length; index++)
            {
                var time = index / (float)rate;
                var note = (int)(time / beat) % notes.Length;
                var local = time % beat;
                var frequency = 440f * Mathf.Pow(2f, (notes[note] - 69) / 12f);
                var envelope = Mathf.Min(local * 35f, 1f) * Mathf.Exp(-local * 5f);
                data[index] =
                    (Mathf.Sin(2f * Mathf.PI * frequency * time) +
                     .18f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * time)) *
                    envelope *
                    .16f;
            }

            var clip = AudioClip.Create(
                "Оригинальная радиопетля " + variation,
                data.Length,
                1,
                rate,
                false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Bell()
        {
            const int rate = 22050;
            var data = new float[rate / 4];

            for (var index = 0; index < data.Length; index++)
            {
                var time = index / (float)rate;
                data[index] =
                    Mathf.Sin(time * 2f * Mathf.PI * 1100f) *
                    Mathf.Exp(-time * 24f) *
                    .4f;
            }

            var clip = AudioClip.Create("Монета", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
