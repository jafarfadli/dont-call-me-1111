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

    /// <summary>
    /// The run in progress, saved between sessions: which days are done and how they went. The
    /// title screen continues from here and each day reads the earlier ones for its echoes. It also
    /// remembers, across weeks, how often each case has been played, so a day can bring the case
    /// the player has seen least.
    /// </summary>
    public static class GameRun
    {
        const string Key = "DCM.Run";
        const string PlayedKey = "DCM.Played";
        const string ForceKey = "DCM.ForceVariant";

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
        }

        [Serializable]
        class PlayedState
        {
            public List<PlayedCase> cases = new List<PlayedCase>();
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

        /// <summary>Stores a finished day. Days after it were built on another outcome, so they are dropped.</summary>
        public static void Record(DayRecord record)
        {
            State.days.RemoveAll(d => d.day >= record.day);
            State.days.Add(record);
            State.days.Sort((a, b) => a.day.CompareTo(b.day));
            Save();
        }

        // ---------------------------------------------------------------- cases played

        /// <summary>How often a day's case has been played to its end, over all weeks (a new week keeps the count).</summary>
        public static int TimesPlayed(int day, string scenario)
        {
            var c = Played.cases.Find(x => x.day == day && x.scenario == scenario);
            return c != null ? c.times : 0;
        }

        /// <summary>Counts a finished case, so the day's next pick prefers the others.</summary>
        public static void MarkPlayed(int day, string scenario)
        {
            var c = Played.cases.Find(x => x.day == day && x.scenario == scenario);
            if (c == null)
            {
                c = new PlayedCase { day = day, scenario = scenario };
                Played.cases.Add(c);
            }
            c.times++;
            PlayerPrefs.SetString(PlayedKey, JsonUtility.ToJson(played));
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
