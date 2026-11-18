using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>
    /// What the player does, line by line, for playtests: when a day starts, what they looked at,
    /// what they circled, stamped and pinned, what they asked, their verdict, and at the end of the
    /// day a summary (right or wrong, false claims caught, decoys circled, time to the first circle,
    /// the first place they looked). One JSON object per line (JSON Lines), read by
    /// <c>Tools/Playtest/summarize.py</c>.
    /// <para>
    /// Where it goes: in a desktop build, <c>playtest/&lt;session&gt;.jsonl</c> under the
    /// persistent data folder; in the editor, the project's <c>Temp/Playtest</c> (so test runs do
    /// not mix with real playtests); in a browser, PlayerPrefs (<c>DCM.Playtest</c>, the newest
    /// <see cref="MaxBrowserChars"/> characters). <see cref="Export"/> (Case Files) opens the folder,
    /// or in a browser downloads the file.
    /// </para>
    /// </summary>
    public static class PlaytestLog
    {
        const string BrowserKey = "DCM.Playtest";
        const int MaxBrowserChars = 400000;

        static string session;
        static int day = -1;
        static string variant = "";
        static float dayStart;
        static float answerAt = -1f;
        static float firstCircleAt = -1f;
        static string firstLook;
        static bool listening;

        /// <summary>One run of the game: the log file's name.</summary>
        public static string Session => session ??= DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        public static string Folder => Application.isEditor
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "Playtest"))
            : Path.Combine(Application.persistentDataPath, "playtest");

        static bool InBrowser => Application.platform == RuntimePlatform.WebGLPlayer;

        // ---------------------------------------------------------------- the day

        public static void BeginDay(DayData data, DayVariant v)
        {
            day = data != null ? data.day : -1;
            variant = v != null ? v.id : "";
            dayStart = Time.time;
            answerAt = -1f;
            firstCircleAt = -1f;
            firstLook = null;
            if (!listening)
            {
                listening = true;
                ClueEvents.Happened += OnLook;
            }
            Write("day_start", variant, data != null ? data.help.ToString() : "",
                  ("case", v != null ? v.Scenario : ""), ("scam", v == null || v.IsScam), ("language", Loc.Current.ToString()),
                  ("demo", GameRun.Demo));
        }

        /// <summary>The call was answered: the clock for the first circle starts.</summary>
        public static void Answer()
        {
            answerAt = Time.time;
            Log("answer");
        }

        public static void Log(string kind, string target = "", string detail = "") => Write(kind, target, detail);

        /// <summary>Something circled for the notebook (or let go of).</summary>
        public static void Circle(MarkDef mark, bool on)
        {
            if (on && firstCircleAt < 0f)
                firstCircleAt = Time.time;
            Write(on ? "circle" : "uncircle", mark.id, mark.claim, ("place", mark.place.ToString()));
        }

        static void OnLook(ClueEvent kind, string target)
        {
            // Circling is logged with its claim by Circle.
            if (kind == ClueEvent.Marked || day < 0)
                return;
            if (firstLook == null && kind != ClueEvent.RecipientShown)
                firstLook = string.IsNullOrEmpty(target) ? kind.ToString() : $"{kind}:{target}";
            Write("look", kind.ToString(), target);
        }

        /// <summary>The day's summary, from its result and its notebook.</summary>
        public static void EndDay(DayResult result, CaseNotebook notebook)
        {
            var v = result.variant;
            var conv = v?.conversation;
            int falseClaims = 0, caught = 0, wrongStamps = 0, stamps = 0;
            if (conv != null)
                foreach (var claim in conv.claims)
                {
                    if (!claim.truth)
                        falseClaims++;
                    var s = notebook != null ? notebook.StampOf(claim.id) : Stamp.None;
                    if (s == Stamp.None)
                        continue;
                    stamps++;
                    if (s == Stamp.False && !claim.truth)
                        caught++;
                    else if ((s == Stamp.False) == claim.truth)
                        wrongStamps++;
                }
            // A decoy is a mark no clue counts.
            var clueMarks = new HashSet<string>();
            if (v != null)
                foreach (var c in v.clues)
                    foreach (var w in c.when)
                        if (w.kind == ClueEvent.Marked)
                            clueMarks.Add(w.target);
            int decoys = 0;
            foreach (string m in result.marks)
                if (!clueMarks.Contains(m))
                    decoys++;
            bool scam = v == null || v.IsScam;
            bool right = result.outcome == Outcome.Verify || (scam ? result.outcome == Outcome.Refuse : result.outcome == Outcome.GoAlong);
            Write("day_end", result.outcome.ToString(), right ? "right" : "wrong",
                  ("case", v != null ? v.Scenario : ""), ("scam", scam), ("right", right),
                  ("help", result.day != null ? result.day.help.ToString() : ""),
                  ("minutes", result.minutesTaken), ("call_seconds", Mathf.RoundToInt(result.callSeconds)),
                  ("claims", conv != null ? conv.claims.Count : 0), ("false_claims", falseClaims), ("caught", caught),
                  ("stamps", stamps), ("wrong_stamps", wrongStamps), ("noted", result.noted.Count),
                  ("circled", result.marks.Count), ("decoys", decoys),
                  ("clues", result.cluesFound.Count), ("clues_total", v != null ? v.clues.Count : 0),
                  ("first_circle_s", firstCircleAt >= 0f && answerAt >= 0f ? Mathf.Round((firstCircleAt - answerAt) * 10f) / 10f : -1f),
                  ("first_look", firstLook ?? ""), ("new_tricks", result.newTricks.Count));
            day = -1;
        }

        // ---------------------------------------------------------------- writing

        static void Write(string kind, string target, string detail, params (string key, object value)[] extra)
        {
            var b = new StringBuilder(160);
            b.Append('{');
            Field(b, "session", Session);
            Field(b, "day", day);
            Field(b, "variant", variant);
            Field(b, "t", day >= 0 ? Mathf.Round((Time.time - dayStart) * 10f) / 10f : 0f);
            Field(b, "clock", GameClockText());
            Field(b, "kind", kind);
            Field(b, "target", target ?? "");
            Field(b, "detail", detail ?? "");
            foreach (var (key, value) in extra)
                Field(b, key, value);
            b.Length--; // the last comma
            b.Append('}');
            Append(b.ToString());
        }

        static string GameClockText()
        {
            try
            {
                return UI.GameClock.Now;
            }
            catch (Exception)
            {
                return "";
            }
        }

        static void Field(StringBuilder b, string key, object value)
        {
            b.Append('"').Append(key).Append("\":");
            switch (value)
            {
                case bool v:
                    b.Append(v ? "true" : "false");
                    break;
                case int v:
                    b.Append(v.ToString(CultureInfo.InvariantCulture));
                    break;
                case float v:
                    b.Append(v.ToString("0.0", CultureInfo.InvariantCulture));
                    break;
                default:
                    b.Append('"');
                    foreach (char c in value?.ToString() ?? "")
                    {
                        if (c == '"' || c == '\\')
                            b.Append('\\').Append(c);
                        else if (c == '\n')
                            b.Append("\\n");
                        else if (c < ' ')
                            b.Append(' ');
                        else
                            b.Append(c);
                    }
                    b.Append('"');
                    break;
            }
            b.Append(',');
        }

        static void Append(string line)
        {
            try
            {
                if (InBrowser)
                {
                    string all = PlayerPrefs.GetString(BrowserKey, "") + line + "\n";
                    if (all.Length > MaxBrowserChars)
                    {
                        int cut = all.IndexOf('\n', all.Length - MaxBrowserChars);
                        all = cut >= 0 ? all.Substring(cut + 1) : "";
                    }
                    PlayerPrefs.SetString(BrowserKey, all);
                    return;
                }
                Directory.CreateDirectory(Folder);
                File.AppendAllText(Path.Combine(Folder, Session + ".jsonl"), line + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PlaytestLog] Could not write: " + e.Message);
            }
        }

        // ---------------------------------------------------------------- export

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void DCM_DownloadText(string fileName, string text);
#endif

        /// <summary>Hands the log over: opens its folder, or in a browser downloads it.</summary>
        public static void Export()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.Save();
            DCM_DownloadText($"dcm-playtest-{Session}.jsonl", PlayerPrefs.GetString(BrowserKey, ""));
#else
            Directory.CreateDirectory(Folder);
            Application.OpenURL(new Uri(Folder).AbsoluteUri);
#endif
        }
    }
}
