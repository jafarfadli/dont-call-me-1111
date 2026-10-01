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
        [Tooltip("The variant played: \"scam\" or \"legit\".")]
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
    /// title screen continues from here and each day reads the earlier ones for its echoes.
    /// </summary>
    public static class GameRun
    {
        const string Key = "DCM.Run";

        [Serializable]
        class RunState
        {
            public List<DayRecord> days = new List<DayRecord>();
        }

        static RunState state;

        /// <summary>The day the Room scene should play; 0 when it was opened directly.</summary>
        public static int PendingDay { get; set; }

        public static IReadOnlyList<DayRecord> Days => State.days;

        public static bool HasProgress => State.days.Count > 0;

        /// <summary>The first day not played yet.</summary>
        public static int NextDay
        {
            get
            {
                int n = 1;
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
            for (int n = 1; n < day; n++)
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
