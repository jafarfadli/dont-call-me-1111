using System.Collections;
using System.Collections.Generic;
using DontCallMe.Data;
using DontCallMe.UI;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>
    /// Runs one conversation at a time on the UI templates: the forced incoming call (slide to
    /// answer), lines with typing, decisions with the patience timer and pressure lines, and the
    /// endings, including the ones reached through the phone (a transfer, calling a looked-up number,
    /// hanging up). Also places ordinary outgoing calls to numbers the world directory knows.
    /// Plain-C# rules (ConversationRunner, VerdictJudge) will move to Core in phase 3.
    /// </summary>
    public class CallDirector : MonoBehaviour
    {
        [SerializeField] UIManager ui;

        enum State { Idle, Ringing, Active, Ending }

        State state;
        ConversationData conv;
        bool isChat;
        bool isOutgoing;
        string title;
        CallerInfo caller;
        float callSeconds;
        ConvDecision pending;
        float patience;
        float patienceMax;
        bool timerRunning;
        bool lineClosed;
        readonly HashSet<PressureLine> firedPressure = new HashSet<PressureLine>();
        IncomingCallView incoming;
        InCallView inCall;
        Coroutine running;
        Coroutine outgoing;

        /// <summary>A voice call is on screen: an answered call, a call back or an ordinary outgoing call.</summary>
        public bool IsCallActive => isOutgoing || ((state == State.Active || state == State.Ending) && !isChat);
        public bool IsBusy => state != State.Idle || isOutgoing;

        PhoneController Phone => ui.PhoneView;
        TalkApp Talk => Phone.App<TalkApp>();

        void Awake()
        {
            if (ui == null)
                ui = FindAnyObjectByType<UIManager>();
        }

        void OnEnable() => PhoneEvents.TransferSent += OnTransfer;

        void OnDisable() => PhoneEvents.TransferSent -= OnTransfer;

        void Start()
        {
            if (ui != null && ui.PhoneView != null)
                Talk.ThreadOpened += OnThreadOpened;
        }

        // ---------------------------------------------------------------- start

        public void StartCall(ConversationData data)
        {
            if (IsBusy || data == null || ui == null)
                return;
            conv = data;
            isChat = false;
            caller = data.caller;
            var contact = ui.Phone.FindContact(caller.number);
            title = contact != null ? contact.name : "Unknown";
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
            BeginCallScreen(caller, title, "ON CALL");
            state = State.Active;
            ui.Transcript.Open(caller, title);
            ui.CallHud.Set(title, caller.number, caller.portrait);
            ui.Transcript.AddSystem($"Call started · {GameClock.Now}");
            Run(conv.nodes.Count > 0 ? conv.nodes[0] : null);
        }

        /// <summary>
        /// Shows the in-call screen. Answered calls replace whatever the phone showed; calls the player
        /// places sit on top, so hanging up returns to the app they dialled from.
        /// </summary>
        void BeginCallScreen(CallerInfo who, string name, string status, bool replace = true)
        {
            if (inCall != null)
                Phone.RemoveScreen(inCall);
            inCall = new InCallView(Phone, who, name, status);
            inCall.HangUpClicked += HangUp;
            Phone.CallView = inCall;
            if (replace)
                Phone.ShowOnly(inCall);
            else
                Phone.Push(inCall);
            callSeconds = 0f;
            lineClosed = false;
        }

        public void StartChat(ConversationData data)
        {
            if (IsBusy || data == null || ui == null)
                return;
            conv = data;
            isChat = true;
            caller = data.caller;
            title = data.caller.displayName;
            if (Talk.Thread(caller.chatId) == null)
                ui.Phone.chats.Insert(0, new ChatThread
                {
                    id = caller.chatId, title = title, avatar = caller.portrait, notFriend = !caller.inContacts,
                    profileId = caller.profileId, profileNote = "Joined Talk today",
                });
            OpenCase();
            state = State.Active;
            Run(conv.nodes.Count > 0 ? conv.nodes[0] : null);
        }

        void OpenCase()
        {
            var c = new CaseFile { caller = caller, callerTitle = isChat ? title : $"{title} · {caller.number}" };
            c.claims.AddRange(conv.claims);
            ui.CurrentCase = c;
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
                yield return PlayLine(line, caller.portrait);
            if (node.hasDecision)
            {
                Present(node.decision);
                yield break;
            }
            GoTo(node.next);
        }

        IEnumerator PlayLine(ConvLine line, string portrait)
        {
            if (line.speaker == Speaker.Caller)
            {
                SetTyping(true, portrait);
                yield return new WaitForSeconds(Mathf.Clamp(line.text.Length / 32f, 0.7f, 2.4f));
                SetTyping(false, portrait);
            }
            else
            {
                yield return new WaitForSeconds(0.25f);
            }
            Show(line.speaker, line.text, line.facts, portrait);
            if (line.facts != null)
                foreach (var f in line.facts)
                    ui.CurrentCase?.AddFact(f);
            Deliver(line.deliver);
            yield return new WaitForSeconds(line.pauseAfter + (line.speaker == Speaker.Caller ? 0.3f : 0.1f));
        }

        void SetTyping(bool on, string portrait)
        {
            if (isChat)
                Talk.ShowTyping(caller.chatId, on);
            else
                ui.Transcript.SetTyping(on, portrait);
        }

        void Show(Speaker speaker, string text, List<Fact> facts, string portrait)
        {
            if (isChat)
            {
                if (speaker == Speaker.System)
                {
                    ui.Toast("app_talk", "Talk", text);
                    return;
                }
                Talk.Append(caller.chatId, new ChatMessage
                {
                    sender = speaker == Speaker.Player ? "" : title, avatar = portrait, when = GameClock.Now, text = text,
                    outgoing = speaker == Speaker.Player, facts = facts != null ? new List<Fact>(facts) : new List<Fact>(),
                });
                return;
            }
            if (speaker == Speaker.System)
            {
                ui.Transcript.AddSystem(text);
                return;
            }
            ui.Transcript.AddLine(speaker, text, facts, portrait);
            Sfx.Play(Sfx.Type, 0.6f);
            if (speaker == Speaker.Caller)
                ui.CallHud.SetLine(text);
        }

        void Deliver(List<Delivery> items)
        {
            if (items == null)
                return;
            foreach (var d in items)
            {
                switch (d.kind)
                {
                    case DeliveryKind.Sms:
                        Phone.App<MessagesApp>().Deliver(d.from, d.text, d.link);
                        break;
                    case DeliveryKind.BankNotice:
                        Phone.App<BankApp>().Notify(d.title, d.text, true);
                        break;
                    case DeliveryKind.BankDeposit:
                        Phone.App<BankApp>().Deposit(d.amount, d.from, d.text);
                        break;
                    case DeliveryKind.Mail:
                        ui.Phone.mails.Insert(0, new MailItem
                        {
                            from = d.from, fromAddress = d.link, subject = d.title, when = GameClock.Now, body = d.text, unread = true,
                        });
                        ui.Toast("app_mail", "Mail · " + d.from, d.title);
                        Sfx.Play(Sfx.Pop);
                        break;
                    case DeliveryKind.ChatMessage:
                        Talk.Append(d.from, new ChatMessage { sender = d.title, when = GameClock.Now, text = d.text });
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
            if (isChat)
            {
                Talk.ShowChoices(caller.chatId, d, Pick);
                timerRunning = Talk.IsOpen(caller.chatId);
            }
            else
            {
                ui.Transcript.ShowDecision(d, Pick);
                timerRunning = true;
            }
        }

        void OnThreadOpened(string chatId)
        {
            if (isChat && pending != null && caller != null && chatId == caller.chatId)
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
                EndWith(ending, caller.portrait);
                return;
            }
            Debug.LogError($"[CallDirector] {conv.name}: no node or ending called '{id}'");
            Cleanup();
        }

        void ClearDecision()
        {
            pending = null;
            ui.Transcript.ShowIdle();
            Talk.ClearChoices();
            ui.CallHud.SetDecision(false, 0f, 1f);
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
                if (isChat)
                {
                    Talk.UpdateTimer(patience, patienceMax);
                }
                else
                {
                    ui.Transcript.UpdateTimer(patience, patienceMax);
                    ui.CallHud.SetDecision(true, patience, patienceMax);
                }
                foreach (var p in pending.pressure)
                {
                    if (firedPressure.Contains(p) || f > p.atPatience)
                        continue;
                    firedPressure.Add(p);
                    Show(Speaker.Caller, p.text, null, caller.portrait);
                }
                if (patience <= 0f)
                {
                    ClearDecision();
                    if (!isChat)
                        ui.Transcript.AddSystem("You took too long.", "bad");
                    GoTo(conv.timeoutEndingId);
                }
                ui.Pressure.Tick(f, dt);
            }
            else
            {
                ui.Pressure.Tick(-1f, dt);
            }
        }

        // ---------------------------------------------------------------- phone actions

        public void HangUp()
        {
            if (isOutgoing)
            {
                EndOutgoing();
                return;
            }
            if (state != State.Active || isChat || conv == null)
                return;
            if (running != null)
                StopCoroutine(running);
            ClearDecision();
            ui.Transcript.SetTyping(false, null);
            ui.Transcript.AddSystem("You hung up.");
            // The line is dead: the caller's ending lines are not heard.
            CloseLine();
            var ending = conv.FindEnding(conv.hangUpEndingId);
            if (ending != null)
                EndWith(ending, caller.portrait, speak: false);
            else
                Cleanup();
        }

        void CloseLine()
        {
            if (lineClosed)
                return;
            lineClosed = true;
            inCall?.SetStatus("CALL ENDED");
            // Nothing to return to: the "Return to call" bar goes away.
            Phone.CallView = null;
            ui.Transcript.AddSystem($"Call ended · {Phone.CallTimerText}");
            Sfx.Play(Sfx.HangUp, 0.7f);
        }

        void OnTransfer(string bank, string account, long amount)
        {
            if (state != State.Active || conv == null)
                return;
            if (!isChat)
                ui.Transcript.AddSystem($"You sent {FactText.Won(amount)} to {bank} {account}.", "bad");
            var trigger = conv.actions.Find(a => a.kind == ActionKind.Transfer && FactText.SameNumber(a.target, account));
            if (trigger == null)
                return;
            if (running != null)
                StopCoroutine(running);
            ClearDecision();
            GoTo(trigger.endingId);
        }

        public void Dial(string number)
        {
            if (state == State.Ringing || state == State.Ending)
                return;
            if (isOutgoing)
            {
                ui.Toast("app_phone", "Phone", "You're already on a call.");
                return;
            }
            if (state == State.Active && !isChat)
            {
                if (FactText.SameNumber(number, caller.number))
                {
                    ui.Toast("app_phone", "Phone", "You're on the line with this number already.");
                    return;
                }
                string who = ui.Phone.FindContact(number)?.name ?? ui.Directory.FindNumber(number)?.owner ?? number;
                ui.Confirm("Hang up and call?", $"End the call with {title} ({caller.number}) and call {who}?", "Hang up and call",
                           "Stay on the line", () => CallAway(number));
                return;
            }
            if (state == State.Active && isChat)
            {
                var trig = conv.actions.Find(a => a.kind == ActionKind.Call && FactText.SameNumber(a.target, number));
                if (trig != null)
                {
                    if (running != null)
                        StopCoroutine(running);
                    ClearDecision();
                    running = StartCoroutine(VerifyCall(number, trig));
                    return;
                }
            }
            outgoing = StartCoroutine(Outgoing(number));
        }

        void CallAway(string number)
        {
            if (state != State.Active || conv == null)
                return;
            if (running != null)
                StopCoroutine(running);
            ClearDecision();
            ui.Transcript.AddSystem($"You hung up on {caller.number} and called {number}.");
            var trig = conv.actions.Find(a => a.kind == ActionKind.Call && FactText.SameNumber(a.target, number));
            if (trig != null)
                running = StartCoroutine(VerifyCall(number, trig));
            else
                GoTo(conv.hangUpEndingId);
        }

        /// <summary>The player called a number they looked up; that line answers with the ending's lines.</summary>
        IEnumerator VerifyCall(string number, ActionTrigger trig)
        {
            state = State.Ending;
            isChat = false;
            var who = new CallerInfo { displayName = trig.answeredBy, number = number, portrait = trig.portrait, inContacts = true };
            Phone.Raise(true);
            BeginCallScreen(who, trig.answeredBy, "CALLING…");
            ui.Transcript.Open(who, trig.answeredBy);
            ui.Transcript.AddSystem($"Calling {number}…");
            ui.CallHud.Set(trig.answeredBy, number, trig.portrait);
            Phone.App<CallsApp>().AddRecord(number, CallKind.Outgoing, "");
            Sfx.Play(Sfx.Tick);
            yield return new WaitForSeconds(0.8f);
            Sfx.Play(Sfx.Tick);
            yield return new WaitForSeconds(0.9f);
            inCall.SetStatus("ON CALL");
            EndWith(conv.FindEnding(trig.endingId), trig.portrait);
        }

        void EndWith(ConvEnding ending, string portrait, bool speak = true)
        {
            if (ending == null)
            {
                Cleanup();
                return;
            }
            state = State.Ending;
            pending = null;
            if (running != null)
                StopCoroutine(running);
            running = StartCoroutine(PlayEnding(ending, portrait, speak));
        }

        IEnumerator PlayEnding(ConvEnding ending, string portrait, bool speak)
        {
            if (speak)
                foreach (string text in ending.lines)
                    yield return PlayLine(new ConvLine { speaker = Speaker.Caller, text = text, pauseAfter = 0.3f }, portrait);
            if (!isChat)
                CloseLine();
            else
                Show(Speaker.System, "Conversation over", null, null);
            if (conv != null && !isChat)
                Phone.App<CallsApp>().AddRecord(conv.caller.number, CallKind.Incoming, Phone.CallTimerText);
            yield return new WaitForSeconds(1.4f);
            ShowResult(ending);
        }

        void ShowResult(ConvEnding ending)
        {
            var card = new EndingCard
            {
                caller = conv == null || string.IsNullOrEmpty(conv.caller.number) ? title : $"{title} · {conv.caller.number}",
                portrait = conv != null ? conv.caller.portrait : caller?.portrait,
                verdict = ending.verdict == Verdict.GoAlong ? "You went along" : ending.verdict == Verdict.Refuse ? "You refused" : "You checked first",
                consequence = ending.consequence,
                money = ending.moneyDelta != 0 ? FactText.Won(ending.moneyDelta) : null,
                moneyIn = ending.moneyDelta > 0,
            };
            if (conv != null && conv.revealAtEnd)
            {
                bool right = ending.verdict == Verdict.Verify || (conv.isScam ? ending.verdict == Verdict.Refuse : ending.verdict == Verdict.GoAlong);
                card.truth = conv.isScam ? "SCAM" : "REAL";
                card.truthGood = !conv.isScam;
                card.judgement = right ? "RIGHT CALL" : "WRONG CALL";
                card.judgementGood = right;
            }
            else
            {
                card.subtitle = "Tomorrow's Seoul Daily will tell you who it really was.";
            }
            ui.ShowEndingCard(card, Cleanup);
        }

        void Cleanup()
        {
            if (running != null)
                StopCoroutine(running);
            running = null;
            Sfx.StopLoop();
            ClearDecision();
            // An ordinary outgoing call placed during a chat keeps its own screen.
            if (inCall != null && !isOutgoing)
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
            isChat = false;
        }

        // ---------------------------------------------------------------- ordinary outgoing calls

        /// <summary>
        /// A call the player places outside a conversation's triggers. It never touches the running
        /// conversation, so a chat keeps waiting (and its timer keeps running) while the player calls.
        /// </summary>
        IEnumerator Outgoing(string number)
        {
            isOutgoing = true;
            var cb = ui.Directory.FindCallback(number);
            var contact = ui.Phone.FindContact(number);
            string name = contact?.name ?? cb?.answeredBy ?? ui.Directory.FindNumber(number)?.owner ?? number;
            string portrait = contact?.portrait ?? cb?.portrait ?? "pt_unknown";
            var who = new CallerInfo { number = number, portrait = portrait, displayName = name };
            Phone.Raise(true);
            BeginCallScreen(who, name, "CALLING…", replace: false);
            ui.Transcript.Open(who, name);
            ui.CallHud.Set(name, number, portrait);
            Phone.App<CallsApp>().AddRecord(number, CallKind.Outgoing, "");
            ui.Transcript.AddSystem($"Calling {number}…");
            yield return new WaitForSeconds(1.8f);
            if (cb == null)
            {
                inCall.SetStatus("NO ANSWER");
                ui.Transcript.AddSystem("No answer.");
                yield return new WaitForSeconds(1.5f);
                EndOutgoing();
                yield break;
            }
            inCall.SetStatus("ON CALL");
            foreach (string text in cb.lines)
            {
                ui.Transcript.SetTyping(true, portrait);
                yield return new WaitForSeconds(Mathf.Clamp(text.Length / 32f, 0.7f, 2.4f));
                ui.Transcript.SetTyping(false, portrait);
                ui.Transcript.AddLine(Speaker.Caller, text, null, portrait);
                ui.CallHud.SetLine(text);
                Sfx.Play(Sfx.Type, 0.6f);
                yield return new WaitForSeconds(0.6f);
            }
            ui.Transcript.AddSystem("Call ended.");
            yield return new WaitForSeconds(1.2f);
            EndOutgoing();
        }

        void EndOutgoing()
        {
            if (outgoing != null)
                StopCoroutine(outgoing);
            outgoing = null;
            Sfx.Play(Sfx.HangUp, 0.6f);
            ui.Transcript.SetTyping(false, null);
            if (inCall != null)
                Phone.RemoveScreen(inCall);
            Phone.CallView = null;
            inCall = null;
            isOutgoing = false;
        }
    }
}
