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
        private int station;
        private float teaCooldown;

        public string RadioName => station == 0
            ? "ТИХАЯ ВОЛНА"
            : station == 1
                ? "ВЕЧЕРНИЙ ДВОР"
                : "ВЫКЛЮЧЕНО";

        public float Volume { get; private set; } = .55f;

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
            ambience.volume = .15f;
            ambienceClip = Noise();
            ambience.clip = ambienceClip;
            ambience.Play();

            radio = gameObject.AddComponent<AudioSource>();
            radio.loop = true;
            radio.volume = .07f;
            radioClip = Melody(0);
            radio.clip = radioClip;
            radio.Play();

            effects = gameObject.AddComponent<AudioSource>();
            effects.volume = .12f;
            bellClip = Bell();

            EventBus.Chime += Chime;
            Rain();
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

            var daylight = Mathf.Sin(Mathf.InverseLerp(5f, 22f, session.Hour) * Mathf.PI);
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(.08f, .75f, daylight);
                sun.transform.rotation = Quaternion.Euler(10f + daylight * 24f, -35f, 0f);
            }

            RenderSettings.fogColor = Color.Lerp(
                new Color(.14f, .19f, .23f),
                new Color(.43f, .49f, .5f),
                daylight);

            if (viewCamera != null) viewCamera.backgroundColor = RenderSettings.fogColor;
        }

        private void Rain()
        {
            rainObject = new GameObject("Дождь за окном");
            rainObject.transform.SetParent(transform);
            rainObject.transform.position = new Vector3(0f, 9f, 9f);

            var particles = rainObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 9f;
            main.startSize = .012f;
            main.maxParticles = 1600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.6f, .72f, .75f, .32f);

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 12f, .1f);

            rainObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var emission = particles.emission;
            emission.rateOverTime = 750f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
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

            particles.Play();
        }

        // Все звуки синтезированы для проекта. Записей реальных радиостанций нет.
        private static AudioClip Noise()
        {
            const int rate = 22050;
            var data = new float[rate * 6];
            var random = new System.Random(421);
            var previous = 0f;

            for (var i = 0; i < data.Length; i++)
            {
                previous = Mathf.Lerp(previous, (float)random.NextDouble() * 2f - 1f, .17f);
                data[i] = previous * .36f;
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
            for (var i = 0; i < data.Length; i++)
            {
                var time = i / (float)rate;
                var index = (int)(time / beat) % notes.Length;
                var local = time % beat;
                var frequency = 440f * Mathf.Pow(2f, (notes[index] - 69) / 12f);
                var envelope = Mathf.Min(local * 35f, 1f) * Mathf.Exp(-local * 5f);
                data[i] =
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

            for (var i = 0; i < data.Length; i++)
            {
                var time = i / (float)rate;
                data[i] =
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
