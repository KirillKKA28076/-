using System;
using UnityEngine;

namespace WarmBread
{
    public sealed class WorldClock : MonoBehaviour
    {
        [SerializeField] private float realSecondsPerGameHour = 60f;
        [SerializeField] private float startHour = 6f;
        [SerializeField] private float closeHour = 20f;
        [SerializeField] private bool runOnEnable = true;

        public int Day { get; private set; } = 1;
        public float Hour { get; private set; }
        public bool IsRunning { get; private set; }
        public float NormalizedDay => Mathf.InverseLerp(startHour, closeHour, Hour);

        public event Action<int, float> TimeChanged;
        public event Action<int> DayClosed;

        private void Awake()
        {
            Hour = Mathf.Clamp(startHour, 0f, 24f);
            IsRunning = runOnEnable;
        }

        private void Update()
        {
            if (!IsRunning || realSecondsPerGameHour <= 0f || Time.timeScale <= 0f) return;
            Hour += Time.deltaTime / realSecondsPerGameHour;
            if (Hour >= closeHour)
            {
                Hour = closeHour;
                IsRunning = false;
                DayClosed?.Invoke(Day);
            }
            TimeChanged?.Invoke(Day, Hour);
        }

        public void StartDay(int day = 1, float hour = -1f)
        {
            Day = Mathf.Max(1, day);
            Hour = hour < 0f ? startHour : Mathf.Clamp(hour, 0f, closeHour);
            IsRunning = true;
            GameEventBus.Publish(new DayStarted(Day));
        }

        public void NextDay()
        {
            StartDay(Day + 1, startHour);
        }

        public void Pause() { IsRunning = false; }
        public void Resume() { IsRunning = true; }
    }
}
