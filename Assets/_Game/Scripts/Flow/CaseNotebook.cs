using System;
using System.Collections.Generic;
using DontCallMe.Data;

namespace DontCallMe.Flow
{
    public enum Stamp { None, True, False }

    /// <summary>Something the player circled, as the notebook files it: what it said and where it was.</summary>
    public class FiledMark
    {
        public MarkDef def;
        public string text;
        public string where;
    }

    /// <summary>
    /// One numbered line of the notebook: a claim the caller made, or, on a day the player writes
    /// the notebook (<see cref="NotebookHelp.Write"/>), a caller's line the player pinned, which may
    /// claim nothing that can be checked (<see cref="claim"/> is then null).
    /// </summary>
    public class NoteEntry
    {
        public ClaimDef claim;
        /// <summary>The caller's line as pinned (empty for a claim written in by the notebook).</summary>
        public string line;
        /// <summary>As the notebook shows it.</summary>
        public string text;
    }

    /// <summary>
    /// Today's case notebook: the claims the caller has made so far, what the player circled under
    /// each, and the stamp the player gave each claim. Panels and apps ask it what can be circled
    /// (<see cref="Find"/>); circling raises <see cref="ClueEvent.Marked"/>, which is how clues are
    /// found. A claim can only be stamped once something is filed under it, so it cannot be guessed.
    /// On a <see cref="NotebookHelp.Write"/> day claims are not written in as they are said: the
    /// player pins up to <see cref="MaxEntries"/> of the caller's lines (<see cref="TogglePin"/>),
    /// and only a pinned line that makes a claim holds evidence.
    /// </summary>
    public class CaseNotebook
    {
        public const int MaxEntries = 3;

        public readonly ConversationData conversation;
        public readonly NotebookHelp help;

        readonly List<MarkDef> marks;
        readonly List<ClaimDef> heard = new List<ClaimDef>();
        readonly List<NoteEntry> entries = new List<NoteEntry>();
        readonly List<FiledMark> filed = new List<FiledMark>();
        readonly Dictionary<string, Stamp> stamps = new Dictionary<string, Stamp>();

        /// <summary>Anything changed: a claim heard or pinned, a mark circled or dropped, a stamp.</summary>
        public event Action Changed;
        public event Action<NoteEntry> EntryAdded;
        public event Action<FiledMark> MarkFiled;
        /// <summary>A pin was refused: the notebook already has <see cref="MaxEntries"/> lines.</summary>
        public event Action Full;

        /// <summary>The claims in the notebook (heard, or pinned on a Write day).</summary>
        public IReadOnlyList<ClaimDef> Heard => heard;
        public IReadOnlyList<NoteEntry> Entries => entries;
        public IReadOnlyList<FiledMark> Filed => filed;

        /// <summary>The player writes the claims down (pins the caller's lines) instead of the notebook.</summary>
        public bool WritesOwn => help == NotebookHelp.Write;

        public CaseNotebook(ConversationData conversation, List<MarkDef> marks, NotebookHelp help)
        {
            this.conversation = conversation;
            this.marks = marks ?? new List<MarkDef>();
            this.help = help;
        }

        // ---------------------------------------------------------------- claims

        /// <summary>The caller has said this claim: it goes into the notebook (once), unless the player writes it.</summary>
        public void Hear(string claimId)
        {
            if (WritesOwn)
                return;
            var claim = conversation != null ? conversation.FindClaim(claimId) : null;
            if (claim == null || heard.Contains(claim))
                return;
            heard.Add(claim);
            var entry = new NoteEntry { claim = claim, line = "", text = claim.text };
            entries.Add(entry);
            EntryAdded?.Invoke(entry);
            Changed?.Invoke();
        }

        public bool IsHeard(string claimId) => heard.Exists(c => c.id == claimId);

        /// <summary>The claim's number in the notebook (1, 2, 3), or 0 when it is not in it.</summary>
        public int NumberOf(string claimId) => entries.FindIndex(e => e.claim != null && e.claim.id == claimId) + 1;

        /// <summary>The notebook line of a claim, or null.</summary>
        public NoteEntry EntryOf(string claimId) => entries.Find(e => e.claim != null && e.claim.id == claimId);

        public bool IsPinned(string line) => !string.IsNullOrEmpty(line) && entries.Exists(e => e.line == line);

        /// <summary>
        /// On a Write day: pins a caller's line (<paramref name="claimId"/> is the claim it makes, or
        /// empty), or takes it out when it is pinned already. Taking a claim out drops its stamp; what
        /// was circled for it stays, under "Other notes".
        /// </summary>
        /// <returns>False when nothing changed (not a Write day, or the notebook is full).</returns>
        public bool TogglePin(string line, string claimId)
        {
            if (!WritesOwn || string.IsNullOrEmpty(line))
                return false;
            int i = entries.FindIndex(e => e.line == line);
            if (i >= 0)
            {
                var old = entries[i];
                entries.RemoveAt(i);
                PlaytestLog.Log("unpin", old.claim?.id ?? "", line);
                if (old.claim != null && !entries.Exists(e => e.claim == old.claim))
                {
                    heard.Remove(old.claim);
                    stamps.Remove(old.claim.id);
                }
                Changed?.Invoke();
                return true;
            }
            if (entries.Count >= MaxEntries)
            {
                PlaytestLog.Log("pin_full", claimId ?? "", line);
                Full?.Invoke();
                return false;
            }
            var claim = conversation != null && !string.IsNullOrEmpty(claimId) ? conversation.FindClaim(claimId) : null;
            var entry = new NoteEntry { claim = claim, line = line, text = "\"" + line + "\"" };
            entries.Add(entry);
            PlaytestLog.Log("pin", claim?.id ?? "", line);
            if (claim != null && !heard.Contains(claim))
                heard.Add(claim);
            EntryAdded?.Invoke(entry);
            Changed?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- marks

        /// <summary>Today's mark for an item, or null when the item cannot be circled.</summary>
        public MarkDef Find(MarkPlace place, string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            foreach (var m in marks)
                if (m.place == place && Matches(place, m.key, key))
                    return m;
            return null;
        }

        static bool Matches(MarkPlace place, string want, string got)
        {
            if (place == MarkPlace.Computer || place == MarkPlace.Contacts)
                return FactText.SameNumber(want, got);
            return string.Equals(want?.Trim(), got?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public bool IsMarked(string markId) => filed.Exists(f => f.def.id == markId);

        /// <summary>Circles the item, or lets go of it when it is circled already.</summary>
        public void Toggle(MarkDef mark, string text, string where)
        {
            if (mark == null)
                return;
            int i = filed.FindIndex(f => f.def.id == mark.id);
            if (i >= 0)
            {
                filed.RemoveAt(i);
                PlaytestLog.Circle(mark, false);
                // Nothing left to stand on: the stamp goes too.
                foreach (string claimId in mark.Claims)
                    if (!CanStamp(claimId))
                        stamps.Remove(claimId);
                Changed?.Invoke();
                return;
            }
            var f = new FiledMark { def = mark, text = text, where = where };
            filed.Add(f);
            PlaytestLog.Circle(mark, true);
            ClueEvents.Raise(ClueEvent.Marked, mark.id);
            MarkFiled?.Invoke(f);
            Changed?.Invoke();
        }

        /// <summary>What is filed under a claim, in the order it was circled. One item can settle two claims and sit under both.</summary>
        public List<FiledMark> FiledUnder(string claimId) => filed.FindAll(f => f.def.Under(claimId));

        /// <summary>Circled things none of whose claims is in the notebook (not said yet, or not pinned).</summary>
        public List<FiledMark> Loose() => filed.FindAll(f => !HeardAny(f.def));

        /// <summary>The notebook line an item goes under: the first of its claims that is in the notebook.</summary>
        public NoteEntry EntryFor(MarkDef mark)
        {
            foreach (string claimId in mark.Claims)
            {
                var entry = EntryOf(claimId);
                if (entry != null)
                    return entry;
            }
            return null;
        }

        bool HeardAny(MarkDef mark)
        {
            foreach (string claimId in mark.Claims)
                if (IsHeard(claimId))
                    return true;
            return false;
        }

        // ---------------------------------------------------------------- stamps

        public bool CanStamp(string claimId) => IsHeard(claimId) && filed.Exists(f => f.def.Under(claimId));

        public Stamp StampOf(string claimId) => stamps.TryGetValue(claimId, out var s) ? s : Stamp.None;

        /// <summary>Stamps the claim; the same stamp again takes it off.</summary>
        public void SetStamp(string claimId, Stamp stamp)
        {
            if (!CanStamp(claimId))
                return;
            if (stamp == Stamp.None || StampOf(claimId) == stamp)
                stamps.Remove(claimId);
            else
                stamps[claimId] = stamp;
            PlaytestLog.Log("stamp", claimId, StampOf(claimId).ToString());
            Changed?.Invoke();
        }

        /// <summary>The claims stamped false so far.</summary>
        public int FalseCount()
        {
            int n = 0;
            foreach (var s in stamps.Values)
                if (s == Stamp.False)
                    n++;
            return n;
        }

        // ---------------------------------------------------------------- saving

        /// <summary>For the day's record: "claim:T" or "claim:F" per stamped claim.</summary>
        public List<string> SavedStamps()
        {
            var list = new List<string>();
            foreach (var claim in heard)
            {
                var s = StampOf(claim.id);
                if (s != Stamp.None)
                    list.Add(claim.id + (s == Stamp.True ? ":T" : ":F"));
            }
            return list;
        }

        public List<string> SavedMarks() => filed.ConvertAll(f => f.def.id);

        /// <summary>For the day's record: the claims that were in the notebook at the end.</summary>
        public List<string> SavedNoted() => heard.ConvertAll(c => c.id);

        /// <summary>Reads a saved stamp ("claim:T").</summary>
        public static Stamp ParseStamp(IEnumerable<string> saved, string claimId)
        {
            if (saved == null)
                return Stamp.None;
            foreach (string s in saved)
                if (s.StartsWith(claimId + ":", StringComparison.Ordinal))
                    return s.EndsWith(":T", StringComparison.Ordinal) ? Stamp.True : Stamp.False;
            return Stamp.None;
        }
    }
}
