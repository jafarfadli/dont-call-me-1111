using System;
using System.Collections.Generic;
using DontCallMe.Data;
using DontCallMe.Gameplay;
using DontCallMe.Player;
using DontCallMe.UI;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>
    /// The guide of a tutorial day (Day 0): one instruction at a time on the yellow note, from
    /// looking around to the verdict. Before the call it teaches looking, the newspaper and
    /// answering; once the caller holds, it walks through the day's clues in their order, saying
    /// for each what that place is for and what to do (<see cref="ClueDef.guide"/>), and showing
    /// what the last one taught. The step follows what has happened in the game, so doing things
    /// in another order never gets it stuck. The object a step is about is spotlit. The day's call
    /// waits for <see cref="ReadyForCall"/>.
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        /// <summary>Steps before the clues: look, newspaper, answer, reply, the case card.</summary>
        const int Lead = 5;

        UIManager ui;
        CallDirector director;
        DayDirector day;
        Interactor interactor;
        Transform view;
        ClueTracker tracker;
        List<ClueDef> clues = new List<ClueDef>();

        Quaternion lastView;
        float turned;
        bool paperOpened;
        bool paperRead;
        bool sawDecision;
        bool running;
        string learned;

        /// <summary>The newspaper has been read (opened and closed): the phone may ring.</summary>
        public bool ReadyForCall => paperRead;

        public void Begin(UIManager ui, CallDirector director, DayDirector day, FirstPersonController player)
        {
            this.ui = ui;
            this.director = director;
            this.day = day;
            interactor = FindAnyObjectByType<Interactor>();
            var cam = player != null ? player.GetComponentInChildren<Camera>() : Camera.main;
            view = cam != null ? cam.transform : null;
            if (view != null)
                lastView = view.rotation;
            tracker = day.Clues;
            clues = day.Plan?.variant?.clues ?? new List<ClueDef>();
            ui.PanelOpened += OnPanelOpened;
            ui.PanelClosed += OnPanelClosed;
            if (tracker != null)
                tracker.Found += OnFound;
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
            if (tracker != null)
                tracker.Found -= OnFound;
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

        /// <summary>What the place just checked showed stays on the note until the next find.</summary>
        void OnFound(ClueDef clue) => learned = clue.text;

        int Total => Lead + Mathf.Max(0, clues.Count - 1) + 1;

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
                        Show(2, "Every morning's paper warns about the trick going round. Things you can use glow: click the newspaper on the desk.");
                    }
                    else if (!paperRead)
                        Show(2, "Read today's warning in the box on the right, then close the paper (Esc).");
                    else
                        Show(3, "Keep that warning in mind: your phone is about to ring.");
                    break;
                case DayDirector.Phase.OnCall:
                    if (director.IsRinging)
                        Show(3, "Your phone is ringing. Drag the green button to the right to answer.");
                    else if (director.HasDecision)
                        Show(4, "Pick a reply: click it, or press 1 or 2.");
                    else
                        Show(4, sawDecision ? "Listen to what the caller asks you to do." : "Listen. What the caller says appears next to the phone.");
                    break;
                case DayDirector.Phase.CaseCard:
                    Show(5, "This is the case: who the caller says they are and what they want. Click Start investigating.");
                    break;
                case DayDirector.Phase.Investigating:
                    spot = Investigate();
                    break;
                default:
                    Stop();
                    return;
            }
            if (interactor != null)
                interactor.Spotlight = spot;
        }

        /// <summary>The first clue not found yet, with how to get there from where the player is; then the verdict.</summary>
        PanelId Investigate()
        {
            int step = Lead;
            foreach (var clue in clues)
            {
                // The newspaper was the lesson before the call.
                if (IsPaper(clue))
                    continue;
                step++;
                if (tracker != null && tracker.IsFound(clue.id))
                    continue;
                var place = PlaceOf(clue);
                ui.Tutorial.Show(step, Total, learned, Way(place) + clue.guide);
                return place;
            }
            string last = ui.OpenPanelView != null ? "You have checked everything. Close this (Esc), then press Tab to raise your phone."
                        : ui.PhoneView.IsUp ? "You have checked everything. Give your verdict under the conversation: send the money, or hang up."
                                            : "You have checked everything. Press Tab to raise your phone and give your verdict.";
            ui.Tutorial.Show(Total, Total, learned, Loc.T(last));
            return PanelId.None;
        }

        static bool IsPaper(ClueDef clue) => PlaceOf(clue) == PanelId.Newspaper;

        /// <summary>Where a clue is found: one of the room's panels, or None for the phone.</summary>
        static PanelId PlaceOf(ClueDef clue)
        {
            if (clue.when.Count == 0)
                return PanelId.None;
            var first = clue.when[0];
            switch (first.kind)
            {
                case ClueEvent.PanelOpened:
                    return Enum.TryParse(first.target, out PanelId panel) ? panel : PanelId.None;
                case ClueEvent.DocumentViewed:
                    return PanelId.Drawer;
                case ClueEvent.NumberChecked:
                    return PanelId.Computer;
                default:
                    return PanelId.None;
            }
        }

        /// <summary>What stands between the player and the place, said first: a panel to close, the phone to lower or raise.</summary>
        string Way(PanelId place)
        {
            var open = ui.OpenPanelView;
            if (place != PanelId.None)
            {
                if (open != null)
                    return open.Id == place ? "" : Loc.T("Close this first (Esc).") + " ";
                return ui.PhoneView.IsUp ? Loc.T("Lower the phone first (Tab).") + " " : "";
            }
            if (open != null || !ui.PhoneView.IsUp)
                return Loc.T("Press Tab to raise your phone.") + " ";
            return "";
        }

        /// <summary>A step before the investigation: no "what you found" line yet, the call has not made its claim.</summary>
        void Show(int step, string instruction) => ui.Tutorial.Show(step, Total, null, Loc.T(instruction));
    }
}
