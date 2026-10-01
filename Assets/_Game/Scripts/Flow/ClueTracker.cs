using System;
using System.Collections.Generic;
using DontCallMe.Data;

namespace DontCallMe.Flow
{
    /// <summary>
    /// What the player looked at. Panels and apps report here; the day's <see cref="ClueTracker"/>
    /// decides which of those count as clues.
    /// </summary>
    public static class ClueEvents
    {
        public static event Action<ClueEvent, string> Happened;

        public static void Raise(ClueEvent kind, string target = "") => Happened?.Invoke(kind, target ?? "");
    }

    /// <summary>Marks a day's clues as found when the matching evidence is seen.</summary>
    public class ClueTracker : IDisposable
    {
        readonly List<ClueDef> clues;
        readonly HashSet<string> found = new HashSet<string>();

        public event Action<ClueDef> Found;

        public IReadOnlyCollection<string> FoundIds => found;
        public int Total => clues.Count;
        public int Count => found.Count;

        public ClueTracker(List<ClueDef> clues)
        {
            this.clues = clues ?? new List<ClueDef>();
            ClueEvents.Happened += OnEvent;
        }

        public void Dispose() => ClueEvents.Happened -= OnEvent;

        public bool IsFound(string id) => found.Contains(id);

        void OnEvent(ClueEvent kind, string target)
        {
            foreach (var clue in clues)
            {
                if (found.Contains(clue.id))
                    continue;
                foreach (var w in clue.when)
                {
                    if (w.kind != kind || !Matches(w.target, target))
                        continue;
                    found.Add(clue.id);
                    Found?.Invoke(clue);
                    break;
                }
            }
        }

        /// <summary>Empty targets match anything; numbers match whatever their dashes and spaces.</summary>
        static bool Matches(string want, string got)
        {
            if (string.IsNullOrEmpty(want))
                return true;
            if (string.Equals(want.Trim(), got?.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
            string a = FactText.Digits(want), b = FactText.Digits(got);
            return a.Length >= 3 && a == b;
        }
    }
}
