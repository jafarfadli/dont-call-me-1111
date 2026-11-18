using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>
    /// The days of a run, in order, in one language. Lives in a Resources folder ("DayCatalog" in
    /// English, "DayCatalog_ko" in Korean) so the title, room and morning scenes find it without
    /// scene references.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Day Catalog")]
    public class DayCatalog : ScriptableObject
    {
        public const string ResourceName = "DayCatalog";

        public List<DayData> days = new List<DayData>();

        [Tooltip("The trick cards: one per scam trick, won by stamping a claim that uses it FALSE.")]
        public List<TrickDef> tricks = new List<TrickDef>();

        public TrickDef Trick(string id) => tricks.Find(t => t.id == id);

        static DayCatalog cached;
        static Lang cachedLang;

        /// <summary>The catalog in the current language (English when there is none).</summary>
        public static DayCatalog Load()
        {
            var lang = Loc.Current;
            if (cached == null || cachedLang != lang)
            {
                cached = (lang == Lang.Ko ? Resources.Load<DayCatalog>(ResourceName + "_ko") : null) ?? Resources.Load<DayCatalog>(ResourceName);
                cachedLang = lang;
            }
            return cached;
        }

        public int Count => days.Count;

        public DayData Get(int day) => days.Find(d => d != null && d.day == day);
    }

    /// <summary>
    /// A trick card: one way scammers lie (the borrowed name, the new account...). The player wins
    /// it by stamping a false claim that uses it FALSE, and keeps it across weeks.
    /// </summary>
    [System.Serializable]
    public class TrickDef
    {
        public string id;
        public string name;
        [Tooltip("How the trick works, in a sentence.")]
        [TextArea(2, 3)] public string how;
        [Tooltip("How to see through it, in a sentence.")]
        [TextArea(2, 3)] public string tell;
    }
}
