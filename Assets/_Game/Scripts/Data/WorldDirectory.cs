using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>
    /// The world's facts that the apps look up: who owns an account (transfer name check),
    /// reports on numbers and accounts (CheckFirst), web pages (Browser), parcels by tracking
    /// number and what a number says when you call it.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/World Directory")]
    public class WorldDirectory : ScriptableObject
    {
        public List<DirAccount> accounts = new List<DirAccount>();
        public List<DirNumber> numbers = new List<DirNumber>();
        public List<WebPage> pages = new List<WebPage>();
        public List<CallbackScript> callbacks = new List<CallbackScript>();

        public DirAccount FindAccount(string bank, string number)
        {
            foreach (var a in accounts)
                if (FactText.SameNumber(a.number, number) && (string.IsNullOrEmpty(bank) || a.bank == bank))
                    return a;
            return null;
        }

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

        public CallbackScript FindCallback(string number)
        {
            foreach (var c in callbacks)
                if (FactText.SameNumber(c.number, number))
                    return c;
            return null;
        }

        /// <summary>Pages whose title, site, url or aliases contain every word of the query.</summary>
        public List<WebPage> Search(string query)
        {
            var results = new List<WebPage>();
            if (string.IsNullOrWhiteSpace(query))
                return results;
            string q = query.Trim().ToLowerInvariant();
            string qDigits = FactText.Digits(q);
            foreach (var p in pages)
            {
                string hay = (p.title + " " + p.site + " " + p.url + " " + string.Join(" ", p.aliases)).ToLowerInvariant();
                bool match = true;
                foreach (string word in q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!hay.Contains(word))
                    {
                        match = false;
                        break;
                    }
                if (!match && qDigits.Length >= 6)
                    match = FactText.Digits(hay).Contains(qDigits);
                if (match)
                    results.Add(p);
            }
            return results;
        }

        public WebPage FindPage(string url)
        {
            string u = (url ?? "").Trim().ToLowerInvariant().Replace("https://", "").Replace("http://", "").TrimEnd('/');
            foreach (var p in pages)
                if (p.url.ToLowerInvariant().TrimEnd('/') == u)
                    return p;
            return null;
        }
    }

    [Serializable]
    public class DirAccount
    {
        public string bank;
        public string number;
        public string holder;
        public List<string> reports = new List<string>();
    }

    [Serializable]
    public class DirNumber
    {
        public string number;
        public string owner;
        public bool official;
        public List<string> reports = new List<string>();
    }

    public enum WebBlockKind { Heading, Paragraph, Contact, Notice, Lookup, Form, Warning }

    [Serializable]
    public class WebBlock
    {
        public WebBlockKind kind;
        [TextArea(1, 6)] public string text;
        public string value;      // a number for Contact, the lookup type for Lookup ("case", "parcel", "business")
    }

    [Serializable]
    public class WebPage
    {
        public string url;
        public string title;
        public string site;
        public bool official;
        public List<string> aliases = new List<string>();
        public List<WebBlock> blocks = new List<WebBlock>();
        public List<LookupEntry> lookup = new List<LookupEntry>();   // answers for a Lookup block
    }

    [Serializable]
    public class LookupEntry
    {
        public string key;        // what the player searches for
        [TextArea(1, 4)] public string result;
    }

    [Serializable]
    public class CallbackScript
    {
        public string number;
        public string answeredBy;
        public string portrait;
        [TextArea(1, 4)] public List<string> lines = new List<string>();
    }
}
