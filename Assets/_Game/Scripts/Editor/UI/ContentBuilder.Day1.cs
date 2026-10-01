using System;
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
        static string ProtectedAccountSpoken => L("one one zero, nine zero zero, five five one two zero seven", "일일공, 구공공, 오오일이공칠");

        static DayData BuildDay1()
        {
            SetDay(1, "16:20");
            var room = HouseholdRoom();
            var n = room.newspaper;
            n.headline = L("Before you send, read the name", "송금 전, 받는 사람 이름을 확인하세요");
            n.subhead = L("Fake \"bank staff\" ask for transfers. Check whose account it is.", "가짜 '은행 직원'이 이체를 요구한다. 누구 계좌인지 확인하라.");
            n.body = new List<string>
            {
                L("Callers claiming to be from a bank's \"account protection team\" are telling customers in Mapo and Seodaemun that a stranger has logged into their account, and that their savings must be moved to a \"protected account\" until a security reset.",
                  "은행 '계좌보호팀'을 사칭한 전화가 마포와 서대문 일대 고객들에게 '누군가 계좌에 접속했다'며 보안 초기화 전까지 예금을 '보호계좌'로 옮겨야 한다고 말하고 있다."),
                L("The callers sound professional, know customers' names and sometimes the last digits of their account. They ask the customer to stay on the line and not to tell anyone.",
                  "이들은 전문가처럼 말하고, 고객의 이름은 물론 계좌 끝자리까지 알고 있는 경우도 있다. 그리고 전화를 끊지 말고 아무에게도 알리지 말라고 한다."),
                L("\"There is no such thing as a protected account,\" a Nuri Bank spokesperson said. \"Every banking app shows the name of the account holder before a transfer goes through. If it's a person's name, stop.\" Customers who are unsure should hang up and call the number printed on their card.",
                  "누리은행 관계자는 \"보호계좌라는 것은 없다\"며 \"모든 은행 앱은 이체 전에 예금주 이름을 보여 준다. 개인 이름이라면 멈춰야 한다\"고 말했다. 의심스러우면 전화를 끊고 카드에 적힌 번호로 직접 전화해야 한다."),
            };
            n.warningTitle = L("WARNING: there is no \"safe account\"", "경고: '안전계좌'는 없다");
            n.warningText = L("Banks, prosecutors and the Financial Supervisory Service never ask you to move money to a \"protected\" or \"safe\" account. If a caller does, hang up and call a number you looked up yourself.",
                              "은행, 검찰, 금융감독원은 절대 '보호계좌'나 '안전계좌'로 돈을 옮기라고 하지 않는다. 그런 전화를 받으면 끊고, 직접 찾은 번호로 전화하라.");
            n.local = new List<NewsItem>
            {
                new NewsItem { title = L("Gas inspections next week", "다음 주 가스 점검"), text = L("Mapo City Gas checks villas in Mangwon-dong from 12 Oct. Inspectors carry ID and never collect money.", "마포도시가스가 12일부터 망원동 빌라를 점검한다. 점검원은 신분증을 지니며 돈을 받지 않는다.") },
                new NewsItem { title = L("Water tank cleaning", "물탱크 청소"), text = L("Several villas on Poeun-ro will be without water on Thursday morning.", "포은로 일대 빌라 여러 곳이 목요일 오전 단수된다.") },
                new NewsItem { title = L("Subway fares", "지하철 요금"), text = L("The base fare stays at 1,550 won through the end of the year.", "기본요금 1,550원이 연말까지 유지된다.") },
            };

            var v = new DayVariant
            {
                id = "scam",
                conversation = Store(Day1Call(), Localized(CallPath)),
                phone = Store(HouseholdPhone(), Localized(PhonePath)),
                room = Store(room, Localized(RoomPath)),
                directory = Store(HouseholdDirectory(), Localized(DirectoryPath)),
                ruleTitle = L("Banks never \"protect\" your money", "은행은 돈을 '보호'해 주지 않는다"),
            };
            v.clues = new List<ClueDef>
            {
                Clue("recipient_name", L("The \"protected account\" is in a person's name: JEONG MIRAN.", "'보호계좌'는 개인 명의다: 정미란."),
                     L("Bank app → Transfer → the recipient check", "은행 앱 → 이체 → 받는 분 확인"),
                     new ClueWhen(ClueEvent.RecipientShown, ProtectedAccount), new ClueWhen(ClueEvent.NumberChecked, ProtectedAccount)),
                Clue("caller_reports", L("The caller's number, 070-8844-2019, has seven scam reports.", "발신 번호 070-8844-2019에는 사기 신고가 7건 있다."),
                     L("CheckFirst → look up the caller's number", "체크퍼스트 → 발신 번호 조회"),
                     new ClueWhen(ClueEvent.NumberChecked, "070-8844-2019")),
                Clue("official_notice", L("Nuri Bank says it never moves money to a \"protected account\".", "누리은행은 '보호계좌'로 돈을 옮기는 일이 없다고 밝히고 있다."),
                     L("Browser → nuribank.co.kr", "인터넷 → nuribank.co.kr"),
                     new ClueWhen(ClueEvent.PageOpened, "nuribank.co.kr/help")),
                Clue("real_number", L("Nuri Bank's real number is 1599-0000, not an 070 line.", "누리은행의 진짜 번호는 070이 아니라 1599-0000이다."),
                     L("Wallet → back of the bank card, or the cork board's numbers", "지갑 → 체크카드 뒷면, 또는 게시판의 번호 메모"),
                     new ClueWhen(ClueEvent.CardFlipped, CardTitle), new ClueWhen(ClueEvent.BoardItemOpened, NumbersTitle)),
                Clue("no_busan_login", L("The bank app shows no sign-in from Busan, only this phone.", "은행 앱에는 부산 로그인 기록이 없고, 이 휴대폰뿐이다."),
                     L("Bank app → Alerts", "은행 앱 → 알림"),
                     new ClueWhen(ClueEvent.BankAlerts)),
                Clue("fake_site", L("The texted link goes to nuri-secure.kr, not the bank's site.", "문자 속 링크는 은행 사이트가 아닌 nuri-secure.kr로 연결된다."),
                     L("Messages → the case-number text", "메시지 → 사건번호 문자"),
                     new ClueWhen(ClueEvent.PageOpened, "nuri-secure.kr/verify")),
                Clue("paper_warning", L("Today's paper warns about fake \"bank staff\" and \"protected accounts\".", "오늘 신문이 가짜 '은행 직원'과 '보호계좌'를 경고하고 있다."),
                     L("The Seoul Daily on the desk", "책상 위의 서울데일리"),
                     new ClueWhen(ClueEvent.PanelOpened, "Newspaper")),
            };
            v.papers = Day1Papers();
            v.rule = L("A bank never moves your money to a \"safe\" or \"protected\" account. Check the name on the transfer screen, then hang up and call the number on your card.",
                       "은행은 절대 '안전계좌'나 '보호계좌'로 돈을 옮기지 않는다. 이체 화면의 이름을 확인하고, 전화를 끊고, 카드에 적힌 번호로 전화하라.");
            v.ruleSource = PaperSource(7);

            var d = ScriptableObject.CreateInstance<DayData>();
            d.day = 1;
            d.dateLabel = DateLabel(today);
            d.shortLabel = ShortLabel(1, today);
            d.weekday = WeekdayLabel(today);
            d.place = L("Mangwon-dong, Seoul", "서울 마포구 망원동");
            d.startTime = "16:20";
            d.intro = L("Your first semester living on your own. Classes are over, the room is quiet, and your phone is on the desk.",
                        "처음으로 혼자 사는 학기. 수업은 끝났고, 방은 조용하고, 휴대폰은 책상 위에 있다.");
            d.ringDelay = 7f;
            d.variants = new List<DayVariant> { v };
            d.nextDateLabel = DateLabel(today.AddDays(1));
            d.builtWith = Version;
            return Store(d, Localized(DayPath));
        }

        static List<EndPaper> Day1Papers()
        {
            string lead = L("A caller calling himself \"Manager Jeon\" of Nuri Bank's \"account protection team\" phoned residents of Mangwon-dong yesterday afternoon. He warned them that their accounts had been accessed from Busan and asked them to move their savings to a \"protected account\" before a 5 p.m. \"security reset\".",
                            "어제 오후 누리은행 '계좌보호팀' '전 과장'을 자칭한 남성이 망원동 주민들에게 전화를 걸었다. 그는 부산에서 계좌 접속 시도가 있었다며 오후 5시 '보안 초기화' 전까지 예금을 '보호계좌'로 옮기라고 요구했다.");
            string bankQuote = L("\"There is no such thing as a protected account,\" said Song Eunji of Nuri Bank's customer centre. \"Every banking app shows the name of the person you are sending money to. If it's a person's name, stop, hang up and call us on the number you already have.\"",
                                 "누리은행 고객센터 송은지 씨는 \"보호계좌라는 것은 없다\"며 \"모든 은행 앱은 돈을 받는 사람의 이름을 보여 준다. 개인 이름이라면 멈추고, 전화를 끊고, 이미 알고 있는 번호로 은행에 전화하라\"고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse,
                      L("\"Protection team\" scam calls hit Mangwon", "망원동에 '보호팀' 사기 전화 기승"),
                      L("Callers posing as Nuri Bank staff asked residents to move their savings to a stranger's account.", "누리은행 직원을 사칭한 전화가 주민들에게 남의 계좌로 예금을 옮기라고 요구했다."),
                      lead + "\n\n" + L("Several residents hung up. Police say the \"protected account\", Nuri Bank 110-900-551207, belongs to a 30-year-old woman paid to lend her account to the ring, and ask anyone called by 070-8844-2019 to report it to 112 or the fraud hotline 1332.",
                                        "여러 주민이 전화를 끊었다. 경찰에 따르면 '보호계좌'인 누리은행 110-900-551207은 돈을 받고 조직에 계좌를 빌려준 30대 여성의 명의다. 경찰은 070-8844-2019로 전화를 받은 사람은 112나 금융감독원 1332에 신고해 달라고 당부했다.") + "\n\n" + bankQuote,
                      L("You hung up and kept your money. Next time, also report it: call the bank on 1599-0000 or the hotline 1332.", "전화를 끊고 돈을 지켰다. 다음에는 신고도 하자: 은행 1599-0000이나 1332로.")),
                Paper(Outcome.GoAlong,
                      L("1.2 million won gone in minutes", "120만 원, 몇 분 만에 사라져"),
                      L("A Mangwon student sent her savings to a fake bank \"protected account\".", "망원동 대학생, 가짜 은행 '보호계좌'로 예금 송금"),
                      lead + "\n\n" + L("A 22-year-old student did as she was told. The money reached an account in the name of Jeong Miran and was withdrawn at an ATM in Guro ten minutes later. Police say the victim stayed on the line throughout, as the caller insisted.",
                                        "22세 대학생 한 명은 시키는 대로 했다. 돈은 정미란 명의 계좌로 들어갔고, 10분 뒤 구로의 ATM에서 인출됐다. 경찰에 따르면 피해자는 상대의 요구대로 내내 전화를 끊지 않았다.") + "\n\n" + bankQuote,
                      L("The transfer screen showed who would get the money: JEONG MIRAN, a person, not the bank.", "이체 화면에 돈을 받는 사람이 나와 있었다: 은행이 아니라 '정미란'이라는 개인.")),
                Paper(Outcome.Timeout,
                      L("\"Protection team\" calls continue in Mapo", "마포 '보호팀' 전화 계속돼"),
                      L("Residents kept their money, but few reported the number.", "주민들은 돈을 지켰지만 신고는 적었다."),
                      lead + "\n\n" + L("Most residents simply let the deadline pass. Police say the ring kept calling into the evening and ask anyone called by 070-8844-2019 to report it to 112 or the fraud hotline 1332.",
                                        "대부분의 주민은 마감 시간이 지나가도록 내버려 뒀다. 경찰은 조직이 저녁까지 전화를 계속 걸었다며, 070-8844-2019로 전화를 받은 사람은 112나 1332에 신고해 달라고 당부했다.") + "\n\n" + bankQuote,
                      L("Nothing was sent, but you ran out of time. Hanging up, or calling your bank, would have ended it on your terms.", "돈은 보내지 않았지만 시간을 넘겼다. 직접 끊거나 은행에 전화했다면 내 뜻대로 끝낼 수 있었다.")),
            };
        }

        // ================================================================ the call

        static ConversationData Day1Call()
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = L("Day 1 · The protected account (M6)", "1일차 · 보호계좌 (M6)");
            c.channel = Channel.Call;
            c.isScam = true;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = L("Manager Jeon", "전 과장"), number = "070-8844-2019", inContacts = false, portrait = "pt_jeon", voice = VoiceJeon };
            c.claims = new List<string>
            {
                L("He is Manager Jeon from Nuri Bank's \"account protection team\".", "누리은행 '계좌보호팀'의 전 과장이라고 한다."),
                L("Someone logged into my account from Busan twenty minutes ago.", "20분 전에 부산에서 누군가 내 계좌에 접속했다고 한다."),
                L("My money has to go to a \"protected account\" until the 17:00 \"security reset\".", "17시 '보안 초기화' 전까지 돈을 '보호계좌'로 옮겨야 한다고 한다."),
                L("Protected account: Nuri Bank 110-900-551207.", "보호계좌: 누리은행 110-900-551207"),
                L("I should stay on the line and not tell anyone.", "전화를 끊지 말고 아무에게도 말하지 말라고 한다."),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 45f,
                      L("Busan? I've been home all day.", "부산이요? 저 오늘 하루 종일 집에 있었는데요."), "story",
                      L("How do I know you're really from Nuri Bank?", "진짜 누리은행에서 전화하신 거 맞아요?"), "proof",
                      P(0.5f, L("Miss Kim? Are you still there? This is urgent.", "김지우 씨? 듣고 계세요? 급한 일입니다."))),
                    Caller(L("Hello, is this Kim Jiwoo? This is Manager Jeon from Nuri Bank's account protection team.", "여보세요, 김지우 씨 되시죠? 누리은행 계좌보호팀 전 과장입니다.")),
                    Caller(L("Twenty minutes ago, someone logged into your account from an unregistered device in Busan.", "20분 전에 부산에서 등록되지 않은 기기로 고객님 계좌에 로그인한 기록이 있습니다."))),
                Node("story", "ask",
                    Caller(L("That's exactly why I'm calling. A fraud ring is emptying accounts in Mapo today, and yours is on their list.",
                             "그래서 전화드린 겁니다. 오늘 마포에서 사기 조직이 계좌를 털고 있는데, 고객님 계좌가 그 명단에 있어요."))),
                Node("proof", "ask",
                    Caller(L("I have your account ending in 8814 on my screen, Miss Kim.", "지금 제 화면에 끝자리 8814인 고객님 계좌가 떠 있습니다."),
                           L("I have your account ending in eight, eight, one, four on my screen, Miss Kim.", "지금 제 화면에 끝자리 팔팔일사인 고객님 계좌가 떠 있습니다.")),
                    Caller(L("We're the protection team, not the call centre, so we call from our direct line.", "저희는 고객센터가 아니라 보호팀이라 직통 번호로 연락드립니다."),
                           F(FactKind.Phone, "070-8844-2019", L("Caller", "발신 번호")))),
                Hold("ask",
                    Caller(L("To keep your savings safe, we'll move them to a protected account until the security reset at five o'clock.",
                             "고객님 예금을 지키기 위해 5시 보안 초기화 때까지 보호계좌로 옮겨 두겠습니다."),
                           L(null, "고객님 예금을 지키기 위해 다섯 시 보안 초기화 때까지 보호계좌로 옮겨 두겠습니다.")),
                    Caller(L("Please transfer 1,200,000 won to the protected account: Nuri Bank 110-900-551207.", "보호계좌로 120만 원을 이체해 주세요. 누리은행 110-900-551207입니다."),
                           L("Please transfer one million, two hundred thousand won to the protected account. Nuri Bank, " + ProtectedAccountSpoken + ".",
                             "보호계좌로 백이십만 원을 이체해 주세요. 누리은행, " + ProtectedAccountSpoken + "입니다."),
                           F(FactKind.Account, ProtectedAccount), F(FactKind.Amount, "1,200,000")),
                    Caller(L("Stay on the line and don't tell anyone. The fraud ring may be watching your messages.", "전화 끊지 마시고 아무에게도 말하지 마세요. 사기 조직이 고객님 메시지를 보고 있을 수도 있습니다.")),
                    Caller(L("I'll hold while you open your bank app. It has to be done before five.", "은행 앱 여시는 동안 기다리겠습니다. 5시 전에 끝내야 합니다."),
                           L(null, "은행 앱 여시는 동안 기다리겠습니다. 다섯 시 전에 끝내야 합니다."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("The protected account", "보호계좌"),
                claimedIdentity = L("Manager Jeon, Nuri Bank's \"account protection team\"", "누리은행 '계좌보호팀' 전 과장"),
                ask = L("Move ₩1,200,000 to a \"protected account\": Nuri Bank 110-900-551207", "'보호계좌'로 120만 원 이체: 누리은행 110-900-551207"),
                deadline = "17:00",
                deadlineReason = L("before the \"security reset\"", "'보안 초기화' 전"),
                objective = L("Is he really from your bank? Investigate before 17:00, then give your verdict on the call: send the money, or hang up.",
                              "정말 은행 직원일까? 17:00 전까지 조사한 뒤, 통화 화면에서 판정을 내리자: 돈을 보낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Send the money", "돈 보내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("Okay... I'm sending it now.", "알겠어요… 지금 보낼게요."),
                refuseLine = L("I'm not sending anything. Goodbye.", "아무것도 안 보낼 거예요. 끊을게요."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask why you can't just call the bank", "왜 은행에 직접 전화하면 안 되는지 묻기"), L("Why can't I just call Nuri Bank's customer centre?", "그냥 누리은행 고객센터에 전화하면 안 돼요?"), null,
                  Caller(L("The call centre can't see protection cases, Miss Kim. If you hang up, the case closes and the fraud ring gets your money.",
                           "고객센터에서는 보호 사건 내용을 볼 수 없습니다. 전화를 끊으시면 사건이 종료되고 사기 조직이 돈을 가져갑니다.")),
                  Caller(L("Please, just stay with me.", "제발 저랑 통화를 유지해 주세요."))),
                Q(L("Ask whose account it is", "누구 계좌인지 묻기"), L("Whose name is on the protected account?", "보호계좌는 누구 명의예요?"), null,
                  Caller(L("It's registered to our security officer, Jeong Miran. Protection accounts are always in an officer's name.",
                           "저희 보안 담당자 정미란 명의로 되어 있습니다. 보호계좌는 항상 담당 직원 명의로 개설돼요."))),
                Q(L("Ask for his employee number", "사번 묻기"), L("Can you give me your employee number?", "사번 좀 알려 주실 수 있어요?"), null,
                  Caller(L("Of course. Employee number 2019-114. Write it down if you like.", "물론이죠. 사번 2019-114입니다. 적어 두셔도 됩니다."),
                         L("Of course. Employee number two zero one nine, one one four. Write it down if you like.", "물론이죠. 사번 이공일구, 일일사입니다. 적어 두셔도 됩니다."))),
                Q(L("Say you want to check with the bank first", "먼저 은행에 확인하겠다고 말하기"), L("I'd like to check with Nuri Bank before I send anything.", "보내기 전에 누리은행에 먼저 확인해 볼게요."), null,
                  Caller(L("Miss Kim, I AM Nuri Bank. Every minute you spend checking, they can take more.", "김지우 씨, 제가 바로 누리은행입니다. 확인하시는 동안에도 돈이 빠져나갈 수 있어요.")),
                  Caller(L("Please. Just open the app and send it to the protected account.", "제발요. 앱 여시고 보호계좌로 보내 주세요."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:34", Caller(L("Miss Kim? Are you still there? Please don't put me on hold for long.", "김지우 씨? 계세요? 너무 오래 기다리게 하지 마세요."))),
                Beat("16:40", WithSms(Caller(L("I've sent the case number to your phone, so you can see this is official.", "공식 절차라는 걸 보여 드리려고 사건번호를 문자로 보내 드렸습니다.")),
                                      "010-8812-4471",
                                      L("[Web발신] [Nuri Bank] Protection case 2019-114 opened. Finish the transfer before 17:00: nuri-secure.kr/verify",
                                        "[Web발신] [누리은행] 보호 사건 2019-114 접수. 17:00 전까지 이체를 완료하세요: nuri-secure.kr/verify"),
                                      "nuri-secure.kr/verify")),
                Beat("16:47", Caller(L("The fraud ring just tried to log in again. We need that transfer now, Miss Kim.", "방금 사기 조직이 또 로그인을 시도했습니다. 지금 바로 이체하셔야 해요."))),
                Beat("16:53", Caller(L("Seven minutes. After the reset I can't protect anything.", "7분 남았습니다. 초기화되면 저도 아무것도 지켜 드릴 수 없어요."),
                                     L(null, "칠 분 남았습니다. 초기화되면 저도 아무것도 지켜 드릴 수 없어요."))),
                Beat("16:57", Caller(L("Three minutes! Please, transfer it now!", "3분 남았어요! 제발 지금 이체하세요!"), L(null, "삼 분 남았어요! 제발 지금 이체하세요!"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = L("You hung up and kept your money. The number 070-8844-2019 has seven reports on CheckFirst, and the \"protected account\" belongs to Jeong Miran.",
                                    "전화를 끊고 돈을 지켰다. 070-8844-2019에는 체크퍼스트 신고가 7건 있었고, '보호계좌'는 정미란 명의였다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = new List<ConvLine>
                    {
                        Caller(L("Miss Kim? It's five o'clock.", "김지우 씨? 다섯 시입니다.")),
                        Caller(L("Fine. Your account will be frozen. Don't call us when it's empty.", "좋습니다. 계좌가 동결될 겁니다. 텅 비어도 저희한테 연락하지 마세요.")),
                    },
                    consequence = L("The caller gave up at five. You lost nothing, but you never decided and nobody reported the number.",
                                    "상대는 다섯 시에 포기했다. 잃은 건 없지만 결정을 내리지 못했고, 아무도 그 번호를 신고하지 않았다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -1200000,
                    lines = new List<ConvLine>
                    {
                        Caller(L("Thank you, Miss Kim. Your money is safe with us now.", "감사합니다, 김지우 씨. 이제 고객님 돈은 안전합니다.")),
                        Caller(L("Don't tell anyone until tomorrow's reset.", "내일 초기화 전까지는 아무에게도 말하지 마세요.")),
                    },
                    consequence = L("The 1,200,000 won went to Jeong Miran's account and was withdrawn within minutes. Nuri Bank has no \"protected accounts\".",
                                    "120만 원은 정미란 명의 계좌로 들어가 몇 분 만에 인출됐다. 누리은행에 '보호계좌'는 없다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = ProtectedAccount, bank = NuriBank, amount = 1200000, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }
    }
}
