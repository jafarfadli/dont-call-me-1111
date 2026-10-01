using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The phone in Jiwoo's hand: frame, status bar, a stack of screens with slide transitions, the
    /// home screen and the three apps (Contacts, Chats, Nuri Bank). Tab raises and lowers it; Esc
    /// goes back one screen, then lowers it. While a call is ringing the phone is locked up: only
    /// the slide can be used.
    /// </summary>
    public class PhoneController
    {
        public readonly UIManager UI;
        public readonly PhoneContent Content;
        public readonly WorldDirectory Directory;
        public readonly List<PhoneApp> Apps = new List<PhoneApp>();

        public VisualElement Root { get; }
        /// <summary>Holds screen and frame; shaken while ringing without disturbing the raise animation.</summary>
        public VisualElement Body { get; }
        public HomeScreen Home { get; }
        public bool IsUp { get; private set; }
        public bool Locked { get; set; }

        /// <summary>The in-call screen, kept while the player investigates in other apps.</summary>
        public PhoneScreen CallView { get; set; }
        public string CallTimerText { get; set; } = "00:00";

        public event Action<bool> UpChanged;

        readonly VisualElement screen;
        readonly VisualElement content;
        readonly Label statusTime;
        readonly VisualElement returnBar;
        readonly Label returnLabel;
        readonly List<PhoneScreen> stack = new List<PhoneScreen>();

        public PhoneController(UIManager ui, PhoneContent content, WorldDirectory directory)
        {
            UI = ui;
            Content = content;
            Directory = directory;

            Root = UIKit.Div("phone");
            // The hand that holds it: palm and wrist behind the phone, fingers and thumb over its edges.
            var palm = UIKit.Div("phone__hand", "phone__hand--back");
            palm.pickingMode = PickingMode.Ignore;
            Root.Add(palm);
            Body = UIKit.Div("phone__body");
            Root.Add(Body);
            screen = UIKit.Div("phone__screen");
            Body.Add(screen);
            this.content = UIKit.Div("phone__content");
            screen.Add(this.content);

            var status = UIKit.Div("status");
            status.pickingMode = PickingMode.Ignore;
            statusTime = UIKit.Text("16:20", "status__time");
            status.Add(statusTime);
            var bars = UIKit.Div("status__bars");
            for (int i = 0; i < 4; i++)
            {
                var b = UIKit.Div("status__bar");
                b.style.height = 5 + i * 3.5f;
                bars.Add(b);
            }
            status.Add(bars);
            var battery = UIKit.Div("status__battery");
            battery.Add(UIKit.Div("status__battery-level"));
            status.Add(battery);
            screen.Add(status);

            returnBar = UIKit.Div("return-bar");
            returnLabel = UIKit.Text(Loc.T("Return to call"), "return-bar__label");
            returnLabel.pickingMode = PickingMode.Ignore;
            returnBar.Add(returnLabel);
            returnBar.RegisterCallback<ClickEvent>(e =>
            {
                ReturnToCall();
                e.StopPropagation();
            });
            returnBar.Hide(true);
            screen.Add(returnBar);

            var homeBar = UIKit.Div("home-bar");
            homeBar.Add(UIKit.Div("home-bar__pill"));
            homeBar.tooltip = Loc.T("Home");
            homeBar.RegisterCallback<ClickEvent>(e =>
            {
                if (!Locked)
                    GoHome();
                e.StopPropagation();
            });
            screen.Add(homeBar);

            var frame = UIKit.Div("phone__frame");
            frame.pickingMode = PickingMode.Ignore;
            Body.Add(frame);

            var fingers = UIKit.Div("phone__hand", "phone__hand--front");
            fingers.pickingMode = PickingMode.Ignore;
            Root.Add(fingers);

            Apps.Add(new ContactsApp());
            Apps.Add(new ChatsApp());
            Apps.Add(new BankApp());
            foreach (var app in Apps)
                app.Attach(this);

            Home = new HomeScreen(this);
            stack.Add(Home);
            this.content.Add(Home.Root);
            UpdateChrome();
        }

        public PhoneScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;

        public T App<T>() where T : PhoneApp
        {
            foreach (var a in Apps)
                if (a is T t)
                    return t;
            return null;
        }

        public int TotalBadges
        {
            get
            {
                int n = 0;
                foreach (var a in Apps)
                    n += a.Badge;
                return n;
            }
        }

        // ---------------------------------------------------------------- raise / lower

        public void Raise(bool up)
        {
            if (IsUp == up)
                return;
            if (!up && Locked)
                return;
            IsUp = up;
            Root.EnableInClassList("phone--up", up);
            Sfx.Play(Sfx.Whoosh, 0.5f);
            if (up)
                Top?.OnShow();
            UpChanged?.Invoke(up);
        }

        public void Toggle() => Raise(!IsUp);

        // ---------------------------------------------------------------- navigation

        public void Push(PhoneScreen s)
        {
            if (s == null)
                return;
            var previous = Top;
            stack.Add(s);
            content.Add(s.Root);
            // The call screen comes back after it slid away (Apps, then Return to call).
            s.Root.RemoveFromClassList("screen--leave");
            s.Root.AddToClassList("screen--enter");
            s.Root.schedule.Execute(() => s.Root.RemoveFromClassList("screen--enter")).ExecuteLater(30);
            previous?.OnHide();
            s.OnShow();
            UpdateChrome();
        }

        public void Back()
        {
            if (Locked)
                return;
            if (stack.Count > 1)
                Pop();
            else
                Raise(false);
        }

        void Pop()
        {
            var top = Top;
            stack.RemoveAt(stack.Count - 1);
            top.OnHide();
            Leave(top);
            Top.OnShow();
            UpdateChrome();
        }

        public void GoHome()
        {
            while (stack.Count > 1)
            {
                var s = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                s.OnHide();
                if (stack.Count == 1)
                {
                    Leave(s);
                }
                else
                {
                    s.Root.RemoveFromHierarchy();
                }
            }
            Home.OnShow();
            UpdateChrome();
        }

        /// <summary>Slides a screen out, then removes it, unless it was pushed again meanwhile.</summary>
        void Leave(PhoneScreen s)
        {
            s.Root.AddToClassList("screen--leave");
            s.Root.schedule.Execute(() =>
            {
                if (!stack.Contains(s))
                    s.Root.RemoveFromHierarchy();
            }).ExecuteLater(260);
        }

        /// <summary>Clear the stack down to the home screen and show one screen on top (calls).</summary>
        public void ShowOnly(PhoneScreen s)
        {
            while (stack.Count > 1)
            {
                var top = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                top.OnHide();
                top.Root.RemoveFromHierarchy();
            }
            Push(s);
        }

        public void RemoveScreen(PhoneScreen s)
        {
            if (s == null || !stack.Contains(s))
                return;
            bool wasTop = Top == s;
            stack.Remove(s);
            s.OnHide();
            s.Root.RemoveFromHierarchy();
            if (wasTop)
                Top?.OnShow();
            UpdateChrome();
        }

        public void ReturnToCall()
        {
            if (CallView == null)
                return;
            if (Top == CallView)
                return;
            stack.Remove(CallView);
            CallView.Root.RemoveFromHierarchy();
            Push(CallView);
        }

        public void OpenApp(PhoneApp app)
        {
            if (Locked)
                return;
            GoHome();
            Push(app.CreateHome());
        }

        public void OpenApp<T>() where T : PhoneApp => OpenApp(App<T>());

        // ---------------------------------------------------------------- per frame

        public void Tick(float dt)
        {
            statusTime.text = GameClock.Now;
            bool showReturn = CallView != null && Top != CallView && !Locked;
            returnBar.Hide(!showReturn);
            content.EnableInClassList("phone__content--return", showReturn);
            if (showReturn)
                returnLabel.text = Loc.T("Return to call") + $"  ·  {CallTimerText}";
        }

        void UpdateChrome()
        {
            screen.EnableInClassList("phone--light", Top != null && Top.LightStatus);
        }
    }

    /// <summary>Base for the apps. An app creates its first screen and pushes the rest itself.</summary>
    public abstract class PhoneApp
    {
        public PhoneController Phone { get; private set; }
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Icon { get; }
        public virtual int Badge => 0;

        public void Attach(PhoneController phone)
        {
            Phone = phone;
            OnAttach();
        }

        protected virtual void OnAttach() { }

        public abstract PhoneScreen CreateHome();

        protected PhoneContent Data => Phone.Content;
        protected WorldDirectory Dir => Phone.Directory;
    }

    /// <summary>Wallpaper, big clock and the row of apps with notification badges.</summary>
    public class HomeScreen : PhoneScreen
    {
        readonly Label clock;
        readonly Dictionary<PhoneApp, Label> badges = new Dictionary<PhoneApp, Label>();

        public override bool LightStatus => false;

        public HomeScreen(PhoneController phone) : base(phone, "", scroll: false, header: false)
        {
            var home = UIKit.Div("home");
            clock = UIKit.Text("16:20", "home__clock");
            home.Add(clock);
            home.Add(UIKit.Text(phone.Content.dateLabel, "home__date"));
            var grid = UIKit.Div("home__grid");
            foreach (var app in phone.Apps)
            {
                var a = UIKit.Div("app");
                a.Add(UIKit.Icon(app.Icon, "app__icon"));
                a.Add(UIKit.Text(app.Name, "app__label"));
                var badge = UIKit.Text("", "badge");
                badge.pickingMode = PickingMode.Ignore;
                a.Add(badge);
                badges[app] = badge;
                var captured = app;
                a.RegisterCallback<ClickEvent>(e =>
                {
                    Sfx.Play(Sfx.Click, 0.5f);
                    phone.OpenApp(captured);
                    e.StopPropagation();
                });
                grid.Add(a);
            }
            home.Add(grid);
            Root.Add(home);
            Root.schedule.Execute(Refresh).Every(500);
        }

        public override void OnShow() => Refresh();

        void Refresh()
        {
            clock.text = GameClock.Now;
            foreach (var pair in badges)
            {
                int n = pair.Key.Badge;
                pair.Value.text = n.ToString();
                pair.Value.Hide(n <= 0);
            }
        }
    }
}
