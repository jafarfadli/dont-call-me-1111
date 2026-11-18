using System.Collections.Generic;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The case notebook at the left edge, from the moment the call is answered: the caller, what
    /// they want and by when; then each claim as the caller makes it, with a grey "?" until the
    /// player stamps it TRUE or FALSE, and under it what the player circled about it. N folds it.
    /// On a Write day it starts with three empty lines that the player fills by clicking the
    /// caller's lines in the transcript (<see cref="Pinnable"/>).
    /// </summary>
    public class NotebookView
    {
        public VisualElement Root { get; }
        public bool Folded { get; private set; }

        readonly CaseNotebook notebook;
        readonly VisualElement body;
        readonly VisualElement claimsBox;
        readonly Label empty;
        readonly Label full;
        readonly Label foldKey;
        readonly Dictionary<string, List<MarkPlace>> hints;
        NoteEntry fresh;
        IVisualElementScheduledItem fullItem;

        public NotebookView(CaseNotebook notebook, int day, CaseInfo info, CallerInfo caller, string callerTitle, Dictionary<string, List<MarkPlace>> hints)
        {
            this.notebook = notebook;
            this.hints = hints ?? new Dictionary<string, List<MarkPlace>>();
            Root = UIKit.Div("notebook");

            var head = UIKit.Div("notebook__head");
            head.Add(UIKit.Text(Loc.F("CASE #{0:00}", day), "notebook__tab"));
            head.Add(UIKit.Text(Loc.T("NOTEBOOK"), "notebook__label"));
            foldKey = UIKit.Text("N", "notebook__key");
            head.Add(foldKey);
            head.RegisterCallback<ClickEvent>(e =>
            {
                Toggle();
                e.StopPropagation();
            });
            head.tooltip = Loc.T("Fold or open the notebook (N)");
            Root.Add(head);

            body = UIKit.Div("notebook__body");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("notebook__scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            var c = scroll.contentContainer;
            c.Add(UIKit.Text(info.caseTitle, "notebook__title"));
            var facts = UIKit.Div("notebook__facts");
            facts.Add(Fact(Loc.T("Caller"), $"{callerTitle} · {caller.number}"));
            facts.Add(Fact(Loc.T("Wants"), info.ask));
            facts.Add(Fact(Loc.T("Deadline"), info.deadline));
            c.Add(facts);
            if (notebook.WritesOwn)
                c.Add(UIKit.Text(Loc.T("Write down what the caller claims: click their lines in the transcript."), "notebook__write"));
            claimsBox = UIKit.Div("notebook__claims");
            c.Add(claimsBox);
            empty = UIKit.Text(Loc.T("Listen: what the caller claims goes here."), "notebook__empty");
            c.Add(empty);
            full = UIKit.Text(Loc.T("Three lines at most. Take one out first."), "notebook__full");
            full.Hide(true);
            c.Add(full);
            c.Add(UIKit.Text(Loc.T("One false claim is enough to hang up."), "notebook__rule"));
            body.Add(scroll);
            Root.Add(body);

            notebook.Changed += Rebuild;
            notebook.EntryAdded += Flash;
            notebook.MarkFiled += f => Flash(notebook.EntryFor(f.def));
            notebook.Full += ShowFull;
            Root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                notebook.Changed -= Rebuild;
                notebook.EntryAdded -= Flash;
                notebook.Full -= ShowFull;
            });
            Rebuild();
        }

        /// <summary>A pin was refused: says why for a few seconds.</summary>
        void ShowFull()
        {
            if (Folded)
                SetFolded(false);
            full.Hide(false);
            fullItem?.Pause();
            fullItem = full.schedule.Execute(() => full.Hide(true));
            fullItem.ExecuteLater(3000);
        }

        static VisualElement Fact(string label, string value)
        {
            var row = UIKit.Div("notebook__fact");
            row.Add(UIKit.Text(label, "notebook__fact-label"));
            row.Add(UIKit.Text(value, "notebook__fact-value"));
            return row;
        }

        public void Toggle() => SetFolded(!Folded);

        public void SetFolded(bool folded)
        {
            Folded = folded;
            Root.EnableInClassList("notebook--folded", folded);
            Sfx.Play(Sfx.Paper, 0.4f);
        }

        /// <summary>The line just added or just given evidence pops for a moment.</summary>
        void Flash(NoteEntry entry)
        {
            if (entry == null)
                return;
            fresh = entry;
            Root.schedule.Execute(() =>
            {
                fresh = null;
                foreach (var e in claimsBox.Children())
                    e.RemoveFromClassList("nb-claim--fresh");
            }).ExecuteLater(700);
        }

        void Rebuild()
        {
            claimsBox.Clear();
            var entries = notebook.Entries;
            empty.Hide(entries.Count > 0 || notebook.WritesOwn);
            for (int i = 0; i < entries.Count; i++)
                claimsBox.Add(Entry(i + 1, entries[i]));
            if (notebook.WritesOwn)
                for (int i = entries.Count; i < CaseNotebook.MaxEntries; i++)
                    claimsBox.Add(Slot(i + 1, i == entries.Count));
            if (entries.Count < CaseNotebook.MaxEntries)
                full.Hide(true);
            var loose = notebook.Loose();
            if (loose.Count > 0)
            {
                var box = UIKit.Div("nb-claim", "nb-claim--loose");
                box.Add(UIKit.Text(Loc.T("Other notes"), "nb-claim__quote"));
                foreach (var f in loose)
                    box.Add(Filed(f));
                claimsBox.Add(box);
            }
        }

        /// <summary>
        /// One numbered line: the claim (or the pinned line), its stamp or "?", where to check it on
        /// easier days, what was circled for it and the stamp buttons. A pinned line that claims
        /// nothing checkable looks the same; nothing will ever be filed under it.
        /// </summary>
        VisualElement Entry(int number, NoteEntry entry)
        {
            string claimId = entry.claim?.id;
            var box = UIKit.Div("nb-claim");
            if (entry == fresh)
                box.AddToClassList("nb-claim--fresh");
            var top = UIKit.Div("nb-claim__top");
            top.Add(UIKit.Text(number.ToString(), "nb-claim__n"));
            top.Add(UIKit.Text(entry.text, "nb-claim__quote"));
            box.Add(top);

            var stamp = claimId != null ? notebook.StampOf(claimId) : Stamp.None;
            var mark = stamp == Stamp.True ? UIKit.Text(Loc.T("TRUE"), "stamp", "stamp--green", "stamp--down", "nb-claim__stamp")
                     : stamp == Stamp.False ? UIKit.Text(Loc.T("FALSE"), "stamp", "stamp--red", "stamp--down", "nb-claim__stamp")
                     : UIKit.Text("?", "nb-claim__unknown");
            mark.pickingMode = PickingMode.Ignore;
            mark.style.whiteSpace = WhiteSpace.NoWrap;
            box.Add(mark);

            bool hinted = notebook.help == NotebookHelp.Guided || notebook.help == NotebookHelp.Hints;
            if (hinted && claimId != null && hints.TryGetValue(claimId, out var places) && places.Count > 0)
            {
                var names = new List<string>();
                foreach (var p in places)
                    names.Add(PlaceName(p));
                box.Add(UIKit.Text(Loc.T("Check: ") + string.Join(", ", names), "nb-claim__hint"));
            }

            if (claimId != null)
                foreach (var f in notebook.FiledUnder(claimId))
                    box.Add(Filed(f));

            var row = UIKit.Div("nb-claim__stamps");
            bool can = claimId != null && notebook.CanStamp(claimId);
            row.Add(StampButton(claimId, Stamp.True, can));
            row.Add(StampButton(claimId, Stamp.False, can));
            var wait = can ? null : UIKit.Text(Loc.T("Circle something about it first"), "nb-claim__wait");
            if (!notebook.WritesOwn)
            {
                if (wait != null)
                    row.Add(wait);
                box.Add(row);
                return box;
            }
            // A Write day: Take out at the end of the row, so the waiting note gets a line of its own.
            var unpin = new Button(() =>
            {
                notebook.TogglePin(entry.line, null);
                Sfx.Play(Sfx.Paper, 0.4f);
            }) { focusable = false, text = Loc.T("Take out") };
            unpin.AddToClassList("nb-unpin");
            unpin.tooltip = Loc.T("Take this line out of your notebook");
            row.Add(UIKit.Div("grow"));
            row.Add(unpin);
            box.Add(row);
            if (wait != null)
            {
                wait.AddToClassList("nb-claim__wait--below");
                box.Add(wait);
            }
            return box;
        }

        /// <summary>An empty line on a Write day; the first one says how to fill it.</summary>
        static VisualElement Slot(int number, bool next)
        {
            var box = UIKit.Div("nb-claim", "nb-slot");
            var top = UIKit.Div("nb-claim__top");
            top.Add(UIKit.Text(number.ToString(), "nb-claim__n"));
            top.Add(UIKit.Text(next ? Loc.T("Click a line the caller says to write it here.") : " ", "nb-slot__text"));
            box.Add(top);
            box.Add(UIKit.Div("nb-slot__line"));
            return box;
        }

        Button StampButton(string claimId, Stamp stamp, bool enabled)
        {
            bool on = claimId != null && notebook.StampOf(claimId) == stamp;
            var b = new Button(() =>
            {
                notebook.SetStamp(claimId, stamp);
                Sfx.Play(Sfx.Stamp, 0.7f);
            }) { focusable = false, text = Loc.T(stamp == Stamp.True ? "TRUE" : "FALSE") };
            b.AddToClassList("nb-stamp-btn");
            b.AddToClassList(stamp == Stamp.True ? "nb-stamp-btn--true" : "nb-stamp-btn--false");
            b.EnableInClassList("nb-stamp-btn--on", on);
            b.SetEnabled(enabled);
            return b;
        }

        static VisualElement Filed(FiledMark f)
        {
            var pin = UIKit.Div("nb-pin");
            pin.Add(UIKit.Text(f.text, "nb-pin__text"));
            if (!string.IsNullOrEmpty(f.where))
                pin.Add(UIKit.Text(f.where, "nb-pin__where"));
            return pin;
        }

        /// <summary>A place as the notebook names it.</summary>
        public static string PlaceName(MarkPlace place) => place switch
        {
            MarkPlace.Newspaper => Loc.T("the newspaper"),
            MarkPlace.Drawer => Loc.T("the desk drawer"),
            MarkPlace.Calendar => Loc.T("the calendar"),
            MarkPlace.Computer => Loc.T("the computer"),
            MarkPlace.Chats => Loc.T("Chats"),
            MarkPlace.Bank => Loc.T("Nuri Bank"),
            MarkPlace.Contacts => Loc.T("Contacts"),
            MarkPlace.Video => Loc.T("the video call"),
            MarkPlace.Call => Loc.T("asking the caller"),
            _ => "",
        };
    }
}
