using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The black card that opens a day: DAY 1, the date, time and place, a line of intro and what
    /// is still on Jiwoo's mind from the days before. Click (or wait) to continue; it fades into the room.
    /// </summary>
    public class DayCardView
    {
        public VisualElement Root { get; }
        Action onDone;
        bool closing;

        public DayCardView(DayData day, IReadOnlyList<string> extraLines, Action onDone)
        {
            this.onDone = onDone;
            Root = UIKit.Div("daycard");
            var inner = UIKit.Div("daycard__inner");
            inner.Add(UIKit.Text($"DAY {day.day}", "daycard__day"));
            inner.Add(UIKit.Div("daycard__rule"));
            inner.Add(UIKit.Text(day.dateLabel, "daycard__date"));
            inner.Add(UIKit.Text($"{day.startTime}  ·  {day.place}", "daycard__place"));
            if (!string.IsNullOrEmpty(day.intro))
                inner.Add(UIKit.Text(day.intro, "daycard__intro"));
            if (extraLines != null)
                foreach (string line in extraLines)
                    inner.Add(UIKit.Text(line, "daycard__echo"));
            inner.Add(UIKit.Text(Loc.T("click to continue"), "daycard__hint"));
            Root.Add(inner);
            Root.RegisterCallback<ClickEvent>(_ => Close());
            int k = 0;
            foreach (var child in inner.Children())
            {
                var c = child;
                c.schedule.Execute(() => c.AddToClassList("daycard--in")).ExecuteLater(250 + 260 * k++);
            }
        }

        public void Close()
        {
            if (closing)
                return;
            closing = true;
            Root.AddToClassList("daycard--out");
            Root.schedule.Execute(() =>
            {
                Root.RemoveFromHierarchy();
                onDone?.Invoke();
                onDone = null;
            }).ExecuteLater(900);
        }
    }

    /// <summary>
    /// CASE OPENED: a paper case file that lands on screen once the caller has made the ask:
    /// who they say they are, what they want, the deadline and the player's job.
    /// </summary>
    public class CaseCardView
    {
        public VisualElement Root { get; }

        public CaseCardView(int day, CaseInfo info, CallerInfo caller, string callerTitle, Action onStart)
        {
            Root = UIKit.Div("modal", "case-shade");
            var card = UIKit.Div("case-file", "paper");
            var tab = UIKit.Div("case-file__tab");
            tab.Add(UIKit.Icon("ic_case", "case-file__tab-icon"));
            tab.Add(UIKit.Text(Loc.F("CASE #{0:00}", day), "case-file__tab-text"));
            card.Add(tab);

            var head = UIKit.Div("case-file__head");
            head.Add(UIKit.Portrait(caller.portrait, "case-file__portrait"));
            var who = UIKit.Div("grow");
            who.Add(UIKit.Text(Loc.T("CASE OPENED"), "section"));
            who.Add(UIKit.Text(info.caseTitle, "case-file__title"));
            who.Add(UIKit.Text($"{callerTitle} · {caller.number}", "case-file__caller"));
            head.Add(who);
            card.Add(head);

            card.Add(Row(Loc.T("Says they are"), info.claimedIdentity));
            card.Add(Row(Loc.T("Wants you to"), info.ask));
            card.Add(Row(Loc.T("Deadline"), string.IsNullOrEmpty(info.deadlineReason) ? info.deadline : $"{info.deadline} · {info.deadlineReason}", "case-file__value--red"));

            var job = UIKit.Div("case-file__job");
            job.Add(UIKit.Text(Loc.T("YOUR JOB"), "case-file__job-title"));
            job.Add(UIKit.Text(info.objective, "case-file__job-text"));
            card.Add(job);

            var stamp = UIKit.Text(Loc.T("OPEN"), "stamp", "stamp--red", "case-file__stamp");
            stamp.style.whiteSpace = WhiteSpace.NoWrap;
            stamp.style.rotate = new Rotate(-9f);
            stamp.schedule.Execute(() =>
            {
                stamp.AddToClassList("stamp--down");
                Sfx.Play(Sfx.Stamp);
            }).ExecuteLater(650);
            card.Add(stamp);

            var buttons = UIKit.Div("modal__buttons");
            buttons.Add(UIKit.Btn(Loc.T("Start investigating"), () =>
            {
                Root.RemoveFromHierarchy();
                onStart?.Invoke();
            }, "btn--green"));
            card.Add(buttons);
            Root.Add(card);
            card.schedule.Execute(() => card.AddToClassList("case-file--in")).ExecuteLater(30);
            Sfx.Play(Sfx.Paper);
        }

        static VisualElement Row(string label, string value, string valueClass = null)
        {
            var row = UIKit.Div("case-file__row");
            row.Add(UIKit.Text(label, "case-file__label"));
            row.Add(UIKit.Text(value, "case-file__value", valueClass));
            return row;
        }
    }

    /// <summary>
    /// The caller's deadline under the clock: "17:00 · 12 min left" and a draining bar. Amber at
    /// half time, red and pulsing in the last quarter.
    /// </summary>
    public class DeadlineView
    {
        public VisualElement Root { get; }
        readonly Label label;
        readonly Label left;
        readonly VisualElement fill;
        float pulse;

        public DeadlineView()
        {
            Root = UIKit.Div("deadline");
            Root.pickingMode = PickingMode.Ignore;
            var top = UIKit.Div("deadline__top");
            top.Add(UIKit.Icon("ic_clock", "deadline__icon"));
            label = UIKit.Text("", "deadline__label");
            top.Add(label);
            left = UIKit.Text("", "deadline__left");
            top.Add(left);
            Root.Add(top);
            var bar = UIKit.Div("deadline__bar");
            fill = UIKit.Div("deadline__fill");
            bar.Add(fill);
            Root.Add(bar);
            Root.Query().ForEach(e => e.pickingMode = PickingMode.Ignore);
            Root.Hide(true);
        }

        public void Show(bool on) => Root.Hide(!on);

        /// <param name="fraction">1 at the start of the investigation, 0 at the deadline.</param>
        public void Set(string deadline, float minutesLeft, float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            label.text = Loc.F("Deadline {0}", deadline);
            int m = Mathf.CeilToInt(Mathf.Max(0f, minutesLeft));
            left.text = m <= 1 ? Loc.T("1 min left!") : Loc.F("{0} min left", m);
            fill.style.width = Length.Percent(fraction * 100f);
            Root.EnableInClassList("deadline--amber", fraction < 0.5f && fraction >= 0.25f);
            Root.EnableInClassList("deadline--red", fraction < 0.25f);
            if (fraction < 0.25f)
            {
                pulse += Time.deltaTime * 4f;
                Root.style.scale = new Scale(Vector2.one * (1f + 0.035f * Mathf.Abs(Mathf.Sin(pulse))));
            }
            else
            {
                Root.style.scale = StyleKeyword.Null;
            }
        }
    }

    /// <summary>A black screen over everything, for scene changes.</summary>
    public class Fader
    {
        public VisualElement Root { get; }

        public Fader(bool startBlack)
        {
            Root = UIKit.Div("fader");
            Root.pickingMode = PickingMode.Ignore;
            Root.style.opacity = startBlack ? 1f : 0f;
        }

        public void FadeIn(float seconds = 1f) => To(0f, seconds, null);

        public void FadeOut(float seconds, Action done) => To(1f, seconds, done);

        void To(float target, float seconds, Action done)
        {
            Root.pickingMode = target > 0f ? PickingMode.Position : PickingMode.Ignore;
            float from = Root.resolvedStyle.opacity;
            float t = 0f;
            IVisualElementScheduledItem item = null;
            item = Root.schedule.Execute(() =>
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds);
                Root.style.opacity = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                if (t < 1f)
                    return;
                item.Pause();
                done?.Invoke();
            }).Every(16);
        }
    }
}
