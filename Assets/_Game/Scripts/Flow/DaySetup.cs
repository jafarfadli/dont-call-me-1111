using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Flow
{
    /// <summary>A day ready to play: its truth, and its evidence with the earlier days' traces added.</summary>
    public class DayPlan
    {
        public DayData day;
        public DayVariant variant;
        public PhoneContent phone;
        public RoomContent room;
        public WorldDirectory directory;
        public readonly List<DayRecord> earlier = new List<DayRecord>();
        /// <summary>Extra lines for the day card, from the echoes.</summary>
        public readonly List<string> dayCardLines = new List<string>();
        /// <summary>Echoes that pop up as notifications while Jiwoo sits at the desk.</summary>
        public readonly List<DayEcho> notifications = new List<DayEcho>();
        public readonly List<string> unreadChats = new List<string>();

        public ConversationData Conversation => variant?.conversation;
    }

    /// <summary>
    /// Picks a day's truth and assembles its evidence: the variant's phone, room and directory
    /// (cloned, so play never edits the assets), then what the earlier days left: the money that
    /// left the account, yesterday's paper on the desk, and the day's echoes for how those days went.
    /// </summary>
    public static class DaySetup
    {
        /// <summary>
        /// A random variant. A run always has at least one legit caller: on the last day that has a
        /// legit variant, if every earlier day was a scam, that day is legit.
        /// </summary>
        public static DayVariant Pick(DayData day, List<DayRecord> earlier, DayCatalog catalog, string force = null)
        {
            if (day == null || day.variants.Count == 0)
                return null;
            if (!string.IsNullOrEmpty(force))
            {
                var forced = day.Variant(force);
                if (forced != null)
                    return forced;
            }
            if (day.variants.Count == 1)
                return day.variants[0];
            var legit = day.variants.Find(v => !v.IsScam);
            if (legit != null && earlier.Count == day.day - GameRun.FirstDay && earlier.TrueForAll(r => r.scam) && !LegitLater(day, catalog))
                return legit;
            return day.variants[Random.Range(0, day.variants.Count)];
        }

        static bool LegitLater(DayData day, DayCatalog catalog)
        {
            if (catalog == null)
                return false;
            foreach (var d in catalog.days)
                if (d != null && d.day > day.day && d.variants.Exists(v => !v.IsScam))
                    return true;
            return false;
        }

        public static DayPlan Compose(DayData day, DayVariant variant, List<DayRecord> earlier, DayCatalog catalog)
        {
            today = WeekdayIndex(day.Weekday);
            var plan = new DayPlan
            {
                day = day,
                variant = variant,
                phone = variant?.phone != null ? Object.Instantiate(variant.phone) : ScriptableObject.CreateInstance<PhoneContent>(),
                room = variant?.room != null ? Object.Instantiate(variant.room) : ScriptableObject.CreateInstance<RoomContent>(),
                directory = variant?.directory != null ? Object.Instantiate(variant.directory) : ScriptableObject.CreateInstance<WorldDirectory>(),
            };
            plan.earlier.AddRange(earlier);
            foreach (var r in earlier)
                AddHistory(plan, r, catalog);
            foreach (var echo in day.echoes)
            {
                var r = earlier.Find(x => x.day == echo.afterDay);
                if (r != null && Matches(echo, r))
                    Apply(plan, echo);
            }
            return plan;
        }

        public static bool Matches(DayEcho echo, DayRecord r)
        {
            if (echo.truth == Truth.Scam && !r.scam || echo.truth == Truth.Legit && r.scam)
                return false;
            var mask = r.outcome switch
            {
                Outcome.GoAlong => OutcomeMask.GoAlong,
                Outcome.Refuse => OutcomeMask.Refuse,
                Outcome.Verify => OutcomeMask.Verify,
                _ => OutcomeMask.Timeout,
            };
            return (echo.outcomes & mask) != 0;
        }

        // ---------------------------------------------------------------- history

        static void AddHistory(DayPlan plan, DayRecord r, DayCatalog catalog)
        {
            var then = catalog != null ? catalog.Get(r.day) : null;
            int ago = plan.day.day - r.day;
            string When(string time) => (ago == 1 ? Loc.Yesterday : then != null ? then.Weekday : "") + (string.IsNullOrEmpty(time) ? "" : " " + time);

            // The money that left on the call stays gone.
            plan.phone.bank.Change(r.moneyDelta);
            if (r.moneyDelta != 0 && !string.IsNullOrEmpty(r.transferTo))
                InsertByTime(plan.phone.bank.transactions, new BankTransaction
                {
                    when = When(r.decidedAt), counterparty = r.transferTo, memo = $"{r.transferBank} {r.transferAccount}".Trim(), amount = r.moneyDelta,
                }, t => t.when);

            // Yesterday's case is on the front page of the paper on the desk.
            var variant = then?.Variant(r.variant);
            if (variant != null && ago == 1)
            {
                var paper = variant.Paper(r.outcome);
                if (paper != null)
                {
                    var n = plan.room.newspaper;
                    n.headline = paper.headline;
                    n.subhead = paper.subhead;
                    n.print = paper.print;
                    n.body = new List<string>();
                    foreach (string p in (paper.body ?? "").Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries))
                        n.body.Add(p.Trim());
                }
            }
        }

        // ---------------------------------------------------------------- echoes

        static void Apply(DayPlan plan, DayEcho e)
        {
            var phone = plan.phone;
            switch (e.kind)
            {
                case EchoKind.Chat:
                {
                    var t = phone.chats.Find(x => x.id == e.from);
                    if (t == null)
                        t = new ChatThread { id = e.from, title = string.IsNullOrEmpty(e.title) ? e.sender : e.title, avatar = e.avatar };
                    else
                        phone.chats.Remove(t);
                    phone.chats.Insert(0, t);
                    InsertInOrder(t.messages, new ChatMessage { sender = e.outgoing ? null : e.sender, avatar = e.avatar, when = e.when, text = e.text, outgoing = e.outgoing }, m => m.when);
                    if (!e.outgoing)
                        plan.unreadChats.Add(e.from);
                    break;
                }
                case EchoKind.BankTransaction:
                    phone.bank.Change(e.amount);
                    InsertByTime(phone.bank.transactions, new BankTransaction { when = e.when, counterparty = e.from, memo = e.title, amount = e.amount }, t => t.when);
                    break;
                case EchoKind.Contact:
                    if (phone.FindContact(e.from) == null)
                        phone.contacts.Add(new Contact { name = e.sender, number = e.from, memo = e.text, portrait = e.avatar });
                    break;
                case EchoKind.DayCard:
                    plan.dayCardLines.Add(e.text);
                    break;
            }
            if (e.notify)
                plan.notifications.Add(e);
        }

        /// <summary>
        /// Lists on the phone run newest first; puts the item before the first older one. Labels are
        /// "Today 09:12", "Yesterday 21:40", a weekday ("Mon 22:47") or a date ("Fri 25 Sep").
        /// </summary>
        static void InsertByTime<T>(List<T> list, T item, System.Func<T, string> when)
        {
            int key = Age(when(item));
            int i = 0;
            while (i < list.Count && Age(when(list[i])) <= key)
                i++;
            list.Insert(i, item);
        }

        static readonly string[] Weekdays = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        static readonly string[] WeekdaysKo = { "월요일", "화요일", "수요일", "목요일", "금요일", "토요일", "일요일" };
        static int today;

        static int WeekdayIndex(string label)
        {
            int i = System.Array.IndexOf(Weekdays, label);
            return i >= 0 ? i : System.Array.IndexOf(WeekdaysKo, label);
        }

        /// <summary>Threads run oldest first; puts the message before the first newer one.</summary>
        static void InsertInOrder<T>(List<T> list, T item, System.Func<T, string> when)
        {
            int key = Age(when(item));
            int i = 0;
            while (i < list.Count && Age(when(list[i])) >= key)
                i++;
            list.Insert(i, item);
        }

        /// <summary>Minutes before the end of today, roughly: smaller is newer.</summary>
        static int Age(string label)
        {
            if (string.IsNullOrEmpty(label))
                return int.MaxValue;
            // "Today 09:12", "Yesterday 21:40", "Mon 22:47", "Fri 25 Sep" (or 오늘, 어제, 월요일, 9월 25일).
            string[] parts = label.Split(' ');
            int weekday = WeekdayIndex(parts[0]);
            int days;
            if (parts[0] == "Today" || parts[0] == "오늘")
                days = 0;
            else if (parts[0] == "Yesterday" || parts[0] == "어제")
                days = 1;
            else if (weekday >= 0 && parts.Length <= 2 && today >= 0)
                days = (today - weekday + 7) % 7 == 0 ? 7 : (today - weekday + 7) % 7;
            else
                days = 30;
            int minutes = 0;
            string last = parts[parts.Length - 1];
            if (last.Length == 5 && last[2] == ':' && int.TryParse(last.Substring(0, 2), out int h) && int.TryParse(last.Substring(3, 2), out int m))
                minutes = h * 60 + m;
            return days * 1440 + (1440 - minutes);
        }
    }
}
