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
}
