using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>
    /// The days of a run, in order. Lives in a Resources folder so the title, room and morning
    /// scenes all find it without scene references.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Day Catalog")]
    public class DayCatalog : ScriptableObject
    {
        public const string ResourceName = "DayCatalog";

        public List<DayData> days = new List<DayData>();

        static DayCatalog cached;

        public static DayCatalog Load()
        {
            if (cached == null)
                cached = Resources.Load<DayCatalog>(ResourceName);
            return cached;
        }

        public int Count => days.Count;

        public DayData Get(int day) => days.Find(d => d != null && d.day == day);
    }
}
