using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    public enum Channel { Call, Chat }
    public enum Speaker { Caller, Player, System }
    public enum DecisionKind { Ask, Test, Commit }
    public enum Verdict { GoAlong, Refuse, Verify }
    public enum ActionKind { Transfer, Call, HangUp }

    /// <summary>
    /// One call or chat: a small graph of nodes with two-option decisions, endings and the
    /// phone actions that also end it (a transfer, a call to a looked-up number, hanging up).
    /// This is the conversation part of the planned ScenarioData (docs/technical-plan.md, section 9).
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Conversation")]
    public class ConversationData : ScriptableObject
    {
        public string title;
        public Channel channel;
        public bool isScam;
        [Tooltip("Template/demo only: say right or wrong when the conversation ends. In the game the truth comes in the next morning's paper.")]
        public bool revealAtEnd = true;
        public CallerInfo caller = new CallerInfo();
        [Tooltip("What the caller claims, listed in the notebook's Case tab as the call goes on.")]
        public List<string> claims = new List<string>();
        public List<ConvNode> nodes = new List<ConvNode>();
        public List<ConvEnding> endings = new List<ConvEnding>();
        public List<ActionTrigger> actions = new List<ActionTrigger>();
        public string hangUpEndingId = "refuse";
        public string timeoutEndingId = "timeout";

        [Header("Investigation (day scenarios)")]
        [Tooltip("Shown on the CASE OPENED card when the caller starts holding the line.")]
        public CaseInfo caseInfo = new CaseInfo();
        [Tooltip("What the player can ask while the caller holds. Each can be asked once.")]
        public List<ConvQuestion> questions = new List<ConvQuestion>();
        [Tooltip("What the caller says (and sends) as the in-game clock passes, while holding.")]
        public List<PressureBeat> beats = new List<PressureBeat>();
        [Tooltip("The verdicts offered on the call while the caller holds (go along, refuse).")]
        public VerdictInfo verdict = new VerdictInfo();

        public ConvNode FindNode(string id) => nodes.Find(n => n.id == id);
        public ConvEnding FindEnding(string id) => endings.Find(e => e.id == id);
    }

    [Serializable]
    public class CallerInfo
    {
        public string displayName;    // shown when saved in contacts, or the chat profile name
        public string number;
        public bool inContacts;
        public string portrait = "pt_unknown";
        public string chatId;         // Talk thread for chats
        public string profileId;      // shown on the chat profile
        [Tooltip("TTS voice (macOS `say` voice name) for the caller's lines.")]
        public string voice;
    }

    /// <summary>What the CASE OPENED card says once the caller has made the ask.</summary>
    [Serializable]
    public class CaseInfo
    {
        public string caseTitle;
        [Tooltip("Who the caller says they are.")]
        public string claimedIdentity;
        [TextArea(1, 3)] public string ask;
        [Tooltip("In-game time the caller set, e.g. 17:00. The investigation ends there.")]
        public string deadline = "17:00";
        public string deadlineReason;
        [TextArea(2, 4)] public string objective;
    }

    public enum VerdictKind { GoAlong, Refuse }

    /// <summary>
    /// How the verdict panel on the call words the two choices. Each kind always has the same
    /// colour (go along gold, refuse red), so the colours never hint at the answer.
    /// </summary>
    [Serializable]
    public class VerdictInfo
    {
        public string goAlong = "Send the money";
        [Tooltip("Filled from the transfer action when empty, e.g. \"₩1,200,000 to Nuri Bank 110-900-551207\".")]
        public string goAlongDetail;
        [Tooltip("Optional: the ending when going along is not a transfer (e.g. agreeing to a visit).")]
        public string goAlongEndingId;
        public string refuse = "Hang up";
        public string refuseDetail = "Say no and end the call";
        [Header("What the player says as they decide")]
        [TextArea(1, 2)] public string goAlongLine = "Okay. I'm sending it now.";
        [TextArea(1, 2)] public string refuseLine = "I'm not sending anything. Goodbye.";
    }

    /// <summary>A question the player can put to a holding caller. The answer may end the call.</summary>
    [Serializable]
    public class ConvQuestion
    {
        public string label;
        [TextArea(1, 3)] public string playerLine;
        public List<ConvLine> answer = new List<ConvLine>();
        [Tooltip("Optional: the ending that follows the answer (e.g. after saying no).")]
        public string endingId;
    }

    /// <summary>A line the holding caller says (with anything it delivers) at an in-game time.</summary>
    [Serializable]
    public class PressureBeat
    {
        public string at = "16:40";
        public ConvLine line = new ConvLine();
    }

    [Serializable]
    public class ConvNode
    {
        public string id;
        public List<ConvLine> lines = new List<ConvLine>();
        public bool hasDecision;
        public ConvDecision decision = new ConvDecision();
        public string next;           // node or ending id when there is no decision
        [Tooltip("After this node's lines the caller holds the line: the case opens and the investigation starts.")]
        public bool holds;
    }

    [Serializable]
    public class ConvLine
    {
        public Speaker speaker;
        [TextArea(1, 5)] public string text;
        [Tooltip("What the voice says when it should differ from the text, e.g. an account number read digit by digit.")]
        [TextArea(1, 5)] public string spoken;
        public List<Fact> facts = new List<Fact>();
        [Tooltip("Evidence that arrives as the line is spoken, e.g. an SMS or a deposit.")]
        public List<Delivery> deliver = new List<Delivery>();
        public float pauseAfter = 0.4f;

        public string Spoken => string.IsNullOrEmpty(spoken) ? text : spoken;

        public static ConvLine Caller(string text, string spoken = null, float pauseAfter = 0.4f) =>
            new ConvLine { speaker = Speaker.Caller, text = text, spoken = spoken, pauseAfter = pauseAfter };
    }

    public enum DeliveryKind { Sms, BankNotice, BankDeposit, Mail, ChatMessage }

    [Serializable]
    public class Delivery
    {
        public DeliveryKind kind;
        public string from;
        public string title;
        [TextArea(1, 4)] public string text;
        public string link;
        public long amount;
    }

    [Serializable]
    public class ConvDecision
    {
        public DecisionKind kind;
        public string prompt;
        public float patienceSeconds = 60f;
        public ConvOption a = new ConvOption();
        public ConvOption b = new ConvOption();
        public List<PressureLine> pressure = new List<PressureLine>();
    }

    [Serializable]
    public class ConvOption
    {
        public string label;
        [TextArea(1, 3)] public string playerLine;
        public string next;           // node or ending id
    }

    [Serializable]
    public class PressureLine
    {
        [Range(0f, 1f)] public float atPatience = 0.5f;
        public string text;
        public string spoken;
    }

    [Serializable]
    public class ConvEnding
    {
        public string id;
        public Verdict verdict;
        [Tooltip("Reached because the caller's deadline passed with no decision.")]
        public bool timedOut;
        public long moneyDelta;
        public List<ConvLine> lines = new List<ConvLine>();   // closing lines (caller or official line)
        [TextArea(2, 5)] public string consequence;          // for the case card and the next morning's paper
    }

    [Serializable]
    public class ActionTrigger
    {
        public ActionKind kind;
        public string target;         // account or phone number
        public string endingId;
        public string answeredBy;     // for Call: who picks up
        public string portrait;
        public string voice;          // for Call: the voice of whoever picks up
        public string bank;           // for Transfer: the bank the caller named
        public long amount;           // for Transfer: how much the caller asked for
    }
}
