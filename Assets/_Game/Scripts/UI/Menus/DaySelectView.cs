using System;
using System.Collections.Generic;
using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The title screen's day select: every day of the week as a calendar page, side by side. A day
    /// opens for good once the day before it is finished (<see cref="GameRun.Unlocked"/>), so a
    /// player never has to go back through the tutorial. Each page shows the day's difficulty, how
    /// it went this week, and what is still to collect there (truths seen, trick cards won); the
    /// day to play next (<see cref="GameRun.Suggested"/>) is marked green. Picking a day hands it to the owner, which fills in
    /// the earlier days (<see cref="GameRun.FillBefore"/>) and starts it.
    /// </summary>
    public class DaySelectView
    {
        public VisualElement Root { get; }

        public DaySelectView(Action onBack, Action<int> onPlay)
        {
            Root = UIKit.Div("menu-card", "paper", "day-select");
            var head = UIKit.Div("pause__head");
            head.Add(UIKit.Icon("ic_clock", "pause__icon"));
            head.Add(UIKit.Text(Loc.T("CHOOSE A DAY"), "menu-card__title"));
            Root.Add(head);
            Root.Add(UIKit.Text(GameRun.Demo ? Loc.T("DEMO: every day is open. Type DEMO on the title screen to close them.")
                                             : Loc.T("Finish a day to open the next one. A day once opened stays open, week after week."),
                                GameRun.Demo ? "day-select__note--demo" : null, "day-select__note"));

            var catalog = DayCatalog.Load();
            var cases = CaseBook.All(catalog);
            int next = GameRun.Suggested(catalog);
            var pages = UIKit.Div("day-select__pages");
            if (catalog != null)
                foreach (var day in catalog.days)
                    if (day != null)
                        pages.Add(Page(day, catalog, cases, day.day == next, onPlay));
            Root.Add(pages);

            var buttons = UIKit.Div("modal__buttons");
            buttons.Add(UIKit.Btn(Loc.T("Back"), onBack, "btn--blue"));
            Root.Add(buttons);
        }

        static VisualElement Page(DayData day, DayCatalog catalog, List<CaseBook.Case> cases, bool isNext, Action<int> onPlay)
        {
            int n = day.day;
            bool open = GameRun.Unlocked(n);
            var record = GameRun.Get(n);
            bool fresh = open && !GameRun.Finished(n);
            var page = UIKit.Div("day-pick", open ? null : "day-pick--locked", open && isNext ? "day-pick--next" : null);

            var top = UIKit.Div("day-pick__top");
            top.Add(UIKit.Text(day.dateLabel, "day-pick__date"));
            page.Add(top);

            var body = UIKit.Div("day-pick__body");
            var title = UIKit.Div("day-pick__head");
            title.Add(UIKit.Text($"DAY {n}", "day-pick__day"));
            if (fresh && n > GameRun.FirstDay)
                title.Add(UIKit.Text(Loc.T("NEW"), "day-pick__new"));
            body.Add(title);
            var stars = UIKit.Text(new string('★', day.level) + new string('☆', Math.Max(0, DayData.TopLevel - day.level)), "day-pick__level");
            stars.tooltip = Loc.F("Difficulty {0} of {1}", day.level, DayData.TopLevel);
            body.Add(stars);
            body.Add(UIKit.Text(HelpName(day), "day-pick__help"));

            // This week: how it went, or why it is still closed.
            var week = UIKit.Div("day-pick__week");
            if (!open)
            {
                week.Add(UIKit.Icon("ic_lock", "day-pick__lock"));
                week.Add(UIKit.Text(Loc.F("Finish Day {0} first", n - 1), "day-pick__why"));
            }
            else if (record != null)
            {
                // The caller and the verdict on one line, the case's name under them at full width.
                var conv = day.Variant(record.variant)?.conversation;
                week.AddToClassList("day-pick__week--played");
                var who = UIKit.Div("day-pick__who");
                who.Add(UIKit.Portrait(conv != null ? conv.caller.portrait : null, "day-pick__portrait"));
                who.Add(UIKit.Div("grow"));
                who.Add(UIKit.Icon(record.Right ? "ic_check" : "ic_cross", "day-pick__mark"));
                week.Add(who);
                week.Add(UIKit.Text(conv != null ? conv.caseInfo.caseTitle : "", "day-pick__case"));
            }
            else
            {
                week.Add(UIKit.Text(Loc.T("Not played this week"), "day-pick__why"));
            }
            body.Add(week);

            // What is still to collect on this day.
            var (seen, truths) = Truths(cases, n);
            var (won, tricks) = Tricks(day);
            var counts = UIKit.Div("day-pick__counts");
            counts.Add(UIKit.Text(Loc.F("Truths {0}/{1}", seen, truths), "day-pick__count"));
            if (tricks > 0)
                counts.Add(UIKit.Text(Loc.F("Trick cards {0}/{1}", won, tricks), "day-pick__count"));
            body.Add(counts);
            body.Add(UIKit.Div("grow"));

            if (open)
            {
                string label = isNext ? Loc.T(n > GameRun.FirstDay || GameRun.HasProgress ? "Continue" : "Play") : Loc.T(record != null ? "Play again" : "Play");
                var play = UIKit.Btn(label, () =>
                {
                    Sfx.Play(Sfx.Click, 0.5f);
                    onPlay(n);
                }, "day-pick__btn", isNext ? "btn--green" : null);
                body.Add(play);
            }
            page.Add(body);
            return page;
        }

        /// <summary>How much the notebook helps that day, in two or three words.</summary>
        static string HelpName(DayData day) => day.tutorial ? Loc.T("Tutorial") : day.help switch
        {
            NotebookHelp.Guided => Loc.T("Tutorial"),
            NotebookHelp.Hints => Loc.T("Notebook hints"),
            NotebookHelp.Write => Loc.T("You write the notes"),
            _ => Loc.T("No hints"),
        };

        static (int seen, int total) Truths(List<CaseBook.Case> cases, int day)
        {
            int seen = 0, total = 0;
            foreach (var c in cases)
                if (c.day == day)
                {
                    seen += c.SeenCount;
                    total += c.truths.Count;
                }
            return (seen, total);
        }

        /// <summary>The trick cards the day's cases can give (one per trick behind a false claim), and how many are won.</summary>
        static (int won, int total) Tricks(DayData day)
        {
            var ids = new HashSet<string>();
            foreach (var v in day.variants)
                if (v.conversation != null)
                    foreach (var claim in v.conversation.claims)
                        if (!claim.truth && !string.IsNullOrEmpty(claim.trick))
                            ids.Add(claim.trick);
            int won = 0;
            foreach (string id in ids)
                if (GameRun.Trick(id) != null)
                    won++;
            return (won, ids.Count);
        }
    }
}
