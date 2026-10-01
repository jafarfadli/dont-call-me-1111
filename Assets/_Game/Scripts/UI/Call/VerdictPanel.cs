using System;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// YOUR VERDICT: the only place a case is decided. Two colour-coded choices (go along gold,
    /// refuse red; the colours follow the kind of verdict, never whether it is right), each
    /// confirmed on a second step: the transfer with the recipient's name, or hanging up.
    /// </summary>
    public class VerdictPanel
    {
        public VisualElement Root { get; }

        readonly VerdictInfo info;
        readonly string goAlongDetail;
        readonly string sendTitle;
        readonly string sendTo;
        readonly Action<VerdictKind> chosen;
        bool done;

        /// <summary>The send confirmation is on screen (it shows the recipient's name, like the bank's own check).</summary>
        public event Action SendShown;

        public VerdictPanel(VerdictInfo info, string goAlongDetail, string sendTitle, string sendTo, Action<VerdictKind> chosen)
        {
            this.info = info ?? new VerdictInfo();
            this.goAlongDetail = goAlongDetail;
            this.sendTitle = sendTitle;
            this.sendTo = sendTo;
            this.chosen = chosen;
            Root = UIKit.Div("verdict");
            ShowChoices();
        }

        void Head(string title, string cls = null)
        {
            var head = UIKit.Div("verdict__head", cls);
            head.Add(UIKit.Icon("ic_case", "verdict__head-icon"));
            head.Add(UIKit.Text(title, "verdict__title"));
            Root.Add(head);
        }

        void ShowChoices()
        {
            Root.Clear();
            Root.RemoveFromClassList("verdict--confirm");
            Head("YOUR VERDICT");
            var row = UIKit.Div("verdict__row");
            row.Add(Choice("verdict-btn--gold", "btn--amber", Glyph("₩"), info.goAlong, goAlongDetail, ShowSend));
            row.Add(Choice("verdict-btn--red", "btn--red", UIKit.Icon("ic_hangup", "verdict-btn__icon"), info.refuse, info.refuseDetail, ShowHangUp));
            Root.Add(row);
        }

        static VisualElement Glyph(string text)
        {
            var g = UIKit.Text(text, "verdict-btn__glyph");
            g.pickingMode = PickingMode.Ignore;
            return g;
        }

        static Button Choice(string kindClass, string colour, VisualElement icon, string label, string detail, Action onClick)
        {
            var b = UIKit.Btn("", onClick, "verdict-btn", kindClass, colour);
            b.Add(icon);
            var text = UIKit.Div("verdict-btn__text");
            text.pickingMode = PickingMode.Ignore;
            text.Add(UIKit.Text(label, "verdict-btn__label"));
            if (!string.IsNullOrEmpty(detail))
                text.Add(UIKit.Text(detail, "verdict-btn__detail"));
            b.Add(text);
            b.Query<Label>().ForEach(l => l.pickingMode = PickingMode.Ignore);
            return b;
        }

        // ---------------------------------------------------------------- confirmations

        void Confirm(string colour, string title, string line1, string line2, string action, string actionColour, Action onConfirm)
        {
            Root.Clear();
            Root.AddToClassList("verdict--confirm");
            Head(title, colour);
            if (!string.IsNullOrEmpty(line1))
                Root.Add(UIKit.Text(line1, "verdict__line"));
            if (!string.IsNullOrEmpty(line2))
                Root.Add(UIKit.Text(line2, "verdict__sub"));
            var buttons = UIKit.Div("verdict__buttons");
            buttons.Add(UIKit.Btn("Back", ShowChoices, "verdict__back"));
            buttons.Add(UIKit.Btn(action, onConfirm, "verdict__go", actionColour));
            Root.Add(buttons);
        }

        void ShowSend()
        {
            Confirm("verdict__head--gold", sendTitle, sendTo, "The money leaves your account as soon as you confirm.",
                    info.goAlong, "btn--amber", () => Choose(VerdictKind.GoAlong));
            SendShown?.Invoke();
        }

        void ShowHangUp() => Confirm("verdict__head--red", "HANG UP?", "You end the call and send nothing.", null,
                                     info.refuse, "btn--red", () => Choose(VerdictKind.Refuse));

        void Choose(VerdictKind kind)
        {
            if (done)
                return;
            done = true;
            Root.SetEnabled(false);
            chosen?.Invoke(kind);
        }
    }
}
