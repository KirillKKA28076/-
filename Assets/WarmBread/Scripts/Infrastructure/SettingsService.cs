using UnityEngine;

namespace WarmBread
{
    public sealed class SettingsService : MonoBehaviour
    {
        private const string Prefix = "WarmBread.Settings.";
        private const string MasterVolumeKey = Prefix + "MasterVolume";
        private const string MouseSensitivityKey = Prefix + "MouseSensitivity";
        private const string SubtitlesKey = Prefix + "Subtitles";

        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0.1f, 5f)] private float mouseSensitivity = 1f;
        [SerializeField] private bool subtitles = true;

        public float MasterVolume => masterVolume;
        public float MouseSensitivity => mouseSensitivity;
        public bool Subtitles => subtitles;

        private void Awake()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
            mouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, mouseSensitivity);
            subtitles = PlayerPrefs.GetInt(SubtitlesKey, subtitles ? 1 : 0) != 0;
            Apply();
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            Apply();
            Save();
        }

        public void SetMouseSensitivity(float value)
        {
            mouseSensitivity = Mathf.Clamp(value, 0.1f, 5f);
            Save();
        }

        public void SetSubtitles(bool value)
        {
            subtitles = value;
            Save();
        }

        public void Apply()
        {
            AudioListener.volume = masterVolume;
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
            PlayerPrefs.SetInt(SubtitlesKey, subtitles ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            masterVolume = 1f;
            mouseSensitivity = 1f;
            subtitles = true;
            Save();
            Apply();
        }
    }
}
