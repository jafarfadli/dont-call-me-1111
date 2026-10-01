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
    /// the green Start button (Continue once a week is under way), Case Files, Settings and Quit,
    /// and a ringing phone whose slide-to-answer also starts (or continues) the game.
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
        ModalView confirm;
        Fader fade;
        bool leaving;
        float t;

        void Awake()
        {
            UISkin.Current = skin;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (GetComponent<Sfx>() == null)
                gameObject.AddComponent<Sfx>();
        }

        void OnEnable() => Build();

        /// <summary>The day Start (or the phone) opens: the first unplayed day, or the tutorial of a new week.</summary>
        static int NextDay(out bool resume)
        {
            var catalog = DayCatalog.Load();
            int next = GameRun.NextDay;
            resume = GameRun.HasProgress && catalog != null && catalog.Get(next) != null;
            return resume ? next : GameRun.FirstDay;
        }

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
            NextDay(out bool resume);
            var start = new Button(Begin) { focusable = false };
            start.AddToClassList("start-btn");
            start.Add(UIKit.Icon("mi_play", "start-btn__icon"));
            start.Add(UIKit.Text(resume ? Loc.T("Continue") : Loc.T("Start"), "start-btn__text"));
            start.Query().ForEach(e =>
            {
                if (e != start)
                    e.pickingMode = PickingMode.Ignore;
            });
            menu.Add(start);
            menu.Add(Row("mi_folder", Loc.T("Case Files"), OpenFiles));
            menu.Add(Row("mi_gear", Loc.T("Settings"), OpenSettings));
            menu.Add(Row("mi_exit", Loc.T("Quit"), Quit));
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
            }
        }

        /// <summary>Start, and the phone's slider: pick up where the week left off, or start one.</summary>
        void Begin()
        {
            int day = NextDay(out bool resume);
            if (!resume)
                GameRun.NewRun();
            StartGame(day);
        }

        void NewWeek()
        {
            if (leaving)
                return;
            GameRun.NewRun();
            StartGame(GameRun.FirstDay);
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
            if (files != null || settings != null)
                return;
            main.Hide(true);
            files = new CaseFilesView(CloseFiles, PlayDay, () =>
            {
                if (!GameRun.HasProgress)
                    NewWeek();
                else
                    Ask(Loc.T("Start a new week?"), Loc.T("Every case file of this week will be cleared."), Loc.T("New week"), NewWeek);
            });
            files.Root.AddToClassList("title-settings");
            files.Root.AddToClassList("menu-card--in");
            screen.Add(files.Root);
        }

        /// <summary>A day picked in the case files. Finishing an earlier day again drops the days after it.</summary>
        void PlayDay(int day)
        {
            if (GameRun.Get(day + 1) != null)
                Ask(Loc.F("Play Day {0} again?", day), Loc.T("When you finish it, the days after it are cleared and played again."), Loc.T("Play again"), () => StartGame(day));
            else
                StartGame(day);
        }

        void CloseFiles()
        {
            files?.Root.RemoveFromHierarchy();
            files = null;
            main.Hide(false);
        }

        // ---------------------------------------------------------------- settings

        void OpenSettings()
        {
            if (settings != null || files != null)
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
