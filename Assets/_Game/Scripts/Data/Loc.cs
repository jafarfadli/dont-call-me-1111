using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    public enum Lang { En, Ko }

    /// <summary>
    /// The game's language. UI text is written in English in the code and looked up in the Korean
    /// table (LocKo.cs) when the game runs in Korean. Content (calls, the phone, the room, the papers)
    /// is built in both languages by the content builder; <see cref="DayCatalog.Load"/> picks the
    /// catalog for the current language. The choice is saved; the first run follows the system language.
    /// </summary>
    public static partial class Loc
    {
        const string Key = "DCM.Language";
        static Lang? current;
        static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>The player picked another language (the caller rebuilds what it shows).</summary>
        public static event Action Changed;

        public static Lang Current
        {
            get
            {
                if (current == null)
                {
                    int saved = PlayerPrefs.GetInt(Key, -1);
                    current = saved >= 0 ? (Lang)saved : Application.systemLanguage == SystemLanguage.Korean ? Lang.Ko : Lang.En;
                }
                return current.Value;
            }
            set
            {
                if (current == value)
                    return;
                current = value;
                PlayerPrefs.SetInt(Key, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static bool Ko => Current == Lang.Ko;

        /// <summary>Uses a language for this session only, without saving it (tools such as the UI tour).</summary>
        public static void Use(Lang lang) => current = lang;

        /// <summary>The text in the current language; the English text is the key.</summary>
        public static string T(string en)
        {
            if (string.IsNullOrEmpty(en) || !Ko)
                return en;
            if (KoText.TryGetValue(en, out string ko))
                return ko;
#if UNITY_EDITOR
            if (warned.Add(en))
                Debug.LogWarning($"[Loc] No Korean for \"{en}\"");
#endif
            return en;
        }

        /// <summary>A formatted text in the current language, e.g. F("Return to call · {0}", time).</summary>
        public static string F(string en, params object[] args) => string.Format(T(en), args);

        /// <summary>"Today" on the phone's lists.</summary>
        public static string Today => Ko ? "오늘" : "Today";

        public static string Yesterday => Ko ? "어제" : "Yesterday";

        /// <summary>The name of each language, written in that language.</summary>
        public static string Name(Lang lang) => lang == Lang.Ko ? "한국어" : "English";
    }
}
