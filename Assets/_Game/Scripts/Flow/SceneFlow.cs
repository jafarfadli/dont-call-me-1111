using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontCallMe.Flow
{
    /// <summary>How a day went, handed from the Room scene to the End scene.</summary>
    public class DayResult
    {
        public DayData day;
        /// <summary>The truth the day took (its call, clues and papers).</summary>
        public DayVariant variant;
        public Outcome outcome;
        public ConvEnding ending;
        public long moneyDelta;
        public long savingsAfter;
        public readonly List<string> cluesFound = new List<string>();
        public string decidedAt;
        public float callSeconds;
        /// <summary>Game minutes from the first ring to the decision.</summary>
        public int minutesTaken;
    }

    /// <summary>The three scenes and what passes between them.</summary>
    public static class SceneFlow
    {
        public const string Home = "Home";
        public const string Room = "Room";
        public const string End = "End";

        /// <summary>Set when a day ends; the End scene reads it.</summary>
        public static DayResult LastResult { get; set; }

        /// <summary>Opens the Room scene on the given day of the run.</summary>
        public static void PlayDay(int day)
        {
            GameRun.PendingDay = Mathf.Max(1, day);
            Load(Room);
        }

        public static void Load(string scene)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(scene);
        }

        public static Outcome OutcomeOf(ConvEnding ending)
        {
            if (ending == null)
                return Outcome.Refuse;
            if (ending.timedOut)
                return Outcome.Timeout;
            return ending.verdict switch
            {
                Verdict.GoAlong => Outcome.GoAlong,
                Verdict.Verify => Outcome.Verify,
                _ => Outcome.Refuse,
            };
        }
    }
}
