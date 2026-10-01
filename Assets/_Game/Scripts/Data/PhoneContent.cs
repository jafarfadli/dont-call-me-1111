using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>
    /// Everything on Jiwoo's phone for one day: contacts, calls, texts, chats, bank, parcels, mail.
    /// The UI templates read it as-is; later the EvidenceComposer builds it per day.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Phone Content")]
    public class PhoneContent : ScriptableObject
    {
        public string ownerName = "Kim Jiwoo";
        public string ownerNumber = "010-6620-0921";
        public string dateLabel = "Tuesday, 6 October";
        public string startTime = "16:20";
        public List<Contact> contacts = new List<Contact>();
        public List<CallRecord> recents = new List<CallRecord>();
        public List<SmsThread> sms = new List<SmsThread>();
        public List<ChatThread> chats = new List<ChatThread>();
        public BankData bank = new BankData();
        public List<Parcel> parcels = new List<Parcel>();
        public List<MailItem> mails = new List<MailItem>();

        public Contact FindContact(string number)
        {
            foreach (var c in contacts)
                if (FactText.SameNumber(c.number, number))
                    return c;
            return null;
        }
    }

    [Serializable]
    public class Contact
    {
        public string name;
        public string number;
        public string memo;
        public string portrait;   // UISkin portrait id, e.g. "pt_mom"
    }

    public enum CallKind { Incoming, Outgoing, Missed }

    [Serializable]
    public class CallRecord
    {
        public string number;
        public CallKind kind;
        public string when;       // "Yesterday 19:02"
        public string duration;   // "3:12"
    }

    [Serializable]
    public class SmsThread
    {
        public string sender;     // name or number
        public List<SmsMessage> messages = new List<SmsMessage>();
    }

    [Serializable]
    public class SmsMessage
    {
        public string when;
        [TextArea(2, 6)] public string text;
        public bool outgoing;
        public string link;       // opens in Browser
    }

    [Serializable]
    public class ChatThread
    {
        public string id;
        public string title;
        public string avatar;           // portrait id
        public bool group;
        public bool notFriend;          // shows the "not in your friends list" banner
        public string profileId;        // shown on the profile
        public string profileNote;      // "Joined Talk today"
        public List<ChatMessage> messages = new List<ChatMessage>();
    }

    [Serializable]
    public class ChatMessage
    {
        public string sender;
        public string avatar;
        public string when;
        [TextArea(2, 6)] public string text;
        public bool outgoing;
        public string photoCaption;     // a photo message, described
        public List<Fact> facts = new List<Fact>();
    }

    [Serializable]
    public class BankData
    {
        public string bankName = "Nuri Bank";
        public List<BankAccount> accounts = new List<BankAccount>();
        public List<BankTransaction> transactions = new List<BankTransaction>();
        public List<BankNotice> notices = new List<BankNotice>();
        public List<string> banks = new List<string>();

        /// <summary>
        /// Adds to (or takes from) the main account. Money going out beyond its balance comes out of
        /// the next accounts (the tuition savings), as Jiwoo would move it to make the transfer.
        /// </summary>
        public void Change(long amount)
        {
            if (accounts.Count == 0)
                return;
            accounts[0].balance += amount;
            for (int i = 1; i < accounts.Count && accounts[0].balance < 0; i++)
            {
                long take = System.Math.Min(-accounts[0].balance, accounts[i].balance);
                accounts[i].balance -= take;
                accounts[0].balance += take;
            }
        }
    }

    [Serializable]
    public class BankAccount
    {
        public string name;
        public string number;
        public long balance;
    }

    [Serializable]
    public class BankTransaction
    {
        public string when;
        public string counterparty;
        public string memo;
        public long amount;       // + in, − out
    }

    [Serializable]
    public class BankNotice
    {
        public string when;
        public string title;
        [TextArea(2, 5)] public string body;
        public bool alert;
    }

    [Serializable]
    public class Parcel
    {
        public string item;
        public string seller;
        public string status;
        public string tracking;
        public string courier;
        public string driver;
        public string driverPhone;
        public string window;
        public bool overseas;
        [Tooltip("A line from the courier under the status, e.g. what is due and how to pay it.")]
        [TextArea(1, 4)] public string note;
        [Tooltip("The account to pay, when something is due (shown as a copy chip).")]
        public string account;
    }

    [Serializable]
    public class MailItem
    {
        public string from;
        public string fromAddress;
        public string subject;
        public string when;
        [TextArea(3, 10)] public string body;
        public List<string> attachments = new List<string>();
        public bool unread;
    }
}
