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
    /// the idle "…" bar, the two responses with the decision timer, or, while the caller holds the
    /// line, the questions the player can still ask. Voiced lines are revealed as they are spoken.
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
        readonly VisualElement voiceBars;
        VisualElement typingRow;
        DecisionTimer timer;
        Action<int> pickHandler;
        List<ConvQuestion> holdQuestions;
        Action<ConvQuestion> askHandler;
        string callerPortrait = "pt_unknown";
        bool shown;
        float speakingLeft;
        Label revealLabel;
        string revealText;
        IVisualElementScheduledItem revealItem;
        VisualElement holdBox;
        VisualElement questionsArea;
        VisualElement verdictArea;

        public bool HasDecision => pickHandler != null;
        public bool HasQuestions => askHandler != null && holdQuestions != null && holdQuestions.Count > 0;

        public TranscriptPanel()
        {
            Root = UIKit.Div("transcript");
            Root.style.visibility = Visibility.Hidden;
            var header = UIKit.Div("transcript__header");
            headerPortrait = UIKit.Portrait("pt_unknown");
            header.Add(headerPortrait);
            var who = UIKit.Div("transcript__who");
            nameLabel = UIKit.Text("Unknown", "transcript__name");
            numberLabel = UIKit.Text("", "transcript__number");
            who.Add(nameLabel);
            who.Add(numberLabel);
            header.Add(who);
            // Little level meter that moves while the other side is speaking.
            voiceBars = UIKit.Div("voice-bars");
            for (int i = 0; i < 5; i++)
                voiceBars.Add(UIKit.Div("voice-bars__bar"));
            voiceBars.pickingMode = PickingMode.Ignore;
            header.Add(voiceBars);
            voiceBars.schedule.Execute(AnimateVoice).Every(70);
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

        /// <summary>
        /// Fades the transcript in or out. Once faded out it is also hidden from the pointer:
        /// an invisible panel in the middle of the screen would otherwise swallow drags and clicks
        /// meant for the room.
        /// </summary>
        public void SetVisible(bool on)
        {
            if (on == shown)
                return;
            shown = on;
            if (on)
                Root.style.visibility = Visibility.Visible;
            Root.EnableInClassList("transcript--open", on);
            if (!on)
                Root.schedule.Execute(() =>
                {
                    if (!shown)
                        Root.style.visibility = Visibility.Hidden;
                }).ExecuteLater(260);
        }

        public void SetCallTime(string t) => recTime.text = t;

        public void SetCaller(string title, string number, string portrait)
        {
            nameLabel.text = title;
            numberLabel.text = number;
            callerPortrait = portrait;
            UIKit.SetImage(headerPortrait, UISkin.Tex(portrait));
        }

        /// <param name="reveal">Seconds over which the text appears, matching its voice clip; 0 shows it at once.</param>
        public void AddLine(Speaker speaker, string text, List<Fact> facts = null, string portrait = null, float reveal = 0f)
        {
            SetTyping(false);
            CompleteReveal();
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
                var label = UIKit.Text(text, "bubble__text");
                bubble.Add(label);
                if (reveal > 0f && !player)
                    Reveal(label, text, reveal);
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

        /// <summary>
        /// Types the text out over the clip's length. The unrevealed rest is kept (transparent) so
        /// the bubble does not grow line by line. Follows scaled time, so it waits while paused.
        /// </summary>
        void Reveal(Label label, string text, float seconds)
        {
            speakingLeft = Mathf.Max(speakingLeft, seconds);
            float elapsed = 0f;
            float span = Mathf.Max(0.3f, seconds * 0.9f);
            label.text = "<alpha=#00>" + text;
            revealLabel = label;
            revealText = text;
            // Timed by the scheduler's own clock (the callback does not run every frame), stopped while paused.
            revealItem = label.schedule.Execute(timer =>
            {
                elapsed += timer.deltaTime / 1000f * Time.timeScale;
                int n = Mathf.Clamp(Mathf.CeilToInt(text.Length * elapsed / span), 0, text.Length);
                label.text = n >= text.Length ? text : text.Substring(0, n) + "<alpha=#00>" + text.Substring(n);
                if (n >= text.Length)
                    CompleteReveal();
            }).Every(30);
        }

        /// <summary>Shows the rest of a line still being revealed (the next line, or a decision, is coming).</summary>
        void CompleteReveal()
        {
            revealItem?.Pause();
            revealItem = null;
            if (revealLabel != null)
                revealLabel.text = revealText;
            revealLabel = null;
        }

        void AnimateVoice(TimerState timer)
        {
            speakingLeft = Mathf.Max(0f, speakingLeft - timer.deltaTime / 1000f * Time.timeScale);
            bool on = speakingLeft > 0f;
            voiceBars.EnableInClassList("voice-bars--on", on);
            int i = 0;
            foreach (var bar in voiceBars.Children())
            {
                float h = on ? 0.25f + 0.75f * Mathf.PerlinNoise(Time.unscaledTime * 9f, i * 3.1f) : 0.2f;
                bar.style.scale = new Scale(new Vector2(1f, h));
                i++;
            }
        }

        public void AddSystem(string text, string kind = null)
        {
            CompleteReveal();
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
            CompleteReveal();
            DropHold();
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

        /// <summary>
        /// The caller is holding the line: the questions still open (null while someone is speaking)
        /// above the verdict panel. Only the questions are rebuilt, so a verdict the player is
        /// confirming survives the caller talking over it.
        /// </summary>
        public void ShowHold(string callerTitle, List<ConvQuestion> questions, Action<ConvQuestion> ask)
        {
            pickHandler = null;
            timer = null;
            holdQuestions = questions;
            askHandler = questions != null ? ask : null;
            if (holdBox == null || holdBox.parent != footer)
            {
                footer.Clear();
                holdBox = UIKit.Div("hold");
                var head = UIKit.Div("hold__head");
                head.Add(UIKit.Div("hold__dot"));
                string who = string.IsNullOrEmpty(callerTitle) || callerTitle == "Unknown" ? "The caller" : callerTitle;
                head.Add(UIKit.Text($"{who} is holding the line  ·  ask him, or give your verdict", "hold__title"));
                holdBox.Add(head);
                questionsArea = UIKit.Div("hold__questions");
                holdBox.Add(questionsArea);
                verdictArea = UIKit.Div("hold__verdict");
                holdBox.Add(verdictArea);
                footer.Add(holdBox);
            }
            questionsArea.Clear();
            if (questions == null)
            {
                questionsArea.Add(UIKit.Text("• • •", "hold__wait"));
            }
            else
            {
                for (int i = 0; i < questions.Count; i++)
                {
                    var q = questions[i];
                    var b = new Button(() => PickQuestion(questions.IndexOf(q))) { focusable = false };
                    b.AddToClassList("btn");
                    b.AddToClassList("option");
                    b.AddToClassList("option--ask");
                    b.RegisterCallback<ClickEvent>(_ => Sfx.Play(Sfx.Click, 0.5f));
                    b.Add(UIKit.Text((i + 1).ToString(), "option__key"));
                    b.Add(UIKit.Text(q.label, "option__text"));
                    questionsArea.Add(b);
                }
            }
            UIKit.ScrollToEnd(log);
        }

        /// <summary>Puts the verdict panel under the questions for the rest of the hold.</summary>
        public void ShowVerdict(VerdictPanel panel)
        {
            if (verdictArea == null)
                return;
            verdictArea.Clear();
            if (panel != null)
                verdictArea.Add(panel.Root);
        }

        public bool PickQuestion(int index)
        {
            if (!HasQuestions || index < 0 || index >= holdQuestions.Count)
                return false;
            var q = holdQuestions[index];
            var ask = askHandler;
            askHandler = null;
            ask(q);
            return true;
        }

        void DropHold()
        {
            holdBox = null;
            questionsArea = null;
            verdictArea = null;
        }

        public void ShowIdle()
        {
            pickHandler = null;
            askHandler = null;
            holdQuestions = null;
            timer = null;
            DropHold();
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
        string idleHint = "";

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
            Root.RegisterCallback<ClickEvent>(e =>
            {
                Clicked?.Invoke();
                e.StopPropagation();
            });
        }

        /// <summary>The player clicked the HUD: bring the phone back up.</summary>
        public event Action Clicked;

        public void Set(string title, string number, string portraitId)
        {
            name.text = $"{title}  <color=#5A4E54>{number}</color>";
            UIKit.SetImage(portrait, UISkin.Tex(portraitId));
            line.text = "";
            idleHint = "";
            hint.text = "";
        }

        public void SetLine(string text) => line.text = text;

        public void SetVisible(bool on) => Root.EnableInClassList("call-hud--on", on);

        public void SetDecision(bool pending, float remaining, float total)
        {
            timer.Hide(!pending);
            hint.text = pending ? "Decision waiting  ·  press Tab" : idleHint;
            if (pending)
                timer.Set(remaining, total);
        }

        /// <summary>The hint under the caller's line when no decision is waiting.</summary>
        public void SetIdleHint(string text)
        {
            idleHint = text ?? "";
            if (timer.resolvedStyle.display == DisplayStyle.None || timer.style.display == DisplayStyle.None)
                hint.text = idleHint;
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
