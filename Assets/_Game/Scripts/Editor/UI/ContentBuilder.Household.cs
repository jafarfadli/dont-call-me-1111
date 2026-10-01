using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Jiwoo's life as one timeline, shown as it stands when the day being built starts: contacts,
    /// calls, texts, chats, the bank, parcels and mail on the phone; the paper, drawer, board and
    /// wallet in the room; and the world the apps look things up in. Nothing here depends on how an
    /// earlier day went (that is what echoes are for) or on today's truth (the variants add that).
    /// </summary>
    public static partial class ContentBuilder
    {
        const long EverydayAtDay1 = 1284300;
        const string Tracking = "HX-5520-1183-KR";
        const string TrackingSpoken = "H X, five five two zero, one one eight three, K R";

        // Timestamps for sorting the phone's lists (newest first) once they are built.
        static readonly Dictionary<object, DateTime> stamps = new Dictionary<object, DateTime>();

        static DateTime Stamp(int month, int dom, string time)
        {
            var at = new DateTime(2026, month, dom);
            if (!string.IsNullOrEmpty(time) && TimeSpan.TryParse(time, out var t))
                at += t;
            return at;
        }

        static void NewestFirst<T>(List<T> list) =>
            list.Sort((a, b) => (stamps.TryGetValue(b, out var tb) ? tb : DateTime.MinValue).CompareTo(stamps.TryGetValue(a, out var ta) ? ta : DateTime.MinValue));

        // ================================================================ phone

        static PhoneContent HouseholdPhone()
        {
            stamps.Clear();
            var p = ScriptableObject.CreateInstance<PhoneContent>();
            p.ownerName = "Kim Jiwoo";
            p.ownerNumber = "010-6620-0921";
            p.dateLabel = today.ToString("dddd, d MMMM", System.Globalization.CultureInfo.InvariantCulture);
            p.startTime = startTime;

            p.contacts = new List<Contact>
            {
                new Contact { name = "Mom", number = "010-2231-7745", memo = "Park Hyejin", portrait = "pt_mom" },
                new Contact { name = "Dad", number = "010-4418-0902", memo = "Kim Dongsu", portrait = "pt_dad" },
                new Contact { name = "Minjun", number = "010-3321-5580", memo = "Little brother · Daejeon", portrait = "pt_minjun" },
                new Contact { name = "Yuna", number = "010-7780-1123", memo = "Concert on 7 Nov!", portrait = "pt_yuna" },
                new Contact { name = "Landlord Choi", number = "010-5512-3380", memo = "1F · rent due on the 7th", portrait = "pt_landlord" },
                new Contact { name = "Taeho (Minjun's friend)", number = "010-9043-2217", memo = "Met at Chuseok", portrait = "pt_taeho" },
                new Contact { name = "Manager Han (café)", number = "010-5530-8812", memo = "Mangwon Roasters", portrait = "pt_cafe" },
                new Contact { name = "Nuri Bank", number = "1599-0000", memo = "Customer centre, from the back of my card", portrait = "app_bank" },
                new Contact { name = "Mangwon Heights office", number = "02-555-0192", memo = "Building management", portrait = "app_contacts" },
            };

            // ---- calls
            void Call(int month, int dom, string time, string number, CallKind kind, string duration = "")
            {
                if (!Seen(month, dom, time))
                    return;
                var r = new CallRecord { number = number, kind = kind, when = At(month, dom, time), duration = duration };
                stamps[r] = Stamp(month, dom, time);
                p.recents.Add(r);
            }
            Call(10, 1, "10:15", "02-555-0181", CallKind.Incoming, "0:52");
            Call(10, 2, "14:12", "070-4852-1170", CallKind.Missed);
            Call(10, 4, "19:20", "010-7780-1123", CallKind.Outgoing, "8:31");
            Call(10, 5, "11:02", "010-5530-8812", CallKind.Incoming, "1:48");
            Call(10, 5, "21:40", "010-2231-7745", CallKind.Outgoing, "12:04");
            Call(10, 7, "12:10", "010-7780-1123", CallKind.Incoming, "2:05");
            Call(10, 7, "20:45", "010-3321-5580", CallKind.Incoming, "4:12");
            NewestFirst(p.recents);

            // ---- texts
            var threads = new Dictionary<string, SmsThread>();
            void Sms(string sender, int month, int dom, string time, string text, string link = null)
            {
                if (!Seen(month, dom, time))
                    return;
                if (!threads.TryGetValue(sender, out var t))
                {
                    t = new SmsThread { sender = sender };
                    threads[sender] = t;
                    p.sms.Add(t);
                }
                t.messages.Add(new SmsMessage { when = At(month, dom, time), text = text, link = link });
                stamps[t] = Stamp(month, dom, time);
            }
            Sms("Hangul Telecom", 10, 1, "09:00", "[Hangul Telecom] Your September bill is ₩43,200, due 20 Oct.");
            Sms("010-5903-2271", 9, 30, "18:22", "[Web발신] [Landlord notice] Your rent account has changed. Check the new account before paying: rent-notice.kr", "rent-notice.kr");
            Sms("010-8812-4471", 10, 4, "23:12", "[Web발신] [Nuri] Your account is restricted for security reasons. Verify within 24 hours:", "nuri-secure.kr/verify");
            Sms("1599-0000", 10, 5, "22:47", "[Web발신] [Nuri Bank] New device registered: MacBook Air (Seoul). If this wasn't you, call 1599-0000.");
            Sms("1599-0000", 10, 6, "09:03", "[Web발신] [Nuri Bank] Card payment ₩4,500 · Mangwon Roasters · approved.");
            Sms("Hangang Express", 10, 6, "08:30", "[Hangang Express] Your parcel (desk lamp bulbs) arrives tomorrow 16:00-18:00. Driver Kim Taesik 010-7715-5521.");
            Sms("010-7240-1182", 10, 6, "11:20", "[Web발신] [International delivery] Customs duty unpaid for your parcel. Pay now or it will be returned: kr-customs.help/pay", "kr-customs.help/pay");
            Sms("1599-0000", 10, 7, "12:40", "[Web발신] [Nuri Bank] Card payment ₩8,900 · Hanbit Univ. cafeteria · approved.");
            Sms("Hangang Express", 10, 7, "17:32", "[Hangang Express] Delivered: desk lamp bulbs, left at the door of 302. Photo in the app.");
            Sms("1599-0000", 10, 8, "09:10", "[Web발신] [Nuri Bank] Card payment ₩4,500 · Mangwon Roasters · approved.");
            NewestFirst(p.sms);

            // ---- chats
            ChatThread Thread(string id, string title, string avatar, bool group) => new ChatThread { id = id, title = title, avatar = avatar, group = group };
            void Chat(ChatThread t, int dom, string time, string sender, string avatar, string text, string photo = null)
            {
                if (!Seen(10, dom, time))
                    return;
                t.messages.Add(new ChatMessage { sender = sender, avatar = avatar, when = Oct(dom, time), text = text, photoCaption = photo });
                stamps[t] = Stamp(10, dom, time);
            }
            void Mine(ChatThread t, int dom, string time, string text)
            {
                if (!Seen(10, dom, time))
                    return;
                t.messages.Add(new ChatMessage { outgoing = true, when = Oct(dom, time), text = text });
                stamps[t] = Stamp(10, dom, time);
            }

            var family = Thread("family", "Our family", "photo_family", true);
            Chat(family, 5, "19:10", "Mom", "pt_mom", "Jiwoo-ya, did you eat dinner?");
            Mine(family, 5, "19:14", "Yes! Kimchi stew after my shift");
            Chat(family, 5, "20:02", "Dad", "pt_dad", null, "Grandma's persimmons in Jeonju");
            Chat(family, 5, "22:30", "Minjun", "pt_minjun", "I'm coming up to Seoul on Saturday for a concert. Noona, can I sleep over?");
            Mine(family, 5, "22:41", "Only if you bring snacks");
            Chat(family, 6, "16:12", "Mom", "pt_mom", null, "Doenjang stew on the stove");
            Chat(family, 6, "16:12", "Mom", "pt_mom", "Your dad cooked today! Come home for Mom's birthday on the 17th, okay?");
            Chat(family, 7, "08:05", "Mom", "pt_mom", "Good morning! Rent day today, don't forget 😊");
            Mine(family, 7, "08:20", "I know, Mom 🙄");
            Chat(family, 7, "21:15", "Minjun", "pt_minjun", "Midterms start Monday. Send help (and snacks)");
            Chat(family, 8, "12:02", "Dad", "pt_dad", null, "Grandma's kimchi, delivered");
            Chat(family, 8, "12:03", "Dad", "pt_dad", "Grandma made extra for you. Come and get it on the 17th.");

            var villa = Thread("villa", "Mangwon Heights residents", "app_contacts", true);
            Chat(villa, 5, "09:00", "Office", "app_contacts", "Reminder: gas safety inspection on Wed 14 Oct, 10:00-17:00. Inspectors never ask for money.");
            Chat(villa, 5, "10:31", "201", "pt_unknown", "Did anyone else get a call about a 'protected account'? Hung up on them.");
            Chat(villa, 6, "14:05", "Office", "app_contacts", "Water tank cleaning on Thursday 10:00-12:00. Please keep some water aside.");
            Chat(villa, 8, "12:15", "Office", "app_contacts", "The water is back on. Thank you for your patience!");

            var yuna = Thread("yuna", "Yuna", "pt_yuna", false);
            Chat(yuna, 4, "18:55", "Yuna", "pt_yuna", "Tickets done!! Standing A-128 and A-129, 7 Nov");
            Mine(yuna, 4, "19:01", "I owe you 55,000 won, sending it on payday");
            Chat(yuna, 4, "19:02", "Yuna", "pt_yuna", "No rush :)");
            Chat(yuna, 7, "12:14", "Yuna", "pt_yuna", "Tteokbokki after class tomorrow?");
            Mine(yuna, 7, "12:20", "Yes!! 5pm?");

            var cafe = Thread("cafe", "Mangwon Roasters crew", "pt_cafe", true);
            Chat(cafe, 5, "21:00", "Manager Han", "pt_cafe", "Wages go out on the 10th as usual. Can someone cover Saturday morning?");
            Chat(cafe, 8, "10:30", "Manager Han", "pt_cafe", "Sunday opening shift still free 🙏 Double stamps for whoever takes it.");

            p.chats = new List<ChatThread> { family, villa, yuna, cafe };
            NewestFirst(p.chats);

            // ---- bank
            long everyday = EverydayAtDay1;
            p.bank = new BankData
            {
                bankName = "Nuri Bank",
                banks = new List<string> { "Nuri Bank", "Hanbit Bank", "Daehan Bank", "Mirae Savings", "K Post" },
            };
            void Tx(int month, int dom, string time, string counterparty, string memo, long amount)
            {
                if (!Seen(month, dom, time))
                    return;
                var t = new BankTransaction { when = At(month, dom, time), counterparty = counterparty, memo = memo, amount = amount };
                stamps[t] = Stamp(month, dom, time);
                p.bank.transactions.Add(t);
                // The balance on Day 1 already counts everything before it.
                if (Stamp(month, dom, time) > Stamp(10, 6, "16:20"))
                    everyday += amount;
            }
            Tx(9, 7, "10:12", "CHOI YOUNGSIK", "September rent", -450000);
            Tx(9, 20, "09:00", "Hangul Telecom", "Phone bill", -43200);
            Tx(10, 1, "09:00", "MANGWON ROASTERS", "September wages", 486000);
            Tx(10, 3, "11:40", "PARK HYEJIN", "Chuseok pocket money", 100000);
            Tx(10, 5, "10:05", "Hangang Mall", "Desk lamp bulbs", -12900);
            Tx(10, 5, "22:41", "DAILY 24 Mangwon", "Card", -5850);
            Tx(10, 6, "09:03", "Mangwon Roasters", "Card", -4500);
            Tx(10, 7, "12:40", "Hanbit Univ. cafeteria", "Card", -8900);
            Tx(10, 8, "09:10", "Mangwon Roasters", "Card", -4500);
            NewestFirst(p.bank.transactions);
            p.bank.accounts = new List<BankAccount>
            {
                new BankAccount { name = "Nuri Everyday", number = "110-302-558814", balance = everyday },
                new BankAccount { name = "Tuition savings", number = "110-302-558990", balance = 3000000 },
            };
            void Notice(int dom, string time, string title, string body)
            {
                if (!Seen(10, dom, time))
                    return;
                var n = new BankNotice { when = Oct(dom, time), title = title, body = body };
                stamps[n] = Stamp(10, dom, time);
                p.bank.notices.Add(n);
            }
            Notice(5, "22:47", "New device registered", "MacBook Air · Seoul. If this wasn't you, call 1599-0000.");
            Notice(6, "09:03", "Card payment ₩4,500", "Mangwon Roasters · approved");
            Notice(6, "16:02", "Signed in", "This phone · Seoul. No other sign-ins today.");
            Notice(7, "12:40", "Card payment ₩8,900", "Hanbit Univ. cafeteria · approved");
            Notice(8, "09:10", "Card payment ₩4,500", "Mangwon Roasters · approved");
            NewestFirst(p.bank.notices);

            // ---- parcels
            p.parcels = new List<Parcel>
            {
                new Parcel
                {
                    item = "Desk lamp bulbs (2)", seller = "Hangang Mall", tracking = "4410 2287 1934", courier = "Hangang Express",
                    driver = "Kim Taesik", driverPhone = "010-7715-5521",
                    status = DayNumber <= 1 ? "Out for delivery tomorrow" : DayNumber == 2 ? "Out for delivery today" : "Delivered Wed 7 Oct, 17:32",
                    window = DayNumber <= 2 ? "Wed 7 Oct, 16:00-18:00" : "",
                },
                // Day 3's variants say where the monitor is by then.
                new Parcel
                {
                    item = "27-inch monitor", seller = "AliStar (overseas)", tracking = Tracking, courier = "Hangang Express (from Incheon)", overseas = true,
                    status = DayNumber <= 1 ? "Shipped from Shenzhen, 3 Oct" : "Arrived in Korea (Incheon), 7 Oct",
                },
                new Parcel
                {
                    item = "Phone case", seller = "AliStar (overseas)", status = "Delivered 21 Sep", tracking = "KR4410983312",
                    courier = "K Post", window = "", overseas = true,
                },
            };

            // ---- mail
            void Mail(int month, int dom, string time, string from, string address, string subject, string body, bool unread = false)
            {
                if (!Seen(month, dom, time))
                    return;
                var m = new MailItem { from = from, fromAddress = address, subject = subject, when = At(month, dom, time), body = body, unread = unread };
                stamps[m] = Stamp(month, dom, time);
                p.mails.Add(m);
            }
            Mail(9, 29, "22:10", "AliStar", "orders@alistar.com", "Order confirmed: 27-inch monitor",
                 "Hi Kim Jiwoo,\n\nThanks for your order!\n\n27-inch IPS monitor × 1 · US$ 214.00\nShipping to Korea · US$ 9.00\n\nWe'll email you when it ships.");
            Mail(9, 30, "10:00", "Hanbit University", "scholarship@hanbit.ac.kr", "Scholarship interview on 16 Oct",
                 "Your scholarship interview is on Friday 16 October at 14:00, Student Centre room 204. Please bring your student ID.\n\nThe scholarship office never charges fees.", true);
            Mail(10, 2, "15:30", "Daeyang Logistics careers", "careers@daeyang-logistics.co.kr", "Application received: Winter internship",
                 "Thank you for applying to the Daeyang Logistics winter internship. We will contact you from our HR office (02-555-0240) if you are invited to an interview.");
            Mail(10, 5, "20:15", "Hanbit University IT", "it-help@hanbit.ac.kr", "Your new laptop is registered",
                 "Hi Kim Jiwoo,\n\nYour MacBook Air is now registered for campus Wi-Fi. Remember to sign in to your apps (e.g. banking) on the new device.\n\nHanbit IT Help Desk");
            Mail(10, 5, "22:47", "Nuri Bank", "notice@nuribank.co.kr", "A new device was registered",
                 "A new device (MacBook Air) was registered to your Nuri Bank account in Seoul.\n\nIf this was you, you don't need to do anything. If not, call 1599-0000.\n\nNuri Bank will never ask you to move your money to 'protect' it.");
            Mail(10, 7, "09:30", "Hanbit University", "notice@hanbit.ac.kr", "Midterm timetable",
                 "Midterm exams run from Monday 13 October. Check your timetable on the student portal.");
            NewestFirst(p.mails);
            return p;
        }

        // ================================================================ world directory

        static WorldDirectory HouseholdDirectory()
        {
            var d = ScriptableObject.CreateInstance<WorldDirectory>();
            d.accounts = new List<DirAccount>
            {
                new DirAccount
                {
                    bank = "Nuri Bank", number = "110-900-551207", holder = "JEONG MIRAN",
                    reports = new List<string>
                    {
                        "4 Oct · \"Said he was from the Nuri Bank protection team. I sent 2,000,000 won.\"",
                        "29 Sep · \"Protected account story. The money was gone in minutes.\"",
                    },
                },
                new DirAccount { bank = "Daehan Bank", number = "3333-12-7788901", holder = "YOON JAEHEE" },
                new DirAccount { bank = "Nuri Bank", number = "110-302-558814", holder = "KIM JIWOO" },
                new DirAccount { bank = "Nuri Bank", number = "110-302-558990", holder = "KIM JIWOO" },
                new DirAccount { bank = "Nuri Bank", number = "110-845-100233", holder = "PARK HYEJIN" },
                new DirAccount { bank = "Nuri Bank", number = "110-771-209944", holder = "CHOI YOUNGSIK" },
                new DirAccount { bank = "Nuri Bank", number = "110-287-004411", holder = "PARK TAEHO" },
            };
            d.numbers = new List<DirNumber>
            {
                new DirNumber
                {
                    number = "070-8844-2019", owner = "Unknown (internet phone)",
                    reports = new List<string>
                    {
                        "5 Oct · \"Nuri Bank 'account protection team'. Wanted my savings moved.\"",
                        "5 Oct · \"Knew the last digits of my account. Very convincing.\"",
                        "4 Oct · \"Manager Jeon. Told me to stay on the line and not tell anyone.\"",
                        "3 Oct · \"Protected account 110-900-551207, someone else's name on it.\"",
                        "2 Oct · \"Said someone logged in from Busan.\"",
                        "1 Oct · \"Scam. Hung up.\"",
                        "30 Sep · \"Bank impersonation.\"",
                    },
                },
                new DirNumber { number = "1599-0000", owner = "Nuri Bank customer centre", official = true },
                new DirNumber { number = "1588-5520", owner = "Hangang Express customer centre", official = true },
                new DirNumber { number = "1332", owner = "Financial Supervisory Service fraud hotline", official = true },
                new DirNumber { number = "112", owner = "Police", official = true },
                new DirNumber { number = "02-555-0112", owner = "Mapo Police Station", official = true },
                new DirNumber { number = "02-555-0181", owner = "Mapo City Gas", official = true },
                new DirNumber { number = "02-555-0192", owner = "Mangwon Heights office", official = true },
                new DirNumber
                {
                    number = "070-4852-1170", owner = "Unknown (internet phone)",
                    reports = new List<string>
                    {
                        "2 Oct · \"'Customs': pay duty for a parcel I never ordered.\"",
                        "1 Oct · \"Customs scam, asked for a transfer.\"",
                        "28 Sep · \"Fake customs clearance.\"",
                        "25 Sep · \"Scam.\"",
                    },
                },
                new DirNumber
                {
                    number = "010-8812-4471", owner = "Unknown",
                    reports = new List<string>
                    {
                        "Sun · \"Fake Nuri 'restricted account' text with a link.\"",
                        "Sat · \"Smishing link nuri-secure.kr\"",
                        "Sat · \"Spam.\"",
                    },
                },
                new DirNumber
                {
                    number = "010-5903-2271", owner = "Unknown",
                    reports = new List<string> { "1 Oct · \"Fake 'rent account changed' text.\"", "30 Sep · \"Smishing, rent-notice.kr\"" },
                },
                new DirNumber
                {
                    number = "010-7240-1182", owner = "Unknown",
                    reports = new List<string> { "6 Oct · \"'Unpaid customs duty' text with a link.\"", "6 Oct · \"Smishing.\"" },
                },
            };
            d.pages = new List<WebPage>
            {
                new WebPage
                {
                    url = "nuribank.co.kr/help", title = "Nuri Bank customer centre", site = "Nuri Bank", official = true,
                    aliases = new List<string> { "nuri bank", "누리은행", "nuri", "bank", "customer centre", "protection team", "protected account", "safe account" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Need help with your account or card? We're here 24 hours a day." },
                        new WebBlock { kind = WebBlockKind.Contact, text = "Customer centre (24h)", value = "1599-0000" },
                        new WebBlock { kind = WebBlockKind.Warning, text = "Nuri Bank never asks you to move money to a 'protected' or 'safe' account. There is no such account. If someone asks, hang up and call 1599-0000 yourself." },
                        new WebBlock { kind = WebBlockKind.Notice, text = "Our staff call you from 1599-0000. If you're unsure, hang up and call us back." },
                    },
                },
                new WebPage
                {
                    url = "nuri-secure.kr/verify", title = "Nuri Bank Security Verification", site = "nuri-secure.kr", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Your account has been restricted. Enter your card details to lift the restriction within 24 hours." },
                        new WebBlock { kind = WebBlockKind.Form, text = "Card number, expiry date and PIN", value = "Verify now" },
                    },
                },
                new WebPage
                {
                    url = "rent-notice.kr", title = "Rent account change notice", site = "rent-notice.kr", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Your landlord has registered a new rent account. Sign in with your bank details to see it." },
                        new WebBlock { kind = WebBlockKind.Form, text = "Bank, account number and PIN", value = "See my new account" },
                    },
                },
                new WebPage
                {
                    url = "kr-customs.help/pay", title = "Korea Customs · pay unpaid duty", site = "kr-customs.help", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Your parcel is held for unpaid customs duty (₩3,200). Pay by card now or it will be returned." },
                        new WebBlock { kind = WebBlockKind.Form, text = "Card number, expiry date and CVC", value = "Pay ₩3,200" },
                    },
                },
                new WebPage
                {
                    url = "fss.or.kr/fraud", title = "Financial Supervisory Service: report financial fraud", site = "FSS", official = true,
                    aliases = new List<string> { "fss", "financial supervisory service", "금융감독원", "1332", "voice phishing", "safe account", "fraud" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Contact, text = "Fraud hotline", value = "1332" },
                        new WebBlock { kind = WebBlockKind.Warning, text = "No bank, prosecutor or government office will ask you to transfer money to a 'safe account' or an 'inspection account'." },
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Sent money to a scammer? Call your bank or 112 at once: a transfer can be frozen if you act within minutes." },
                    },
                },
                new WebPage
                {
                    url = "police.go.kr/mapo", title = "Mapo Police Station", site = "Mapo Police", official = true,
                    aliases = new List<string> { "mapo police", "police", "경찰", "mapo police station" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Contact, text = "Main number", value = "02-555-0112" },
                        new WebBlock { kind = WebBlockKind.Contact, text = "Emergency", value = "112" },
                        new WebBlock { kind = WebBlockKind.Notice, text = "Police never ask for transfers, 'safe accounts' or your bank details over the phone." },
                    },
                },
                new WebPage
                {
                    url = "mapocitygas.co.kr", title = "Mapo City Gas: safety inspections", site = "Mapo City Gas", official = true,
                    aliases = new List<string> { "gas", "mapo city gas", "inspection", "boiler" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Contact, text = "Customer centre", value = "02-555-0181" },
                        new WebBlock { kind = WebBlockKind.Notice, text = "Inspections are announced by your building two weeks ahead. Inspectors show an ID card and never collect money." },
                    },
                },
                new WebPage
                {
                    url = "seoulhousing.go.kr/rent", title = "Seoul Housing Centre: paying your rent safely", site = "Seoul Housing Centre", official = true,
                    aliases = new List<string> { "rent", "landlord", "housing", "lease", "rent account", "tenant", "월세" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Most leases say how the rent account can change. If yours says 'in writing', a phone call alone is not enough." },
                        new WebBlock { kind = WebBlockKind.Warning, text = "A new rent account in the name of someone who isn't in your lease is the most common rental scam. Check the name before you send." },
                        new WebBlock { kind = WebBlockKind.Notice, text = "Unsure? Ask your landlord on the number you already have, or check the residents' notices." },
                    },
                },
                new WebPage
                {
                    url = "customs.go.kr/import", title = "Korea Customs Service: shopping from overseas", site = "Korea Customs Service", official = true,
                    aliases = new List<string> { "customs", "관세청", "duty", "import", "vat", "korea customs", "overseas" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Personal orders up to US$150 are free of duty. Above that you pay duty and VAT, unless the seller collected them at checkout." },
                        new WebBlock { kind = WebBlockKind.Notice, text = "Duty is paid through your courier (their app or a virtual account in the courier's name) or on our UNI-PASS site." },
                        new WebBlock { kind = WebBlockKind.Warning, text = "Customs never asks you to pay into an account in a person's name, or through a link in a text." },
                    },
                },
                new WebPage
                {
                    url = "seouldaily.kr/2026/10/06/read-the-name", title = "Before you send, read the name", site = "Seoul Daily", official = true,
                    aliases = new List<string> { "seoul daily", "news", "read the name", "protected account", "voice phishing" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Callers claiming to be bank 'protection teams' are asking customers to move their savings to 'protected accounts'." },
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Every banking app shows the account holder's name before a transfer goes through. A person's name on an 'official' account is the giveaway." },
                    },
                },
            };
            d.callbacks = new List<CallbackScript>
            {
                Callback("1599-0000", "Nuri Bank customer centre", "pt_song", VoiceBank,
                    Say("Nuri Bank customer centre, this is Song Eunji speaking. How can I help?"),
                    Say("Your account looks normal. If anyone asks you to move money, hang up and call us on this number.")),
                Callback("1332", "FSS fraud hotline", "pt_oh", VoiceFss,
                    Say("Financial Supervisory Service, fraud hotline."),
                    Say("If someone asks you to move money to a 'safe account', it's a scam. Report the number to 112.",
                        "If someone asks you to move money to a safe account, it's a scam. Report the number to one one two.")),
                Callback("112", "Police 112", "pt_oh", VoicePolice,
                    Say("112, police. What's happening?", "One one two, police. What's happening?"),
                    Say("Tell us the number and the account. We can ask the bank to freeze a transfer if you call right away.")),
                Callback("010-2231-7745", "Mom", "pt_mom", VoiceMom,
                    Say("Jiwoo-ya! Did you eat?", "Jiwoo ya! Did you eat?"),
                    Say("Your dad cooked doenjang stew today. Come home on the 17th, okay?", "Your dad cooked doenjang stew today. Come home on the seventeenth, okay?")),
                Callback("010-4418-0902", "Dad", "pt_dad", VoiceDad,
                    Say("Jiwoo! Everything okay?"),
                    Say("Call your mom, she misses you.")),
                Callback("02-555-0192", "Mangwon Heights office", "app_contacts", VoicePolice,
                    Say("Mangwon Heights office."),
                    Say("The gas inspection is on the 14th. They won't ask for any money.", "The gas inspection is on the fourteenth. They won't ask for any money.")),
                Callback("070-8844-2019", "Manager Jeon", "pt_jeon", VoiceJeon,
                    Say("Nuri Bank protection team... Miss Kim? Good, please go ahead with the transfer.")),
            };
            return d;
        }

        // ================================================================ room

        static RoomContent HouseholdRoom()
        {
            var r = ScriptableObject.CreateInstance<RoomContent>();
            r.newspaper = new NewspaperData
            {
                masthead = "SEOUL DAILY",
                issue = $"No. {12407 + DayNumber:N0}",
                dateLine = today.ToString("dddd, MMMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant(),
                price = "1,000 won",
                photo = Tex("news_photo"),
                caption = "The transfer screen shows who really gets the money. Photo: Seoul Daily",
                ads = new List<NewsItem>
                {
                    new NewsItem { title = "치킨 CHICKEN 24H", text = "Crispy & soy garlic. Free delivery over 20,000 won. 02-555-0147" },
                    new NewsItem { title = "HANBIT ACADEMY", text = "TOEIC weekend class, 30% off for students. 02-555-0166" },
                    new NewsItem { title = "EASY MONEY!", text = "Remote job: forward client deposits, keep 3%. No experience! 010-3309-2214" },
                },
            };

            r.drawer = new List<DocumentData>
            {
                new DocumentData
                {
                    title = "Lease contract · Unit 302", kind = DocumentKind.Contract, issuer = "Mangwon Heights",
                    fields = new List<DocField>
                    {
                        Field("Landlord", "Choi Youngsik"),
                        Field("Landlord's phone", "010-5512-3380", FactKind.Phone),
                        Field("Rent", "450,000 won a month, due on the 7th"),
                        Field("Rent account", "Nuri Bank 110-771-209944 (Choi Youngsik)", FactKind.Account),
                        Field("Deposit", "10,000,000 won"),
                        Field("If absent, contact", "Choi Hyunwoo (son) 010-2280-6614", FactKind.Phone),
                    },
                    body = "Special terms\n1. The rent account changes only with a written notice from the landlord.\n2. The tenant allows the annual gas safety inspection.\n3. No smoking inside the unit.",
                },
                new DocumentData
                {
                    title = "Gas bill · September", kind = DocumentKind.Bill, issuer = "Mapo City Gas", stamp = "stamp_paid",
                    fields = new List<DocField>
                    {
                        Field("Customer", "Kim Jiwoo · Mangwon Heights 302"),
                        Field("Amount", "18,420 won"),
                        Field("Paid", "28 Sep (auto-pay)"),
                        Field("Customer centre", "02-555-0181", FactKind.Phone),
                    },
                    body = "Safety inspection this month: Wednesday 14 October. Our inspectors carry an ID card and never ask for money.",
                },
                new DocumentData
                {
                    title = "Mobile bill · September", kind = DocumentKind.Bill, issuer = "Hangul Telecom",
                    fields = new List<DocField>
                    {
                        Field("Number", "010-6620-0921"),
                        Field("Amount", "43,200 won"),
                        Field("Due", "20 October"),
                        Field("Customer centre", "02-555-0114", FactKind.Phone),
                    },
                },
                new DocumentData
                {
                    title = "Receipt · 5 Oct", kind = DocumentKind.Receipt, issuer = "DAILY 24 Mangwon", image = Tex("doc_receipt"),
                    fields = new List<DocField>
                    {
                        Field("Total", "5,850 won"),
                        Field("Card", "Nuri ****0921"),
                    },
                },
                new DocumentData
                {
                    title = "Scholarship interview", kind = DocumentKind.Letter, issuer = "Hanbit University",
                    fields = new List<DocField>
                    {
                        Field("When", "Fri 16 Oct, 14:00"),
                        Field("Where", "Student Centre 204"),
                        Field("Office", "02-555-0200", FactKind.Phone),
                    },
                    body = "Please bring your student ID. The scholarship office never charges a fee.",
                },
                new DocumentData
                {
                    title = "Delivery slip", kind = DocumentKind.Stub, issuer = "Hangang Express",
                    fields = new List<DocField>
                    {
                        Field("Tracking", "4410 2287 1934", FactKind.Case),
                        Field("Driver", "Kim Taesik"),
                        Field("Driver's phone", "010-7715-5521", FactKind.Phone),
                        Field("Customer centre", "1588-5520", FactKind.Phone),
                    },
                },
            };

            r.board = new List<BoardItem>
            {
                new BoardItem
                {
                    title = "Happy Pharmacy calendar · October", kind = BoardItemKind.Calendar, image = Tex("board_calendar"),
                    position = new Vector2(0.03f, 0.05f), width = 0.25f, rotation = -1.5f,
                    calendar = new List<CalendarEntry>
                    {
                        new CalendarEntry { day = 5, text = "Substitute holiday" },
                        new CalendarEntry { day = 7, text = "Rent" },
                        new CalendarEntry { day = 8, text = "Dentist" },
                        new CalendarEntry { day = 13, text = "Midterms start" },
                        new CalendarEntry { day = 14, text = "Gas check" },
                        new CalendarEntry { day = 17, text = "Mom's birthday" },
                    },
                },
                new BoardItem
                {
                    title = "Important numbers", kind = BoardItemKind.Note, image = Tex("board_numbers"),
                    position = new Vector2(0.32f, 0.04f), width = 0.24f, rotation = 2.5f,
                    details = new List<DocField>
                    {
                        Field("Police", "112", FactKind.Phone),
                        Field("Fire / ambulance", "119", FactKind.Phone),
                        Field("Voice phishing (FSS)", "1332", FactKind.Phone),
                        Field("Nuri Bank", "1599-0000", FactKind.Phone),
                        Field("Landlord", "010-5512-3380", FactKind.Phone),
                        Field("Mom", "010-2231-7745", FactKind.Phone),
                        Field("Dad", "010-4418-0902", FactKind.Phone),
                    },
                },
                new BoardItem
                {
                    title = "Building notice", kind = BoardItemKind.Notice, image = Tex("board_notice"),
                    position = new Vector2(0.6f, 0.05f), width = 0.25f, rotation = -2f,
                    details = new List<DocField>
                    {
                        Field("Gas inspection", "Wed 14 Oct, 10:00-17:00"),
                        Field("Water tank cleaning", "Thu 8 Oct, 10:00-12:00"),
                        Field("Note", "Inspectors never ask for money or bank details"),
                        Field("Office", "02-555-0192", FactKind.Phone),
                    },
                },
                new BoardItem
                {
                    title = "Sticky note", kind = BoardItemKind.Note, image = Tex("board_sticky_y"),
                    position = new Vector2(0.33f, 0.52f), width = 0.1f, rotation = -4f,
                    details = new List<DocField> { Field("Reminder", "Phone bill: pay by the 20th (auto-pay?)") },
                },
                new BoardItem
                {
                    title = "Sticky note", kind = BoardItemKind.Note, image = Tex("board_sticky_p"),
                    position = new Vector2(0.45f, 0.57f), width = 0.1f, rotation = 7f,
                    details = new List<DocField> { Field("Reminder", "Mom's birthday on the 17th: call + gift") },
                },
                new BoardItem
                {
                    title = "Concert ticket", kind = BoardItemKind.Ticket, image = Tex("board_ticket"),
                    position = new Vector2(0.58f, 0.55f), width = 0.24f, rotation = -3f,
                    details = new List<DocField> { Field("Show", "The Rainy Seasons, Hongdae"), Field("When", "Sat 7 Nov, 19:00"), Field("Seat", "Standing A-128") },
                },
                new BoardItem
                {
                    title = "Photo booth strip", kind = BoardItemKind.Photo, image = Tex("board_strip"),
                    position = new Vector2(0.86f, 0.42f), width = 0.09f, rotation = 4f,
                    details = new List<DocField> { Field("With", "Yuna, 12 Sep") },
                },
                new BoardItem
                {
                    title = "ID photo", kind = BoardItemKind.Photo, image = Tex("photo_id"),
                    position = new Vector2(0.88f, 0.08f), width = 0.08f, rotation = -6f,
                },
            };

            r.wallet = new List<WalletCard>
            {
                new WalletCard
                {
                    title = "Nuri Bank check card", front = Tex("card_nuri_front"), back = Tex("card_nuri_back"),
                    details = new List<DocField>
                    {
                        Field("Name", "KIM JIWOO"),
                        Field("Card", "9410 2231 7745 0921"),
                        Field("Customer centre", "1599-0000", FactKind.Phone),
                    },
                },
                new WalletCard
                {
                    title = "Student ID", front = Tex("card_student"),
                    details = new List<DocField> { Field("University", "Hanbit University"), Field("Student no.", "2024-10573") },
                },
                new WalletCard
                {
                    title = "Café stamp card", front = Tex("card_cafe"),
                    details = new List<DocField> { Field("Stamps", "7 of 10") },
                },
            };

            r.rules = new List<RuleEntry>
            {
                new RuleEntry
                {
                    title = "Call a number you looked up yourself",
                    text = "Unsure? Hang up and call the number on your card or the official website, not the number the caller gives you.",
                    learnedOn = "Seoul Daily, 6 Oct",
                },
                new RuleEntry
                {
                    title = "Read the name on the account",
                    text = "Before you send money, check the account holder's name on the transfer screen. A person's name on an 'official' account is a red flag.",
                    learnedOn = "Seoul Daily, 6 Oct",
                },
            };
            return r;
        }

        /// <summary>Adds a message to a Talk thread of a built phone (for a variant's own evidence).</summary>
        static void AddChat(PhoneContent p, string chatId, int dom, string time, string sender, string avatar, string text)
        {
            var t = p.chats.Find(c => c.id == chatId);
            if (t == null || !Seen(10, dom, time))
                return;
            t.messages.Add(new ChatMessage { sender = sender, avatar = avatar, when = Oct(dom, time), text = text });
            p.chats.Remove(t);
            p.chats.Insert(0, t);
        }

        /// <summary>Adds a text to a thread of a built phone, putting the thread on top.</summary>
        static void AddSms(PhoneContent p, string sender, int dom, string time, string text, string link = null)
        {
            if (!Seen(10, dom, time))
                return;
            var t = p.sms.Find(x => x.sender == sender);
            if (t == null)
                t = new SmsThread { sender = sender };
            else
                p.sms.Remove(t);
            p.sms.Insert(0, t);
            t.messages.Add(new SmsMessage { when = Oct(dom, time), text = text, link = link });
        }
    }
}
