using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>How one finished day went, kept for the rest of the run (and the week's summary).</summary>
    [Serializable]
    public class DayRecord
    {
        public int day;
        [Tooltip("The variant played (DayVariant.id), e.g. \"scam\", \"legit\" or \"sale_scam\".")]
        public string variant;
        public bool scam;
        public Outcome outcome;
        public long moneyDelta;
        public long savingsAfter;
        public List<string> clues = new List<string>();
        public int cluesTotal;
        [Tooltip("The notebook's stamps, \"claim:T\" or \"claim:F\".")]
        public List<string> stamps = new List<string>();
        [Tooltip("The marks the player circled (MarkDef ids).")]
        public List<string> marks = new List<string>();
        [Tooltip("The claims in the notebook at the end (ClaimDef ids).")]
        public List<string> noted = new List<string>();
        public string decidedAt;
        public int minutesTaken;
        public int callSeconds;
        public string callerNumber;
        public string callTime;
        [Tooltip("Set when money left the account on the call.")]
        public string transferTo;
        public string transferBank;
        public string transferAccount;

        /// <summary>Refusing a scam and going along with a legit caller are right; running out of time never is.</summary>
        public bool Right => outcome == Outcome.Verify || (scam ? outcome == Outcome.Refuse : outcome == Outcome.GoAlong);
    }

    /// <summary>A trick card won: which trick, and the day and case where its false claim was caught first.</summary>
    [Serializable]
    public class CaughtTrick
    {
        public string id;
        public int day;
        public string scenario;
    }

    /// <summary>
    /// The run in progress, saved between sessions: which days are done and how they went. The
    /// title screen continues from here and each day reads the earlier ones for its echoes. It also
    /// remembers, across weeks (PlayerPrefs "DCM.Played"), how often each case has been played, so a
    /// day can bring the case the player has seen least, which of its truths were seen, the trick
    /// cards won, and each day's progress: how often it was finished and called right, and its last
    /// record. A day finished once opens the next one for good (<see cref="Unlocked"/>), and the
    /// day select can start any open day (<see cref="FillBefore"/>).
    /// </summary>
    public static class GameRun
    {
        const string Key = "DCM.Run";
        const string PlayedKey = "DCM.Played";
        const string ForceKey = "DCM.ForceVariant";
        const string DemoKey = "DCM.Demo";

        [Serializable]
        class RunState
        {
            public List<DayRecord> days = new List<DayRecord>();
        }

        [Serializable]
        class PlayedCase
        {
            public int day;
            public string scenario;
            public int times;
            // Which truths of the case have been played to the end.
            public bool sawScam;
            public bool sawLegit;
        }

        [Serializable]
        class DayProgress
        {
            public int day;
            // Times played to the end, and times the call was right.
            public int finished;
            public int right;
            // The last finished record, so a later day can be started in another week.
            public DayRecord last;
        }

        [Serializable]
        class PlayedState
        {
            public List<PlayedCase> cases = new List<PlayedCase>();
            public List<CaughtTrick> tricks = new List<CaughtTrick>();
            public List<DayProgress> days = new List<DayProgress>();
        }

        static RunState state;
        static PlayedState played;

        /// <summary>The week opens with the tutorial, Day 0.</summary>
        public const int FirstDay = 0;

        /// <summary>The day the Room scene should play; negative when it was opened directly.</summary>
        public static int PendingDay { get; set; } = -1;

        public static IReadOnlyList<DayRecord> Days => State.days;

        public static bool HasProgress => State.days.Count > 0;

        /// <summary>The first day not played yet.</summary>
        public static int NextDay
        {
            get
            {
                int n = FirstDay;
                while (Get(n) != null)
                    n++;
                return n;
            }
        }

        public static DayRecord Get(int day) => State.days.Find(d => d.day == day);

        /// <summary>The finished days before <paramref name="day"/>, in order, as long as none is missing.</summary>
        public static List<DayRecord> Before(int day)
        {
            var list = new List<DayRecord>();
            for (int n = FirstDay; n < day; n++)
            {
                var r = Get(n);
                if (r == null)
                    return new List<DayRecord>();
                list.Add(r);
            }
            return list;
        }

        public static void NewRun()
        {
            state = new RunState();
            Save();
        }

        /// <summary>
        /// Stores a finished day. Days after it were built on another outcome, so they are dropped.
        /// The day's progress (kept across weeks) counts it and keeps it as its last record.
        /// </summary>
        public static void Record(DayRecord record)
        {
            // Loaded first: an old save's progress is made from this week's days, not counting this one.
            var progress = Played;
            State.days.RemoveAll(d => d.day >= record.day);
            State.days.Add(record);
            State.days.Sort((a, b) => a.day.CompareTo(b.day));
            Save();

            var p = ProgressOf(record.day);
            if (p == null)
            {
                p = new DayProgress { day = record.day };
                progress.days.Add(p);
            }
            p.finished++;
            if (record.Right)
                p.right++;
            p.last = Copy(record);
            SavePlayed();
        }

        // ---------------------------------------------------------------- days opened

        /// <summary>Whether a day has been played to its end, this week or in an earlier one.</summary>
        public static bool Finished(int day) => Get(day) != null || TimesFinished(day) > 0;

        /// <summary>The first day is always open; every other opens for good once the day before it is finished (or in <see cref="Demo"/>).</summary>
        public static bool Unlocked(int day) => day <= FirstDay || Demo || Finished(day - 1);

        /// <summary>
        /// For showing the game (an incubator booth, a pitch): every day open, whatever was played.
        /// Typed as DEMO on the title screen, kept on this machine (PlayerPrefs "DCM.Demo") until typed again.
        /// A day started without its earlier days plays without their echoes.
        /// </summary>
        public static bool Demo
        {
            get => PlayerPrefs.GetInt(DemoKey, 0) == 1;
            set
            {
                if (value)
                    PlayerPrefs.SetInt(DemoKey, 1);
                else
                    PlayerPrefs.DeleteKey(DemoKey);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// The day to play next: where this week goes on, else the first open day never finished (the
        /// one the last finish opened); -1 when every open day has been finished, so the player picks.
        /// </summary>
        public static int Suggested(DayCatalog catalog)
        {
            if (catalog == null)
                return -1;
            int next = NextDay;
            if (HasProgress && catalog.Get(next) != null)
                return next;
            foreach (var d in catalog.days)
                if (d != null && Unlocked(d.day) && !Finished(d.day))
                    return d.day;
            return -1;
        }

        /// <summary>How often a day has been played to its end, over all weeks.</summary>
        public static int TimesFinished(int day) => ProgressOf(day)?.finished ?? 0;

        /// <summary>How often a day's call was right, over all weeks.</summary>
        public static int TimesRight(int day) => ProgressOf(day)?.right ?? 0;

        /// <summary>
        /// Before a day picked in the day select: the run needs every earlier day, for the money and
        /// the echoes. An earlier day not played this week comes back as it went the last time it was
        /// finished, in whichever week that was.
        /// </summary>
        public static void FillBefore(int day)
        {
            bool changed = false;
            for (int n = FirstDay; n < day; n++)
            {
                if (Get(n) != null)
                    continue;
                var last = ProgressOf(n)?.last;
                if (last == null || last.day != n || string.IsNullOrEmpty(last.variant))
                    break;
                State.days.Add(Copy(last));
                changed = true;
            }
            if (!changed)
                return;
            State.days.Sort((a, b) => a.day.CompareTo(b.day));
            Save();
        }

        static DayProgress ProgressOf(int day) => Played.days.Find(p => p.day == day);

        static DayRecord Copy(DayRecord r) => JsonUtility.FromJson<DayRecord>(JsonUtility.ToJson(r));

        // ---------------------------------------------------------------- cases played

        /// <summary>How often a day's case has been played to its end, over all weeks (a new week keeps the count).</summary>
        public static int TimesPlayed(int day, string scenario)
        {
            var c = Played.cases.Find(x => x.day == day && x.scenario == scenario);
            return c != null ? c.times : 0;
        }

        /// <summary>Counts a finished case and the truth it had, so the day's next pick prefers the others.</summary>
        public static void MarkPlayed(int day, string scenario, bool scam)
        {
            var c = Played.cases.Find(x => x.day == day && x.scenario == scenario);
            if (c == null)
            {
                c = new PlayedCase { day = day, scenario = scenario };
                Played.cases.Add(c);
            }
            c.times++;
            if (scam)
                c.sawScam = true;
            else
                c.sawLegit = true;
            SavePlayed();
        }

        /// <summary>Whether the scam truth of a case, or its legit truth, has been played to the end (over all weeks).</summary>
        public static bool SawTruth(int day, string scenario, bool scam)
        {
            var c = Played.cases.Find(x => x.day == day && x.scenario == scenario);
            return c != null && (scam ? c.sawScam : c.sawLegit);
        }

        // ---------------------------------------------------------------- trick cards

        /// <summary>The trick cards won so far, over all weeks, in the order they were won.</summary>
        public static IReadOnlyList<CaughtTrick> Tricks => Played.tricks;

        public static CaughtTrick Trick(string id) => Played.tricks.Find(t => t.id == id);

        /// <summary>Wins a trick card. False when it was won before.</summary>
        public static bool CatchTrick(string id, int day, string scenario)
        {
            if (string.IsNullOrEmpty(id) || Trick(id) != null)
                return false;
            Played.tricks.Add(new CaughtTrick { id = id, day = day, scenario = scenario });
            SavePlayed();
            return true;
        }

        static void SavePlayed()
        {
            PlayerPrefs.SetString(PlayedKey, JsonUtility.ToJson(Played));
            PlayerPrefs.Save();
        }

        static PlayedState Played
        {
            get
            {
                if (played != null)
                    return played;
                try
                {
                    string json = PlayerPrefs.GetString(PlayedKey, "");
                    played = string.IsNullOrEmpty(json) ? new PlayedState() : JsonUtility.FromJson<PlayedState>(json) ?? new PlayedState();
                }
                catch (Exception)
                {
                    played = new PlayedState();
                }
                // Saves from before the day select kept no progress: this week's days count as finished
                // once, saved at once so a new week cannot lose them.
                if (played.days.Count == 0 && State.days.Count > 0)
                {
                    foreach (var r in State.days)
                        played.days.Add(new DayProgress { day = r.day, finished = 1, right = r.Right ? 1 : 0, last = Copy(r) });
                    SavePlayed();
                }
                return played;
            }
        }

        // ---------------------------------------------------------------- development

        /// <summary>Development and tools: the next day played takes this variant (a DayVariant.id) instead of picking one.</summary>
        public static void ForceNextVariant(string id)
        {
            PlayerPrefs.SetString(ForceKey, id ?? "");
            PlayerPrefs.Save();
        }

        /// <summary>The variant asked for with <see cref="ForceNextVariant"/>, once: reading it clears it.</summary>
        public static string TakeForcedVariant()
        {
            string id = PlayerPrefs.GetString(ForceKey, "");
            if (id.Length > 0)
            {
                PlayerPrefs.DeleteKey(ForceKey);
                PlayerPrefs.Save();
            }
            return id;
        }

        static RunState State
        {
            get
            {
                if (state == null)
                    Load();
                return state;
            }
        }

        static void Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                state = string.IsNullOrEmpty(json) ? new RunState() : JsonUtility.FromJson<RunState>(json) ?? new RunState();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameRun] Could not read the saved run, starting a new one: " + e.Message);
                state = new RunState();
            }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }
    }
}
