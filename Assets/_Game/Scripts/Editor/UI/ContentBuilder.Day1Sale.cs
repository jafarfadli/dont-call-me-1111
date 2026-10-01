using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 1, another case: "the extra zero". Jiwoo is selling her old monitor on Neighbour Market
    /// for 60,000 won. The buyer, Bae Sujin, rings in a panic: she paid in advance and typed one
    /// zero too many, and wants the extra 540,000 won back before her own rent is due. One clue,
    /// and it is in the bank app:
    /// <list type="bullet">
    /// <item>Legit: the bank history shows 600,000 won from BAE SUJIN at 16:07.</item>
    /// <item>Scam: the bank history shows no deposit at all.</item>
    /// </list>
    /// The computer cannot tell the two apart: in both, the number and the account are in the name
    /// she gives, years old and without reports.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string SaleNumber = "010-3378-9024";
        const string SaleAccount = "620-481-207356";
        const long SaleSent = 600000;
        const long SaleReturn = 540000;

        static string SaleAccountSpoken => L("six two zero, four eight one, two zero seven three five six", "육이공, 사팔일, 이공칠삼오육");
        static string VoiceBuyer => L("Tessa", "Flo (Korean (South Korea))");
        static string Buyer => L("BAE SUJIN", "배수진");
        static string SaleMemo => L("Monitor (Neighbour Market)", "모니터 (이웃마켓)");

        static DayVariant SaleVariant(bool scam)
        {
            SetDay(1, "16:20");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();

            // ---- the paper on the desk: yesterday's case in front, today's warning in the box
            FrontPage(room, Day0Papers().Find(p => p.outcome == Outcome.Refuse));
            var n = room.newspaper;
            n.warningTitle = L("SELLING SECOND-HAND? READ YOUR OWN BANK", "중고 거래 중이라면 내 통장부터");
            n.warningText = L("\"I paid you too much, send back the difference\" is an honest slip on some days and a trick on others, and the buyer's name and account can be real either way. A transfer arrives within seconds. Only your own bank history shows whether the money came, and from whom.",
                              "'돈을 더 보냈으니 차액을 돌려 달라'는 말은 진짜 실수일 때도 있고 속임수일 때도 있다. 어느 쪽이든 구매자의 이름과 계좌는 진짜일 수 있다. 이체는 몇 초면 들어온다. 돈이 정말 들어왔는지, 누가 보냈는지는 내 통장 내역만이 알려 준다.");
            n.local = Day1Local();

            // ---- the sale, on the calendar in both truths; the money, in the bank only when it was sent
            Note(room, 10, L("Monitor buyer comes at 11:00 (Neighbour Market, 60,000 won paid ahead)", "모니터 구매자 11시 방문 (이웃마켓, 6만 원 선입금)"));
            if (!scam)
                AddTx(phone, 6, "16:07", Buyer, SaleMemo, SaleSent);

            // ---- what the computer knows: the name she gives, an old phone and an old account
            directory.numbers.Add(new DirNumber { number = SaleNumber, owner = Buyer, note = L("Mobile phone, since 2017", "휴대전화 · 2017년 가입") });
            directory.accounts.Add(new DirAccount { bank = HanbitBank, number = SaleAccount, holder = Buyer, note = L("Personal account, opened 2016", "개인 계좌 · 2016년 개설") });

            var v = StoreVariant(scam ? "sale_scam" : "sale_legit", CaseSale, Day1Dir, scam ? "Day1_Sale_Scam" : "Day1_Sale_Legit", SaleCall(scam), phone, room, directory);
            v.intro = L("Classes are over. Your old monitor is sold on Neighbour Market: the buyer pays 60,000 won today and picks it up on Saturday.",
                        "수업은 끝났다. 쓰던 모니터는 이웃마켓에서 팔렸다. 구매자가 오늘 6만 원을 보내고 토요일에 가지러 온다.");
            v.clues = new List<ClueDef>
            {
                scam
                    ? Clue("deposit", L("Your bank history shows no deposit today. Nothing came from her, so there is nothing to send back.", "은행 내역에 오늘 들어온 돈이 없다. 상대가 보낸 돈이 없으니 돌려줄 돈도 없다."),
                           AtBank, new ClueWhen(ClueEvent.BankOpened))
                    : Clue("deposit", L("Your bank history shows ₩600,000 from BAE SUJIN at 16:07, ten times the price. The extra ₩540,000 is hers.", "은행 내역에 16:07 '배수진' 이름으로 60만 원이 들어와 있다. 약속한 값의 열 배다. 더 들어온 54만 원은 상대의 돈이다."),
                           AtBank, new ClueWhen(ClueEvent.BankOpened)),
            };
            v.papers = scam ? SaleScamPapers() : SaleLegitPapers();
            v.ruleTitle = scam ? L("\"I sent you too much\"? Open your bank", "'더 보냈어요'? 통장부터 열어라") : L("Return it to the name it came from", "들어온 이름 그대로 돌려주라");
            v.rule = scam
                ? L("Asked to send back money that came by mistake? Open your own bank history first. Money that never arrived cannot be returned.",
                    "잘못 보낸 돈을 돌려 달라는 말을 들으면 먼저 내 은행 내역을 열어 보라. 들어오지 않은 돈은 돌려줄 수 없다.")
                : L("When the deposit is in your own history under the caller's name, the extra is theirs. Send it back to that same name.",
                    "내 내역에 상대 이름으로 입금이 찍혀 있다면 더 들어온 돈은 그 사람 것이다. 같은 이름의 계좌로 돌려주면 된다.");
            v.ruleSource = PaperSource(7);
            return v;
        }

        // ================================================================ the call

        static ConversationData SaleCall(bool scam)
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? L("Day 1 · The extra zero (scam)", "1일차 · 0 하나 더 (사기)") : L("Day 1 · The extra zero (legit)", "1일차 · 0 하나 더 (진짜)");
            c.isScam = scam;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = L("Bae Sujin", "배수진"), number = SaleNumber, inContacts = false, portrait = "pt_buyer", voice = VoiceBuyer };
            c.claims = new List<string>
            {
                L("She is Bae Sujin, the buyer of my old monitor on Neighbour Market.", "이웃마켓에서 내 모니터를 사기로 한 배수진이라고 한다."),
                L("She meant to send ₩60,000 at 16:07 but typed ₩600,000.", "16:07에 6만 원을 보내려다 60만 원을 보냈다고 한다."),
                L($"She wants the extra ₩540,000 back: Hanbit Bank {SaleAccount}, in her name.", $"더 보낸 54만 원을 돌려 달라고 한다: 한빛은행 {SaleAccount}, 본인 명의"),
                L("It is her rent money, and her rent is due at 17:00.", "그 돈은 월세라서 17시까지 필요하다고 한다."),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 45f,
                      L("One zero too many? How much did you send?", "0을 하나 더요? 얼마를 보내셨는데요?"), "amount",
                      L("Oh. I haven't looked at my bank yet.", "아, 저 아직 통장을 안 봤는데요."), "look",
                      P(0.5f, L("Hello? Can you hear me?", "여보세요? 들리세요?"))),
                    Caller(L("Hello, is this Jiwoo, with the monitor on Neighbour Market? It's Bae Sujin, the buyer.", "여보세요, 이웃마켓에 모니터 올리신 지우 님 맞으시죠? 구매하기로 한 배수진이에요.")),
                    Caller(L("I'm so sorry, I've done something stupid. I just paid you, and my finger slipped: one zero too many.", "정말 죄송한데 제가 실수를 했어요. 방금 입금하다가 손이 미끄러져서 0을 하나 더 눌렀지 뭐예요."),
                           L(null, "정말 죄송한데 제가 실수를 했어요. 방금 입금하다가 손이 미끄러져서 영을 하나 더 눌렀지 뭐예요."))),
                Node("amount", "ask",
                    Caller(L("Six hundred thousand won instead of sixty thousand.", "6만 원을 보낸다는 게 60만 원이 나갔어요."),
                           L(null, "육만 원을 보낸다는 게 육십만 원이 나갔어요."))),
                Node("look", "ask",
                    Caller(L("It went out at seven past four. Six hundred thousand won instead of sixty thousand.", "4시 7분에 나갔어요. 6만 원을 보낸다는 게 60만 원이 나갔고요."),
                           L(null, "네 시 칠 분에 나갔어요. 육만 원을 보낸다는 게 육십만 원이 나갔고요."))),
                Hold("ask",
                    Caller(L("So you have 540,000 won of mine. Could you please send it back?", "그러니까 지우 님한테 제 돈 54만 원이 가 있어요. 그것만 돌려주실 수 있을까요?"),
                           L("So you have five hundred and forty thousand won of mine. Could you please send it back?", "그러니까 지우 님한테 제 돈 오십사만 원이 가 있어요. 그것만 돌려주실 수 있을까요?"),
                           F(FactKind.Amount, "540,000")),
                    Caller(L($"My account is Hanbit Bank {SaleAccount}, in my name, Bae Sujin.", $"제 계좌는 한빛은행 {SaleAccount}이고, 예금주는 저 배수진이에요."),
                           L($"My account is Hanbit Bank, {SaleAccountSpoken}, in my name, Bae Sujin.", $"제 계좌는 한빛은행 {SaleAccountSpoken}이고, 예금주는 저 배수진이에요."),
                           F(FactKind.Account, SaleAccount)),
                    Caller(L("It's my rent money. My landlord wants it by five today, and my bank says a reversal takes two weeks.", "그게 월세 낼 돈이에요. 집주인이 오늘 5시까지 달라는데, 은행에서는 반환 신청하면 2주 걸린대요."),
                           L(null, "그게 월세 낼 돈이에요. 집주인이 오늘 다섯 시까지 달라는데, 은행에서는 반환 신청하면 이 주 걸린대요.")),
                    Caller(L("I'll stay on the line. I'm really sorry about this.", "끊지 않고 기다릴게요. 정말 죄송해요."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("The extra zero", "0 하나 더"),
                claimedIdentity = L("Bae Sujin, the buyer of your old monitor", "모니터 구매자 배수진"),
                ask = L($"Send back the ₩540,000 she says she overpaid, to Hanbit Bank {SaleAccount}", $"더 보냈다는 54만 원을 한빛은행 {SaleAccount}로 반환"),
                deadline = "17:00",
                deadlineReason = L("before her own rent is due", "상대의 월세 납부 시한 전"),
                objective = L("Did she really send you too much? Check before 17:00, then give your verdict on the call: send the money back, or hang up.",
                              "정말 돈을 더 보냈을까? 17:00 전까지 확인한 뒤, 통화 화면에서 판정을 내리자: 돈을 돌려줄지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Send it back", "돌려주기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("Okay, I'm sending it back now.", "네, 지금 돌려 드릴게요."),
                refuseLine = L("I can't send that over the phone. Ask your bank. Goodbye.", "전화로는 못 보내요. 은행에 문의하세요. 끊을게요."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Say you'll look at your bank first", "통장부터 확인하겠다고 하기"), L("Let me look at my bank app first.", "통장 내역부터 볼게요."), null,
                  scam
                      ? Caller(L("Please do, but it can take a while to show up between banks. It has left my account, I promise.", "네, 보세요. 근데 은행이 다르면 뜨는 데 시간이 좀 걸리기도 해요. 제 통장에서는 분명히 나갔어요."))
                      : Caller(L("Of course, please do. It should be right at the top, under my name.", "네, 그럼요. 제 이름으로 맨 위에 있을 거예요."))),
                Q(L("Ask why her bank can't take it back", "왜 은행에서 못 돌려받는지 묻기"), L("Can't your bank just take it back?", "은행에서 바로 돌려받으면 안 돼요?"), null,
                  Caller(L("They said a wrong-transfer claim takes up to two weeks, and only if you agree. My rent is due today.", "착오송금 반환은 길면 2주 걸리고, 그것도 받는 분이 동의해야 한대요. 월세는 오늘까지고요."),
                         L(null, "착오송금 반환은 길면 이 주 걸리고, 그것도 받는 분이 동의해야 한대요. 월세는 오늘까지고요."))),
                Q(L("Ask whose account the money goes back to", "돌려받을 계좌가 누구 명의인지 묻기"), L("Whose account is that?", "그 계좌는 누구 명의예요?"), null,
                  Caller(L("Mine, Bae Sujin. The same name the payment came from.", "제 거예요, 배수진. 입금자 이름이랑 같아요."))),
                Q(L("Offer to settle it on Saturday", "토요일에 만나서 정산하자고 하기"), L("Can't we sort it out when you come on Saturday?", "토요일에 오실 때 정산하면 안 될까요?"), null,
                  Caller(L("I wish I could wait. My landlord won't. Please, it's only my own money I'm asking for.", "저도 그러고 싶은데 집주인이 안 기다려 줘요. 부탁드려요, 제 돈만 돌려 달라는 거예요."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:33", Caller(L("Jiwoo? Have you found it? It was at seven past four.", "지우 님? 확인하셨어요? 4시 7분이었어요."), L(null, "지우 님? 확인하셨어요? 네 시 칠 분이었어요."))),
                Beat("16:40", Caller(L("I feel terrible bothering you. I just can't be late with the rent again.", "번거롭게 해서 정말 죄송해요. 제가 월세를 또 밀릴 수는 없어서요."))),
                Beat("16:47", Caller(L("My landlord just texted me. Could you send it now, please?", "방금 집주인한테 문자가 왔어요. 지금 보내 주실 수 있을까요?"))),
                Beat("16:53", Caller(L("Seven minutes. Please, Jiwoo.", "7분 남았어요. 부탁드려요, 지우 님."), L(null, "칠 분 남았어요. 부탁드려요, 지우 님."))),
                Beat("16:57", Caller(L("Three minutes! Please just send it back!", "3분 남았어요! 제발 돌려주세요!"), L(null, "삼 분 남았어요! 제발 돌려주세요!"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? L("You hung up. Your bank history had no deposit from her: there was nothing to send back.", "전화를 끊었다. 은행 내역에는 상대가 보낸 돈이 없었다. 돌려줄 돈도 없었다.")
                        : L("You hung up on a buyer who had really overpaid. Her ₩600,000 was at the top of your bank history, under her own name.", "정말로 돈을 더 보낸 구매자의 전화를 끊어 버렸다. 60만 원은 상대의 이름으로 은행 내역 맨 위에 있었다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Forget it. I'll report you to the market.", "됐어요. 이웃마켓에 신고할게요.")) }
                        : new List<ConvLine> { Caller(L("It's five. I'll have to ask my bank, then. Two weeks...", "5시네요. 은행에 신청할 수밖에 없겠어요. 2주라니…"), L(null, "다섯 시네요. 은행에 신청할 수밖에 없겠어요. 이 주라니…")) },
                    consequence = scam
                        ? L("She gave up at five. Nothing had ever arrived from her.", "상대는 5시에 포기했다. 처음부터 들어온 돈은 없었다.")
                        : L("Five o'clock passed with her ₩540,000 still in your account.", "5시가 지났다. 상대의 54만 원은 여전히 당신 통장에 있었다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -SaleReturn,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Oh, thank you. You're a lifesaver. See you on Saturday!", "아, 감사해요. 덕분에 살았어요. 토요일에 봬요!")) }
                        : new List<ConvLine> { Caller(L("It's here! Thank you so much. I'll bring you a coffee on Saturday.", "들어왔어요! 정말 감사해요. 토요일에 커피 사 갈게요.")) },
                    consequence = scam
                        ? L("You sent ₩540,000 of your own money. Nothing had ever arrived from her, and her Neighbour Market account was gone by the evening.",
                            "당신은 자기 돈 54만 원을 보냈다. 상대가 보낸 돈은 처음부터 없었고, 그의 이웃마켓 계정은 저녁에 사라졌다.")
                        : L("Her extra ₩540,000 went back to the name it came from. The ₩60,000 for the monitor stays with you.", "더 들어온 54만 원은 보낸 사람 이름 그대로 돌아갔다. 모니터값 6만 원은 당신에게 남았다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = SaleAccount, bank = HanbitBank, amount = SaleReturn, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> SaleScamPapers()
        {
            string lead = L("Sellers on the second-hand app Neighbour Market were phoned on Tuesday by a \"buyer\" close to tears. She had paid in advance, she said, and typed one zero too many: could they send the difference back before 5 p.m., because it was her rent?",
                            "화요일, 중고 거래 앱 이웃마켓의 판매자들에게 울먹이는 '구매자'의 전화가 걸려 왔다. 선입금하다가 0을 하나 더 눌렀다며, 월세 낼 돈이니 오후 5시 전에 차액을 돌려 달라는 것이었다.");
            string facts = L("No money had been sent. Police say the callers pick sellers who have given an account number for an advance payment, and count on them not opening their bank app while someone is crying on the line.",
                             "돈은 들어온 적이 없었다. 경찰에 따르면 이들은 선입금을 받으려고 계좌번호를 알려 준 판매자를 골라, 누군가 전화로 울고 있는 동안에는 은행 앱을 열어 보지 않으리라는 점을 노린다.");
            string quote = L("\"A transfer arrives in seconds,\" a Mapo police officer said. \"If it is not in your history, it was never sent. Look before you 'return' anything.\"",
                             "마포경찰서 관계자는 \"이체는 몇 초면 들어온다\"며 \"내역에 없으면 보낸 적이 없는 것이다. 뭔가를 '돌려주기' 전에 먼저 확인하라\"고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, L("\"I paid you too much\": sellers asked to return money that never came", "'돈을 더 보냈어요': 들어오지도 않은 돈을 돌려 달라는 전화"),
                      L("Sellers who opened their bank app first lost nothing.", "은행 앱부터 열어 본 판매자들은 피해가 없었다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("You hung up. Your bank history showed that nothing had come from her.", "전화를 끊었다. 은행 내역에는 상대가 보낸 돈이 없었다.")),
                Paper(Outcome.GoAlong, L("Seller \"returns\" 540,000 won that never arrived", "들어온 적 없는 54만 원을 '돌려준' 판매자"),
                      L("A Mangwon student selling a monitor sent her own money to a fake buyer.", "모니터를 팔던 망원동 대학생, 가짜 구매자에게 자기 돈 송금"),
                      lead + "\n\n" + L("A 22-year-old student selling a 60,000-won monitor sent 540,000 won of her own. The account was in the name the caller gave, Bae Sujin; police say its owner had lent it out for a fee.",
                                        "6만 원짜리 모니터를 팔던 22세 대학생은 자기 돈 54만 원을 보냈다. 계좌는 상대가 댄 이름인 '배수진' 명의였는데, 경찰은 예금주가 돈을 받고 통장을 빌려준 것으로 보고 있다.") + "\n\n" + quote,
                      L("Your bank history had no deposit from her. There was nothing to send back.", "은행 내역에는 상대가 보낸 돈이 없었다. 돌려줄 돈은 처음부터 없었다.")),
                Paper(Outcome.Timeout, L("The \"extra zero\" calls", "'0 하나 더' 전화 주의보"),
                      L("Fake buyers ask second-hand sellers for refunds.", "가짜 구매자들이 중고 판매자에게 환불을 요구했다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("Nothing was sent, but you never gave her an answer. One look at your bank history would have settled it.", "돈은 보내지 않았지만 끝내 답을 하지 않았다. 은행 내역만 한 번 봤어도 알 수 있었다.")),
            };
        }

        static List<EndPaper> SaleLegitPapers()
        {
            string lead = L("It happens about forty times a day at Nuri Bank alone: a finger slips, one zero too many, and ten times the money is gone. On Tuesday it happened to Bae Sujin, 31, who was paying 60,000 won for a second-hand monitor in Mangwon-dong and sent 600,000.",
                            "누리은행에서만 하루 마흔 번쯤 일어나는 일이다. 손가락이 미끄러져 0이 하나 더 붙고, 열 배의 돈이 나간다. 화요일에는 배수진(31) 씨에게 그 일이 일어났다. 망원동에서 중고 모니터값 6만 원을 보내려다 60만 원을 보낸 것이다.");
            string advice = L("A bank's wrong-transfer claim can take two weeks. Nuri Bank says a seller who finds the deposit in her own history, under the caller's name, can simply send the extra back to that same name.",
                              "은행의 착오송금 반환 절차는 2주가 걸리기도 한다. 누리은행은 판매자가 자기 내역에서 상대 이름으로 들어온 입금을 확인했다면, 더 들어온 돈을 같은 이름의 계좌로 돌려주면 된다고 설명한다.");
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, L("One zero too many, and a seller who checked", "0 하나 더 보낸 구매자, 확인하고 돌려준 판매자"),
                      L("A buyer got her rent money back within the hour.", "구매자는 한 시간 만에 월세 낼 돈을 돌려받았다."),
                      lead + "\n\n" + L("\"She said, let me look at my bank first,\" Ms. Bae said. \"Then she sent it back. I paid my rent with minutes to spare.\"",
                                        "배 씨는 \"판매자가 '통장부터 볼게요' 하더라\"며 \"그러고는 바로 돌려줬다. 마감 몇 분 전에 월세를 냈다\"고 말했다.") + "\n\n" + advice,
                      L("She really had overpaid, and you checked: ₩600,000 from her own name was in your bank history.", "상대는 정말 돈을 더 보냈고, 당신은 확인했다. 은행 내역에 상대 이름으로 60만 원이 들어와 있었다.")),
                Paper(Outcome.Refuse, L("When the overpayment is real", "진짜로 더 보낸 돈일 때"),
                      L("After a wave of fake \"extra zero\" calls, a real one was hung up on.", "가짜 '0 하나 더' 전화가 판치자 진짜 전화도 끊겼다."),
                      lead + "\n\n" + L("The seller hung up on her. \"I understand why,\" Ms. Bae said. \"But the money was sitting in her account with my name on it.\" She paid her rent late.",
                                        "판매자는 전화를 끊었다. 배 씨는 \"이해는 한다\"면서도 \"그 돈은 제 이름이 찍힌 채 그분 통장에 있었다\"고 말했다. 그는 월세를 늦게 냈다.") + "\n\n" + advice,
                      L("This one was real: ₩600,000 from BAE SUJIN was at the top of your bank history.", "이번엔 진짜였다. 은행 내역 맨 위에 '배수진' 이름으로 60만 원이 들어와 있었다.")),
                Paper(Outcome.Timeout, L("The rent, and one zero too many", "월세, 그리고 0 하나 더"),
                      L("A buyer waited on the line for a seller who never decided.", "구매자는 끝내 결정하지 못한 판매자를 전화로 기다렸다."),
                      lead + "\n\n" + L("\"I held the line until five,\" Ms. Bae said. \"She only had to open her bank app.\"",
                                        "배 씨는 \"5시까지 전화를 붙들고 있었다\"며 \"은행 앱만 열어 보면 되는 일이었다\"고 말했다.") + "\n\n" + advice,
                      L("She really had overpaid, and the deposit was in your bank history. Deciding in time matters too.", "상대는 정말 돈을 더 보냈고, 입금은 은행 내역에 있었다. 제때 결정하는 것도 중요하다.")),
            };
        }

        // ================================================================ what the case leaves behind

        /// <summary>Wednesday and Thursday after "the extra zero" (built for the day being built).</summary>
        static IEnumerable<DayEcho> SaleEchoes(bool nextDay)
        {
            string yuna = L("Yuna", "유나");
            var list = new List<DayEcho>
            {
                // The buyer's money did come, whatever Jiwoo did; kept, it went back on Wednesday morning.
                EchoBank(1, Truth.Legit, Any, Buyer, Oct(6, "16:07"), SaleMemo, SaleSent),
                EchoBank(1, Truth.Legit, Kept, Buyer, Oct(7, "08:20"), L($"Overpayment returned · Hanbit {SaleAccount}", $"착오 입금 반환 · 한빛 {SaleAccount}"), -SaleReturn),
            };
            if (nextDay)
                list.AddRange(new[]
                {
                    EchoCard(1, Truth.Scam, Sent, L("Nothing had ever arrived from yesterday's \"buyer\". Your ₩540,000 is gone.", "어제의 '구매자'는 돈을 보낸 적이 없었다. 54만 원은 돌아오지 않는다.")),
                    EchoMine(1, Truth.Scam, Sent, "yuna", Oct(6, "18:05"), L("I just sent 540,000 won to a fake monitor buyer. I didn't even look at my bank first.", "나 방금 가짜 모니터 구매자한테 54만 원 보냈어. 통장도 안 보고…")),
                    EchoChat(1, Truth.Scam, Sent, "yuna", yuna, "pt_yuna", Oct(6, "18:07"),
                             L("NO. Report it right now, 112 and your bank!! I'm coming over with tteokbokki.", "헐 안 돼. 지금 당장 신고해, 112랑 은행!! 떡볶이 사 들고 갈게."), true),

                    EchoCard(1, Truth.Scam, HungUp, L("Yesterday's \"buyer\" had never sent a won. You looked at your bank, and hung up.", "어제의 '구매자'는 한 푼도 보낸 적이 없었다. 당신은 통장을 확인하고 전화를 끊었다.")),
                    EchoMine(1, Truth.Scam, HungUp, "yuna", Oct(6, "17:25"), L("Someone tried the \"I paid you too much\" trick on my monitor listing!", "내 모니터 글에 '돈 더 보냈어요' 수법 쓰는 사람 있었어!")),
                    EchoChat(1, Truth.Scam, HungUp, "yuna", yuna, "pt_yuna", Oct(6, "17:27"), L("The classic!! Did you check your bank first?", "고전 수법이네!! 통장부터 확인했어?"), true),
                    EchoMine(1, Truth.Scam, HungUp, "yuna", Oct(6, "17:28"), L("Obviously. Nothing there.", "당연하지. 아무것도 없었어.")),

                    EchoCard(1, Truth.Scam, TooLate, L("Yesterday's \"buyer\" gave up at five. Nothing had ever arrived from her.", "어제의 '구매자'는 5시에 포기했다. 들어온 돈은 처음부터 없었다.")),

                    EchoCard(1, Truth.Legit, Sent, L("The buyer got her ₩540,000 back in time for her rent. She collects the monitor on Saturday.", "구매자는 54만 원을 제때 돌려받아 월세를 냈다. 모니터는 토요일에 가지러 온다.")),
                    EchoCard(1, Truth.Legit, Kept, L("The buyer really had sent ₩600,000. You returned the extra this morning, with an apology.", "구매자는 정말로 60만 원을 보냈었다. 오늘 아침 사과와 함께 차액을 돌려주었다.")),
                });
            return OfCase(CaseSale, list.ToArray());
        }
    }
}
