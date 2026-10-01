using System.Collections.Generic;
using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The next morning (End scene): the Seoul Daily reveals who really called, next to the case
    /// summary: the verdict stamp, money kept, paid or lost, when the player decided, which clues
    /// they found and where the missed ones were, and the rule learned. Then the next day, the same
    /// day again or the menu; after the last day, the week's summary.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] UISkin skin;
        [Tooltip("Shown when the scene is opened directly, without a finished day.")]
        [SerializeField] DayData previewDay;
        [SerializeField] Outcome previewOutcome = Outcome.Refuse;

        VisualElement root;
        VisualElement screen;
        Fader fade;
        bool built;
        bool leaving;

        void Awake()
        {
            UISkin.Current = skin;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (GetComponent<Sfx>() == null)
                gameObject.AddComponent<Sfx>();
        }

        void OnEnable() => Build();

        DayResult Preview()
        {
            if (previewDay == null || previewDay.variants.Count == 0)
                return null;
            var variant = previewDay.variants[0];
            var r = new DayResult { day = previewDay, variant = variant, outcome = previewOutcome, decidedAt = "16:48", callSeconds = 312f, minutesTaken = 27, savingsAfter = 4284300 };
            foreach (var c in variant.clues)
                if (r.cluesFound.Count < 4)
                    r.cluesFound.Add(c.id);
            if (previewOutcome == Outcome.GoAlong)
            {
                var transfer = variant.conversation != null ? variant.conversation.actions.Find(a => a.kind == ActionKind.Transfer) : null;
                r.moneyDelta = transfer != null ? -transfer.amount : -1200000;
                r.savingsAfter += r.moneyDelta;
            }
            return r;
        }

        void Build()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null || built)
                return;
            built = true;
            root.AddToClassList("dcm-root");
            root.pickingMode = PickingMode.Ignore;
            var result = SceneFlow.LastResult ?? Preview();
            screen = UIKit.Div("end-screen");
            root.Add(screen);
            fade = new Fader(true);
            root.Add(fade.Root);
            fade.FadeIn(1.6f);
            if (result == null || result.day == null)
            {
                screen.Add(UIKit.Text("No day was played.", "end-caption", "end--in"));
                screen.Add(PauseMenuView.MenuButton("ic_exit", "Main menu", () => Leave(SceneFlow.Home), "btn--blue"));
                return;
            }

            var day = result.day;
            var variant = result.variant ?? (day.variants.Count > 0 ? day.variants[0] : null);
            var paper = variant?.Paper(result.outcome);
            var caption = UIKit.Text($"The next morning  ·  {day.nextDateLabel}", "end-caption");
            screen.Add(caption);
            var row = UIKit.Div("end-row");
            var left = UIKit.Div("end-left");
            var news = BuildPaper(day, paper);
            left.Add(news);
            var rule = BuildRule(variant);
            if (rule != null)
                left.Add(rule);
            var summary = BuildSummary(result, variant, paper);
            row.Add(left);
            row.Add(summary);
            screen.Add(row);

            caption.schedule.Execute(() => caption.AddToClassList("end--in")).ExecuteLater(500);
            news.schedule.Execute(() =>
            {
                news.AddToClassList("end--in");
                Sfx.Play(Sfx.Paper);
            }).ExecuteLater(1100);
            summary.schedule.Execute(() => summary.AddToClassList("end--in")).ExecuteLater(1700);
            if (rule != null)
                rule.schedule.Execute(() => rule.AddToClassList("end--in")).ExecuteLater(4600);
        }

        static VisualElement BuildRule(DayVariant variant)
        {
            if (variant == null || string.IsNullOrEmpty(variant.rule))
                return null;
            var rule = UIKit.Div("end-rule");
            rule.Add(UIKit.Text("RULE LEARNED  ·  written in your notebook", "end-rule__title"));
            rule.Add(UIKit.Text(variant.rule, "end-rule__text"));
            if (!string.IsNullOrEmpty(variant.ruleSource))
                rule.Add(UIKit.Text(variant.ruleSource, "end-rule__source"));
            return rule;
        }

        VisualElement BuildPaper(DayData day, EndPaper paper)
        {
            var sheet = UIKit.Div("news", "end-paper");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("news__scroll");
            var c = scroll.contentContainer;
            var top = UIKit.Div("news__top");
            top.Add(UIKit.Text("서울데일리", "news__kor"));
            top.Add(UIKit.Text("SEOUL DAILY", "news__masthead"));
            top.Add(UIKit.Text($"No. {12408 + day.day:N0}", "news__issue"));
            c.Add(top);
            var date = UIKit.Div("news__dateline");
            date.Add(UIKit.Text((day.nextDateLabel ?? "").ToUpperInvariant()));
            date.Add(UIKit.Text("MANGWON · MAPO · SEOUL"));
            date.Add(UIKit.Text("1,000 won"));
            c.Add(date);
            if (paper != null)
            {
                c.Add(UIKit.Text(paper.headline, "news__headline", "end-paper__headline"));
                c.Add(UIKit.Text(paper.subhead, "news__subhead"));
                foreach (string p in (paper.body ?? "").Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries))
                    c.Add(UIKit.Text(p.Trim(), "news__body"));
            }
            sheet.Add(scroll);
            return sheet;
        }

        VisualElement BuildSummary(DayResult result, DayVariant variant, EndPaper paper)
        {
            var day = result.day;
            var conv = variant?.conversation;
            var card = UIKit.Div("end-card", "paper");
            var head = UIKit.Div("end-card__head");
            head.Add(UIKit.Portrait(conv != null ? conv.caller.portrait : null, "end-card__portrait"));
            var who = UIKit.Div("grow");
            who.Add(UIKit.Text($"CASE #{day.day:00}  ·  CLOSED", "section"));
            who.Add(UIKit.Text(conv != null ? conv.caseInfo.caseTitle : day.name, "end-card__title"));
            head.Add(who);
            card.Add(head);

            bool scam = variant == null || variant.IsScam;
            var (verdict, good) = Judge(scam, result.outcome);
            var stamps = UIKit.Div("end-card__stamps");
            stamps.Add(Stamp(scam ? "SCAM" : "REAL", !scam, -6f, 2300));
            stamps.Add(Stamp(verdict, good, 5f, 2800));
            card.Add(stamps);
            if (paper != null && !string.IsNullOrEmpty(paper.verdictNote))
                card.Add(UIKit.Text(paper.verdictNote, "end-card__note"));

            var stats = UIKit.Div("end-card__stats");
            if (result.moneyDelta < 0)
                stats.Add(Stat(scam ? "Lost" : "Paid", FactText.Won(result.moneyDelta), scam ? "amount-out" : null));
            else
                stats.Add(Stat("Savings kept", FactText.Won(result.savingsAfter), "amount-in"));
            stats.Add(Stat("Decided at", result.decidedAt ?? "--:--", null));
            stats.Add(Stat("Time taken", $"{Mathf.Max(1, result.minutesTaken)} min", null));
            card.Add(stats);

            var clues = variant != null ? variant.clues : new List<ClueDef>();
            var found = new HashSet<string>(result.cluesFound);
            card.Add(UIKit.Text($"CLUES YOU FOUND  ·  {found.Count}/{clues.Count}", "section"));
            var list = UIKit.Div("end-card__clues");
            int i = 0;
            foreach (var clue in clues)
            {
                bool got = found.Contains(clue.id);
                var line = UIKit.Div("end-clue", got ? "end-clue--found" : "end-clue--missed");
                line.Add(UIKit.Icon(got ? "ic_check" : "ic_cross", "end-clue__icon"));
                var text = UIKit.Div("grow");
                text.style.flexShrink = 1;
                text.Add(UIKit.Text(clue.text, "end-clue__text"));
                if (!got)
                    text.Add(UIKit.Text("Where: " + clue.where, "end-clue__where"));
                line.Add(text);
                list.Add(line);
                var l = line;
                l.schedule.Execute(() =>
                {
                    l.AddToClassList("end-clue--in");
                    Sfx.Play(got ? Sfx.Pop : Sfx.Tick, 0.5f);
                }).ExecuteLater(3300 + 180 * i++);
            }
            card.Add(list);

            var catalog = DayCatalog.Load();
            bool hasNext = catalog != null && catalog.Get(day.day + 1) != null;
            var buttons = UIKit.Div("end-card__buttons");
            if (hasNext)
                buttons.Add(PauseMenuView.MenuButton("ic_play", $"Day {day.day + 1}", () => Leave(SceneFlow.Room, day.day + 1), "btn--green"));
            else
                buttons.Add(PauseMenuView.MenuButton("ic_star", "Your week", ShowWeek, "btn--green"));
            buttons.Add(PauseMenuView.MenuButton("ic_back", "Play again", () => Leave(SceneFlow.Room, day.day), null));
            buttons.Add(PauseMenuView.MenuButton("ic_exit", "Main menu", () => Leave(SceneFlow.Home), "btn--blue"));
            card.Add(buttons);
            return card;
        }

        static (string text, bool good) Judge(bool scam, Outcome outcome) => outcome switch
        {
            Outcome.Timeout => ("TOO LATE", false),
            Outcome.GoAlong => (scam ? "WRONG CALL" : "RIGHT CALL", !scam),
            Outcome.Refuse => (scam ? "RIGHT CALL" : "WRONG CALL", scam),
            _ => ("BEST CALL", true),
        };

        // ---------------------------------------------------------------- the week

        /// <summary>After the last day: every day's truth and verdict, the money, a rating and the rules learned.</summary>
        void ShowWeek()
        {
            var catalog = DayCatalog.Load();
            var days = GameRun.Days;
            screen.Clear();
            Sfx.Play(Sfx.Paper);

            var caption = UIKit.Text("YOUR WEEK  ·  WHO REALLY CALLED", "end-caption");
            screen.Add(caption);
            var card = UIKit.Div("week-card", "paper");

            int right = 0;
            long lost = 0;
            var table = UIKit.Div("week-table");
            int i = 0;
            foreach (var r in days)
            {
                var data = catalog != null ? catalog.Get(r.day) : null;
                var variant = data != null ? data.Variant(r.variant) : null;
                var conv = variant?.conversation;
                if (r.Right)
                    right++;
                if (r.scam && r.moneyDelta < 0)
                    lost += -r.moneyDelta;

                var row = UIKit.Div("week-row");
                row.Add(UIKit.Portrait(conv != null ? conv.caller.portrait : null, "week-row__portrait"));
                var name = UIKit.Div("week-row__name");
                name.Add(UIKit.Text($"DAY {r.day}  ·  {(data != null ? data.dateLabel : "")}", "week-row__day"));
                name.Add(UIKit.Text(conv != null ? conv.caseInfo.caseTitle : "", "week-row__title"));
                row.Add(name);
                row.Add(UIKit.Text(r.scam ? "SCAM" : "REAL", "week-row__truth", r.scam ? "week-row__truth--scam" : "week-row__truth--real"));
                row.Add(UIKit.Text(Did(r, conv), "week-row__did"));
                row.Add(UIKit.Text($"{r.clues.Count}/{r.cluesTotal}", "week-row__clues"));
                var mark = UIKit.Div("week-row__mark");
                mark.Add(UIKit.Icon(r.Right ? "ic_check" : "ic_cross", "week-row__icon"));
                row.Add(mark);
                table.Add(row);
                var captured = row;
                captured.schedule.Execute(() =>
                {
                    captured.AddToClassList("week-row--in");
                    Sfx.Play(Sfx.Tick, 0.6f);
                }).ExecuteLater(900 + 350 * i++);
            }
            card.Add(table);

            long start = StartingSavings(catalog);
            long end = days.Count > 0 ? days[days.Count - 1].savingsAfter : start;
            var stats = UIKit.Div("end-card__stats", "week-stats");
            stats.Add(Stat("Right calls", $"{right}/{days.Count}", null));
            stats.Add(Stat("Lost to scams", lost > 0 ? FactText.Won(-lost) : "₩0", lost > 0 ? "amount-out" : "amount-in"));
            stats.Add(Stat("Savings", $"{FactText.Won(start)} → {FactText.Won(end)}", null));
            card.Add(stats);

            string rating = right == days.Count && days.Count > 0 ? "SCAM-PROOF" : right >= days.Count - 1 && days.Count > 1 ? "CAREFUL" : "AT RISK";
            var stamps = UIKit.Div("end-card__stamps");
            stamps.Add(Stamp(rating, rating != "AT RISK", -4f, 900 + 350 * days.Count + 400));
            card.Add(stamps);

            card.Add(UIKit.Text("RULES IN YOUR NOTEBOOK", "section"));
            var rules = UIKit.Div("week-rules");
            foreach (var r in days)
            {
                var variant = catalog != null ? catalog.Get(r.day)?.Variant(r.variant) : null;
                if (variant != null && !string.IsNullOrEmpty(variant.rule))
                    rules.Add(UIKit.Text("• " + variant.rule, "week-rules__rule"));
            }
            card.Add(rules);

            var buttons = UIKit.Div("end-card__buttons");
            buttons.Add(PauseMenuView.MenuButton("ic_play", "New week", () =>
            {
                GameRun.NewRun();
                Leave(SceneFlow.Room, 1);
            }, "btn--green"));
            buttons.Add(PauseMenuView.MenuButton("ic_exit", "Main menu", () => Leave(SceneFlow.Home), "btn--blue"));
            card.Add(buttons);
            screen.Add(card);

            caption.schedule.Execute(() => caption.AddToClassList("end--in")).ExecuteLater(100);
            card.schedule.Execute(() => card.AddToClassList("end--in")).ExecuteLater(350);
        }

        /// <summary>What the player did, in a few words: "Sent ₩450,000", "Hung up", "Ran out of time".</summary>
        static string Did(DayRecord r, ConversationData conv) => r.outcome switch
        {
            Outcome.GoAlong => r.moneyDelta < 0 ? "Sent " + FactText.Won(-r.moneyDelta) : conv?.verdict?.goAlong ?? "Went along",
            Outcome.Refuse => "Hung up",
            Outcome.Timeout => "Ran out of time",
            _ => "Checked first",
        };

        static long StartingSavings(DayCatalog catalog)
        {
            var first = catalog != null ? catalog.Get(1) : null;
            var phone = first != null && first.variants.Count > 0 ? first.variants[0].phone : null;
            long total = 0;
            if (phone != null)
                foreach (var a in phone.bank.accounts)
                    total += a.balance;
            return total;
        }

        // ---------------------------------------------------------------- helpers

        static VisualElement Stat(string label, string value, string valueClass)
        {
            var s = UIKit.Div("end-stat");
            s.Add(UIKit.Text(label, "end-stat__label"));
            s.Add(UIKit.Text(value, "end-stat__value", valueClass));
            return s;
        }

        static VisualElement Stamp(string text, bool good, float angle, long delayMs)
        {
            var stamp = UIKit.Text(text, "stamp", good ? "stamp--green" : "stamp--red");
            stamp.style.whiteSpace = WhiteSpace.NoWrap;
            stamp.style.rotate = new Rotate(angle);
            stamp.schedule.Execute(() =>
            {
                stamp.AddToClassList("stamp--down");
                Sfx.Play(Sfx.Stamp);
            }).ExecuteLater(delayMs);
            return stamp;
        }

        void Leave(string scene, int day = 0)
        {
            if (leaving)
                return;
            leaving = true;
            MusicPlayer.Current?.Stop(1f);
            fade.FadeOut(1f, () =>
            {
                if (scene == SceneFlow.Room)
                    SceneFlow.PlayDay(day);
                else
                    SceneFlow.Load(scene);
            });
        }
    }
}
