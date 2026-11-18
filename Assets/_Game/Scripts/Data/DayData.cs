using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    public enum Outcome { GoAlong, Refuse, Verify, Timeout }

    /// <summary>Something the player does that can reveal a clue (see <see cref="ClueDef"/>).</summary>
    public enum ClueEvent
    {
        PanelOpened,      // target: PanelId name, e.g. "Newspaper" or "Calendar"
        DocumentViewed,   // target: drawer document title
        NumberChecked,    // target: phone number or account looked up on the computer
        RecipientShown,   // target: account number, when the verdict's send step shows who gets the money
        ChatRead,         // target: chat thread id
        BankOpened,       // target: ""
        ContactsOpened,   // target: ""
        Marked,           // target: MarkDef.id, when the player circles it
    }

    /// <summary>How much the case notebook helps on a day (the difficulty the DAY card announces).</summary>
    public enum NotebookHelp
    {
        Guided,   // the tutorial: claims written in, where to check them, circleable things glow
        Hints,    // claims written in, with where each can be checked
        Plain,    // claims written in, nothing else
        Write,    // empty lines: the player pins the caller's lines from the transcript themselves
    }

    /// <summary>
    /// The places where things can be circled. Video: the caller's face on a video call. Call: a
    /// caller's answer in the transcript (what they said when asked).
    /// </summary>
    public enum MarkPlace { Newspaper, Drawer, Calendar, Computer, Chats, Bank, Contacts, Video, Call }

    /// <summary>
    /// One day: the date, where the player starts, and the cases the day can bring. Each
    /// <see cref="DayVariant"/> is one truth of one case (the call, the evidence, the clues and the
    /// next morning's papers); a scam and its legit twin share a <see cref="DayVariant.scenario"/>.
    /// One is picked when the day starts, so the same caller can be a scammer or exactly who they
    /// say, and another week can bring another case. <see cref="echoes"/> are what earlier days
    /// left behind.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Day")]
    public class DayData : ScriptableObject
    {
        public int day = 1;
        [Tooltip("A guided day: the tutorial guide walks the player through its clues, in order.")]
        public bool tutorial;
        [Tooltip("Day card, e.g. \"Tuesday, 6 October\".")]
        public string dateLabel;
        [Tooltip("The weekday as the phone writes it for earlier days, e.g. \"Tue\" or \"화요일\".")]
        public string weekday;
        [Tooltip("HUD tag, e.g. \"Day 1 · Tue 6 Oct\".")]
        public string shortLabel;
        public string place = "Mangwon-dong, Seoul";
        public string startTime = "16:20";
        [TextArea(2, 4)] public string intro;
        [Tooltip("Seconds in the chair before the phone rings.")]
        public float ringDelay = 7f;
        [Tooltip("How much the case notebook helps today.")]
        public NotebookHelp help = NotebookHelp.Plain;
        [Tooltip("The difficulty the DAY card shows as stars, out of TopLevel; 0 shows none (the tutorial).")]
        public int level;
        [Tooltip("The DAY card's NEW TODAY line: what is harder or new today.")]
        [TextArea(1, 3)] public string newToday;

        /// <summary>The most stars a DAY card shows.</summary>
        public const int TopLevel = 4;

        [Tooltip("The cases this day can bring, as their truths (a scam and its legit twin share the caller, the ask and the scenario). One is picked when the day starts.")]
        public List<DayVariant> variants = new List<DayVariant>();

        [Tooltip("What earlier days left behind: texts, chat messages, a note on the board, a line on the day card.")]
        public List<DayEcho> echoes = new List<DayEcho>();

        [Header("Next morning")]
        public string nextDateLabel;

        [HideInInspector, Tooltip("The content builder version that wrote this day.")]
        public int builtWith;

        /// <summary>The weekday for labels such as "Tue 16:21" (from the date when not set).</summary>
        public string Weekday => !string.IsNullOrEmpty(weekday) ? weekday : string.IsNullOrEmpty(dateLabel) || dateLabel.Length < 3 ? "" : dateLabel.Substring(0, 3);

        public DayVariant Variant(string id) => variants.Find(v => v.id == id);
    }

    /// <summary>One truth of one of a day's cases: the call, the evidence that goes with it, the clues and the papers.</summary>
    [Serializable]
    public class DayVariant
    {
        [Tooltip("Unique within the day: \"scam\" or \"legit\" for the day's first case, \"sale_scam\", \"sale_legit\"... for the others.")]
        public string id = "scam";
        [Tooltip("The case this truth belongs to, e.g. \"rent\". A scam and its legit twin share it.")]
        public string scenario;
        [Tooltip("The day card's line of intro when this case is played; empty uses the day's.")]
        [TextArea(2, 4)] public string intro;
        public ConversationData conversation;
        [Tooltip("Jiwoo's phone, the room and the world the apps look things up in, for this truth.")]
        public PhoneContent phone;
        public RoomContent room;
        public WorldDirectory directory;
        public List<ClueDef> clues = new List<ClueDef>();
        [Tooltip("What can be circled for the notebook, each filed under a claim. Clues count when one of their marks is circled (ClueEvent.Marked).")]
        public List<MarkDef> marks = new List<MarkDef>();
        public List<EndPaper> papers = new List<EndPaper>();
        [Tooltip("Short name for the rule.")]
        public string ruleTitle;
        [TextArea(2, 3)] public string rule;
        public string ruleSource;

        public bool IsScam => conversation == null || conversation.isScam;

        /// <summary>The case's name, for picking a case and for echoes (the id when no scenario is set).</summary>
        public string Scenario => string.IsNullOrEmpty(scenario) ? id : scenario;

        public EndPaper Paper(Outcome outcome) => papers.Find(p => p.outcome == outcome) ?? (papers.Count > 0 ? papers[0] : null);
    }

    /// <summary>A piece of evidence the player needs to see, and the actions that count as seeing it.</summary>
    [Serializable]
    public class ClueDef
    {
        public string id;
        [Tooltip("What the clue shows, e.g. \"The 'protected account' belongs to JEONG MIRAN\".")]
        public string text;
        [Tooltip("Where to find it, e.g. \"Computer · check the account\".")]
        public string where;
        [Tooltip("Tutorial days: what the guide tells the player to do to find it, and why that place is worth checking.")]
        [TextArea(2, 4)] public string guide;
        public List<ClueWhen> when = new List<ClueWhen>();
    }

    /// <summary>
    /// One thing the player can circle (a document line, a chat message, a lookup result...) and
    /// the claim it is filed under. Decoys are marks too: they are about a claim but decide nothing.
    /// </summary>
    [Serializable]
    public class MarkDef
    {
        public string id;
        [Tooltip("The claim (ClaimDef.id) it is filed under.")]
        public string claim;
        [Tooltip("Other claims it settles too: it is filed under each of them as well.")]
        public List<string> also = new List<string>();
        public MarkPlace place;
        [Tooltip("Which item, by place. Newspaper: \"warning\", \"headline\", a paragraph's text, or a local item's or an ad's title. Drawer: \"<document title>|<field label>\", or \"<document title>|body\". Calendar: the note's text. Computer: the number or account looked up. Chats: \"<thread id>|<message text>\". Bank: \"<counterparty>|<amount>\", \"balance|<account number>\" for an account's card, or \"today|none\" for the history's \"nothing received yet today\" row. Contacts: the number. Video: \"face\", or \"wave\" (once the caller has waved). Call: the caller's line, as said.")]
        public string key;

        /// <summary>Is it filed under this claim (its own, or one it also settles)?</summary>
        public bool Under(string claimId) =>
            !string.IsNullOrEmpty(claimId) && (claim == claimId || also != null && also.Contains(claimId));

        /// <summary>Every claim it is filed under, its own first.</summary>
        public IEnumerable<string> Claims
        {
            get
            {
                yield return claim;
                if (also != null)
                    foreach (string c in also)
                        yield return c;
            }
        }
    }

    [Serializable]
    public class ClueWhen
    {
        public ClueEvent kind;
        public string target;

        public ClueWhen() { }

        public ClueWhen(ClueEvent kind, string target = "")
        {
            this.kind = kind;
            this.target = target;
        }
    }

    /// <summary>The next morning's front page for one outcome.</summary>
    [Serializable]
    public class EndPaper
    {
        public Outcome outcome;
        public string headline;
        [TextArea(1, 3)] public string subhead;
        [TextArea(3, 8)] public string body;
        [Tooltip("One line about what the player did, shown on the case summary.")]
        [TextArea(1, 3)] public string verdictNote;
        [Tooltip("This front page printed on the desk prop, when the paper lies there the next day.")]
        public Texture2D print;
    }

    public enum Truth { Any, Scam, Legit }

    [Flags]
    public enum OutcomeMask
    {
        None = 0,
        GoAlong = 1,
        Refuse = 2,
        Verify = 4,
        Timeout = 8,
        Any = GoAlong | Refuse | Verify | Timeout,
    }

    /// <summary>Note: a calendar note (added, or put in place of one with the same text in <see cref="DayEcho.title"/>).</summary>
    public enum EchoKind { Chat, BankTransaction, Contact, DayCard, Note }

    /// <summary>
    /// Something an earlier day left behind, shown only when that day went a certain way: a chat
    /// message from the landlord, the rent paid again, a line on the day card. It never holds a
    /// clue for today's case, so how yesterday went never makes today easier or harder.
    /// </summary>
    [Serializable]
    public class DayEcho
    {
        [Tooltip("The earlier day this reacts to.")]
        public int afterDay = 1;
        [Tooltip("Only when that day played this case (a DayVariant.scenario); empty for any case.")]
        public string scenario;
        [Tooltip("Only when that day's caller was a scammer (or legit).")]
        public Truth truth;
        [Tooltip("Only for these verdicts on that day.")]
        public OutcomeMask outcomes = OutcomeMask.Any;
        public EchoKind kind;
        [Tooltip("Chat: thread id. Contact: the number. BankTransaction: counterparty.")]
        public string from;
        [Tooltip("Chat: who wrote it (empty for Jiwoo). Contact: the saved name.")]
        public string sender;
        [Tooltip("Portrait id for chats and contacts.")]
        public string avatar;
        [Tooltip("As the phone shows it, e.g. \"Today 09:12\".")]
        public string when;
        [Tooltip("Transaction memo; the chat thread's title when the thread is new.")]
        public string title;
        [TextArea(2, 5)] public string text;
        [Tooltip("BankTransaction: + in, - out (also changes the balance).")]
        public long amount;
        [Tooltip("Chat: Jiwoo wrote it.")]
        public bool outgoing;
        [Tooltip("Chat: a photo message, described.")]
        public string photoCaption;
        [Tooltip("Note: the day of the month.")]
        public int date;
        [Tooltip("Note: the calendar's picture from now on (a note crossed out on it), or none to keep it.")]
        public Texture2D image;
        [Tooltip("Also pops up as a notification while Jiwoo sits at the desk, before the call.")]
        public bool notify;
    }
}
