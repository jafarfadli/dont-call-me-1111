using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>
    /// What the computer's CheckFirst site knows: who owns a phone number or a bank account, and
    /// the reports people filed about it. The verdict's send step reads the account holder from
    /// here too, as a bank shows the recipient before a transfer.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/World Directory")]
    public class WorldDirectory : ScriptableObject
    {
        public List<DirAccount> accounts = new List<DirAccount>();
        public List<DirNumber> numbers = new List<DirNumber>();

        public DirAccount FindAccount(string number)
        {
            foreach (var a in accounts)
                if (FactText.SameNumber(a.number, number))
                    return a;
            return null;
        }

        public DirNumber FindNumber(string number)
        {
            foreach (var n in numbers)
                if (FactText.SameNumber(n.number, number))
                    return n;
            return null;
        }
    }

    [Serializable]
    public class DirAccount
    {
        public string bank;
        public string number;
        public string holder;
        [Tooltip("One line under the holder, e.g. \"Personal account\" or \"Virtual account of Hangang Express\".")]
        public string note;
        public List<string> reports = new List<string>();
    }

    [Serializable]
    public class DirNumber
    {
        public string number;
        public string owner;
        [Tooltip("One line under the owner, e.g. \"Internet phone, opened 3 days ago\".")]
        public string note;
        [Tooltip("A company's or public office's own number.")]
        public bool official;
        public List<string> reports = new List<string>();
    }
}
