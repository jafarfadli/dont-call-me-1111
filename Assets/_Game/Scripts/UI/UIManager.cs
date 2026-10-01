using System;
using System.Collections.Generic;
using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Flow;
using DontCallMe.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>What the "case closed" card shows at the end of a call or chat.</summary>
    public class EndingCard
    {
        public string caller;
        public string portrait;
        public string verdict;
        /// <summary>"SCAM" or "REAL" when the truth is revealed at once, else empty.</summary>
        public string truth;
        public bool truthGood;
        /// <summary>"RIGHT CALL" or "WRONG CALL" when revealed, else empty.</summary>
        public string judgement;
        public bool judgementGood;
        public string subtitle;
        public string money;
        public bool moneyIn;
        public string consequence;
    }

    /// <summary>
    /// Builds the game UI on one UIDocument and owns its layers: HUD, room panels, transcript,
    /// phone, pressure overlay, day and case cards, toasts, modals, the pause menu and the screen
    /// fade. Handles Tab (phone), Esc/right-click (back, then pause) and 1–4 (answer or ask), and
    /// locks walking while anything is open.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        [Header("Content when no day runs (cloned at start, so play mode never edits the assets)")]
        [SerializeField] PhoneContent phoneContent;
        [SerializeField] WorldDirectory directory;
        [SerializeField] RoomContent roomContent;
        [SerializeField] UISkin skin;
        [SerializeField] VoiceBank voices;

        [Header("Input")]
        [SerializeField] InputActionReference phoneAction;
        [SerializeField] InputActionReference backAction;

        [Header("Scene")]
        [SerializeField] FirstPersonController player;
        [SerializeField] CallDirector director;

        public PhoneContent Phone { get; private set; }
        public WorldDirectory Directory { get; private set; }
        public RoomContent Room { get; private set; }
        public PhoneController PhoneView { get; private set; }
        public TranscriptPanel Transcript { get; private set; }
        public CallHud CallHud { get; private set; }
        public PressureOverlay Pressure { get; private set; }
        public HudView Hud { get; private set; }
        /// <summary>The tutorial day's guide card (hidden until a step is shown).</summary>
        public TutorialCard Tutorial { get; private set; }
        public CaseFile CurrentCase { get; set; }
        public bool InputLocked { get; private set; }
        public bool Paused => pauseMenu != null;
        /// <summary>A case call is on: the phone is for investigating, the verdict is given on the call.</summary>
        public bool CaseCallActive => director != null && director.IsCaseCall;
        public RoomPanel OpenPanelView => openPanel;
        public Fader Fade { get; private set; }

        public event Action<PanelId> PanelOpened;
        public event Action<PanelId> PanelClosed;

        VisualElement root;
        VisualElement panelLayer;
        VisualElement overlayLayer;
        VisualElement dim;
        RoomPanel openPanel;
        ModalView modal;
        VisualElement endingCard;
        VisualElement card;
        PauseMenuView pauseMenu;
        // Not serialized: after a script reload in Play mode the views are gone and must not be touched.
        [System.NonSerialized] bool built;

        void Awake()
        {
            UISkin.Current = skin;
            if (voices != null)
                VoiceBank.Current = voices;
            // A day brings its own evidence (already cloned); otherwise use the scene's content.
            var dayDirector = FindAnyObjectByType<DayDirector>();
            var plan = dayDirector != null && dayDirector.enabled ? dayDirector.Prepare() : null;
            Phone = plan != null ? plan.phone : phoneContent != null ? Instantiate(phoneContent) : ScriptableObject.CreateInstance<PhoneContent>();
            Directory = plan != null ? plan.directory : directory != null ? Instantiate(directory) : ScriptableObject.CreateInstance<WorldDirectory>();
            Room = plan != null ? plan.room : roomContent != null ? Instantiate(roomContent) : ScriptableObject.CreateInstance<RoomContent>();
            GameClock.Start(Phone.startTime);
            if (director == null)
                director = FindAnyObjectByType<CallDirector>();
            if (player == null)
                player = FindAnyObjectByType<FirstPersonController>();
            if (GetComponent<Sfx>() == null)
                gameObject.AddComponent<Sfx>();
        }

        void OnEnable()
        {
            Build();
            phoneAction?.action.Enable();
            backAction?.action.Enable();
            Clipboard.Copied += OnCopied;
            FirstPersonController.PointerOverUI = IsPointerOverUI;
        }

        void OnDisable()
        {
            if (Paused)
                SetPaused(false);
            Clipboard.Copied -= OnCopied;
            if (FirstPersonController.PointerOverUI == (Func<bool>)IsPointerOverUI)
                FirstPersonController.PointerOverUI = null;
        }

        void Build()
        {
            if (built)
                return;
            var doc = GetComponent<UIDocument>();
            root = doc.rootVisualElement;
            if (root == null)
                return;
            built = true;
            root.pickingMode = PickingMode.Ignore;
            root.AddToClassList("dcm-root");

            Hud = new HudView(TogglePhone);
            root.Add(Hud.Root);

            CallHud = new CallHud();
            CallHud.Clicked += () =>
            {
                if (openPanel != null)
                    ClosePanel();
                PhoneView.Raise(true);
            };
            root.Add(CallHud.Root);

            panelLayer = Layer();
            dim = UIKit.Div("dim");
            dim.pickingMode = PickingMode.Ignore;
            panelLayer.Add(dim);
            root.Add(panelLayer);

            Transcript = new TranscriptPanel();
            root.Add(Transcript.Root);

            PhoneView = new PhoneController(this, Phone, Directory);
            root.Add(PhoneView.Root);

            Pressure = new PressureOverlay();
            root.Add(Pressure.Root);

            overlayLayer = Layer();
            root.Add(overlayLayer);
            // Over the panels (it guides inside them), under the cards, modals and the pause menu.
            Tutorial = new TutorialCard();
            overlayLayer.Add(Tutorial.Root);
            overlayLayer.Add(Hud.Toasts);

            Fade = new Fader(false);
            root.Add(Fade.Root);
        }

        VisualElement Layer()
        {
            var l = new VisualElement();
            l.style.position = Position.Absolute;
            l.style.left = 0;
            l.style.top = 0;
            l.style.right = 0;
            l.style.bottom = 0;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        void Update()
        {
            if (!built)
                return;
            float dt = Time.deltaTime;
            GameClock.Tick(dt);
            Hud.SetTime(GameClock.Now);
            Hud.SetBadge(PhoneView.TotalBadges);
            PhoneView.Tick(dt);
            HandleInput();
            PutPhoneAwayOnRoomPress();

            bool inCall = director != null && director.IsCallActive;
            Transcript.SetVisible(inCall && PhoneView.IsUp);
            CallHud.SetVisible(inCall && !PhoneView.IsUp);
            Hud.SetHintsVisible(!PhoneView.IsUp && openPanel == null && card == null && (player == null || !player.IsSeated));
            Hud.SetButtonsVisible(!PhoneView.IsUp);

            bool locked = PhoneView.IsUp || openPanel != null || modal != null || endingCard != null || card != null || Paused;
            if (locked != InputLocked)
            {
                InputLocked = locked;
                if (player != null)
                    player.SetInputLocked(locked);
                if (locked)
                    Hud.HidePrompt();
            }
        }

        /// <summary>
        /// A press on the room (anywhere that is not UI) while the phone is up lowers the phone, so
        /// the same drag turns the view. A ringing phone stays up: the call has to be answered.
        /// </summary>
        void PutPhoneAwayOnRoomPress()
        {
            var mouse = Mouse.current;
            if (mouse == null || !PhoneView.IsUp || PhoneView.Locked || openPanel != null || modal != null || endingCard != null ||
                card != null || Paused)
                return;
            if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame)
                return;
            if (!IsPointerOverUI())
                PhoneView.Raise(false);
        }

        void HandleInput()
        {
            bool typing = root.panel?.focusController?.focusedElement is TextField ||
                          root.panel?.focusController?.focusedElement is VisualElement v && v.GetFirstAncestorOfType<TextField>() != null;

            if (phoneAction != null && phoneAction.action.WasPressedThisFrame())
            {
                if (modal == null && endingCard == null && card == null && !Paused)
                {
                    if (openPanel != null)
                    {
                        ClosePanel();
                        PhoneView.Raise(true);
                    }
                    else
                    {
                        TogglePhone();
                    }
                }
            }

            bool back = backAction != null && backAction.action.WasPressedThisFrame();
            if (!back && Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame && (PhoneView.IsUp || openPanel != null))
                back = true;
            if (back)
                Back();

            var kb = Keyboard.current;
            if (kb == null || typing || modal != null || endingCard != null || card != null || Paused)
                return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                Choose(0);
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                Choose(1);
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                Choose(2);
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
                Choose(3);
        }

        /// <summary>Keys 1–4: a decision's option, or a question for a holding caller.</summary>
        void Choose(int index)
        {
            if (!PhoneView.IsUp || director == null || !director.IsCallActive)
                return;
            if (Transcript.HasDecision)
                Transcript.Pick(index);
            else if (Transcript.HasQuestions)
                Transcript.PickQuestion(index);
        }

        /// <summary>Esc: close the top thing (modal, phone screen, panel); with nothing open, pause.</summary>
        public void Back()
        {
            if (modal != null)
            {
                CloseModal();
                return;
            }
            if (Paused)
            {
                if (!pauseMenu.CloseSettings())
                    SetPaused(false);
                return;
            }
            if (endingCard != null)
                return;
            if (PhoneView.IsUp && !PhoneView.Locked)
            {
                PhoneView.Back();
                return;
            }
            if (openPanel != null)
            {
                if (!openPanel.Back())
                    ClosePanel();
                return;
            }
            SetPaused(true);
        }

        // ---------------------------------------------------------------- pause

        /// <summary>Freezes the game (time, voices, the ringtone) and shows the pause menu; music keeps playing, quieter.</summary>
        public void SetPaused(bool on)
        {
            if (on == Paused || !built)
                return;
            if (on)
            {
                pauseMenu = new PauseMenuView($"{GameClock.DayLabel}  ·  {GameClock.Now}", () => SetPaused(false), ConfirmMainMenu, ConfirmLanguage);
                overlayLayer.Add(pauseMenu.Root);
                Time.timeScale = 0f;
                AudioListener.pause = true;
                MusicPlayer.Current?.Duck(true);
                Hud.HidePrompt();
                Sfx.Play(Sfx.Paper, 0.6f);
            }
            else
            {
                pauseMenu.Root.RemoveFromHierarchy();
                pauseMenu = null;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                MusicPlayer.Current?.Duck(false);
            }
        }

        void ConfirmMainMenu()
        {
            Confirm(Loc.T("Back to the main menu?"), Loc.T("Today's case ends here. You can start the day again from the title screen."),
                    Loc.T("Main menu"), Loc.T("Stay"), () =>
                    {
                        MusicPlayer.Current?.Stop(0.8f);
                        Fade.FadeOut(0.8f, () => SceneFlow.Load(SceneFlow.Home));
                    });
        }

        /// <summary>
        /// A new language from the pause menu: today's case starts again in that language (the call
        /// and the evidence are built per language).
        /// </summary>
        void ConfirmLanguage(Lang lang)
        {
            // Asked in the language the player picked.
            var previous = Loc.Current;
            Loc.Use(lang);
            string title = Loc.T("Restart today's case?"), text = Loc.F("The case starts again from the beginning, in {0}.", Loc.Name(lang)),
                   yes = Loc.T("Restart"), no = Loc.T("Stay");
            Loc.Use(previous);
            Confirm(title, text, yes, no, () =>
            {
                Loc.Current = lang;
                var dayDirector = FindAnyObjectByType<DayDirector>();
                int day = dayDirector?.Day?.day ?? GameRun.FirstDay;
                // The same case and truth again, not another one of the day's cases.
                if (dayDirector?.Plan?.variant != null)
                    GameRun.ForceNextVariant(dayDirector.Plan.variant.id);
                MusicPlayer.Current?.Stop(0.8f);
                Fade.FadeOut(0.8f, () => SceneFlow.PlayDay(day));
            });
        }

        // ---------------------------------------------------------------- day and case cards

        /// <summary>The black DAY card; <paramref name="onDone"/> runs once it has faded into the room.</summary>
        public DayCardView ShowDayCard(DayData day, string intro, IReadOnlyList<string> extraLines, Action onDone)
        {
            var view = new DayCardView(day, intro, extraLines, () =>
            {
                card = null;
                onDone?.Invoke();
            });
            card = view.Root;
            overlayLayer.Add(view.Root);
            return view;
        }

        /// <summary>The CASE OPENED file; <paramref name="onStart"/> runs when the player starts investigating.</summary>
        public void ShowCaseCard(int day, CaseInfo info, CallerInfo caller, string callerTitle, Action onStart)
        {
            ClosePanel();
            var view = new CaseCardView(day, info, caller, callerTitle, () =>
            {
                card = null;
                onStart?.Invoke();
            });
            card = view.Root;
            overlayLayer.Add(view.Root);
            // The guide's note explains the case file, so it stays in the light, over the file's shade.
            Tutorial.Root.BringToFront();
        }

        public void TogglePhone()
        {
            if (PhoneView.Locked)
                return;
            if (openPanel != null)
                ClosePanel();
            PhoneView.Toggle();
        }

        // ---------------------------------------------------------------- room panels

        public void OpenPanel(PanelId id)
        {
            if (!built || id == PanelId.None)
                return;
            if (PhoneView.Locked)
                return;
            if (openPanel != null)
                ClosePanel();
            if (PhoneView.IsUp)
                PhoneView.Raise(false);
            RoomPanel p = id switch
            {
                PanelId.Newspaper => new NewspaperPanel(this),
                PanelId.Drawer => new DrawerPanel(this),
                PanelId.Calendar => new CalendarPanel(this),
                PanelId.Computer => new ComputerPanel(this),
                _ => null,
            };
            if (p == null)
                return;
            openPanel = p;
            ClueEvents.Raise(ClueEvent.PanelOpened, id.ToString());
            p.Root.EnableInClassList("room-panel--guided", Tutorial.IsOn);
            panelLayer.Add(p.Root);
            dim.AddToClassList("dim--on");
            p.Root.schedule.Execute(() => p.Root.AddToClassList("room-panel--open")).ExecuteLater(20);
            p.OnOpen();
            PanelOpened?.Invoke(id);
        }

        public void ClosePanel()
        {
            if (openPanel == null)
                return;
            var p = openPanel;
            openPanel = null;
            dim.RemoveFromClassList("dim--on");
            p.Root.RemoveFromClassList("room-panel--open");
            p.Root.schedule.Execute(() => p.Root.RemoveFromHierarchy()).ExecuteLater(220);
            PanelClosed?.Invoke(p.Id);
        }

        // ---------------------------------------------------------------- modals, toasts, endings

        public void Confirm(string title, string text, string yes, string no, Action onYes, Action onNo = null)
        {
            CloseModal();
            modal = new ModalView(title, text, yes, no, () =>
            {
                CloseModal();
                onYes?.Invoke();
            }, () =>
            {
                CloseModal();
                onNo?.Invoke();
            });
            overlayLayer.Add(modal.Root);
        }

        void CloseModal()
        {
            modal?.Root.RemoveFromHierarchy();
            modal = null;
        }

        public void ShowEndingCard(EndingCard info, Action onClose)
        {
            endingCard?.RemoveFromHierarchy();
            var shade = UIKit.Div("modal");
            var card = UIKit.Div("modal__box", "paper", "ending");
            var head = UIKit.Div("ending__head");
            head.Add(UIKit.Portrait(info.portrait, "ending__portrait"));
            var who = UIKit.Div("grow");
            who.Add(UIKit.Text(Loc.T("CASE CLOSED"), "section"));
            who.Add(UIKit.Text(info.caller, "ending__caller"));
            head.Add(who);
            card.Add(head);
            card.Add(UIKit.Text(info.verdict, "ending__verdict"));
            var stamps = UIKit.Div("ending__stamps");
            if (!string.IsNullOrEmpty(info.truth))
                stamps.Add(Stamp(info.truth, info.truthGood, -7f, 450));
            if (!string.IsNullOrEmpty(info.judgement))
                stamps.Add(Stamp(info.judgement, info.judgementGood, 5f, 950));
            if (stamps.childCount > 0)
                card.Add(stamps);
            if (!string.IsNullOrEmpty(info.subtitle))
                card.Add(UIKit.Text(info.subtitle, "modal__text", "t-center"));
            if (!string.IsNullOrEmpty(info.money))
                card.Add(UIKit.Text(info.money, "recipient__amount", "t-center", info.moneyIn ? "amount-in" : "amount-out"));
            if (!string.IsNullOrEmpty(info.consequence))
                card.Add(UIKit.Text(info.consequence, "ending__line"));
            var buttons = UIKit.Div("modal__buttons");
            buttons.Add(UIKit.Btn(Loc.T("Continue"), () =>
            {
                endingCard?.RemoveFromHierarchy();
                endingCard = null;
                onClose?.Invoke();
            }, "btn--green"));
            card.Add(buttons);
            shade.Add(card);
            endingCard = shade;
            overlayLayer.Add(shade);
        }

        /// <summary>A rubber stamp that lands on the card after a delay, with a thump.</summary>
        static VisualElement Stamp(string text, bool good, float angle, long delayMs)
        {
            var stamp = UIKit.Text(text, "stamp", good ? "stamp--green" : "stamp--red");
            stamp.style.whiteSpace = WhiteSpace.NoWrap;
            stamp.style.rotate = new Rotate(angle);
            stamp.schedule.Execute(() =>
            {
                stamp.AddToClassList("stamp--down");
                Sfx.Play(Sfx.Stamp);
            }).ExecuteLater(delayMs);
            return stamp;
        }

        public void Toast(string icon, string title, string text) => Hud?.Toast(icon, title, text);

        void OnCopied(Fact f) => Hud?.Toast("ic_copy", Loc.T("Copied"), f.value + Loc.T("  ·  paste it on the computer"));

        /// <summary>Facts worth offering as one-click suggestions: today's case first, then recent copies.</summary>
        public List<Fact> SuggestedFacts(params FactKind[] kinds)
        {
            var list = new List<Fact>();
            var seen = new HashSet<string>();
            void Take(IEnumerable<Fact> facts)
            {
                foreach (var f in facts)
                {
                    if (list.Count >= 6)
                        return;
                    if (f == null || string.IsNullOrEmpty(f.value) || seen.Contains(f.value))
                        continue;
                    if (kinds.Length > 0 && Array.IndexOf(kinds, f.kind) < 0)
                        continue;
                    seen.Add(f.value);
                    list.Add(f);
                }
            }
            if (CurrentCase != null)
            {
                Take(new[] { new Fact(FactKind.Phone, CurrentCase.caller.number) });
                Take(CurrentCase.facts);
            }
            Take(Clipboard.History);
            return list;
        }

        // ---------------------------------------------------------------- pointer, prompt

        public bool IsPointerOverUI()
        {
            if (root?.panel == null || Mouse.current == null)
                return false;
            var p = Mouse.current.position.ReadValue();
            var panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(p.x, Screen.height - p.y));
            var picked = root.panel.Pick(panelPos);
            return picked != null && picked != root && IsSeen(picked);
        }

        /// <summary>
        /// False for an element that is faded out (its own or an ancestor's opacity is near zero):
        /// the player cannot see it, so a press there belongs to the room.
        /// </summary>
        static bool IsSeen(VisualElement e)
        {
            float opacity = 1f;
            for (var c = e; c != null; c = c.parent)
                opacity *= c.resolvedStyle.opacity;
            return opacity > 0.05f;
        }

        public void ShowPrompt(string text, Vector2 screenPosition)
        {
            if (root?.panel == null || InputLocked)
                return;
            var panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            Hud.ShowPrompt(Loc.T(text), panelPos);
        }

        public void HidePrompt() => Hud?.HidePrompt();
    }
}
