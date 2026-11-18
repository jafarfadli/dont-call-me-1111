using System.Collections.Generic;
using System.Linq;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 4, Friday 9 October (Hangul Day, no classes): "Yuna's stolen bag", the first video call.
    /// Yuna flew to Fukuoka that morning with her cousin Somin. At 16:20 a video call comes from a
    /// number Jiwoo does not know, and Yuna's face is on it: her bag was snatched at Hakata station,
    /// and could Jiwoo send 650,000 won for the hotel to Somin's account before five?
    /// <list type="bullet">
    /// <item>Scam: someone wears Yuna's face, made from her public videos. Its lips trail its voice,
    /// its edges shimmer, and when it waves a hand in front of it the face stays on top of the hand.
    /// The real Yuna sent Jiwoo a photo from a ramen queue at 15:58, from her own phone.</item>
    /// <item>Legit: it is Yuna. Her lips match her voice, her hand covers her face as it passes, and
    /// she wrote to Jiwoo from the hotel lobby PC at 15:47 that she would call from Somin's phone.</item>
    /// </list>
    /// Three clues: the face, the wave (both circled on the video) and Yuna's own chat. The
    /// computer cannot tell the truths apart: the number and the account are a real Kim Somin's.
    /// The notebook is empty, as on Day 3: the player writes the claims down.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string Day4Dir = "Assets/_Game/Data/Day4";
        const string SominNumber = "010-4870-2263";
        const string SominAccount = "620-418-905372";
        const long HotelMoney = 650000;
        const string YunaNumber = "010-7780-1123";
        const string YunaChat = "yuna";

        static string SominAccountSpoken => L("six two zero, four one eight, nine zero five three seven two", "육이공, 사일팔, 구공오삼칠이");
        // Yuna's own voices are still to be chosen for ElevenLabs; until then she speaks with Flo.
        static string VoiceYuna => L("Flo (English (US))", "Flo (Korean (South Korea))");
        static string Somin => L("KIM SOMIN", "김소민");
        static string AtYunaChat => L("Chats → Yuna", "채팅 → 유나");

        // What Yuna wrote to Jiwoo that afternoon, in one truth or the other.
        static string YunaRamen => L("Ramen queue, 40 minutes. Worth it??", "라멘 줄 40분째. 이거 맞아??");
        static string YunaSnatched => L("Jiwoo!! Someone snatched my bag at Hakata station. Phone, wallet, cards, all gone. Writing from the hotel lobby PC. I'll video call you from Somin's phone!!",
                                        "지우야!! 하카타역에서 가방 날치기 당했어. 폰, 지갑, 카드 다 없어. 지금 호텔 로비 PC로 쓰는 중. 소민이 폰으로 영상통화 걸게!!");

        static DayData BuildDay4()
        {
            var legit = Day4Variant(false);
            var scam = Day4Variant(true);

            var d = ScriptableObject.CreateInstance<DayData>();
            d.day = 4;
            d.help = NotebookHelp.Write;
            d.level = 4;
            d.newToday = L("Video calls. A face can be faked too: watch the lips against the voice, and ask the caller to wave a hand slowly in front of the face. Click the face to note what you see.",
                           "영상통화. 얼굴도 가짜일 수 있다. 입 모양이 목소리와 맞는지 보고, 얼굴 앞에서 천천히 손을 흔들어 달라고 하라. 얼굴을 클릭하면 본 것이 노트에 적힌다.");
            d.dateLabel = DateLabel(today);
            d.shortLabel = ShortLabel(4, today);
            d.weekday = WeekdayLabel(today);
            d.place = L("Mangwon-dong, Seoul", "서울 마포구 망원동");
            d.startTime = "16:20";
            d.intro = L("Hangul Day: no classes. Yuna flew to Fukuoka this morning with her cousin Somin, for the long weekend.",
                        "한글날이라 수업이 없다. 유나는 오늘 아침 사촌 소민이와 연휴 동안 후쿠오카로 떠났다.");
            d.ringDelay = 6f;
            d.variants = new List<DayVariant> { scam, legit };
            d.echoes = Day4Echoes();
            d.nextDateLabel = DateLabel(today.AddDays(1));
            d.builtWith = Version;
            return Store(d, Localized(Day4Dir + "/Day4.asset"));
        }

        static List<NewsItem> Day4Local() => new List<NewsItem>
        {
            News(L("Hangul Day at Mangwon market", "망원시장의 한글날"), L("Free calligraphy stalls by the main gate, 11:00-17:00.", "정문 앞 무료 붓글씨 체험, 11:00~17:00.")),
            News(L("Library hours", "도서관 운영 시간"), L("Mapo Central Library is closed today and opens at 9:00 on Saturday.", "마포중앙도서관은 오늘 휴관하고 토요일 9시에 문을 연다.")),
            News(L("Long weekend at the airports", "연휴 공항 혼잡"), L("Gimpo and Incheon expect their busiest Friday since the summer.", "김포와 인천 공항은 여름 이후 가장 붐비는 금요일을 예상한다.")),
        };

        // The question only Yuna can answer, and what the caller says to it.
        static string YunaQuizDodge => L("Jiwoo, I'm stuck in Japan without a wallet and you're giving me a quiz?", "지우야, 나 지갑도 없이 일본에 갇혔는데 지금 퀴즈를 내?");
        static string YunaQuizAnswer => L("Tteokbokki at the market, and you burned your tongue on the fish cake.", "시장 떡볶이. 너 어묵 먹다가 혀 데었잖아.");

        static DayVariant Day4Variant(bool scam)
        {
            SetDay(4, "16:20");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();

            // ---- the paper on the desk: yesterday's case in front (once Day 3 is played), today's warning in the box
            FrontPage(room, Day3ScamPapers().Find(p => p.outcome == Outcome.Refuse));
            var n = room.newspaper;
            n.warningTitle = L("VIDEO CALLS CAN BE FAKED TOO", "영상통화도 가짜일 수 있다");
            n.warningText = L("Scammers now put a friend's face on a live video call, made from a few of their public videos. Watch the lips against the voice, and ask for a slow wave in front of the face: a pasted face stays on top of the hand. Then ask them where you always talk.",
                              "사기범들은 이제 공개된 영상 몇 개로 친구의 얼굴을 만들어 실시간 영상통화에 씌운다. 입 모양이 목소리와 맞는지 보고, 얼굴 앞에서 천천히 손을 흔들어 달라고 하라. 씌운 얼굴은 손 위에 그대로 떠 있다. 그리고 늘 연락하던 곳에서 본인에게 물어보라.");
            n.local = Day4Local();

            // ---- what Yuna wrote that afternoon, in her chat with Jiwoo
            string yuna = L("Yuna", "유나");
            if (scam)
                AddChat(phone, YunaChat, 9, "15:58", yuna, "pt_yuna", YunaRamen, L("The queue outside a ramen shop near Hakata station", "하카타역 근처 라멘집 앞 줄"));
            else
                AddChat(phone, YunaChat, 9, "15:47", yuna, "pt_yuna", YunaSnatched);

            // ---- what the computer knows: a real Kim Somin with an old phone and an old account, in both truths
            directory.numbers.Add(new DirNumber { number = SominNumber, owner = Somin, note = L("Mobile phone, since 2019 · roaming in Japan", "휴대전화 · 2019년 가입 · 일본 로밍 중") });
            directory.accounts.Add(new DirAccount { bank = HanbitBank, number = SominAccount, holder = Somin, note = L("Personal account, opened 2019", "개인 계좌 · 2019년 개설") });

            var v = StoreVariant(scam ? "scam" : "legit", CaseYuna, Day4Dir, scam ? "Day4_Scam" : "Day4_Legit", Day4Call(scam), phone, room, directory);
            v.marks = new List<MarkDef>
            {
                MarkVideo("video_face", "yuna", "face"),
                MarkVideo("video_wave", "yuna", "wave"),
                MarkChat("chat_yuna", "stolen", YunaChat, scam ? YunaRamen : YunaSnatched),
                MarkCall("call_quiz", "yuna", scam ? YunaQuizDodge : YunaQuizAnswer),
                // Decoys: true in both truths, deciding nothing.
                MarkChat("chat_landed", "yuna", YunaChat, YunaLanded),
                MarkNote("cal_trip", "stolen", YunaTripNote),
                MarkContact("contact_yuna", "yuna", YunaNumber),
                MarkCheck("check_number", "yuna", SominNumber),
                MarkCheck("check_account", "stolen", SominAccount),
            };
            v.clues = new List<ClueDef>
            {
                Clue("face", scam ? L("Her lips trailed her voice and the edges of her face shimmered: a face put on by software.", "입 모양이 목소리보다 늦었고 얼굴 가장자리가 일렁였다. 소프트웨어로 씌운 얼굴이었다.")
                                  : L("Her lips moved with her words, and the picture held steady.", "입 모양이 말과 맞았고 화면도 흔들림 없었다."),
                     L("Video call → circle her face", "영상통화 → 얼굴에 동그라미"), Circled("video_face")),
                Clue("wave", scam ? L("When she waved, her face stayed on top of her hand: only her fingertips showed.", "손을 흔들자 얼굴이 손 위에 그대로 떠 있었다. 손가락 끝만 보였다.")
                                  : L("When she waved, her hand covered her face as it passed.", "손을 흔들자 손이 지나가며 얼굴을 가렸다."),
                     L("Video call → ask her to wave, then circle it", "영상통화 → 손을 흔들어 달라고 한 뒤 동그라미"), Circled("video_wave")),
                Clue("yuna_chat", scam ? L("Yuna sent you a photo from a ramen queue at 15:58, from her own phone.", "유나는 15:58에 자기 폰으로 라멘 줄 사진을 보냈다.")
                                       : L("Yuna wrote to you from the hotel lobby PC at 15:47: her bag was snatched, and she would call from Somin's phone.", "유나는 15:47에 호텔 로비 PC로 썼다. 가방을 날치기 당했고 소민이 폰으로 전화하겠다고."),
                     AtYunaChat, Circled("chat_yuna")),
                Clue("quiz", scam ? L("She dodged a question only Yuna could answer: what you ate after class yesterday.", "유나만 답할 수 있는 질문을 피했다. 어제 수업 끝나고 뭘 먹었는지.")
                                  : L("She knew what you ate after class yesterday: tteokbokki, and your burnt tongue.", "어제 수업 끝나고 뭘 먹었는지 알고 있었다. 떡볶이, 그리고 데인 혀까지."),
                     L("The call → ask what you ate yesterday", "통화 → 어제 뭐 먹었는지 물어보기"), Circled("call_quiz")),
            };
            v.papers = scam ? Day4ScamPapers() : Day4LegitPapers();
            v.ruleTitle = scam ? L("A face can be copied too", "얼굴도 복제된다") : L("Test the face, then help", "얼굴을 시험하고, 그다음에 도와주라");
            v.rule = scam
                ? L("A friend's face on a video call can be faked from her public videos. Watch the lips against the voice, ask for a slow wave in front of the face, and ask her in the chat you already share.",
                    "영상통화 속 친구의 얼굴은 공개된 영상으로 가짜를 만들 수 있다. 입 모양과 목소리를 맞춰 보고, 얼굴 앞에서 천천히 손을 흔들어 달라고 하고, 원래 쓰던 채팅방에서 물어보라.")
                : L("The same checks clear a real call: her lips matched her voice, her hand covered her face, and she had written in your chat herself.",
                    "같은 확인이 진짜 전화도 가려 준다. 입 모양은 목소리와 맞았고, 손은 얼굴을 가렸고, 본인이 채팅방에 직접 써 두었다.");
            v.ruleSource = PaperSource(10);
            return v;
        }

        // ================================================================ the call

        static ConversationData Day4Call(bool scam)
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? L("Day 4 · Yuna's stolen bag (scam)", "4일차 · 유나의 도난당한 가방 (사기)") : L("Day 4 · Yuna's stolen bag (legit)", "4일차 · 유나의 도난당한 가방 (진짜)");
            c.isScam = scam;
            c.revealAtEnd = false;
            // Somin's number: the incoming screen cannot show a face; the video does.
            c.caller = new CallerInfo { displayName = L("Yuna", "유나"), number = SominNumber, inContacts = false, portrait = "pt_unknown", voice = VoiceYuna };
            c.video = new VideoCall
            {
                face = "vc_yuna",
                faked = scam,
                faceNote = scam
                    ? L("Her lips keep moving after her voice stops, and the edges of her face shimmer now and then.", "목소리가 멈춘 뒤에도 입이 계속 움직이고, 얼굴 가장자리가 가끔 일렁인다.")
                    : L("Her lips move with her words. The picture is grainy, but her face holds steady.", "입 모양이 말과 맞는다. 화질은 거칠지만 얼굴은 흔들림 없다."),
                waveNote = scam
                    ? L("When her hand passed in front of her face, the face stayed on top of the hand: only her fingertips showed above it.", "손이 얼굴 앞을 지나갈 때 얼굴이 손 위에 그대로 떠 있었다. 그 위로 손가락 끝만 보였다.")
                    : L("Her hand covered her face as it passed. Nothing slid or flickered.", "손이 지나가며 얼굴을 가렸다. 미끄러지거나 깜빡이는 것은 없었다."),
            };
            c.claims = new List<ClaimDef>
            {
                Claim("yuna", L("\"It's me, Yuna, on Somin's phone.\"", "\"나야, 유나. 소민이 폰이야.\""), !scam,
                      scam ? L("False. The face was pasted on: its lips trailed the voice and it stayed on top of the hand when it waved. The real Yuna was in a ramen queue with her own phone.",
                               "거짓. 씌운 얼굴이었다. 입 모양이 목소리보다 늦었고, 손을 흔들자 얼굴이 손 위에 떠 있었다. 진짜 유나는 자기 폰을 들고 라멘 줄에 서 있었다.")
                           : L("True. Her lips matched her voice, her hand covered her face as she waved, and she had written that she would call from Somin's phone.",
                               "사실. 입 모양이 목소리와 맞았고, 손을 흔들자 손이 얼굴을 가렸고, 소민이 폰으로 전화하겠다고 미리 써 두었다.")).Via(TrickBorrowedFace),
                Claim("stolen", L("\"My bag was snatched at Hakata station an hour ago.\"", "\"한 시간 전에 하카타역에서 가방을 날치기 당했어.\""), !scam,
                      scam ? L("False. At 15:58 Yuna sent you a photo from a ramen queue, from her own phone. A lookup of Somin proves nothing either way.",
                               "거짓. 유나는 15:58에 자기 폰으로 라멘 줄 사진을 보냈다. 소민이를 조회해도 어느 쪽인지는 알 수 없었다.")
                           : L("True. She wrote to you from the hotel lobby PC at 15:47, in your own chat.", "사실. 유나가 15:47에 호텔 로비 PC로 두 사람의 채팅방에 직접 썼다.")).Via(TrickStranded),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 45f,
                      L("Oh no! Are you okay?", "헐! 괜찮아?"), "okay",
                      L("Why a video call?", "왜 영상통화야?"), "why",
                      P(0.5f, L("Jiwoo? Can you hear me?", "지우야? 들려?"))),
                    Caller(L("Jiwoo! Can you see me? It's me, Yuna! I'm on Somin's phone.", "지우야! 보여? 나야, 유나! 소민이 폰이야.")).Says("yuna"),
                    Caller(L("Someone snatched my bag at Hakata station an hour ago. My phone, my wallet, my cards, everything.", "한 시간 전에 하카타역에서 누가 내 가방을 날치기했어. 폰, 지갑, 카드 전부 다.")).Says("stolen")),
                Node("okay", "ask",
                    Caller(L("I'm fine, just shaky. Somin is right here with me.", "난 괜찮아, 좀 떨릴 뿐이야. 소민이가 옆에 있어."))),
                Node("why", "ask",
                    Caller(L("So you can see it's really me! Everyone says there are scams everywhere.", "진짜 나인 거 보라고! 요즘 사기가 그렇게 많다잖아."))),
                Hold("ask",
                    Caller(L("The hotel wants both nights paid by five or they give the room away, and I booked with the card that was in my bag.", "호텔에서 5시까지 이틀 치를 안 내면 방을 다른 사람한테 준대. 예약한 카드가 가방에 있었거든."),
                           L("The hotel wants both nights paid by five or they give the room away, and I booked with the card that was in my bag.", "호텔에서 다섯 시까지 이틀 치를 안 내면 방을 다른 사람한테 준대. 예약한 카드가 가방에 있었거든.")),
                    Caller(L("Somin paid for the flights and her card is maxed out. Can you send 650,000 won to her account?", "비행기 표는 소민이가 냈는데 카드 한도가 다 찼어. 소민이 계좌로 65만 원만 보내 줄 수 있어?"),
                           L("Somin paid for the flights and her card is maxed out. Can you send six hundred and fifty thousand won to her account?", "비행기 표는 소민이가 냈는데 카드 한도가 다 찼어. 소민이 계좌로 육십오만 원만 보내 줄 수 있어?"),
                           F(FactKind.Amount, "650,000")),
                    Caller(L($"Hanbit Bank {SominAccount}, Kim Somin.", $"한빛은행 {SominAccount}, 김소민."),
                           L($"Hanbit Bank, {SominAccountSpoken}. Kim Somin.", $"한빛은행 {SominAccountSpoken}, 김소민."),
                           F(FactKind.Account, SominAccount)),
                    Caller(L("I'll pay you back on Monday, I promise. I'll stay on the line.", "월요일에 꼭 갚을게, 약속해. 끊지 않고 기다릴게."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("Yuna's stolen bag", "유나의 도난당한 가방"),
                claimedIdentity = L("Yuna, your friend, on a video call from her cousin's phone", "사촌 폰으로 영상통화를 건 친구 유나"),
                ask = L($"Send ₩650,000 for the hotel to Kim Somin: Hanbit Bank {SominAccount}", $"호텔비 65만 원을 김소민 명의 한빛은행 {SominAccount}로 송금"),
                deadline = "17:00",
                deadlineReason = L("before the hotel gives the room away", "호텔이 방을 넘기기 전"),
                objective = L("Is that really Yuna? Check before 17:00, then give your verdict on the call: send the money, or hang up.",
                              "정말 유나일까? 17:00 전까지 확인한 뒤, 통화 화면에서 판정을 내리자: 돈을 보낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Send the money", "돈 보내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("Okay, I'm sending it to Somin now.", "알았어, 지금 소민이한테 보낼게."),
                refuseLine = L("I'll talk to you in our chat, Yuna. Bye.", "유나야, 우리 채팅방에서 얘기하자. 끊을게."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask her to wave a hand in front of her face", "얼굴 앞에서 손을 흔들어 달라고 하기"),
                  L("Can you wave your hand slowly in front of your face? Humour me.", "얼굴 앞에서 손 한번 천천히 흔들어 줄래? 그냥 해 줘."), null,
                  scam
                      ? new[]
                      {
                          Caller(L("Seriously? Fine, like this.", "진심이야? 알았어, 이렇게.")).Act("wave"),
                          Caller(L("There. Happy now? The signal here is terrible.", "됐지? 이제 됐어? 여기 신호 진짜 안 좋아.")),
                      }
                      : new[]
                      {
                          Caller(L("Ha, you've been reading the warnings. Like this?", "하, 너 경고문 읽었구나. 이렇게?")).Act("wave"),
                          Caller(L("Smart. Somin says hi.", "똑똑하네. 소민이가 안녕이래.")),
                      }),
                Q(L("Ask what you ate on Thursday", "목요일에 뭐 먹었는지 묻기"), L("What did we eat after class yesterday?", "어제 수업 끝나고 우리 뭐 먹었지?"), null,
                  scam
                      ? Caller(YunaQuizDodge)
                      : Caller(YunaQuizAnswer)),
                Q(L("Ask her to write to you in your chat", "채팅방에 직접 써 달라고 하기"), L("Can you write to me in our chat first?", "우리 채팅방에 먼저 한 줄 써 줄래?"), null,
                  scam
                      ? Caller(L("My phone is in the bag, Jiwoo! That's the whole problem.", "폰이 가방에 있다니까, 지우야! 그게 문제잖아."))
                      : Caller(L("I already did! From the hotel lobby PC, before I called. Look.", "벌써 썼어! 전화하기 전에 호텔 로비 PC로. 봐 봐."))),
                Q(L("Ask who Somin is", "소민이가 누군지 묻기"), L("Somin is your cousin, right?", "소민이가 네 사촌이지?"), null,
                  Caller(L("Yes! Kim Somin, my cousin. I told you on Thursday night.", "응! 김소민, 내 사촌. 목요일 밤에 말했잖아."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:33", Caller(L("Jiwoo? Are you sending it? The man at the front desk keeps looking at us.", "지우야? 보내고 있어? 프런트 직원이 계속 우리 쳐다봐."))),
                Beat("16:40", Caller(L("Somin says the room goes to someone else at five.", "소민이가 5시에 방이 다른 사람한테 간대."), L(null, "소민이가 다섯 시에 방이 다른 사람한테 간대."))),
                Beat("16:47", Caller(L("Please, Jiwoo. We've got nowhere else to sleep tonight.", "제발, 지우야. 오늘 밤에 잘 데가 없어."))),
                Beat("16:53", Caller(L("Seven minutes. Please.", "7분 남았어. 제발."), L(null, "칠 분 남았어. 제발."))),
                Beat("16:57", Caller(L("Three minutes! Jiwoo!", "3분 남았어! 지우야!"), L(null, "삼 분 남았어! 지우야!"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? L("You hung up. The real Yuna had sent you a photo from a ramen queue at 15:58, from her own phone.", "전화를 끊었다. 진짜 유나는 15:58에 자기 폰으로 라멘 줄 사진을 보냈었다.")
                        : L("You hung up on your best friend. She had written to you from the hotel lobby at 15:47 that she would call from Somin's phone.", "가장 친한 친구의 전화를 끊어 버렸다. 유나는 15:47에 호텔 로비에서 소민이 폰으로 전화하겠다고 써 두었다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Forget it. Thanks a lot.", "됐어. 정말 고맙다.")) }
                        : new List<ConvLine> { Caller(L("It's five. They gave the room away. We'll figure something out. Bye.", "5시야. 방 넘어갔대. 어떻게든 해 볼게. 끊어."), L(null, "다섯 시야. 방 넘어갔대. 어떻게든 해 볼게. 끊어.")) },
                    consequence = scam
                        ? L("The caller gave up at five. The real Yuna was eating ramen the whole time.", "상대는 5시에 포기했다. 진짜 유나는 내내 라멘을 먹고 있었다.")
                        : L("Five o'clock passed. The hotel gave the room away; Yuna and Somin spent the night at an internet cafe.", "5시가 지났다. 호텔은 방을 넘겼고, 유나와 소민이는 그날 밤을 PC방에서 보냈다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -HotelMoney,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Got it! Thanks, Jiwoo. I'll call you later.", "들어왔다! 고마워, 지우야. 나중에 전화할게.")) }
                        : new List<ConvLine> { Caller(L("It came through! You're a lifesaver. Somin says she owes you dinner.", "들어왔어! 네가 살렸다. 소민이가 저녁 쏜대.")) },
                    consequence = scam
                        ? L("The ₩650,000 went to an account in Kim Somin's name and was gone within minutes. The real Yuna had her phone, her bag and no idea.",
                            "65만 원은 김소민 명의 계좌로 들어가 몇 분 만에 사라졌다. 진짜 유나는 폰도 가방도 그대로였고 아무것도 몰랐다.")
                        : L("The ₩650,000 reached Somin, and the room was paid by five. Yuna's own message in your chat had explained the strange number.",
                            "65만 원은 소민이에게 들어갔고, 방값은 5시 전에 치렀다. 모르는 번호는 유나가 채팅방에 직접 남긴 글로 설명이 됐다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = SominAccount, bank = HanbitBank, amount = HotelMoney, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> Day4ScamPapers()
        {
            string lead = L("\"Can you see me? It's me.\" On Hangul Day, students in Mapo took video calls from friends travelling abroad: a familiar face, a stolen bag, a hotel to pay before five, into a cousin's account.",
                            "\"보여? 나야.\" 한글날, 마포의 대학생들이 해외여행 중인 친구들의 영상통화를 받았다. 익숙한 얼굴, 도난당한 가방, 5시 전에 내야 하는 호텔비, 그리고 사촌 명의의 계좌.");
            string facts = L("Police say the faces were made from the friends' public travel videos and laid over the callers in real time. The phone and the account belonged to a real woman who had lent both for a \"side job\", so a lookup showed nothing wrong.",
                             "경찰은 친구들이 올린 여행 영상으로 얼굴을 만들어 실시간으로 씌운 것으로 보고 있다. 전화와 계좌는 '부업'이라는 말에 속아 명의를 빌려준 실제 여성의 것이어서, 조회해도 이상한 점이 나오지 않았다.");
            string quote = L("\"Ask for a slow wave in front of the face,\" a Mapo police officer said. \"A pasted face stays on top of the hand. And ask your friend in the chat you already share.\"",
                             "마포경찰서 관계자는 \"얼굴 앞에서 천천히 손을 흔들어 달라고 하라. 씌운 얼굴은 손 위에 그대로 떠 있다. 그리고 원래 쓰던 채팅방에서 친구에게 물어보라\"고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, L("\"It's me\": friends' faces copied onto video calls", "'나야': 친구 얼굴을 씌운 영상통화"),
                      L("The faces were familiar. The friends were eating ramen in Fukuoka.", "얼굴은 익숙했다. 진짜 친구들은 후쿠오카에서 라멘을 먹고 있었다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("You hung up. The real Yuna had just sent you a photo from her own phone.", "전화를 끊었다. 진짜 유나는 방금 자기 폰으로 사진을 보낸 참이었다.")),
                Paper(Outcome.GoAlong, L("650,000 won for a bag that was never stolen", "도난당한 적 없는 가방에 65만 원"),
                      L("A Mangwon student paid her \"friend's\" hotel. Her friend was in a ramen queue.", "망원동 대학생, '친구' 호텔비 송금… 친구는 라멘 줄에 서 있었다"),
                      lead + "\n\n" + L("A 22-year-old student sent 650,000 won. Her friend, on holiday in Fukuoka, had sent her a photo from a ramen queue twenty minutes before the call.",
                                        "22세 대학생 한 명은 65만 원을 보냈다. 후쿠오카에서 휴가 중이던 친구는 전화 20분 전에 라멘 줄 사진을 보낸 상태였다.") + " " + facts + "\n\n" + quote,
                      L("Her own chat had the answer: Yuna had sent a photo from her own phone at 15:58.", "답은 채팅방에 있었다. 유나는 15:58에 자기 폰으로 사진을 보냈다.")),
                Paper(Outcome.Timeout, L("The face was hers. The call was not.", "얼굴은 친구, 전화는 남"),
                      L("Students in Mapo were video-called by copied faces on Hangul Day.", "한글날, 복제된 얼굴이 마포의 대학생들에게 영상통화를 걸었다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("Nothing was sent, but you never decided. A wave, or one look at your chat, would have told you.", "돈은 보내지 않았지만 결정도 내리지 못했다. 손 한 번, 채팅방 한 번이면 알 수 있었다.")),
            };
        }

        static List<EndPaper> Day4LegitPapers()
        {
            string lead = L("After a month of faked faces, a real video call for help can look exactly like a fake one. A student on holiday in Fukuoka found that out on Hangul Day, when her bag was snatched at Hakata station and she called her friend in Seoul from her cousin's phone.",
                            "가짜 얼굴 사기가 한 달째 이어지면서, 진짜 도움 요청 영상통화도 가짜와 똑같아 보이게 됐다. 후쿠오카에서 휴가를 보내던 한 대학생은 한글날 그걸 실감했다. 하카타역에서 가방을 날치기 당해 사촌의 폰으로 서울의 친구에게 전화를 걸었을 때였다.");
            string advice = L("Police advise friends to do two things before sending money: ask for a slow wave in front of the face, and look for a message from the friend's own account. \"If both are right, help,\" an officer said.",
                              "경찰은 돈을 보내기 전에 두 가지를 하라고 권한다. 얼굴 앞에서 천천히 손을 흔들어 달라고 하고, 친구 본인 계정에서 온 메시지를 찾아보라는 것이다. 경찰 관계자는 \"둘 다 맞으면 도와주라\"고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, L("A stolen bag, and a friend who checked first", "도난당한 가방, 그리고 먼저 확인한 친구"),
                      L("She had written from the hotel lobby before she called. Her friend read it, then paid.", "그녀는 전화하기 전에 호텔 로비에서 글을 남겼다. 친구는 그걸 읽고 나서 돈을 보냈다."),
                      lead + "\n\n" + L("\"She asked me to wave in front of my face, read my message, and sent the money,\" she said. \"We kept the room.\"",
                                        "그녀는 \"친구가 얼굴 앞에서 손을 흔들어 보라고 하더니, 내 메시지를 읽고 돈을 보내 줬다\"며 \"덕분에 방을 지켰다\"고 말했다.") + "\n\n" + advice,
                      L("It really was Yuna, and you checked: her face held when she waved, and her own message explained the strange number.", "정말 유나였고, 당신은 확인했다. 손을 흔들어도 얼굴은 그대로였고, 유나가 직접 남긴 글이 모르는 번호를 설명해 주었다.")),
                Paper(Outcome.Refuse, L("\"She hung up on me\": when the real friend calls", "'친구가 끊었어요': 진짜 친구가 전화했을 때"),
                      L("A student robbed in Fukuoka could not convince her best friend.", "후쿠오카에서 가방을 도난당한 대학생은 가장 친한 친구를 설득하지 못했다."),
                      lead + "\n\n" + L("\"She hung up,\" she said. \"I don't blame her. But I had written it in our chat before I called.\" Her cousin borrowed the money from the hotel's owner.",
                                        "그녀는 \"친구가 전화를 끊었다\"면서도 \"탓할 수는 없다. 다만 전화하기 전에 우리 채팅방에 써 뒀었다\"고 말했다. 돈은 사촌이 호텔 주인에게 빌렸다.") + "\n\n" + advice,
                      L("This one was real: her lips matched her voice, and she had written in your chat that she would call from Somin's phone.", "이번엔 진짜였다. 입 모양은 목소리와 맞았고, 유나는 소민이 폰으로 전화하겠다고 채팅방에 써 두었다.")),
                Paper(Outcome.Timeout, L("The room went at five", "방은 5시에 넘어갔다"),
                      L("A student in Fukuoka waited on the line for a friend who never decided.", "후쿠오카의 대학생은 끝내 결정하지 못한 친구를 영상통화로 기다렸다."),
                      lead + "\n\n" + L("\"I held on until five,\" she said. \"It was all in our chat.\" She and her cousin spent the night at an internet cafe.",
                                        "그녀는 \"5시까지 전화를 붙들고 있었다\"며 \"채팅방에 다 써 뒀는데\"라고 말했다. 그녀와 사촌은 그날 밤을 PC방에서 보냈다.") + "\n\n" + advice,
                      L("It really was Yuna, and the answer was in your chat and on her face. Deciding in time matters too.", "정말 유나였고, 답은 채팅방과 그녀의 얼굴에 있었다. 제때 결정하는 것도 중요하다.")),
            };
        }

        // ================================================================ what Thursday left behind

        static List<DayEcho> Day4Echoes()
        {
            SetDay(4, "16:20");
            var echoes = new List<DayEcho>(Day3ParcelEchoes());
            echoes.AddRange(Day3DentistEchoes());
            // Two days back and more: what was written and moved is still there, the day card's line is not.
            echoes.AddRange(StillThere(OfCase(CaseRent, Day2RentEchoes())));
            echoes.AddRange(StillThere(BrotherEchoes()));
            echoes.AddRange(StillThere(Day1Echoes()));
            echoes.AddRange(StillThere(OfCase(CaseProtected, Day1ProtectedEchoes())));
            echoes.AddRange(PaperEchoes(false));
            echoes.AddRange(SaleEchoes(false));
            return echoes;
        }

        /// <summary>After "the held parcel": where the monitor is now.</summary>
        static DayEcho[] Day3ParcelEchoes()
        {
            string delivered = L("[Hangang Express] Delivered: monitor, left at the door of 302.", "[한강익스프레스] 배송 완료: 모니터, 302호 문 앞.");
            return OfCase(CaseParcel,
                EchoChat(3, Truth.Scam, Any, CourierChat, Courier, "av_courier", Oct(9, "10:12"), delivered, true),
                EchoChat(3, Truth.Legit, Sent, CourierChat, Courier, "av_courier", Oct(9, "10:12"), delivered, true),
                EchoChat(3, Truth.Legit, Kept, CourierChat, Courier, "av_courier", Oct(8, "17:05"),
                         L("[Hangang Express] The customs duty was not paid by 17:00: your monitor is going back to the sender.", "[한강익스프레스] 관세가 17:00까지 납부되지 않아 모니터가 발송인에게 반송됩니다.")),
                EchoCard(3, Truth.Scam, Sent, L("Yesterday's \"customs\" call was a scam: the ₩56,600 is gone. Your monitor arrived this morning, its taxes long paid.", "어제의 '세관' 전화는 사기였다. 5만 6,600원은 돌아오지 않는다. 모니터는 오늘 아침 도착했다. 세금은 진작 낸 상태였다.")),
                EchoCard(3, Truth.Scam, HungUp, L("Your monitor arrived this morning. Nothing was ever due: yesterday's caller was one digit off the courier's number.", "모니터는 오늘 아침 도착했다. 낼 돈은 처음부터 없었다. 어제 전화는 택배사 번호와 한 자리가 달랐다.")),
                EchoCard(3, Truth.Scam, TooLate, L("Yesterday's \"customs\" caller gave up at five. Your monitor arrived this morning anyway.", "어제의 '세관' 전화는 5시에 포기했다. 모니터는 어차피 오늘 아침 도착했다.")),
                EchoCard(3, Truth.Legit, Sent, L("The duty went through in time. Your new monitor arrived this morning.", "관세는 제때 냈다. 새 모니터는 오늘 아침 도착했다.")),
                EchoCard(3, Truth.Legit, Kept, L("The duty wasn't paid in time: your monitor is on its way back to Incheon.", "관세를 제때 내지 못했다. 모니터는 인천으로 되돌아가는 중이다.")));
        }

        /// <summary>After "the dentist's deposit".</summary>
        static DayEcho[] Day3DentistEchoes()
        {
            return OfCase(CaseDentist,
                // What Jiwoo wrote on the calendar on Thursday is still there.
                EchoNote(3, Truth.Scam, Any, 8, DentistToday(true), DentistNote, "board_calendar_moved"),
                EchoNote(3, Truth.Scam, Any, DentistMovedTo, DentistMoved),
                EchoNote(3, Truth.Legit, Any, 8, DentistToday(false), DentistNote),
                EchoCard(3, Truth.Scam, Sent, L("Yesterday's \"clinic\" never had your booking: the ₩30,000 went to a stranger. Your real appointment is still on the 22nd.", "어제의 '치과'는 당신의 예약을 가진 적이 없었다. 3만 원은 모르는 사람에게 갔다. 진짜 예약은 여전히 22일이다.")),
                EchoCard(3, Truth.Scam, HungUp, L("Your appointment is still on the 22nd, where you moved it. Yesterday's \"deposit\" call was not from the clinic.", "예약은 옮겨 둔 대로 22일이다. 어제의 '예약금' 전화는 치과가 아니었다.")),
                EchoCard(3, Truth.Scam, TooLate, L("Yesterday's \"clinic\" gave up at five. Your appointment is still on the 22nd.", "어제의 '치과'는 5시에 포기했다. 예약은 여전히 22일이다.")),
                EchoCard(3, Truth.Legit, Sent, L("The wisdom tooth came out last night. Soup for lunch again today.", "사랑니는 어젯밤에 뽑았다. 오늘 점심도 죽이다.")),
                EchoCard(3, Truth.Legit, Kept, L("The clinic gave your slot away. The next free one is in three weeks.", "치과는 당신의 자리를 넘겼다. 다음 빈자리는 3주 뒤다.")));
        }
    }
}
