using System;
using UnityEngine;

namespace WarmBread
{
    public sealed class InteractionTarget : MonoBehaviour
    {
        public string Label { get; private set; }
        private Action action;
        public void Configure(string label, Action callback) { Label = label; action = callback; }
        public void Use() => action?.Invoke();
    }
}
