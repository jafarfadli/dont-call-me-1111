using System.Collections.Generic;
using System.Linq;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 3, Thursday 8 October (Hard): "Yoon Seora from Hangang Express's customs desk" says the
    /// monitor Jiwoo ordered from AliStar is held at Incheon for unpaid duty and VAT. She knows the
    /// item and the tracking number in both variants (last month's AliStar leak, in today's paper).
    /// <list type="bullet">
    /// <item>Legit: the Parcels app shows the duty and the same account, AliStar's shipping email says
    /// taxes were not included, the call comes from Hangang Express's official 1588-5520 and the
    /// account is a virtual account in the courier's name.</item>
    /// <item>Scam: the monitor has already cleared customs with taxes paid, the call comes from
    /// 1588-5502 (one digit off), the payment text is a web-sent text from that number, and the
    /// account is "KR CUSTOMS CLEARANCE". CheckFirst knows nothing about either (a new number).</item>
    /// </list>
    /// </summary>
    public static partial class ContentBuilder
    {
        const string Day3LegitNumber = "1588-5520";
        const string Day3ScamNumber = "1588-5502";
        const string Day3LegitAccount = "5620-44-018830";
        const string Day3ScamAccount = "620-557-301144";
        const long Duty = 56600;
        const string ShippedSubject = "Your order has shipped: 27-inch monitor";

        static DayData BuildDay3()
        {
            var legit = Day3Variant(false);
            var scam = Day3Variant(true);

            var d = ScriptableObject.CreateInstance<DayData>();
            d.day = 3;
            d.dateLabel = "Thursday, 8 October";
            d.shortLabel = "Day 3 · Thu 8 Oct";
            d.place = "Mangwon-dong, Seoul";
            d.startTime = "16:25";
            d.intro = "Midterms are next week. The water came back at noon, and the monitor you ordered should arrive any day now.";
            d.ringDelay = 6f;
            d.variants = new List<DayVariant> { scam, legit };
            d.echoes = Day3Echoes();
            d.nextDateLabel = "Friday, 9 October";
            d.builtWith = Version;
            return Store(d, Day3Dir + "/Day3.asset");
        }

        static DayVariant Day3Variant(bool scam)
        {
            SetDay(3, "16:25");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();
            string account = scam ? Day3ScamAccount : Day3LegitAccount;
            string bank = scam ? "Hanbit Bank" : "Nuri Bank";

            // ---- the paper on the desk (its front page is yesterday's case once Day 2 is played)
            var n = room.newspaper;
            var fallback = Day2ScamPapers().Find(p => p.outcome == Outcome.Refuse);
            n.headline = fallback.headline;
            n.subhead = fallback.subhead;
            n.body = new List<string>(fallback.body.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries));
            n.warningTitle = "PARCEL TEXTS: CHECK THE ADDRESS";
            n.warningText = "Fake \"unpaid duty\" texts link to look-alike sites. Don't tap the link: open your courier's own app or website yourself and see whether anything is due.";
            n.local = new List<NewsItem>
            {
                new NewsItem { title = "AliStar data leak", text = "AliStar says the names, phone numbers and recent orders of some Korean customers were exposed last month. No card details were taken, the company says." },
                new NewsItem { title = "Water tank cleaning today", text = "Villas on Poeun-ro are without water from 10:00 to 12:00." },
                new NewsItem { title = "Mangwon market by night", text = "Open until 23:00 on Saturday, with street food and live music." },
            };

            // ---- where the monitor really is
            var monitor = phone.parcels.Find(p => p.tracking == Tracking);
            if (scam)
            {
                monitor.status = "Customs cleared · out for delivery Fri 9 Oct";
                monitor.note = "Import taxes were paid by the seller at checkout. Nothing to pay on delivery.";
                monitor.window = "Fri 9 Oct, 14:00-18:00";
                AddSms(phone, "Hangang Express", 8, "10:02", "[Hangang Express] Your overseas parcel HX-5520-1183-KR has cleared customs (taxes paid by the seller). Delivery: Fri 9 Oct.");
            }
            else
            {
                monitor.status = "Held at Incheon customs · duty due";
                monitor.note = "Duty and VAT: ₩56,600. The seller didn't collect import taxes. Pay by 17:00 today to our virtual account (HANGANG EXPRESS (CUSTOMS)), or the parcel goes back to the seller.";
                monitor.account = Day3LegitAccount;
                AddSms(phone, "Hangang Express", 8, "10:02", "[Hangang Express] Your overseas parcel HX-5520-1183-KR is held at customs: duty and VAT ₩56,600 due. Details in the Parcels app. Our customs desk may call you from 1588-5520.");
            }
            var shipped = new MailItem
            {
                from = "AliStar", fromAddress = "orders@alistar.com", subject = ShippedSubject, when = Oct(7, "21:05"),
                body = scam
                    ? "Your 27-inch IPS monitor is on its way with Hangang Express (HX-5520-1183-KR).\n\nImport taxes: included. We collected duty and VAT at checkout, so there's nothing more to pay on arrival."
                    : "Your 27-inch IPS monitor is on its way with Hangang Express (HX-5520-1183-KR).\n\nImport taxes: not included. Orders over US$150 may be charged duty and VAT on arrival in Korea, collected by the courier.",
            };
            phone.mails.Insert(0, shipped);

            // ---- the courier's site, and who owns the numbers and accounts
            directory.pages.Add(new WebPage
            {
                url = "hangangexpress.co.kr/customs", title = "Hangang Express: customs and duty", site = "Hangang Express", official = true,
                aliases = new List<string> { "hangang express", "hangang", "한강익스프레스", "courier", "customs", "duty", "parcel", "tracking", "track" },
                blocks = new List<WebBlock>
                {
                    new WebBlock { kind = WebBlockKind.Contact, text = "Customer centre and customs desk", value = Day3LegitNumber },
                    new WebBlock { kind = WebBlockKind.Notice, text = "If duty is due on an overseas parcel, we text you and it shows in the Parcels app. Our customs desk may call you from 1588-5520." },
                    new WebBlock { kind = WebBlockKind.Warning, text = "Pay duty only in the Parcels app or to a virtual account in the name HANGANG EXPRESS (CUSTOMS). We never ask you to pay any other account or through a link from another number." },
                    new WebBlock { kind = WebBlockKind.Lookup, text = "Track a parcel", value = "parcel" },
                },
                lookup = new List<LookupEntry>
                {
                    new LookupEntry
                    {
                        key = Tracking,
                        result = scam
                            ? "HX-5520-1183-KR · 27-inch monitor · Customs cleared 7 Oct, import taxes paid by the seller · Out for delivery Fri 9 Oct"
                            : "HX-5520-1183-KR · 27-inch monitor · Held at Incheon customs since 8 Oct · Duty and VAT ₩56,600 due · Pay to Nuri Bank 5620-44-018830 (HANGANG EXPRESS (CUSTOMS))",
                    },
                    new LookupEntry { key = "4410 2287 1934", result = "4410 2287 1934 · Desk lamp bulbs · Delivered Wed 7 Oct, 17:32" },
                },
            });
            if (scam)
            {
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = "KR CUSTOMS CLEARANCE" });
                directory.numbers.Add(new DirNumber { number = Day3ScamNumber, owner = "Unknown" });
                directory.pages.Add(new WebPage
                {
                    url = "hangang-customs.kr/pay", title = "Hangang Express · customs payment", site = "hangang-customs.kr", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = "Parcel HX-5520-1183-KR: duty and VAT ₩56,600 unpaid. Pay by card before 17:00 or the parcel is returned." },
                        new WebBlock { kind = WebBlockKind.Form, text = "Card number, expiry date and CVC", value = "Pay ₩56,600" },
                    },
                });
            }
            else
            {
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = "HANGANG EXPRESS (CUSTOMS)" });
            }

            var v = StoreVariant(scam ? "scam" : "legit", Day3Dir, scam ? "Day3_Scam" : "Day3_Legit", Day3Call(scam), phone, room, directory);
            var officialNumber = new[]
            {
                new ClueWhen(ClueEvent.PageOpened, "hangangexpress.co.kr/customs"), new ClueWhen(ClueEvent.DocumentViewed, "Delivery slip"),
                new ClueWhen(ClueEvent.NumberChecked, scam ? Day3ScamNumber : Day3LegitNumber),
            };
            v.clues = scam
                ? new List<ClueDef>
                {
                    Clue("parcel_cleared", "The Parcels app shows the monitor already cleared customs, its taxes paid.", "Parcels → 27-inch monitor",
                         new ClueWhen(ClueEvent.ParcelOpened, Tracking), new ClueWhen(ClueEvent.PageOpened, "hangangexpress.co.kr/customs")),
                    Clue("taxes_included", "AliStar's shipping email says the import taxes were included.", "Mail → \"Your order has shipped\"",
                         new ClueWhen(ClueEvent.MailRead, ShippedSubject)),
                    Clue("number_off", "Hangang Express's number is 1588-5520. The caller's 1588-5502 is one digit off.", "Hangang Express's site, or the delivery slip in the drawer",
                         officialNumber),
                    Clue("account_name", "The account belongs to \"KR CUSTOMS CLEARANCE\", not to Hangang Express.", "Bank app → Transfer → the recipient check",
                         new ClueWhen(ClueEvent.RecipientShown, account), new ClueWhen(ClueEvent.NumberChecked, account)),
                    Clue("web_text", "The payment text came from 1588-5502 as a web-sent text, not from Hangang Express.", "Messages → 1588-5502",
                         new ClueWhen(ClueEvent.MessageRead, Day3ScamNumber)),
                }
                : new List<ClueDef>
                {
                    Clue("parcel_held", "The Parcels app shows the monitor held at customs, with ₩56,600 due and the same account.", "Parcels → 27-inch monitor",
                         new ClueWhen(ClueEvent.ParcelOpened, Tracking), new ClueWhen(ClueEvent.PageOpened, "hangangexpress.co.kr/customs")),
                    Clue("taxes_not_included", "AliStar's shipping email says the import taxes were not included.", "Mail → \"Your order has shipped\"",
                         new ClueWhen(ClueEvent.MailRead, ShippedSubject)),
                    Clue("official_number", "1588-5520, the number calling you, is Hangang Express's official number.", "Hangang Express's site, or the delivery slip in the drawer",
                         officialNumber),
                    Clue("account_name", "The account is a virtual account in the name HANGANG EXPRESS (CUSTOMS).", "Bank app → Transfer → the recipient check",
                         new ClueWhen(ClueEvent.RecipientShown, account), new ClueWhen(ClueEvent.NumberChecked, account)),
                };
            v.papers = scam ? Day3ScamPapers() : Day3LegitPapers();
            v.ruleTitle = scam ? "Is anything due? Ask the app" : "Pay duty only to the courier";
            v.rule = scam
                ? "Courier or customs asking for money? Open the courier's own app or website yourself. If nothing is due there, nothing is due."
                : "Real duty shows up in the courier's own app, with an account in the courier's name. When they match the call, paying is safe.";
            v.ruleSource = "Seoul Daily, 9 Oct";
            return v;
        }

        // ================================================================ the call

        static ConversationData Day3Call(bool scam)
        {
            string number = scam ? Day3ScamNumber : Day3LegitNumber;
            string account = scam ? Day3ScamAccount : Day3LegitAccount;
            string bank = scam ? "Hanbit Bank" : "Nuri Bank";
            string accountSpoken = scam ? "six two zero, five five seven, three zero one one four four" : "five six two zero, four four, zero one eight eight three zero";

            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? "Day 3 · The held parcel (M2, scam)" : "Day 3 · The held parcel (legit)";
            c.channel = Channel.Call;
            c.isScam = scam;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = "Yoon Seora", number = number, inContacts = false, portrait = "pt_hr", voice = VoiceCustoms };
            c.claims = new List<string>
            {
                "She is Yoon Seora from Hangang Express's customs clearance desk.",
                "My AliStar monitor (HX-5520-1183-KR) is held at Incheon customs.",
                "Duty and VAT of ₩56,600 haven't been paid.",
                $"Pay into {bank} {account} before 17:00, or it goes back to the seller.",
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, "How do you answer?", 40f,
                      "Yes, that's mine. Is something wrong?", "problem",
                      "How do you know what I ordered?", "know",
                      P(0.5f, "Miss Kim? Are you there?")),
                    Caller("Hello, is this Kim Jiwoo? This is Yoon Seora from Hangang Express, the customs clearance desk."),
                    Caller($"I'm calling about your parcel from AliStar: the 27-inch monitor, tracking number {Tracking}.",
                           $"I'm calling about your parcel from AliStar. The twenty seven inch monitor, tracking number {TrackingSpoken}.",
                           F(FactKind.Case, Tracking, "Tracking"))),
                Node("problem", "ask",
                    Caller("It's held at Incheon customs. The import duty and VAT haven't been paid.")),
                Node("know", "ask",
                    Caller("It's on the customs declaration, Miss Kim. We see every item that comes through."),
                    Caller("It's held at Incheon customs because the import duty and VAT haven't been paid.")),
                Hold("ask",
                    Caller("The total is ₩56,600. Once it's paid, the monitor is released and delivered tomorrow.",
                           "The total is fifty six thousand, six hundred won. Once it's paid, the monitor is released and delivered tomorrow.",
                           F(FactKind.Amount, "56,600")),
                    Caller($"Please pay it into our customs account: {bank} {account}.",
                           $"Please pay it into our customs account. {bank}, {accountSpoken}.",
                           F(FactKind.Account, account)),
                    Caller("The desk closes at five. After that the parcel goes back to the seller, and you pay the return shipping."),
                    Caller("I'll stay on the line while you pay.")),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = "The held parcel",
                claimedIdentity = "Yoon Seora, Hangang Express's customs desk",
                ask = $"Pay ₩56,600 duty and VAT into {bank} {account}",
                deadline = "17:00",
                deadlineReason = "before the customs desk closes",
                objective = "Is your monitor really held at customs, and is this really Hangang Express? Check before 17:00, then give your verdict on the call: pay the duty, or hang up.",
            };
            c.verdict = new VerdictInfo
            {
                goAlong = "Pay the duty",
                refuse = "Hang up",
                refuseDetail = "Say no and end the call",
                goAlongLine = "Okay, I'm paying it now.",
                refuseLine = "I'm not paying anything over the phone. Goodbye.",
            };
            c.questions = new List<ConvQuestion>
            {
                Q("Ask whose name is on the account", "Whose name is on that account?", null,
                  scam
                      ? Caller("KR Customs Clearance. We handle customs for Hangang Express.")
                      : Caller("It's a virtual account in the name Hangang Express, customs. It's in your Parcels app too.")),
                Q("Ask why you owe duty", "Why do I have to pay duty?", null,
                  Caller("Orders over a hundred and fifty dollars pay duty and VAT when they arrive, unless the seller collected it.")),
                Q("Ask if you can pay in the Parcels app", "Can I pay in the Parcels app instead?", null,
                  scam
                      ? Caller("The app doesn't show customs payments, Miss Kim. It has to be a transfer.")
                      : Caller("Of course. The same account is under your parcel in the app. Either way works.")),
                Q("Ask for a number to call back", "What number can I call you back on?", null,
                  scam
                      ? Caller("This line: 1588-5502. Ask for the customs desk.", "This line. One five eight eight, five five zero two. Ask for the customs desk.",
                               F(FactKind.Phone, Day3ScamNumber, "Call back"))
                      : Caller("Our customer centre, 1588-5520. It's the number I'm calling from. Ask for the customs desk.",
                               "Our customer centre, one five eight eight, five five two zero. It's the number I'm calling from. Ask for the customs desk.",
                               F(FactKind.Phone, Day3LegitNumber, "Call back"))),
            };
            var text = scam
                ? WithSms(Caller("I've texted you the payment details, so you have them in writing."), Day3ScamNumber,
                          "[Web발신] [Hangang Express] Customs duty ₩56,600 unpaid for HX-5520-1183-KR. Pay by 17:00: Hanbit Bank 620-557-301144 or hangang-customs.kr/pay",
                          "hangang-customs.kr/pay")
                : WithSms(Caller("I've texted you the payment details, so you have them in writing."), "Hangang Express",
                          "[Hangang Express] Customs: duty and VAT ₩56,600 due for HX-5520-1183-KR. Pay by 17:00 in the app or to virtual account Nuri Bank 5620-44-018830 (HANGANG EXPRESS (CUSTOMS)).");
            c.beats = new List<PressureBeat>
            {
                Beat("16:39", text),
                Beat("16:46", Caller("Miss Kim? The customs desk is asking whether the payment is on its way.")),
                Beat("16:52", Caller("Eight minutes before the desk closes.")),
                Beat("16:57", Caller("Three minutes. After five I can't stop the return.")),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? "You hung up. Your monitor had cleared customs with its taxes paid, and 1588-5502 is one digit off Hangang Express's real number."
                        : "The duty wasn't paid by five. The monitor went back to Incheon, and the return shipping comes out of your refund.",
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller("Miss Kim? Fine. Your parcel will be returned.") }
                        : new List<ConvLine> { Caller("Miss Kim? The desk has closed. I'll have to mark it for return.") },
                    consequence = scam
                        ? "The caller gave up at five. Your monitor arrived on Friday anyway."
                        : "Five o'clock passed. The monitor went back to Incheon.",
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -Duty,
                    lines = scam
                        ? new List<ConvLine> { Caller("Thank you. Oh, and there's a storage fee as well. I'll call you back about it.") }
                        : new List<ConvLine> { Caller("Payment received. Your monitor will be delivered tomorrow afternoon. Thank you, Miss Kim.") },
                    consequence = scam
                        ? "The ₩56,600 went to \"KR Customs Clearance\", an account opened last week. Your monitor, its taxes paid by the seller, arrived on Friday anyway."
                        : "The duty went to Hangang Express's customs account. The monitor arrived on Friday.",
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = account, bank = bank, amount = Duty, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> Day3ScamPapers()
        {
            const string lead = "Callers posing as Hangang Express's \"customs clearance desk\" phoned AliStar customers in Mapo yesterday. They read out each customer's order and tracking number and asked for \"unpaid duty\" to be paid into an account before 5 p.m.";
            const string facts = "The details came from last month's AliStar data leak. The parcels had already cleared customs with their taxes paid, and the calls came from 1588-5502, one digit off Hangang Express's real number, 1588-5520.";
            const string courier = "\"If duty is due, you'll see it in our app, with an account in our name,\" a Hangang Express spokesperson said. \"We never ask you to pay anyone else.\"";
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, "Fake customs desk used leaked AliStar orders",
                      "The callers knew the item and the tracking number. The parcels had already cleared customs.",
                      lead + "\n\n" + facts + "\n\n" + courier,
                      "You hung up. The parcel had cleared customs, the taxes were included, and the number was one digit off."),
                Paper(Outcome.GoAlong, "56,600 won of \"duty\" on a parcel that was already paid for",
                      "Scammers with leaked AliStar orders posed as a courier's customs desk.",
                      lead + "\n\nA Mangwon student paid 56,600 won into an account in the name \"KR Customs Clearance\", opened a week ago. Her monitor had cleared customs on Wednesday, its taxes paid by the seller at checkout. It arrived this morning.\n\n" + courier,
                      "The Parcels app and the shipping email both said the taxes were paid, and the account wasn't in Hangang Express's name."),
                Paper(Outcome.Timeout, "Customs desk calling? Check the app",
                      "Scammers with leaked orders called AliStar customers yesterday.",
                      lead + "\n\n" + facts + "\n\n" + courier,
                      "Nothing was sent. The Parcels app would have told you the monitor had already cleared customs."),
            };
        }

        static List<EndPaper> Day3LegitPapers()
        {
            const string lead = "With cheap overseas shopping booming, Hangang Express says it now holds hundreds of parcels a day at Incheon for unpaid duty and VAT, usually because the seller didn't collect the taxes at checkout.";
            const string desk = "\"When duty is due, we text you, show it in the app and call from 1588-5520,\" said Yoon Seora of the courier's customs desk. \"The account is always a virtual account in our name.\"";
            const string rule = "Personal orders up to US$150 are duty-free. Above that, duty and VAT are due on arrival unless the seller collected them.";
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, "Shopping abroad: when the duty is real",
                      "Couriers say more overseas orders are held for duty, and that customers are right to check before paying.",
                      lead + "\n\n" + desk + " \"Customers who check that first are doing exactly the right thing.\"\n\n" + rule,
                      "The duty was real, and you checked: the app, the email, the official number and the account name all matched."),
                Paper(Outcome.Refuse, "Real duty notices ignored after a wave of scams",
                      "Parcels pile up at Incheon as customers hang up on real customs calls.",
                      lead + " After weeks of fake customs texts, many customers now hang up on the real thing, and dozens of parcels were sent back yesterday.\n\n" + desk + " \"Check it, by all means. But check the app, not just your nerves.\"\n\n" + rule,
                      "This one was real: the Parcels app showed the same duty and account, and the call came from Hangang Express's official number."),
                Paper(Outcome.Timeout, "Parcels go back as duty goes unpaid",
                      "Customers who never decided lost their orders to the return queue.",
                      lead + "\n\n" + desk + "\n\n" + rule,
                      "The duty was real, and the evidence was in the app and the email. Deciding in time matters too."),
            };
        }

        // ================================================================ what Day 2 left behind

        static List<DayEcho> Day3Echoes()
        {
            SetDay(3, "16:25");
            const string warning = "Everyone: someone is calling tenants pretending to be my son. I am NOT in hospital, and the rent account has NOT changed: Nuri Bank 110-771-209944, as always. – Choi, 1F";

            DayEcho RentPaid(Truth truth, OutcomeMask outcomes, string to, string when, string memo)
            {
                var e = Echo(2, truth, outcomes, EchoKind.BankTransaction);
                e.from = to;
                e.when = when;
                e.title = memo;
                e.amount = -Rent;
                return e;
            }

            var hyunwoo = Echo(2, Truth.Legit, Any, EchoKind.Contact);
            hyunwoo.from = Day2LegitNumber;
            hyunwoo.sender = "Hyunwoo (landlord's son)";
            hyunwoo.avatar = "pt_hyunwoo";
            hyunwoo.text = "Looks after the building while Mr. Choi recovers";

            return new List<DayEcho>
            {
                // The "son" was a scammer: the landlord warns everyone; the real rent still has to be paid.
                EchoCard(2, Truth.Scam, Sent, "Mr. Choi never asked for a new account. You paid him the real rent this morning, again."),
                EchoCard(2, Truth.Scam, HungUp, "The \"landlord's son\" was a fake. Mr. Choi thanked the tenants who hung up."),
                EchoCard(2, Truth.Scam, TooLate, "The \"landlord's son\" was a fake. Mr. Choi warned everyone in the chat last night."),
                EchoChat(2, Truth.Scam, Any, "villa", "Landlord Choi", "pt_landlord", Oct(7, "19:02"), warning, true),
                EchoChat(2, Truth.Scam, HungUp, "villa", "Landlord Choi", "pt_landlord", Oct(7, "19:05"), "302 hung up on him. Well done! 👍"),
                EchoSms(2, Truth.Scam, Sent, "010-5512-3380", Oct(7, "19:10"), "Jiwoo, I heard what happened. I'm so sorry. Please report it to the police. Take until the end of the month for the rent."),
                RentPaid(Truth.Scam, Sent, "CHOI YOUNGSIK", Oct(8, "09:20"), "October rent"),
                RentPaid(Truth.Scam, Kept, "CHOI YOUNGSIK", Oct(7, "19:30"), "October rent"),
                EchoNote(2, Truth.Scam, Sent, "Sticky note", "Rent account changes ONLY in writing. Check the lease!", "board_sticky_y", new Vector2(0.41f, 0.77f), 5f),
                EchoNote(2, Truth.Scam, HungUp, "Sticky note", "Fake \"son\": 010-4127-8830. Hung up ✓", "board_sticky_p", new Vector2(0.41f, 0.77f), -4f),
                EchoNote(2, Truth.Scam, TooLate, "Sticky note", "Fake \"son\": 010-4127-8830. Next time, decide sooner.", "board_sticky_p", new Vector2(0.41f, 0.77f), -4f),

                // The son was real: he thanks, or asks again.
                EchoCard(2, Truth.Legit, Sent, "The rent reached Hyunwoo. Mr. Choi is recovering in hospital."),
                EchoCard(2, Truth.Legit, Kept, "The \"landlord's son\" was real. You sent him the rent this morning, a day late."),
                hyunwoo,
                EchoSms(2, Truth.Legit, Sent, Day2LegitNumber, Oct(7, "18:02"), "Thanks for the rent, Jiwoo-ssi! Dad says hi from the ward 😊 – Hyunwoo", true),
                EchoSms(2, Truth.Legit, Kept, Day2LegitNumber, Oct(7, "17:20"), "Hi Jiwoo-ssi, it's Hyunwoo from the first floor. I understand you were careful. Dad's note is on your board and in the chat. Please send the rent when you can.", true),
                RentPaid(Truth.Legit, Kept, "CHOI HYUNWOO", Oct(8, "09:20"), "October rent (late)"),
                EchoChat(2, Truth.Legit, Any, "villa", "Landlord Choi", "pt_landlord", Oct(8, "10:15"), "Thank you all for the get-well messages. The hospital food is terrible 😅"),
                EchoNote(2, Truth.Legit, Any, "Sticky note", "Mr. Choi in hospital till next week. Rent goes to Hyunwoo (it's real!)", "board_sticky_y", new Vector2(0.41f, 0.77f), 3f),
            }.Concat(Day1Notes()).ToList();
        }
    }
}
