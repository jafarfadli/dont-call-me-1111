using System.Collections;
using System.Collections.Generic;
using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Player;
using DontCallMe.UI;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>
    /// Runs one day in the Room scene: picks the day's truth and sets out its evidence (with what
    /// earlier days left behind, see <see cref="DaySetup"/>), then the DAY card, the player seated at
    /// the desk while yesterday's aftermath buzzes in, the forced call, the CASE OPENED card once the
    /// caller has made the ask and starts holding the line, the investigation against the caller's
    /// deadline (pressure beats, music that tightens, red screen edges and a ticking clock), and
    /// finally the record of the day and the fade to the next morning (End scene). A tutorial day
    /// (Day 0) runs with the <see cref="TutorialGuide"/>: its call waits until the newspaper has
    /// been read.
    /// </summary>
    public class DayDirector : MonoBehaviour
    {
        public enum Phase { Waiting, Intro, Seated, OnCall, CaseCard, Investigating, Ending, Done }

        [Tooltip("The day played when the Room scene is opened directly (the run picks it otherwise).")]
        [SerializeField] DayData day;
        [Tooltip("Development: \"scam\" or \"legit\" plays that truth instead of a random one.")]
        [SerializeField] string forceVariant = "";
        [SerializeField] UIManager ui;
        [SerializeField] CallDirector director;
        [SerializeField] FirstPersonController player;
        [SerializeField] MusicPlayer music;
        [SerializeField] AudioClip calmLoop;
        [SerializeField] AudioClip tenseLoop;
        [Tooltip("Feet position in the desk chair; its Y rotation faces the desk, X tilts the view down.")]
        [SerializeField] Transform seat;
        [Tooltip("Where the player stands when the investigation starts.")]
        [SerializeField] Transform standSpot;
        [SerializeField] bool autoStart = true;

        public Phase Current { get; private set; }
        public DayData Day => Plan != null ? Plan.day : day;
        public DayPlan Plan { get; private set; }
        /// <summary>Which of today's clues the player has seen (set when the day starts).</summary>
        public ClueTracker Clues => clues;
        ConversationData Conv => Plan?.Conversation;

        ClueTracker clues;
        float deadline;
        float holdStart;
        float rangAt;
        float tickTimer;
        readonly HashSet<PressureBeat> fired = new HashSet<PressureBeat>();
        bool subscribed;
        float decidedMinutes = -1f;
        string decidedAt;
        TutorialGuide tutorial;
        DayCardView dayCard;

        /// <summary>The longest the tutorial day's call waits for the player to read the newspaper.</summary>
        const float TutorialWait = 75f;

        void Awake()
        {
            if (ui == null)
                ui = FindAnyObjectByType<UIManager>();
            if (director == null)
                director = FindAnyObjectByType<CallDirector>();
            if (player == null)
                player = FindAnyObjectByType<FirstPersonController>();
            if (music == null)
                music = FindAnyObjectByType<MusicPlayer>();
        }

        void Start()
        {
            if (autoStart)
                Begin();
        }

        void OnDestroy()
        {
            Unsubscribe();
            clues?.Dispose();
        }

        /// <summary>
        /// Picks the day and its truth and composes its evidence, once. The UI calls this while it
        /// wakes up, so the phone and the room show this day's content from the first frame.
        /// </summary>
        public DayPlan Prepare()
        {
            if (Plan != null)
                return Plan;
            var catalog = DayCatalog.Load();
            int number = GameRun.PendingDay >= 0 ? GameRun.PendingDay : day != null ? day.day : GameRun.FirstDay;
            var data = (catalog != null ? catalog.Get(number) : null) ?? day;
            if (data == null)
                return null;
            var earlier = GameRun.Before(data.day);
            var variant = DaySetup.Pick(data, earlier, catalog, forceVariant);
            Plan = DaySetup.Compose(data, variant, earlier, catalog);
            Debug.Log($"[DayDirector] Day {data.day}: {variant?.id ?? "no case"}" + (earlier.Count > 0 ? $", after {earlier.Count} earlier day(s)" : ""));
            return Plan;
        }

        public void Begin()
        {
            if (Prepare() == null || Conv == null || Current != Phase.Waiting)
                return;
            StartCoroutine(Run());
        }

        /// <summary>Stops the day where it is and hands the room back (development tools).</summary>
        public void Cancel()
        {
            StopAllCoroutines();
            Unsubscribe();
            if (tutorial != null)
                tutorial.Stop();
            dayCard?.Close();
            dayCard = null;
            Current = Phase.Done;
            GameClock.Running = true;
            if (director != null)
            {
                director.showEndingCard = true;
                director.ExternalPressure = -1f;
            }
            if (ui != null && ui.Hud != null)
                ui.Hud.Deadline.Show(false);
            if (player != null && player.IsSeated && standSpot != null)
                player.StandUp(standSpot.position, 0.01f);
        }

        IEnumerator Run()
        {
            var today = Plan.day;
            Current = Phase.Intro;
            Subscribe();
            clues = new ClueTracker(Plan.variant.clues);
            ShowPaperOnDesk(Plan.room.newspaper.print);
            GameClock.DayLabel = today.shortLabel;
            ui.Hud.SetDayLabel(today.shortLabel);
            GameClock.Start(today.startTime);
            GameClock.Running = false;
            if (seat != null)
                player.Sit(seat.position, seat.eulerAngles.y, Mathf.DeltaAngle(0f, seat.eulerAngles.x));
            MarkUnread();

            bool shown = false;
            dayCard = ui.ShowDayCard(today, Plan.dayCardLines, () => shown = true);
            float wait = 0f;
            float readTime = 7f + 2.5f * Plan.dayCardLines.Count;
            while (!shown)
            {
                wait += Time.deltaTime;
                if (wait > readTime)
                    dayCard.Close();
                yield return null;
            }
            dayCard = null;

            Current = Phase.Seated;
            if (today.tutorial)
            {
                // The guide teaches looking and reading the paper first; the clock waits with the call.
                tutorial = gameObject.AddComponent<TutorialGuide>();
                tutorial.Begin(ui, director, this, player);
                float t = 0f;
                while (!tutorial.ReadyForCall && t < TutorialWait)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                yield return new WaitForSeconds(1.8f);
                GameClock.Running = true;
            }
            else
            {
                GameClock.Running = true;
                // Yesterday's aftermath buzzes in while Jiwoo sits down.
                float waited = 0f;
                foreach (var echo in Plan.notifications)
                {
                    yield return new WaitForSeconds(1.3f);
                    waited += 1.3f;
                    Notify(echo);
                }
                yield return new WaitForSeconds(Mathf.Max(2.5f, today.ringDelay - waited * 0.5f));
            }

            Current = Phase.OnCall;
            rangAt = GameClock.Minutes;
            director.StartCall(Conv);
        }

        /// <summary>The folded paper on the desk shows today's issue (its front page is yesterday's case).</summary>
        static void ShowPaperOnDesk(Texture2D print)
        {
            var paper = print != null ? GameObject.Find("INT_Newspaper") : null;
            if (paper == null)
                return;
            foreach (var r in paper.GetComponentsInChildren<Renderer>())
            {
                if (r.name != "News_Front")
                    continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetTexture("_BaseMap", print);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>The echoes' chat messages start out unread.</summary>
        void MarkUnread()
        {
            foreach (string chat in Plan.unreadChats)
                ui.PhoneView.App<ChatsApp>().MarkUnread(chat);
        }

        void Notify(DayEcho echo)
        {
            switch (echo.kind)
            {
                case EchoKind.Chat:
                    ui.Toast("app_talk", Loc.T("Chats") + " · " + (ui.PhoneView.App<ChatsApp>().Thread(echo.from)?.title ?? echo.sender), echo.text);
                    break;
                case EchoKind.BankTransaction:
                    ui.Toast("app_bank", Loc.T("Nuri Bank"), string.IsNullOrEmpty(echo.title) ? echo.text : echo.title);
                    break;
                default:
                    return;
            }
            Sfx.Play(Sfx.Pop);
        }

        void OnHold()
        {
            if (Current == Phase.OnCall)
                StartCoroutine(OpenCase());
        }

        IEnumerator OpenCase()
        {
            Current = Phase.CaseCard;
            GameClock.Running = false;
            yield return new WaitForSeconds(0.8f);
            var conv = Conv;
            bool go = false;
            string who = ui.Phone.FindContact(conv.caller.number)?.name ?? Loc.T("Unknown caller");
            ui.ShowCaseCard(Plan.day.day, conv.caseInfo, conv.caller, who, () => go = true);
            while (!go)
                yield return null;

            GameClock.Running = true;
            deadline = GameClock.Parse(conv.caseInfo.deadline);
            holdStart = GameClock.Minutes;
            Current = Phase.Investigating;
            ui.PhoneView.Raise(false);
            if (player.IsSeated && standSpot != null)
                player.StandUp(standSpot.position);
            ui.Hud.Deadline.Show(true);
            if (music != null && calmLoop != null)
                music.Play(calmLoop, tenseLoop, 3f);
            if (tutorial == null)
                ui.Toast("ic_case", Loc.T("Investigate"), Loc.F("The caller is holding the line. Check the room and your phone before {0}, then give your verdict on the call (Tab).", conv.caseInfo.deadline));
        }

        void Update()
        {
            if (Current != Phase.Investigating || !director.IsBusy)
                return;
            if (!director.OnHold)
            {
                // The player acted: the caller's deadline no longer matters.
                StopInvestigation();
                return;
            }
            float now = GameClock.Minutes;
            float total = Mathf.Max(1f, deadline - holdStart);
            float left = deadline - now;
            float fraction = Mathf.Clamp01(left / total);
            ui.Hud.Deadline.Set(Conv.caseInfo.deadline, left, fraction);
            if (music != null)
                music.SetTension(Mathf.SmoothStep(0f, 1f, 1f - fraction));
            // Screen edges close in over the last 60%, with a heartbeat at the end.
            director.ExternalPressure = fraction < 0.6f ? fraction : -1f;

            foreach (var beat in Conv.beats)
            {
                if (fired.Contains(beat) || now < GameClock.Parse(beat.at))
                    continue;
                fired.Add(beat);
                director.Say(beat.line);
            }

            if (fraction < 0.12f && left > 0f)
            {
                tickTimer -= Time.deltaTime;
                if (tickTimer <= 0f)
                {
                    tickTimer = 1f;
                    Sfx.Play(Sfx.Tick, 0.8f);
                }
            }

            if (left <= 0f)
            {
                StopInvestigation();
                director.Timeout();
            }
        }

        /// <summary>The decision is made (or the time ran out): drop the deadline, the edges and the tension.</summary>
        void StopInvestigation()
        {
            if (decidedMinutes < 0f)
            {
                decidedMinutes = GameClock.Minutes;
                decidedAt = GameClock.Now;
            }
            Current = Phase.Ending;
            ui.Hud.Deadline.Show(false);
            director.ExternalPressure = -1f;
            music?.SetTension(0f);
        }

        void OnEnded(ConvEnding ending)
        {
            if (Current == Phase.Done)
                return;
            StopInvestigation();
            var bank = ui.Phone.bank;
            long savings = 0;
            foreach (var a in bank.accounts)
                savings += a.balance;
            var result = new DayResult
            {
                day = Plan.day,
                variant = Plan.variant,
                outcome = SceneFlow.OutcomeOf(ending),
                ending = ending,
                moneyDelta = ending.moneyDelta,
                savingsAfter = savings,
                decidedAt = decidedAt ?? GameClock.Now,
                callSeconds = director.CallSeconds,
                minutesTaken = Mathf.RoundToInt((decidedMinutes >= 0f ? decidedMinutes : GameClock.Minutes) - rangAt),
            };
            result.cluesFound.AddRange(clues.FoundIds);
            SceneFlow.LastResult = result;
            GameRun.Record(Record(result));
            StartCoroutine(Outro());
        }

        DayRecord Record(DayResult result)
        {
            var conv = Conv;
            var r = new DayRecord
            {
                day = Plan.day.day,
                variant = Plan.variant.id,
                scam = Plan.variant.IsScam,
                outcome = result.outcome,
                moneyDelta = result.moneyDelta,
                savingsAfter = result.savingsAfter,
                cluesTotal = Plan.variant.clues.Count,
                decidedAt = result.decidedAt,
                minutesTaken = result.minutesTaken,
                callSeconds = Mathf.RoundToInt(result.callSeconds),
                callerNumber = conv.caller.number,
                callTime = GameClock.Format(rangAt),
            };
            r.clues.AddRange(result.cluesFound);
            var transfer = conv.actions.Find(a => a.kind == ActionKind.Transfer);
            if (result.moneyDelta != 0 && transfer != null)
            {
                r.transferTo = ui.Directory.FindAccount(transfer.target)?.holder ?? transfer.target;
                r.transferBank = transfer.bank;
                r.transferAccount = transfer.target;
            }
            return r;
        }

        IEnumerator Outro()
        {
            yield return new WaitForSeconds(2.4f);
            music?.Stop(1.8f);
            bool faded = false;
            ui.Fade.FadeOut(1.8f, () => faded = true);
            while (!faded)
                yield return null;
            director.Finish();
            Unsubscribe();
            Current = Phase.Done;
            SceneFlow.Load(SceneFlow.End);
        }

        void Subscribe()
        {
            if (subscribed)
                return;
            subscribed = true;
            director.showEndingCard = false;
            director.HoldStarted += OnHold;
            director.ConversationEnded += OnEnded;
        }

        void Unsubscribe()
        {
            if (!subscribed || director == null)
                return;
            subscribed = false;
            director.HoldStarted -= OnHold;
            director.ConversationEnded -= OnEnded;
        }
    }
}
