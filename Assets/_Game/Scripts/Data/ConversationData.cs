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
    }

    [Serializable]
    public class ConvNode
    {
        public string id;
        public List<ConvLine> lines = new List<ConvLine>();
        public bool hasDecision;
        public ConvDecision decision = new ConvDecision();
        public string next;           // node or ending id when there is no decision
    }

    [Serializable]
    public class ConvLine
    {
        public Speaker speaker;
        [TextArea(1, 5)] public string text;
        public List<Fact> facts = new List<Fact>();
        [Tooltip("Evidence that arrives as the line is spoken, e.g. an SMS or a deposit.")]
        public List<Delivery> deliver = new List<Delivery>();
        public float pauseAfter = 0.4f;
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
    }

    [Serializable]
    public class ConvEnding
    {
        public string id;
        public Verdict verdict;
        public long moneyDelta;
        [TextArea(1, 4)] public List<string> lines = new List<string>();   // closing lines (caller or official line)
        [TextArea(2, 5)] public string consequence;                        // for the next morning's paper
    }

    [Serializable]
    public class ActionTrigger
    {
        public ActionKind kind;
        public string target;         // account or phone number
        public string endingId;
        public string answeredBy;     // for Call: who picks up
        public string portrait;
    }
}
