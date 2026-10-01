using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 1, another case: "the subscription fee". Oh Sangchul, manager of the Seoul Daily's Mapo
    /// branch, says the branch no longer collects at the door and asks for three months' fee by
    /// transfer, into an account in his own name. One clue, and it is in the newspaper:
    /// <list type="bullet">
    /// <item>Legit: today's paper prints the notice itself, with his name, his number, the amount
    /// and the account.</item>
    /// <item>Scam: today's paper prints a notice that the branch has a new manager, that Mr. Oh no
    /// longer works for the paper and that fees are paid by giro slip only.</item>
    /// </list>
    /// The computer cannot tell the two apart: in both, the number and the account are his own,
    /// years old and without reports. He is a real person; the question is whether he may collect.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string PaperNumber = "010-8265-3317";
        const string PaperAccount = "110-457-803312";
        const long PaperFee = 54000;

        static string PaperAccountSpoken => L("one one zero, four five seven, eight zero three three one two", "일일공, 사오칠, 팔공삼삼일이");
        static string PaperNumberSpoken => L("zero one zero, eight two six five, three three one seven", "공일공, 팔이육오, 삼삼일칠");
        static string VoiceBranch => L("Grandpa (English (US))", "Grandpa (Korean (South Korea))");

        static DayVariant PaperVariant(bool scam)
        {
            SetDay(1, "16:20");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();

            // ---- the paper on the desk: yesterday's case in front, and the paper's own notice in the box
            FrontPage(room, Day0Papers().Find(p => p.outcome == Outcome.Refuse));
            var n = room.newspaper;
            n.warningTitle = L("TO OUR READERS IN MAPO", "마포 지역 독자 여러분께");
            n.warningText = scam
                ? L("Our Mapo branch has a new manager since 1 October. The former manager, Oh Sangchul, no longer works for the Seoul Daily and may not collect fees. We never ask for a transfer by phone: the fee is paid only with the giro slip that comes with your paper on the 20th.",
                    "10월 1일부로 마포지국 지국장이 바뀌었습니다. 전 지국장 오상철 씨는 더 이상 서울데일리와 관계가 없으며 구독료를 받을 수 없습니다. 본사는 전화로 계좌이체를 요구하지 않습니다. 구독료는 매달 20일 신문과 함께 배달되는 지로 용지로만 납부합니다.")
                : L($"From October our Mapo branch no longer collects at the door. This week the branch manager, Oh Sangchul, phones every reader ({PaperNumber}) for the October to December fee: 54,000 won, paid to Nuri Bank {PaperAccount} (Oh Sangchul).",
                    $"10월부터 마포지국은 방문 수금을 하지 않습니다. 이번 주 오상철 지국장이 독자 여러분께 전화({PaperNumber})로 10~12월 구독료를 안내합니다. 54,000원이며, 누리은행 {PaperAccount}(예금주 오상철)로 받습니다.");
            n.local = Day1Local();

            // ---- what the computer knows: a real man with an old phone and an old account, whoever he works for
            directory.numbers.Add(new DirNumber { number = PaperNumber, owner = L("OH SANGCHUL", "오상철"), note = L("Mobile phone, since 2012", "휴대전화 · 2012년 가입") });
            directory.accounts.Add(new DirAccount { bank = NuriBank, number = PaperAccount, holder = L("OH SANGCHUL", "오상철"), note = L("Personal account, opened 2014", "개인 계좌 · 2014년 개설") });

            var v = StoreVariant(scam ? "paper_scam" : "paper_legit", CasePaper, Day1Dir, scam ? "Day1_Paper_Scam" : "Day1_Paper_Legit", PaperCall(scam), phone, room, directory);
            v.intro = L("Classes are over. On the desk lies the Seoul Daily your dad signed you up for in March: it comes every morning.",
                        "수업은 끝났다. 책상 위에는 3월에 아빠가 구독 신청해 준 서울데일리가 놓여 있다. 매일 아침 오는 신문이다.");
            v.clues = new List<ClueDef>
            {
                scam
                    ? Clue("notice", L("Today's Seoul Daily prints a notice: the Mapo branch has a new manager since 1 October, Oh Sangchul no longer works for the paper, and the fee is paid by giro slip only, never by a transfer over the phone.",
                                       "오늘 자 서울데일리에 알림이 실렸다. 10월 1일부로 마포지국장이 바뀌었고, 오상철 씨는 더 이상 신문사와 관계가 없으며, 구독료는 지로로만 받고 전화로 이체를 요구하지 않는다고."),
                           AtPaper, new ClueWhen(ClueEvent.PanelOpened, "Newspaper"))
                    : Clue("notice", L("Today's Seoul Daily prints the notice itself: this week branch manager Oh Sangchul phones readers for the October to December fee, 54,000 won, into the very account he gave you.",
                                       "오늘 자 서울데일리에 직접 알림이 실렸다. 이번 주 오상철 지국장이 전화로 10~12월 구독료 54,000원을 안내하며, 그가 불러 준 바로 그 계좌로 받는다고."),
                           AtPaper, new ClueWhen(ClueEvent.PanelOpened, "Newspaper")),
            };
            v.papers = scam ? PaperScamPapers() : PaperLegitPapers();
            v.ruleTitle = scam ? L("A real name is not permission", "진짜 이름이 곧 자격은 아니다") : L("The company's own notice settles it", "회사가 낸 알림이 답이다");
            v.rule = scam
                ? L("Someone collecting for a company you pay? Looking him up only proves that his name is real. The company's own notice says who may collect, and how.",
                    "거래하는 회사 이름으로 돈을 걷는 사람이 있다면? 조회로는 그 이름이 진짜라는 것만 알 수 있다. 누가, 어떤 방법으로 걷는지는 회사가 직접 낸 알림에 있다.")
                : L("When the company's own notice names the collector, the amount and the account, paying him is safe. Read the notice, then act.",
                    "회사가 직접 낸 알림에 수금하는 사람, 금액, 계좌가 그대로 나와 있다면 보내도 안전하다. 알림을 읽고, 그다음에 행동하라.");
            v.ruleSource = PaperSource(7);
            return v;
        }

        // ================================================================ the call

        static ConversationData PaperCall(bool scam)
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? L("Day 1 · The subscription fee (scam)", "1일차 · 신문 구독료 (사기)") : L("Day 1 · The subscription fee (legit)", "1일차 · 신문 구독료 (진짜)");
            c.isScam = scam;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = L("Oh Sangchul", "오상철"), number = PaperNumber, inContacts = false, portrait = "pt_branch", voice = VoiceBranch };
            c.claims = new List<string>
            {
                L("He is Oh Sangchul, manager of the Seoul Daily's Mapo branch.", "서울데일리 마포지국장 오상철이라고 한다."),
                L("From October the branch takes the subscription fee by transfer, not at the door.", "10월부터 지국이 구독료를 방문 수금 대신 계좌이체로 받는다고 한다."),
                L($"October to December costs ₩54,000: Nuri Bank {PaperAccount}, in his name.", $"10~12월 구독료 54,000원: 누리은행 {PaperAccount}, 본인 명의"),
                L("If it isn't paid by 17:00, the paper stops coming tomorrow.", "17시까지 내지 않으면 내일부터 신문이 끊긴다고 한다."),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 45f,
                      L("I didn't know that. How much is it?", "몰랐어요. 얼마예요?"), "amount",
                      L("Shouldn't you call my dad about that?", "그건 아빠한테 전화하셔야 하지 않아요?"), "dad",
                      P(0.5f, L("Hello? Student? Are you there?", "여보세요? 학생? 듣고 있어요?"))),
                    Caller(L("Hello, is this the student in 302, Mangwon Heights? This is Oh Sangchul, the manager of the Seoul Daily's Mapo branch.",
                             "여보세요, 망원하이츠 302호 학생 맞지요? 서울데일리 마포지국장 오상철입니다."),
                           L("Hello, is this the student in three oh two, Mangwon Heights? This is Oh Sangchul, the manager of the Seoul Daily's Mapo branch.",
                             "여보세요, 망원하이츠 삼백이 호 학생 맞지요? 서울데일리 마포지국장 오상철입니다.")),
                    Caller(L("Your father took out the paper for you in March and paid up to September. From October, the fee hasn't been paid.",
                             "3월에 아버님이 학생 앞으로 신문을 신청하시면서 9월분까지 내셨어요. 10월부터는 구독료가 안 들어왔고요."),
                           L(null, "삼월에 아버님이 학생 앞으로 신문을 신청하시면서 구월분까지 내셨어요. 시월부터는 구독료가 안 들어왔고요."))),
                Node("amount", "ask",
                    Caller(L("Eighteen thousand won a month. Most readers pay three months at a time.", "한 달에 만 팔천 원이에요. 보통 석 달 치를 한 번에들 내세요."))),
                Node("dad", "ask",
                    Caller(L("I tried him this morning and he didn't pick up. And the paper comes to your door, student.", "아침에 아버님께 전화드렸는데 안 받으시더라고요. 그리고 신문은 학생 집으로 들어가잖아요."))),
                Hold("ask",
                    Caller(L("We don't collect at the door any more. From this month it's by bank transfer, and I'm phoning every reader myself this week.",
                             "이제 방문 수금은 안 해요. 이번 달부터는 계좌이체로 받고, 이번 주에 제가 독자분들께 일일이 전화드리고 있어요.")),
                    Caller(L($"October to December comes to 54,000 won. Please send it to Nuri Bank {PaperAccount}. The account is in my name, Oh Sangchul.",
                             $"10월부터 12월까지 5만 4천 원이에요. 누리은행 {PaperAccount}로 보내 주세요. 예금주는 저, 오상철입니다."),
                           L($"October to December comes to fifty four thousand won. Please send it to Nuri Bank, {PaperAccountSpoken}. The account is in my name, Oh Sangchul.",
                             $"시월부터 십이월까지 오만 사천 원이에요. 누리은행 {PaperAccountSpoken}로 보내 주세요. 예금주는 저, 오상철입니다."),
                           F(FactKind.Account, PaperAccount), F(FactKind.Amount, "54,000")),
                    Caller(L("I close the branch's books at five. Whoever hasn't paid by then comes off tomorrow's delivery list.",
                             "지국 장부를 5시에 마감해요. 그때까지 안 들어온 집은 내일 배달 명단에서 빠집니다."),
                           L(null, "지국 장부를 다섯 시에 마감해요. 그때까지 안 들어온 집은 내일 배달 명단에서 빠집니다.")),
                    Caller(L("I'll stay on the line while you send it.", "보내시는 동안 기다릴게요."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("The subscription fee", "신문 구독료"),
                claimedIdentity = L("Oh Sangchul, manager of the Seoul Daily's Mapo branch", "서울데일리 마포지국장 오상철"),
                ask = L($"Pay the paper's October to December fee, ₩54,000, into Nuri Bank {PaperAccount}", $"10~12월 구독료 54,000원을 누리은행 {PaperAccount}로 입금"),
                deadline = "17:00",
                deadlineReason = L("before the branch closes its books", "지국 장부 마감 전"),
                objective = L("Is he really the one who collects for your newspaper this month? Check before 17:00, then give your verdict on the call: pay the fee, or hang up.",
                              "정말 이번 달 내 신문 구독료를 걷는 사람일까? 17:00 전까지 확인한 뒤, 통화 화면에서 판정을 내리자: 구독료를 낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Pay the fee", "구독료 내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("All right, I'm sending it now.", "네, 지금 보낼게요."),
                refuseLine = L("I'm not paying this over the phone. Goodbye.", "전화로는 안 낼게요. 끊을게요."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask whose name is on the account", "누구 명의 계좌인지 묻기"), L("Whose name is on that account?", "그 계좌는 누구 명의예요?"), null,
                  Caller(L("Mine. Oh Sangchul. A branch is its manager's own business, student, so the account is in my name.", "제 명의예요. 오상철. 지국은 지국장 개인 사업이라 계좌도 제 이름으로 돼 있어요."))),
                Q(L("Ask why nobody comes to the door any more", "왜 이제 방문 수금을 안 하는지 묻기"), L("Why don't you collect at the door any more?", "왜 이제 직접 안 오세요?"), null,
                  scam
                      ? Caller(L("Head office changed it from October. It was in the paper last week.", "본사에서 10월부터 바꿨어요. 지난주 신문에 났었어요."),
                               L(null, "본사에서 시월부터 바꿨어요. 지난주 신문에 났었어요."))
                      : Caller(L("Head office changed it from October. It's in this morning's paper, in the box on the front page.", "본사에서 10월부터 바꿨어요. 오늘 아침 신문 1면 박스에 실렸어요."),
                               L(null, "본사에서 시월부터 바꿨어요. 오늘 아침 신문 일 면 박스에 실렸어요."))),
                Q(L("Ask for the branch office's number", "지국 사무실 번호 묻기"), L("What's the number of the branch office?", "지국 사무실 번호가 어떻게 돼요?"), null,
                  scam
                      ? Caller(L($"This mobile, student. The office line was cut when the branch moved: {PaperNumber}.", $"이 휴대폰이에요, 학생. 지국 이사하면서 사무실 전화는 없앴어요. {PaperNumber}."),
                               L($"This mobile, student. The office line was cut when the branch moved. {PaperNumberSpoken}.", $"이 휴대폰이에요, 학생. 지국 이사하면서 사무실 전화는 없앴어요. {PaperNumberSpoken}."),
                               F(FactKind.Phone, PaperNumber, L("Branch", "지국")))
                      : Caller(L($"This mobile, student. It's the number printed in today's notice: {PaperNumber}.", $"이 휴대폰이에요, 학생. 오늘 알림에 실린 번호 그대로예요. {PaperNumber}."),
                               L($"This mobile, student. It's the number printed in today's notice. {PaperNumberSpoken}.", $"이 휴대폰이에요, 학생. 오늘 알림에 실린 번호 그대로예요. {PaperNumberSpoken}."),
                               F(FactKind.Phone, PaperNumber, L("Branch", "지국")))),
                Q(L("Say you'll ask your father first", "아빠한테 먼저 물어보겠다고 하기"), L("Let me ask my dad first.", "아빠한테 먼저 물어볼게요."), null,
                  Caller(L("Of course. But I close the books at five, and starting again later means a new sign-up.", "그러세요. 다만 장부를 5시에 마감하고, 나중에 다시 넣으려면 새로 신청하셔야 해요."),
                         L(null, "그러세요. 다만 장부를 다섯 시에 마감하고, 나중에 다시 넣으려면 새로 신청하셔야 해요."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:33", Caller(L("Student? Still there? Take your time, but I do close at five.", "학생? 듣고 있어요? 천천히 해요, 근데 5시에는 마감해야 해요."),
                                     L(null, "학생? 듣고 있어요? 천천히 해요, 근데 다섯 시에는 마감해야 해요."))),
                Beat("16:41", Caller(L("I have forty more readers to call today. Has it gone through?", "오늘 전화드릴 집이 아직 마흔 곳 남았어요. 이체는 됐나요?"))),
                Beat("16:48", Caller(L("Twelve minutes, student. I'd hate to stop the paper your father chose for you.", "12분 남았어요. 아버님이 골라 주신 신문인데 끊기면 아깝잖아요."),
                                     L(null, "십이 분 남았어요. 아버님이 골라 주신 신문인데 끊기면 아깝잖아요."))),
                Beat("16:54", Caller(L("Six minutes, student.", "6분 남았어요, 학생."), L(null, "육 분 남았어요, 학생."))),
                Beat("16:57", Caller(L("Three minutes. Shall I take you off the list, then?", "3분 남았어요. 그럼 명단에서 뺄까요?"), L(null, "삼 분 남았어요. 그럼 명단에서 뺄까요?"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? L("You hung up. Today's paper had the notice: the Mapo branch has a new manager, and Oh Sangchul no longer collects for the Seoul Daily.",
                            "전화를 끊었다. 오늘 신문에 알림이 실려 있었다. 마포지국장이 바뀌었고, 오상철은 더 이상 서울데일리 구독료를 걷지 않는다고.")
                        : L("You hung up on the real branch manager. The notice in today's paper named him, the amount and the account.",
                            "진짜 지국장의 전화를 끊어 버렸다. 오늘 신문의 알림에 그의 이름과 금액, 계좌가 그대로 실려 있었다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("It's five. I'm taking you off the list.", "5시네요. 명단에서 빼겠습니다."), L(null, "다섯 시네요. 명단에서 빼겠습니다.")) }
                        : new List<ConvLine> { Caller(L("Student? It's five, I have to close the books. Call me when you've decided.", "학생? 5시라 장부를 마감해야 해요. 마음 정해지면 전화 줘요."),
                                                      L(null, "학생? 다섯 시라 장부를 마감해야 해요. 마음 정해지면 전화 줘요.")) },
                    consequence = scam
                        ? L("He gave up at five. The paper came the next morning all the same: he had nothing to do with it any more.", "상대는 5시에 포기했다. 다음 날 아침에도 신문은 그대로 왔다. 그는 이제 신문과 아무 관계가 없었다.")
                        : L("Five o'clock passed. Mr. Oh closed his books without your fee.", "5시가 지났다. 오 지국장은 당신의 구독료 없이 장부를 마감했다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -PaperFee,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Received. Thank you, student. Keep reading!", "들어왔네요. 고마워요, 학생. 신문 열심히 봐요!")) }
                        : new List<ConvLine> { Caller(L("It's in. Thank you! The paper will be at your door at six, as always.", "들어왔어요. 고마워요! 신문은 늘 그렇듯 6시에 문 앞에 있을 거예요."),
                                                      L(null, "들어왔어요. 고마워요! 신문은 늘 그렇듯 여섯 시에 문 앞에 있을 거예요.")) },
                    consequence = scam
                        ? L("The ₩54,000 went to Oh Sangchul, who had not worked for the Seoul Daily since September. The notice was in the paper on your desk.",
                            "54,000원은 9월 이후 서울데일리와 무관해진 오상철에게 들어갔다. 알림은 책상 위 신문에 실려 있었다.")
                        : L("The fee reached the branch manager, as the paper's own notice said it should. The Seoul Daily keeps coming until December.",
                            "구독료는 신문에 실린 알림대로 지국장에게 들어갔다. 서울데일리는 12월까지 계속 온다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = PaperAccount, bank = NuriBank, amount = PaperFee, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> PaperScamPapers()
        {
            string lead = L("A man who ran the Seoul Daily's Mapo branch until September spent Tuesday phoning its subscribers. From October, he told them, the fee was collected by bank transfer, and three months were due by 5 p.m.: 54,000 won, into an account in his own name.",
                            "9월까지 서울데일리 마포지국을 맡았던 남성이 화요일 내내 구독자들에게 전화를 걸었다. 그는 10월부터 구독료를 계좌이체로 받는다며, 석 달 치 54,000원을 오후 5시까지 자기 명의 계좌로 보내라고 했다.");
            string facts = L("The name and the account were real: Oh Sangchul, 58, ran the branch for six years. The paper ended his contract on 30 September and said so in a notice on Tuesday's front page. The fee is paid by giro slip only.",
                             "이름과 계좌는 진짜였다. 오상철(58) 씨는 6년간 지국을 운영했다. 본사는 9월 30일 그와의 계약을 끝냈고, 화요일 자 1면 알림으로 이를 알렸다. 구독료는 지로로만 받는다.");
            string quote = L("\"Look him up and you learn that his name is real. You don't learn whether he may still collect,\" said the new branch manager, Ryu Mina. \"That is what our notice is for.\"",
                             "새 지국장 류미나 씨는 \"조회해 보면 이름이 진짜라는 건 나온다. 하지만 그 사람이 아직 수금할 자격이 있는지는 안 나온다\"며 \"그래서 알림을 내는 것\"이라고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, L("Former branch manager kept \"collecting\" for the paper", "전 지국장, 그만둔 뒤에도 '구독료 수금'"),
                      L("His name and his account were real. Readers who read the notice hung up.", "이름도 계좌도 진짜였다. 알림을 읽은 독자들은 전화를 끊었다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("You hung up. The notice in the paper on your desk said he no longer collected.", "전화를 끊었다. 책상 위 신문의 알림에 그는 더 이상 수금하지 않는다고 적혀 있었다.")),
                Paper(Outcome.GoAlong, L("Three months paid to a man who no longer delivers", "신문과 무관한 사람에게 낸 석 달 치 구독료"),
                      L("A Mangwon student sent 54,000 won to the branch's former manager.", "망원동 대학생, 전 지국장에게 54,000원 송금"),
                      lead + "\n\n" + L("A 22-year-old student in Mangwon-dong sent the money. Her lookup showed a real name on a twelve-year-old account, and that was true.",
                                        "망원동의 22세 대학생 한 명은 돈을 보냈다. 조회 결과에는 12년 된 계좌와 진짜 이름이 나왔고, 그건 사실이었다.") + " " + facts + "\n\n" + quote,
                      L("His name was real, and so was the account. The paper's own notice said he had left.", "이름도 계좌도 진짜였다. 하지만 신문사 알림에는 그가 그만뒀다고 나와 있었다.")),
                Paper(Outcome.Timeout, L("\"Pay by five or lose your paper\"", "'5시까지 안 내면 신문 끊깁니다'"),
                      L("The papers arrived the next morning all the same.", "다음 날 아침에도 신문은 그대로 배달됐다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("Nothing was sent, but you never gave him an answer. The notice was in the paper on your desk.", "돈은 보내지 않았지만 끝내 답을 하지 않았다. 알림은 책상 위 신문에 있었다.")),
            };
        }

        static List<EndPaper> PaperLegitPapers()
        {
            string lead = L("The Seoul Daily's Mapo branch stopped collecting at the door this month. Its manager, Oh Sangchul, 58, spent Tuesday phoning his 400 subscribers to ask for three months' fee by bank transfer.",
                            "서울데일리 마포지국이 이달부터 방문 수금을 중단했다. 오상철(58) 지국장은 화요일 하루 동안 구독자 400명에게 전화를 걸어 석 달 치 구독료를 계좌이체로 부탁했다.");
            string quote = L("\"Half of them took me for a scammer,\" Mr. Oh said. \"I don't blame them. That is why we printed my name, my number and the account on the front page first.\"",
                             "오 지국장은 \"절반은 저를 사기꾼으로 알더라\"면서도 \"탓할 일이 아니다. 그래서 제 이름과 번호, 계좌를 1면에 먼저 실은 것\"이라고 말했다.");
            string advice = L("The Mapo Consumer Centre says a company's own notice is the place to check a collector: who may collect, how much, and into which account.",
                              "마포소비자센터는 수금하는 사람을 확인할 곳은 회사가 직접 낸 알림이라고 말한다. 누가, 얼마를, 어느 계좌로 받는지가 거기에 있다.");
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, L("Our Mapo branch stops knocking on doors", "마포지국, 방문 수금 대신 계좌이체로"),
                      L("The manager phoned every reader himself, after a notice on the front page.", "1면 알림에 이어 지국장이 독자들에게 직접 전화했다."),
                      lead + "\n\n" + quote + L(" One student read the notice while he waited on the line, then paid. \"That is exactly how it should go,\" he said.",
                                               " 한 대학생은 그가 전화로 기다리는 동안 알림을 읽고 나서 구독료를 냈다. 그는 \"바로 그렇게 하는 것\"이라고 말했다.") + "\n\n" + advice,
                      L("He was the real branch manager, and you checked: the paper's own notice named him, the amount and the account.", "그는 진짜 지국장이었고, 당신은 확인했다. 신문사 알림에 그의 이름과 금액, 계좌가 그대로 있었다.")),
                Paper(Outcome.Refuse, L("Readers hang up on their own newspaper", "자기 신문 지국장 전화도 끊는 독자들"),
                      L("A branch manager's real calls were taken for a scam.", "지국장의 진짜 전화가 사기로 오해받았다."),
                      lead + "\n\n" + quote + L(" He delivered to all of them on Wednesday anyway. \"One more day,\" he said.",
                                               " 그는 수요일에도 모든 집에 신문을 넣었다. \"하루만 더 넣어 보는 것\"이라고 했다.") + "\n\n" + advice,
                      L("This one was real: the notice in the paper on your desk named him, the amount and the account.", "이번엔 진짜였다. 책상 위 신문의 알림에 그의 이름과 금액, 계좌가 실려 있었다.")),
                Paper(Outcome.Timeout, L("The books closed at five", "장부는 5시에 마감됐다"),
                      L("A branch manager waited on the line for readers who never decided.", "지국장은 끝내 결정하지 못한 독자들을 전화로 기다렸다."),
                      lead + "\n\n" + quote + "\n\n" + advice,
                      L("He was the real branch manager, and the notice was in the paper on your desk. Deciding in time matters too.", "그는 진짜 지국장이었고, 알림은 책상 위 신문에 있었다. 제때 결정하는 것도 중요하다.")),
            };
        }

        // ================================================================ what the case leaves behind

        /// <summary>Wednesday and Thursday after "the subscription fee" (built for the day being built).</summary>
        static IEnumerable<DayEcho> PaperEchoes(bool nextDay)
        {
            string dad = L("Dad", "아빠");
            var list = new List<DayEcho>
            {
                // The real manager, paid late: the fee went out on Wednesday morning.
                EchoBank(1, Truth.Legit, Kept, L("OH SANGCHUL", "오상철"), Oct(7, "08:10"), L("Seoul Daily, October to December", "서울데일리 10~12월 구독료"), -PaperFee),
            };
            if (nextDay)
                list.AddRange(new[]
                {
                    EchoCard(1, Truth.Scam, Sent, L("Yesterday's \"branch manager\" had been let go in September. Your ₩54,000 is gone.", "어제의 '지국장'은 9월에 그만둔 사람이었다. 54,000원은 돌아오지 않는다.")),
                    EchoChat(1, Truth.Scam, Sent, FamilyChat, dad, "pt_dad", Oct(6, "19:40"),
                             L("The Seoul Daily called me back. That man hasn't worked for them since September. Report it to 112, Jiwoo. And read the front page next time!",
                               "서울데일리에서 연락이 왔다. 그 사람 9월에 그만뒀단다. 지우야, 112에 신고해라. 다음부터는 1면부터 읽고!"), true),
                    EchoMine(1, Truth.Scam, Sent, FamilyChat, Oct(6, "19:44"), L("I know... The notice was right there on my desk.", "알아요… 알림이 바로 책상 위에 있었는데.")),

                    EchoCard(1, Truth.Scam, HungUp, L("Yesterday's \"branch manager\" no longer worked for the paper. You hung up.", "어제의 '지국장'은 이미 신문사를 떠난 사람이었다. 당신은 전화를 끊었다.")),
                    EchoMine(1, Truth.Scam, HungUp, FamilyChat, Oct(6, "17:20"),
                             L("Dad, someone called to collect the newspaper fee. The paper itself said he was let go, so I hung up.", "아빠, 누가 신문 구독료 내라고 전화했어요. 신문에 그 사람 그만뒀다고 실려 있어서 끊었어요.")),
                    EchoChat(1, Truth.Scam, HungUp, FamilyChat, dad, "pt_dad", Oct(6, "17:35"),
                             L("Well done. So you do read it! I pay by giro, you don't owe anyone a thing.", "잘했다. 신문을 읽긴 읽는구나! 구독료는 아빠가 지로로 낸다. 넌 누구한테도 낼 것 없다."), true),

                    EchoCard(1, Truth.Scam, TooLate, L("Yesterday's \"branch manager\" gave up at five. The paper still came this morning.", "어제의 '지국장'은 5시에 포기했다. 오늘 아침에도 신문은 왔다.")),

                    EchoCard(1, Truth.Legit, Sent, L("The paper is paid until December. Mr. Oh left a thank-you note on this morning's copy.", "12월까지 구독료를 냈다. 오 지국장이 오늘 아침 신문에 감사 메모를 붙여 두었다.")),
                    EchoChat(1, Truth.Legit, Sent, FamilyChat, dad, "pt_dad", Oct(6, "18:10"),
                             L("The branch texted me that you paid the paper. Thank you! I owe you a dinner.", "지국에서 네가 구독료 냈다고 문자 왔다. 고맙다! 아빠가 밥 한번 사마."), true),

                    EchoCard(1, Truth.Legit, Kept, L("Mr. Oh was the real branch manager. He left today's paper anyway; you paid him this morning.", "오 지국장은 진짜였다. 그래도 오늘 신문은 넣어 주었고, 당신은 아침에 구독료를 보냈다.")),
                    EchoChat(1, Truth.Legit, Kept, FamilyChat, dad, "pt_dad", Oct(6, "18:30"),
                             L("Jiwoo, the branch manager called me. He's real, it was in today's paper. Send him the fee tomorrow morning, will you?",
                               "지우야, 지국장님한테 전화 왔다. 진짜 맞다, 오늘 신문에도 났어. 내일 아침에 구독료 꼭 보내 드려라."), true),
                });
            return OfCase(CasePaper, list.ToArray());
        }
    }
}
