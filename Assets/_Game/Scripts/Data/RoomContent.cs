using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Data
{
    /// <summary>What the room's panels show for one day: newspaper, desk drawer, cork board, wallet, rules.</summary>
    [CreateAssetMenu(menuName = "Don't Call Me/Room Content")]
    public class RoomContent : ScriptableObject
    {
        public NewspaperData newspaper = new NewspaperData();
        public List<DocumentData> drawer = new List<DocumentData>();
        public List<BoardItem> board = new List<BoardItem>();
        public List<WalletCard> wallet = new List<WalletCard>();
        public List<RuleEntry> rules = new List<RuleEntry>();
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

    public enum BoardItemKind { Calendar, Note, Notice, Photo, Ticket }

    [Serializable]
    public class BoardItem
    {
        public string title;
        public BoardItemKind kind;
        public Texture2D image;
        public Vector2 position;      // 0..1 on the board, top-left of the item
        public float width = 0.2f;    // fraction of the board width
        public float rotation;
        public List<DocField> details = new List<DocField>();
        public List<CalendarEntry> calendar = new List<CalendarEntry>();
    }

    [Serializable]
    public class CalendarEntry
    {
        public int day;
        public string text;
    }

    [Serializable]
    public class WalletCard
    {
        public string title;
        public Texture2D front;
        public Texture2D back;
        public List<DocField> details = new List<DocField>();
    }

    [Serializable]
    public class RuleEntry
    {
        public string title;
        [TextArea(2, 5)] public string text;
        public string learnedOn;
    }
}
