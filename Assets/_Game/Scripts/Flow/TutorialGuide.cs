using DontCallMe.Data;
using DontCallMe.Gameplay;
using DontCallMe.Player;
using DontCallMe.UI;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>
    /// The first day's guide: one instruction at a time on the tutorial card, from looking around
    /// to the verdict. The step shown follows what is happening in the game, so doing things in
    /// another order (or skipping ahead) never gets it stuck. The object a step is about is
    /// spotlit. The day's call waits for <see cref="ReadyForCall"/>.
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        const int Steps = 7;

        UIManager ui;
        CallDirector director;
        DayDirector day;
        FirstPersonController player;
        Interactor interactor;
        Transform view;

        Quaternion lastView;
        float turned;
        bool paperOpened;
        bool paperRead;
        bool sawDecision;
        bool checkedAccount;
        bool checkedNumber;
        bool running;

        /// <summary>The newspaper has been read (opened and closed): the phone may ring.</summary>
        public bool ReadyForCall => paperRead;

        public void Begin(UIManager ui, CallDirector director, DayDirector day, FirstPersonController player)
        {
            this.ui = ui;
            this.director = director;
            this.day = day;
            this.player = player;
            interactor = FindAnyObjectByType<Interactor>();
            var cam = player != null ? player.GetComponentInChildren<Camera>() : Camera.main;
            view = cam != null ? cam.transform : null;
            if (view != null)
                lastView = view.rotation;
            ui.PanelOpened += OnPanelOpened;
            ui.PanelClosed += OnPanelClosed;
            ClueEvents.Happened += OnClue;
            running = true;
        }

        void OnDestroy() => Stop();

        public void Stop()
        {
            if (!running)
                return;
            running = false;
            if (ui != null)
            {
                ui.PanelOpened -= OnPanelOpened;
                ui.PanelClosed -= OnPanelClosed;
                ui.Tutorial?.Hide();
            }
            ClueEvents.Happened -= OnClue;
            if (interactor != null)
                interactor.Spotlight = PanelId.None;
        }

        void OnPanelOpened(PanelId id)
        {
            if (id == PanelId.Newspaper)
                paperOpened = true;
        }

        void OnPanelClosed(PanelId id)
        {
            if (id == PanelId.Newspaper && paperOpened)
                paperRead = true;
        }

        void OnClue(ClueEvent kind, string target)
        {
            var conv = day.Plan?.Conversation;
            if (kind != ClueEvent.NumberChecked || conv == null)
                return;
            if (FactText.SameNumber(target, conv.caller.number))
                checkedNumber = true;
            var transfer = conv.actions.Find(a => a.kind == ActionKind.Transfer);
            if (transfer != null && FactText.SameNumber(target, transfer.target))
                checkedAccount = true;
        }

        void Update()
        {
            if (!running || ui == null || day == null)
                return;
            if (view != null)
            {
                turned += Quaternion.Angle(lastView, view.rotation);
                lastView = view.rotation;
            }
            if (director.HasDecision)
                sawDecision = true;

            var spot = PanelId.None;
            switch (day.Current)
            {
                case DayDirector.Phase.Seated:
                    if (turned < 25f && !paperOpened)
                        Show(1, "Hold the mouse button and drag to look around.");
                    else if (!paperOpened)
                    {
                        spot = PanelId.Newspaper;
                        Show(2, "Things you can use glow. Click the newspaper on the desk.");
                    }
                    else if (!paperRead)
                        Show(2, "Read today's warning in the box on the right, then close the paper (Esc).");
                    else
                        Show(3, "Good. Keep that in mind: your phone is about to ring.");
                    break;
                case DayDirector.Phase.OnCall:
                    if (director.IsRinging)
                        Show(3, "Your phone is ringing. Drag the green button to the right to answer.");
                    else if (director.HasDecision)
                        Show(4, "Pick a reply: click it, or press 1 or 2.");
                    else
                        Show(4, sawDecision ? "Listen to what he asks you to do." : "Listen. What the caller says appears next to the phone.");
                    break;
                case DayDirector.Phase.CaseCard:
                    Show(5, "This is the case. Read what he wants, then click Start investigating.");
                    break;
                case DayDirector.Phase.Investigating:
                    bool atComputer = ui.OpenPanelView is ComputerPanel;
                    if (checkedAccount && checkedNumber)
                        Show(7, atComputer ? "You know enough. Close the laptop (Esc), then press Tab to raise your phone."
                                           : ui.PhoneView.IsUp ? "Give your verdict under the conversation: send the money, or hang up."
                                                               : "Press Tab to raise your phone and give your verdict.");
                    else if (!atComputer)
                    {
                        spot = PanelId.Computer;
                        Show(6, ui.PhoneView.IsUp ? "He is holding the line. Press Tab to lower the phone, then use the laptop on the desk."
                                                  : "He is holding the line. Walk with W A S D and click the glowing laptop on the desk.");
                    }
                    else if (!checkedAccount)
                        Show(6, "Choose Check a bank account, then click the account number he gave you.");
                    else
                        Show(6, "Now choose Check a phone number, then click his number.");
                    break;
                default:
                    Stop();
                    return;
            }
            if (interactor != null)
                interactor.Spotlight = spot;
        }

        void Show(int step, string instruction) => ui.Tutorial.Show(step, Steps, Loc.T(instruction));
    }
}
