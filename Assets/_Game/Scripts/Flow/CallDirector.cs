using System;
using System.Collections;
using System.Collections.Generic;
using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.UI;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>
    /// Runs one call at a time on the UI templates: the forced incoming call (slide to answer),
    /// voiced lines revealed as they are spoken, decisions with the patience timer and pressure
    /// lines, the hold (the caller waits on the line while the player investigates and can ask
    /// questions), the verdict on the call and the endings. A <see cref="DayDirector"/> drives
    /// the hold's clock.
    /// </summary>
    public class CallDirector : MonoBehaviour
    {
        [SerializeField] UIManager ui;
        [Tooltip("Off during a day: the DayDirector shows the result the next morning instead.")]
        public bool showEndingCard = true;

        enum State { Idle, Ringing, Active, Ending }

        State state;
        ConversationData conv;
        string title;
        string voice;
        CallerInfo caller;
        float callSeconds;
        ConvDecision pending;
        float patience;
        float patienceMax;
        bool timerRunning;
        bool lineClosed;
        bool onHold;
        readonly HashSet<PressureLine> firedPressure = new HashSet<PressureLine>();
        readonly HashSet<ConvQuestion> asked = new HashSet<ConvQuestion>();
        readonly Queue<ConvLine> sayQueue = new Queue<ConvLine>();
        IncomingCallView incoming;
        InCallView inCall;
        Coroutine running;

        /// <summary>The caller has made the ask and is holding the line.</summary>
        public event Action HoldStarted;

        /// <summary>The conversation reached an ending (after its closing lines).</summary>
        public event Action<ConvEnding> ConversationEnded;

        /// <summary>The call has been answered and is on screen.</summary>
        public bool IsCallActive => state == State.Active || state == State.Ending;
        public bool IsRinging => state == State.Ringing;
        public bool IsBusy => state != State.Idle;
        public bool OnHold => onHold && state == State.Active;
        /// <summary>The caller is waiting for one of the two replies.</summary>
        public bool HasDecision => pending != null;

        /// <summary>A case is on the line: the phone is only for investigating and the verdict is given on the call.</summary>
        public bool IsCaseCall => conv != null && (state == State.Active || state == State.Ending);
        public ConversationData Current => conv;
        public float CallSeconds => callSeconds;

        /// <summary>Screen-edge pressure from outside a decision (the day's deadline); negative for none.</summary>
        public float ExternalPressure { get; set; } = -1f;

        PhoneController Phone => ui.PhoneView;

        void Awake()
        {
            if (ui == null)
                ui = FindAnyObjectByType<UIManager>();
        }

        // ---------------------------------------------------------------- start

        public void StartCall(ConversationData data)
        {
            if (IsBusy || data == null || ui == null)
                return;
            conv = data;
            caller = data.caller;
            voice = data.caller.voice;
            var contact = ui.Phone.FindContact(caller.number);
            title = contact != null ? contact.name : Loc.T("Unknown");
            OpenCase();
            state = State.Ringing;
            ui.ClosePanel();
            Phone.Raise(true);
            Phone.Locked = true;
            incoming = new IncomingCallView(Phone, caller, title);
            incoming.Slider.Answered += Answer;
            Phone.ShowOnly(incoming);
            Sfx.Loop(Sfx.Ring, 0.9f);
        }

        void Answer()
        {
            if (state != State.Ringing)
                return;
            Sfx.StopLoop();
            Phone.Locked = false;
            Phone.RemoveScreen(incoming);
            incoming = null;
            BeginCallScreen(caller, title, Loc.T("ON CALL"), allowHangUp: false);
            state = State.Active;
            ui.Transcript.Open(caller, title);
            ui.CallHud.Set(title, caller.number, caller.portrait);
            ui.Transcript.AddSystem(Loc.F("Call started · {0}", GameClock.Now));
            Run(conv.nodes.Count > 0 ? conv.nodes[0] : null);
        }

        /// <summary>Shows the in-call screen in place of whatever the phone showed.</summary>
        void BeginCallScreen(CallerInfo who, string name, string status, bool allowHangUp = true)
        {
            if (inCall != null)
                Phone.RemoveScreen(inCall);
            inCall = new InCallView(Phone, who, name, status);
            inCall.HangUpClicked += HangUp;
            // On a case the call ends through the verdict, not the phone's red button.
            inCall.SetHangUpEnabled(allowHangUp);
            Phone.CallView = inCall;
            Phone.ShowOnly(inCall);
            callSeconds = 0f;
            lineClosed = false;
        }

        void OpenCase()
        {
            var c = new CaseFile { caller = caller, callerTitle = $"{title} · {caller.number}" };
            c.claims.AddRange(conv.claims);
            ui.CurrentCase = c;
            asked.Clear();
            sayQueue.Clear();
            onHold = false;
        }

        // ---------------------------------------------------------------- playback

        void Run(ConvNode node)
        {
            if (running != null)
                StopCoroutine(running);
            running = node != null ? StartCoroutine(PlayNode(node)) : null;
        }

        IEnumerator PlayNode(ConvNode node)
        {
            foreach (var line in node.lines)
                yield return PlayLine(line, caller.portrait, voice);
            running = null;
            if (node.hasDecision)
            {
                Present(node.decision);
                yield break;
            }
            if (node.holds)
            {
                EnterHold();
                yield break;
            }
            GoTo(node.next);
        }

        /// <summary>
        /// One line. A voiced caller line plays its clip and its text is revealed while it is spoken;
        /// without a clip the caller "types" for a moment first.
        /// </summary>
        IEnumerator PlayLine(ConvLine line, string portrait, string voiceId)
        {
            bool fromCaller = line.speaker == Speaker.Caller;
            var clip = fromCaller ? VoiceBank.Lookup(voiceId, line.Spoken) : null;
            if (fromCaller && clip == null)
            {
                SetTyping(true, portrait);
                yield return new WaitForSeconds(Mathf.Clamp(line.text.Length / 32f, 0.7f, 2.4f));
                SetTyping(false, portrait);
            }
            else
            {
                yield return new WaitForSeconds(fromCaller ? 0.2f : 0.25f);
            }
            float speaking = VoicePlayer.Play(clip);
            Show(line.speaker, line.text, line.facts, portrait, speaking);
            if (line.facts != null)
                foreach (var f in line.facts)
                    ui.CurrentCase?.AddFact(f);
            Deliver(line.deliver);
            if (speaking > 0f)
                yield return new WaitForSeconds(speaking);
            yield return new WaitForSeconds(line.pauseAfter + (fromCaller ? 0.3f : 0.1f));
        }

        void SetTyping(bool on, string portrait) => ui.Transcript.SetTyping(on, portrait);

        void Show(Speaker speaker, string text, List<Fact> facts, string portrait, float reveal = 0f)
        {
            if (speaker == Speaker.System)
            {
                ui.Transcript.AddSystem(text);
                return;
            }
            ui.Transcript.AddLine(speaker, text, facts, portrait, reveal);
            if (reveal <= 0f)
                Sfx.Play(Sfx.Type, 0.6f);
            if (speaker == Speaker.Caller)
                ui.CallHud.SetLine(text);
        }

        public void Deliver(List<Delivery> items)
        {
            if (items == null)
                return;
            foreach (var d in items)
            {
                switch (d.kind)
                {
                    case DeliveryKind.BankDeposit:
                        Phone.App<BankApp>().Deposit(d.amount, d.from, d.text);
                        break;
                    case DeliveryKind.ChatMessage:
                        Phone.App<ChatsApp>().Append(d.from, new ChatMessage { sender = d.title, when = Loc.Today + " " + GameClock.Now, text = d.text });
                        break;
                }
            }
        }

        // ---------------------------------------------------------------- decisions

        void Present(ConvDecision d)
        {
            pending = d;
            patienceMax = Mathf.Max(5f, d.patienceSeconds);
            patience = patienceMax;
            firedPressure.Clear();
            ui.Transcript.ShowDecision(d, Pick);
            timerRunning = true;
        }

        void Pick(int index)
        {
            if (pending == null)
                return;
            var option = index == 0 ? pending.a : pending.b;
            ClearDecision();
            running = StartCoroutine(AfterPick(option));
        }

        IEnumerator AfterPick(ConvOption option)
        {
            if (!string.IsNullOrEmpty(option.playerLine))
            {
                Show(Speaker.Player, option.playerLine, null, null);
                yield return new WaitForSeconds(0.5f);
            }
            running = null;
            GoTo(option.next);
        }

        void GoTo(string id)
        {
            if (conv == null)
                return;
            var node = conv.FindNode(id);
            if (node != null)
            {
                Run(node);
                return;
            }
            var ending = conv.FindEnding(id);
            if (ending != null)
            {
                EndWith(ending, caller.portrait, voice);
                return;
            }
            Debug.LogError($"[CallDirector] {conv.name}: no node or ending called '{id}'");
            Cleanup();
        }

        void ClearDecision()
        {
            pending = null;
            ui.Transcript.ShowIdle();
            ui.CallHud.SetDecision(false, 0f, 1f);
        }

        // ---------------------------------------------------------------- the hold

        void EnterHold()
        {
            onHold = true;
            ShowHoldOptions();
            ui.Transcript.ShowVerdict(BuildVerdict());
            HoldStarted?.Invoke();
        }

        // ---------------------------------------------------------------- the verdict

        ActionTrigger TransferTrigger => conv.actions.Find(a => a.kind == ActionKind.Transfer);

        VerdictPanel BuildVerdict()
        {
            var info = conv.verdict ?? new VerdictInfo();
            var t = TransferTrigger;
            string detail = info.goAlongDetail;
            string sendTitle = (info.goAlong ?? Loc.T("GO ALONG")).ToUpperInvariant() + "?";
            string sendTo = null;
            if (t != null)
            {
                string bank = string.IsNullOrEmpty(t.bank) ? "" : t.bank + " ";
                if (string.IsNullOrEmpty(detail))
                    detail = Loc.F("{0} to {1}{2}", FactText.Won(t.amount), bank, t.target);
                string holder = ui.Directory.FindAccount(t.target)?.holder;
                sendTitle = Loc.F("SEND {0}?", FactText.Won(t.amount));
                sendTo = string.IsNullOrEmpty(holder) ? Loc.F("To {0}{1}", bank, t.target) : Loc.F("To {0}  ·  {1}{2}", holder, bank, t.target);
            }
            var panel = new VerdictPanel(info, detail, sendTitle, sendTo, OnVerdict);
            if (t != null)
                panel.SendShown += () => ClueEvents.Raise(ClueEvent.RecipientShown, t.target);
            return panel;
        }

        /// <summary>The verdict from the call: say it, then act on it. The caller is cut off mid-sentence.</summary>
        void OnVerdict(VerdictKind kind)
        {
            if (!OnHold)
                return;
            StopSpeech();
            ClearDecision();
            running = StartCoroutine(Decide(kind));
        }

        IEnumerator Decide(VerdictKind kind)
        {
            var info = conv.verdict ?? new VerdictInfo();
            string line = kind == VerdictKind.GoAlong ? info.goAlongLine : info.refuseLine;
            if (!string.IsNullOrEmpty(line))
            {
                Show(Speaker.Player, line, null, null);
                yield return new WaitForSeconds(0.9f);
            }
            running = null;
            if (kind == VerdictKind.GoAlong)
                SendTheMoney();
            else
                HangUp();
        }

        void SendTheMoney()
        {
            StopSpeech();
            ClearDecision();
            var t = TransferTrigger;
            var info = conv.verdict ?? new VerdictInfo();
            if (t != null)
            {
                string holder = ui.Directory.FindAccount(t.target)?.holder ?? t.target;
                Phone.App<BankApp>().RecordTransfer(holder, t.bank, t.target, t.amount);
                ui.Transcript.AddSystem(Loc.F("You sent {0} to {1} ({2} {3}).", FactText.Won(t.amount), holder, t.bank, t.target), "bad");
                Sfx.Play(Sfx.Success, 0.7f);
            }
            GoTo(!string.IsNullOrEmpty(info.goAlongEndingId) ? info.goAlongEndingId : t != null ? t.endingId : conv.hangUpEndingId);
        }

        void ShowHoldOptions()
        {
            if (!OnHold)
                return;
            var open = conv.questions.FindAll(q => !asked.Contains(q));
            ui.Transcript.ShowHold(title, open, Ask);
            ui.CallHud.SetIdleHint(Loc.T("Holding the line  ·  Tab to ask or decide"));
        }

        /// <summary>Put one of the conversation's questions to the holding caller.</summary>
        public void Ask(ConvQuestion question)
        {
            if (!OnHold || running != null || question == null || asked.Contains(question))
                return;
            asked.Add(question);
            ui.Transcript.ShowHold(title, null, null);
            running = StartCoroutine(AskRoutine(question));
        }

        IEnumerator AskRoutine(ConvQuestion question)
        {
            if (!string.IsNullOrEmpty(question.playerLine))
            {
                Show(Speaker.Player, question.playerLine, null, null);
                yield return new WaitForSeconds(0.6f);
            }
            foreach (var line in question.answer)
                yield return PlayLine(line, caller.portrait, voice);
            running = null;
            if (!string.IsNullOrEmpty(question.endingId))
            {
                onHold = false;
                GoTo(question.endingId);
                yield break;
            }
            AfterHoldSpeech();
        }

        /// <summary>A line from the holding caller (a pressure beat). Queued behind anything being said.</summary>
        public void Say(ConvLine line)
        {
            if (!OnHold || line == null)
                return;
            sayQueue.Enqueue(line);
            if (running == null)
                running = StartCoroutine(SayRoutine());
        }

        IEnumerator SayRoutine()
        {
            ui.Transcript.ShowHold(title, null, null);
            while (sayQueue.Count > 0 && OnHold)
                yield return PlayLine(sayQueue.Dequeue(), caller.portrait, voice);
            running = null;
            AfterHoldSpeech();
        }

        void AfterHoldSpeech()
        {
            if (!OnHold)
                return;
            if (sayQueue.Count > 0)
                running = StartCoroutine(SayRoutine());
            else
                ShowHoldOptions();
        }

        /// <summary>The caller's deadline passed with no decision.</summary>
        public void Timeout()
        {
            if (state != State.Active || conv == null)
                return;
            StopSpeech();
            ClearDecision();
            ui.Transcript.AddSystem(Loc.F("It's {0}. Time's up.", GameClock.Now), "bad");
            GoTo(conv.timeoutEndingId);
        }

        void StopSpeech()
        {
            if (running != null)
                StopCoroutine(running);
            running = null;
            VoicePlayer.Stop();
            ui.Transcript.SetTyping(false, null);
            onHold = false;
            sayQueue.Clear();
        }

        void Update()
        {
            if (ui == null || ui.PhoneView == null)
                return;
            float dt = Time.deltaTime;
            if (IsCallActive && !lineClosed)
            {
                callSeconds += dt;
                string t = $"{(int)callSeconds / 60:00}:{(int)callSeconds % 60:00}";
                Phone.CallTimerText = t;
                inCall?.SetTimer(t);
                ui.Transcript.SetCallTime(t);
            }
            if (pending != null && timerRunning && state == State.Active)
            {
                patience -= dt;
                float f = Mathf.Clamp01(patience / patienceMax);
                ui.Transcript.UpdateTimer(patience, patienceMax);
                ui.CallHud.SetDecision(true, patience, patienceMax);
                foreach (var p in pending.pressure)
                {
                    if (firedPressure.Contains(p) || f > p.atPatience)
                        continue;
                    firedPressure.Add(p);
                    var clip = VoiceBank.Lookup(voice, string.IsNullOrEmpty(p.spoken) ? p.text : p.spoken);
                    Show(Speaker.Caller, p.text, null, caller.portrait, VoicePlayer.Play(clip));
                }
                if (patience <= 0f)
                {
                    ClearDecision();
                    ui.Transcript.AddSystem(Loc.T("You took too long."), "bad");
                    GoTo(conv.timeoutEndingId);
                }
                ui.Pressure.Tick(f, dt);
            }
            else
            {
                ui.Pressure.Tick(ExternalPressure, dt);
            }
        }

        // ---------------------------------------------------------------- phone actions

        public void HangUp()
        {
            if (state != State.Active || conv == null)
                return;
            StopSpeech();
            ClearDecision();
            ui.Transcript.AddSystem(Loc.T("You hung up."));
            // The line is dead: the caller's ending lines are not heard.
            CloseLine();
            var ending = conv.FindEnding(conv.hangUpEndingId);
            if (ending != null)
                EndWith(ending, caller.portrait, voice, speak: false);
            else
                Cleanup();
        }

        void CloseLine()
        {
            if (lineClosed)
                return;
            lineClosed = true;
            inCall?.SetStatus(Loc.T("CALL ENDED"));
            // Nothing to return to: the "Return to call" bar goes away.
            Phone.CallView = null;
            ui.Transcript.AddSystem(Loc.F("Call ended · {0}", Phone.CallTimerText));
            Sfx.Play(Sfx.HangUp, 0.7f);
        }

        void EndWith(ConvEnding ending, string portrait, string voiceId, bool speak = true)
        {
            if (ending == null)
            {
                Cleanup();
                return;
            }
            state = State.Ending;
            pending = null;
            onHold = false;
            sayQueue.Clear();
            ui.Transcript.ShowIdle();
            if (running != null)
                StopCoroutine(running);
            running = StartCoroutine(PlayEnding(ending, portrait, voiceId, speak));
        }

        IEnumerator PlayEnding(ConvEnding ending, string portrait, string voiceId, bool speak)
        {
            if (speak)
                foreach (var line in ending.lines)
                    yield return PlayLine(line, portrait, voiceId);
            CloseLine();
            yield return new WaitForSeconds(1.4f);
            running = null;
            ConversationEnded?.Invoke(ending);
            if (showEndingCard)
                ShowResult(ending);
        }

        void ShowResult(ConvEnding ending)
        {
            var card = new EndingCard
            {
                caller = conv == null || string.IsNullOrEmpty(conv.caller.number) ? title : $"{title} · {conv.caller.number}",
                portrait = conv != null ? conv.caller.portrait : caller?.portrait,
                verdict = Loc.T(ending.verdict == Verdict.GoAlong ? "You went along" : ending.verdict == Verdict.Refuse ? "You refused" : "You checked first"),
                consequence = ending.consequence,
                money = ending.moneyDelta != 0 ? FactText.Won(ending.moneyDelta) : null,
                moneyIn = ending.moneyDelta > 0,
            };
            if (conv != null && conv.revealAtEnd)
            {
                bool right = ending.verdict == Verdict.Verify || (conv.isScam ? ending.verdict == Verdict.Refuse : ending.verdict == Verdict.GoAlong);
                card.truth = Loc.T(conv.isScam ? "SCAM" : "REAL");
                card.truthGood = !conv.isScam;
                card.judgement = Loc.T(right ? "RIGHT CALL" : "WRONG CALL");
                card.judgementGood = right;
            }
            else
            {
                card.subtitle = Loc.T("Tomorrow's Seoul Daily will tell you who it really was.");
            }
            ui.ShowEndingCard(card, Cleanup);
        }

        /// <summary>Close the finished conversation (the DayDirector calls this when it shows no card).</summary>
        public void Finish() => Cleanup();

        void Cleanup()
        {
            if (running != null)
                StopCoroutine(running);
            running = null;
            VoicePlayer.Stop();
            Sfx.StopLoop();
            ClearDecision();
            if (inCall != null)
            {
                Phone.RemoveScreen(inCall);
                Phone.CallView = null;
                inCall = null;
            }
            if (incoming != null)
                Phone.RemoveScreen(incoming);
            Phone.Locked = false;
            incoming = null;
            state = State.Idle;
            conv = null;
            onHold = false;
            sayQueue.Clear();
        }
    }
}
