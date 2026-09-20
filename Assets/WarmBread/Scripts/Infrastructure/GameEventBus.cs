using System;
using System.Collections.Generic;

namespace WarmBread
{
    public interface IGameEvent { }

    public static class GameEventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (Handlers.TryGetValue(type, out var current))
                Handlers[type] = Delegate.Combine(current, handler);
            else
                Handlers[type] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var current)) return;
            var next = Delegate.Remove(current, handler);
            if (next == null) Handlers.Remove(type);
            else Handlers[type] = next;
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (!Handlers.TryGetValue(typeof(T), out var current)) return;
            foreach (Action<T> handler in current.GetInvocationList())
            {
                try { handler(gameEvent); }
                catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
            }
        }

        public static void Clear() { Handlers.Clear(); }
    }

    public readonly struct DayStarted : IGameEvent
    {
        public readonly int Day;
        public DayStarted(int day) { Day = day; }
    }

    public readonly struct MoneyChanged : IGameEvent
    {
        public readonly int Balance;
        public MoneyChanged(int balance) { Balance = balance; }
    }

    public readonly struct WeatherChanged : IGameEvent
    {
        public readonly string WeatherId;
        public WeatherChanged(string weatherId) { WeatherId = weatherId; }
    }
}
