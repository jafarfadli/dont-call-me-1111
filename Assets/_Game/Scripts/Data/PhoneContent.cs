using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>
    /// Everything on Jiwoo's phone for one day: contacts, chats and the bank. The three apps read
    /// it as-is; the content builder writes it per day and variant.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Phone Content")]
    public class PhoneContent : ScriptableObject
    {
        public string ownerName = "Kim Jiwoo";
        public string ownerNumber = "010-6620-0921";
        public string dateLabel = "Tuesday, 6 October";
        public string startTime = "16:20";
        public List<Contact> contacts = new List<Contact>();
        public List<ChatThread> chats = new List<ChatThread>();
        public BankData bank = new BankData();

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

    [Serializable]
    public class ChatThread
    {
        public string id;
        public string title;
        public string avatar;           // portrait id
        public bool group;
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
        public long amount;       // + in, - out
    }
}
