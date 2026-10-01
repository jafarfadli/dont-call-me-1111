using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 2, Wednesday 7 October, rent day. Its first case (Medium): "Choi Hyunwoo, the landlord's
    /// son" says his father went into hospital this morning and asks for the rent on a new account.
    /// The day's other case is "Minjun's broken phone" (ContentBuilder.Day2Brother.cs). Both truths
    /// share the caller, the story and the ask; three clues tell them apart, and none of them is a
    /// pile of reports any more:
    /// <list type="bullet">
    /// <item>Legit: the caller's number is the son's in the lease, the account is in his name, and the
    /// landlord announced it in the residents' chat (Jiwoo noted it on the calendar).</item>
    /// <item>Scam: a new prepaid number, an account in a stranger's name, and the landlord chatting
    /// from home all afternoon.</item>
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
            d.dateLabel = DateLabel(today);
            d.shortLabel = ShortLabel(2, today);
            d.weekday = WeekdayLabel(today);
            d.place = L("Mangwon-dong, Seoul", "서울 마포구 망원동");
            d.startTime = "16:20";
            d.intro = L("Rent day. You meant to send it to Mr. Choi after class, like every month.", "월세 내는 날. 매달 그랬듯 수업 끝나고 최 사장님께 보낼 생각이었다.");
            d.ringDelay = 7f;
            d.variants = new List<DayVariant> { scam, legit, BrotherVariant(true), BrotherVariant(false) };
            d.echoes = Day2Echoes();
            d.nextDateLabel = DateLabel(today.AddDays(1));
            d.builtWith = Version;
            return Store(d, Localized(Day2Dir + "/Day2.asset"));
        }

        static DayVariant Day2Variant(bool scam)
        {
            SetDay(2, "16:20");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();
            string account = scam ? Day2ScamAccount : Day2LegitAccount;
            string bank = scam ? HanbitBank : NuriBank;

            // ---- the paper on the desk (its front page is yesterday's case once Day 1 is played)
            var n = room.newspaper;
            var fallback = Day1Papers().Find(p => p.outcome == Outcome.Refuse);
            n.headline = fallback.headline;
            n.subhead = fallback.subhead;
            n.body = new List<string>(fallback.body.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries));
            n.warningTitle = L("RENT DAY: CHECK THE NEW ACCOUNT", "월세 날: 새 계좌를 확인하라");
            n.warningText = L("Tenants across Mapo report calls about a \"new rent account\" around rent day. Not every bad account has reports yet, so check who is behind it: is the account in the name of your landlord or someone your lease names? Is the caller's number in your lease?",
                              "마포 곳곳에서 월세 날 무렵 '새 월세 계좌'를 안내하는 전화가 잇따르고 있다. 나쁜 계좌라고 다 신고가 있는 건 아니다. 누구 계좌인지 확인하라. 예금주가 집주인이나 계약서에 적힌 사람인가? 발신 번호가 계약서에 있는가?");
            n.local = Day2Local();

            // ---- the truth: in the residents' chat, on the calendar and in what CheckFirst knows
            if (scam)
            {
                AddChat(phone, VillaChat, 7, "13:40", Landlord, "pt_landlord",
                        L("Thank you all for keeping the stairs so clean! I'm fixing the light on the second floor now. See you at the water tank cleaning tomorrow. – Choi, 1F",
                          "계단 깨끗하게 써 주셔서 다들 고마워요! 지금 2층 전등 고치는 중이에요. 내일 물탱크 청소 때 봬요. - 1층 최"));
                AddChat(phone, VillaChat, 7, "15:52", Landlord, "pt_landlord",
                        L("The light is fixed. Rent as always, please: Nuri Bank 110-771-209944.", "전등 다 고쳤어요. 월세는 늘 보내던 누리은행 110-771-209944로 부탁해요."));
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = L("SEO JIYEON", "서지연"), note = L("Personal account, opened 12 days ago", "개인 계좌 · 개설 12일") });
                directory.numbers.Add(new DirNumber { number = Day2ScamNumber, owner = L("Not registered", "미등록"), note = L("Prepaid phone, opened 6 days ago", "선불폰 · 개통 6일") });
            }
            else
            {
                AddChat(phone, VillaChat, 7, "08:10", Landlord, "pt_landlord",
                        L("Good morning everyone. I'm going into Severance Hospital today for my knee, back next week. My son Hyunwoo will look after the building and collect this month's rent: Nuri Bank 110-771-202358 (Choi Hyunwoo). He'll call you today. Thank you! – Choi, 1F",
                          "다들 좋은 아침이에요. 무릎 때문에 오늘 세브란스에 입원해서 다음 주에 돌아옵니다. 그동안 아들 현우가 건물을 봐주고 이번 달 월세도 받을 거예요: 누리은행 110-771-202358 (최현우). 오늘 중으로 연락드릴 겁니다. 고마워요! - 1층 최"));
                AddChat(phone, VillaChat, 7, "08:42", L("201", "201호"), "pt_unknown", L("Get well soon, Mr. Choi!", "사장님 빨리 쾌차하세요!"));
                Note(room, 7, L("Mr. Choi in hospital this week. His son Hyunwoo collects the rent (see the residents' chat)", "최 사장님 이번 주 입원. 월세는 아들 현우 씨가 받음 (입주민 채팅 참고)"));
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = L("CHOI HYUNWOO", "최현우"), note = L("Personal account, opened 2016", "개인 계좌 · 2016년 개설") });
            }

            var v = StoreVariant(scam ? "scam" : "legit", CaseRent, Day2Dir, scam ? "Day2_Scam" : "Day2_Legit", Day2Call(scam), phone, room, directory);
            v.clues = scam
                ? new List<ClueDef>
                {
                    Clue("number", L("The caller's number isn't the son's: your lease gives 010-2280-6614, and CheckFirst shows the caller's as a prepaid phone opened six days ago.",
                                     "발신 번호는 아들 번호가 아니다. 계약서의 아들 번호는 010-2280-6614이고, 체크퍼스트에는 발신 번호가 6일 전 개통된 선불폰으로 나온다."),
                         L("Desk drawer → Lease contract, or Computer → Check a phone number", "책상 서랍 → 임대차 계약서, 또는 컴퓨터 → 전화번호 조회"),
                         new ClueWhen(ClueEvent.DocumentViewed, LeaseTitle), new ClueWhen(ClueEvent.NumberChecked, Day2ScamNumber)),
                    Clue("account_owner", L("The \"new rent account\" is in a stranger's name: SEO JIYEON.", "'새 월세 계좌'는 모르는 사람 명의다: 서지연."), AtAccountCheck,
                         new ClueWhen(ClueEvent.NumberChecked, account), new ClueWhen(ClueEvent.RecipientShown, account)),
                    Clue("landlord", L("Mr. Choi was writing in the residents' chat this afternoon: at home, fixing a light, asking for the rent on the usual account.",
                                       "최 사장님은 오후에 입주민 채팅에 글을 올렸다. 집에서 전등을 고치고 있었고, 월세는 늘 보내던 계좌로 달라고 했다."), AtVillaChat,
                         new ClueWhen(ClueEvent.ChatRead, VillaChat)),
                }
                : new List<ClueDef>
                {
                    Clue("number", L("The caller's number, 010-2280-6614, is the son's number in your lease, registered to Choi Hyunwoo.", "발신 번호 010-2280-6614는 계약서에 적힌 아들 번호이고, 최현우 명의다."),
                         L("Desk drawer → Lease contract, or Computer → Check a phone number", "책상 서랍 → 임대차 계약서, 또는 컴퓨터 → 전화번호 조회"),
                         new ClueWhen(ClueEvent.DocumentViewed, LeaseTitle), new ClueWhen(ClueEvent.NumberChecked, Day2LegitNumber)),
                    Clue("account_owner", L("The new account is in Choi Hyunwoo's name, the son your lease names.", "새 계좌는 계약서에 적힌 아들 최현우 명의다."), AtAccountCheck,
                         new ClueWhen(ClueEvent.NumberChecked, account), new ClueWhen(ClueEvent.RecipientShown, account)),
                    Clue("landlord", L("Mr. Choi wrote in the residents' chat this morning that he is in hospital and that Hyunwoo collects this month's rent on this account.",
                                       "최 사장님이 오늘 아침 입주민 채팅에 입원 소식과, 이번 달 월세는 현우 씨가 이 계좌로 받는다는 글을 올렸다."),
                         L("Chats → Mangwon Heights residents, or the calendar on the wall", "채팅 → 망원하이츠 입주민, 또는 벽에 걸린 달력"),
                         new ClueWhen(ClueEvent.ChatRead, VillaChat), new ClueWhen(ClueEvent.PanelOpened, "Calendar")),
                };
            v.papers = scam ? Day2ScamPapers() : Day2LegitPapers();
            v.ruleTitle = scam ? L("A new rent account? Check the lease", "새 월세 계좌? 계약서를 확인하라") : L("Checking works both ways", "확인은 양쪽 모두를 위한 것");
            v.rule = scam
                ? L("No reports doesn't mean safe. Check the caller's number and the account holder's name against your lease, and ask the landlord's own chat.",
                    "신고가 없다고 안전한 건 아니다. 발신 번호와 예금주 이름을 계약서와 대조하고, 집주인이 직접 쓴 글을 확인하라.")
                : L("When the number in your lease, the landlord's own message and the name on the account all match, the change is real. Check, then act.",
                    "계약서의 번호, 집주인이 직접 쓴 글, 예금주 이름이 모두 맞으면 변경은 진짜다. 확인하고, 그다음에 행동하라.");
            v.ruleSource = PaperSource(8);
            return v;
        }

        /// <summary>Wednesday's local news, the same whichever case the day brings.</summary>
        static List<NewsItem> Day2Local() => new List<NewsItem>
        {
            News(L("Water tank cleaning tomorrow", "내일 물탱크 청소"), L("Villas on Poeun-ro will be without water on Thursday, 10:00-12:00.", "포은로 일대 빌라가 목요일 10:00~12:00 단수된다.")),
            News(L("Mangwon market by night", "망원시장 야시장"), L("The market stays open until 23:00 this Saturday, with street food and live music.", "이번 주 토요일 밤 11시까지 문을 열고 길거리 음식과 공연을 선보인다.")),
            News(L("Midterms", "중간고사"), L("Hanbit University's library opens around the clock from Friday.", "한빛대학교 도서관이 금요일부터 24시간 개방된다.")),
        };

        // ================================================================ the call

        static ConversationData Day2Call(bool scam)
        {
            string number = scam ? Day2ScamNumber : Day2LegitNumber;
            string account = scam ? Day2ScamAccount : Day2LegitAccount;
            string bank = scam ? HanbitBank : NuriBank;
            string accountSpoken = scam ? L("six two zero, one one eight, four four nine zero two seven", "육이공, 일일팔, 사사구공이칠")
                                        : L("one one zero, seven seven one, two zero two three five eight", "일일공, 칠칠일, 이공이삼오팔");

            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? L("Day 2 · The new rent account (H3, scam)", "2일차 · 새 월세 계좌 (H3, 사기)") : L("Day 2 · The new rent account (M5, legit)", "2일차 · 새 월세 계좌 (M5, 진짜)");
            c.isScam = scam;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = L("Choi Hyunwoo", "최현우"), number = number, inContacts = false, portrait = "pt_hyunwoo", voice = VoiceHyunwoo };
            c.claims = new List<string>
            {
                L("He is Choi Hyunwoo, the landlord's son.", "집주인 아들 최현우라고 한다."),
                L("Mr. Choi went into hospital this morning (his knee).", "최 사장님이 오늘 아침 무릎 때문에 입원했다고 한다."),
                L($"This month the rent goes to his account: {bank} {account}.", $"이번 달 월세는 자기 계좌로 보내 달라고 한다: {bank} {account}"),
                L("He needs it before 17:00 to pay the hospital bill.", "병원비 때문에 17시 전까지 필요하다고 한다."),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 45f,
                      L("Oh no. Is he okay?", "어머, 괜찮으세요?"), "okay",
                      L("Sorry, who is this again?", "죄송한데, 누구시라고요?"), "who",
                      P(0.5f, L("Jiwoo-ssi? Can you hear me?", "지우 씨? 제 말 들리세요?"))),
                    Caller(L("Hello, is this Kim Jiwoo in 302? This is Choi Hyunwoo, Mr. Choi's son, from the first floor.", "안녕하세요, 302호 김지우 씨 맞으시죠? 1층 최 사장님 아들 최현우입니다."),
                           L("Hello, is this Kim Jiwoo in three oh two? This is Choi Hyunwoo, Mr. Choi's son, from the first floor.", "안녕하세요, 삼백이 호 김지우 씨 맞으시죠? 일 층 최 사장님 아들 최현우입니다.")),
                    Caller(L("Sorry to call out of the blue. My father went into hospital this morning. It's his knee. Nothing serious, but he'll be there all week.",
                             "갑자기 전화드려 죄송해요. 아버지가 오늘 아침에 입원하셨어요. 무릎 때문인데 큰일은 아니고, 이번 주 내내 병원에 계실 거예요."))),
                Node("okay", "ask",
                    Caller(L("He's fine, thanks. Grumpy about the hospital food.", "네, 괜찮으세요. 병원 밥이 맛없다고 투덜대시긴 하지만요."))),
                Node("who", "ask",
                    Caller(L("Choi Hyunwoo, the landlord's son. I'm on your lease, under 'if absent'.", "집주인 아들 최현우요. 계약서 '부재 시 연락처'에 제 이름 있을 거예요."),
                           L("Choi Hyunwoo, the landlord's son. I'm on your lease, under if absent.", "집주인 아들 최현우요. 계약서 부재 시 연락처에 제 이름 있을 거예요."))),
                Hold("ask",
                    Caller(L("So I'm looking after the building this week, and today's the seventh. Rent day.", "그래서 이번 주는 제가 건물을 관리하는데, 오늘이 7일이잖아요. 월세 날이요."),
                           L(null, "그래서 이번 주는 제가 건물을 관리하는데, 오늘이 칠 일이잖아요. 월세 날이요.")),
                    Caller(L("Dad can't use his banking app from the hospital, so this month the rent comes to me.", "아버지가 병원에서 은행 앱을 못 쓰셔서, 이번 달 월세는 제가 받기로 했어요.")),
                    Caller(L($"Please send the ₩450,000 to my account: {bank} {account}.", $"월세 45만 원은 제 계좌로 보내 주세요. {bank} {account}입니다."),
                           L($"Please send the four hundred and fifty thousand won to my account. {bank}, {accountSpoken}.", $"월세 사십오만 원은 제 계좌로 보내 주세요. {bank}, {accountSpoken}입니다."),
                           F(FactKind.Account, account), F(FactKind.Amount, "450,000")),
                    Caller(L("I'm paying his hospital bill with the rents, and the billing desk closes at five. Could you send it before then? I'll hold.",
                             "월세로 병원비를 내야 하는데 원무과가 5시에 닫아요. 그 전에 보내 주실 수 있을까요? 기다릴게요."),
                           L(null, "월세로 병원비를 내야 하는데 원무과가 다섯 시에 닫아요. 그 전에 보내 주실 수 있을까요? 기다릴게요."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("The new rent account", "새 월세 계좌"),
                claimedIdentity = L("Choi Hyunwoo, the landlord's son", "집주인 아들 최현우"),
                ask = L($"Pay this month's rent, ₩450,000, into a new account: {bank} {account}", $"이번 달 월세 45만 원을 새 계좌로 입금: {bank} {account}"),
                deadline = "17:00",
                deadlineReason = L("before the hospital's billing desk closes", "병원 원무과 마감 전"),
                objective = L("Is he really the landlord's son, and did the rent account really change? Check before 17:00, then give your verdict on the call: pay the rent, or hang up.",
                              "정말 집주인 아들일까? 월세 계좌가 정말 바뀌었을까? 17:00 전까지 확인한 뒤, 통화 화면에서 판정을 내리자: 월세를 보낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Pay the rent", "월세 보내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("Okay, I'm sending the rent now.", "네, 지금 월세 보낼게요."),
                refuseLine = L("I'm not paying rent into a new account over the phone. Goodbye.", "전화로 새 계좌에 월세를 보낼 수는 없어요. 끊을게요."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask whose name is on the account", "누구 명의 계좌인지 묻기"), L("Whose name is on that account?", "그 계좌는 누구 명의예요?"), null,
                  scam
                      ? Caller(L("My wife's, Seo Jiyeon. Mine's a business account, it takes a day to clear.", "제 아내 서지연 명의예요. 제 건 사업자 계좌라 입금 확인이 하루 걸려서요."))
                      : Caller(L("Mine. Choi Hyunwoo. Same as on your lease.", "제 거예요. 최현우. 계약서에 있는 이름 그대로요."))),
                Q(L("Ask why the account changed", "왜 계좌가 바뀌었는지 묻기"), L("Why isn't it going to your father's account like always?", "왜 평소처럼 아버님 계좌로 안 보내요?"), null,
                  scam
                      ? Caller(L("Just for this month. Dad didn't want to bother everyone with a notice.", "이번 달만이에요. 아버지가 공지까지 하긴 번거롭다고 하셔서요."))
                      : Caller(L("Dad can't get into his banking app from the ward. He wrote it in the residents' chat this morning, before he went in.",
                                 "아버지가 병실에서 은행 앱을 못 쓰세요. 오늘 아침 입원하시기 전에 입주민 채팅방에 올리셨어요."))),
                Q(L("Ask about the number he's calling from", "지금 거는 번호에 대해 묻기"), L("Is this your own number?", "이거 본인 번호 맞아요?"), null,
                  scam
                      ? Caller(L("It's my work phone. My own one is the number on your lease, but the screen's broken.", "이건 회사 폰이에요. 계약서에 있는 건 제 개인 번호인데 액정이 깨져서요."))
                      : Caller(L("Yes. It's the one on your lease, under 'if absent'.", "네. 계약서 '부재 시 연락처'에 있는 그 번호예요."),
                               L("Yes. It's the one on your lease, under if absent.", "네. 계약서 부재 시 연락처에 있는 그 번호예요."))),
                Q(L("Ask to speak to Mr. Choi", "최 사장님과 통화하고 싶다고 하기"), L("Can I talk to your father?", "아버님이랑 통화할 수 있을까요?"), null,
                  scam
                      ? Caller(L("He's in surgery right now. Please, I don't want to worry him.", "지금 수술 중이세요. 걱정 끼쳐 드리고 싶지 않아요."))
                      : Caller(L("He's asleep after the operation. Look in the residents' chat: he wrote there this morning.",
                                 "수술 끝나고 주무세요. 입주민 채팅방 보세요. 오늘 아침에 올리셨어요."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:32", Caller(L("Jiwoo-ssi? Still there? No rush, but the desk closes at five.", "지우 씨? 아직 계세요? 재촉하는 건 아닌데 원무과가 5시에 닫아서요."),
                                     L(null, "지우 씨? 아직 계세요? 재촉하는 건 아닌데 원무과가 다섯 시에 닫아서요."))),
                Beat("16:40", Caller(L("The nurse is asking me about the bill again. Any luck with the transfer?", "간호사가 또 병원비 얘기를 하네요. 이체는 어떻게 되고 있어요?"))),
                Beat("16:47", Caller(L("The hospital just called me again about the bill. Have you sent it?", "병원에서 방금 또 병원비 얘기로 전화 왔어요. 보내셨어요?"))),
                Beat("16:53", Caller(L("Seven minutes, Jiwoo-ssi. Please.", "7분 남았어요, 지우 씨. 부탁드려요."), L(null, "칠 분 남았어요, 지우 씨. 부탁드려요."))),
                Beat("16:57", Caller(L("Three minutes! Please, just send it.", "3분 남았어요! 제발 보내 주세요."), L(null, "삼 분 남았어요! 제발 보내 주세요."))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? L("You hung up. Mr. Choi wasn't in hospital: he was writing in the residents' chat all afternoon, and his son's real number is in your lease.",
                            "전화를 끊었다. 최 사장님은 입원하지 않았다. 오후 내내 입주민 채팅방에 글을 올리고 있었고, 아들의 진짜 번호는 계약서에 있었다.")
                        : L("You hung up on the real Choi Hyunwoo. His father had announced the new account in the residents' chat that morning.",
                            "진짜 최현우의 전화를 끊어 버렸다. 아버지가 그날 아침 입주민 채팅방에 새 계좌를 알렸었다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("It's five. Forget it. You'll get a late notice.", "다섯 시네요. 됐어요. 연체 통지나 받으세요.")) }
                        : new List<ConvLine> { Caller(L("Jiwoo-ssi? It's five. The desk's closed. I'll sort it out some other way.", "지우 씨? 다섯 시예요. 원무과 닫았네요. 다른 방법을 찾아볼게요.")) },
                    consequence = scam
                        ? L("The caller gave up at five. You kept your money, but you never decided.", "상대는 다섯 시에 포기했다. 돈은 지켰지만 결정은 내리지 못했다.")
                        : L("Five o'clock came and went. Hyunwoo paid the hospital from his own savings, and your rent is still due.", "다섯 시가 지나갔다. 현우 씨는 자기 돈으로 병원비를 냈고, 월세는 아직 밀려 있다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -Rent,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Received. Thank you... I'll send you a receipt.", "확인했습니다. 감사합니다… 영수증 보내 드릴게요.")) }
                        : new List<ConvLine> { Caller(L("Got it! Thank you, Jiwoo-ssi. I'll tell Dad you were the first.", "받았어요! 고마워요, 지우 씨. 아버지께 1등으로 보냈다고 전할게요."),
                                                      L(null, "받았어요! 고마워요, 지우 씨. 아버지께 일 등으로 보냈다고 전할게요.")) },
                    consequence = scam
                        ? L("The ₩450,000 went to Seo Jiyeon's account and was withdrawn within minutes. Mr. Choi was at home all afternoon, and the rent account had never changed.",
                            "45만 원은 서지연 명의 계좌로 들어가 몇 분 만에 인출됐다. 최 사장님은 오후 내내 집에 있었고, 월세 계좌는 바뀐 적이 없었다.")
                        : L("The rent reached Choi Hyunwoo's account. The change was real: the number in your lease, the name on the account and the landlord's own message all said so.",
                            "월세가 최현우 계좌에 들어갔다. 변경은 진짜였다. 계약서의 번호, 예금주 이름, 집주인이 직접 쓴 글이 모두 같은 말을 하고 있었다."),
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
            string lead = L("A man claiming to be the son of a Mangwon-dong landlord phoned tenants on rent day yesterday. His father was in hospital, he said, and this month's rent should go to a new account before 5 p.m.",
                            "어제 월세 날, 망원동의 한 집주인 아들을 자칭한 남성이 세입자들에게 전화를 걸었다. 그는 아버지가 입원했다며 이번 달 월세를 오후 5시 전까지 새 계좌로 보내 달라고 했다.");
            string landlord = L("The real landlord, Choi Youngsik, 71, was not in hospital: he spent the afternoon organising a water tank cleaning. \"My son's number is in every lease,\" he said. \"And the rent account only changes when I write it down.\"",
                                "진짜 집주인 최영식(71) 씨는 입원하지 않았다. 그는 오후 내내 물탱크 청소를 준비하고 있었다. 최 씨는 \"아들 번호는 모든 계약서에 적혀 있다\"며 \"월세 계좌는 내가 글로 써서 알릴 때만 바뀐다\"고 말했다.");
            string report = L("Police ask anyone called by 010-4127-8830 to report it to 112.", "경찰은 010-4127-8830으로 전화를 받은 사람은 112에 신고해 달라고 당부했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, L("Fake \"landlord's son\" calls tenants on rent day", "월세 날 '가짜 집주인 아들' 전화"),
                      L("The landlord was at home all along. Tenants who checked their lease kept their rent.", "집주인은 내내 집에 있었다. 계약서를 확인한 세입자들은 월세를 지켰다."),
                      lead + "\n\n" + landlord + "\n\n" + L("Police say the account the caller gave, Hanbit Bank 620-118-449027, belongs to a woman who sold her bank book online. Anyone called by 010-4127-8830 should report it to 112.",
                                                           "경찰에 따르면 남성이 불러 준 한빛은행 620-118-449027은 온라인에서 통장을 판 여성의 명의다. 010-4127-8830으로 전화를 받은 사람은 112에 신고해 달라고 경찰은 당부했다."),
                      L("You hung up and kept the rent. The lease had the son's real number, and the chat showed Mr. Choi at home.", "전화를 끊고 월세를 지켰다. 계약서에는 아들의 진짜 번호가 있었고, 채팅방은 최 사장님이 집에 있다는 걸 보여 줬다.")),
                Paper(Outcome.GoAlong, L("Tenant loses a month's rent to a fake landlord's son", "세입자, 가짜 집주인 아들에게 한 달 치 월세 날려"),
                      L("450,000 won went to a stranger's account. The landlord never asked for it.", "45만 원이 모르는 사람 계좌로 들어갔다. 집주인은 그런 요청을 한 적이 없다."),
                      lead + "\n\n" + L("A 22-year-old student sent 450,000 won to Hanbit Bank 620-118-449027. The account belongs to a woman who sold her bank book online; the money was withdrawn within minutes.",
                                        "22세 대학생 한 명이 한빛은행 620-118-449027로 45만 원을 보냈다. 이 계좌는 온라인에서 통장을 판 여성의 명의로, 돈은 몇 분 만에 인출됐다.") + "\n\n" + landlord + "\n\n" + report,
                      L("The account was in SEO JIYEON's name, and the caller's number wasn't the one in your lease.", "계좌는 '서지연' 명의였고, 발신 번호는 계약서의 번호가 아니었다.")),
                Paper(Outcome.Timeout, L("Rent-day calls: \"pay my new account\"", "월세 날 전화: '새 계좌로 보내 주세요'"),
                      L("Tenants in Mangwon kept their rent, but few reported the calls.", "망원동 세입자들은 월세를 지켰지만 신고는 적었다."),
                      lead + "\n\n" + L("Most tenants let the call run on until the caller gave up. ", "대부분의 세입자는 상대가 포기할 때까지 전화를 붙들고만 있었다. ") + landlord + "\n\n" + report,
                      L("Nothing was sent, but you never decided. The lease and the residents' chat had the answer.", "돈은 보내지 않았지만 결정도 내리지 못했다. 답은 계약서와 입주민 채팅방에 있었다.")),
            };
        }

        static List<EndPaper> Day2LegitPapers()
        {
            string lead = L("Choi Youngsik, 71, who owns Mangwon Heights on Poeun-ro, had knee surgery at Severance Hospital yesterday. While he recovers, his son Choi Hyunwoo is collecting the rent, and found out how hard that can be.",
                            "포은로 망원하이츠의 건물주 최영식(71) 씨가 어제 세브란스병원에서 무릎 수술을 받았다. 그가 회복하는 동안 아들 최현우 씨가 월세를 걷고 있는데, 이게 생각보다 쉽지 않았다.");
            string advice = L("The Seoul Housing Centre says tenants are right to check a new rent account against the lease, the landlord's own message and the name on the account. \"Checking works both ways,\" a spokesperson said. \"When they all match, the change is real.\"",
                              "서울주거포털은 세입자가 새 월세 계좌를 계약서, 집주인이 직접 쓴 글, 예금주 이름과 대조하는 것이 옳다고 말한다. 관계자는 \"확인은 양쪽 모두를 위한 것\"이라며 \"모두 일치하면 변경은 진짜\"라고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, L("A son keeps the building running", "아버지 대신 건물을 지키는 아들"),
                      L("While Mangwon Heights' landlord recovers from surgery, his son collects the rent, after a message to every tenant.", "망원하이츠 집주인이 수술 후 회복하는 동안, 아들이 채팅방 공지를 거쳐 월세를 걷고 있다."),
                      lead + "\n\n" + L("\"Dad wrote the new account in the residents' chat before he went in,\" Hyunwoo said. \"Some tenants checked everything before paying, which I was glad to see.\"",
                                        "현우 씨는 \"아버지가 입원하시기 전에 입주민 채팅방에 새 계좌를 올리셨다\"며 \"모든 걸 확인하고 보내 준 세입자도 있었는데, 그게 오히려 반가웠다\"고 말했다.") + "\n\n" + advice,
                      L("The change was real, and you checked it: the number in your lease, the name on the account and the landlord's own message all matched.", "변경은 진짜였고, 당신은 확인했다. 계약서의 번호, 예금주 이름, 집주인이 직접 쓴 글이 모두 일치했다.")),
                Paper(Outcome.Refuse, L("When the real call sounds like a scam", "진짜 전화가 사기처럼 들릴 때"),
                      L("A landlord's son says half the tenants hung up on him on rent day.", "집주인 아들 \"월세 날 세입자 절반이 전화를 끊었다\""),
                      lead + "\n\n" + L("\"After all the scam calls, I understand,\" Hyunwoo said. \"But Dad wrote the new account in the chat that morning, and my number is in every lease. It was all there.\"",
                                        "현우 씨는 \"사기 전화가 하도 많으니 이해한다\"면서도 \"아버지가 그날 아침 채팅방에 새 계좌를 올리셨고, 제 번호는 모든 계약서에 있다. 다 거기 있었다\"고 말했다.") + "\n\n" + advice,
                      L("This one was real. The number in your lease, the landlord's own message and the name on the account all matched.", "이번엔 진짜였다. 계약서의 번호, 집주인이 직접 쓴 글, 예금주 이름이 모두 일치했다.")),
                Paper(Outcome.Timeout, L("Rent day, hospital day", "월세 날, 입원한 날"),
                      L("A landlord's son waited on the line for tenants who never decided.", "집주인 아들은 끝내 결정하지 못한 세입자를 전화로 기다렸다."),
                      lead + "\n\n" + L("\"I waited on the phone for ages,\" Hyunwoo said. \"The chat message and the lease had everything they needed.\"",
                                        "현우 씨는 \"한참을 전화로 기다렸다\"며 \"채팅방 글과 계약서에 필요한 건 다 있었다\"고 말했다.") + "\n\n" + advice,
                      L("The change was real, and the evidence was in the lease, the chat and the account's name. Deciding in time matters too.", "변경은 진짜였고, 증거는 계약서와 채팅방, 예금주 이름에 있었다. 제때 결정하는 것도 중요하다.")),
            };
        }

        // ================================================================ what Day 1 left behind

        static List<DayEcho> Day2Echoes()
        {
            SetDay(2, "16:20");
            var echoes = new List<DayEcho>(OfCase(CaseProtected, Day1ProtectedEchoes()));
            echoes.AddRange(PaperEchoes(true));
            echoes.AddRange(SaleEchoes(true));
            return echoes;
        }

        /// <summary>After "the protected account".</summary>
        static DayEcho[] Day1ProtectedEchoes()
        {
            return new[]
            {
                // Sent the ₩1,200,000
                EchoCard(1, Truth.Any, Sent, L("The bank said the ₩1,200,000 is probably gone for good.", "은행에서는 120만 원을 되찾기 어려울 거라고 했다.")),
                EchoChat(1, Truth.Any, Sent, FamilyChat, L("Mom", "엄마"), "pt_mom", Oct(6, "21:30"),
                         L("Jiwoo-ya, Dad told me. Don't blame yourself, these people are professionals. We'll help with tuition ♥",
                           "지우야, 아빠한테 들었어. 너무 자책하지 마, 그 사람들 전문가들이야. 등록금은 엄마 아빠가 도와줄게 ♥"), true),
                EchoMine(1, Truth.Any, Sent, FamilyChat, Oct(6, "21:34"), L("Thanks Mom... I should have looked up the account first.", "고마워 엄마… 계좌만 조회해 봤어도 됐는데.")),

                // Hung up
                EchoCard(1, Truth.Any, HungUp, L("Yesterday's fake \"bank\" call made this morning's paper. You hung up on it.", "어제 받은 가짜 '은행' 전화가 오늘 아침 신문에 실렸다. 당신은 그 전화를 끊었다.")),
                EchoMine(1, Truth.Any, HungUp, "yuna", Oct(6, "17:30"), L("A fake bank called me today!! They wanted 1.2 million", "오늘 가짜 은행한테 전화 왔어!! 120만 원 보내래")),
                EchoChat(1, Truth.Any, HungUp, "yuna", L("Yuna", "유나"), "pt_yuna", Oct(6, "17:31"), L("WHAT. Good thing you hung up!!", "헐. 끊어서 다행이다 ㄷㄷ"), true),
                EchoMine(1, Truth.Any, HungUp, VillaChat, Oct(6, "17:50"), L("The 'protection team' called me too. Hung up. It's 070-8844-2019.", "저도 '보호팀' 전화 받았어요. 끊었어요. 070-8844-2019예요.")),
                EchoChat(1, Truth.Any, HungUp, VillaChat, L("201", "201호"), "pt_unknown", Oct(6, "18:10"), L("Same number! Thanks 302, reported it.", "같은 번호네요! 302호 감사해요, 신고했어요.")),

                // Ran out of time
                EchoCard(1, Truth.Any, TooLate, L("Yesterday's \"bank\" caller gave up at five. You never gave him an answer.", "어제 '은행' 전화는 다섯 시에 끊겼다. 당신은 끝내 대답하지 않았다.")),
                EchoChat(1, Truth.Any, TooLate, VillaChat, L("201", "201호"), "pt_unknown", Oct(6, "18:10"), L("That 'protection team' is still calling people. It's 070-8844-2019: just hang up.", "그 '보호팀' 아직도 전화 돌리네요. 070-8844-2019예요. 그냥 끊으세요."), true),
            };
        }
    }
}
