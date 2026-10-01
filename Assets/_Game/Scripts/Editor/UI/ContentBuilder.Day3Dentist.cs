using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 3, another case: "the dentist's deposit". Jiwoo's calendar has said "Dentist, 18:30" for
    /// today all week. Moon Jihye, the coordinator at Saebom Dental, rings: evening appointments
    /// need a 30,000 won deposit, paid before five, or the slot goes to someone else. One clue, and
    /// it is on the calendar:
    /// <list type="bullet">
    /// <item>Legit: Jiwoo's own note for today says the clinic rings this afternoon for a 30,000
    /// won deposit that comes off the bill.</item>
    /// <item>Scam: Jiwoo moved the appointment this morning, to the 22nd, and wrote it down (the
    /// calendar's picture shows it too). The caller, working from a leaked list, still has today.</item>
    /// </list>
    /// The computer cannot tell the two apart: in both, the number is a mobile phone in the name
    /// she gives, and the account is a payment company's virtual account that does not show whose
    /// it is.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string DentistNumber = "010-5127-4406";
        const string DentistAccount = "5620-71-336029";
        const long DentistDeposit = 30000;
        const int DentistMovedTo = 22;

        static string DentistAccountSpoken => L("five six two zero, seven one, three three six zero two nine", "오육이공, 칠일, 삼삼육공이구");
        static string VoiceDental => L("Karen", "Shelley (Korean (South Korea))");

        static DayVariant DentistVariant(bool scam)
        {
            SetDay(3, "16:25");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();

            // ---- the paper on the desk: yesterday's case in front, today's warning in the box
            FrontPage(room, Day2ScamPapers().Find(p => p.outcome == Outcome.Refuse));
            var n = room.newspaper;
            n.warningTitle = L("A DEPOSIT FOR YOUR APPOINTMENT?", "예약금을 내라는 전화?");
            n.warningText = L("Clinics, salons and restaurants do take no-show deposits, and booking lists leak. A caller who knows your appointment proves little, and a payment company's virtual account does not say whose it is. What did you arrange yourself? Check your own notes.",
                              "병원, 미용실, 식당은 실제로 노쇼 방지 예약금을 받는다. 그리고 예약 명단은 유출된다. 내 예약을 안다고 해서 믿을 수는 없고, 결제대행사 가상계좌는 누구 것인지 알려 주지 않는다. 내가 직접 정한 건 무엇이었나? 내가 적어 둔 메모를 확인하라.");
            n.local = new List<NewsItem>
            {
                News(L("Booking app leak", "예약 앱 정보 유출"),
                     L("The clinic booking app DocDay says the names, phone numbers and appointment times of some users were exposed last week.", "병원 예약 앱 닥데이는 지난주 일부 이용자의 이름, 전화번호, 예약 시간이 유출됐다고 밝혔다.")),
                News(L("Water tank cleaning today", "오늘 물탱크 청소"), L("Villas on Poeun-ro are without water from 10:00 to 12:00.", "포은로 일대 빌라가 10:00~12:00 단수된다.")),
                News(L("Mangwon market by night", "망원시장 야시장"), L("Open until 23:00 on Saturday, with street food and live music.", "토요일 밤 11시까지, 길거리 음식과 공연.")),
            };

            // ---- the truth: what Jiwoo wrote on her calendar this morning
            if (scam)
            {
                Renote(room, 8, L("Dentist 18:30 → MOVED to Thu 22nd. I called them this morning", "치과 18:30 → 22일(목)로 변경. 오늘 아침에 전화함"));
                Note(room, DentistMovedTo, L("Dentist 18:30, wisdom tooth (moved from the 8th)", "치과 18:30, 사랑니 (8일에서 옮김)"));
                // The calendar's picture with the 8th crossed out and the 22nd circled (ui_art.py).
                var moved = Tex("board_calendar_moved");
                if (moved != null)
                    room.calendar.image = moved;
            }
            else
            {
                Renote(room, 8, L("Dentist 18:30, wisdom tooth. Deposit 30,000 won: they ring this afternoon, it comes off the bill",
                                  "치과 18:30, 사랑니. 예약금 3만 원: 오후에 전화 온댔음, 진료비에서 빠짐"));
            }

            // ---- what the computer knows: a mobile in the name she gives, and an account that hides its owner
            directory.numbers.Add(new DirNumber { number = DentistNumber, owner = L("MOON JIHYE", "문지혜"), note = L("Mobile phone, since 2018", "휴대전화 · 2018년 가입") });
            directory.accounts.Add(new DirAccount
            {
                bank = NuriBank, number = DentistAccount, holder = L("GAON PAY (payment agency)", "가온페이(결제대행)"),
                note = L("Virtual account of a payment company. It does not show which shop is behind it.", "결제대행사 가상계좌 · 어느 가맹점 것인지는 표시되지 않음"),
            });

            var v = StoreVariant(scam ? "dentist_scam" : "dentist_legit", CaseDentist, Day3Dir, scam ? "Day3_Dentist_Scam" : "Day3_Dentist_Legit", DentistCall(scam), phone, room, directory);
            v.intro = L("Midterms are next week. The water came back at noon, and the dentist has been on your calendar all week.",
                        "중간고사는 다음 주. 단수는 정오에 끝났고, 달력에는 일주일 내내 치과 예약이 적혀 있었다.");
            v.clues = new List<ClueDef>
            {
                scam
                    ? Clue("calendar", L("Your calendar says you moved the dentist this morning, to Thursday the 22nd. There is no appointment today to pay a deposit for.",
                                         "달력을 보니 오늘 아침 치과 예약을 22일(목)로 옮겼다. 오늘은 예약금을 낼 예약 자체가 없다."),
                           AtCalendar, new ClueWhen(ClueEvent.PanelOpened, "Calendar"))
                    : Clue("calendar", L("Your own note for today says it: dentist at 18:30, and the clinic rings this afternoon for a 30,000 won deposit.",
                                         "오늘 날짜에 내가 직접 적어 두었다. 치과 18:30, 그리고 오후에 예약금 3만 원 전화가 온다고."),
                           AtCalendar, new ClueWhen(ClueEvent.PanelOpened, "Calendar")),
            };
            v.papers = scam ? DentistScamPapers() : DentistLegitPapers();
            v.ruleTitle = scam ? L("What did you arrange yourself?", "내가 정한 건 무엇이었나") : L("Your own note is evidence too", "내 메모도 증거다");
            v.rule = scam
                ? L("A caller who knows your appointment is not proof. Your own notes say what you arranged: if they don't match the call, don't pay.",
                    "내 예약을 아는 전화라고 해서 믿을 수는 없다. 내가 무엇을 정했는지는 내 메모가 알려 준다. 전화 내용과 다르면 내지 마라.")
                : L("When your own note says the same as the caller (the time, the amount, the call itself), the request is real. Check, then pay.",
                    "내 메모가 전화 내용과 같다면(시간, 금액, 전화가 온다는 것까지) 그 요청은 진짜다. 확인하고, 그다음에 내라.");
            v.ruleSource = PaperSource(9);
            return v;
        }

        // ================================================================ the call

        static ConversationData DentistCall(bool scam)
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? L("Day 3 · The dentist's deposit (scam)", "3일차 · 치과 예약금 (사기)") : L("Day 3 · The dentist's deposit (legit)", "3일차 · 치과 예약금 (진짜)");
            c.isScam = scam;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = L("Moon Jihye", "문지혜"), number = DentistNumber, inContacts = false, portrait = "pt_dental", voice = VoiceDental };
            c.claims = new List<string>
            {
                L("She is Moon Jihye, the coordinator at Saebom Dental.", "새봄치과 문지혜 실장이라고 한다."),
                L("My appointment is today at 18:30.", "오늘 18:30에 예약이 잡혀 있다고 한다."),
                L($"Evening slots need a ₩30,000 deposit: Nuri Bank {DentistAccount}.", $"저녁 진료는 예약금 3만 원이 필요하다고 한다: 누리은행 {DentistAccount}"),
                L("Slots that are not confirmed by 17:00 go to someone else.", "17시까지 확정하지 않은 예약은 다른 환자에게 넘어간다고 한다."),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 40f,
                      L("Yes? Is there a problem?", "네, 무슨 일이세요?"), "why",
                      L("How did you get this number?", "제 번호는 어떻게 아셨어요?"), "number",
                      P(0.5f, L("Hello? Ms. Kim?", "여보세요? 김지우 님?"))),
                    Caller(L("Hello, is this Kim Jiwoo? This is Moon Jihye, the coordinator at Saebom Dental.", "안녕하세요, 김지우 님 맞으시죠? 새봄치과 문지혜 실장입니다.")),
                    Caller(L("I'm calling about your appointment today, at half past six.", "오늘 저녁 6시 30분 예약 건으로 연락드렸어요."),
                           L(null, "오늘 저녁 여섯 시 삼십 분 예약 건으로 연락드렸어요."))),
                Node("why", "ask",
                    Caller(L("No problem at all. It's just a confirmation call.", "문제는 아니고요, 예약 확인 전화예요."))),
                Node("number", "ask",
                    Caller(L("It's the number on your booking, Ms. Kim. We call every evening patient on the day.", "예약하실 때 남기신 번호예요. 저녁 진료 환자분들께는 당일에 다 전화드려요."))),
                Hold("ask",
                    Caller(L("Evening slots are short, so we take a 30,000 won deposit to hold them. It comes off your bill tonight.", "저녁 시간은 자리가 적어서 예약금 3만 원을 받고 있어요. 오늘 진료비에서 그대로 빠집니다."),
                           L("Evening slots are short, so we take a thirty thousand won deposit to hold them. It comes off your bill tonight.", "저녁 시간은 자리가 적어서 예약금 삼만 원을 받고 있어요. 오늘 진료비에서 그대로 빠집니다."),
                           F(FactKind.Amount, "30,000")),
                    Caller(L($"Please send it to our payment account: Nuri Bank {DentistAccount}.", $"결제 계좌로 보내 주세요. 누리은행 {DentistAccount}입니다."),
                           L($"Please send it to our payment account. Nuri Bank, {DentistAccountSpoken}.", $"결제 계좌로 보내 주세요. 누리은행 {DentistAccountSpoken}입니다."),
                           F(FactKind.Account, DentistAccount)),
                    Caller(L("At five we release every slot that isn't confirmed. I'll hold while you send it.", "5시가 되면 확정 안 된 예약은 다른 분께 넘어가요. 보내시는 동안 기다릴게요."),
                           L(null, "다섯 시가 되면 확정 안 된 예약은 다른 분께 넘어가요. 보내시는 동안 기다릴게요."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("The dentist's deposit", "치과 예약금"),
                claimedIdentity = L("Moon Jihye, coordinator at Saebom Dental", "새봄치과 문지혜 실장"),
                ask = L($"Pay a ₩30,000 deposit for tonight's appointment into Nuri Bank {DentistAccount}", $"오늘 저녁 예약의 예약금 3만 원을 누리은행 {DentistAccount}로 입금"),
                deadline = "17:00",
                deadlineReason = L("before unconfirmed slots are released", "미확정 예약 정리 전"),
                objective = L("Is this really your dentist, about an appointment you really have? Check before 17:00, then give your verdict on the call: pay the deposit, or hang up.",
                              "정말 내가 다니는 치과일까? 정말 그 예약이 있는 걸까? 17:00 전까지 확인한 뒤, 통화 화면에서 판정을 내리자: 예약금을 낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Pay the deposit", "예약금 내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("Okay, I'm sending the deposit now.", "네, 지금 예약금 보낼게요."),
                refuseLine = L("I'm not paying a deposit over the phone. Goodbye.", "전화로는 예약금 안 낼게요. 끊을게요."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask which day and time she has you down for", "예약 날짜와 시간을 다시 묻기"), L("Which day and time do you have me down for?", "제 예약이 며칠 몇 시로 돼 있어요?"), null,
                  Caller(L("Today, Thursday the eighth, at half past six.", "오늘, 8일 목요일 저녁 6시 30분이요."),
                         L(null, "오늘, 팔 일 목요일 저녁 여섯 시 삼십 분이요."))),
                Q(L("Ask what the appointment is for", "무슨 진료인지 묻기"), L("What am I booked in for?", "제가 무슨 진료로 예약했죠?"), null,
                  scam
                      ? Caller(L("It just says a check-up here, Ms. Kim. The doctor will go through it with you.", "여기는 그냥 검진이라고만 나와 있어요. 자세한 건 원장님이 설명해 주실 거예요."))
                      : Caller(L("Your lower-right wisdom tooth, with Dr. Jang. You booked it on the 28th.", "오른쪽 아래 사랑니 발치요, 장 원장님 진료고요. 28일에 예약하셨어요."),
                               L("Your lower-right wisdom tooth, with Dr. Jang. You booked it on the twenty eighth.", "오른쪽 아래 사랑니 발치요, 장 원장님 진료고요. 이십팔 일에 예약하셨어요."))),
                Q(L("Ask whose name is on the account", "누구 명의 계좌인지 묻기"), L("Whose name is on that account?", "그 계좌는 누구 명의예요?"), null,
                  Caller(L("It's a virtual account from our payment company, Gaon Pay. It's issued for your booking.", "결제대행사 가온페이의 가상계좌예요. 고객님 예약 건으로 발급된 거고요."))),
                Q(L("Offer to pay at the desk tonight", "이따 가서 내겠다고 하기"), L("Can't I just pay at the desk tonight?", "이따 가서 데스크에서 내면 안 돼요?"), null,
                  Caller(L("I'm afraid not for evening slots, Ms. Kim. Too many people didn't turn up, so it has to be before five.", "저녁 진료는 안 돼요, 고객님. 안 오시는 분이 너무 많아서 5시 전에 받고 있어요."),
                         L(null, "저녁 진료는 안 돼요, 고객님. 안 오시는 분이 너무 많아서 다섯 시 전에 받고 있어요."))),
            };
            c.beats = new List<PressureBeat>
            {
                Beat("16:36", Caller(L("Ms. Kim? Shall I keep your slot?", "김지우 님? 예약 유지할까요?"))),
                Beat("16:43", Caller(L("I have someone on the waiting list for half past six. Are you sending it?", "6시 30분 대기자가 한 분 계세요. 보내고 계신가요?"),
                                     L(null, "여섯 시 삼십 분 대기자가 한 분 계세요. 보내고 계신가요?"))),
                Beat("16:50", Caller(L("Ten minutes until we release the evening slots.", "저녁 예약 정리까지 10분 남았어요."), L(null, "저녁 예약 정리까지 십 분 남았어요."))),
                Beat("16:55", Caller(L("Five minutes, Ms. Kim. The next free slot is in three weeks.", "5분 남았어요, 고객님. 다음 빈 시간은 3주 뒤예요."), L(null, "오 분 남았어요, 고객님. 다음 빈 시간은 삼 주 뒤예요."))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? L("You hung up. Your calendar said you had moved the appointment that morning: nothing was booked for tonight.", "전화를 끊었다. 달력에는 그날 아침 예약을 옮겼다고 적혀 있었다. 오늘 저녁에는 예약이 없었다.")
                        : L("You hung up on your own dentist. Your calendar note said they would ring for a 30,000 won deposit this afternoon.", "다니는 치과의 전화를 끊어 버렸다. 달력 메모에는 오후에 예약금 3만 원 전화가 온다고 적혀 있었다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("It's five, Ms. Kim. I'm releasing your slot.", "5시네요, 고객님. 예약은 취소 처리할게요."), L(null, "다섯 시네요, 고객님. 예약은 취소 처리할게요.")) }
                        : new List<ConvLine> { Caller(L("It's five, Ms. Kim. I have to give the slot to the next patient. Call us to book again.", "5시가 됐어요, 고객님. 다음 분께 자리를 드려야 해요. 다시 예약하시려면 전화 주세요."),
                                                      L(null, "다섯 시가 됐어요, 고객님. 다음 분께 자리를 드려야 해요. 다시 예약하시려면 전화 주세요.")) },
                    consequence = scam
                        ? L("The caller gave up at five. Your real appointment, on the 22nd, was never in danger.", "상대는 5시에 포기했다. 22일로 옮긴 진짜 예약에는 아무 일도 없었다.")
                        : L("Five o'clock passed. The clinic gave your slot away; the next one is in three weeks.", "5시가 지났다. 치과는 당신의 자리를 다른 환자에게 넘겼다. 다음 빈 시간은 3주 뒤다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -DentistDeposit,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Thank you. Your slot is confirmed. See you tonight.", "감사합니다. 예약 확정됐어요. 이따 뵐게요.")) }
                        : new List<ConvLine> { Caller(L("Thank you, it's in. See you at half past six, Ms. Kim.", "입금 확인됐어요. 6시 30분에 뵐게요, 고객님."), L(null, "입금 확인됐어요. 여섯 시 삼십 분에 뵐게요, 고객님.")) },
                    consequence = scam
                        ? L("The ₩30,000 went into a payment company's virtual account that had nothing to do with your dentist. You had moved the appointment to the 22nd that morning.",
                            "3만 원은 치과와 아무 관계 없는 결제대행사 가상계좌로 들어갔다. 예약은 그날 아침 22일로 옮겨 둔 상태였다.")
                        : L("The deposit held your slot. The wisdom tooth came out at 18:30, and the ₩30,000 came off the bill.", "예약금으로 자리가 확정됐다. 사랑니는 18:30에 뽑았고, 3만 원은 진료비에서 빠졌다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = DentistAccount, bank = NuriBank, amount = DentistDeposit, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> DentistScamPapers()
        {
            string lead = L("Patients of clinics across Mapo were phoned on Thursday by a \"coordinator\" who knew their name and the time of their appointment. Evening slots, she said, now needed a 30,000 won deposit before 5 p.m.",
                            "목요일, 마포 일대 병원 환자들에게 이름과 예약 시간을 아는 '실장'의 전화가 걸려 왔다. 그는 저녁 진료는 오후 5시 전에 예약금 3만 원을 내야 한다고 했다.");
            string facts = L("The details came from last week's leak at the booking app DocDay. The list was a week old: patients who had since moved or cancelled were called about appointments they no longer had. The deposits went into a payment company's virtual account, which hides the name of whoever is behind it.",
                             "이 정보는 지난주 예약 앱 닥데이에서 유출된 것이었다. 명단은 일주일 전 것이어서, 그사이 예약을 옮기거나 취소한 환자들도 이미 없는 예약으로 전화를 받았다. 예약금은 뒤에 누가 있는지 드러나지 않는 결제대행사 가상계좌로 들어갔다.");
            string quote = L("\"We do take deposits,\" a Mapo dentist said, \"and we tell the patient so when they book. If you didn't arrange it yourself, it isn't ours.\"",
                             "마포의 한 치과 원장은 \"예약금을 받긴 한다\"면서 \"다만 예약할 때 환자에게 미리 말씀드린다. 본인이 정한 적 없는 예약금이라면 저희 것이 아니다\"라고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, L("Fake \"deposit\" calls follow a booking app leak", "예약 앱 유출 뒤 가짜 '예약금' 전화"),
                      L("The callers knew the appointment. They didn't know it had been moved.", "상대는 예약을 알고 있었다. 예약이 옮겨진 건 몰랐다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("You hung up. Your own calendar said the appointment had been moved to the 22nd.", "전화를 끊었다. 달력에는 예약을 22일로 옮겼다고 적혀 있었다.")),
                Paper(Outcome.GoAlong, L("A deposit for an appointment that had been moved", "옮겨진 예약에 낸 예약금"),
                      L("A Mangwon student paid 30,000 won to hold a slot she no longer had.", "망원동 대학생, 이미 없는 예약에 3만 원 송금"),
                      lead + "\n\n" + L("A 22-year-old student paid. She had moved her appointment that very morning and written the new date on her calendar.",
                                        "22세 대학생 한 명은 돈을 보냈다. 그는 바로 그날 아침 예약을 옮기고 새 날짜를 달력에 적어 둔 상태였다.") + " " + facts + "\n\n" + quote,
                      L("Your calendar said it: you had moved the dentist to the 22nd that morning.", "달력에 적혀 있었다. 그날 아침 치과 예약을 22일로 옮겼다고.")),
                Paper(Outcome.Timeout, L("\"Pay by five or lose your slot\"", "'5시까지 안 내면 예약 취소'"),
                      L("Clinic patients were phoned with week-old booking data.", "일주일 전 예약 정보로 환자들에게 전화가 걸려 왔다."),
                      lead + "\n\n" + facts + "\n\n" + quote,
                      L("Nothing was sent, but you never decided. One look at your calendar would have settled it.", "돈은 보내지 않았지만 결정도 내리지 못했다. 달력만 한 번 봤어도 알 수 있었다.")),
            };
        }

        static List<EndPaper> DentistLegitPapers()
        {
            string lead = L("One patient in five did not turn up for an evening appointment last year, Mapo's dentists say. Since September many neighbourhood clinics take a deposit by phone on the day, usually 30,000 won, which comes off the bill.",
                            "마포 지역 치과들에 따르면 지난해 저녁 예약 환자 다섯 명 중 한 명은 나타나지 않았다. 9월부터는 당일에 전화로 예약금을 받는 동네 병원이 많아졌다. 보통 3만 원이고, 진료비에서 뺀다.");
            string quote = L("\"We tell every patient when they book that we will ring,\" said Moon Jihye, coordinator at Saebom Dental in Mangwon-dong. \"The ones who wrote it down pay in a minute. The ones who didn't take me for a scammer.\"",
                             "망원동 새봄치과 문지혜 실장은 \"예약하실 때 전화드린다고 모든 환자분께 말씀드린다\"며 \"적어 두신 분은 1분이면 내시고, 안 적어 두신 분은 저를 사기꾼으로 아신다\"고 말했다.");
            string advice = L("After last week's leak at the booking app DocDay, the Mapo Consumer Centre says a caller who knows your appointment proves nothing by that alone. What counts is what you arranged yourself.",
                              "지난주 예약 앱 닥데이 유출 이후, 마포소비자센터는 내 예약을 안다는 것만으로는 아무것도 증명되지 않는다고 말한다. 중요한 건 내가 직접 정한 내용이다.");
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, L("No-show deposits reach the neighbourhood dentist", "동네 치과까지 온 노쇼 예약금"),
                      L("Clinics now phone evening patients for a deposit, and say so when the patient books.", "저녁 진료 예약금을 전화로 받는 병원들, 예약할 때 미리 알린다"),
                      lead + "\n\n" + quote + "\n\n" + advice,
                      L("It was your dentist, and you checked: your own note had the time, the amount and the call.", "다니는 치과가 맞았고, 당신은 확인했다. 내 메모에 시간과 금액, 전화가 온다는 것까지 적혀 있었다.")),
                Paper(Outcome.Refuse, L("Clinics lose evening patients to scam fears", "사기 걱정에 끊기는 병원 전화"),
                      L("A real deposit call sounds just like a fake one.", "진짜 예약금 전화도 가짜처럼 들린다."),
                      lead + "\n\n" + quote + L(" On Thursday three of her evening patients hung up and lost their slots.", " 목요일에는 저녁 예약 환자 세 명이 전화를 끊어 예약이 취소됐다.") + "\n\n" + advice,
                      L("This one was real: your own calendar note said the clinic would ring for a 30,000 won deposit.", "이번엔 진짜였다. 달력 메모에 치과에서 예약금 3만 원 전화가 온다고 적혀 있었다.")),
                Paper(Outcome.Timeout, L("The evening slots went at five", "저녁 예약은 5시에 정리됐다"),
                      L("Patients who never decided were moved three weeks back.", "결정을 미룬 환자들의 예약은 3주 뒤로 밀렸다."),
                      lead + "\n\n" + quote + "\n\n" + advice,
                      L("It was your dentist, and the note was on your calendar. Deciding in time matters too.", "다니는 치과가 맞았고, 메모는 달력에 있었다. 제때 결정하는 것도 중요하다.")),
            };
        }
    }
}
