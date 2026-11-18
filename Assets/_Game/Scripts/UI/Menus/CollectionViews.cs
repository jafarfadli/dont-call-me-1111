using System.Collections.Generic;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// Every case of the game and its truths. A scam and its legit twin share a case
    /// (<see cref="DayVariant.scenario"/>); the player sees one truth per play, so Case Files shows
    /// the other face down ("1 of 2 truths seen"), a reason to play another week.
    /// </summary>
    public static class CaseBook
    {
        public class Case
        {
            public int day;
            public string scenario;
            /// <summary>Its truths: one variant each, the scam first.</summary>
            public readonly List<DayVariant> truths = new List<DayVariant>();

            /// <summary>Played to the end in any week (or in this week's save, which older saves only have).</summary>
            public bool Seen(DayVariant v)
            {
                if (GameRun.SawTruth(day, scenario, v.IsScam))
                    return true;
                foreach (var r in GameRun.Days)
                    if (r.day == day && r.variant == v.id)
                        return true;
                return false;
            }

            public int SeenCount
            {
                get
                {
                    int n = 0;
                    foreach (var v in truths)
                        if (Seen(v))
                            n++;
                    return n;
                }
            }

            public string Title => truths.Count > 0 && truths[0].conversation != null ? truths[0].conversation.caseInfo.caseTitle : scenario;
            public string Portrait => truths.Count > 0 && truths[0].conversation != null ? truths[0].conversation.caller.portrait : null;
        }

        /// <summary>All cases, day by day, in the order the content lists them.</summary>
        public static List<Case> All(DayCatalog catalog)
        {
            var list = new List<Case>();
            if (catalog == null)
                return list;
            foreach (var day in catalog.days)
            {
                if (day == null)
                    continue;
                foreach (var v in day.variants)
                {
                    var c = list.Find(x => x.day == day.day && x.scenario == v.Scenario);
                    if (c == null)
                    {
                        c = new Case { day = day.day, scenario = v.Scenario };
                        list.Add(c);
                    }
                    c.truths.Add(v);
                }
            }
            foreach (var c in list)
                c.truths.Sort((a, b) => b.IsScam.CompareTo(a.IsScam));
            return list;
        }

        public static Case Find(DayCatalog catalog, int day, string scenario) => All(catalog).Find(c => c.day == day && c.scenario == scenario);

        /// <summary>Truths seen over all cases, and how many there are.</summary>
        public static (int seen, int total) Count(DayCatalog catalog)
        {
            int seen = 0, total = 0;
            foreach (var c in All(catalog))
            {
                seen += c.SeenCount;
                total += c.truths.Count;
            }
            return (seen, total);
        }

        /// <summary>The Cases page of Case Files: each case with its truths, face up when seen.</summary>
        public static VisualElement Page(DayCatalog catalog)
        {
            var page = UIKit.Div("casebook");
            int lastDay = -1;
            foreach (var c in All(catalog))
            {
                if (c.day != lastDay)
                {
                    lastDay = c.day;
                    var day = catalog.Get(c.day);
                    page.Add(UIKit.Text(Loc.F("DAY {0}  ·  {1}", c.day, day != null ? day.dateLabel : ""), "casebook__day"));
                }
                page.Add(Row(c));
            }
            page.Add(UIKit.Text(Loc.T("Every case has a twin: the same caller, the same words, the other truth. Play a day again to meet it."), "casebook__note"));
            return page;
        }

        static VisualElement Row(Case c)
        {
            int seen = c.SeenCount;
            var row = UIKit.Div("casebook__row", seen == 0 ? "casebook__row--unseen" : null);
            row.Add(UIKit.Portrait(seen > 0 ? c.Portrait : "pt_unknown", "casebook__portrait"));
            row.Add(UIKit.Text(seen > 0 ? c.Title : Loc.T("A case you haven't had yet"), "casebook__title"));
            var tokens = UIKit.Div("casebook__truths");
            foreach (var v in c.truths)
            {
                if (c.Seen(v))
                    tokens.Add(UIKit.Text(Loc.T(v.IsScam ? "SCAM" : "REAL"), "casebook__truth", v.IsScam ? "casebook__truth--scam" : "casebook__truth--real"));
                else
                    tokens.Add(UIKit.Text("?", "casebook__truth", "casebook__truth--down"));
            }
            row.Add(tokens);
            string count = c.truths.Count == 1 ? Loc.T(seen > 0 ? "Its only truth, seen" : "One truth") : Loc.F("{0} of {1} truths seen", seen, c.truths.Count);
            row.Add(UIKit.Text(count, "casebook__count"));
            return row;
        }
    }

    /// <summary>
    /// The trick cards: one per way the callers lie (<see cref="TrickDef"/>, in the catalog). A
    /// card is won by stamping a false claim that uses it FALSE (<see cref="GameRun.CatchTrick"/>)
    /// and kept across weeks; the ones not won yet show as face-down silhouettes.
    /// </summary>
    public static class TrickCards
    {
        /// <summary>The Trick cards page of Case Files: every card, won or face down.</summary>
        public static VisualElement Page(DayCatalog catalog)
        {
            var page = UIKit.Div("tricks");
            var grid = UIKit.Div("tricks__grid");
            if (catalog != null)
                for (int i = 0; i < catalog.tricks.Count; i++)
                    grid.Add(Card(catalog, i));
            page.Add(grid);
            page.Add(UIKit.Text(Loc.T("Stamp a false claim FALSE in your notebook to win the card of its trick."), "casebook__note"));
            return page;
        }

        public static (int won, int total) Count(DayCatalog catalog)
        {
            if (catalog == null)
                return (0, 0);
            int won = 0;
            foreach (var t in catalog.tricks)
                if (GameRun.Trick(t.id) != null)
                    won++;
            return (won, catalog.tricks.Count);
        }

        /// <summary>One card: its number, name, how it works, the tell and where it was caught; or face down.</summary>
        public static VisualElement Card(DayCatalog catalog, int index)
        {
            var t = catalog.tricks[index];
            var caught = GameRun.Trick(t.id);
            var card = UIKit.Div("trick-card", caught == null ? "trick-card--down" : null);
            card.Add(UIKit.Text(Loc.F("TRICK #{0:00}", index + 1), "trick-card__no"));
            if (caught == null)
            {
                card.Add(UIKit.Text("?", "trick-card__mark"));
                card.Add(UIKit.Text(Loc.T("Not caught yet"), "trick-card__hint"));
                return card;
            }
            card.Add(UIKit.Text(t.name, "trick-card__name"));
            card.Add(UIKit.Text(t.how, "trick-card__how"));
            card.Add(UIKit.Text(Loc.T("THE TELL"), "trick-card__label"));
            card.Add(UIKit.Text(t.tell, "trick-card__tell"));
            var found = CaseBook.Find(catalog, caught.day, caught.scenario);
            card.Add(UIKit.Text(found != null ? Loc.F("Caught on Day {0}  ·  {1}", caught.day, found.Title) : Loc.F("Caught on Day {0}", caught.day), "trick-card__caught"));
            return card;
        }

        /// <summary>The next morning: the cards won today, small, under the red pen.</summary>
        public static VisualElement Won(DayCatalog catalog, IReadOnlyList<string> ids)
        {
            if (catalog == null || ids == null || ids.Count == 0)
                return null;
            var box = UIKit.Div("trick-won");
            box.Add(UIKit.Text(Loc.T(ids.Count == 1 ? "NEW TRICK CARD" : "NEW TRICK CARDS"), "trick-won__tag"));
            var names = UIKit.Div("trick-won__names");
            foreach (string id in ids)
            {
                var t = catalog.Trick(id);
                if (t == null)
                    continue;
                var chip = UIKit.Text(t.name, "trick-won__name");
                chip.tooltip = t.tell;
                names.Add(chip);
            }
            box.Add(names);
            box.Add(UIKit.Text(Loc.T("in Case Files"), "trick-won__where"));
            return box;
        }
    }
}
