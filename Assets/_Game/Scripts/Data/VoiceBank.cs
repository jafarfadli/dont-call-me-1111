using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>
    /// Every generated voice clip, looked up by voice and spoken text, so any line anywhere
    /// (conversation, pressure beat, question, official line) finds its audio without ids.
    /// Filled by Tools → Don't Call Me → Audio → Import Voices from the TTS manifest.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Voice Bank")]
    public class VoiceBank : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string voice;
            [TextArea(1, 4)] public string text;
            public AudioClip clip;
        }

        public List<Entry> entries = new List<Entry>();

        Dictionary<string, AudioClip> byKey;

        public static VoiceBank Current { get; set; }

        public static string Key(string voice, string text) => (voice ?? "").Trim() + "|" + Normalize(text);

        /// <summary>Whitespace-insensitive, so line breaks in the inspector do not break the lookup.</summary>
        public static string Normalize(string text) => Regex.Replace((text ?? "").Trim(), @"\s+", " ");

        public AudioClip Find(string voice, string text)
        {
            if (string.IsNullOrEmpty(voice) || string.IsNullOrEmpty(text))
                return null;
            if (byKey == null || byKey.Count != entries.Count)
            {
                byKey = new Dictionary<string, AudioClip>();
                foreach (var e in entries)
                    if (e.clip != null)
                        byKey[Key(e.voice, e.text)] = e.clip;
            }
            return byKey.TryGetValue(Key(voice, text), out var clip) ? clip : null;
        }

        public static AudioClip Lookup(string voice, string text) => Current != null ? Current.Find(voice, text) : null;
    }
}
