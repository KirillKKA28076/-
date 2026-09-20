using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarmBread
{
    [Serializable]
    public sealed class AchievementState
    {
        public string id;
        public bool unlocked;
        public int progress;
    }

    public sealed class AchievementService : MonoBehaviour
    {
        private readonly Dictionary<string, AchievementState> states = new Dictionary<string, AchievementState>();
        public event Action<string> Unlocked;

        public bool Register(string id)
        {
            if (string.IsNullOrEmpty(id) || states.ContainsKey(id)) return false;
            states.Add(id, new AchievementState { id = id });
            return true;
        }

        public bool AddProgress(string id, int amount, int required)
        {
            if (!states.TryGetValue(id, out var state) || state.unlocked || amount <= 0) return false;
            state.progress = Mathf.Clamp(state.progress + amount, 0, Mathf.Max(1, required));
            if (state.progress >= Mathf.Max(1, required))
            {
                state.unlocked = true;
                Unlocked?.Invoke(id);
            }
            return true;
        }

        public bool IsUnlocked(string id)
        {
            return states.TryGetValue(id, out var state) && state.unlocked;
        }
    }
}
