using System;
using System.Linq;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// Always-on HUD: day and clock tag, control hints, the phone button with the notification
    /// badge, the interact prompt at the cursor and toast notifications.
    /// </summary>
    public class HudView
    {
        public VisualElement Root { get; }
        public VisualElement Toasts { get; }

        /// <summary>The caller's deadline, under the clock, while a case is being investigated.</summary>
        public DeadlineView Deadline { get; }

        readonly Label day;
        readonly Label time;
        readonly Label badge;
        readonly VisualElement prompt;
        readonly Label promptKey;
        readonly Label promptText;
        readonly VisualElement hints;
        readonly VisualElement buttons;

        public HudView(Action onPhone)
        {
            Root = UIKit.Div();
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.top = 0;
            Root.style.right = 0;
            Root.style.bottom = 0;
            Root.pickingMode = PickingMode.Ignore;

            var tag = UIKit.Div("daytag");
            tag.pickingMode = PickingMode.Ignore;
            day = UIKit.Text(GameClock.DayLabel, "daytag__day");
            time = UIKit.Text("16:20", "daytag__time");
            tag.Add(day);
            tag.Add(time);
            IgnorePointer(tag);
            Root.Add(tag);

            Deadline = new DeadlineView();
            Root.Add(Deadline.Root);

            hints = UIKit.Div("controls-hint");
            hints.pickingMode = PickingMode.Ignore;
            foreach (string h in new[] { "WASD  walk", "Drag  look", "Click  interact", "Tab  phone", "Esc  back" }.Select(Loc.T))
                hints.Add(UIKit.Text(h));
            IgnorePointer(hints);
            Root.Add(hints);

            var hud = UIKit.Div("hud");
            var phone = UIKit.IconBtn("ic_phone", onPhone, "hud-btn");
            phone.tooltip = Loc.T("Phone (Tab)");
            phone.Add(UIKit.Text("Tab", "hud-btn__key"));
            badge = UIKit.Text("", "badge");
            badge.pickingMode = PickingMode.Ignore;
            phone.Add(badge);
            hud.Add(phone);
            Root.Add(hud);
            buttons = hud;

            prompt = UIKit.Div("prompt");
            prompt.pickingMode = PickingMode.Ignore;
            promptKey = UIKit.Text(Loc.T("Click"), "prompt__key");
            promptText = UIKit.Text("", "prompt__text");
            prompt.Add(promptKey);
            prompt.Add(promptText);
            IgnorePointer(prompt);
            prompt.Hide(true);
            Root.Add(prompt);

            Toasts = UIKit.Div("toasts");
            Toasts.pickingMode = PickingMode.Ignore;
            Root.Add(Toasts);
        }

        public void SetTime(string now) => time.text = now;

        public void SetDayLabel(string label) => day.text = label;

        public void SetDay(string label) => day.text = label;

        public void SetBadge(int n)
        {
            badge.text = n.ToString();
            badge.Hide(n <= 0);
        }

        public void SetHintsVisible(bool on) => hints.Hide(!on);

        /// <summary>The phone button steps aside while the phone is in Jiwoo's hand (Tab, Esc or a click on the room lowers it).</summary>
        public void SetButtonsVisible(bool on) => buttons.Hide(!on);

        public void ShowPrompt(string text, Vector2 panelPosition)
        {
            promptText.text = text;
            prompt.style.left = panelPosition.x + 26;
            prompt.style.top = panelPosition.y + 20;
            prompt.Hide(false);
        }

        public void HidePrompt() => prompt.Hide(true);

        /// <summary>Informative text: presses go through it to the room.</summary>
        static void IgnorePointer(VisualElement e) => e.Query().ForEach(c => c.pickingMode = PickingMode.Ignore);

        public void Toast(string icon, string title, string text, Action onClick = null)
        {
            var t = UIKit.Div("toast");
            t.Add(UIKit.Icon(icon, "toast__icon"));
            var col = UIKit.Div("grow");
            col.style.flexShrink = 1;
            col.Add(UIKit.Text(title, "toast__title"));
            col.Add(UIKit.Text(text.Length > 90 ? text.Substring(0, 90) + "…" : text, "toast__text"));
            t.Add(col);
            if (onClick != null)
                t.RegisterCallback<ClickEvent>(e =>
                {
                    onClick();
                    t.RemoveFromHierarchy();
                    e.StopPropagation();
                });
            else
                t.Query().ForEach(e => e.pickingMode = PickingMode.Ignore);
            Toasts.Insert(0, t);
            t.schedule.Execute(() => t.AddToClassList("toast--on")).ExecuteLater(20);
            t.schedule.Execute(() => t.RemoveFromClassList("toast--on")).ExecuteLater(4200);
            t.schedule.Execute(() => t.RemoveFromHierarchy()).ExecuteLater(4600);
            while (Toasts.childCount > 4)
                Toasts.RemoveAt(Toasts.childCount - 1);
        }
    }

    /// <summary>
    /// The tutorial day's guide: a yellow note at the left edge with the step number, what the last
    /// thing checked showed (in green, with a tick) and one instruction. It never takes the pointer.
    /// </summary>
    public class TutorialCard
    {
        public VisualElement Root { get; }

        readonly Label count;
        readonly VisualElement learnedRow;
        readonly Label learned;
        readonly Label text;

        public TutorialCard()
        {
            Root = UIKit.Div("tutorial");
            var head = UIKit.Div("tutorial__head");
            head.Add(UIKit.Icon("ic_star", "tutorial__icon"));
            head.Add(UIKit.Text(Loc.T("HOW TO PLAY"), "tutorial__title"));
            count = UIKit.Text("", "tutorial__count");
            head.Add(count);
            Root.Add(head);
            learnedRow = UIKit.Div("tutorial__learned");
            learnedRow.Add(UIKit.Icon("ic_check", "tutorial__learned-icon"));
            learned = UIKit.Text("", "tutorial__learned-text");
            learnedRow.Add(learned);
            learnedRow.Hide(true);
            Root.Add(learnedRow);
            text = UIKit.Text("", "tutorial__text");
            Root.Add(text);
            Root.pickingMode = PickingMode.Ignore;
            Root.Query().ForEach(c => c.pickingMode = PickingMode.Ignore);
        }

        /// <summary>Shows a step; the card pops when the instruction changes.</summary>
        /// <param name="found">What the place just checked showed, or null.</param>
        public void Show(int step, int total, string found, string instruction)
        {
            bool changed = text.text != instruction;
            count.text = $"{step}/{total}";
            text.text = instruction;
            learned.text = found ?? "";
            learnedRow.Hide(string.IsNullOrEmpty(found));
            Root.AddToClassList("tutorial--on");
            if (!changed)
                return;
            Root.AddToClassList("tutorial--pop");
            Root.schedule.Execute(() => Root.RemoveFromClassList("tutorial--pop")).ExecuteLater(260);
        }

        public void Hide() => Root.RemoveFromClassList("tutorial--on");

        /// <summary>The note is on screen: room panels open beside it, not under it.</summary>
        public bool IsOn => Root.ClassListContains("tutorial--on");
    }

    /// <summary>A yes/no question on paper, e.g. "Restart today's case?"</summary>
    public class ModalView
    {
        public VisualElement Root { get; }

        public ModalView(string title, string text, string yes, string no, Action onYes, Action onNo)
        {
            Root = UIKit.Div("modal");
            var box = UIKit.Div("modal__box", "paper");
            box.Add(UIKit.Text(title, "modal__title"));
            box.Add(UIKit.Text(text, "modal__text"));
            var buttons = UIKit.Div("modal__buttons");
            if (!string.IsNullOrEmpty(no))
                buttons.Add(UIKit.Btn(no, onNo));
            buttons.Add(UIKit.Btn(yes, onYes, "btn--green"));
            box.Add(buttons);
            Root.Add(box);
        }
    }
}
