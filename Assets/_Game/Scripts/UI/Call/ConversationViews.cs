using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>What the notebook's Case tab shows: who is calling, their claims and every fact heard.</summary>
    public class CaseFile
    {
        public CallerInfo caller;
        public string callerTitle;
        public readonly List<string> claims = new List<string>();
        public readonly List<Fact> facts = new List<Fact>();

        public void AddFact(Fact f)
        {
            if (f == null || string.IsNullOrEmpty(f.value))
                return;
            if (facts.Exists(x => x.value == f.value))
                return;
            facts.Add(f);
        }
    }

    /// <summary>
    /// The call transcript next to the phone (references/UI/refUI1): caller lines with portrait and
    /// time on the left, the player's on the right, a typing indicator, fact chips, and at the bottom
    /// either the idle "…" bar or the two responses with the decision timer.
    /// </summary>
    public class TranscriptPanel
    {
        public VisualElement Root { get; }

        readonly VisualElement headerPortrait;
        readonly Label nameLabel;
        readonly Label numberLabel;
        readonly Label recTime;
        readonly ScrollView log;
        readonly VisualElement footer;
        VisualElement typingRow;
        DecisionTimer timer;
        Action<int> pickHandler;
        string callerPortrait = "pt_unknown";

        public bool HasDecision => pickHandler != null;

        public TranscriptPanel()
        {
            Root = UIKit.Div("transcript");
            var header = UIKit.Div("transcript__header");
            headerPortrait = UIKit.Portrait("pt_unknown");
            header.Add(headerPortrait);
            var who = UIKit.Div("transcript__who");
            nameLabel = UIKit.Text("Unknown", "transcript__name");
            numberLabel = UIKit.Text("", "transcript__number");
            who.Add(nameLabel);
            who.Add(numberLabel);
            header.Add(who);
            var rec = UIKit.Div("rec");
            rec.Add(UIKit.Div("rec__dot"));
            recTime = UIKit.Text("00:00", "rec__time");
            rec.Add(recTime);
            header.Add(rec);
            Root.Add(header);

            log = new ScrollView(ScrollViewMode.Vertical);
            log.AddToClassList("transcript__log");
            log.contentContainer.AddToClassList("transcript__content");
            log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(log);

            footer = UIKit.Div("decide");
            Root.Add(footer);
            ShowIdle();
        }

        public void Open(CallerInfo caller, string title)
        {
            log.Clear();
            callerPortrait = string.IsNullOrEmpty(caller.portrait) ? "pt_unknown" : caller.portrait;
            UIKit.SetImage(headerPortrait, UISkin.Tex(callerPortrait));
            nameLabel.text = title;
            numberLabel.text = caller.number;
            recTime.text = "00:00";
            ShowIdle();
        }

        public void SetVisible(bool on) => Root.EnableInClassList("transcript--open", on);

        public void SetCallTime(string t) => recTime.text = t;

        public void SetCaller(string title, string number, string portrait)
        {
            nameLabel.text = title;
            numberLabel.text = number;
            callerPortrait = portrait;
            UIKit.SetImage(headerPortrait, UISkin.Tex(portrait));
        }

        public void AddLine(Speaker speaker, string text, List<Fact> facts = null, string portrait = null)
        {
            SetTyping(false);
            VisualElement row;
            if (speaker == Speaker.System)
            {
                row = UIKit.Div("line", "line--system");
                row.Add(UIKit.Text(text, "system-tag"));
            }
            else
            {
                bool player = speaker == Speaker.Player;
                row = UIKit.Div("line", player ? "line--player" : null);
                row.Add(UIKit.Portrait(player ? "pt_jiwoo" : portrait ?? callerPortrait));
                var col = UIKit.Div("line__col");
                var bubble = UIKit.Div("bubble");
                bubble.Add(UIKit.Text(text, "bubble__text"));
                if (facts != null && facts.Count > 0)
                {
                    var chips = UIKit.Div("chip-row");
                    foreach (var f in facts)
                        chips.Add(UIKit.Chip(f));
                    bubble.Add(chips);
                }
                col.Add(bubble);
                var time = UIKit.Text(GameClock.Now, "line__time");
                if (player)
                    time.style.alignSelf = Align.FlexEnd;
                col.Add(time);
                row.Add(col);
            }
            log.Add(row);
            row.schedule.Execute(() => row.AddToClassList("line--shown")).ExecuteLater(20);
            UIKit.ScrollToEnd(log);
        }

        public void AddSystem(string text, string kind = null)
        {
            var row = UIKit.Div("line", "line--system");
            row.Add(UIKit.Text(text, "system-tag", kind == null ? null : "system-tag--" + kind));
            log.Add(row);
            row.schedule.Execute(() => row.AddToClassList("line--shown")).ExecuteLater(20);
            UIKit.ScrollToEnd(log);
        }

        public void SetTyping(bool on, string portrait = null)
        {
            if (on && typingRow == null)
            {
                typingRow = UIKit.Div("line", "line--shown");
                typingRow.Add(UIKit.Portrait(portrait ?? callerPortrait));
                var bubble = UIKit.Div("bubble");
                bubble.Add(new TypingDots());
                typingRow.Add(bubble);
                log.Add(typingRow);
                UIKit.ScrollToEnd(log);
            }
            else if (!on && typingRow != null)
            {
                typingRow.RemoveFromHierarchy();
                typingRow = null;
            }
        }

        public void ShowDecision(ConvDecision d, Action<int> pick)
        {
            footer.Clear();
            pickHandler = pick;
            var options = UIKit.Div("decide__options");
            options.Add(UIKit.Text(string.IsNullOrEmpty(d.prompt) ? "Your answer" : d.prompt, "decide__prompt"));
            var opts = new[] { d.a, d.b };
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var b = new Button(() => Pick(index)) { focusable = false };
                b.AddToClassList("btn");
                b.AddToClassList("option");
                b.RegisterCallback<ClickEvent>(_ => Sfx.Play(Sfx.Click, 0.5f));
                b.Add(UIKit.Text((i + 1).ToString(), "option__key"));
                b.Add(UIKit.Text(opts[i].label, "option__text"));
                options.Add(b);
            }
            footer.Add(options);
            timer = new DecisionTimer();
            footer.Add(timer);
            UIKit.ScrollToEnd(log);
        }

        public void UpdateTimer(float remaining, float total) => timer?.Set(remaining, total);

        public bool Pick(int index)
        {
            if (pickHandler == null)
                return false;
            var p = pickHandler;
            ShowIdle();
            p(index);
            return true;
        }

        public void ShowIdle()
        {
            pickHandler = null;
            timer = null;
            footer.Clear();
            var idle = UIKit.Div("decide__idle");
            idle.Add(UIKit.Text("• • •", "decide__idle-dots"));
            idle.Add(UIKit.Div("decide__send"));
            footer.Add(idle);
        }
    }

    /// <summary>Compact call bar at the top of the screen while the phone is down.</summary>
    public class CallHud
    {
        public VisualElement Root { get; }
        readonly VisualElement portrait;
        readonly Label name;
        readonly Label line;
        readonly Label hint;
        readonly DecisionTimer timer;

        public CallHud()
        {
            Root = UIKit.Div("call-hud");
            portrait = UIKit.Portrait("pt_unknown");
            Root.Add(portrait);
            var text = UIKit.Div("call-hud__text");
            name = UIKit.Text("", "call-hud__name");
            line = UIKit.Text("", "call-hud__line");
            hint = UIKit.Text("", "call-hud__hint");
            text.Add(name);
            text.Add(line);
            text.Add(hint);
            Root.Add(text);
            timer = new DecisionTimer(true);
            Root.Add(timer);
            timer.Hide(true);
            Root.pickingMode = PickingMode.Ignore;
        }

        public void Set(string title, string number, string portraitId)
        {
            name.text = $"{title}  <color=#5A4E54>{number}</color>";
            UIKit.SetImage(portrait, UISkin.Tex(portraitId));
        }

        public void SetLine(string text) => line.text = text;

        public void SetVisible(bool on) => Root.EnableInClassList("call-hud--on", on);

        public void SetDecision(bool pending, float remaining, float total)
        {
            timer.Hide(!pending);
            hint.text = pending ? "Decision waiting  ·  press Tab" : "";
            if (pending)
                timer.Set(remaining, total);
        }
    }

    /// <summary>Red edges that close in as patience runs out, pulsing with a heartbeat at the end.</summary>
    public class PressureOverlay
    {
        public VisualElement Root { get; }
        float beat;
        float current;

        public PressureOverlay()
        {
            Root = UIKit.Div("pressure");
            Root.pickingMode = PickingMode.Ignore;
        }

        /// <param name="patience">1 = relaxed, 0 = out of time; negative hides the overlay.</param>
        public void Tick(float patience, float dt)
        {
            float target = patience < 0f ? 0f : Mathf.Clamp01((0.6f - patience) / 0.6f);
            current = Mathf.MoveTowards(current, target, dt * 1.5f);
            float o = current * 0.8f;
            if (patience >= 0f && patience < 0.25f)
            {
                beat += dt;
                if (beat > 0.95f)
                {
                    beat = 0f;
                    Sfx.Play(Sfx.Heart, 0.9f);
                }
                o += 0.15f * Mathf.Exp(-beat * 6f);
            }
            Root.style.opacity = o;
        }
    }
}
