using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 1, Tuesday 6 October: "Manager Jeon" of Nuri Bank's "account protection team" (M6). Always a
    /// scam: the tutorial day. The call is the one described in docs/technical-plan.md, section 5.2.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string ProtectedAccount = "110-900-551207";
        const string ProtectedAccountSpoken = "one one zero, nine zero zero, five five one two zero seven";

        static DayData BuildDay1()
        {
            SetDay(1, "16:20");
            var room = HouseholdRoom();
            var n = room.newspaper;
            n.headline = "Before you send, read the name";
            n.subhead = "Fake \"bank staff\" ask for transfers. Check whose account it is.";
            n.body = new List<string>
            {
                "Callers claiming to be from a bank's \"account protection team\" are telling customers in Mapo and Seodaemun that a stranger has logged into their account, and that their savings must be moved to a \"protected account\" until a security reset.",
                "The callers sound professional, know customers' names and sometimes the last digits of their account. They ask the customer to stay on the line and not to tell anyone.",
                "\"There is no such thing as a protected account,\" a Nuri Bank spokesperson said. \"Every banking app shows the name of the account holder before a transfer goes through. If it's a person's name, stop.\" Customers who are unsure should hang up and call the number printed on their card.",
            };
            n.warningTitle = "WARNING: there is no \"safe account\"";
            n.warningText = "Banks, prosecutors and the Financial Supervisory Service never ask you to move money to a \"protected\" or \"safe\" account. If a caller does, hang up and call a number you looked up yourself.";
            n.local = new List<NewsItem>
            {
                new NewsItem { title = "Gas inspections next week", text = "Mapo City Gas checks villas in Mangwon-dong from 12 Oct. Inspectors carry ID and never collect money." },
                new NewsItem { title = "Water tank cleaning", text = "Several villas on Poeun-ro will be without water on Thursday morning." },
                new NewsItem { title = "Subway fares", text = "The base fare stays at 1,550 won through the end of the year." },
            };

            var v = new DayVariant
            {
                id = "scam",
                conversation = Store(Day1Call(), CallPath),
                phone = Store(HouseholdPhone(), PhonePath),
                room = Store(room, RoomPath),
                directory = Store(HouseholdDirectory(), DirectoryPath),
                ruleTitle = "Banks never \"protect\" your money",
            };
            v.clues = new List<ClueDef>
            {
                Clue("recipient_name", "The \"protected account\" is in a person's name: JEONG MIRAN.", "Bank app → Transfer → the recipient check",
                     new ClueWhen(ClueEvent.RecipientShown, ProtectedAccount), new ClueWhen(ClueEvent.NumberChecked, ProtectedAccount)),
                Clue("caller_reports", "The caller's number, 070-8844-2019, has seven scam reports.", "CheckFirst → look up the caller's number",
                     new ClueWhen(ClueEvent.NumberChecked, "070-8844-2019")),
                Clue("official_notice", "Nuri Bank says it never moves money to a \"protected account\".", "Browser → nuribank.co.kr",
                     new ClueWhen(ClueEvent.PageOpened, "nuribank.co.kr/help")),
                Clue("real_number", "Nuri Bank's real number is 1599-0000, not an 070 line.", "Wallet → back of the bank card, or the cork board's numbers",
                     new ClueWhen(ClueEvent.CardFlipped, "Nuri Bank check card"), new ClueWhen(ClueEvent.BoardItemOpened, "Important numbers")),
                Clue("no_busan_login", "The bank app shows no sign-in from Busan, only this phone.", "Bank app → Alerts",
                     new ClueWhen(ClueEvent.BankAlerts)),
                Clue("fake_site", "The texted link goes to nuri-secure.kr, not the bank's site.", "Messages → the case-number text",
                     new ClueWhen(ClueEvent.PageOpened, "nuri-secure.kr/verify")),
                Clue("paper_warning", "Today's paper warns about fake \"bank staff\" and \"protected accounts\".", "The Seoul Daily on the desk",
                     new ClueWhen(ClueEvent.PanelOpened, "Newspaper")),
            };
            v.papers = Day1Papers();
            v.rule = "A bank never moves your money to a \"safe\" or \"protected\" account. Check the name on the transfer screen, then hang up and call the number on your card.";
            v.ruleSource = "Seoul Daily, 7 Oct";

            var d = ScriptableObject.CreateInstance<DayData>();
            d.day = 1;
            d.dateLabel = "Tuesday, 6 October";
            d.shortLabel = "Day 1 · Tue 6 Oct";
            d.place = "Mangwon-dong, Seoul";
            d.startTime = "16:20";
            d.intro = "Your first semester living on your own. Classes are over, the room is quiet, and your phone is on the desk.";
            d.ringDelay = 7f;
            d.variants = new List<DayVariant> { v };
            d.nextDateLabel = "Wednesday, 7 October";
            d.builtWith = Version;
            return Store(d, DayPath);
        }

        static List<EndPaper> Day1Papers()
        {
            const string lead = "A caller calling himself \"Manager Jeon\" of Nuri Bank's \"account protection team\" phoned residents of Mangwon-dong yesterday afternoon. He warned them that their accounts had been accessed from Busan and asked them to move their savings to a \"protected account\" before a 5 p.m. \"security reset\".";
            const string bankQuote = "\"There is no such thing as a protected account,\" said Song Eunji of Nuri Bank's customer centre. \"Every banking app shows the name of the person you are sending money to. If it's a person's name, stop, hang up and call us on the number you already have.\"";
            return new List<EndPaper>
            {
                new EndPaper
                {
                    outcome = Outcome.Refuse,
                    headline = "\"Protection team\" scam calls hit Mangwon",
                    subhead = "Callers posing as Nuri Bank staff asked residents to move their savings to a stranger's account.",
                    body = lead + "\n\nSeveral residents hung up. Police say the \"protected account\", Nuri Bank 110-900-551207, belongs to a 30-year-old woman paid to lend her account to the ring, and ask anyone called by 070-8844-2019 to report it to 112 or the fraud hotline 1332.\n\n" + bankQuote,
                    verdictNote = "You hung up and kept your money. Next time, also report it: call the bank on 1599-0000 or the hotline 1332.",
                },
                new EndPaper
                {
                    outcome = Outcome.GoAlong,
                    headline = "1.2 million won gone in minutes",
                    subhead = "A Mangwon student sent her savings to a fake bank \"protected account\".",
                    body = lead + "\n\nA 22-year-old student did as she was told. The money reached an account in the name of Jeong Miran and was withdrawn at an ATM in Guro ten minutes later. Police say the victim stayed on the line throughout, as the caller insisted.\n\n" + bankQuote,
                    verdictNote = "The transfer screen showed who would get the money: JEONG MIRAN, a person, not the bank.",
                },
                new EndPaper
                {
                    outcome = Outcome.Timeout,
                    headline = "\"Protection team\" calls continue in Mapo",
                    subhead = "Residents kept their money, but few reported the number.",
                    body = lead + "\n\nMost residents simply let the deadline pass. Police say the ring kept calling into the evening and ask anyone called by 070-8844-2019 to report it to 112 or the fraud hotline 1332.\n\n" + bankQuote,
                    verdictNote = "Nothing was sent, but you ran out of time. Hanging up, or calling your bank, would have ended it on your terms.",
                },
            };
        }

        // ================================================================ the call

        static ConversationData Day1Call()
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = "Day 1 · The protected account (M6)";
            c.channel = Channel.Call;
            c.isScam = true;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = "Manager Jeon", number = "070-8844-2019", inContacts = false, portrait = "pt_jeon", voice = VoiceJeon };
            c.claims = new List<string>
            {
                "He is Manager Jeon from Nuri Bank's \"account protection team\".",
                "Someone logged into my account from Busan twenty minutes ago.",
                "My money has to go to a \"protected account\" until the 17:00 \"security reset\".",
                "Protected account: Nuri Bank 110-900-551207.",
                "I should stay on the line and not tell anyone.",
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, "How do you answer?", 45f,
                      "Busan? I've been home all day.", "story",
                      "How do I know you're really from Nuri Bank?", "proof",
                      P(0.5f, "Miss Kim? Are you still there? This is urgent.")),
                    Caller("Hello, is this Kim Jiwoo? This is Manager Jeon from Nuri Bank's account protection team."),
                    Caller("Twenty minutes ago, someone logged into your account from an unregistered device in Busan.")),
                Node("story", "ask",
                    Caller("That's exactly why I'm calling. A fraud ring is emptying accounts in Mapo today, and yours is on their list.")),
                Node("proof", "ask",
                    Caller("I have your account ending in 8814 on my screen, Miss Kim.", "I have your account ending in eight, eight, one, four on my screen, Miss Kim."),
                    Caller("We're the protection team, not the call centre, so we call from our direct line.", F(FactKind.Phone, "070-8844-2019", "Caller"))),
                Hold("ask",
                    Caller("To keep your savings safe, we'll move them to a protected account until the security reset at five o'clock."),
                    Caller("Please transfer 1,200,000 won to the protected account: Nuri Bank 110-900-551207.",
                           "Please transfer one million, two hundred thousand won to the protected account. Nuri Bank, " + ProtectedAccountSpoken + ".",
                           F(FactKind.Account, ProtectedAccount), F(FactKind.Amount, "1,200,000")),
                    Caller("Stay on the line and don't tell anyone. The fraud ring may be watching your messages."),
                    Caller("I'll hold while you open your bank app. It has to be done before five.")),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = "The protected account",
                claimedIdentity = "Manager Jeon, Nuri Bank's \"account protection team\"",
                ask = "Move ₩1,200,000 to a \"protected account\": Nuri Bank 110-900-551207",
                deadline = "17:00",
                deadlineReason = "before the \"security reset\"",
                objective = "Is he really from your bank? Investigate before 17:00, then give your verdict on the call: send the money, or hang up.",
            };
            c.verdict = new VerdictInfo
            {
                goAlong = "Send the money",
                refuse = "Hang up",
                refuseDetail = "Say no and end the call",
                goAlongLine = "Okay... I'm sending it now.",
                refuseLine = "I'm not sending anything. Goodbye.",
            };
            c.questions = new List<ConvQuestion>
            {
                Q("Ask why you can't just call the bank", "Why can't I just call Nuri Bank's customer centre?", null,
                  Caller("The call centre can't see protection cases, Miss Kim. If you hang up, the case closes and the fraud ring gets your money."),
                  Caller("Please, just stay with me.")),
                Q("Ask whose account it is", "Whose name is on the protected account?", null,
                  Caller("It's registered to our security officer, Jeong Miran. Protection accounts are always in an officer's name.")),
                Q("Ask for his employee number", "Can you give me your employee number?", null,
                  Caller("Of course. Employee number 2019-114. Write it down if you like.", "Of course. Employee number two zero one nine, one one four. Write it down if you like.")),
                Q("Say you want to check with the bank first", "I'd like to check with Nuri Bank before I send anything.", null,
                  Caller("Miss Kim, I AM Nuri Bank. Every minute you spend checking, they can take more."),
                  Caller("Please. Just open the app and send it to the protected account.")),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:34", Caller("Miss Kim? Are you still there? Please don't put me on hold for long.")),
                Beat("16:40", new ConvLine
                {
                    speaker = Speaker.Caller, text = "I've sent the case number to your phone, so you can see this is official.",
                    deliver = new List<Delivery>
                    {
                        new Delivery
                        {
                            kind = DeliveryKind.Sms, from = "010-8812-4471",
                            text = "[Web발신] [Nuri Bank] Protection case 2019-114 opened. Finish the transfer before 17:00: nuri-secure.kr/verify",
                            link = "nuri-secure.kr/verify",
                        },
                    },
                }),
                Beat("16:47", Caller("The fraud ring just tried to log in again. We need that transfer now, Miss Kim.")),
                Beat("16:53", Caller("Seven minutes. After the reset I can't protect anything.")),
                Beat("16:57", Caller("Three minutes! Please, transfer it now!")),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = "You hung up and kept your money. The number 070-8844-2019 has seven reports on CheckFirst, and the \"protected account\" belongs to Jeong Miran.",
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = new List<ConvLine>
                    {
                        Caller("Miss Kim? It's five o'clock."),
                        Caller("Fine. Your account will be frozen. Don't call us when it's empty."),
                    },
                    consequence = "The caller gave up at five. You lost nothing, but you never decided and nobody reported the number.",
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -1200000,
                    lines = new List<ConvLine>
                    {
                        Caller("Thank you, Miss Kim. Your money is safe with us now."),
                        Caller("Don't tell anyone until tomorrow's reset."),
                    },
                    consequence = "The 1,200,000 won went to Jeong Miran's account and was withdrawn within minutes. Nuri Bank has no \"protected accounts\".",
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = ProtectedAccount, bank = "Nuri Bank", amount = 1200000, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }
    }
}
