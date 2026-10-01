using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    public enum Outcome { GoAlong, Refuse, Verify, Timeout }

    /// <summary>Something the player does that can reveal a clue (see <see cref="ClueDef"/>).</summary>
    public enum ClueEvent
    {
        PanelOpened,      // target: PanelId name, e.g. "Newspaper"
        CardFlipped,      // target: wallet card title
        BoardItemOpened,  // target: board item title
        DocumentViewed,   // target: drawer document title
        RecipientShown,   // target: account number on the transfer check screen
        NumberChecked,    // target: number or account looked up on CheckFirst
        PageOpened,       // target: url
        BankAlerts,       // target: ""
        MessageRead,      // target: sender
        MailRead,         // target: subject
        ParcelOpened,     // target: tracking number
        ChatRead,         // target: Talk thread id
    }

    /// <summary>
    /// One day: the date, where the player starts, and the truths the day can take. Each
    /// <see cref="DayVariant"/> is a whole case (the call, the evidence, the clues and the next
    /// morning's papers); one is picked when the day starts, so the same caller can be a scammer
    /// or exactly who they say. <see cref="echoes"/> are what earlier days left behind.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Day")]
    public class DayData : ScriptableObject
    {
        public int day = 1;
        [Tooltip("Day card, e.g. \"Tuesday, 6 October\".")]
        public string dateLabel;
        [Tooltip("HUD tag, e.g. \"Day 1 · Tue 6 Oct\".")]
        public string shortLabel;
        public string place = "Mangwon-dong, Seoul";
        public string startTime = "16:20";
        [TextArea(2, 4)] public string intro;
        [Tooltip("Seconds in the chair before the phone rings.")]
        public float ringDelay = 7f;

        [Tooltip("The truths this day can take (a scam and its legit twin share the caller and the ask). One is picked at random.")]
        public List<DayVariant> variants = new List<DayVariant>();

        [Tooltip("What earlier days left behind: texts, chat messages, a note on the board, a line on the day card.")]
        public List<DayEcho> echoes = new List<DayEcho>();

        [Header("Next morning")]
        public string nextDateLabel;

        [HideInInspector, Tooltip("The content builder version that wrote this day.")]
        public int builtWith;

        /// <summary>"Tue" from "Tuesday, 6 October", for labels such as "Tue 16:21".</summary>
        public string Weekday => string.IsNullOrEmpty(dateLabel) || dateLabel.Length < 3 ? "" : dateLabel.Substring(0, 3);

        public DayVariant Variant(string id) => variants.Find(v => v.id == id);
    }

    /// <summary>One truth of a day: the call, the evidence that goes with it, the clues and the papers.</summary>
    [Serializable]
    public class DayVariant
    {
        [Tooltip("\"scam\" or \"legit\".")]
        public string id = "scam";
        public ConversationData conversation;
        [Tooltip("Jiwoo's phone, the room and the world the apps look things up in, for this truth.")]
        public PhoneContent phone;
        public RoomContent room;
        public WorldDirectory directory;
        public List<ClueDef> clues = new List<ClueDef>();
        public List<EndPaper> papers = new List<EndPaper>();
        [Tooltip("Short name for the rule in the notebook.")]
        public string ruleTitle;
        [TextArea(2, 3)] public string rule;
        public string ruleSource;

        public bool IsScam => conversation == null || conversation.isScam;

        public EndPaper Paper(Outcome outcome) => papers.Find(p => p.outcome == outcome) ?? (papers.Count > 0 ? papers[0] : null);
    }

    /// <summary>A piece of evidence the player needs to see, and the actions that count as seeing it.</summary>
    [Serializable]
    public class ClueDef
    {
        public string id;
        [Tooltip("What the clue shows, e.g. \"The 'protected account' belongs to JEONG MIRAN\".")]
        public string text;
        [Tooltip("Where to find it, e.g. \"Bank app · transfer check\".")]
        public string where;
        public List<ClueWhen> when = new List<ClueWhen>();
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

    public enum EchoKind { Sms, Chat, Mail, BankNotice, BankTransaction, MissedCall, BoardNote, Contact, DayCard }

    /// <summary>
    /// Something an earlier day left behind, shown only when that day went a certain way: a text
    /// from the landlord, a note on the board, a line on the day card. It never holds a clue for
    /// today's case, so how yesterday went never makes today easier or harder.
    /// </summary>
    [Serializable]
    public class DayEcho
    {
        [Tooltip("The earlier day this reacts to.")]
        public int afterDay = 1;
        [Tooltip("Only when that day's caller was a scammer (or legit).")]
        public Truth truth;
        [Tooltip("Only for these verdicts on that day.")]
        public OutcomeMask outcomes = OutcomeMask.Any;
        public EchoKind kind;
        [Tooltip("Sms: sender. Chat: thread id. Mail: sender's name. MissedCall, Contact: the number. BankTransaction: counterparty.")]
        public string from;
        [Tooltip("Chat: who wrote it (empty for Jiwoo). Contact: the saved name. Chat thread title when the thread is new.")]
        public string sender;
        [Tooltip("Portrait id for chats and contacts.")]
        public string avatar;
        [Tooltip("As the phone shows it, e.g. \"Today 09:12\".")]
        public string when;
        [Tooltip("Mail subject, notice title, transaction memo, board note title.")]
        public string title;
        [TextArea(2, 5)] public string text;
        public string link;
        [Tooltip("BankTransaction: + in, − out (also changes the balance).")]
        public long amount;
        [Tooltip("Chat: Jiwoo wrote it.")]
        public bool outgoing;
        [Tooltip("BoardNote: the note's picture, where it is pinned (0..1) and its tilt.")]
        public Texture2D image;
        public Vector2 position;
        public float rotation;
        [Tooltip("Also pops up as a notification while Jiwoo sits at the desk, before the call.")]
        public bool notify;
    }
}
