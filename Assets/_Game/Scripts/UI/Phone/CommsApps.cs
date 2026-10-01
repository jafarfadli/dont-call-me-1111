using System;
using System.Collections.Generic;
using System.Linq;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    // ==================================================================== Phone (recents + keypad)

    public class CallsApp : PhoneApp
    {
        public override string Id => "phone";
        public override string Name => Loc.T("Phone");
        public override string Icon => "app_phone";
        public override int Badge => unseenMissed;

        int unseenMissed;

        public override PhoneScreen CreateHome()
        {
            unseenMissed = 0;
            return new CallsScreen(this, false, null);
        }

        public PhoneScreen CreateKeypad(string prefill = null) => new CallsScreen(this, true, prefill);

        /// <summary>Missed calls already in the log when the day starts (they show on the app's badge).</summary>
        public void AddMissed(int count) => unseenMissed += Mathf.Max(0, count);

        public void AddRecord(string number, CallKind kind, string duration = "")
        {
            Data.recents.Insert(0, new CallRecord { number = number, kind = kind, when = Loc.Today + " " + GameClock.Now, duration = duration });
            if (kind == CallKind.Missed)
                unseenMissed++;
        }

        class CallsScreen : PhoneScreen
        {
            readonly CallsApp app;
            readonly VisualElement list;
            readonly Button recentsTab;
            readonly Button keypadTab;
            Label display;
            Label nameLabel;
            string typed = "";

            public CallsScreen(CallsApp app, bool keypad, string prefill) : base(app.Phone, Loc.T("Phone"), "#5E9E55")
            {
                this.app = app;
                var tabs = UIKit.Div("row");
                recentsTab = UIKit.Btn(Loc.T("Recents"), () => Show(false), "grow");
                keypadTab = UIKit.Btn(Loc.T("Keypad"), () => Show(true), "grow");
                tabs.Add(recentsTab);
                tabs.Add(keypadTab);
                Content.Add(tabs);
                list = UIKit.Div();
                Content.Add(list);
                typed = prefill ?? "";
                Show(keypad);
            }

            void Show(bool keypad)
            {
                recentsTab.EnableInClassList("btn--amber", !keypad);
                keypadTab.EnableInClassList("btn--amber", keypad);
                list.Clear();
                if (keypad)
                    BuildKeypad();
                else
                    BuildRecents();
            }

            void BuildRecents()
            {
                foreach (var r in app.Data.recents)
                {
                    var contact = app.Data.FindContact(r.number);
                    string title = contact != null ? contact.name : r.number;
                    string kind = Loc.T(r.kind == CallKind.Missed ? "Missed" : r.kind == CallKind.Incoming ? "Incoming" : "Outgoing");
                    string sub = contact != null ? $"{kind} · {r.number}" : kind + (string.IsNullOrEmpty(r.duration) ? "" : $" · {r.duration}");
                    var number = r.number;
                    Row(contact?.portrait ?? "pt_unknown", true, title, sub, r.when, () => app.Phone.Dial(number),
                        r.kind == CallKind.Missed ? "list-row__meta--red" : null, list);
                }
            }

            void BuildKeypad()
            {
                display = UIKit.Text(typed, "keypad-display");
                nameLabel = UIKit.Text("", "keypad-name");
                list.Add(display);
                list.Add(nameLabel);
                var pad = UIKit.Div("keypad");
                foreach (string k in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" })
                {
                    var key = k;
                    pad.Add(UIKit.Btn(k, () => Type(key), "key"));
                }
                list.Add(pad);
                var actions = UIKit.Div("row");
                actions.style.justifyContent = Justify.SpaceAround;
                actions.Add(UIKit.Btn(Loc.T("Paste"), () =>
                {
                    if (!string.IsNullOrEmpty(Clipboard.Value))
                    {
                        typed = Clipboard.Value;
                        Refresh();
                    }
                }));
                var call = UIKit.Div("call-btn");
                call.Add(UIKit.Icon("ic_answer"));
                call.tooltip = Loc.T("Call");
                call.RegisterCallback<ClickEvent>(e =>
                {
                    if (FactText.Digits(typed).Length >= 3)
                        app.Phone.Dial(typed);
                    e.StopPropagation();
                });
                actions.Add(call);
                actions.Add(UIKit.Btn(Loc.T("Del"), () =>
                {
                    if (typed.Length > 0)
                        typed = typed.Substring(0, typed.Length - 1);
                    Refresh();
                }));
                list.Add(actions);
                Refresh();
            }

            void Type(string k)
            {
                if (typed.Length < 16)
                    typed += k;
                Sfx.Play(Sfx.Tick, 0.4f);
                Refresh();
            }

            void Refresh()
            {
                if (display == null)
                    return;
                display.text = typed;
                var contact = app.Data.FindContact(typed);
                var known = app.Dir.FindNumber(typed);
                nameLabel.text = contact != null ? contact.name : known != null && known.official ? known.owner : "";
            }
        }
    }

    // ==================================================================== Contacts

    public class ContactsApp : PhoneApp
    {
        public override string Id => "contacts";
        public override string Name => Loc.T("Contacts");
        public override string Icon => "app_contacts";

        public override PhoneScreen CreateHome()
        {
            var s = new PhoneScreen(Phone, Loc.T("Contacts"), "#D9853B");
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
            var actions = UIKit.Div("row");
            actions.style.justifyContent = Justify.Center;
            actions.Add(UIKit.Btn(Loc.T("Call"), () => Phone.Dial(c.number), "btn--green"));
            var chat = Data.chats.Find(t => t.title == c.name);
            if (chat != null)
                actions.Add(UIKit.Btn(Loc.T("Message"), () => Phone.Push(Phone.App<TalkApp>().OpenThread(chat.id)), "btn--blue"));
            d.Add(actions);
            s.Content.Add(d);
            return s;
        }
    }

    // ==================================================================== shared bubbles

    public static class Bubbles
    {
        public static VisualElement Message(PhoneController phone, string text, bool outgoing, bool sms, string avatar = null,
                                            string name = null, string when = null, string photoCaption = null, string link = null,
                                            List<Fact> facts = null)
        {
            var msg = UIKit.Div("msg");
            msg.EnableInClassList("msg--out", outgoing);
            msg.EnableInClassList("msg--sms", sms);
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
                if (!string.IsNullOrEmpty(link))
                {
                    var l = UIKit.Text($"<u>{link}</u>", "t-bold");
                    l.style.color = new Color(0.17f, 0.29f, 0.6f);
                    l.style.marginTop = 4;
                    l.tooltip = Loc.T("Open in Browser");
                    l.RegisterCallback<ClickEvent>(e =>
                    {
                        phone.OpenUrl(link);
                        e.StopPropagation();
                    });
                    bubble.Add(l);
                    var chips = UIKit.Div("chip-row");
                    chips.Add(UIKit.Chip(link, FactKind.Url));
                    bubble.Add(chips);
                }
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

    // ==================================================================== Messages (SMS)

    public class MessagesApp : PhoneApp
    {
        public override string Id => "messages";
        public override string Name => Loc.T("Messages");
        public override string Icon => "app_messages";
        public override int Badge => unread.Count;

        readonly HashSet<string> unread = new HashSet<string>();
        PhoneScreen openThreadScreen;
        string openSender;

        public override PhoneScreen CreateHome()
        {
            var s = new PhoneScreen(Phone, Loc.T("Messages"), "#4F79B8");
            foreach (var t in Data.sms)
            {
                var thread = t;
                var last = t.messages.Count > 0 ? t.messages[t.messages.Count - 1] : null;
                string snippet = last == null ? "" : last.text.Length > 46 ? last.text.Substring(0, 46) + "…" : last.text;
                var contact = Data.FindContact(t.sender);
                RowIn(s, contact?.portrait ?? "app_messages", contact == null, contact?.name ?? t.sender, snippet, last?.when,
                      () => Phone.Push(Thread(thread)), unread.Contains(t.sender));
            }
            return s;
        }

        void RowIn(PhoneScreen s, string image, bool icon, string title, string sub, string meta, Action click, bool isUnread)
        {
            var row = UIKit.Div("list-row");
            var img = UIKit.Div(icon ? "list-row__icon" : "list-row__avatar");
            UIKit.SetImage(img, UISkin.Tex(image));
            row.Add(img);
            var text = UIKit.Div("list-row__text");
            text.Add(UIKit.Text(title, "list-row__title"));
            text.Add(UIKit.Text(sub, "list-row__subtitle"));
            row.Add(text);
            if (!string.IsNullOrEmpty(meta))
                row.Add(UIKit.Text(meta, "list-row__meta"));
            if (isUnread)
                row.Add(UIKit.Div("dot-unread"));
            row.RegisterCallback<ClickEvent>(e =>
            {
                Sfx.Play(Sfx.Click, 0.5f);
                click();
                e.StopPropagation();
            });
            s.Content.Add(row);
        }

        PhoneScreen Thread(SmsThread t)
        {
            ClueEvents.Raise(ClueEvent.MessageRead, t.sender);
            unread.Remove(t.sender);
            var contact = Data.FindContact(t.sender);
            var s = new ThreadScreen(this, contact?.name ?? t.sender);
            s.Content.Add(UIKit.Chip(t.sender, FactKind.Phone));
            foreach (var m in t.messages)
                s.Content.Add(Bubbles.Message(Phone, m.text, m.outgoing, true, null, null, m.when, null, m.link));
            openThreadScreen = s;
            openSender = t.sender;
            UIKit.ScrollToEnd(s.Body);
            return s;
        }

        class ThreadScreen : PhoneScreen
        {
            readonly MessagesApp app;

            public ThreadScreen(MessagesApp app, string title) : base(app.Phone, title, "#4F79B8")
            {
                this.app = app;
            }

            public override void OnHide()
            {
                if (app.openThreadScreen == this)
                    app.openThreadScreen = null;
            }
        }

        /// <summary>A thread that came in before the day started and hasn't been read.</summary>
        public void MarkUnread(string sender) => unread.Add(sender);

        /// <summary>A text arrives: stored, badge, toast; appended live if its thread is open.</summary>
        public void Deliver(string sender, string text, string link)
        {
            var t = Data.sms.Find(x => x.sender == sender);
            if (t == null)
            {
                t = new SmsThread { sender = sender };
                Data.sms.Insert(0, t);
            }
            else
            {
                Data.sms.Remove(t);
                Data.sms.Insert(0, t);
            }
            var m = new SmsMessage { when = GameClock.Now, text = text, link = link };
            t.messages.Add(m);
            if (openThreadScreen != null && openSender == sender)
            {
                openThreadScreen.Content.Add(Bubbles.Message(Phone, m.text, false, true, null, null, m.when, null, m.link));
                UIKit.ScrollToEnd(openThreadScreen.Body);
            }
            else
            {
                unread.Add(sender);
            }
            Sfx.Play(Sfx.Pop);
            Phone.UI.Toast("app_messages", Loc.T("Messages") + " · " + (Data.FindContact(sender)?.name ?? sender), text);
        }
    }

    // ==================================================================== Talk (messenger)

    public class TalkApp : PhoneApp
    {
        public override string Id => "talk";
        public override string Name => Loc.T("Talk");
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

        public event Action<string> ThreadOpened;

        readonly Dictionary<string, int> unread = new Dictionary<string, int>();
        ThreadScreen open;
        PendingChoice pending;

        class PendingChoice
        {
            public string chatId;
            public ConvDecision decision;
            public Action<int> pick;
        }

        public override PhoneScreen CreateHome()
        {
            var s = new PhoneScreen(Phone, Loc.T("Talk"), "#F2CB3A");
            foreach (var t in Data.chats)
            {
                var thread = t;
                var last = t.messages.Count > 0 ? t.messages[t.messages.Count - 1] : null;
                string snippet = last == null ? "" : !string.IsNullOrEmpty(last.photoCaption) && string.IsNullOrEmpty(last.text) ? Loc.T("[Photo]") : last.text;
                if (snippet != null && snippet.Length > 42)
                    snippet = snippet.Substring(0, 42) + "…";
                unread.TryGetValue(t.id, out int n);
                var row = UIKit.Div("list-row");
                var av = UIKit.Div("list-row__avatar");
                UIKit.SetImage(av, UISkin.Tex(t.avatar) ?? UISkin.Tex("pt_unknown"));
                row.Add(av);
                var text = UIKit.Div("list-row__text");
                text.Add(UIKit.Text(t.title + (t.group ? $"  <color=#8A7C74>{Members(t)}</color>" : ""), "list-row__title"));
                text.Add(UIKit.Text(snippet, "list-row__subtitle"));
                row.Add(text);
                if (last != null)
                    row.Add(UIKit.Text(last.when, "list-row__meta"));
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

        public bool IsOpen(string chatId) => open != null && open.ChatId == chatId;

        /// <summary>The thread's screen, or null when there is no thread with that id.</summary>
        public PhoneScreen OpenThread(string chatId)
        {
            var thread = Thread(chatId);
            if (thread == null)
                return null;
            unread.Remove(chatId);
            ClueEvents.Raise(ClueEvent.ChatRead, chatId);
            open = new ThreadScreen(this, thread);
            ThreadOpened?.Invoke(chatId);
            return open;
        }

        /// <summary>A message that came in before the day started and hasn't been read.</summary>
        public void MarkUnread(string chatId)
        {
            unread.TryGetValue(chatId, out int n);
            unread[chatId] = n + 1;
        }

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
                Phone.UI.Toast("app_talk", Loc.T("Talk") + $" · {t.title}", string.IsNullOrEmpty(m.text) ? Loc.T("[Photo]") : m.text);
            }
            if (!m.outgoing)
                Sfx.Play(Sfx.Pop);
        }

        public void ShowTyping(string chatId, bool on)
        {
            if (open != null && open.ChatId == chatId)
                open.SetTyping(on);
        }

        public void ShowChoices(string chatId, ConvDecision decision, Action<int> pick)
        {
            pending = new PendingChoice { chatId = chatId, decision = decision, pick = pick };
            if (open != null && open.ChatId == chatId)
                open.ShowChoices(decision, pick);
        }

        public void ClearChoices()
        {
            pending = null;
            open?.ClearChoices();
        }

        public void UpdateTimer(float remaining, float total) => open?.UpdateTimer(remaining, total);

        /// <summary>Choose option 0 or 1 of the pending decision (keys 1 and 2).</summary>
        public bool PickFromKeyboard(int index)
        {
            if (pending == null || open == null || open.ChatId != pending.chatId)
                return false;
            var p = pending;
            ClearChoices();
            p.pick(index);
            return true;
        }

        class ThreadScreen : PhoneScreen
        {
            readonly TalkApp app;
            readonly ChatThread thread;
            readonly VisualElement replyBar;
            TypingDots typing;
            DecisionTimer timer;

            public string ChatId => thread.id;

            public ThreadScreen(TalkApp app, ChatThread thread) : base(app.Phone, thread.title, "#F2CB3A")
            {
                this.app = app;
                this.thread = thread;
                Header.RegisterCallback<ClickEvent>(_ => app.Phone.Push(Profile(thread)));
                Header.tooltip = Loc.T("Profile");
                if (thread.notFriend)
                {
                    var warn = UIKit.Div("banner-warn");
                    warn.Add(UIKit.Icon("ic_warning", "list-row__icon"));
                    warn.Add(UIKit.Text(Loc.T("<b>Not in your friends list.</b> Be careful if this profile asks for money.")));
                    Content.Add(warn);
                }
                foreach (var m in thread.messages)
                    AddMessage(m, false);
                replyBar = UIKit.Div("reply-bar");
                replyBar.Hide(true);
                Root.Add(replyBar);
                if (app.pending != null && app.pending.chatId == thread.id)
                    ShowChoices(app.pending.decision, app.pending.pick);
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
                SetTyping(false);
                Content.Add(Bubbles.Message(app.Phone, m.text, m.outgoing, false, m.avatar ?? thread.avatar,
                                            thread.group ? m.sender : null, m.when, m.photoCaption, null, m.facts));
                if (scroll)
                    UIKit.ScrollToEnd(Body);
            }

            public void SetTyping(bool on)
            {
                if (on && typing == null)
                {
                    typing = new TypingDots();
                    Content.Add(typing);
                    UIKit.ScrollToEnd(Body);
                }
                else if (!on && typing != null)
                {
                    typing.RemoveFromHierarchy();
                    typing = null;
                }
            }

            public void ShowChoices(ConvDecision d, Action<int> pick)
            {
                replyBar.Clear();
                var top = UIKit.Div("reply-bar__prompt");
                top.Add(UIKit.Text(string.IsNullOrEmpty(d.prompt) ? Loc.T("Reply") : d.prompt, "decide__prompt"));
                timer = new DecisionTimer(true);
                top.Add(timer);
                replyBar.Add(top);
                var options = new[] { d.a, d.b };
                for (int i = 0; i < 2; i++)
                {
                    int index = i;
                    var b = UIKit.Btn($"{i + 1}.  {options[i].label}", () =>
                    {
                        var pick2 = pick;
                        app.ClearChoices();
                        pick2(index);
                    });
                    replyBar.Add(b);
                }
                replyBar.Hide(false);
                UIKit.ScrollToEnd(Body);
            }

            public void ClearChoices()
            {
                replyBar.Clear();
                replyBar.Hide(true);
                timer = null;
            }

            public void UpdateTimer(float remaining, float total) => timer?.Set(remaining, total);

            PhoneScreen Profile(ChatThread t)
            {
                var s = new PhoneScreen(app.Phone, Loc.T("Profile"), "#F2CB3A");
                var d = UIKit.Div("detail");
                var p = UIKit.Portrait(t.avatar, "portrait--round");
                p.style.width = 160;
                p.style.height = 160;
                p.style.alignSelf = Align.Center;
                d.Add(p);
                d.Add(UIKit.Text(t.title, "detail__big"));
                if (!string.IsNullOrEmpty(t.profileId))
                {
                    var chips = UIKit.Div("chip-row");
                    chips.style.justifyContent = Justify.Center;
                    chips.Add(UIKit.Chip(t.profileId, FactKind.Text, "ID"));
                    d.Add(chips);
                }
                if (!string.IsNullOrEmpty(t.profileNote))
                    d.Add(UIKit.Text(t.profileNote, "detail__line", "t-center"));
                if (t.notFriend)
                {
                    var warn = UIKit.Div("banner-warn");
                    warn.Add(UIKit.Text(Loc.T("Not in your friends list. No friends in common.")));
                    d.Add(warn);
                }
                s.Content.Add(d);
                return s;
            }
        }
    }
}
