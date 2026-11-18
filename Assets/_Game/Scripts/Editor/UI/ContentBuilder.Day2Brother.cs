using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 2, another case: "Minjun's broken phone". A caller with Minjun's voice rings from a
    /// number Jiwoo does not know: he dropped his phone, he is at the service centre, and could
    /// she send the repair money to his roommate's account before the desk closes? The answer
    /// comes from someone else in the family, Mom, in the family chat (Yuna's case on Day 4 has
    /// it in the friend's own chat):
    /// <list type="bullet">
    /// <item>Legit: Mom wrote at 15:31 that Minjun had just called her from his roommate Dohyun's
    /// phone, his screen dead.</item>
    /// <item>Scam: Mom wrote at 15:52 that Minjun had just texted her from his own phone, from
    /// the library.</item>
    /// </list>
    /// A second clue is a question only Minjun can answer: what Jiwoo told him about Saturday.
    /// The computer cannot tell the two apart: in both, the number and the account belong to a
    /// real Lee Dohyun, years old and without reports. In this case Jiwoo paid the rent after
    /// class, so the day is not about the landlord.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string BrotherNumber = "010-9046-2715";
        const string BrotherAccount = "620-204-771035";
        const long RepairCost = 280000;

        static string BrotherAccountSpoken => L("six two zero, two zero four, seven seven one zero three five", "육이공, 이공사, 칠칠일공삼오");
        static string VoiceBrother => L("Eddy (English (US))", "Eddy (Korean (South Korea))");
        static string Roommate => L("LEE DOHYUN", "이도현");
        static string LandlordPaid => L("CHOI YOUNGSIK", "최영식");
        static string OctoberRent => L("October rent · Nuri 110-771-209944", "10월 월세 · 누리 110-771-209944");

        // What Mom wrote that afternoon; the next days' phone shows it too.
        static string MomBroken => L("Minjun just called me from his roommate Dohyun's phone: he dropped his phone on the stairs and the screen is dead. He's taking it to the service centre. I'm in a meeting till six. Jiwoo, can you help him?",
                                     "민준이가 방금 룸메이트 도현이 폰으로 전화했어. 계단에서 폰 떨어뜨려서 액정이 나갔대. 서비스센터 간다고. 엄마는 6시까지 회의야. 지우야, 좀 도와줄 수 있어?");
        static string MomLibrary => L("Minjun just texted me from his phone, in the library: midterms till ten tonight, phone on silent. Text him, don't call!",
                                      "민준이가 방금 자기 폰으로 도서관에서 톡 보냈어. 중간고사 때문에 밤 10시까지 있고 폰은 무음이래. 전화 말고 톡 해!");

        // The question only Minjun can answer, and what the caller says to it.
        static string QuizDodge => L("Noona, come on, I don't have time for quizzes. The centre closes at five.", "누나, 지금 퀴즈 낼 때야? 센터 5시에 닫는다니까.");
        static string QuizAnswer => L("Only if I bring snacks. I'm bringing snacks, noona. Lots.", "간식 사 오면 된다며. 사 갈게, 누나. 많이.");

        static DayVariant BrotherVariant(bool scam)
        {
            SetDay(2, "16:20");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();

            // ---- the paper on the desk: yesterday's case in front, today's warning in the box
            FrontPage(room, Day1Papers().Find(p => p.outcome == Outcome.Refuse));
            var n = room.newspaper;
            n.warningTitle = L("FAMILY ON A STRANGE NUMBER? ASK THEM YOURSELF", "모르는 번호로 걸려 온 가족 전화? 직접 물어보라");
            n.warningText = L("Scammers copy a voice from a few seconds of video and call from \"a friend's phone\". The phone and the account they name can belong to a real person, so a lookup settles nothing. Ask someone else in the family, in the chat you always use.",
                              "사기범들은 몇 초짜리 영상만으로 목소리를 복제해 '친구 폰'이라며 전화를 건다. 그들이 대는 전화와 계좌는 실제 사람 것일 수 있어서 조회로는 가려낼 수 없다. 늘 쓰는 채팅방에서 다른 가족에게 물어보라.");
            n.local = Day2Local();

            // ---- the rent is paid, so today is about Minjun; the truth is in what Mom wrote to the family
            AddTx(phone, 7, "15:10", LandlordPaid, OctoberRent, -Rent);
            string mom = L("Mom", "엄마");
            if (scam)
                AddChat(phone, FamilyChat, 7, "15:52", mom, "pt_mom", MomLibrary);
            else
                AddChat(phone, FamilyChat, 7, "15:31", mom, "pt_mom", MomBroken);

            // ---- what the computer knows: a real student with an old phone and an old account, in both truths
            directory.numbers.Add(new DirNumber { number = BrotherNumber, owner = Roommate, note = L("Mobile phone, since 2021", "휴대전화 · 2021년 가입") });
            directory.accounts.Add(new DirAccount { bank = HanbitBank, number = BrotherAccount, holder = Roommate, note = L("Personal account, opened 2021", "개인 계좌 · 2021년 개설") });

            var v = StoreVariant(scam ? "brother_scam" : "brother_legit", CaseBrother, Day2Dir, scam ? "Day2_Brother_Scam" : "Day2_Brother_Legit", BrotherCall(scam), phone, room, directory);
            v.intro = L("Rent day: you sent Mr. Choi the 450,000 won after class, like every month. Midterms are a week away.",
                        "월세 내는 날. 수업이 끝나고 여느 달처럼 최 사장님께 45만 원을 보냈다. 중간고사는 일주일 뒤다.");
            v.marks = new List<MarkDef>
            {
                MarkChat("chat_mom", "minjun", FamilyChat, scam ? MomLibrary : MomBroken).Also("broken"),
                MarkCall("call_quiz", "minjun", scam ? QuizDodge : QuizAnswer),
                MarkContact("contact_minjun", "minjun", "010-3321-5580"),
                MarkCheck("check_number", "minjun", BrotherNumber),
                MarkCheck("check_account", "broken", BrotherAccount),
            };
            v.clues = new List<ClueDef>
            {
                scam
                    ? Clue("family_chat", L("Mom wrote in the family chat at 15:52: Minjun had just texted her from his own phone, in the library. His phone is not broken.",
                                            "엄마가 15:52에 가족 채팅방에 썼다. 민준이가 방금 자기 폰으로 도서관에서 톡을 보냈다고. 폰은 고장 나지 않았다."),
                           AtFamilyChat, Circled("chat_mom"))
                    : Clue("family_chat", L("Mom wrote in the family chat at 15:31: Minjun had just called her from his roommate Dohyun's phone, his screen dead.",
                                            "엄마가 15:31에 가족 채팅방에 썼다. 민준이가 방금 룸메이트 도현이 폰으로 전화했고, 액정이 나갔다고."),
                           AtFamilyChat, Circled("chat_mom")),
                scam
                    ? Clue("quiz", L("He dodged a question only Minjun could answer: what you told him about Saturday.", "민준이만 답할 수 있는 질문을 피했다. 토요일에 뭐라고 했는지."),
                           AtQuiz, Circled("call_quiz"))
                    : Clue("quiz", L("He knew what you told him about Saturday: only if he brings snacks.", "토요일에 뭐라고 했는지 알고 있었다. 간식 사 오면 된다고."),
                           AtQuiz, Circled("call_quiz")),
            };
            v.papers = scam ? BrotherScamPapers() : BrotherLegitPapers();
            v.ruleTitle = scam ? L("Ask another family member", "다른 가족에게 물어보라") : L("Someone else in the family can vouch", "다른 가족이 확인해 줄 수 있다");
            v.rule = scam
                ? L("Family asking for money from a strange number? A voice can be copied and a lookup can come back clean. Ask someone else in the family, in the chat you already share.",
                    "가족이 모르는 번호로 돈을 부탁한다면? 목소리는 복제될 수 있고 조회 결과는 깨끗할 수 있다. 원래 쓰던 채팅방에서 다른 가족에게 물어보라.")
                : L("The same check clears a real call: when another family member confirms it in your own chat, the strange number is explained. Check, then help.",
                    "같은 확인이 진짜 전화도 가려 준다. 다른 가족이 원래 쓰던 채팅방에서 확인해 주면 모르는 번호는 설명이 된다. 확인하고, 그다음에 도와주라.");
            v.ruleSource = PaperSource(8);
            return v;
        }

        static string AtQuiz => L("The call → ask what you told him about Saturday", "통화 → 토요일 얘기 물어보기");

        // ================================================================ the call

        static ConversationData BrotherCall(bool scam)
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? L("Day 2 · Minjun's broken phone (scam)", "2일차 · 민준이의 깨진 폰 (사기)") : L("Day 2 · Minjun's broken phone (legit)", "2일차 · 민준이의 깨진 폰 (진짜)");
            c.isScam = scam;
            c.revealAtEnd = false;
            // A number Jiwoo has never seen: the screen cannot show her brother's face.
            c.caller = new CallerInfo { displayName = L("Minjun", "민준이"), number = BrotherNumber, inContacts = false, portrait = "pt_unknown", voice = VoiceBrother };
            c.claims = new List<ClaimDef>
            {
                Claim("minjun", L("\"It's me, Minjun, on my roommate's phone.\"", "\"나야, 민준이. 룸메이트 폰으로 거는 거야.\""), !scam,
                      scam ? L("False. Mom wrote at 15:52 that Minjun had just texted her from his own phone, in the library. And he dodged your Saturday question.", "거짓. 엄마가 15:52에 썼다. 민준이가 방금 자기 폰으로 도서관에서 톡을 보냈다고. 토요일 질문에도 대답을 피했다.")
                           : L("True. Mom wrote at 15:31 that he had called her from Dohyun's phone, and he knew about Saturday.", "사실. 엄마가 15:31에 민준이가 도현이 폰으로 전화했다고 썼고, 토요일 얘기도 알고 있었다.")).Via(TrickBorrowedPhone),
                Claim("broken", L("\"My screen is dead. The repair is ₩280,000.\"", "\"액정이 나갔어. 수리비가 28만 원이래.\""), !scam,
                      scam ? L("False. His phone worked well enough to text Mom from the library. A lookup of Dohyun proves nothing either way.", "거짓. 그 폰으로 도서관에서 엄마한테 톡까지 보냈다. 도현이를 조회해도 어느 쪽인지는 알 수 없었다.")
                           : L("True. He called Mom about it first, and she wrote it in the family chat.", "사실. 민준이가 먼저 엄마한테 전화했고, 엄마가 가족 채팅방에 남겼다.")).Via(TrickEmergency),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 45f,
                      L("Again? Are you okay?", "또? 다친 데는 없어?"), "okay",
                      L("You sound different.", "목소리가 좀 다른데."), "voice",
                      P(0.5f, L("Noona? Hello?", "누나? 여보세요?"))),
                    Caller(L("Noona? It's me, Minjun. Can you hear me? The line is bad in here.", "누나? 나야, 민준이. 들려? 여기 전화가 잘 안 터져.")).Says("minjun"),
                    Caller(L("Don't freak out about the number. I dropped my phone on the stairs after lunch and the screen is dead. This is my roommate Dohyun's phone.",
                             "번호 보고 놀라지 마. 점심 먹고 계단에서 폰 떨어뜨려서 액정이 나갔어. 이거 룸메이트 도현이 폰이야."))),
                Node("okay", "ask",
                    Caller(L("I'm fine. The phone isn't.", "난 멀쩡해. 폰이 문제지."))),
                Node("voice", "ask",
                    Caller(L("It's Dohyun's phone, it sounds weird on my end too. And I've had a cold since Monday.", "도현이 폰이라 그래, 나도 이상하게 들려. 그리고 월요일부터 감기 기운도 있고."))),
                Hold("ask",
                    Caller(L("I'm at the service centre. The screen is 280,000 won, and they can do it today if I pay now.", "지금 서비스센터인데, 액정이 28만 원이래. 지금 결제하면 오늘 안에 해 준대."),
                           L("I'm at the service centre. The screen is two hundred and eighty thousand won, and they can do it today if I pay now.", "지금 서비스센터인데, 액정이 이십팔만 원이래. 지금 결제하면 오늘 안에 해 준대."),
                           F(FactKind.Amount, "280,000")).Says("broken"),
                    Caller(L("My banking app is on the dead phone, so I can't pay. Can you send it to Dohyun? He'll pay with his card.", "은행 앱이 그 폰에 있어서 내가 결제를 못 해. 도현이한테 보내 줄 수 있어? 도현이가 카드로 긁어 주기로 했어.")),
                    Caller(L($"Hanbit Bank {BrotherAccount}, Lee Dohyun.", $"한빛은행 {BrotherAccount}, 이도현."),
                           L($"Hanbit Bank, {BrotherAccountSpoken}. Lee Dohyun.", $"한빛은행 {BrotherAccountSpoken}, 이도현."),
                           F(FactKind.Account, BrotherAccount)),
                    Caller(L("They stop taking repairs at five. I'll pay you back on the 25th, I swear. I'll hold on.", "접수가 5시에 끝난대. 25일에 꼭 갚을게, 진짜야. 끊지 않고 기다릴게."),
                           L("They stop taking repairs at five. I'll pay you back on the twenty fifth, I swear. I'll hold on.", "접수가 다섯 시에 끝난대. 이십오 일에 꼭 갚을게, 진짜야. 끊지 않고 기다릴게."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("Minjun's broken phone", "민준이의 깨진 폰"),
                claimedIdentity = L("Minjun, your brother, on his roommate's phone", "룸메이트 폰으로 건 남동생 민준이"),
                ask = L($"Send ₩280,000 for the repair to Lee Dohyun: Hanbit Bank {BrotherAccount}", $"수리비 28만 원을 이도현 명의 한빛은행 {BrotherAccount}로 송금"),
                deadline = "17:00",
                deadlineReason = L("before the service centre stops taking repairs", "서비스센터 접수 마감 전"),
                objective = L("Is that really Minjun? Check before 17:00, then give your verdict on the call: send the money, or hang up.",
                              "정말 민준이일까? 17:00 전까지 확인한 뒤, 통화 화면에서 판정을 내리자: 돈을 보낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Send the money", "돈 보내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("Okay, I'm sending it to Dohyun now.", "알았어, 지금 도현이한테 보낼게."),
                refuseLine = L("I'll ask Minjun myself. Bye.", "민준이한테 직접 물어볼게. 끊는다."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask what you told him about Saturday", "토요일 얘기 때 뭐라고 했는지 묻기"), L("What did I say when you asked to sleep over on Saturday?", "토요일에 자고 가도 되냐고 했을 때 내가 뭐랬지?"), null,
                  scam
                      ? Caller(QuizDodge, L(null, "누나, 지금 퀴즈 낼 때야? 센터 다섯 시에 닫는다니까."))
                      : Caller(QuizAnswer)),
                Q(L("Ask why he doesn't ask Mom", "왜 엄마한테 말 안 하는지 묻기"), L("Why don't you ask Mom?", "엄마한테 말하지 그래?"), null,
                  scam
                      ? Caller(L("Don't tell Mom, please! She'd kill me. Just this once, noona.", "엄마한테는 말하지 마, 제발! 나 죽어. 이번 한 번만, 누나."))
                      : Caller(L("I called her first. She's in a meeting till six, so she wrote it in the family chat. Look.", "엄마한테 먼저 전화했어. 6시까지 회의라서 가족 톡방에 써 놨대. 봐 봐."),
                               L(null, "엄마한테 먼저 전화했어. 여섯 시까지 회의라서 가족 톡방에 써 놨대. 봐 봐."))),
                Q(L("Ask who Dohyun is", "도현이가 누구인지 묻기"), L("Who is Dohyun?", "도현이가 누군데?"), null,
                  Caller(L("My roommate, Lee Dohyun. Same department. The account is his.", "내 룸메이트 이도현. 같은 과야. 계좌도 도현이 거고."))),
                Q(L("Say you'll check the family chat first", "가족 채팅방부터 확인하겠다고 하기"), L("I'll check the family chat first.", "가족 톡방부터 볼게."), null,
                  scam
                      ? Caller(L("Don't drag Mom into this, noona. Please, there's no time.", "엄마까지 끌어들이지 마, 누나. 제발, 시간 없어."))
                      : Caller(L("Go ahead. I called Mom before you, she wrote it there.", "봐 봐. 누나보다 엄마한테 먼저 전화했어, 거기 써 놨대."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:33", Caller(L("Noona? Are you sending it?", "누나? 보내고 있어?"))),
                Beat("16:40", Caller(L("The guy at the counter keeps looking at me. Noona, please.", "카운터 직원이 자꾸 쳐다봐. 누나, 제발."))),
                Beat("16:47", Caller(L("Dohyun has a class at five. He can't wait around much longer.", "도현이 5시에 수업 있대. 오래 못 기다린대."), L(null, "도현이 다섯 시에 수업 있대. 오래 못 기다린대."))),
                Beat("16:53", Caller(L("Seven minutes. They're closing the repair desk.", "7분 남았어. 접수 창구 닫는대."), L(null, "칠 분 남았어. 접수 창구 닫는대."))),
                Beat("16:57", Caller(L("Three minutes! Noona!", "3분 남았어! 누나!"), L(null, "삼 분 남았어! 누나!"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? L("You hung up. Mom had written at 15:52 that the real Minjun was in the library, texting from his own phone.", "전화를 끊었다. 엄마가 15:52에 썼듯이, 진짜 민준이는 도서관에서 자기 폰으로 톡을 하고 있었다.")
                        : L("You hung up on your own brother. He had called Mom first, and she wrote it in the family chat at 15:31.", "친동생의 전화를 끊어 버렸다. 민준이는 먼저 엄마한테 전화했고, 엄마가 15:31에 가족 채팅방에 남겨 두었다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Forget it. Thanks for nothing.", "됐어. 누나 진짜 너무하다.")) }
                        : new List<ConvLine> { Caller(L("It's five, they've closed. I'll figure something out. Bye, noona.", "5시야, 접수 끝났대. 내가 알아서 해 볼게. 끊어, 누나."), L(null, "다섯 시야, 접수 끝났대. 내가 알아서 해 볼게. 끊어, 누나.")) },
                    consequence = scam
                        ? L("The caller gave up at five. The real Minjun was in the library the whole time.", "상대는 5시에 포기했다. 진짜 민준이는 내내 도서관에 있었다.")
                        : L("Five o'clock passed. Minjun's phone stayed broken for the night.", "5시가 지났다. 민준이의 폰은 그날 밤에도 고장 난 채였다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -RepairCost,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Got it. Thanks, noona. I'll call you later.", "들어왔대. 고마워, 누나. 나중에 전화할게.")) }
                        : new List<ConvLine> { Caller(L("Dohyun got it! You're the best, noona. Snacks on Saturday, I promise.", "도현이한테 들어왔대! 누나가 최고야. 토요일에 간식 꼭 사 갈게.")) },
                    consequence = scam
                        ? L("The ₩280,000 went to an account in Lee Dohyun's name and was gone within minutes. The real Minjun was in the library, his phone on silent.",
                            "28만 원은 이도현 명의 계좌로 들어가 몇 분 만에 사라졌다. 진짜 민준이는 폰을 무음으로 해 둔 채 도서관에 있었다.")
                        : L("The ₩280,000 reached Minjun's roommate, and the phone was fixed by six. Mom's message in the family chat had explained the strange number.",
                            "28만 원은 민준이의 룸메이트에게 들어갔고, 폰은 6시 전에 고쳐졌다. 모르는 번호는 엄마가 가족 채팅방에 남긴 글로 설명이 됐다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = BrotherAccount, bank = HanbitBank, amount = RepairCost, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> BrotherScamPapers()
        {
            string lead = L("\"Noona, it's me. I broke my phone.\" Students and parents across Mapo took that call on Wednesday, from a number they did not know and in a voice they did. The caller needed a screen repair paid before 5 p.m., into a \"roommate's\" account.",
                            "\"누나, 나야. 폰이 깨졌어.\" 수요일, 마포 일대의 대학생과 부모들이 모르는 번호로, 그러나 아는 목소리로 걸려 온 전화를 받았다. 상대는 오후 5시 전에 액정 수리비를 '룸메이트' 계좌로 보내 달라고 했다.");
            string facts = L("Police say the voices were copied from short videos on social media. The phone and the account belonged to a real student who had lent both for a \"part-time job\", so a lookup showed nothing wrong.",
                             "경찰은 SNS의 짧은 영상에서 목소리를 복제한 것으로 보고 있다. 전화와 계좌는 '알바'라는 말에 속아 명의를 빌려준 실제 대학생의 것이어서, 조회해도 이상한 점이 나오지 않았다.");
            string quote = L("\"Don't test the voice. Ask someone else in the family,\" a Mapo police officer said. \"One line in the chat you already share usually settles it.\"",
                             "마포경찰서 관계자는 \"목소리를 시험하지 말고 다른 가족에게 물어보라\"며 \"원래 쓰던 가족 채팅방에 한 줄이면 대개 답이 나온다\"고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, L("\"Noona, my phone broke\": copied voices call families", "'누나, 나 폰 깨졌어': 복제된 목소리가 가족에게 전화한다"),
                      L("The number was new, the voice familiar. The real brothers were in class.", "번호는 낯설고 목소리는 익숙했다. 진짜 동생들은 수업 중이었다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("You hung up. Mom had just written in the family chat: the real Minjun was texting from the library.", "전화를 끊었다. 엄마가 방금 가족 채팅방에 썼다. 진짜 민준이는 도서관에서 톡을 하고 있었다.")),
                Paper(Outcome.GoAlong, L("280,000 won for a phone that was never broken", "깨진 적 없는 폰에 수리비 28만 원"),
                      L("A Mangwon student paid for her \"brother's\" repair. He was in the library.", "망원동 대학생, '동생' 수리비 송금… 동생은 도서관에 있었다"),
                      lead + "\n\n" + L("A 22-year-old student sent 280,000 won. Her brother, a first-year in Daejeon, had texted their mother from the library half an hour earlier.",
                                        "22세 대학생 한 명은 28만 원을 보냈다. 대전에서 대학에 다니는 1학년 남동생은 30분 전 도서관에서 어머니에게 메시지를 보낸 상태였다.") + " " + facts + "\n\n" + quote,
                      L("The family chat had the answer: at 15:52 Mom wrote that Minjun had just texted her from his own phone.", "답은 가족 채팅방에 있었다. 15:52에 엄마가 민준이가 방금 자기 폰으로 톡을 보냈다고 썼다.")),
                Paper(Outcome.Timeout, L("The voice was his. The call was not.", "목소리는 동생, 전화는 남"),
                      L("Families in Mapo were phoned by copied voices on Wednesday.", "수요일, 복제된 목소리가 마포의 가족들에게 전화를 걸었다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("Nothing was sent, but you never decided. One look at the family chat would have told you.", "돈은 보내지 않았지만 결정도 내리지 못했다. 가족 채팅방만 봤어도 알 수 있었다.")),
            };
        }

        static List<EndPaper> BrotherLegitPapers()
        {
            string lead = L("After a month of copied-voice scams, a real call for help can sound exactly like a fake one. A first-year student in Daejeon found that out on Wednesday, when he broke his phone and rang his sister in Seoul from his roommate's.",
                            "복제 음성 사기가 한 달째 이어지면서, 진짜 도움 요청도 가짜와 똑같이 들리게 됐다. 대전의 한 대학 신입생은 수요일에 그걸 실감했다. 폰이 깨져 룸메이트 폰으로 서울의 누나에게 전화를 걸었을 때였다.");
            string advice = L("Police advise families to agree on one place to check: the chat they already share. \"If someone else in the family confirms it there, the strange number is explained,\" an officer said.",
                              "경찰은 가족끼리 확인할 곳을 하나 정해 두라고 권한다. 원래 쓰던 채팅방이다. 경찰 관계자는 \"다른 가족이 거기서 확인해 주면 모르는 번호는 설명이 된다\"고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, L("A broken phone, and a sister who checked first", "깨진 폰, 그리고 먼저 확인한 누나"),
                      L("He called their mother first. His sister read her message, then paid.", "동생은 먼저 어머니에게 전화했다. 누나는 어머니의 글을 읽고 나서 돈을 보냈다."),
                      lead + "\n\n" + L("\"I called Mom first from my roommate's phone, and she wrote it in our family chat,\" he said. \"My sister read it, asked me one question, and sent the money. The phone was fixed by six.\"",
                                        "그는 \"먼저 룸메이트 폰으로 엄마한테 전화했고, 엄마가 가족 채팅방에 써 줬다\"며 \"누나는 그걸 읽고, 하나만 물어보고, 돈을 보내 줬다. 6시 전에 폰을 고쳤다\"고 말했다.") + "\n\n" + advice,
                      L("It really was Minjun, and you checked: Mom's message in the family chat explained the strange number.", "정말 민준이였고, 당신은 확인했다. 엄마가 가족 채팅방에 남긴 글이 모르는 번호를 설명해 주었다.")),
                Paper(Outcome.Refuse, L("\"She hung up on me\": when the real brother calls", "'누나가 끊었어요': 진짜 동생이 전화했을 때"),
                      L("A student with a broken phone could not convince his own sister.", "폰이 깨진 대학생은 친누나를 설득하지 못했다."),
                      lead + "\n\n" + L("\"She hung up on me,\" he said. \"I don't blame her. But Mom had written it in our family chat an hour before.\" His roommate lent him the money.",
                                        "그는 \"누나가 전화를 끊었다\"면서도 \"탓할 수는 없다. 다만 한 시간 전에 엄마가 가족 채팅방에 써 뒀었다\"고 말했다. 수리비는 룸메이트가 빌려줬다.") + "\n\n" + advice,
                      L("This one was real: Mom had written in the family chat that he was calling from his roommate's phone.", "이번엔 진짜였다. 엄마가 민준이가 룸메이트 폰으로 전화한다고 가족 채팅방에 써 두었다.")),
                Paper(Outcome.Timeout, L("The repair desk closed at five", "접수 창구는 5시에 닫혔다"),
                      L("A student waited on the line for a sister who never decided.", "대학생은 끝내 결정하지 못한 누나를 전화로 기다렸다."),
                      lead + "\n\n" + L("\"I held on until they closed,\" he said. \"It was all in the family chat.\" His roommate lent him the money.",
                                        "그는 \"접수가 끝날 때까지 전화를 붙들고 있었다\"며 \"가족 채팅방에 다 써 뒀는데\"라고 말했다. 수리비는 룸메이트가 빌려줬다.") + "\n\n" + advice,
                      L("It really was Minjun, and the answer was in the family chat. Deciding in time matters too.", "정말 민준이였고, 답은 가족 채팅방에 있었다. 제때 결정하는 것도 중요하다.")),
            };
        }

        // ================================================================ what the case leaves behind

        /// <summary>Thursday after "Minjun's broken phone" (built for the day being built).</summary>
        static IEnumerable<DayEcho> BrotherEchoes()
        {
            string minjun = L("Minjun", "민준이"), mom = L("Mom", "엄마");
            return OfCase(CaseBrother,
                // The rent went out before the call, and what Mom wrote that afternoon is still in the chat.
                EchoBank(2, Truth.Any, Any, LandlordPaid, Oct(7, "15:10"), OctoberRent, -Rent),
                EchoChat(2, Truth.Scam, Any, FamilyChat, mom, "pt_mom", Oct(7, "15:52"), MomLibrary),
                EchoChat(2, Truth.Legit, Any, FamilyChat, mom, "pt_mom", Oct(7, "15:31"), MomBroken),

                // A copied voice: the real Minjun knew nothing.
                EchoCard(2, Truth.Scam, Sent, L("The \"Minjun\" on the phone was a copied voice. The real one was in the library all afternoon.", "전화 속 '민준이'는 복제된 목소리였다. 진짜 민준이는 오후 내내 도서관에 있었다.")),
                EchoMine(2, Truth.Scam, Sent, FamilyChat, Oct(7, "17:10"), L("Minjun, did Dohyun get the money? Is your phone fixed?", "민준아, 도현이한테 돈 들어갔대? 폰은 고쳤어?")),
                EchoChat(2, Truth.Scam, Sent, FamilyChat, minjun, "pt_minjun", Oct(7, "17:14"), L("?? What money? My phone is fine. Who is Dohyun??", "?? 무슨 돈? 내 폰 멀쩡한데. 도현이가 누구야??"), true),
                EchoChat(2, Truth.Scam, Sent, FamilyChat, mom, "pt_mom", Oct(7, "17:20"), L("Jiwoo, call the bank and 112 right now. I'm stepping out of my meeting.", "지우야, 지금 바로 은행이랑 112에 전화해. 엄마 회의 나와서 전화할게.")),

                EchoCard(2, Truth.Scam, HungUp, L("Yesterday's \"Minjun\" was a copied voice. Mom's message in the family chat gave it away.", "어제의 '민준이'는 복제된 목소리였다. 가족 채팅방의 엄마 글이 그걸 알려 줬다.")),
                EchoMine(2, Truth.Scam, HungUp, FamilyChat, Oct(7, "16:50"), L("Minjun, did you just call me from someone else's phone?", "민준아, 방금 다른 사람 폰으로 나한테 전화했어?")),
                EchoChat(2, Truth.Scam, HungUp, FamilyChat, minjun, "pt_minjun", Oct(7, "16:53"), L("No?? I'm in the library. Why", "아니?? 나 도서관인데. 왜")),
                EchoMine(2, Truth.Scam, HungUp, FamilyChat, Oct(7, "16:54"), L("Someone with your voice wanted 280,000 won for a broken phone.", "네 목소리로 누가 폰 깨졌다고 28만 원 보내 달래.")),
                EchoChat(2, Truth.Scam, HungUp, FamilyChat, minjun, "pt_minjun", Oct(7, "16:55"), L("WHAT. That's creepy. Good thing you asked!!", "헐. 소름. 물어봐서 다행이다!!"), true),

                EchoCard(2, Truth.Scam, TooLate, L("Yesterday's \"Minjun\" gave up at five. The real one never knew.", "어제의 '민준이'는 5시에 포기했다. 진짜 민준이는 아무것도 몰랐다.")),

                // It was Minjun: the phone is fixed, with Jiwoo's money or his roommate's.
                EchoCard(2, Truth.Legit, Sent, L("Minjun's phone is fixed. He owes you 280,000 won and a bag of snacks.", "민준이 폰은 고쳐졌다. 민준이는 28만 원과 간식 한 봉지를 빚졌다.")),
                EchoChat(2, Truth.Legit, Sent, FamilyChat, minjun, "pt_minjun", Oct(7, "17:48"), L("Phone's alive!! Thank you noona ♥ Paying you back on the 25th.", "폰 살아났다!! 누나 고마워 ♥ 25일에 갚을게."), true),

                EchoCard(2, Truth.Legit, Kept, L("That really was Minjun. His roommate lent him the money; you paid Dohyun back last night.", "정말 민준이였다. 수리비는 룸메이트가 빌려줬고, 당신은 어젯밤 도현이에게 갚아 주었다.")),
                EchoChat(2, Truth.Legit, Kept, FamilyChat, minjun, "pt_minjun", Oct(7, "18:20"),
                         L("Noona... that really was me. Dohyun lent me the money. Can you send it to him tonight?", "누나… 그거 진짜 나였어. 도현이가 빌려줘서 고쳤어. 오늘 밤에 도현이한테 보내 줄 수 있어?"), true),
                EchoMine(2, Truth.Legit, Kept, FamilyChat, Oct(7, "18:31"), L("Sorry!! Sent it just now.", "미안!! 방금 보냈어.")),
                EchoBank(2, Truth.Legit, Kept, Roommate, Oct(7, "18:30"), L($"Minjun's repair · Hanbit {BrotherAccount}", $"민준이 폰 수리비 · 한빛 {BrotherAccount}"), -RepairCost));
        }
    }
}
