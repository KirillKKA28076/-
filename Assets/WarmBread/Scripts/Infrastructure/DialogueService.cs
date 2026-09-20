using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarmBread
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speakerId;
        [TextArea] public string text;
        public float duration = 3f;
    }

    public sealed class DialogueService : MonoBehaviour
    {
        private readonly Queue<DialogueLine> queue = new Queue<DialogueLine>();
        public DialogueLine Current { get; private set; }
        public event Action<DialogueLine> LineStarted;
        public event Action LineFinished;

        [SerializeField] private bool playOnUpdate = true;
        private float remaining;

        public void Enqueue(DialogueLine line)
        {
            if (line == null || string.IsNullOrEmpty(line.text)) return;
            queue.Enqueue(line);
            if (Current == null && playOnUpdate) BeginNext();
        }

        private void Update()
        {
            if (Current == null || !playOnUpdate) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                LineFinished?.Invoke();
                Current = null;
                BeginNext();
            }
        }

        public void Skip()
        {
            if (Current == null) return;
            LineFinished?.Invoke();
            Current = null;
            BeginNext();
        }

        private void BeginNext()
        {
            if (queue.Count == 0) return;
            Current = queue.Dequeue();
            remaining = Mathf.Max(0.1f, Current.duration);
            LineStarted?.Invoke(Current);
        }

        public void Clear()
        {
            queue.Clear();
            Current = null;
            remaining = 0f;
        }
    }
}
