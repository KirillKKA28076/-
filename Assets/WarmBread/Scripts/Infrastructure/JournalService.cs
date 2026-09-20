using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarmBread
{
    [Serializable]
    public sealed class JournalEntry
    {
        public string id;
        public int day;
        public string title;
        [TextArea] public string text;
    }

    public sealed class JournalService : MonoBehaviour
    {
        private readonly List<JournalEntry> entries = new List<JournalEntry>();
        private readonly HashSet<string> ids = new HashSet<string>();
        public IReadOnlyList<JournalEntry> Entries => entries;

        public bool Add(string id, int day, string title, string text)
        {
            if (string.IsNullOrEmpty(id) || ids.Contains(id)) return false;
            ids.Add(id);
            entries.Add(new JournalEntry { id = id, day = Mathf.Max(1, day), title = title ?? "", text = text ?? "" });
            return true;
        }

        public bool Contains(string id) { return !string.IsNullOrEmpty(id) && ids.Contains(id); }

        public void Clear()
        {
            ids.Clear();
            entries.Clear();
        }
    }
}
