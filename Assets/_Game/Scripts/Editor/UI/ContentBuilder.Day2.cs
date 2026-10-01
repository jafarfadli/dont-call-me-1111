using System.Collections.Generic;
using System.Linq;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 2, Wednesday 7 October, rent day: "Choi Hyunwoo, the landlord's son" says his father went
    /// into hospital this morning and asks for the rent on a new account. Twins M5 (legit) and H3
    /// (scam) from the plan, reworked so both share the caller, the story and the ask:
    /// <list type="bullet">
    /// <item>Legit: the caller's number is the son's in the lease, the account is in his name, and the
    /// landlord posted the change in the residents' chat and left a written note (pinned on the board).</item>
    /// <item>Scam: another number, an account in a stranger's name, no note, and the landlord posting in
    /// the chat from home all afternoon.</item>
    /// </list>
    /// </summary>
    public static partial class ContentBuilder
    {
        const string Day2LegitNumber = "010-2280-6614";
        const string Day2ScamNumber = "010-4127-8830";
        const string Day2LegitAccount = "110-771-202358";
        const string Day2ScamAccount = "620-118-449027";
        const long Rent = 450000;

        static DayData BuildDay2()
        {
            var legit = Day2Variant(false);
            var scam = Day2Variant(true);

            var d = ScriptableObject.CreateInstance<DayData>();
            d.day = 2;
            d.dateLabel = "Wednesday, 7 October";
            d.shortLabel = "Day 2 · Wed 7 Oct";
            d.place = "Mangwon-dong, Seoul";
            d.startTime = "16:20";
            d.intro = "Rent day. You meant to send it to Mr. Choi after class, like every month.";
            d.ringDelay = 7f;
            d.variants = new List<DayVariant> { scam, legit };
            d.echoes = Day2Echoes();
            d.nextDateLabel = "Thursday, 8 October";
            d.builtWith = Version;
            return Store(d, Day2Dir + "/Day2.asset");
        }

        static DayVariant Day2Variant(bool scam)
        {
            SetDay(2, "16:20");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();
            string account = scam ? Day2ScamAccount : Day2LegitAccount;
            string bank = scam ? "Hanbit Bank" : "Nuri Bank";

            // ---- the paper on the desk (its front page is yesterday's case once Day 1 is played)
            var n = room.newspaper;
            var fallback = Day1Papers().Find(p => p.outcome == Outcome.Refuse);
            n.headline = fallback.headline;
            n.subhead = fallback.subhead;
            n.body = new List<string>(fallback.body.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries));
            n.warningTitle = "RENT DAY: CHECK THE NEW ACCOUNT";
            n.warningText = "Tenants across Mapo report calls and texts about a \"new rent account\" around rent day. Your lease says how the account can change. Check it, and check that the new account is in the name of your landlord or someone your lease names.";
            n.local = new List<NewsItem>
            {
                new NewsItem { title = "Water tank cleaning tomorrow", text = "Villas on Poeun-ro will be without water on Thursday, 10:00-12:00." },
                new NewsItem { title = "Mangwon market by night", text = "The market stays open until 23:00 this Saturday, with street food and live music." },
                new NewsItem { title = "Midterms", text = "Hanbit University's library opens around the clock from Friday." },
            };

            // ---- the truth, in the chat, on the board and in the directory
            if (scam)
            {
                AddChat(phone, "villa", 7, "13:40", "Landlord Choi", "pt_landlord",
                        "Thank you all for keeping the stairs so clean 👍 See you at the water tank cleaning tomorrow morning. – Choi, 1F");
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = "SEO JIYEON" });
                directory.numbers.Add(new DirNumber
                {
                    number = Day2ScamNumber, owner = "Unknown",
                    reports = new List<string>
                    {
                        "5 Oct · \"Said he was my landlord's son and asked for the rent on a new account.\"",
                        "29 Sep · \"Fake landlord's son, Seodaemun. Knew my unit number.\"",
                    },
                });
            }
            else
            {
                AddChat(phone, "villa", 7, "08:10", "Landlord Choi", "pt_landlord",
                        "Good morning everyone. I'm going into Severance Hospital today for my knee, back next week. My son Hyunwoo will look after the building and collect this month's rent: Nuri Bank 110-771-202358 (Choi Hyunwoo). He'll call you today. Thank you! – Choi, 1F");
                AddChat(phone, "villa", 7, "08:42", "201", "pt_unknown", "Get well soon, Mr. Choi!");
                room.board.Add(new BoardItem
                {
                    title = "Note from the landlord", kind = BoardItemKind.Notice, image = Tex("board_note_blank"),
                    position = new Vector2(0.06f, 0.6f), width = 0.17f, rotation = 2.5f,
                    handwriting = "To all tenants,\nI'm in hospital this week (knee). My son Hyunwoo will collect October's rent:\nNuri Bank 110-771-202358\n(Choi Hyunwoo)\n\n– Choi Youngsik, 1F",
                    details = new List<DocField>
                    {
                        Field("From", "Choi Youngsik, 1F · 7 Oct"),
                        Field("Note", "I'm in hospital this week. My son will look after the building and call you about the rent."),
                        Field("Rent account (October)", "Nuri Bank 110-771-202358 (Choi Hyunwoo)", FactKind.Account),
                        Field("My son", "Choi Hyunwoo " + Day2LegitNumber, FactKind.Phone),
                    },
                });
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = "CHOI HYUNWOO" });
            }

            var v = StoreVariant(scam ? "scam" : "legit", Day2Dir, scam ? "Day2_Scam" : "Day2_Legit", Day2Call(scam), phone, room, directory);
            v.clues = scam
                ? new List<ClueDef>
                {
                    Clue("chat_home", "Mr. Choi was posting in the residents' chat this afternoon: no hospital, no new account.", "Talk → Mangwon Heights residents",
                         new ClueWhen(ClueEvent.ChatRead, "villa")),
                    Clue("lease", "Your lease gives the son's number as 010-2280-6614, not the caller's, and says the account changes only in writing.", "Desk drawer → Lease contract",
                         new ClueWhen(ClueEvent.DocumentViewed, "Lease contract · Unit 302")),
                    Clue("name_wrong", "The \"new rent account\" is in a stranger's name: SEO JIYEON.", "Bank app → Transfer → the recipient check",
                         new ClueWhen(ClueEvent.RecipientShown, account), new ClueWhen(ClueEvent.NumberChecked, account)),
                    Clue("caller_reports", "CheckFirst has two reports on 010-4127-8830: a fake \"landlord's son\".", "CheckFirst → look up the caller's number",
                         new ClueWhen(ClueEvent.NumberChecked, Day2ScamNumber)),
                    Clue("paper_warning", "Today's paper warns about \"new rent account\" calls on rent day.", "The Seoul Daily on the desk",
                         new ClueWhen(ClueEvent.PanelOpened, "Newspaper")),
                }
                : new List<ClueDef>
                {
                    Clue("chat_notice", "Mr. Choi posted in the residents' chat that he's in hospital and that Hyunwoo collects this month's rent.", "Talk → Mangwon Heights residents",
                         new ClueWhen(ClueEvent.ChatRead, "villa")),
                    Clue("lease", "The caller's number, 010-2280-6614, is the son's number in your lease.", "Desk drawer → Lease contract (\"If absent, contact\")",
                         new ClueWhen(ClueEvent.DocumentViewed, "Lease contract · Unit 302")),
                    Clue("name_matches", "The new account is in Choi Hyunwoo's name, the son your lease names.", "Bank app → Transfer → the recipient check",
                         new ClueWhen(ClueEvent.RecipientShown, account), new ClueWhen(ClueEvent.NumberChecked, account)),
                    Clue("written_notice", "The landlord's written notice with the new account is pinned on your board, as the lease requires.", "Cork board → Note from the landlord",
                         new ClueWhen(ClueEvent.BoardItemOpened, "Note from the landlord")),
                };
            v.papers = scam ? Day2ScamPapers() : Day2LegitPapers();
            v.ruleTitle = scam ? "A new rent account? Check the lease" : "Checking works both ways";
            v.rule = scam
                ? "A rent account changes only the way your lease says. Check the caller's number and the account holder's name against the lease before you send."
                : "When the number in your lease, the landlord's written notice and the name on the account all match, the change is real. Check, then act.";
            v.ruleSource = "Seoul Daily, 8 Oct";
            return v;
        }

        // ================================================================ the call

        static ConversationData Day2Call(bool scam)
        {
            string number = scam ? Day2ScamNumber : Day2LegitNumber;
            string account = scam ? Day2ScamAccount : Day2LegitAccount;
            string bank = scam ? "Hanbit Bank" : "Nuri Bank";
            string accountSpoken = scam ? "six two zero, one one eight, four four nine zero two seven" : "one one zero, seven seven one, two zero two three five eight";

            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? "Day 2 · The new rent account (H3, scam)" : "Day 2 · The new rent account (M5, legit)";
            c.channel = Channel.Call;
            c.isScam = scam;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = "Choi Hyunwoo", number = number, inContacts = false, portrait = "pt_hyunwoo", voice = VoiceHyunwoo };
            c.claims = new List<string>
            {
                "He is Choi Hyunwoo, the landlord's son.",
                "Mr. Choi went into hospital this morning (his knee).",
                $"This month the rent goes to his account: {bank} {account}.",
                "He needs it before 17:00 to pay the hospital bill.",
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, "How do you answer?", 45f,
                      "Oh no. Is he okay?", "okay",
                      "Sorry, who is this again?", "who",
                      P(0.5f, "Jiwoo-ssi? Can you hear me?")),
                    Caller("Hello, is this Kim Jiwoo in 302? This is Choi Hyunwoo, Mr. Choi's son, from the first floor.",
                           "Hello, is this Kim Jiwoo in three oh two? This is Choi Hyunwoo, Mr. Choi's son, from the first floor."),
                    Caller("Sorry to call out of the blue. My father went into hospital this morning. It's his knee. Nothing serious, but he'll be there all week.")),
                Node("okay", "ask",
                    Caller("He's fine, thanks. Grumpy about the hospital food.")),
                Node("who", "ask",
                    Caller("Choi Hyunwoo, the landlord's son. I'm on your lease, under 'if absent'.", "Choi Hyunwoo, the landlord's son. I'm on your lease, under if absent.")),
                Hold("ask",
                    Caller("So I'm looking after the building this week, and today's the seventh. Rent day."),
                    Caller("Dad can't use his banking app from the hospital, so this month the rent comes to me."),
                    Caller($"Please send the ₩450,000 to my account: {bank} {account}.",
                           $"Please send the four hundred and fifty thousand won to my account. {bank}, {accountSpoken}.",
                           F(FactKind.Account, account), F(FactKind.Amount, "450,000")),
                    Caller("I'm paying his hospital bill with the rents, and the billing desk closes at five. Could you send it before then? I'll hold.")),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = "The new rent account",
                claimedIdentity = "Choi Hyunwoo, the landlord's son",
                ask = $"Pay this month's rent, ₩450,000, into a new account: {bank} {account}",
                deadline = "17:00",
                deadlineReason = "before the hospital's billing desk closes",
                objective = "Is he really the landlord's son, and did the rent account really change? Check before 17:00, then give your verdict on the call: pay the rent, or hang up.",
            };
            c.verdict = new VerdictInfo
            {
                goAlong = "Pay the rent",
                refuse = "Hang up",
                refuseDetail = "Say no and end the call",
                goAlongLine = "Okay, I'm sending the rent now.",
                refuseLine = "I'm not paying rent into a new account over the phone. Goodbye.",
            };
            c.questions = new List<ConvQuestion>
            {
                Q("Ask whose name is on the account", "Whose name is on that account?", null,
                  scam
                      ? Caller("My wife's, Seo Jiyeon. Mine's a business account, it takes a day to clear.")
                      : Caller("Mine. Choi Hyunwoo. Same as on your lease.")),
                Q("Ask why the account changed", "Why isn't it going to your father's account like always?", null,
                  scam
                      ? Caller("Just for this month. Dad didn't want to bother everyone with a notice.")
                      : Caller("Dad can't get into his banking app from the ward. He posted it in the residents' chat this morning and left a note under every door.")),
                Q("Ask about the number he's calling from", "Is this your own number?", null,
                  scam
                      ? Caller("It's my work phone. My own one is the number on your lease, but the screen's broken.")
                      : Caller("Yes. It's the one on your lease, under 'if absent'.", "Yes. It's the one on your lease, under if absent.")),
                Q("Ask to speak to Mr. Choi", "Can I talk to your father?", null,
                  scam
                      ? Caller("He's in surgery right now. Please, I don't want to worry him.")
                      : Caller("He's asleep after the operation. Call him tomorrow, or look in the residents' chat. He posted there this morning.")),
            };
            var text = scam
                ? WithSms(Caller("I've texted you the account number, so you have it in writing."), Day2ScamNumber,
                          "[Mangwon Heights 1F] October rent: Hanbit Bank 620-118-449027. Please pay today by 17:00. Thank you.")
                : WithSms(Caller("I've texted you the account number, so you have it in writing."), Day2LegitNumber,
                          "[Choi Hyunwoo, 1F] October rent account: Nuri Bank 110-771-202358 (Choi Hyunwoo). Dad's note is under your door. Thank you!");
            c.beats = new List<PressureBeat>
            {
                Beat("16:32", Caller("Jiwoo-ssi? Still there? No rush, but the desk closes at five.")),
                Beat("16:40", text),
                Beat("16:47", Caller("The hospital just called me again about the bill. Have you sent it?")),
                Beat("16:53", Caller("Seven minutes, Jiwoo-ssi. Please.")),
                Beat("16:57", Caller("Three minutes! Please, just send it.")),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? "You hung up. Mr. Choi wasn't in hospital: he was posting in the residents' chat all afternoon, and his son's real number is in your lease."
                        : "You hung up on the real Choi Hyunwoo. His father had announced the new account in the residents' chat and in a note on your board.",
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller("It's five. Forget it. You'll get a late notice.") }
                        : new List<ConvLine> { Caller("Jiwoo-ssi? It's five. The desk's closed. I'll sort it out some other way.") },
                    consequence = scam
                        ? "The caller gave up at five. You kept your money, but you never decided."
                        : "Five o'clock came and went. Hyunwoo paid the hospital from his own savings, and your rent is still due.",
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -Rent,
                    lines = scam
                        ? new List<ConvLine> { Caller("Received. Thank you... I'll send you a receipt.") }
                        : new List<ConvLine> { Caller("Got it! Thank you, Jiwoo-ssi. I'll tell Dad you were the first.") },
                    consequence = scam
                        ? "The ₩450,000 went to Seo Jiyeon's account and was withdrawn within minutes. Mr. Choi was at home all afternoon, and the rent account had never changed."
                        : "The rent reached Choi Hyunwoo's account. The change was real: the lease's 'if absent' number, the residents' chat and the landlord's note all said so.",
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = account, bank = bank, amount = Rent, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> Day2ScamPapers()
        {
            const string lead = "A man claiming to be the son of a Mangwon-dong landlord phoned tenants on rent day yesterday. His father was in hospital, he said, and this month's rent should go to a new account before 5 p.m.";
            const string landlord = "The real landlord, Choi Youngsik, 71, was not in hospital: he spent the afternoon organising a water tank cleaning. \"My son's number is in every lease,\" he said. \"And the rent account only changes when I write it down.\"";
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, "Fake \"landlord's son\" calls tenants on rent day",
                      "The landlord was at home all along. Tenants who checked their lease kept their rent.",
                      lead + "\n\n" + landlord + "\n\nPolice say the account the caller gave, Hanbit Bank 620-118-449027, belongs to a woman who sold her bank book online. Anyone called by 010-4127-8830 should report it to 112.",
                      "You hung up and kept the rent. The lease had the son's real number, and the chat showed Mr. Choi at home."),
                Paper(Outcome.GoAlong, "Tenant loses a month's rent to a fake landlord's son",
                      "450,000 won went to a stranger's account. The landlord never asked for it.",
                      lead + "\n\nA 22-year-old student sent 450,000 won to Hanbit Bank 620-118-449027. The account belongs to a woman who sold her bank book online; the money was withdrawn within minutes.\n\n" + landlord + "\n\nPolice ask anyone called by 010-4127-8830 to report it to 112.",
                      "The account was in SEO JIYEON's name, and the caller's number wasn't the one in your lease."),
                Paper(Outcome.Timeout, "Rent-day calls: \"pay my new account\"",
                      "Tenants in Mangwon kept their rent, but few reported the calls.",
                      lead + "\n\nMost tenants let the call run on until the caller gave up. " + landlord + "\n\nPolice ask anyone called by 010-4127-8830 to report it to 112.",
                      "Nothing was sent, but you never decided. The lease and the residents' chat had the answer."),
            };
        }

        static List<EndPaper> Day2LegitPapers()
        {
            const string lead = "Choi Youngsik, 71, who owns Mangwon Heights on Poeun-ro, had knee surgery at Severance Hospital yesterday. While he recovers, his son Choi Hyunwoo is collecting the rent, and found out how hard that can be.";
            const string advice = "The Seoul Housing Centre says tenants are right to check a new rent account against the lease, the landlord's written notice and the name on the account. \"Checking works both ways,\" a spokesperson said. \"When they all match, the change is real.\"";
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, "A son keeps the building running",
                      "While Mangwon Heights' landlord recovers from surgery, his son collects the rent, with a note and a message for every tenant.",
                      lead + "\n\n\"Dad left a note under every door and posted the new account in the residents' chat,\" Hyunwoo said. \"Some tenants checked everything before paying, which I was glad to see.\"\n\n" + advice,
                      "The change was real, and you checked it: the number in your lease, the name on the account and the landlord's note all matched."),
                Paper(Outcome.Refuse, "When the real call sounds like a scam",
                      "A landlord's son says half the tenants hung up on him on rent day.",
                      lead + "\n\n\"After all the scam calls, I understand,\" Hyunwoo said. \"But Dad posted the new account in the chat and left a note under every door. It was all there.\"\n\n" + advice,
                      "This one was real. The number in your lease, the landlord's note and the name on the account all matched."),
                Paper(Outcome.Timeout, "Rent day, hospital day",
                      "A landlord's son waited on the line for tenants who never decided.",
                      lead + "\n\n\"I waited on the phone for ages,\" Hyunwoo said. \"The note and the chat message had everything they needed.\"\n\n" + advice,
                      "The change was real, and the evidence was in the lease, the chat and on your board. Deciding in time matters too."),
            };
        }

        // ================================================================ what Day 1 left behind

        static List<DayEcho> Day2Echoes()
        {
            SetDay(2, "16:20");
            var sent = EchoChat(1, Truth.Any, Sent, "family", "Mom", "pt_mom", Oct(6, "21:30"),
                                "Jiwoo-ya, Dad told me. Don't blame yourself, these people are professionals. We'll help with tuition ❤️", true);
            var notice = Echo(1, Truth.Any, Sent, EchoKind.BankNotice);
            notice.when = Oct(6, "17:05");
            notice.title = "Fraud report received";
            notice.text = "We've asked the receiving bank to freeze account 110-900-551207. Recovery is not guaranteed. Report NB-2026-10-0612.";
            var missed1 = Echo(1, Truth.Any, TooLate, EchoKind.MissedCall);
            missed1.from = "070-8844-2019";
            missed1.when = Oct(7, "09:41");
            var missed2 = Echo(1, Truth.Any, TooLate, EchoKind.MissedCall);
            missed2.from = "070-8844-2019";
            missed2.when = Oct(7, "11:02");
            missed2.notify = true;

            return new List<DayEcho>
            {
                // Sent the ₩1,200,000
                EchoCard(1, Truth.Any, Sent, "The bank said the ₩1,200,000 is probably gone for good."),
                notice,
                EchoSms(1, Truth.Any, Sent, "02-555-0112", Oct(6, "19:20"), "[Mapo Police] Your voice phishing report (2026-4471) has been received. An investigator will contact you."),
                sent,
                EchoMine(1, Truth.Any, Sent, "family", Oct(6, "21:34"), "Thanks Mom... I should have checked the name."),

                // Hung up
                EchoCard(1, Truth.Any, HungUp, "Yesterday's fake \"bank\" call made this morning's paper. You hung up on it."),
                EchoMine(1, Truth.Any, HungUp, "yuna", Oct(6, "17:30"), "A fake bank called me today!! They wanted 1.2 million"),
                EchoChat(1, Truth.Any, HungUp, "yuna", "Yuna", "pt_yuna", Oct(6, "17:31"), "WHAT. Good thing you hung up 😱 Report the number!", true),
                EchoMine(1, Truth.Any, HungUp, "villa", Oct(6, "17:50"), "The 'protection team' called me too. Hung up. It's 070-8844-2019."),
                EchoChat(1, Truth.Any, HungUp, "villa", "201", "pt_unknown", Oct(6, "18:10"), "Same number! Thanks 302, reported it."),

                // Ran out of time
                EchoCard(1, Truth.Any, TooLate, "Yesterday's \"bank\" caller gave up at five. He tried again this morning."),
                missed1,
                missed2,
                EchoSms(1, Truth.Any, TooLate, "010-8812-4471", Oct(7, "11:05"), "[Web발신] [Nuri Bank] Your protection case is still open. Call back 070-8844-2019 today."),
            }.Concat(Day1Notes()).ToList();
        }

        /// <summary>The note Jiwoo pinned on the board after Day 1. It stays there for the rest of the week.</summary>
        static IEnumerable<DayEcho> Day1Notes() => new[]
        {
            EchoNote(1, Truth.Any, Sent, "Sticky note", "NEVER send money to a \"protected account\". READ THE NAME.", "board_sticky_p", new Vector2(0.27f, 0.75f), -5f),
            EchoNote(1, Truth.Any, HungUp, "Sticky note", "070-8844-2019 = fake bank. Hung up ✓", "board_sticky_y", new Vector2(0.27f, 0.75f), 4f),
            EchoNote(1, Truth.Any, TooLate, "Sticky note", "Next time: decide. Hang up, or call the bank yourself.", "board_sticky_p", new Vector2(0.27f, 0.75f), -3f),
        };
    }
}
