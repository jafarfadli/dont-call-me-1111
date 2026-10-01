using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>What the room's panels show for one day: the newspaper, the desk drawer and the wall calendar.</summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Room Content")]
    public class RoomContent : ScriptableObject
    {
        public NewspaperData newspaper = new NewspaperData();
        public List<DocumentData> drawer = new List<DocumentData>();
        public CalendarData calendar = new CalendarData();
    }

    [Serializable]
    public class NewspaperData
    {
        public string masthead = "SEOUL DAILY";
        public string issue = "No. 12,408";
        public string dateLine = "TUESDAY, OCTOBER 6, 2026";
        public string price = "1,000 won";
        public string headline;
        public string subhead;
        public Texture2D photo;
        public string caption;
        [TextArea(3, 10)] public List<string> body = new List<string>();
        public string warningTitle;
        [TextArea(2, 6)] public string warningText;
        [Tooltip("The printed front page on the desk prop for this issue; empty keeps the room's own texture.")]
        public Texture2D print;
        public List<NewsItem> local = new List<NewsItem>();
        public List<NewsItem> ads = new List<NewsItem>();
    }

    [Serializable]
    public class NewsItem
    {
        public string title;
        [TextArea(2, 6)] public string text;
    }

    public enum DocumentKind { Contract, Bill, Receipt, Letter, Stub }

    [Serializable]
    public class DocumentData
    {
        public string title;
        public DocumentKind kind;
        public string issuer;
        public List<DocField> fields = new List<DocField>();
        [TextArea(2, 8)] public string body;
        public string stamp;          // "stamp_paid", "stamp_overdue" or empty
        public Texture2D image;       // optional scan, e.g. a receipt
    }

    [Serializable]
    public class DocField
    {
        public string label;
        public string value;
        public bool isFact;
        public FactKind factKind;
    }

    /// <summary>The pharmacy calendar on the cork board, with what Jiwoo wrote on it.</summary>
    [Serializable]
    public class CalendarData
    {
        public string title;
        public Texture2D image;
        [Tooltip("Today, as the panel's heading says it, e.g. \"Today: Wednesday, 7 October\".")]
        public string todayLabel;
        [Tooltip("Day of the month, to mark today's note.")]
        public int today;
        public List<CalendarEntry> entries = new List<CalendarEntry>();
    }

    [Serializable]
    public class CalendarEntry
    {
        public int day;
        [Tooltip("The date as the list shows it, e.g. \"Wed 7\".")]
        public string label;
        [TextArea(1, 3)] public string text;
    }
}
