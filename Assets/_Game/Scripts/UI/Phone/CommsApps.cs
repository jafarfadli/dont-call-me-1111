using System.Collections.Generic;
using System.Linq;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    // ==================================================================== Contacts

    public class ContactsApp : PhoneApp
    {
        public override string Id => "contacts";
        public override string Name => Loc.T("Contacts");
        public override string Icon => "app_contacts";

        public override PhoneScreen CreateHome()
        {
            var s = new PhoneScreen(Phone, Loc.T("Contacts"), "#D9853B");
            ClueEvents.Raise(ClueEvent.ContactsOpened);
            foreach (var c in Data.contacts.OrderBy(c => c.name))
            {
                var contact = c;
                RowFor(s, contact);
            }
            return s;
        }

        void RowFor(PhoneScreen s, Contact contact)
        {
            var row = UIKit.Div("list-row");
            var av = UIKit.Div("list-row__avatar");
            UIKit.SetImage(av, UISkin.Tex(contact.portrait ?? "pt_unknown") ?? UISkin.Tex("pt_unknown"));
            row.Add(av);
            var text = UIKit.Div("list-row__text");
            text.Add(UIKit.Text(contact.name, "list-row__title"));
            text.Add(UIKit.Text(contact.number, "list-row__subtitle"));
            row.Add(text);
            row.RegisterCallback<ClickEvent>(e =>
            {
                Sfx.Play(Sfx.Click, 0.5f);
                Phone.Push(Detail(contact));
                e.StopPropagation();
            });
            s.Content.Add(row);
        }

        PhoneScreen Detail(Contact c)
        {
            var s = new PhoneScreen(Phone, c.name, "#D9853B");
            var d = UIKit.Div("detail");
            var p = UIKit.Portrait(c.portrait, "portrait--round");
            p.style.width = 150;
            p.style.height = 150;
            p.style.borderTopLeftRadius = p.style.borderTopRightRadius = p.style.borderBottomLeftRadius = p.style.borderBottomRightRadius = 75;
            p.style.alignSelf = Align.Center;
            d.Add(p);
            d.Add(UIKit.Text(c.name, "detail__big"));
            var chips = UIKit.Div("chip-row");
            chips.style.justifyContent = Justify.Center;
            chips.Add(UIKit.Chip(c.number, FactKind.Phone));
            d.Add(chips);
            if (!string.IsNullOrEmpty(c.memo))
                d.Add(UIKit.Text(c.memo, "detail__line", "t-center"));
            var chat = Data.chats.Find(t => t.title == c.name);
            if (chat != null)
            {
                var actions = UIKit.Div("row");
                actions.style.justifyContent = Justify.Center;
                actions.Add(UIKit.Btn(Loc.T("Open chat"), () => Phone.Push(Phone.App<ChatsApp>().OpenThread(chat.id)), "btn--blue"));
                d.Add(actions);
            }
            s.Content.Add(d);
            return s;
        }
    }

    // ==================================================================== chat bubbles

    public static class Bubbles
    {
        public static VisualElement Message(string text, bool outgoing, string avatar = null, string name = null, string when = null,
                                            string photoCaption = null, List<Fact> facts = null)
        {
            var msg = UIKit.Div("msg");
            msg.EnableInClassList("msg--out", outgoing);
            if (!outgoing && !string.IsNullOrEmpty(avatar))
                msg.Add(UIKit.Portrait(avatar, "msg__avatar"));
            var col = UIKit.Div("msg__col");
            if (!outgoing && !string.IsNullOrEmpty(name))
                col.Add(UIKit.Text(name, "msg__name"));
            if (!string.IsNullOrEmpty(photoCaption))
            {
                var photo = UIKit.Div("msg__photo");
                photo.Add(UIKit.Text(Loc.T("[Photo]") + "  " + photoCaption, "msg__photo-caption"));
                col.Add(photo);
            }
            if (!string.IsNullOrEmpty(text))
            {
                var bubble = UIKit.Div("msg__bubble");
                bubble.Add(UIKit.Text(text));
                if (facts != null && facts.Count > 0)
                {
                    var chips = UIKit.Div("chip-row");
                    foreach (var f in facts)
                        chips.Add(UIKit.Chip(f));
                    bubble.Add(chips);
                }
                col.Add(bubble);
            }
            if (!string.IsNullOrEmpty(when))
                col.Add(UIKit.Text(when, "msg__time"));
            msg.Add(col);
            return msg;
        }
    }

    // ==================================================================== Chats (messenger)

    /// <summary>Jiwoo's chats: family, the building's residents, friends and the notice channels of shops and couriers.</summary>
    public class ChatsApp : PhoneApp
    {
        public override string Id => "chats";
        public override string Name => Loc.T("Chats");
        public override string Icon => "app_talk";

        public override int Badge
        {
            get
            {
                int n = 0;
                foreach (var v in unread.Values)
                    n += v;
                return n;
            }
        }

        readonly Dictionary<string, int> unread = new Dictionary<string, int>();
        ThreadScreen open;

        public override PhoneScreen CreateHome()
        {
            var s = new PhoneScreen(Phone, Loc.T("Chats"), "#F2CB3A");
            foreach (var t in Data.chats)
            {
                var thread = t;
                var last = t.messages.Count > 0 ? t.messages[t.messages.Count - 1] : null;
                string snippet = last == null ? "" : !string.IsNullOrEmpty(last.photoCaption) && string.IsNullOrEmpty(last.text) ? Loc.T("[Photo]") : last.text;
                // Two lines of preview: Hangul is about twice as wide as Latin letters.
                int max = Loc.Ko ? 30 : 54;
                if (snippet != null && snippet.Length > max)
                    snippet = snippet.Substring(0, max).TrimEnd() + "…";
                unread.TryGetValue(t.id, out int n);
                var row = UIKit.Div("list-row");
                var av = UIKit.Div("list-row__avatar");
                UIKit.SetImage(av, UISkin.Tex(t.avatar) ?? UISkin.Tex("pt_unknown"));
                row.Add(av);
                var text = UIKit.Div("list-row__text");
                var top = UIKit.Div("list-row__top");
                top.Add(UIKit.Text(t.title + (t.group ? $"  <color=#453C42>{Members(t)}</color>" : ""), "list-row__title"));
                if (last != null)
                    top.Add(UIKit.Text(last.when, "list-row__meta"));
                text.Add(top);
                text.Add(UIKit.Text(snippet, "list-row__subtitle"));
                row.Add(text);
                if (n > 0)
                {
                    var badge = UIKit.Text(n.ToString(), "badge");
                    badge.style.position = Position.Relative;
                    badge.style.right = 0;
                    badge.style.top = 0;
                    row.Add(badge);
                }
                row.RegisterCallback<ClickEvent>(e =>
                {
                    Sfx.Play(Sfx.Click, 0.5f);
                    Phone.Push(OpenThread(thread.id));
                    e.StopPropagation();
                });
                s.Content.Add(row);
            }
            return s;
        }

        static string Members(ChatThread t)
        {
            var names = new HashSet<string>();
            foreach (var m in t.messages)
                if (!m.outgoing && !string.IsNullOrEmpty(m.sender))
                    names.Add(m.sender);
            return (names.Count + 1).ToString();
        }

        public ChatThread Thread(string id) => Data.chats.Find(t => t.id == id);

        /// <summary>The thread's screen, or null when there is no thread with that id.</summary>
        public PhoneScreen OpenThread(string chatId)
        {
            var thread = Thread(chatId);
            if (thread == null)
                return null;
            unread.Remove(chatId);
            ClueEvents.Raise(ClueEvent.ChatRead, chatId);
            open = new ThreadScreen(this, thread);
            return open;
        }

        /// <summary>A message that came in before the day started and hasn't been read.</summary>
        public void MarkUnread(string chatId)
        {
            unread.TryGetValue(chatId, out int n);
            unread[chatId] = n + 1;
        }

        /// <summary>A message arrives: stored, the thread moves to the top; shown live if it is open, else a badge and a toast.</summary>
        public void Append(string chatId, ChatMessage m)
        {
            var t = Thread(chatId);
            if (t == null)
                return;
            t.messages.Add(m);
            Data.chats.Remove(t);
            Data.chats.Insert(0, t);
            if (open != null && open.ChatId == chatId)
            {
                open.AddMessage(m);
            }
            else if (!m.outgoing)
            {
                unread.TryGetValue(chatId, out int n);
                unread[chatId] = n + 1;
                Phone.UI.Toast("app_talk", Loc.T("Chats") + $" · {t.title}", string.IsNullOrEmpty(m.text) ? Loc.T("[Photo]") : m.text);
            }
            if (!m.outgoing)
                Sfx.Play(Sfx.Pop);
        }

        class ThreadScreen : PhoneScreen
        {
            readonly ChatsApp app;
            readonly ChatThread thread;

            public string ChatId => thread.id;

            public ThreadScreen(ChatsApp app, ChatThread thread) : base(app.Phone, thread.title, "#F2CB3A")
            {
                this.app = app;
                this.thread = thread;
                foreach (var m in thread.messages)
                    AddMessage(m, false);
                UIKit.ScrollToEnd(Body);
            }

            public override void OnHide()
            {
                if (app.open == this)
                    app.open = null;
            }

            public override void OnShow()
            {
                app.open = this;
            }

            public void AddMessage(ChatMessage m, bool scroll = true)
            {
                Content.Add(Bubbles.Message(m.text, m.outgoing, m.avatar ?? thread.avatar, thread.group ? m.sender : null, m.when, m.photoCaption, m.facts));
                if (scroll)
                    UIKit.ScrollToEnd(Body);
            }
        }
    }
}
