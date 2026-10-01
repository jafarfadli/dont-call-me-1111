using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.UI
{
    /// <summary>The last copied fact. Every input field has a Paste button that reads it.</summary>
    public static class Clipboard
    {
        public static string Value { get; private set; }
        public static FactKind Kind { get; private set; }
        public static readonly List<Fact> History = new List<Fact>();
        public static event Action<Fact> Copied;

        public static void Copy(Fact fact)
        {
            if (fact == null || string.IsNullOrEmpty(fact.value))
                return;
            Value = fact.value;
            Kind = fact.kind;
            History.RemoveAll(f => f.value == fact.value);
            History.Insert(0, fact);
            if (History.Count > 12)
                History.RemoveAt(History.Count - 1);
            Copied?.Invoke(fact);
        }
    }

    /// <summary>Things the player does on the phone that a conversation may care about.</summary>
    public static class PhoneEvents
    {
        public static event Action<string, string, long> TransferSent;   // bank, account, amount
        public static event Action<string> NumberDialed;
        public static event Action<string> LinkOpened;

        public static void RaiseTransfer(string bank, string account, long amount) => TransferSent?.Invoke(bank, account, amount);
        public static void RaiseDial(string number) => NumberDialed?.Invoke(number);
        public static void RaiseLink(string url) => LinkOpened?.Invoke(url);
    }

    /// <summary>
    /// In-game clock: starts at the day's start time, one game minute per real
    /// <see cref="SecondsPerMinute"/>. Stops while <see cref="Running"/> is false (day cards,
    /// the case card) and, through Time.deltaTime, while the game is paused.
    /// </summary>
    public static class GameClock
    {
        public const float SecondsPerMinute = 8f;
        static float minutes;

        public static string DayLabel { get; set; } = "Day 1 · Tue 6 Oct";

        public static bool Running { get; set; } = true;

        /// <summary>Minutes since midnight.</summary>
        public static float Minutes => minutes;

        public static void Start(string hhmm)
        {
            minutes = Parse(hhmm);
            Running = true;
        }

        public static void Tick(float dt)
        {
            if (Running)
                minutes += dt / SecondsPerMinute;
        }

        /// <summary>"16:20" → 980 minutes since midnight.</summary>
        public static float Parse(string hhmm)
        {
            var parts = (string.IsNullOrEmpty(hhmm) ? "16:20" : hhmm).Split(':');
            return int.Parse(parts[0]) * 60 + (parts.Length > 1 ? int.Parse(parts[1]) : 0);
        }

        public static string Format(float totalMinutes)
        {
            int m = Mathf.FloorToInt(totalMinutes);
            return $"{(m / 60) % 24:00}:{m % 60:00}";
        }

        public static string Now => Format(minutes);
    }
}
