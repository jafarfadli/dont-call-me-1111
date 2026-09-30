using System;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>Pieces shared by the call screens: the dimmed night backdrop and the round portrait.</summary>
    static class CallArt
    {
        public static VisualElement Screen()
        {
            var call = UIKit.Div("call");
            call.Add(UIKit.Div("call__bg"));
            call.Add(UIKit.Div("call__shade"));
            return call;
        }

        /// <summary>The caller's name; long names ("Nuri Bank customer centre") get a smaller size.</summary>
        public static Label Name(string title)
        {
            var name = UIKit.Text(title, "call__name");
            if (title != null && title.Length > 14)
                name.AddToClassList("call__name--long");
            return name;
        }

        public static VisualElement Avatar(CallerInfo caller, params string[] classes)
        {
            var avatar = UIKit.Div("call__avatar").With(classes);
            UIKit.SetImage(avatar, UISkin.Tex(caller.portrait) ?? UISkin.Tex("pt_unknown"));
            avatar.pickingMode = PickingMode.Ignore;
            return avatar;
        }
    }

    /// <summary>
    /// Incoming call: the caller's round pixel portrait with a pulsing ring, "Unknown" or the saved
    /// name, the number and one control, slide to answer. No decline.
    /// </summary>
    public class IncomingCallView : PhoneScreen
    {
        public SlideToAnswer Slider { get; }
        public override bool LightStatus => false;

        public IncomingCallView(PhoneController phone, CallerInfo caller, string title, string label = "INCOMING CALL")
            : base(phone, "", scroll: false, header: false)
        {
            var call = CallArt.Screen();
            var ring = UIKit.Div("ring-pulse");
            ring.pickingMode = PickingMode.Ignore;
            call.Add(ring);
            call.Add(CallArt.Avatar(caller));
            var info = UIKit.Div("call__info");
            info.Add(UIKit.Text(label, "call__label"));
            info.Add(CallArt.Name(title));
            info.Add(UIKit.Text(caller.number, "call__number"));
            call.Add(info);
            Slider = new SlideToAnswer();
            call.Add(Slider);
            Root.Add(call);

            float t = 0f;
            Root.schedule.Execute(() =>
            {
                t += 0.03f;
                float k = t % 1.4f / 1.4f;
                ring.style.scale = new Scale(new Vector2(1f + k * 0.5f, 1f + k * 0.5f));
                ring.style.opacity = 1f - k;
                // The phone shakes while it rings.
                float shake = Mathf.Sin(t * 60f) * (t % 1.2f < 0.5f ? 2.5f : 0f);
                phone.Body.style.translate = new Translate(shake, 0);
            }).Every(30);
        }

        public override void OnHide()
        {
            Phone.Body.style.translate = StyleKeyword.Null;
        }
    }

    /// <summary>
    /// The call in progress: portrait, name, call timer, shortcuts to investigate (keypad, apps,
    /// notebook) and the red hang-up button.
    /// </summary>
    public class InCallView : PhoneScreen
    {
        public event Action HangUpClicked;
        public override bool LightStatus => false;

        readonly Label timer;
        readonly Label status;

        public InCallView(PhoneController phone, CallerInfo caller, string title, string statusText = "ON CALL")
            : base(phone, "", scroll: false, header: false)
        {
            var call = CallArt.Screen();
            call.Add(CallArt.Avatar(caller, "call__avatar--small"));
            var info = UIKit.Div("call__info", "call__info--active");
            status = UIKit.Text(statusText, "call__label");
            info.Add(status);
            info.Add(CallArt.Name(title));
            info.Add(UIKit.Text(caller.number, "call__number"));
            timer = UIKit.Text("00:00", "call__timer");
            info.Add(timer);
            call.Add(info);

            var buttons = UIKit.Div("call__buttons");
            var grid = UIKit.Div("call__grid");
            grid.Add(Small("ic_keypad", "Keypad", () => phone.Push(phone.App<CallsApp>().CreateKeypad())));
            grid.Add(Small("ic_apps", "Apps", phone.GoHome));
            grid.Add(Small("ic_notebook", "Notes", () => phone.UI.OpenNotebook(true)));
            grid.Add(Small("ic_speaker", "Speaker", () => Sfx.Play(Sfx.Click)));
            buttons.Add(grid);
            var hang = UIKit.Div("call__hang");
            var red = UIKit.Div("call-btn", "call-btn--red");
            red.Add(UIKit.Icon("ic_hangup"));
            red.tooltip = "Hang up";
            red.RegisterCallback<ClickEvent>(e =>
            {
                HangUpClicked?.Invoke();
                e.StopPropagation();
            });
            hang.Add(red);
            hang.Add(UIKit.Text("Hang up"));
            buttons.Add(hang);
            call.Add(buttons);
            Root.Add(call);
        }

        VisualElement Small(string icon, string label, Action onClick)
        {
            var box = UIKit.Div("call__small");
            var b = UIKit.Div("call-btn", "call-btn--dark");
            b.Add(UIKit.Icon(icon));
            b.RegisterCallback<ClickEvent>(e =>
            {
                Sfx.Play(Sfx.Click, 0.5f);
                onClick();
                e.StopPropagation();
            });
            box.Add(b);
            box.Add(UIKit.Text(label));
            return box;
        }

        public void SetTimer(string text) => timer.text = text;

        public void SetStatus(string text) => status.text = text;
    }
}
