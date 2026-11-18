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
    /// summary: the verdict stamp, money kept, paid or lost, when the player decided, the case
    /// notebook checked in red (each claim's truth and where it could be seen), and the rule learned. Then the next day, the same
    /// day again or the menu; after the last day, the week's summary.
    /// The truth is held back for a few seconds: the paper lands with its headline hidden, the
    /// room goes dark with one spotlight on the spot where the stamp will land, and a drumroll
    /// builds before SCAM or REAL is stamped; a short roll, the verdict, then the lights and the rest.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] UISkin skin;
        [Tooltip("Shown when the scene is opened directly, without a finished day.")]
        [SerializeField] DayData previewDay;
        [SerializeField] Outcome previewOutcome = Outcome.Refuse;

        // The reveal, in milliseconds after the scene opens.
        const long PaperAt = 1100;
        const long CardAt = 1700;
        const long SuspenseAt = 2300;
        /// <summary>The pause between the truth (SCAM or REAL) and the verdict on the player's call.</summary>
        const long VerdictAfter = 1300;
        const long RestAfter = 650;
        /// <summary>The spotlight's dark sheet (a square, in panel pixels; the same in Menus.uss): big enough to cover the screen from any spot.</summary>
        const float SpotSize = 4400f;

        VisualElement root;
        VisualElement screen;
        Fader fade;
        bool built;
        bool leaving;

        // What waits for the reveal: the headline, then everything in "late", then the clues one by one.
        readonly List<VisualElement> late = new List<VisualElement>();
        readonly List<(VisualElement line, bool got)> clueLines = new List<(VisualElement line, bool got)>();
        VisualElement headline;
        VisualElement truthStamp;
        VisualElement verdictStamp;
        VisualElement buttons;
        VisualElement spot;
        VisualElement afterRedPen;
        // Something won under the red pen (a trick card, a day opened): it gets the success chime.
        bool wonTricks;
        bool goodCall;

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
                screen.Add(UIKit.Text(Loc.T("No day was played."), "end-caption", "end--in"));
                screen.Add(PauseMenuView.MenuButton("ic_exit", Loc.T("Main menu"), () => Leave(SceneFlow.Home), "btn--blue"));
                return;
            }

            var day = result.day;
            var variant = result.variant ?? (day.variants.Count > 0 ? day.variants[0] : null);
            var paper = variant?.Paper(result.outcome);
            var caption = UIKit.Text(Loc.F("The next morning  ·  {0}", day.nextDateLabel), "end-caption");
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

            // Quiet until the truth is out: the morning's music waits for the verdict.
            var music = FindAnyObjectByType<MusicPlayer>();
            if (music != null)
                music.Hold();

            long truthAt = SuspenseAt + (long)(Sfx.SuspenseSeconds * 1000f);
            long verdictAt = truthAt + VerdictAfter;
            long restAt = verdictAt + RestAfter;

            caption.schedule.Execute(() => caption.AddToClassList("end--in")).ExecuteLater(500);
            news.schedule.Execute(() =>
            {
                news.AddToClassList("end--in");
                Sfx.Play(Sfx.Paper);
            }).ExecuteLater(PaperAt);
            summary.schedule.Execute(() => summary.AddToClassList("end--in")).ExecuteLater(CardAt);
            // The wait: the room goes dark, one light narrows on the empty spot of the stamp, and a
            // drumroll quickens and swells.
            spot.schedule.Execute(() =>
            {
                Aim(truthStamp, false);
                spot.AddToClassList("end-spot--on");
                Sfx.Play(Sfx.Suspense);
            }).ExecuteLater(SuspenseAt);
            // Who really called: the stamp comes down in the light and the headline appears with it.
            screen.schedule.Execute(() =>
            {
                Slam(truthStamp);
                Sfx.Play(Sfx.Reveal);
                headline?.AddToClassList("end-late--in");
            }).ExecuteLater(truthAt);
            // The light moves to the second spot and a short roll leads into the verdict.
            spot.schedule.Execute(() =>
            {
                Aim(verdictStamp, true);
                Sfx.Play(Sfx.Roll);
            }).ExecuteLater(verdictAt - (long)(Sfx.RollSeconds * 1000f));
            // Was the player right.
            screen.schedule.Execute(() =>
            {
                Slam(verdictStamp);
                Sfx.Play(goodCall ? Sfx.RevealGood : Sfx.RevealBad);
            }).ExecuteLater(verdictAt);
            // The lights come back up.
            spot.schedule.Execute(() => spot.RemoveFromClassList("end-spot--on")).ExecuteLater(verdictAt + 300);
            // The story, the numbers and the buttons, and the morning's music.
            screen.schedule.Execute(() =>
            {
                foreach (var e in late)
                    e.AddToClassList("end-late--in");
                buttons.SetEnabled(true);
                if (music != null)
                    music.PlayMain(3f);
            }).ExecuteLater(restAt);
            for (int i = 0; i < clueLines.Count; i++)
            {
                var (line, got) = clueLines[i];
                line.schedule.Execute(() =>
                {
                    line.AddToClassList("end-clue--in");
                    Sfx.Play(got ? Sfx.Pop : Sfx.Tick, 0.5f);
                }).ExecuteLater(restAt + 450 + 180 * i);
            }
            long penDone = restAt + 450 + 180 * clueLines.Count;
            if (afterRedPen != null)
                afterRedPen.schedule.Execute(() =>
                {
                    afterRedPen.AddToClassList("end-after--in");
                    if (wonTricks)
                        Sfx.Play(Sfx.Success, 0.8f);
                }).ExecuteLater(penDone + 250);
            if (rule != null)
                rule.schedule.Execute(() => rule.AddToClassList("end--in")).ExecuteLater(penDone + 500);
        }

        /// <summary>
        /// Points the spotlight at a stamp: the dark sheet is a big square with a clear hole in its
        /// middle, so its middle goes where the stamp's is (both sit in the same row). It is placed
        /// on the first stamp and glides to the second (<paramref name="glide"/>).
        /// </summary>
        void Aim(VisualElement stamp, bool glide)
        {
            var first = truthStamp.layout.center;
            if (!glide)
            {
                spot.style.left = first.x - SpotSize / 2f;
                spot.style.top = first.y - SpotSize / 2f;
                spot.style.translate = new Translate(0, 0);
                return;
            }
            var to = stamp.layout.center;
            spot.style.translate = new Translate(to.x - first.x, to.y - first.y);
        }

        static VisualElement BuildRule(DayVariant variant)
        {
            if (variant == null || string.IsNullOrEmpty(variant.rule))
                return null;
            var rule = UIKit.Div("end-rule");
            rule.Add(UIKit.Text(Loc.T("RULE LEARNED"), "end-rule__title"));
            rule.Add(UIKit.Text(variant.rule, "end-rule__text"));
            if (!string.IsNullOrEmpty(variant.ruleSource))
                rule.Add(UIKit.Text(variant.ruleSource, "end-rule__source"));
            return rule;
        }

        VisualElement BuildPaper(DayData day, EndPaper paper)
        {
            var sheet = UIKit.Div("news", "end-paper");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.AddToClassList("news__scroll");
            var c = scroll.contentContainer;
            var top = UIKit.Div("news__top");
            top.Add(UIKit.Text(Loc.Ko ? "SEOUL DAILY" : "서울데일리", "news__kor"));
            top.Add(UIKit.Text(Loc.T("SEOUL DAILY"), "news__masthead"));
            top.Add(UIKit.Text(Loc.F("No. {0:N0}", 12408 + day.day), "news__issue"));
            c.Add(top);
            var date = UIKit.Div("news__dateline");
            date.Add(UIKit.Text((day.nextDateLabel ?? "").ToUpperInvariant()));
            date.Add(UIKit.Text(Loc.T("MANGWON · MAPO · SEOUL")));
            date.Add(UIKit.Text(Loc.T("1,000 won")));
            c.Add(date);
            if (paper != null)
            {
                // The headline gives the truth away: it appears with the stamp, the story after the verdict.
                headline = UIKit.Text(paper.headline, "news__headline", "end-paper__headline", "end-late");
                c.Add(headline);
                c.Add(Late(UIKit.Text(paper.subhead, "news__subhead")));
                foreach (string p in (paper.body ?? "").Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries))
                    c.Add(Late(UIKit.Text(p.Trim(), "news__body")));
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
            who.Add(UIKit.Text(Loc.F("CASE #{0:00}  ·  CLOSED", day.day), "section"));
            who.Add(UIKit.Text(conv != null ? conv.caseInfo.caseTitle : day.name, "end-card__title"));
            head.Add(who);
            card.Add(head);

            bool scam = variant == null || variant.IsScam;
            var (verdict, good) = Judge(scam, result.outcome);
            goodCall = good;
            var stamps = UIKit.Div("end-card__stamps");
            truthStamp = Stamp(Loc.T(scam ? "SCAM" : "REAL"), !scam, -6f);
            verdictStamp = Stamp(verdict, good, 5f);
            // The spotlight's dark sheet lives in the stamps' row, so it stays on their spot however
            // the screen is laid out; it is drawn over everything before it (the paper, the case's head).
            spot = UIKit.Div("end-spot");
            spot.pickingMode = PickingMode.Ignore;
            stamps.Add(spot);
            stamps.Add(truthStamp);
            stamps.Add(verdictStamp);
            card.Add(stamps);
            if (paper != null && !string.IsNullOrEmpty(paper.verdictNote))
                card.Add(Late(UIKit.Text(paper.verdictNote, "end-card__note")));

            var stats = UIKit.Div("end-card__stats");
            if (result.moneyDelta < 0)
                stats.Add(Stat(Loc.T(scam ? "Lost" : "Paid"), FactText.Won(result.moneyDelta), scam ? "amount-out" : null));
            else
                stats.Add(Stat(Loc.T("Savings kept"), FactText.Won(result.savingsAfter), "amount-in"));
            stats.Add(Stat(Loc.T("Decided at"), result.decidedAt ?? "--:--", null));
            stats.Add(Stat(Loc.T("Time taken"), Loc.F("{0} min", Mathf.Max(1, result.minutesTaken)), null));
            card.Add(Late(stats));

            card.Add(Late(UIKit.Text(Loc.T("YOUR NOTEBOOK, CHECKED BY THE PAPER"), "section")));
            card.Add(BuildRedPen(conv, result));

            var catalog = DayCatalog.Load();
            // After the red pen: the case's other truth, and the trick cards won today.
            var after = UIKit.Div("end-after");
            var book = variant != null ? CaseBook.Find(catalog, day.day, variant.Scenario) : null;
            if (book != null && book.truths.Count > 1)
                after.Add(UIKit.Text(Loc.F("Every case has a twin. You have seen {0} of {1} truths of this one.", book.SeenCount, book.truths.Count), "end-twin"));
            var won = TrickCards.Won(catalog, result.newTricks);
            if (won != null)
                after.Add(won);
            // The first finish of a day opens the next one for good, in the title's day select.
            if (result.opensDay >= 0)
            {
                var opened = UIKit.Div("day-opened");
                opened.Add(UIKit.Text(Loc.F("DAY {0} OPENED", result.opensDay), "day-opened__tag"));
                opened.Add(UIKit.Text(Loc.T("It stays open: start it any time from Choose a day."), "day-opened__text"));
                after.Add(opened);
            }
            wonTricks = won != null || result.opensDay >= 0;
            if (after.childCount > 0)
            {
                afterRedPen = after;
                card.Add(after);
            }
            bool hasNext = catalog != null && catalog.Get(day.day + 1) != null;
            // Nothing to press until the truth is out.
            buttons = Late(UIKit.Div("end-card__buttons"));
            buttons.SetEnabled(false);
            if (hasNext)
                buttons.Add(PauseMenuView.MenuButton("ic_play", Loc.F("Day {0}", day.day + 1), () => Leave(SceneFlow.Room, day.day + 1), "btn--green"));
            else
                buttons.Add(PauseMenuView.MenuButton("ic_star", Loc.T("Your week"), ShowWeek, "btn--green"));
            buttons.Add(PauseMenuView.MenuButton("ic_back", Loc.T("Play again"), () => Leave(SceneFlow.Room, day.day), null));
            buttons.Add(PauseMenuView.MenuButton("ic_exit", Loc.T("Main menu"), () => Leave(SceneFlow.Home), "btn--blue"));
            card.Add(buttons);
            return card;
        }

        /// <summary>
        /// The case notebook gone over in red ink: every claim the caller made, the stamp the player
        /// gave it (or "?"), a red tick or cross, and the truth with where it could be seen. The lines
        /// come in one by one after the verdict.
        /// </summary>
        VisualElement BuildRedPen(ConversationData conv, DayResult result)
        {
            var list = UIKit.Div("end-card__clues");
            if (conv == null)
                return list;
            // On a Write day the player chose what went into the notebook: say which claims never did.
            bool wrote = result.day != null && result.day.help == NotebookHelp.Write;
            for (int i = 0; i < conv.claims.Count; i++)
            {
                var claim = conv.claims[i];
                bool missed = wrote && !result.noted.Contains(claim.id);
                var stamp = CaseNotebook.ParseStamp(result.stamps, claim.id);
                bool right = stamp == (claim.truth ? Flow.Stamp.True : Flow.Stamp.False);
                var line = UIKit.Div("end-clue", "end-claim", right ? "end-clue--found" : "end-clue--missed");
                var top = UIKit.Div("end-claim__top");
                top.Add(UIKit.Text((i + 1).ToString(), "end-claim__n"));
                top.Add(UIKit.Text(claim.text, "end-claim__quote"));
                var yours = stamp == Flow.Stamp.True ? UIKit.Text(Loc.T("TRUE"), "end-claim__stamp", "end-claim__stamp--true")
                          : stamp == Flow.Stamp.False ? UIKit.Text(Loc.T("FALSE"), "end-claim__stamp", "end-claim__stamp--false")
                          : UIKit.Text("?", "end-claim__stamp");
                yours.style.whiteSpace = WhiteSpace.NoWrap;
                top.Add(yours);
                line.Add(top);
                var pen = UIKit.Div("end-claim__pen");
                pen.Add(UIKit.Icon(right ? "ic_check" : "ic_cross", "end-clue__icon"));
                pen.Add(UIKit.Text(missed ? Loc.T("You never wrote this one down.") + " " + claim.note : claim.note, "end-claim__note"));
                line.Add(pen);
                list.Add(line);
                clueLines.Add((line, right));
            }
            return list;
        }

        static (string text, bool good) Judge(bool scam, Outcome outcome) => outcome switch
        {
            Outcome.Timeout => (Loc.T("TOO LATE"), false),
            Outcome.GoAlong => (Loc.T(scam ? "WRONG CALL" : "RIGHT CALL"), !scam),
            Outcome.Refuse => (Loc.T(scam ? "RIGHT CALL" : "WRONG CALL"), scam),
            _ => (Loc.T("BEST CALL"), true),
        };

        // ---------------------------------------------------------------- the week

        /// <summary>After the last day: every day's truth and verdict, the money, a rating and the rules learned.</summary>
        void ShowWeek()
        {
            var catalog = DayCatalog.Load();
            var days = GameRun.Days;
            screen.Clear();
            Sfx.Play(Sfx.Paper);

            var caption = UIKit.Text(Loc.T("YOUR WEEK  ·  WHO REALLY CALLED"), "end-caption");
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
                name.Add(UIKit.Text(Loc.F("DAY {0}  ·  {1}", r.day, data != null ? data.dateLabel : ""), "week-row__day"));
                name.Add(UIKit.Text(conv != null ? conv.caseInfo.caseTitle : "", "week-row__title"));
                row.Add(name);
                row.Add(UIKit.Text(Loc.T(r.scam ? "SCAM" : "REAL"), "week-row__truth", r.scam ? "week-row__truth--scam" : "week-row__truth--real"));
                row.Add(UIKit.Text(DayRecordText.Did(r, conv), "week-row__did"));
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
            stats.Add(Stat(Loc.T("Right calls"), $"{right}/{days.Count}", null));
            stats.Add(Stat(Loc.T("Lost to scams"), lost > 0 ? FactText.Won(-lost) : "₩0", lost > 0 ? "amount-out" : "amount-in"));
            stats.Add(Stat(Loc.T("Savings"), $"{FactText.Won(start)} → {FactText.Won(end)}", null));
            card.Add(stats);
            // What carries over to another week: the truths still face down, the cards still to win.
            var (seenTruths, allTruths) = CaseBook.Count(catalog);
            var (wonCards, allCards) = TrickCards.Count(catalog);
            card.Add(UIKit.Text(Loc.F("Truths seen {0} of {1}  ·  Trick cards {2} of {3}  ·  both kept for your next week", seenTruths, allTruths, wonCards, allCards), "week-collection"));

            string rating = right == days.Count && days.Count > 0 ? "SCAM-PROOF" : right >= days.Count - 1 && days.Count > 1 ? "CAREFUL" : "AT RISK";
            var stamps = UIKit.Div("end-card__stamps");
            stamps.Add(Stamp(Loc.T(rating), rating != "AT RISK", -4f, 900 + 350 * days.Count + 400));
            card.Add(stamps);

            card.Add(UIKit.Text(Loc.T("RULES YOU LEARNED"), "section"));
            var rules = UIKit.Div("week-rules");
            foreach (var r in days)
            {
                var variant = catalog != null ? catalog.Get(r.day)?.Variant(r.variant) : null;
                if (variant != null && !string.IsNullOrEmpty(variant.rule))
                    rules.Add(UIKit.Text("• " + variant.rule, "week-rules__rule"));
            }
            card.Add(rules);

            var weekButtons = UIKit.Div("end-card__buttons");
            // Any day can be played again from the title's day select; the tutorial is not forced on a new week.
            weekButtons.Add(PauseMenuView.MenuButton("ic_play", Loc.T("Choose a day"), () =>
            {
                SceneFlow.ShowDays = true;
                Leave(SceneFlow.Home);
            }, "btn--green"));
            weekButtons.Add(PauseMenuView.MenuButton("ic_exit", Loc.T("Main menu"), () => Leave(SceneFlow.Home), "btn--blue"));
            card.Add(weekButtons);
            screen.Add(card);

            caption.schedule.Execute(() => caption.AddToClassList("end--in")).ExecuteLater(100);
            card.schedule.Execute(() => card.AddToClassList("end--in")).ExecuteLater(350);
        }

        static long StartingSavings(DayCatalog catalog)
        {
            var first = catalog != null ? catalog.Get(GameRun.FirstDay) ?? catalog.Get(GameRun.FirstDay + 1) : null;
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

        /// <summary>Marks an element as held back until the verdict is out.</summary>
        T Late<T>(T element) where T : VisualElement
        {
            element.AddToClassList("end-late");
            late.Add(element);
            return element;
        }

        static VisualElement Stamp(string text, bool good, float angle)
        {
            var stamp = UIKit.Text(text, "stamp", good ? "stamp--green" : "stamp--red");
            stamp.style.whiteSpace = WhiteSpace.NoWrap;
            stamp.style.rotate = new Rotate(angle);
            return stamp;
        }

        /// <summary>A stamp that comes down by itself after a delay (the week's rating).</summary>
        static VisualElement Stamp(string text, bool good, float angle, long delayMs)
        {
            var stamp = Stamp(text, good, angle);
            stamp.schedule.Execute(() => Slam(stamp)).ExecuteLater(delayMs);
            return stamp;
        }

        static void Slam(VisualElement stamp)
        {
            stamp.AddToClassList("stamp--down");
            Sfx.Play(Sfx.Stamp);
        }

        void Leave(string scene, int day = -1)
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
