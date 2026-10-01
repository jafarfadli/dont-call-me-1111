using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The title screen (Home scene): the DON'T CALL ME! logo over the room at dusk, the menu
    /// (Continue or Start, New week, Settings, Quit) and a ringing phone whose slide-to-answer
    /// also starts (or continues) the game.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] UISkin skin;

        VisualElement root;
        VisualElement main;
        VisualElement logo;
        VisualElement phone;
        SettingsView settings;
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

        void Build()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null || main != null)
                return;
            root.AddToClassList("dcm-root");
            root.pickingMode = PickingMode.Ignore;

            var screen = UIKit.Div("title-screen");
            screen.Add(UIKit.Div("title-screen__shade"));

            main = UIKit.Div("title-screen__left");
            logo = UIKit.Div("title-logo");
            UIKit.SetImage(logo, UISkin.Tex("title_logo"));
            main.Add(logo);
            var tag = UIKit.Div("title-tagline", "paper");
            tag.Add(UIKit.Text("A scam-call detective game", "title-tagline__text"));
            tag.Add(UIKit.Text("전화하지 마세요!", "title-tagline__ko"));
            main.Add(tag);
            var menu = UIKit.Div("title-menu");
            var catalog = DayCatalog.Load();
            int days = catalog != null ? catalog.Count : 1;
            int next = GameRun.NextDay;
            if (GameRun.HasProgress && next <= days)
            {
                menu.Add(PauseMenuView.MenuButton("ic_play", $"Continue  ·  Day {next}", () => StartGame(next), "btn--green"));
                menu.Add(PauseMenuView.MenuButton("ic_star", "New week", ConfirmNewWeek, null));
            }
            else
            {
                string label = GameRun.HasProgress ? "New week  ·  Day 1" : "Start  ·  Day 1";
                menu.Add(PauseMenuView.MenuButton("ic_play", label, NewWeek, "btn--green"));
            }
            menu.Add(PauseMenuView.MenuButton("ic_gear", "Settings", OpenSettings, null));
            menu.Add(PauseMenuView.MenuButton("ic_exit", "Quit", Quit, "btn--red"));
            main.Add(menu);
            screen.Add(main);

            phone = BuildPhone();
            screen.Add(phone);

            var foot = UIKit.Div("title-footer");
            foot.Add(UIKit.Text("Headphones on  ·  Drag to look  ·  Esc pauses", "title-footer__text"));
            screen.Add(foot);
            root.Add(screen);

            fade = new Fader(true);
            root.Add(fade.Root);
            fade.FadeIn(1.4f);
            logo.schedule.Execute(() =>
            {
                logo.AddToClassList("title-logo--in");
                Sfx.Play(Sfx.Stamp, 0.9f);
            }).ExecuteLater(500);
            tag.schedule.Execute(() => tag.AddToClassList("title-tagline--in")).ExecuteLater(900);
            menu.schedule.Execute(() => menu.AddToClassList("title-menu--in")).ExecuteLater(1150);
            phone.schedule.Execute(() => phone.AddToClassList("title-phone--in")).ExecuteLater(1400);
        }

        /// <summary>A small phone with an incoming call from an unknown number; answering starts the game.</summary>
        VisualElement BuildPhone()
        {
            // Same structure and classes as the in-game phone and its incoming-call screen, scaled down by USS.
            var box = UIKit.Div("title-phone");
            var screen = UIKit.Div("phone__screen");
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
            info.Add(UIKit.Text("INCOMING CALL", "call__label"));
            info.Add(UIKit.Text("Unknown", "call__name"));
            info.Add(UIKit.Text("070-8844-2019", "call__number"));
            call.Add(info);
            var slider = new SlideToAnswer();
            slider.Answered += Answer;
            call.Add(slider);
            content.Add(call);
            screen.Add(content);
            box.Add(screen);
            var frame = UIKit.Div("phone__frame");
            frame.pickingMode = PickingMode.Ignore;
            box.Add(frame);

            float k = 0f;
            ring.schedule.Execute(() =>
            {
                k = (k + 0.03f) % 1.4f;
                float f = k / 1.4f;
                ring.style.scale = new Scale(Vector2.one * (1f + f * 0.5f));
                ring.style.opacity = 1f - f;
            }).Every(30);
            return box;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (logo != null)
                logo.style.rotate = new Rotate(Mathf.Sin(t * 1.1f) * 1.2f);
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
            }
        }

        /// <summary>The phone's slide picks up where the run left off, or starts one.</summary>
        void Answer()
        {
            var catalog = DayCatalog.Load();
            int next = GameRun.NextDay;
            if (GameRun.HasProgress && next <= (catalog != null ? catalog.Count : 1))
                StartGame(next);
            else
                NewWeek();
        }

        void NewWeek()
        {
            if (leaving)
                return;
            GameRun.NewRun();
            StartGame(1);
        }

        void ConfirmNewWeek()
        {
            if (confirm != null)
                return;
            confirm = new ModalView("Start a new week?", $"Your week so far (up to Day {GameRun.NextDay - 1}) will be forgotten.",
                                    "New week", "Cancel", () =>
                                    {
                                        CloseConfirm();
                                        NewWeek();
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

        void OpenSettings()
        {
            if (settings != null)
                return;
            main.Hide(true);
            settings = new SettingsView(CloseSettings);
            settings.Root.AddToClassList("title-settings");
            settings.Root.AddToClassList("menu-card--in");
            root.Q(className: "title-screen").Add(settings.Root);
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
