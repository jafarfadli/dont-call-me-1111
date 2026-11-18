using System;
using System.Collections.Generic;
using System.Text;
using DontCallMe.Data;
using UnityEditor;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Checks the built content for the case notebook, in both languages: every claim is said by a
    /// line, a case has one to three claims, a legit caller's claims are all true and a scammer's
    /// are not, every mark is filed under a claim of its case and points at something that is on
    /// screen that day, and every clue can be circled. Every false claim names a trick card that
    /// exists, and every trick card can be won. A video call has all its frames and both its notes.
    /// </summary>
    public static class ContentCheck
    {
        [MenuItem("Tools/Don't Call Me/Content/Check Notebook Content")]
        public static void Run()
        {
            var report = new StringBuilder();
            int problems = 0, marks = 0;
            foreach (string path in new[] { "Assets/_Game/Resources/DayCatalog.asset", "Assets/_Game/Resources/DayCatalog_ko.asset" })
            {
                var catalog = AssetDatabase.LoadAssetAtPath<DayCatalog>(path);
                if (catalog == null)
                {
                    report.AppendLine("No catalog at " + path + ": build the days first.");
                    problems++;
                    continue;
                }
                string language = path.Contains("_ko") ? "KO" : "EN";
                var used = new HashSet<string>();
                foreach (var day in catalog.days)
                    foreach (var v in day.variants)
                    {
                        problems += Check($"{language} Day {day.day} {v.id}", day, v, report, ref marks);
                        // Every false claim wins a trick card that exists.
                        foreach (var claim in v.conversation.claims)
                        {
                            if (!string.IsNullOrEmpty(claim.trick))
                                used.Add(claim.trick);
                            if (claim.truth)
                                continue;
                            if (string.IsNullOrEmpty(claim.trick))
                                report.AppendLine($"{language} Day {day.day} {v.id}: false claim {claim.id} has no trick");
                            else if (catalog.Trick(claim.trick) == null)
                                report.AppendLine($"{language} Day {day.day} {v.id}: claim {claim.id} names missing trick {claim.trick}");
                            else
                                continue;
                            problems++;
                        }
                    }
                foreach (var t in catalog.tricks)
                    if (!used.Contains(t.id))
                    {
                        report.AppendLine($"{language}: trick {t.id} is never used");
                        problems++;
                    }
            }
            string summary = $"[ContentCheck] {marks} marks checked, {problems} problems";
            if (problems > 0)
                Debug.LogWarning(summary + "\n" + report);
            else
                Debug.Log(summary);
        }

        static int Check(string tag, DayData dayData, DayVariant v, StringBuilder report, ref int marks)
        {
            int day = dayData.day;
            int problems = 0;
            void Problem(string text)
            {
                report.AppendLine($"{tag}: {text}");
                problems++;
            }

            var conv = v.conversation;
            var said = new HashSet<string>();
            void Said(ConvLine line)
            {
                if (string.IsNullOrEmpty(line.claim))
                    return;
                said.Add(line.claim);
                // On a Write day only the caller's lines can be pinned.
                if (line.speaker != Speaker.Caller)
                    Problem($"claim {line.claim} is said by {line.speaker}, not the caller");
            }
            foreach (var node in conv.nodes)
                foreach (var line in node.lines)
                    Said(line);
            foreach (var q in conv.questions)
                foreach (var line in q.answer)
                    Said(line);
            foreach (var claim in conv.claims)
                if (!said.Contains(claim.id))
                    Problem($"claim {claim.id} is never said");
            foreach (string id in said)
                if (conv.FindClaim(id) == null)
                    Problem($"a line says unknown claim {id}");
            if (conv.claims.Count == 0 || conv.claims.Count > 3)
                Problem($"{conv.claims.Count} claims (one to three)");
            if (!conv.isScam && conv.claims.Exists(c => !c.truth))
                Problem("a legit caller with a false claim");
            if (conv.isScam && !conv.claims.Exists(c => !c.truth))
                Problem("a scam with no false claim");
            if (conv.IsVideo)
            {
                var frames = new List<string> { "", "_talk", "_talk2", "_blink", "_wave1", "_wave2", "_wave3" };
                if (conv.video.faked)
                    frames.AddRange(new[] { "_fake_wave1", "_fake_wave2", "_fake_wave3", "_fake_glitch" });
                foreach (string f in frames)
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Game/UI/Sprites/{conv.video.face}{f}.png") == null)
                        Problem($"video frame {conv.video.face}{f} is missing (Tools/ArtGen/ui_art.py video_faces)");
                if (string.IsNullOrEmpty(conv.video.faceNote) || string.IsNullOrEmpty(conv.video.waveNote))
                    Problem("a video call without its face and wave notes");
            }

            foreach (var m in v.marks)
            {
                marks++;
                foreach (string claimId in m.Claims)
                    if (conv.FindClaim(claimId) == null)
                        Problem($"mark {m.id} is filed under unknown claim {claimId}");
                if (!OnScreen(m, v))
                    Problem($"mark {m.id} ({m.place}) is not on screen: {m.key}");
                // After Day 0 the front page is yesterday's case whenever an earlier day was played
                // (DaySetup), so the day's own headline and body are only there when it was not.
                else if (day > 0 && m.place == MarkPlace.Newspaper && (m.key == "headline" || v.room.newspaper.body.Contains(m.key)))
                    Problem($"mark {m.id} points at the front page, which yesterday's case replaces: use the warning box, a local item or an ad");
            }
            foreach (var clue in v.clues)
            {
                bool circleable = false;
                foreach (var w in clue.when)
                {
                    if (w.kind != ClueEvent.Marked)
                        continue;
                    if (v.marks.Exists(m => m.id == w.target))
                        circleable = true;
                    else
                        Problem($"clue {clue.id} names missing mark {w.target}");
                }
                if (!circleable)
                    Problem($"clue {clue.id} cannot be circled");
            }
            // A claim that can be false (here, or in the twin) has something under it that settles
            // it: a mark some clue counts. Otherwise its stamp rests on evidence filed elsewhere.
            foreach (var claim in conv.claims)
            {
                bool falseSomewhere = !claim.truth || dayData.variants.Exists(t => t != v && t.Scenario == v.Scenario &&
                                                                                  t.conversation.FindClaim(claim.id) is ClaimDef c && !c.truth);
                if (!falseSomewhere)
                    continue;
                bool settled = v.marks.Exists(m => m.Under(claim.id) && v.clues.Exists(cl => cl.when.Exists(w => w.kind == ClueEvent.Marked && w.target == m.id)));
                if (!settled)
                    Problem($"claim {claim.id} has no mark under it that a clue counts");
            }
            return problems;
        }

        /// <summary>Whether the item the mark points at is in the day's evidence (see MarkDef.key).</summary>
        static bool OnScreen(MarkDef m, DayVariant v)
        {
            switch (m.place)
            {
                case MarkPlace.Newspaper:
                    var n = v.room.newspaper;
                    return m.key == "warning" && !string.IsNullOrEmpty(n.warningTitle) || m.key == "headline" ||
                           n.body.Contains(m.key) || n.local.Exists(i => i.title == m.key) || n.ads.Exists(i => i.title == m.key);
                case MarkPlace.Drawer:
                    int bar = m.key.IndexOf('|');
                    if (bar < 0)
                        return false;
                    string title = m.key.Substring(0, bar), part = m.key.Substring(bar + 1);
                    var doc = v.room.drawer.Find(d => d.title == title);
                    return doc != null && (part == "image" ? doc.image != null :
                                           doc.fields.Exists(f => f.label == part) || Array.IndexOf((doc.body ?? "").Split('\n'), part) >= 0);
                case MarkPlace.Calendar:
                    return v.room.calendar.entries.Exists(e => e.text == m.key);
                case MarkPlace.Computer:
                    return v.directory.FindNumber(m.key) != null || v.directory.FindAccount(m.key) != null;
                case MarkPlace.Contacts:
                    return v.phone.FindContact(m.key) != null;
                case MarkPlace.Chats:
                    int cut = m.key.IndexOf('|');
                    var thread = cut < 0 ? null : v.phone.chats.Find(c => c.id == m.key.Substring(0, cut));
                    return thread != null && thread.messages.Exists(msg => (msg.text ?? msg.photoCaption) == m.key.Substring(cut + 1));
                case MarkPlace.Bank:
                    if (m.key.StartsWith("balance|", StringComparison.Ordinal))
                        return v.phone.bank.accounts.Exists(a => a.number == m.key.Substring(8));
                    // The "nothing received yet today" row shows only when no money came in today.
                    if (m.key == "today|none")
                        return !v.phone.bank.transactions.Exists(t => t.amount > 0 && t.when != null && (t.when.StartsWith("Today", StringComparison.Ordinal) || t.when.StartsWith("오늘", StringComparison.Ordinal)));
                    return v.phone.bank.transactions.Exists(t => $"{t.counterparty}|{t.amount}" == m.key);
                case MarkPlace.Call:
                    // A caller's line, said in the call or in an answer.
                    bool Said(ConvLine l) => l.speaker == Speaker.Caller && l.text == m.key;
                    return v.conversation.nodes.Exists(n => n.lines.Exists(Said)) || v.conversation.questions.Exists(q => q.answer.Exists(Said));
                case MarkPlace.Video:
                    var call = v.conversation;
                    if (!call.IsVideo)
                        return false;
                    // The wave is only on screen once a line has the caller wave.
                    return m.key == "face" || m.key == "wave" && (call.questions.Exists(q => q.answer.Exists(l => l.act == "wave")) ||
                                                                  call.nodes.Exists(n => n.lines.Exists(l => l.act == "wave")));
                default:
                    return false;
            }
        }
    }
}
