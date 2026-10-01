using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 0, Monday 5 October (a public holiday): the tutorial. "The billing team of Mapo City Gas"
    /// says the September bill is unpaid and the gas goes off at five unless it is transferred now.
    /// Always a scam, with a small sum and a far deadline, and built so that every place the player
    /// can check gives one piece of the answer. The guide (TutorialGuide) walks through the clues in
    /// the order they are listed here, showing each clue's <see cref="ClueDef.guide"/>.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string Day0Dir = "Assets/_Game/Data/Day0";
        const string GasScamNumber = "010-7359-2046";
        const string GasScamAccount = "620-339-104772";
        const string GasOfficialNumber = "02-555-0181";
        const long GasBill = 18420;

        static string GasScamAccountSpoken => L("six two zero, three three nine, one zero four seven seven two", "육이공, 삼삼구, 일공사칠칠이");
        static string GasCompany => L("Mapo City Gas", "마포도시가스");
        static string VoiceBilling => L("Samantha", "Sandy (Korean (South Korea))");

        static DayData BuildDay0()
        {
            SetDay(0, "14:10");
            var room = HouseholdRoom();
            var n = room.newspaper;
            n.headline = L("\"Pay now or we cut your gas\"", "\"지금 안 내면 가스 끊깁니다\"");
            n.subhead = L("Fake billing calls go after people who live alone. A few quick checks stop them.",
                          "혼자 사는 사람을 노린 가짜 요금 독촉 전화. 몇 가지만 확인하면 막을 수 있다.");
            n.body = new List<string>
            {
                L("Callers claiming to be from gas, power and phone companies are telling residents of Mapo that a bill is unpaid, and that the service will be cut off within hours unless they transfer the money at once.",
                  "가스·전기·통신 회사를 사칭한 전화가 마포 주민들에게 '요금이 미납됐다'며 당장 이체하지 않으면 몇 시간 안에 공급이 끊긴다고 말하고 있다."),
                L("The amounts are real, often taken from leaked customer lists. The account is not: it belongs to a stranger, and the money is gone within minutes.",
                  "금액은 유출된 고객 명단에서 가져온 진짜 금액인 경우가 많다. 하지만 계좌는 가짜다. 모르는 사람의 계좌이고, 돈은 몇 분 만에 사라진다."),
                L("\"We never phone a customer to collect a bill, and never on a holiday,\" a Mapo City Gas spokesperson said. \"Look at your bill and your bank history, and look up who owns the number and the account. On CheckFirst (checkfirst.kr) that takes two minutes.\"",
                  "마포도시가스 관계자는 \"전화로 요금을 걷는 일은 없고, 공휴일에는 더더욱 없다\"며 \"고지서와 은행 내역을 보고, 그 번호와 계좌의 주인을 조회해 보라. 체크퍼스트(checkfirst.kr)에서 2분이면 된다\"고 말했다."),
            };
            n.warningTitle = L("WARNING: nobody collects a bill by phone", "경고: 전화로 요금을 걷는 곳은 없다");
            n.warningText = L("A gas, power or phone company sends a bill. It does not phone you for a transfer. Before you pay a caller, check who owns the number and the account, what your own bill and your bank history say, and what people you know have written.",
                              "가스·전기·통신 회사는 고지서를 보낸다. 전화로 이체를 요구하지 않는다. 돈을 보내기 전에 그 번호와 계좌의 주인이 누구인지, 내 고지서와 은행 내역은 뭐라고 하는지, 아는 사람들이 뭐라고 썼는지 확인하라.");
            n.local = new List<NewsItem>
            {
                new NewsItem { title = L("Holiday today", "오늘은 대체공휴일"), text = L("Banks, public offices and customer centres are closed for the substitute holiday. They open again on Tuesday.", "은행, 관공서, 고객센터는 대체공휴일로 쉰다. 화요일에 다시 문을 연다.") },
                new NewsItem { title = L("Gas inspections next week", "다음 주 가스 점검"), text = L("Mapo City Gas checks villas in Mangwon-dong from 12 Oct. Inspectors carry ID and never collect money.", "마포도시가스가 12일부터 망원동 빌라를 점검한다. 점검원은 신분증을 지니며 돈을 받지 않는다.") },
                new NewsItem { title = L("Subway fares", "지하철 요금"), text = L("The base fare stays at 1,550 won through the end of the year.", "기본요금 1,550원이 연말까지 유지된다.") },
            };

            var v = StoreVariant("scam", Day0Dir, "Day0", Day0Call(), HouseholdPhone(), room, HouseholdDirectory());
            v.clues = new List<ClueDef>
            {
                Guided(Clue("paper", L("Today's paper warns about exactly this: callers who say a bill is unpaid and want a transfer.", "오늘 신문이 바로 이 수법을 경고한다. 요금이 미납됐다며 이체를 요구하는 전화."),
                            AtPaper, new ClueWhen(ClueEvent.PanelOpened, "Newspaper")),
                       L("Every morning's paper warns about the trick going round. Click the newspaper on the desk.", "매일 아침 신문에는 요즘 도는 수법이 실린다. 책상 위 신문을 클릭하세요.")),
                Guided(Clue("caller_number", L("Her number is a prepaid phone opened three days ago, with four scam reports. It is not the gas company's.", "발신 번호는 3일 전 개통된 선불폰이고 사기 신고가 4건 있다. 가스 회사 번호가 아니다."),
                            AtNumberCheck, new ClueWhen(ClueEvent.NumberChecked, GasScamNumber)),
                       L("The computer shows who really owns a number. Click the glowing laptop, then click her number under Check a phone number.", "컴퓨터로 그 번호의 진짜 주인을 볼 수 있다. 빛나는 노트북을 클릭한 뒤, '전화번호 조회'에서 상대의 번호를 클릭하세요.")),
                Guided(Clue("account_owner", L("The \"collection account\" belongs to a private person, HAN SUNGMIN, not to Mapo City Gas.", "'수납 계좌'의 예금주는 마포도시가스가 아니라 개인 '한성민'이다."),
                            AtAccountCheck, new ClueWhen(ClueEvent.NumberChecked, GasScamAccount), new ClueWhen(ClueEvent.RecipientShown, GasScamAccount)),
                       L("The computer shows whose account it is, too. Click the laptop, choose Check a bank account, then click the account she gave you.", "컴퓨터로 누구 계좌인지도 알 수 있다. 노트북을 클릭하고 '계좌 조회'를 고른 뒤, 상대가 불러 준 계좌를 클릭하세요.")),
                Guided(Clue("bill", L("The gas bill in your drawer is stamped PAID (auto-pay, 28 September), and the company's real number on it is 02-555-0181.", "서랍 속 가스 고지서에는 '납부완료' 도장이 찍혀 있고(9월 28일 자동이체), 회사의 진짜 번호는 02-555-0181이다."),
                            AtGasBill, new ClueWhen(ClueEvent.DocumentViewed, GasBillTitle)),
                       L("Your own papers are in the desk drawer. Open it and find the gas bill.", "내 서류는 책상 서랍에 있다. 서랍을 열고 가스 고지서를 찾아보세요.")),
                Guided(Clue("calendar", L("Your calendar says today is a public holiday. Offices are closed, yet \"the gas company\" is calling.", "달력을 보니 오늘은 대체공휴일이다. 회사들이 쉬는 날인데 '가스 회사'에서 전화가 왔다."),
                            AtCalendar, new ClueWhen(ClueEvent.PanelOpened, "Calendar")),
                       L("The calendar on the wall shows what day it is and what you wrote down. Click it.", "벽에 걸린 달력에는 오늘이 무슨 날인지, 내가 적어 둔 일정이 있다. 달력을 클릭하세요.")),
                Guided(Clue("bank", L("Your bank history shows 18,420 won went to Mapo City Gas on 28 September. The bill is paid.", "은행 내역에 9월 28일 마포도시가스로 18,420원이 나간 기록이 있다. 요금은 이미 냈다."),
                            AtBank, new ClueWhen(ClueEvent.BankOpened)),
                       L("Your bank app shows what really left your account. On the phone, press Apps, then open Nuri Bank.", "은행 앱에서 실제로 빠져나간 돈을 볼 수 있다. 휴대폰에서 '앱'을 누른 뒤 누리은행을 여세요.")),
                Guided(Clue("chat", L("In the residents' chat the landlord warns everyone: the gas company never phones for money.", "입주민 채팅방에서 집주인이 모두에게 경고했다. 가스 회사는 전화로 돈을 요구하지 않는다고."),
                            AtVillaChat, new ClueWhen(ClueEvent.ChatRead, VillaChat)),
                       L("People you know write to you themselves. Go back to Apps, open Chats and read the residents' chat.", "아는 사람들은 직접 글을 남긴다. '앱'으로 돌아가 '채팅'을 열고 입주민 채팅방을 읽어 보세요.")),
                Guided(Clue("contacts", L("The caller is not in your contacts. A number you saved would show its name when it rings.", "상대는 연락처에 없는 번호다. 저장된 번호라면 전화가 올 때 이름이 뜬다."),
                            AtContacts, new ClueWhen(ClueEvent.ContactsOpened)),
                       L("A number you saved shows its name when it calls. Go back to Apps and open Contacts: is she there?", "저장된 번호는 전화가 올 때 이름이 뜬다. '앱'으로 돌아가 '연락처'를 열어 보세요. 상대가 있나요?")),
            };
            v.papers = Day0Papers();
            v.ruleTitle = L("Check before you pay", "보내기 전에 확인하라");
            v.rule = L("A caller's story is only a story. The number, the account, your own papers, your calendar, your bank and your chats tell you what is true. Check them, then decide.",
                       "전화 건 사람의 말은 말일 뿐이다. 번호, 계좌, 내 서류, 달력, 은행 내역, 채팅이 사실을 알려 준다. 확인한 다음 결정하라.");
            v.ruleSource = PaperSource(6);

            var d = ScriptableObject.CreateInstance<DayData>();
            d.day = 0;
            d.tutorial = true;
            d.dateLabel = DateLabel(today);
            d.shortLabel = ShortLabel(0, today);
            d.weekday = WeekdayLabel(today);
            d.place = L("Mangwon-dong, Seoul", "서울 마포구 망원동");
            d.startTime = "14:10";
            d.intro = L("A holiday Monday, your first autumn living on your own. This first call is practice: the yellow note shows you every place worth checking.",
                        "월요일 대체공휴일, 혼자 살며 맞는 첫 가을. 첫 전화는 연습이다. 노란 메모가 확인할 곳을 하나씩 알려 준다.");
            d.ringDelay = 7f;
            d.variants = new List<DayVariant> { v };
            d.nextDateLabel = DateLabel(today.AddDays(1));
            d.builtWith = Version;
            return Store(d, Localized(Day0Dir + "/Day0.asset"));
        }

        /// <summary>The clue with what the tutorial guide says to find it.</summary>
        static ClueDef Guided(ClueDef clue, string guide)
        {
            clue.guide = guide;
            return clue;
        }

        static List<EndPaper> Day0Papers()
        {
            string lead = L("A woman calling herself \"Team Leader Baek\" of Mapo City Gas's billing team phoned residents of Mangwon-dong on Monday's holiday. She said their September gas bill was unpaid, and that the gas would be shut off at five unless they transferred the money at once.",
                            "월요일 대체공휴일, 마포도시가스 요금팀 '백 팀장'을 자칭한 여성이 망원동 주민들에게 전화를 걸었다. 그는 9월 가스 요금이 미납됐다며 당장 이체하지 않으면 오후 5시에 가스가 끊긴다고 말했다.");
            string quote = L("\"We do not phone customers to collect bills, and our offices were closed for the holiday,\" Mapo City Gas said. \"Our only number is 02-555-0181, the one printed on the bill.\"",
                             "마포도시가스는 \"전화로 요금을 걷지 않으며, 그날은 공휴일이라 사무실도 쉬었다\"며 \"회사 번호는 고지서에 적힌 02-555-0181 하나뿐\"이라고 밝혔다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse,
                      L("Holiday \"gas bill\" calls were fake", "공휴일 '가스 요금' 전화는 가짜였다"),
                      L("Residents who checked their bill and their bank history hung up.", "고지서와 은행 내역을 확인한 주민들은 전화를 끊었다."),
                      lead + "\n\n" + L("Most of those called had already paid by automatic transfer. Police say the account she gave, Hanbit Bank 620-339-104772, is in the name of a man in his twenties who sold it to the ring. Anyone called by 010-7359-2046 can report it to 112 or the fraud hotline 1332.",
                                        "전화를 받은 주민 대부분은 이미 자동이체로 요금을 낸 상태였다. 경찰에 따르면 상대가 불러 준 한빛은행 620-339-104772 계좌는 조직에 통장을 판 20대 남성의 명의다. 010-7359-2046으로 전화를 받은 사람은 112나 금융감독원 1332에 신고하면 된다.") + "\n\n" + quote,
                      L("You hung up. The bill had been paid on 28 September, and the account was a stranger's.", "전화를 끊었다. 요금은 9월 28일에 이미 납부됐고, 계좌는 모르는 사람 것이었다.")),
                Paper(Outcome.GoAlong,
                      L("Paid twice: a gas bill that was never due", "두 번 낸 가스 요금"),
                      L("A Mangwon student transferred 18,420 won to a stranger on the holiday.", "망원동 대학생, 공휴일에 모르는 사람에게 18,420원 송금"),
                      lead + "\n\n" + L("A 22-year-old student sent the money. Her real bill had been paid by automatic transfer a week earlier; the 18,420 won went to an account in the name of Han Sungmin and was gone within minutes. \"Small amounts are how they find out who will pay,\" a police officer said. \"The next call asks for more.\"",
                                        "22세 대학생 한 명은 돈을 보냈다. 진짜 요금은 일주일 전 자동이체로 이미 납부된 상태였다. 18,420원은 한성민 명의 계좌로 들어가 몇 분 만에 사라졌다. 경찰 관계자는 \"소액은 누가 돈을 보내는지 떠보는 수법\"이라며 \"다음 전화는 더 큰돈을 요구한다\"고 말했다.") + "\n\n" + quote,
                      L("The bill was already paid. The drawer, the bank app and the computer each said so.", "요금은 이미 낸 상태였다. 서랍, 은행 앱, 컴퓨터가 모두 그렇게 말하고 있었다.")),
                Paper(Outcome.Timeout,
                      L("Fake \"gas bill\" calls go on through the holiday", "공휴일 내내 이어진 가짜 '가스 요금' 전화"),
                      L("The gas stayed on. Few reported the number.", "가스는 끊기지 않았다. 신고는 적었다."),
                      lead + "\n\n" + L("Five o'clock came and went, and nobody's gas was shut off. Police say the caller kept dialling until the evening, and ask anyone called by 010-7359-2046 to report it to 112 or the fraud hotline 1332.",
                                        "오후 5시가 지났지만 가스가 끊긴 집은 없었다. 경찰은 상대가 저녁까지 전화를 계속 걸었다며, 010-7359-2046으로 전화를 받은 사람은 112나 1332에 신고해 달라고 당부했다.") + "\n\n" + quote,
                      L("Nothing was sent, but you never gave an answer. Check, then hang up yourself.", "돈은 보내지 않았지만 끝내 답을 하지 않았다. 확인한 뒤 직접 끊었어야 했다.")),
            };
        }

        // ================================================================ the call

        static ConversationData Day0Call()
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = L("Day 0 · The unpaid gas bill (tutorial)", "0일차 · 미납 가스 요금 (튜토리얼)");
            c.isScam = true;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = L("Team Leader Baek", "백 팀장"), number = GasScamNumber, inContacts = false, portrait = "pt_billing", voice = VoiceBilling };
            c.claims = new List<string>
            {
                L("She is Team Leader Baek from the billing team at Mapo City Gas.", "마포도시가스 요금팀 백 팀장이라고 한다."),
                L("My September gas bill, 18,420 won, is unpaid.", "9월 가스 요금 18,420원이 미납이라고 한다."),
                L("The gas goes off at 17:00 today unless I transfer it now.", "지금 이체하지 않으면 오늘 17시에 가스가 끊긴다고 한다."),
                L("Collection account: Hanbit Bank 620-339-104772.", "수납 계좌: 한빛은행 620-339-104772"),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 60f,
                      L("Unpaid? I thought it was on auto-pay.", "미납이요? 자동이체로 나가는 줄 알았는데요."), "autopay",
                      L("Shut off today? But today is a holiday.", "오늘 끊긴다고요? 오늘 공휴일인데요."), "holiday",
                      P(0.5f, L("Ma'am? Can you hear me?", "고객님? 들리세요?"))),
                    Caller(L("Hello, is this Kim Jiwoo at Mangwon Heights 302? This is Team Leader Baek from the billing team at Mapo City Gas.",
                             "여보세요, 망원하이츠 302호 김지우 고객님 맞으시죠? 마포도시가스 요금팀 백 팀장입니다."),
                           L("Hello, is this Kim Jiwoo at Mangwon Heights three oh two? This is Team Leader Baek from the billing team at Mapo City Gas.",
                             "여보세요, 망원하이츠 삼백이 호 김지우 고객님 맞으시죠? 마포도시가스 요금팀 백 팀장입니다.")),
                    Caller(L("Your September gas bill, 18,420 won, has not been paid. The gas to your unit will be shut off at five o'clock today.",
                             "9월 가스 요금 18,420원이 미납 상태입니다. 오늘 오후 5시에 댁의 가스 공급이 차단될 예정이에요."),
                           L("Your September gas bill, eighteen thousand, four hundred and twenty won, has not been paid. The gas to your unit will be shut off at five o'clock today.",
                             "구월 가스 요금 만 팔천사백이십 원이 미납 상태입니다. 오늘 오후 다섯 시에 댁의 가스 공급이 차단될 예정이에요."))),
                Node("autopay", "ask",
                    Caller(L("The auto-payment failed, ma'am. It happens when a bank updates its system.", "자동이체가 실패했습니다, 고객님. 은행 전산 점검 때 종종 그래요."))),
                Node("holiday", "ask",
                    Caller(L("The shut-off is automatic, ma'am. The system doesn't take holidays.", "차단은 자동으로 진행됩니다, 고객님. 전산은 공휴일에도 돌아가거든요."))),
                Hold("ask",
                    Caller(L("To stop it, please transfer 18,420 won to our collection account: Hanbit Bank 620-339-104772.", "차단을 막으려면 수납 계좌로 18,420원을 이체해 주세요. 한빛은행 620-339-104772입니다."),
                           L("To stop it, please transfer eighteen thousand, four hundred and twenty won to our collection account. Hanbit Bank, " + GasScamAccountSpoken + ".",
                             "차단을 막으려면 수납 계좌로 만 팔천사백이십 원을 이체해 주세요. 한빛은행, " + GasScamAccountSpoken + "입니다."),
                           F(FactKind.Account, GasScamAccount), F(FactKind.Amount, "18,420")),
                    Caller(L("As soon as it arrives, I'll cancel the shut-off. I'll stay on the line while you send it.", "입금이 확인되는 대로 차단을 취소해 드릴게요. 보내시는 동안 기다리겠습니다."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("The unpaid gas bill", "미납 가스 요금"),
                claimedIdentity = L("Team Leader Baek, Mapo City Gas billing team", "마포도시가스 요금팀 백 팀장"),
                ask = L("Transfer ₩18,420 to a \"collection account\": Hanbit Bank 620-339-104772", "'수납 계좌'로 18,420원 이체: 한빛은행 620-339-104772"),
                deadline = "17:00",
                deadlineReason = L("before the gas is shut off", "가스 차단 전"),
                objective = L("Is this really the gas company? Follow the yellow note: it shows you every place to check. Then give your verdict on the call: send the money, or hang up.",
                              "정말 가스 회사일까? 노란 메모를 따라 확인할 곳을 하나씩 살펴본 뒤, 통화 화면에서 판정을 내리자: 돈을 보낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Pay the bill", "요금 보내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("All right, I'm sending it now.", "알겠어요, 지금 보낼게요."),
                refuseLine = L("My bill is already paid. Goodbye.", "요금은 이미 냈어요. 끊을게요."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask whose name is on the account", "누구 명의 계좌인지 묻기"), L("Whose name is on that account?", "그 계좌는 누구 명의예요?"), null,
                  Caller(L("It's our collection officer's, Han Sungmin. On a holiday, payments go through his account.", "수납 담당자 한성민 명의입니다. 공휴일에는 담당자 계좌로 받고 있어요."))),
                Q(L("Ask why she isn't calling from the customer centre", "왜 고객센터 번호로 전화하지 않았는지 묻기"), L("Why aren't you calling from the customer centre?", "왜 고객센터 번호로 전화하지 않으셨어요?"), null,
                  Caller(L("The centre is closed today, ma'am, so I'm calling from my work mobile.", "오늘은 고객센터가 쉬는 날이라 업무용 휴대폰으로 연락드렸어요."))),
                Q(L("Say you'll check your bill first", "고지서부터 확인하겠다고 하기"), L("Let me check my bill first.", "고지서부터 확인해 볼게요."), null,
                  Caller(L("Of course, ma'am, but please be quick. The shut-off order is already in the system.", "네, 확인하세요. 하지만 서둘러 주셔야 해요. 차단 접수가 이미 들어가 있어서요."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("14:35", Caller(L("Ma'am? Are you still there?", "고객님? 듣고 계세요?"))),
                Beat("15:10", Caller(L("I'm still holding, ma'am. The shut-off is set for five.", "아직 기다리고 있습니다. 차단은 5시로 잡혀 있어요."),
                                     L(null, "아직 기다리고 있습니다. 차단은 다섯 시로 잡혀 있어요."))),
                Beat("16:00", Caller(L("One hour left, ma'am. After that I can't stop it.", "한 시간 남았습니다. 그 뒤엔 저도 막을 수 없어요."))),
                Beat("16:40", Caller(L("Twenty minutes. Please send it now.", "20분 남았어요. 지금 보내 주세요."), L(null, "이십 분 남았어요. 지금 보내 주세요."))),
                Beat("16:55", Caller(L("Five minutes, ma'am!", "5분 남았습니다!"), L(null, "오 분 남았습니다!"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = L("You hung up. The gas bill had been paid on 28 September, and the \"collection account\" belongs to Han Sungmin.",
                                    "전화를 끊었다. 가스 요금은 9월 28일에 이미 납부됐고, '수납 계좌'는 한성민 명의였다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = new List<ConvLine>
                    {
                        Caller(L("It's five o'clock, ma'am.", "5시가 됐습니다, 고객님."), L(null, "다섯 시가 됐습니다, 고객님.")),
                        Caller(L("Fine. Don't blame us when the gas goes off.", "알겠습니다. 가스가 끊겨도 저희 탓은 하지 마세요.")),
                    },
                    consequence = L("She gave up at five. The gas stayed on, but you never gave her an answer.", "상대는 5시에 포기했다. 가스는 끊기지 않았지만, 끝내 답을 하지 않았다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -GasBill,
                    lines = new List<ConvLine>
                    {
                        Caller(L("Thank you, ma'am. I've cancelled the shut-off.", "감사합니다, 고객님. 차단은 취소해 드렸어요.")),
                        Caller(L("Enjoy the rest of your holiday.", "남은 연휴 잘 보내세요.")),
                    },
                    consequence = L("The 18,420 won went to Han Sungmin's account. Your real bill had been paid a week ago.", "18,420원은 한성민 명의 계좌로 들어갔다. 진짜 요금은 일주일 전에 이미 납부된 상태였다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = GasScamAccount, bank = HanbitBank, amount = GasBill, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }
    }
}
