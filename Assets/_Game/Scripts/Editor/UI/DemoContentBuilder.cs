using System.Collections.Generic;
using System.IO;
using DontCallMe.Data;
using UnityEditor;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Writes the demo content the UI templates run on (Assets/_Game/Data/Demo): Jiwoo's phone on
    /// Tuesday 6 October, the world directory, the room's papers, the M6 call ("protected account",
    /// a scam) and the E2 chat ("Mom's broken phone", a scam). Rerun it to reset edits.
    /// </summary>
    public static class DemoContentBuilder
    {
        const string Dir = "Assets/_Game/Data/Demo";
        public const string PhonePath = Dir + "/Demo_Phone.asset";
        public const string DirectoryPath = Dir + "/Demo_Directory.asset";
        public const string RoomPath = Dir + "/Demo_Room.asset";
        public const string CallPath = Dir + "/Demo_Call_M6_ProtectedAccount.asset";
        public const string ChatPath = Dir + "/Demo_Chat_E2_MomsBrokenPhone.asset";
        const string Sprites = "Assets/_Game/UI/Sprites/";

        [MenuItem("Tools/Don't Call Me/UI/Rebuild Demo Content")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data"))
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Demo");
            Save(BuildPhone(), PhonePath);
            Save(BuildDirectory(), DirectoryPath);
            Save(BuildRoom(), RoomPath);
            Save(BuildCall(), CallPath);
            Save(BuildChat(), ChatPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[DemoContentBuilder] Demo content written to " + Dir);
        }

        static void Save<T>(T data, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(data, existing);
                existing.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(data);
            }
            else
            {
                AssetDatabase.CreateAsset(data, path);
            }
        }

        static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Sprites + name + ".png");

        static Fact F(FactKind k, string v, string label = null) => new Fact(k, v, label);

        static DocField Field(string label, string value, FactKind? fact = null) =>
            new DocField { label = label, value = value, isFact = fact.HasValue, factKind = fact ?? FactKind.Text };

        // ================================================================ phone

        static PhoneContent BuildPhone()
        {
            var p = ScriptableObject.CreateInstance<PhoneContent>();
            p.ownerName = "Kim Jiwoo";
            p.ownerNumber = "010-6620-0921";
            p.dateLabel = "Tuesday, 6 October";
            p.startTime = "16:20";

            p.contacts = new List<Contact>
            {
                new Contact { name = "Mom", number = "010-2231-7745", memo = "Park Hyejin", portrait = "pt_mom" },
                new Contact { name = "Dad", number = "010-4418-0902", memo = "Kim Dongsu", portrait = "pt_dad" },
                new Contact { name = "Minjun", number = "010-3321-5580", memo = "Little brother · Daejeon", portrait = "pt_minjun" },
                new Contact { name = "Yuna", number = "010-7780-1123", memo = "Concert on 7 Nov!", portrait = "pt_yuna" },
                new Contact { name = "Landlord Choi", number = "010-5512-3380", memo = "1F · rent due on the 25th", portrait = "pt_landlord" },
                new Contact { name = "Hyunwoo (landlord's son)", number = "010-2280-6614", memo = "Helps with the building", portrait = "pt_hyunwoo" },
                new Contact { name = "Taeho (Minjun's friend)", number = "010-9043-2217", memo = "Met at Chuseok", portrait = "pt_taeho" },
                new Contact { name = "Manager Han (café)", number = "010-5530-8812", memo = "Mangwon Roasters", portrait = "pt_cafe" },
                new Contact { name = "Nuri Bank", number = "1599-0000", memo = "Customer centre, from the back of my card", portrait = "app_bank" },
                new Contact { name = "Mangwon Heights office", number = "02-555-0192", memo = "Building management", portrait = "app_contacts" },
            };

            p.recents = new List<CallRecord>
            {
                new CallRecord { number = "010-2231-7745", kind = CallKind.Outgoing, when = "Yesterday 21:40", duration = "12:04" },
                new CallRecord { number = "010-5530-8812", kind = CallKind.Incoming, when = "Yesterday 11:02", duration = "1:48" },
                new CallRecord { number = "010-7780-1123", kind = CallKind.Outgoing, when = "Sun 19:20", duration = "8:31" },
                new CallRecord { number = "070-4852-1170", kind = CallKind.Missed, when = "Fri 2 Oct 14:12", duration = "" },
                new CallRecord { number = "02-555-0181", kind = CallKind.Incoming, when = "Thu 1 Oct 10:15", duration = "0:52" },
            };

            p.sms = new List<SmsThread>
            {
                new SmsThread
                {
                    sender = "1599-0000",
                    messages = new List<SmsMessage>
                    {
                        new SmsMessage { when = "Mon 22:47", text = "[Web발신] [Nuri Bank] New device registered: MacBook Air (Seoul). If this wasn't you, call 1599-0000." },
                        new SmsMessage { when = "Today 09:03", text = "[Web발신] [Nuri Bank] Card payment ₩4,500 · Mangwon Roasters · approved." },
                    },
                },
                new SmsThread
                {
                    sender = "Hangang Express",
                    messages = new List<SmsMessage>
                    {
                        new SmsMessage { when = "Today 08:30", text = "[Hangang Express] Your parcel (desk lamp bulbs) arrives tomorrow 16:00-18:00. Driver Kim Taesik 010-7715-5521." },
                    },
                },
                new SmsThread
                {
                    sender = "010-8812-4471",
                    messages = new List<SmsMessage>
                    {
                        new SmsMessage
                        {
                            when = "Sun 23:12", text = "[Web발신] [Nuri] Your account is restricted for security reasons. Verify within 24 hours:",
                            link = "nuri-secure.kr/verify",
                        },
                    },
                },
                new SmsThread
                {
                    sender = "Hangul Telecom",
                    messages = new List<SmsMessage>
                    {
                        new SmsMessage { when = "Thu 1 Oct", text = "[Hangul Telecom] Your September bill is ₩43,200, due 20 Oct." },
                    },
                },
            };

            p.chats = new List<ChatThread>
            {
                new ChatThread
                {
                    id = "family", title = "Our family", avatar = "photo_family", group = true,
                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { sender = "Mom", avatar = "pt_mom", when = "Yesterday 19:10", text = "Jiwoo-ya, did you eat dinner?" },
                        new ChatMessage { outgoing = true, when = "Yesterday 19:14", text = "Yes! Kimchi stew after my shift" },
                        new ChatMessage { sender = "Dad", avatar = "pt_dad", when = "Yesterday 20:02", photoCaption = "Grandma's persimmons in Jeonju" },
                        new ChatMessage { sender = "Minjun", avatar = "pt_minjun", when = "Yesterday 22:30", text = "I'm coming up to Seoul on Saturday for a concert. Noona, can I sleep over?" },
                        new ChatMessage { outgoing = true, when = "Yesterday 22:41", text = "Only if you bring snacks" },
                        new ChatMessage { sender = "Mom", avatar = "pt_mom", when = "Today 16:12", photoCaption = "Doenjang stew on the stove" },
                        new ChatMessage { sender = "Mom", avatar = "pt_mom", when = "Today 16:12", text = "Your dad cooked today! Come home for Mom's birthday on the 17th, okay?" },
                    },
                },
                new ChatThread
                {
                    id = "villa", title = "Mangwon Heights residents", avatar = "app_contacts", group = true,
                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { sender = "Office", avatar = "app_contacts", when = "Mon 09:00", text = "Reminder: gas safety inspection on Wed 14 Oct, 10:00-17:00. Inspectors never ask for money." },
                        new ChatMessage { sender = "201", avatar = "pt_unknown", when = "Mon 10:31", text = "Did anyone else get a call about a 'protected account'? Hung up on them." },
                        new ChatMessage { sender = "Landlord Choi", avatar = "pt_landlord", when = "Today 14:05", text = "Water tank cleaning on Thursday 10:00-12:00. Please keep some water aside." },
                    },
                },
                new ChatThread
                {
                    id = "yuna", title = "Yuna", avatar = "pt_yuna",
                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { sender = "Yuna", when = "Sun 18:55", text = "Tickets done!! Standing A-128 and A-129, 7 Nov" },
                        new ChatMessage { outgoing = true, when = "Sun 19:01", text = "I owe you 55,000 won, sending it on payday" },
                        new ChatMessage { sender = "Yuna", when = "Sun 19:02", text = "No rush :)" },
                    },
                },
                new ChatThread
                {
                    id = "cafe", title = "Mangwon Roasters crew", avatar = "pt_cafe", group = true,
                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { sender = "Manager Han", avatar = "pt_cafe", when = "Mon 21:00", text = "Wages go out on the 10th as usual. Can someone cover Saturday morning?" },
                    },
                },
            };

            p.bank = new BankData
            {
                bankName = "Nuri Bank",
                banks = new List<string> { "Nuri Bank", "Hanbit Bank", "Daehan Bank", "Mirae Savings", "K Post" },
                accounts = new List<BankAccount>
                {
                    new BankAccount { name = "Nuri Everyday", number = "110-302-558814", balance = 1284300 },
                    new BankAccount { name = "Tuition savings", number = "110-302-558990", balance = 3000000 },
                },
                transactions = new List<BankTransaction>
                {
                    new BankTransaction { when = "Today 09:03", counterparty = "Mangwon Roasters", memo = "Card", amount = -4500 },
                    new BankTransaction { when = "Mon 22:41", counterparty = "DAILY 24 Mangwon", memo = "Card", amount = -5850 },
                    new BankTransaction { when = "Mon 5 Oct", counterparty = "Hangang Mall", memo = "Desk lamp bulbs", amount = -12900 },
                    new BankTransaction { when = "Sat 3 Oct", counterparty = "PARK HYEJIN", memo = "Chuseok pocket money", amount = 100000 },
                    new BankTransaction { when = "Thu 1 Oct", counterparty = "MANGWON ROASTERS", memo = "September wages", amount = 486000 },
                    new BankTransaction { when = "Fri 25 Sep", counterparty = "CHOI YOUNGSIK", memo = "September rent", amount = -450000 },
                    new BankTransaction { when = "Sun 20 Sep", counterparty = "Hangul Telecom", memo = "Phone bill", amount = -43200 },
                },
                notices = new List<BankNotice>
                {
                    new BankNotice { when = "Mon 22:47", title = "New device registered", body = "MacBook Air · Seoul. If this wasn't you, call 1599-0000." },
                    new BankNotice { when = "Today 09:03", title = "Card payment ₩4,500", body = "Mangwon Roasters · approved" },
                },
            };

            p.parcels = new List<Parcel>
            {
                new Parcel
                {
                    item = "Desk lamp bulbs (2)", seller = "Hangang Mall", status = "Out for delivery tomorrow", tracking = "4410 2287 1934",
                    courier = "Hangang Express", driver = "Kim Taesik", driverPhone = "010-7715-5521", window = "Wed 16:00-18:00",
                },
                new Parcel
                {
                    item = "Phone case", seller = "AliStar (overseas)", status = "Delivered 21 Sep", tracking = "KR4410983312",
                    courier = "K Post", window = "", overseas = true,
                },
            };

            p.mails = new List<MailItem>
            {
                new MailItem
                {
                    from = "Hanbit University IT", fromAddress = "it-help@hanbit.ac.kr", subject = "Your new laptop is registered", when = "Mon 20:15",
                    body = "Hi Kim Jiwoo,\n\nYour MacBook Air is now registered for campus Wi-Fi. Remember to sign in to your apps (e.g. banking) on the new device.\n\nHanbit IT Help Desk",
                },
                new MailItem
                {
                    from = "Nuri Bank", fromAddress = "notice@nuribank.co.kr", subject = "A new device was registered", when = "Mon 22:47",
                    body = "A new device (MacBook Air) was registered to your Nuri Bank account in Seoul.\n\nIf this was you, you don't need to do anything. If not, call 1599-0000.\n\nNuri Bank will never ask you to move your money to 'protect' it.",
                },
                new MailItem
                {
                    from = "Daeyang Logistics careers", fromAddress = "careers@daeyang-logistics.co.kr", subject = "Application received: Winter internship", when = "Fri 2 Oct",
                    body = "Thank you for applying to the Daeyang Logistics winter internship. We will contact you from our HR office (02-555-0240) if you are invited to an interview.",
                },
                new MailItem
                {
                    from = "Hanbit University", fromAddress = "scholarship@hanbit.ac.kr", subject = "Scholarship interview on 16 Oct", when = "Wed 30 Sep", unread = true,
                    body = "Your scholarship interview is on Friday 16 October at 14:00, Student Centre room 204. Please bring your student ID.\n\nThe scholarship office never charges fees.",
                },
            };
            return p;
        }

        // ================================================================ world directory

        static WorldDirectory BuildDirectory()
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
                new DirAccount { bank = "Nuri Bank", number = "110-771-202358", holder = "CHOI HYUNWOO" },
                new DirAccount { bank = "Nuri Bank", number = "110-287-004411", holder = "PARK TAEHO" },
                new DirAccount { bank = "Hanbit Bank", number = "620-118-449027", holder = "CHOI HYUNWOO" },
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
                    aliases = new List<string>(),
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Your account has been restricted. Enter your card details to lift the restriction within 24 hours." },
                        new WebBlock { kind = WebBlockKind.Form, text = "Card number, expiry date and PIN", value = "Verify now" },
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
                new CallbackScript
                {
                    number = "1599-0000", answeredBy = "Nuri Bank customer centre", portrait = "pt_song",
                    lines = new List<string> { "Nuri Bank customer centre, Song Eunji speaking. How can I help?", "Your account looks normal. If anyone asks you to move money, hang up and call us." },
                },
                new CallbackScript
                {
                    number = "1332", answeredBy = "FSS fraud hotline", portrait = "pt_oh",
                    lines = new List<string> { "Financial Supervisory Service, fraud hotline.", "If someone asks you to move money to a 'safe account', it's a scam. Report the number to 112." },
                },
                new CallbackScript
                {
                    number = "112", answeredBy = "Police 112", portrait = "pt_oh",
                    lines = new List<string> { "112, police. What's happening?", "Tell us the number and the account. We can ask the bank to freeze a transfer if you call right away." },
                },
                new CallbackScript
                {
                    number = "010-2231-7745", answeredBy = "Mom", portrait = "pt_mom",
                    lines = new List<string> { "Jiwoo-ya! Did you eat?", "Your dad cooked doenjang stew today. Come home on the 17th, okay?" },
                },
                new CallbackScript
                {
                    number = "010-4418-0902", answeredBy = "Dad", portrait = "pt_dad",
                    lines = new List<string> { "Jiwoo! Everything okay?", "Call your mom, she misses you." },
                },
                new CallbackScript
                {
                    number = "02-555-0192", answeredBy = "Mangwon Heights office", portrait = "app_contacts",
                    lines = new List<string> { "Mangwon Heights office.", "The gas inspection is on the 14th. They won't ask for any money." },
                },
                new CallbackScript
                {
                    number = "070-8844-2019", answeredBy = "Manager Jeon", portrait = "pt_jeon",
                    lines = new List<string> { "Nuri Bank protection team… Miss Kim? Good, please go ahead with the transfer." },
                },
            };
            return d;
        }

        // ================================================================ room

        static RoomContent BuildRoom()
        {
            var r = ScriptableObject.CreateInstance<RoomContent>();
            r.newspaper = new NewspaperData
            {
                masthead = "SEOUL DAILY",
                issue = "No. 12,408",
                dateLine = "TUESDAY, OCTOBER 6, 2026",
                price = "1,000 won",
                headline = "Before you send, read the name",
                subhead = "Fake \"bank staff\" ask for transfers. Check whose account it is.",
                photo = Tex("news_photo"),
                caption = "The transfer screen shows who really gets the money. Photo: Seoul Daily",
                body = new List<string>
                {
                    "Callers claiming to be from a bank's \"account protection team\" are telling customers in Mapo and Seodaemun that a stranger has logged into their account, and that their savings must be moved to a \"protected account\" until a security reset.",
                    "The callers sound professional, know customers' names and sometimes the last digits of their account. They ask the customer to stay on the line and not to tell anyone.",
                    "\"There is no such thing as a protected account,\" a Nuri Bank spokesperson said. \"Every banking app shows the name of the account holder before a transfer goes through. If it's a person's name, stop.\" Customers who are unsure should hang up and call the number printed on their card.",
                },
                warningTitle = "WARNING: there is no \"safe account\"",
                warningText = "Banks, prosecutors and the Financial Supervisory Service never ask you to move money to a \"protected\" or \"safe\" account. If a caller does, hang up and call a number you looked up yourself.",
                local = new List<NewsItem>
                {
                    new NewsItem { title = "Gas inspections next week", text = "Mapo City Gas checks villas in Mangwon-dong from 12 Oct. Inspectors carry ID and never collect money." },
                    new NewsItem { title = "Water tank cleaning", text = "Several villas on Poeun-ro will be without water on Thursday morning." },
                    new NewsItem { title = "Subway fares", text = "The base fare stays at 1,550 won through the end of the year." },
                },
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
                        Field("Rent", "450,000 won a month, due on the 25th"),
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
                        new CalendarEntry { day = 8, text = "Dentist" },
                        new CalendarEntry { day = 14, text = "Gas check" },
                        new CalendarEntry { day = 17, text = "Mom's birthday" },
                        new CalendarEntry { day = 25, text = "Rent" },
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

        // ================================================================ M6: the "protected account" call

        static ConvLine Caller(string text, params Fact[] facts) => new ConvLine { speaker = Speaker.Caller, text = text, facts = new List<Fact>(facts) };

        static ConvNode Node(string id, string next, params ConvLine[] lines) => new ConvNode { id = id, next = next, lines = new List<ConvLine>(lines) };

        static ConvNode Decide(string id, ConvDecision d, params ConvLine[] lines) =>
            new ConvNode { id = id, hasDecision = true, decision = d, lines = new List<ConvLine>(lines) };

        static ConvDecision D(DecisionKind kind, string prompt, float seconds, string aLabel, string aNext, string bLabel, string bNext, params PressureLine[] pressure) =>
            new ConvDecision
            {
                kind = kind, prompt = prompt, patienceSeconds = seconds,
                a = new ConvOption { label = aLabel, playerLine = aLabel, next = aNext },
                b = new ConvOption { label = bLabel, playerLine = bLabel, next = bNext },
                pressure = new List<PressureLine>(pressure),
            };

        static PressureLine P(float at, string text) => new PressureLine { atPatience = at, text = text };

        static ConversationData BuildCall()
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = "M6 · Protected account (demo)";
            c.channel = Channel.Call;
            c.isScam = true;
            c.revealAtEnd = true;
            c.caller = new CallerInfo { displayName = "Manager Jeon", number = "070-8844-2019", inContacts = false, portrait = "pt_jeon" };
            c.claims = new List<string>
            {
                "He is Manager Jeon from Nuri Bank's \"account protection team\".",
                "Someone logged into my account from Busan twenty minutes ago.",
                "My money has to go to a \"protected account\" until tomorrow's reset.",
                "Protected account: Nuri Bank 110-900-551207.",
                "I should stay on the line and not tell anyone.",
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, "How do you answer?", 60f,
                      "Busan? I've been home all day. What happened?", "story",
                      "How do I know you're really from Nuri Bank?", "proof",
                      P(0.5f, "Miss Kim? Are you still there? This is urgent."),
                      P(0.25f, "Every minute we wait, they can take more.")),
                    Caller("Hello, is this Kim Jiwoo? This is Manager Jeon from Nuri Bank's account protection team."),
                    Caller("Twenty minutes ago someone logged into your account from an unregistered device in Busan.")),
                Node("story", "ask",
                    Caller("A fraud ring is emptying accounts today. We'll move your balance to a temporary protected account and return it after tomorrow's security reset.")),
                Node("proof", "ask",
                    Caller("I have your account ending in 8814 on my screen, Miss Kim."),
                    Caller("We're the protection team, not the call centre, so we call from our direct line.", F(FactKind.Phone, "070-8844-2019", "Caller"))),
                Decide("ask",
                    D(DecisionKind.Test, "What do you do?", 60f,
                      "I'll call the number on my card and call you back.", "push",
                      "Okay, I'm opening the bank app.", "final",
                      P(0.5f, "Miss Kim, are you opening the app?"),
                      P(0.25f, "If they empty your account, the bank can't refund you!")),
                    Caller("Please transfer 1,200,000 won to the protected account now: Nuri Bank 110-900-551207.",
                           F(FactKind.Account, "110-900-551207"), F(FactKind.Amount, "1,200,000")),
                    Caller("Stay on the line. This call is recorded for your protection.")),
                Node("push", "final",
                    Caller("No, no! If you hang up now, the ring will act in minutes. The call centre can't see protection cases.")),
                Decide("final",
                    D(DecisionKind.Commit, "The caller is waiting.", 45f,
                      "Wait, the app says the account belongs to someone called Jeong Miran.", "name",
                      "I'm not sending anything. Goodbye.", "refuse",
                      P(0.5f, "Miss Kim? The transfer?"),
                      P(0.25f, "This is your last chance to protect your savings!")),
                    Caller("Have you sent it yet? Tell me when it's done.")),
                Decide("name",
                    D(DecisionKind.Commit, "The caller is waiting.", 30f,
                      "A bank account in a person's name? No.", "refuse",
                      "Okay... I'll send it now.", "wait_transfer",
                      P(0.5f, "Please, Miss Kim. Hurry.")),
                    Caller("That's our agent who manages the protected accounts. It's normal. Please send it now.")),
                Decide("wait_transfer",
                    D(DecisionKind.Commit, "Send the transfer in Nuri Bank, or stop.", 60f,
                      "Done, I sent it.", "not_seen",
                      "Actually, no. I'm hanging up.", "refuse",
                      P(0.5f, "I'm still waiting, Miss Kim."),
                      P(0.25f, "Hurry, they're trying again right now!")),
                    Caller("Good. I'll stay on the line while you send it.")),
                Node("not_seen", "wait_transfer",
                    Caller("I don't see it yet. Open Nuri Bank, tap Transfer and send 1,200,000 won to 110-900-551207.",
                           F(FactKind.Account, "110-900-551207"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    lines = new List<string> { "Fine. Don't blame us when your account is empty." },
                    consequence = "You kept your money. The number 070-8844-2019 has seven reports on CheckFirst, and the \"protected account\" belongs to Jeong Miran.",
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse,
                    lines = new List<string> { "Hello? Miss Kim? ... Hello?" },
                    consequence = "The caller gave up. You lost nothing, but nobody reported the number.",
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -1200000,
                    lines = new List<string> { "Thank you, Miss Kim. Don't tell anyone until tomorrow's reset." },
                    consequence = "The 1,200,000 won went to Jeong Miran's account and was withdrawn within minutes. Nuri Bank has no \"protected accounts\".",
                },
                new ConvEnding
                {
                    id = "verify", verdict = Verdict.Verify,
                    lines = new List<string>
                    {
                        "Nuri Bank customer centre, Song Eunji speaking.",
                        "A protected account? No. We never move customers' money to protect it.",
                        "Your account shows no break-in. Please report that number to 112.",
                    },
                    consequence = "You called the number on your card. Nuri Bank confirmed there is no such thing as a protected account.",
                },
                new ConvEnding
                {
                    id = "verify_fss", verdict = Verdict.Verify,
                    lines = new List<string> { "Financial Supervisory Service, fraud hotline.", "A 'protected account'? That's a scam. Don't send anything, and report the number to 112." },
                    consequence = "You called the fraud hotline yourself. They recognised the script at once.",
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = "110-900-551207", endingId = "go_along" },
                new ActionTrigger { kind = ActionKind.Call, target = "1599-0000", endingId = "verify", answeredBy = "Nuri Bank customer centre", portrait = "pt_song" },
                new ActionTrigger { kind = ActionKind.Call, target = "1332", endingId = "verify_fss", answeredBy = "FSS fraud hotline", portrait = "pt_oh" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ E2: "Mom's broken phone" chat

        static ConversationData BuildChat()
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = "E2 · Mom's broken phone (demo)";
            c.channel = Channel.Chat;
            c.isScam = true;
            c.revealAtEnd = true;
            c.caller = new CallerInfo
            {
                displayName = "엄마 ♥", number = "", inContacts = false, portrait = "pt_mom", chatId = "mom_new", profileId = "mom_hyejin72",
            };
            c.claims = new List<string>
            {
                "It's Mom, writing from a new Talk profile.",
                "Her phone screen broke; she's on the repair shop's tablet.",
                "She needs 200,000 won sent to the shop owner's account.",
                "Account: Daehan Bank 3333-12-7788901.",
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, "Reply", 90f,
                      "Mom? Why a new profile? Call me.", "no_call",
                      "Oh no! Where do I send it?", "account"),
                    Caller("Jiwoo-ya, it's Mom. My phone screen broke so I'm using the repair shop's tablet."),
                    Caller("Can you do me a favour? I need to pay for the repair and a new phone deposit, 200,000 won.")),
                Node("no_call", "account",
                    Caller("I can't call, the tablet has no phone. Just send it quickly, the shop closes soon.")),
                Decide("account",
                    D(DecisionKind.Commit, "Reply", 90f,
                      "Let me check something first.", "stall",
                      "I'm not sending money to a stranger's account.", "refuse",
                      P(0.4f, "Jiwoo-ya? Are you there?")),
                    Caller("Send it to the shop owner's account, I'll pay you back tonight: Daehan Bank 3333-12-7788901.",
                           F(FactKind.Account, "3333-12-7788901")),
                    Caller("Please hurry, dear. They won't give me the phone until it's paid.")),
                Decide("stall",
                    D(DecisionKind.Commit, "Reply", 60f,
                      "Sending it now.", "wait",
                      "I'll call your real number first.", "refuse",
                      P(0.4f, "Jiwoo-ya, please! Before 5 o'clock!")),
                    Caller("What is there to check? It's me! Please, before 5 o'clock.")),
                Decide("wait",
                    D(DecisionKind.Commit, "Send it in Nuri Bank, or stop.", 90f,
                      "Done.", "not_seen",
                      "Actually, wait.", "stall"),
                    Caller("Thank you!! Tell me when it's done.")),
                Node("not_seen", "wait",
                    Caller("The shop says nothing came in yet. Please check and send it again.", F(FactKind.Account, "3333-12-7788901"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    lines = new List<string> { "..." },
                    consequence = "You didn't send anything. The \"엄마 ♥\" profile disappeared an hour later. Mom's usual profile had posted a photo of dinner eight minutes before the first message.",
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse,
                    lines = new List<string>(),
                    consequence = "You never answered. The profile gave up.",
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -200000,
                    lines = new List<string> { "Thank you dear!! ♥" },
                    consequence = "The 200,000 won went to Yoon Jaehee's account. Your real mom's phone was fine all along.",
                },
                new ConvEnding
                {
                    id = "verify", verdict = Verdict.Verify,
                    lines = new List<string> { "Jiwoo-ya! Did you eat?", "What? My phone is fine, I'm at home with your dad.", "Don't send anything! Block that profile." },
                    consequence = "You called Mom's saved number. Her phone was fine: the new profile was a scammer.",
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = "3333-12-7788901", endingId = "go_along" },
                new ActionTrigger { kind = ActionKind.Call, target = "010-2231-7745", endingId = "verify", answeredBy = "Mom", portrait = "pt_mom" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }
    }
}
