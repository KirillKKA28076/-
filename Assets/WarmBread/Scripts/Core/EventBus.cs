using System;
using UnityEngine;

namespace WarmBread
{
    public static class EventBus
    {
        public static event Action Changed;
        public static event Action<string> Message;
        public static event Action<float> Chime;
        public static void Refresh() => Changed?.Invoke();
        public static void Say(string text) => Message?.Invoke(text);
        public static void Sound(float pitch) => Chime?.Invoke(pitch);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Changed = null; Message = null; Chime = null; }
    }
}
