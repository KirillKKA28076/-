using UnityEngine;

namespace WarmBread
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldClock worldClock;
        [SerializeField] private EconomyLedger economyLedger;
        [SerializeField] private SettingsService settingsService;

        private void Awake()
        {
            if (worldClock == null) worldClock = FindObjectOfType<WorldClock>();
            if (economyLedger == null) economyLedger = FindObjectOfType<EconomyLedger>();
            if (settingsService == null) settingsService = FindObjectOfType<SettingsService>();

            if (settingsService != null) settingsService.Apply();
            if (worldClock != null && !worldClock.IsRunning) worldClock.StartDay();
        }

        private void OnDestroy()
        {
            GameEventBus.Clear();
        }
    }
}
