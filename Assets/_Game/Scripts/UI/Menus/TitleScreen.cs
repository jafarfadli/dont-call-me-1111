using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The title screen (Home scene), over the room at dusk: the DON'T CALL ME! sticker logo,
    /// the green Start button (Continue once a week is under way, Choose a day once it is over),
    /// Choose a day (<see cref="DaySelectView"/>), Case Files, Settings and Quit, and a ringing
    /// phone whose slide-to-answer does what the green button does.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] UISkin skin;

        VisualElement root;
        VisualElement screen;
        VisualElement main;
        VisualElement logo;
        VisualElement phone;
        SettingsView settings;
        CaseFilesView files;
        DaySelectView days;
        ModalView confirm;
        Fader fade;
        bool leaving;
        float t;
        // The last letters typed, for the DEMO switch.
        string typed = "";
        VisualElement toast;

        void Awake()
        {
            UISkin.Current = skin;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (GetComponent<Sfx>() == null)
                gameObject.AddComponent<Sfx>();
        }

        void OnEnable()
        {
            Build();
            if (Keyboard.current != null)
                Keyboard.current.onTextInput += OnText;
        }

        void OnDisable()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput -= OnText;
        }

        /// <summary>Typing DEMO opens every day for a demo (<see cref="GameRun.Demo"/>); typing it again closes them.</summary>
        void OnText(char c)
        {
            if (leaving || !char.IsLetter(c))
                return;
            typed = (typed + char.ToLowerInvariant(c));
            if (typed.Length > 4)
                typed = typed.Substring(typed.Length - 4);
            if (typed != "demo")
                return;
            typed = "";
            GameRun.Demo = !GameRun.Demo;
            Sfx.Play(GameRun.Demo ? Sfx.Success : Sfx.Click);
            ShowToast(GameRun.Demo ? Loc.T("Demo: every day is open. Type DEMO again to close them.") : Loc.T("Demo off: days open as they are played."));
            if (days != null)
            {
                CloseDays();
                OpenDays();
            }
        }

        void ShowToast(string text)
        {
            toast?.RemoveFromHierarchy();
            toast = UIKit.Text(text, "demo-toast");
            toast.pickingMode = PickingMode.Ignore;
            root.Add(toast);
            var shown = toast;
            shown.schedule.Execute(() => shown.AddToClassList("demo-toast--in")).ExecuteLater(20);
            shown.schedule.Execute(() => shown.RemoveFromClassList("demo-toast--in")).ExecuteLater(3200);
            shown.schedule.Execute(() => shown.RemoveFromHierarchy()).ExecuteLater(3700);
        }

        /// <summary>
        /// The day Start (or the phone) plays: where this week goes on, else the day the last finish
        /// opened (<see cref="GameRun.Suggested"/>); -1 when every open day is finished, and Start
        /// opens the day select instead. Never the tutorial again once it is done.
        /// </summary>
        static int StartDay => GameRun.Suggested(DayCatalog.Load());

        void Build()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null || main != null)
                return;
            root.AddToClassList("dcm-root");
            root.pickingMode = PickingMode.Ignore;

            screen = UIKit.Div("title-screen");
            screen.Add(UIKit.Div("title-screen__shade"));

            main = UIKit.Div("title-screen__left");
            logo = UIKit.Div("title-logo");
            UIKit.SetImage(logo, UISkin.Tex("title_logo"));
            main.Add(logo);
            var menu = UIKit.Div("title-menu");
            int first = StartDay;
            var start = new Button(Begin) { focusable = false };
            start.AddToClassList("start-btn");
            start.Add(UIKit.Icon("mi_play", "start-btn__icon"));
            string label = first < 0 ? Loc.T("Choose a day") : first > GameRun.FirstDay || GameRun.HasProgress ? Loc.T("Continue") : Loc.T("Start");
            start.Add(UIKit.Text(label, "start-btn__text"));
            start.Query().ForEach(e =>
            {
                if (e != start)
                    e.pickingMode = PickingMode.Ignore;
            });
            menu.Add(start);
            // When every open day is done the green button itself opens the day select.
            if (first >= 0)
                menu.Add(Row("mi_calendar", Loc.T("Choose a day"), OpenDays));
            menu.Add(Row("mi_folder", Loc.T("Case Files"), OpenFiles));
            menu.Add(Row("mi_gear", Loc.T("Settings"), OpenSettings));
#if !UNITY_WEBGL || UNITY_EDITOR
            // A page in a browser has nothing to quit.
            menu.Add(Row("mi_exit", Loc.T("Quit"), Quit));
#endif
            main.Add(menu);
            screen.Add(main);

            phone = BuildPhone();
            screen.Add(phone);
            root.Add(screen);

            fade = new Fader(true);
            root.Add(fade.Root);
            fade.FadeIn(1.4f);
            logo.schedule.Execute(() =>
            {
                logo.AddToClassList("title-logo--in");
                Sfx.Play(Sfx.Stamp, 0.9f);
            }).ExecuteLater(500);
            menu.schedule.Execute(() => menu.AddToClassList("title-menu--in")).ExecuteLater(1000);
            phone.schedule.Execute(() => phone.AddToClassList("title-phone--in")).ExecuteLater(1400);
            // The week's summary sends the player here to pick the next day.
            if (SceneFlow.ShowDays)
            {
                SceneFlow.ShowDays = false;
                OpenDays();
            }
        }


        /// <summary>A menu line: a sticker icon and its label, lighting up under the pointer.</summary>
        static VisualElement Row(string icon, string label, System.Action onClick)
        {
            var row = UIKit.Div("title-row");
            row.Add(UIKit.Icon(icon, "title-row__icon"));
            var text = UIKit.Text(label, "title-row__text");
            text.pickingMode = PickingMode.Ignore;
            row.Add(text);
            row.RegisterCallback<ClickEvent>(e =>
            {
                Sfx.Play(Sfx.Click, 0.5f);
                onClick();
                e.StopPropagation();
            });
            return row;
        }

        /// <summary>A small phone with an incoming call from an unknown number; answering starts the game.</summary>
        VisualElement BuildPhone()
        {
            // Same structure and classes as the in-game phone and its incoming-call screen, scaled down by USS.
            var box = UIKit.Div("title-phone");
            var view = UIKit.Div("phone__screen");
            var content = UIKit.Div("phone__content");
            var call = UIKit.Div("call");
            call.Add(UIKit.Div("call__bg"));
            call.Add(UIKit.Div("call__shade"));
            var ring = UIKit.Div("ring-pulse");
            ring.pickingMode = PickingMode.Ignore;
            call.Add(ring);
            var avatar = UIKit.Div("call__avatar");
            UIKit.SetImage(avatar, UISkin.Tex("pt_unknown"));
            call.Add(avatar);
            var info = UIKit.Div("call__info");
            info.Add(UIKit.Text(Loc.T("INCOMING CALL"), "call__label"));
            info.Add(UIKit.Text(Loc.T("Unknown"), "call__name"));
            info.Add(UIKit.Text("070-8844-2019", "call__number"));
            call.Add(info);
            var slider = new SlideToAnswer();
            slider.Answered += Begin;
            call.Add(slider);
            content.Add(call);
            view.Add(content);
            box.Add(view);
            var frame = UIKit.Div("phone__frame");
            frame.pickingMode = PickingMode.Ignore;
            box.Add(frame);

            float k = 0f;
            ring.schedule.Execute(() =>
            {
                k = (k + 0.03f) % 1.4f;
                float f = k / 1.4f;
                ring.style.scale = new Scale(new Vector2(1f + f * 0.5f, 1f + f * 0.5f));
                ring.style.opacity = 1f - f;
            }).Every(30);
            return box;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            // The phone buzzes in short bursts, like a call you are trying to ignore.
            if (phone != null)
            {
                float burst = t % 3.2f;
                float shake = burst < 0.9f ? Mathf.Sin(t * 70f) * 3f : 0f;
                phone.style.translate = new Translate(shake, 0);
                if (burst < Time.unscaledDeltaTime && !leaving)
                    Sfx.Play(Sfx.Buzz, 0.35f);
            }
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (confirm != null)
                    CloseConfirm();
                else if (settings != null)
                    CloseSettings();
                else if (files != null)
                    CloseFiles();
                else if (days != null)
                    CloseDays();
            }
        }

        /// <summary>Start, and the phone's slider: the suggested day, or the day select when every open day is done.</summary>
        void Begin()
        {
            int day = StartDay;
            if (day < 0)
                OpenDays();
            else
                PlayDay(day);
        }

        /// <summary>Clears this week. A player who has opened more than the tutorial picks where the new week starts.</summary>
        void NewWeek()
        {
            if (leaving)
                return;
            GameRun.NewRun();
            if (GameRun.Unlocked(GameRun.FirstDay + 1))
            {
                CloseFiles();
                OpenDays();
            }
            else
            {
                StartGame(GameRun.FirstDay);
            }
        }

        void Ask(string title, string text, string yes, System.Action onYes)
        {
            if (confirm != null)
                return;
            confirm = new ModalView(title, text, yes, Loc.T("Cancel"), () =>
            {
                CloseConfirm();
                onYes();
            }, CloseConfirm);
            root.Add(confirm.Root);
        }

        void CloseConfirm()
        {
            confirm?.Root.RemoveFromHierarchy();
            confirm = null;
        }

        void StartGame(int day)
        {
            if (leaving)
                return;
            leaving = true;
            Sfx.Play(Sfx.Answer);
            MusicPlayer.Current?.Stop(1.1f);
            fade.FadeOut(1.1f, () => SceneFlow.PlayDay(day));
        }

        // ---------------------------------------------------------------- case files

        void OpenFiles()
        {
            if (files != null || settings != null || days != null)
                return;
            main.Hide(true);
            files = new CaseFilesView(CloseFiles, () =>
            {
                if (!GameRun.HasProgress)
                    NewWeek();
                else
                    Ask(Loc.T("Start a new week?"), Loc.T("This week's case files will be cleared. Days you have opened stay open."), Loc.T("New week"), NewWeek);
            });
            files.Root.AddToClassList("title-settings");
            files.Root.AddToClassList("menu-card--in");
            screen.Add(files.Root);
        }

        void CloseFiles()
        {
            files?.Root.RemoveFromHierarchy();
            files = null;
            main.Hide(false);
        }

        // ---------------------------------------------------------------- day select

        void OpenDays()
        {
            if (days != null || files != null || settings != null || leaving)
                return;
            main.Hide(true);
            days = new DaySelectView(CloseDays, PlayDay);
            days.Root.AddToClassList("title-settings");
            days.Root.AddToClassList("menu-card--in");
            screen.Add(days.Root);
        }

        /// <summary>
        /// A day picked in the day select. Days before it that this week has not played come back as
        /// they went last time; finishing it drops this week's days after it, which are played again.
        /// </summary>
        void PlayDay(int day)
        {
            if (!GameRun.Unlocked(day))
                return;
            GameRun.FillBefore(day);
            StartGame(day);
        }

        void CloseDays()
        {
            days?.Root.RemoveFromHierarchy();
            days = null;
            main.Hide(false);
        }

        // ---------------------------------------------------------------- settings

        void OpenSettings()
        {
            if (settings != null || files != null || days != null)
                return;
            main.Hide(true);
            settings = new SettingsView(CloseSettings, ChangeLanguage);
            settings.Root.AddToClassList("title-settings");
            settings.Root.AddToClassList("menu-card--in");
            screen.Add(settings.Root);
        }

        /// <summary>The title screen comes back in the new language.</summary>
        void ChangeLanguage(Lang lang)
        {
            if (leaving)
                return;
            leaving = true;
            Loc.Current = lang;
            fade.FadeOut(0.4f, () => SceneFlow.Load(SceneFlow.Home));
        }

        void CloseSettings()
        {
            settings?.Root.RemoveFromHierarchy();
            settings = null;
            main.Hide(false);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
