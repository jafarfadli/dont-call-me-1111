using System;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>A checkable detail as a chip: click to copy it, then paste it into any app field.</summary>
    public class FactChip : VisualElement
    {
        public Fact Fact { get; }

        public FactChip(Fact fact)
        {
            Fact = fact;
            AddToClassList("chip");
            var label = new Label(fact.Display);
            label.AddToClassList("chip__label");
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.pickingMode = PickingMode.Ignore;
            Add(label);
            var icon = new VisualElement();
            icon.AddToClassList("chip__icon");
            icon.pickingMode = PickingMode.Ignore;
            Add(icon);
            tooltip = Loc.T("Click to copy");
            RegisterCallback<ClickEvent>(e =>
            {
                Clipboard.Copy(Fact);
                AddToClassList("chip--copied");
                Sfx.Play(Sfx.Pop, 0.6f);
                schedule.Execute(() => RemoveFromClassList("chip--copied")).ExecuteLater(900);
                e.StopPropagation();
            });
        }
    }

    /// <summary>
    /// The decision timer: a ring that empties clockwise with the seconds in the middle.
    /// Green above half, amber below, red and pulsing in the last quarter.
    /// </summary>
    public class DecisionTimer : VisualElement
    {
        static readonly Color Ink = new Color32(43, 36, 48, 255);
        static readonly Color Cream = new Color32(248, 239, 218, 255);
        static readonly Color Track = new Color32(220, 199, 160, 255);
        static readonly Color Green = new Color32(94, 158, 85, 255);
        static readonly Color Amber = new Color32(227, 165, 60, 255);
        static readonly Color Red = new Color32(201, 72, 58, 255);

        readonly Label label;
        readonly bool small;
        float fraction = 1f;
        float pulse;

        public DecisionTimer(bool small = false)
        {
            this.small = small;
            AddToClassList("timer");
            if (small)
                AddToClassList("timer--small");
            label = new Label("--");
            label.AddToClassList("timer__label");
            label.pickingMode = PickingMode.Ignore;
            Add(label);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public float Fraction => fraction;

        public void Set(float remaining, float total)
        {
            fraction = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
            label.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
            label.style.color = fraction < 0.25f ? Red : Ink;
            if (fraction < 0.25f)
            {
                pulse += Time.unscaledDeltaTime * 8f;
                float s = 1f + 0.06f * Mathf.Abs(Mathf.Sin(pulse));
                style.scale = new Scale(new Vector2(s, s));
            }
            else
            {
                style.scale = new Scale(Vector2.one);
            }
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 4f)
                return;
            var p = ctx.painter2D;
            var c = r.center;
            float rad = Mathf.Min(r.width, r.height) * 0.5f - 3f;
            float ring = small ? 6f : 10f;

            p.fillColor = Cream;
            p.BeginPath();
            p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();

            p.lineWidth = ring;
            p.strokeColor = Track;
            p.BeginPath();
            p.Arc(c, rad - ring * 0.5f - 2f, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Stroke();

            if (fraction > 0.001f)
            {
                p.strokeColor = fraction > 0.5f ? Green : fraction > 0.25f ? Amber : Red;
                p.lineCap = LineCap.Round;
                p.BeginPath();
                p.Arc(c, rad - ring * 0.5f - 2f, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * fraction));
                p.Stroke();
            }

            p.lineCap = LineCap.Butt;
            p.lineWidth = small ? 2.5f : 3.5f;
            p.strokeColor = Ink;
            p.BeginPath();
            p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Stroke();
        }
    }

    /// <summary>
    /// Slide to answer. The knob follows the pointer and springs back unless it reaches the end;
    /// there is no other way to answer, and no decline.
    /// </summary>
    public class SlideToAnswer : VisualElement
    {
        public event Action Answered;

        readonly VisualElement knob;
        readonly Label hint;
        float progress;
        bool dragging;
        bool done;
        int pointerId = -1;
        float startX;
        float startProgress;
        float time;

        public SlideToAnswer()
        {
            AddToClassList("slide");
            hint = new Label(Loc.T("slide to answer") + "   › › ›");
            hint.AddToClassList("slide__hint");
            hint.style.whiteSpace = WhiteSpace.NoWrap;
            hint.pickingMode = PickingMode.Ignore;
            Add(hint);
            knob = new VisualElement();
            knob.AddToClassList("slide__knob");
            knob.Add(UIKit.Icon("ic_answer"));
            Add(knob);
            knob.RegisterCallback<PointerDownEvent>(OnDown);
            knob.RegisterCallback<PointerMoveEvent>(OnMove);
            knob.RegisterCallback<PointerUpEvent>(OnUp);
            knob.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            schedule.Execute(Animate).Every(16);
        }

        float Travel => Mathf.Max(1f, layout.width - knob.layout.width - 12f);

        public void ResetSlider()
        {
            done = false;
            progress = 0f;
            Apply(0f);
        }

        /// <summary>Answer as if the knob reached the end (tests and accessibility).</summary>
        public void Complete()
        {
            if (done)
                return;
            done = true;
            progress = 1f;
            Apply(0f);
            Sfx.Play(Sfx.Answer);
            Answered?.Invoke();
        }

        void OnDown(PointerDownEvent e)
        {
            if (done)
                return;
            dragging = true;
            pointerId = e.pointerId;
            startX = e.position.x;
            startProgress = progress;
            knob.CapturePointer(pointerId);
            e.StopPropagation();
        }

        void OnMove(PointerMoveEvent e)
        {
            if (!dragging || !knob.HasPointerCapture(pointerId))
                return;
            progress = Mathf.Clamp01(startProgress + (e.position.x - startX) / Travel);
            Apply(0f);
            if (progress >= 0.95f)
            {
                knob.ReleasePointer(pointerId);
                dragging = false;
                Complete();
            }
        }

        void OnUp(PointerUpEvent e)
        {
            if (!dragging)
                return;
            knob.ReleasePointer(pointerId);
            dragging = false;
            if (progress >= 0.75f)
                Complete();
        }

        void Animate()
        {
            time += 0.016f;
            float nudge = 0f;
            if (!dragging && !done)
            {
                if (progress > 0f)
                    progress = Mathf.MoveTowards(progress, 0f, 0.06f);
                else
                {
                    // A little nudge every two seconds shows which way to slide.
                    float phase = time % 2f;
                    nudge = phase < 0.4f ? Mathf.Sin(phase / 0.4f * Mathf.PI) * 18f : 0f;
                }
            }
            Apply(nudge);
            hint.style.opacity = (1f - progress) * (0.65f + 0.35f * Mathf.Sin(time * 4f));
        }

        void Apply(float nudge)
        {
            knob.style.left = 6f + progress * Travel + nudge;
        }
    }

    /// <summary>Three dots that light up in turn while someone is typing.</summary>
    public class TypingDots : VisualElement
    {
        readonly VisualElement[] dots = new VisualElement[3];
        int step;

        public TypingDots()
        {
            AddToClassList("typing");
            for (int i = 0; i < 3; i++)
            {
                dots[i] = new VisualElement();
                dots[i].AddToClassList("typing__dot");
                Add(dots[i]);
            }
            schedule.Execute(() =>
            {
                step = (step + 1) % 3;
                for (int i = 0; i < 3; i++)
                    dots[i].EnableInClassList("typing__dot--on", i == step);
            }).Every(260);
        }
    }
}
