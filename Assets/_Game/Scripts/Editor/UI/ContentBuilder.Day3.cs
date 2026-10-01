using System.Collections.Generic;
using System.Linq;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Day 3, Thursday 8 October (Hard): "Yoon Seora from Hangang Express's customs desk" says the
    /// monitor Jiwoo ordered from AliStar is held at Incheon for unpaid duty and VAT. She knows the
    /// item and the tracking number in both variants (last month's AliStar leak, in today's paper).
    /// <list type="bullet">
    /// <item>Legit: the Parcels app shows the duty and the same account, AliStar's shipping email says
    /// taxes were not included, the call comes from Hangang Express's official 1588-5520 and the
    /// account is a virtual account in the courier's name.</item>
    /// <item>Scam: the monitor has already cleared customs with taxes paid, the call comes from
    /// 1588-5502 (one digit off), the payment text is a web-sent text from that number, and the
    /// account is "KR CUSTOMS CLEARANCE". CheckFirst knows nothing about either (a new number).</item>
    /// </list>
    /// </summary>
    public static partial class ContentBuilder
    {
        const string Day3LegitNumber = "1588-5520";
        const string Day3ScamNumber = "1588-5502";
        const string Day3LegitAccount = "5620-44-018830";
        const string Day3ScamAccount = "620-557-301144";
        const long Duty = 56600;

        static string ShippedSubject => L("Your order has shipped: 27-inch monitor", "주문하신 상품이 발송되었습니다: 27인치 모니터");

        static DayData BuildDay3()
        {
            var legit = Day3Variant(false);
            var scam = Day3Variant(true);

            var d = ScriptableObject.CreateInstance<DayData>();
            d.day = 3;
            d.dateLabel = DateLabel(today);
            d.shortLabel = ShortLabel(3, today);
            d.weekday = WeekdayLabel(today);
            d.place = L("Mangwon-dong, Seoul", "서울 마포구 망원동");
            d.startTime = "16:25";
            d.intro = L("Midterms are next week. The water came back at noon, and the monitor you ordered should arrive any day now.",
                        "중간고사는 다음 주. 단수는 정오에 끝났고, 주문한 모니터는 곧 도착할 것이다.");
            d.ringDelay = 6f;
            d.variants = new List<DayVariant> { scam, legit };
            d.echoes = Day3Echoes();
            d.nextDateLabel = DateLabel(today.AddDays(1));
            d.builtWith = Version;
            return Store(d, Localized(Day3Dir + "/Day3.asset"));
        }

        static DayVariant Day3Variant(bool scam)
        {
            SetDay(3, "16:25");
            var phone = HouseholdPhone();
            var room = HouseholdRoom();
            var directory = HouseholdDirectory();
            string account = scam ? Day3ScamAccount : Day3LegitAccount;
            string bank = scam ? HanbitBank : NuriBank;

            // ---- the paper on the desk (its front page is yesterday's case once Day 2 is played)
            var n = room.newspaper;
            var fallback = Day2ScamPapers().Find(p => p.outcome == Outcome.Refuse);
            n.headline = fallback.headline;
            n.subhead = fallback.subhead;
            n.body = new List<string>(fallback.body.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries));
            n.warningTitle = L("PARCEL TEXTS: CHECK THE ADDRESS", "택배 문자: 주소를 확인하라");
            n.warningText = L("Fake \"unpaid duty\" texts link to look-alike sites. Don't tap the link: open your courier's own app or website yourself and see whether anything is due.",
                              "가짜 '관세 미납' 문자는 비슷하게 생긴 사이트로 연결된다. 링크를 누르지 말고, 택배사 앱이나 공식 홈페이지를 직접 열어 낼 돈이 있는지 확인하라.");
            n.local = new List<NewsItem>
            {
                new NewsItem { title = L("AliStar data leak", "알리스타 개인정보 유출"),
                               text = L("AliStar says the names, phone numbers and recent orders of some Korean customers were exposed last month. No card details were taken, the company says.",
                                        "알리스타는 지난달 일부 한국 고객의 이름, 전화번호, 최근 주문 내역이 유출됐다고 밝혔다. 카드 정보는 유출되지 않았다고 회사 측은 설명했다.") },
                new NewsItem { title = L("Water tank cleaning today", "오늘 물탱크 청소"), text = L("Villas on Poeun-ro are without water from 10:00 to 12:00.", "포은로 일대 빌라가 10:00~12:00 단수된다.") },
                new NewsItem { title = L("Mangwon market by night", "망원시장 야시장"), text = L("Open until 23:00 on Saturday, with street food and live music.", "토요일 밤 11시까지, 길거리 음식과 공연.") },
            };

            // ---- where the monitor really is
            var monitor = phone.parcels.Find(p => p.tracking == Tracking);
            if (scam)
            {
                monitor.status = L("Customs cleared · out for delivery Fri 9 Oct", "통관 완료 · 10월 9일(금) 배송 예정");
                monitor.note = L("Import taxes were paid by the seller at checkout. Nothing to pay on delivery.", "수입 세금은 판매자가 결제 시 납부했습니다. 받으실 때 낼 금액은 없습니다.");
                monitor.window = L("Fri 9 Oct, 14:00-18:00", "10월 9일(금) 14:00~18:00");
                AddSms(phone, Courier, 8, "10:02",
                       L("[Hangang Express] Your overseas parcel HX-5520-1183-KR has cleared customs (taxes paid by the seller). Delivery: Fri 9 Oct.",
                         "[한강익스프레스] 해외 택배 HX-5520-1183-KR 통관 완료(세금은 판매자 납부). 배송 예정일: 10월 9일(금)"));
            }
            else
            {
                monitor.status = L("Held at Incheon customs · duty due", "인천세관 보류 · 관세 미납");
                monitor.note = L("Duty and VAT: ₩56,600. The seller didn't collect import taxes. Pay by 17:00 today to our virtual account (HANGANG EXPRESS (CUSTOMS)), or the parcel goes back to the seller.",
                                 "관세·부가세: 56,600원. 판매자가 수입 세금을 받지 않았습니다. 오늘 17:00까지 가상계좌(한강익스프레스(관세))로 납부하지 않으면 판매자에게 반송됩니다.");
                monitor.account = Day3LegitAccount;
                AddSms(phone, Courier, 8, "10:02",
                       L("[Hangang Express] Your overseas parcel HX-5520-1183-KR is held at customs: duty and VAT ₩56,600 due. Details in the Parcels app. Our customs desk may call you from 1588-5520.",
                         "[한강익스프레스] 해외 택배 HX-5520-1183-KR이 세관에 보류되었습니다: 관세·부가세 56,600원. 자세한 내용은 택배 앱에서 확인하세요. 통관팀이 1588-5520으로 연락드릴 수 있습니다."));
            }
            var shipped = new MailItem
            {
                from = L("AliStar", "알리스타"), fromAddress = "orders@alistar.com", subject = ShippedSubject, when = Oct(7, "21:05"),
                body = scam
                    ? L("Your 27-inch IPS monitor is on its way with Hangang Express (HX-5520-1183-KR).\n\nImport taxes: included. We collected duty and VAT at checkout, so there's nothing more to pay on arrival.",
                        "27인치 IPS 모니터가 한강익스프레스(HX-5520-1183-KR)로 발송되었습니다.\n\n수입 세금: 포함. 결제 시 관세와 부가세를 함께 받았으므로 도착 후 추가로 낼 금액은 없습니다.")
                    : L("Your 27-inch IPS monitor is on its way with Hangang Express (HX-5520-1183-KR).\n\nImport taxes: not included. Orders over US$150 may be charged duty and VAT on arrival in Korea, collected by the courier.",
                        "27인치 IPS 모니터가 한강익스프레스(HX-5520-1183-KR)로 발송되었습니다.\n\n수입 세금: 미포함. 미화 150달러가 넘는 주문은 한국 도착 시 택배사를 통해 관세와 부가세가 부과될 수 있습니다."),
            };
            phone.mails.Insert(0, shipped);

            // ---- the courier's site, and who owns the numbers and accounts
            directory.pages.Add(new WebPage
            {
                url = "hangangexpress.co.kr/customs", title = L("Hangang Express: customs and duty", "한강익스프레스: 통관 및 관세 안내"), site = Courier, official = true,
                aliases = new List<string> { "hangang express", "hangang", "한강익스프레스", "한강", "courier", "택배", "customs", "통관", "관세", "duty", "parcel", "tracking", "track", "운송장" },
                blocks = new List<WebBlock>
                {
                    new WebBlock { kind = WebBlockKind.Contact, text = L("Customer centre and customs desk", "고객센터 · 통관팀"), value = Day3LegitNumber },
                    new WebBlock { kind = WebBlockKind.Notice, text = L("If duty is due on an overseas parcel, we text you and it shows in the Parcels app. Our customs desk may call you from 1588-5520.",
                                                                         "해외 택배에 관세가 부과되면 문자로 안내하고 택배 앱에도 표시됩니다. 통관팀은 1588-5520으로 연락드릴 수 있습니다.") },
                    new WebBlock { kind = WebBlockKind.Warning, text = L("Pay duty only in the Parcels app or to a virtual account in the name HANGANG EXPRESS (CUSTOMS). We never ask you to pay any other account or through a link from another number.",
                                                                          "관세는 택배 앱이나 '한강익스프레스(관세)' 명의의 가상계좌로만 납부하세요. 다른 계좌나 다른 번호에서 온 링크로 납부를 요구하는 일은 절대 없습니다.") },
                    new WebBlock { kind = WebBlockKind.Lookup, text = L("Track a parcel", "운송장 조회"), value = "parcel" },
                },
                lookup = new List<LookupEntry>
                {
                    new LookupEntry
                    {
                        key = Tracking,
                        result = scam
                            ? L("HX-5520-1183-KR · 27-inch monitor · Customs cleared 7 Oct, import taxes paid by the seller · Out for delivery Fri 9 Oct",
                                "HX-5520-1183-KR · 27인치 모니터 · 10월 7일 통관 완료, 수입 세금 판매자 납부 · 10월 9일(금) 배송 예정")
                            : L("HX-5520-1183-KR · 27-inch monitor · Held at Incheon customs since 8 Oct · Duty and VAT ₩56,600 due · Pay to Nuri Bank 5620-44-018830 (HANGANG EXPRESS (CUSTOMS))",
                                "HX-5520-1183-KR · 27인치 모니터 · 10월 8일부터 인천세관 보류 · 관세·부가세 56,600원 미납 · 누리은행 5620-44-018830 (한강익스프레스(관세))로 납부"),
                    },
                    new LookupEntry { key = "4410 2287 1934", result = L("4410 2287 1934 · Desk lamp bulbs · Delivered Wed 7 Oct, 17:32", "4410 2287 1934 · 스탠드 전구 · 10월 7일(수) 17:32 배송 완료") },
                },
            });
            if (scam)
            {
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = L("KR CUSTOMS CLEARANCE", "(주)케이알통관") });
                directory.numbers.Add(new DirNumber { number = Day3ScamNumber, owner = L("Unknown", "알 수 없음") });
                directory.pages.Add(new WebPage
                {
                    url = "hangang-customs.kr/pay", title = L("Hangang Express · customs payment", "한강익스프레스 · 관세 납부"), site = "hangang-customs.kr", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Parcel HX-5520-1183-KR: duty and VAT ₩56,600 unpaid. Pay by card before 17:00 or the parcel is returned.",
                                                                                "택배 HX-5520-1183-KR: 관세·부가세 56,600원 미납. 17:00 전까지 카드로 납부하지 않으면 반송됩니다.") },
                        new WebBlock { kind = WebBlockKind.Form, text = L("Card number, expiry date and CVC", "카드번호, 유효기간, CVC"), value = L("Pay ₩56,600", "56,600원 결제") },
                    },
                });
            }
            else
            {
                directory.accounts.Add(new DirAccount { bank = bank, number = account, holder = L("HANGANG EXPRESS (CUSTOMS)", "한강익스프레스(관세)") });
            }

            var v = StoreVariant(scam ? "scam" : "legit", Day3Dir, scam ? "Day3_Scam" : "Day3_Legit", Day3Call(scam), phone, room, directory);
            var officialNumber = new[]
            {
                new ClueWhen(ClueEvent.PageOpened, "hangangexpress.co.kr/customs"), new ClueWhen(ClueEvent.DocumentViewed, SlipTitle),
                new ClueWhen(ClueEvent.NumberChecked, scam ? Day3ScamNumber : Day3LegitNumber),
            };
            string parcelPlace = L("Parcels → 27-inch monitor", "택배 → 27인치 모니터");
            string mailPlace = L("Mail → \"Your order has shipped\"", "메일 → '주문하신 상품이 발송되었습니다'");
            string numberPlace = L("Hangang Express's site, or the delivery slip in the drawer", "한강익스프레스 홈페이지, 또는 서랍 속 택배 송장");
            string bankCheck = L("Bank app → Transfer → the recipient check", "은행 앱 → 이체 → 받는 분 확인");
            v.clues = scam
                ? new List<ClueDef>
                {
                    Clue("parcel_cleared", L("The Parcels app shows the monitor already cleared customs, its taxes paid.", "택배 앱에는 모니터가 세금이 납부된 채 이미 통관을 마쳤다고 나온다."), parcelPlace,
                         new ClueWhen(ClueEvent.ParcelOpened, Tracking), new ClueWhen(ClueEvent.PageOpened, "hangangexpress.co.kr/customs")),
                    Clue("taxes_included", L("AliStar's shipping email says the import taxes were included.", "알리스타 발송 메일에는 수입 세금이 포함되어 있다고 적혀 있다."), mailPlace,
                         new ClueWhen(ClueEvent.MailRead, ShippedSubject)),
                    Clue("number_off", L("Hangang Express's number is 1588-5520. The caller's 1588-5502 is one digit off.", "한강익스프레스 번호는 1588-5520이다. 발신 번호 1588-5502는 한 자리가 다르다."), numberPlace,
                         officialNumber),
                    Clue("account_name", L("The account belongs to \"KR CUSTOMS CLEARANCE\", not to Hangang Express.", "계좌 주인은 한강익스프레스가 아니라 '(주)케이알통관'이다."), bankCheck,
                         new ClueWhen(ClueEvent.RecipientShown, account), new ClueWhen(ClueEvent.NumberChecked, account)),
                    Clue("web_text", L("The payment text came from 1588-5502 as a web-sent text, not from Hangang Express.", "납부 문자는 한강익스프레스가 아니라 1588-5502에서 [Web발신]으로 왔다."),
                         L("Messages → 1588-5502", "메시지 → 1588-5502"),
                         new ClueWhen(ClueEvent.MessageRead, Day3ScamNumber)),
                }
                : new List<ClueDef>
                {
                    Clue("parcel_held", L("The Parcels app shows the monitor held at customs, with ₩56,600 due and the same account.", "택배 앱에도 모니터가 세관에 보류되어 있고, 56,600원과 같은 계좌가 나와 있다."), parcelPlace,
                         new ClueWhen(ClueEvent.ParcelOpened, Tracking), new ClueWhen(ClueEvent.PageOpened, "hangangexpress.co.kr/customs")),
                    Clue("taxes_not_included", L("AliStar's shipping email says the import taxes were not included.", "알리스타 발송 메일에는 수입 세금이 포함되지 않았다고 적혀 있다."), mailPlace,
                         new ClueWhen(ClueEvent.MailRead, ShippedSubject)),
                    Clue("official_number", L("1588-5520, the number calling you, is Hangang Express's official number.", "지금 전화한 1588-5520은 한강익스프레스의 공식 번호다."), numberPlace,
                         officialNumber),
                    Clue("account_name", L("The account is a virtual account in the name HANGANG EXPRESS (CUSTOMS).", "계좌는 '한강익스프레스(관세)' 명의의 가상계좌다."), bankCheck,
                         new ClueWhen(ClueEvent.RecipientShown, account), new ClueWhen(ClueEvent.NumberChecked, account)),
                };
            v.papers = scam ? Day3ScamPapers() : Day3LegitPapers();
            v.ruleTitle = scam ? L("Is anything due? Ask the app", "낼 돈이 있나? 앱에 물어보라") : L("Pay duty only to the courier", "관세는 택배사에만");
            v.rule = scam
                ? L("Courier or customs asking for money? Open the courier's own app or website yourself. If nothing is due there, nothing is due.",
                    "택배사나 세관이 돈을 요구하면 택배사 앱이나 홈페이지를 직접 열어 보라. 거기에 낼 돈이 없다면, 낼 돈은 없는 것이다.")
                : L("Real duty shows up in the courier's own app, with an account in the courier's name. When they match the call, paying is safe.",
                    "진짜 관세는 택배사 앱에 택배사 명의 계좌와 함께 나온다. 전화 내용과 일치하면 내도 안전하다.");
            v.ruleSource = PaperSource(9);
            return v;
        }

        // ================================================================ the call

        static ConversationData Day3Call(bool scam)
        {
            string number = scam ? Day3ScamNumber : Day3LegitNumber;
            string account = scam ? Day3ScamAccount : Day3LegitAccount;
            string bank = scam ? HanbitBank : NuriBank;
            string accountSpoken = scam ? L("six two zero, five five seven, three zero one one four four", "육이공, 오오칠, 삼공일일사사")
                                        : L("five six two zero, four four, zero one eight eight three zero", "오육이공, 사사, 공일팔팔삼공");

            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = scam ? L("Day 3 · The held parcel (M2, scam)", "3일차 · 붙잡힌 택배 (M2, 사기)") : L("Day 3 · The held parcel (legit)", "3일차 · 붙잡힌 택배 (진짜)");
            c.channel = Channel.Call;
            c.isScam = scam;
            c.revealAtEnd = false;
            c.caller = new CallerInfo { displayName = L("Yoon Seora", "윤서라"), number = number, inContacts = false, portrait = "pt_hr", voice = VoiceCustoms };
            c.claims = new List<string>
            {
                L("She is Yoon Seora from Hangang Express's customs clearance desk.", "한강익스프레스 통관팀 윤서라라고 한다."),
                L("My AliStar monitor (HX-5520-1183-KR) is held at Incheon customs.", "알리스타에서 산 모니터(HX-5520-1183-KR)가 인천세관에 묶여 있다고 한다."),
                L("Duty and VAT of ₩56,600 haven't been paid.", "관세와 부가세 5만 6,600원이 미납이라고 한다."),
                L($"Pay into {bank} {account} before 17:00, or it goes back to the seller.", $"17시 전까지 {bank} {account}로 납부하지 않으면 판매자에게 반송된다고 한다."),
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, L("How do you answer?", "어떻게 대답할까?"), 40f,
                      L("Yes, that's mine. Is something wrong?", "네, 제 거 맞아요. 무슨 문제 있어요?"), "problem",
                      L("How do you know what I ordered?", "제가 뭘 주문했는지 어떻게 아세요?"), "know",
                      P(0.5f, L("Miss Kim? Are you there?", "김지우 고객님? 들리세요?"))),
                    Caller(L("Hello, is this Kim Jiwoo? This is Yoon Seora from Hangang Express, the customs clearance desk.", "안녕하세요, 김지우 고객님 맞으시죠? 한강익스프레스 통관팀 윤서라입니다.")),
                    Caller(L($"I'm calling about your parcel from AliStar: the 27-inch monitor, tracking number {Tracking}.", $"알리스타에서 주문하신 27인치 모니터 건으로 연락드렸어요. 운송장 번호 {Tracking}입니다."),
                           L($"I'm calling about your parcel from AliStar. The twenty seven inch monitor, tracking number {TrackingSpoken}.", $"알리스타에서 주문하신 이십칠 인치 모니터 건으로 연락드렸어요. 운송장 번호 {TrackingSpoken}입니다."),
                           F(FactKind.Case, Tracking, L("Tracking", "운송장")))),
                Node("problem", "ask",
                    Caller(L("It's held at Incheon customs. The import duty and VAT haven't been paid.", "인천세관에 보류되어 있어요. 관세와 부가세가 납부되지 않았거든요."))),
                Node("know", "ask",
                    Caller(L("It's on the customs declaration, Miss Kim. We see every item that comes through.", "통관 신고서에 다 나와 있어요, 고객님. 들어오는 물품은 저희가 전부 확인하거든요.")),
                    Caller(L("It's held at Incheon customs because the import duty and VAT haven't been paid.", "관세와 부가세가 납부되지 않아서 인천세관에 보류되어 있어요."))),
                Hold("ask",
                    Caller(L("The total is ₩56,600. Once it's paid, the monitor is released and delivered tomorrow.", "총 5만 6,600원이에요. 납부되면 바로 통관되고 내일 배송됩니다."),
                           L("The total is fifty six thousand, six hundred won. Once it's paid, the monitor is released and delivered tomorrow.", "총 오만 육천육백 원이에요. 납부되면 바로 통관되고 내일 배송됩니다."),
                           F(FactKind.Amount, "56,600")),
                    Caller(L($"Please pay it into our customs account: {bank} {account}.", $"관세 계좌로 납부해 주세요. {bank} {account}입니다."),
                           L($"Please pay it into our customs account. {bank}, {accountSpoken}.", $"관세 계좌로 납부해 주세요. {bank}, {accountSpoken}입니다."),
                           F(FactKind.Account, account)),
                    Caller(L("The desk closes at five. After that the parcel goes back to the seller, and you pay the return shipping.", "통관팀은 5시에 마감해요. 그 뒤로는 판매자에게 반송되고, 반송비는 고객님 부담이에요."),
                           L(null, "통관팀은 다섯 시에 마감해요. 그 뒤로는 판매자에게 반송되고, 반송비는 고객님 부담이에요.")),
                    Caller(L("I'll stay on the line while you pay.", "납부하시는 동안 기다릴게요."))),
            };
            c.caseInfo = new CaseInfo
            {
                caseTitle = L("The held parcel", "붙잡힌 택배"),
                claimedIdentity = L("Yoon Seora, Hangang Express's customs desk", "한강익스프레스 통관팀 윤서라"),
                ask = L($"Pay ₩56,600 duty and VAT into {bank} {account}", $"관세·부가세 5만 6,600원을 {bank} {account}로 납부"),
                deadline = "17:00",
                deadlineReason = L("before the customs desk closes", "통관팀 마감 전"),
                objective = L("Is your monitor really held at customs, and is this really Hangang Express? Check before 17:00, then give your verdict on the call: pay the duty, or hang up.",
                              "모니터가 정말 세관에 묶여 있을까? 정말 한강익스프레스일까? 17:00 전까지 확인한 뒤, 통화 화면에서 판정을 내리자: 관세를 낼지, 전화를 끊을지."),
            };
            c.verdict = new VerdictInfo
            {
                goAlong = L("Pay the duty", "관세 내기"),
                refuse = L("Hang up", "전화 끊기"),
                refuseDetail = L("Say no and end the call", "거절하고 통화 종료"),
                goAlongLine = L("Okay, I'm paying it now.", "네, 지금 낼게요."),
                refuseLine = L("I'm not paying anything over the phone. Goodbye.", "전화로는 아무것도 안 낼 거예요. 끊을게요."),
            };
            c.questions = new List<ConvQuestion>
            {
                Q(L("Ask whose name is on the account", "누구 명의 계좌인지 묻기"), L("Whose name is on that account?", "그 계좌는 누구 명의예요?"), null,
                  scam
                      ? Caller(L("KR Customs Clearance. We handle customs for Hangang Express.", "케이알통관이에요. 한강익스프레스 통관 업무를 대행하고 있어요."))
                      : Caller(L("It's a virtual account in the name Hangang Express, customs. It's in your Parcels app too.", "한강익스프레스 관세 명의의 가상계좌예요. 택배 앱에도 똑같이 나와 있어요."))),
                Q(L("Ask why you owe duty", "왜 관세를 내야 하는지 묻기"), L("Why do I have to pay duty?", "왜 관세를 내야 해요?"), null,
                  Caller(L("Orders over a hundred and fifty dollars pay duty and VAT when they arrive, unless the seller collected it.", "150달러가 넘는 주문은 도착할 때 관세와 부가세를 내셔야 해요. 판매자가 미리 받은 경우만 빼고요."),
                         L(null, "백오십 달러가 넘는 주문은 도착할 때 관세와 부가세를 내셔야 해요. 판매자가 미리 받은 경우만 빼고요."))),
                Q(L("Ask if you can pay in the Parcels app", "택배 앱으로 낼 수 있는지 묻기"), L("Can I pay in the Parcels app instead?", "택배 앱으로 내면 안 돼요?"), null,
                  scam
                      ? Caller(L("The app doesn't show customs payments, Miss Kim. It has to be a transfer.", "앱에는 관세 납부가 안 떠요, 고객님. 계좌이체로만 가능해요."))
                      : Caller(L("Of course. The same account is under your parcel in the app. Either way works.", "물론이죠. 앱의 택배 상세에도 같은 계좌가 있어요. 어느 쪽으로 내셔도 돼요."))),
                Q(L("Ask for a number to call back", "다시 걸 번호 묻기"), L("What number can I call you back on?", "제가 다시 걸려면 어느 번호로 하면 돼요?"), null,
                  scam
                      ? Caller(L("This line: 1588-5502. Ask for the customs desk.", "지금 이 번호요. 1588-5502로 거셔서 통관팀 찾으시면 돼요."),
                               L("This line. One five eight eight, five five zero two. Ask for the customs desk.", "지금 이 번호요. 일오팔팔, 오오공이로 거셔서 통관팀 찾으시면 돼요."),
                               F(FactKind.Phone, Day3ScamNumber, L("Call back", "회신 번호")))
                      : Caller(L("Our customer centre, 1588-5520. It's the number I'm calling from. Ask for the customs desk.", "고객센터 1588-5520이요. 지금 거는 번호예요. 통관팀 찾으시면 돼요."),
                               L("Our customer centre, one five eight eight, five five two zero. It's the number I'm calling from. Ask for the customs desk.", "고객센터 일오팔팔, 오오이공이요. 지금 거는 번호예요. 통관팀 찾으시면 돼요."),
                               F(FactKind.Phone, Day3LegitNumber, L("Call back", "회신 번호")))),
            };
            var text = scam
                ? WithSms(Caller(L("I've texted you the payment details, so you have them in writing.", "납부 안내는 문자로도 보내 드렸어요.")), Day3ScamNumber,
                          L("[Web발신] [Hangang Express] Customs duty ₩56,600 unpaid for HX-5520-1183-KR. Pay by 17:00: Hanbit Bank 620-557-301144 or hangang-customs.kr/pay",
                            "[Web발신] [한강익스프레스] HX-5520-1183-KR 관세 56,600원 미납. 17:00까지 납부: 한빛은행 620-557-301144 또는 hangang-customs.kr/pay"),
                          "hangang-customs.kr/pay")
                : WithSms(Caller(L("I've texted you the payment details, so you have them in writing.", "납부 안내는 문자로도 보내 드렸어요.")), Courier,
                          L("[Hangang Express] Customs: duty and VAT ₩56,600 due for HX-5520-1183-KR. Pay by 17:00 in the app or to virtual account Nuri Bank 5620-44-018830 (HANGANG EXPRESS (CUSTOMS)).",
                            "[한강익스프레스] HX-5520-1183-KR 관세·부가세 56,600원 납부 안내. 17:00까지 앱 또는 가상계좌 누리은행 5620-44-018830(한강익스프레스(관세))로 납부해 주세요."));
            c.beats = new List<PressureBeat>
            {
                Beat("16:39", text),
                Beat("16:46", Caller(L("Miss Kim? The customs desk is asking whether the payment is on its way.", "고객님? 통관팀에서 납부 진행 중이신지 묻고 있어요."))),
                Beat("16:52", Caller(L("Eight minutes before the desk closes.", "마감까지 8분 남았어요."), L(null, "마감까지 팔 분 남았어요."))),
                Beat("16:57", Caller(L("Three minutes. After five I can't stop the return.", "3분 남았어요. 다섯 시가 지나면 반송을 막을 수 없어요."), L(null, "삼 분 남았어요. 다섯 시가 지나면 반송을 막을 수 없어요."))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    consequence = scam
                        ? L("You hung up. Your monitor had cleared customs with its taxes paid, and 1588-5502 is one digit off Hangang Express's real number.",
                            "전화를 끊었다. 모니터는 세금이 이미 납부된 채 통관을 마쳤고, 1588-5502는 한강익스프레스 진짜 번호와 한 자리가 달랐다.")
                        : L("The duty wasn't paid by five. The monitor went back to Incheon, and the return shipping comes out of your refund.",
                            "다섯 시까지 관세를 내지 않았다. 모니터는 인천으로 되돌아갔고, 반송비는 환불금에서 빠진다."),
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Miss Kim? Fine. Your parcel will be returned.", "고객님? 알겠습니다. 택배는 반송 처리할게요.")) }
                        : new List<ConvLine> { Caller(L("Miss Kim? The desk has closed. I'll have to mark it for return.", "고객님? 통관팀이 마감됐어요. 반송 처리할 수밖에 없겠네요.")) },
                    consequence = scam
                        ? L("The caller gave up at five. Your monitor arrived on Friday anyway.", "상대는 다섯 시에 포기했다. 모니터는 어차피 금요일에 도착했다.")
                        : L("Five o'clock passed. The monitor went back to Incheon.", "다섯 시가 지났다. 모니터는 인천으로 되돌아갔다."),
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -Duty,
                    lines = scam
                        ? new List<ConvLine> { Caller(L("Thank you. Oh, and there's a storage fee as well. I'll call you back about it.", "감사합니다. 아, 보관료도 따로 있어요. 그건 다시 연락드릴게요.")) }
                        : new List<ConvLine> { Caller(L("Payment received. Your monitor will be delivered tomorrow afternoon. Thank you, Miss Kim.", "납부 확인됐어요. 모니터는 내일 오후에 배송될 거예요. 감사합니다, 고객님.")) },
                    consequence = scam
                        ? L("The ₩56,600 went to \"KR Customs Clearance\", an account opened last week. Your monitor, its taxes paid by the seller, arrived on Friday anyway.",
                            "5만 6,600원은 지난주에 개설된 '케이알통관' 계좌로 들어갔다. 판매자가 세금을 이미 낸 모니터는 어차피 금요일에 도착했다.")
                        : L("The duty went to Hangang Express's customs account. The monitor arrived on Friday.", "관세는 한강익스프레스 관세 계좌로 들어갔다. 모니터는 금요일에 도착했다."),
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = account, bank = bank, amount = Duty, endingId = "go_along" },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }

        // ================================================================ the next morning

        static List<EndPaper> Day3ScamPapers()
        {
            string lead = L("Callers posing as Hangang Express's \"customs clearance desk\" phoned AliStar customers in Mapo yesterday. They read out each customer's order and tracking number and asked for \"unpaid duty\" to be paid into an account before 5 p.m.",
                            "어제 한강익스프레스 '통관팀'을 사칭한 전화가 마포의 알리스타 고객들에게 걸려 왔다. 이들은 고객의 주문 내역과 운송장 번호를 읊으며 '미납 관세'를 오후 5시 전까지 계좌로 내라고 요구했다.");
            string facts = L("The details came from last month's AliStar data leak. The parcels had already cleared customs with their taxes paid, and the calls came from 1588-5502, one digit off Hangang Express's real number, 1588-5520.",
                             "이 정보는 지난달 알리스타 개인정보 유출에서 나온 것이었다. 택배는 이미 세금이 납부된 채 통관을 마쳤고, 전화는 한강익스프레스의 진짜 번호 1588-5520과 한 자리 다른 1588-5502에서 걸려 왔다.");
            string courier = L("\"If duty is due, you'll see it in our app, with an account in our name,\" a Hangang Express spokesperson said. \"We never ask you to pay anyone else.\"",
                               "한강익스프레스 관계자는 \"관세가 있으면 저희 앱에 저희 명의 계좌와 함께 표시된다\"며 \"다른 사람에게 돈을 내라고 하는 일은 절대 없다\"고 말했다.");
            return new List<EndPaper>
            {
                Paper(Outcome.Refuse, L("Fake customs desk used leaked AliStar orders", "가짜 통관팀, 유출된 알리스타 주문 정보 악용"),
                      L("The callers knew the item and the tracking number. The parcels had already cleared customs.", "상대는 상품과 운송장 번호까지 알고 있었다. 택배는 이미 통관을 마친 상태였다."),
                      lead + "\n\n" + facts + "\n\n" + courier,
                      L("You hung up. The parcel had cleared customs, the taxes were included, and the number was one digit off.", "전화를 끊었다. 택배는 통관을 마쳤고, 세금은 포함되어 있었으며, 번호는 한 자리가 달랐다.")),
                Paper(Outcome.GoAlong, L("56,600 won of \"duty\" on a parcel that was already paid for", "이미 세금 낸 택배에 '관세' 5만 6,600원"),
                      L("Scammers with leaked AliStar orders posed as a courier's customs desk.", "유출된 알리스타 주문 정보를 손에 넣은 사기범들이 택배사 통관팀을 사칭했다."),
                      lead + "\n\n" + L("A Mangwon student paid 56,600 won into an account in the name \"KR Customs Clearance\", opened a week ago. Her monitor had cleared customs on Wednesday, its taxes paid by the seller at checkout. It arrived this morning.",
                                        "망원동의 한 대학생은 일주일 전 개설된 '케이알통관' 명의 계좌로 5만 6,600원을 보냈다. 그의 모니터는 판매자가 결제 때 세금을 낸 상태로 수요일에 이미 통관을 마쳤고, 오늘 아침 도착했다.") + "\n\n" + courier,
                      L("The Parcels app and the shipping email both said the taxes were paid, and the account wasn't in Hangang Express's name.", "택배 앱과 발송 메일 모두 세금이 납부됐다고 했고, 계좌도 한강익스프레스 명의가 아니었다.")),
                Paper(Outcome.Timeout, L("Customs desk calling? Check the app", "통관팀 전화? 앱부터 확인하라"),
                      L("Scammers with leaked orders called AliStar customers yesterday.", "유출된 주문 정보로 사기범들이 어제 알리스타 고객들에게 전화를 걸었다."),
                      lead + "\n\n" + facts + "\n\n" + courier,
                      L("Nothing was sent. The Parcels app would have told you the monitor had already cleared customs.", "돈은 보내지 않았다. 택배 앱만 봤어도 모니터가 이미 통관된 걸 알 수 있었다.")),
            };
        }

        static List<EndPaper> Day3LegitPapers()
        {
            string lead = L("With cheap overseas shopping booming, Hangang Express says it now holds hundreds of parcels a day at Incheon for unpaid duty and VAT, usually because the seller didn't collect the taxes at checkout.",
                            "해외직구가 늘면서 한강익스프레스는 관세·부가세 미납으로 인천에 보류되는 택배가 하루 수백 건에 이른다고 밝혔다. 대부분 판매자가 결제 때 세금을 받지 않은 경우다.");
            string desk = L("\"When duty is due, we text you, show it in the app and call from 1588-5520,\" said Yoon Seora of the courier's customs desk. \"The account is always a virtual account in our name.\"",
                            "한강익스프레스 통관팀 윤서라 씨는 \"관세가 있으면 문자로 알리고, 앱에 표시하고, 1588-5520으로 전화한다\"며 \"계좌는 언제나 저희 명의의 가상계좌\"라고 말했다.");
            string rule = L("Personal orders up to US$150 are duty-free. Above that, duty and VAT are due on arrival unless the seller collected them.",
                            "개인 해외직구는 미화 150달러까지 면세다. 그 이상이면 판매자가 미리 받지 않은 한 도착 시 관세와 부가세를 내야 한다.");
            return new List<EndPaper>
            {
                Paper(Outcome.GoAlong, L("Shopping abroad: when the duty is real", "해외직구, 진짜 관세일 때"),
                      L("Couriers say more overseas orders are held for duty, and that customers are right to check before paying.", "택배사들은 관세 보류 택배가 늘고 있다며, 내기 전에 확인하는 것이 옳다고 말한다."),
                      lead + "\n\n" + desk + L(" \"Customers who check that first are doing exactly the right thing.\"", " 그는 \"이걸 먼저 확인하는 고객이야말로 제대로 하는 것\"이라고 덧붙였다.") + "\n\n" + rule,
                      L("The duty was real, and you checked: the app, the email, the official number and the account name all matched.", "관세는 진짜였고, 당신은 확인했다. 앱, 메일, 공식 번호, 예금주 이름이 모두 일치했다.")),
                Paper(Outcome.Refuse, L("Real duty notices ignored after a wave of scams", "사기 기승에 진짜 관세 안내도 외면"),
                      L("Parcels pile up at Incheon as customers hang up on real customs calls.", "고객들이 진짜 통관 전화마저 끊으면서 인천에 택배가 쌓이고 있다."),
                      lead + L(" After weeks of fake customs texts, many customers now hang up on the real thing, and dozens of parcels were sent back yesterday.", " 몇 주째 가짜 관세 문자가 이어지자 진짜 안내 전화까지 끊는 고객이 많아졌고, 어제만 수십 건의 택배가 반송됐다.") +
                      "\n\n" + desk + L(" \"Check it, by all means. But check the app, not just your nerves.\"", " 그는 \"확인은 꼭 하시라. 다만 불안한 마음이 아니라 앱으로 확인하시라\"고 말했다.") + "\n\n" + rule,
                      L("This one was real: the Parcels app showed the same duty and account, and the call came from Hangang Express's official number.", "이번엔 진짜였다. 택배 앱에 같은 관세와 계좌가 나와 있었고, 전화도 한강익스프레스 공식 번호였다.")),
                Paper(Outcome.Timeout, L("Parcels go back as duty goes unpaid", "관세 미납으로 반송되는 택배들"),
                      L("Customers who never decided lost their orders to the return queue.", "결정을 미룬 고객들의 주문품이 반송 대기열로 넘어갔다."),
                      lead + "\n\n" + desk + "\n\n" + rule,
                      L("The duty was real, and the evidence was in the app and the email. Deciding in time matters too.", "관세는 진짜였고, 증거는 앱과 메일에 있었다. 제때 결정하는 것도 중요하다.")),
            };
        }

        // ================================================================ what Day 2 left behind

        static List<DayEcho> Day3Echoes()
        {
            SetDay(3, "16:25");
            string warning = L("Everyone: someone is calling tenants pretending to be my son. I am NOT in hospital, and the rent account has NOT changed: Nuri Bank 110-771-209944, as always. – Choi, 1F",
                               "여러분, 누가 제 아들인 척 세입자들에게 전화하고 있어요. 저는 입원하지 않았고, 월세 계좌도 바뀌지 않았습니다. 늘 그렇듯 누리은행 110-771-209944예요. - 1층 최");

            DayEcho RentPaid(Truth truth, OutcomeMask outcomes, string to, string when, string memo)
            {
                var e = Echo(2, truth, outcomes, EchoKind.BankTransaction);
                e.from = to;
                e.when = when;
                e.title = memo;
                e.amount = -Rent;
                return e;
            }

            var hyunwoo = Echo(2, Truth.Legit, Any, EchoKind.Contact);
            hyunwoo.from = Day2LegitNumber;
            hyunwoo.sender = L("Hyunwoo (landlord's son)", "현우 씨 (집주인 아들)");
            hyunwoo.avatar = "pt_hyunwoo";
            hyunwoo.text = L("Looks after the building while Mr. Choi recovers", "최 사장님 회복 동안 건물 관리");

            string landlordPaid = L("CHOI YOUNGSIK", "최영식"), october = L("October rent", "10월 월세");
            return new List<DayEcho>
            {
                // The "son" was a scammer: the landlord warns everyone; the real rent still has to be paid.
                EchoCard(2, Truth.Scam, Sent, L("Mr. Choi never asked for a new account. You paid him the real rent this morning, again.", "최 사장님은 새 계좌를 요청한 적이 없었다. 오늘 아침 진짜 월세를 다시 보냈다.")),
                EchoCard(2, Truth.Scam, HungUp, L("The \"landlord's son\" was a fake. Mr. Choi thanked the tenants who hung up.", "'집주인 아들'은 가짜였다. 최 사장님은 전화를 끊은 세입자들에게 고마워했다.")),
                EchoCard(2, Truth.Scam, TooLate, L("The \"landlord's son\" was a fake. Mr. Choi warned everyone in the chat last night.", "'집주인 아들'은 가짜였다. 최 사장님이 어젯밤 단톡방에서 모두에게 경고했다.")),
                EchoChat(2, Truth.Scam, Any, "villa", Landlord, "pt_landlord", Oct(7, "19:02"), warning, true),
                EchoChat(2, Truth.Scam, HungUp, "villa", Landlord, "pt_landlord", Oct(7, "19:05"), L("302 hung up on him. Well done!", "302호는 바로 끊었다네요. 잘했어요!")),
                EchoSms(2, Truth.Scam, Sent, "010-5512-3380", Oct(7, "19:10"),
                        L("Jiwoo, I heard what happened. I'm so sorry. Please report it to the police. Take until the end of the month for the rent.",
                          "지우 학생, 얘기 들었어요. 정말 안타깝네요. 경찰에 꼭 신고하고, 월세는 월말까지 천천히 줘도 돼요.")),
                RentPaid(Truth.Scam, Sent, landlordPaid, Oct(8, "09:20"), october),
                RentPaid(Truth.Scam, Kept, landlordPaid, Oct(7, "19:30"), october),
                EchoNote(2, Truth.Scam, Sent, StickyTitle, L("Rent account changes ONLY in writing. Check the lease!", "월세 계좌는 서면으로만 바뀐다. 계약서 확인!"), "board_sticky_y", new Vector2(0.41f, 0.77f), 5f),
                EchoNote(2, Truth.Scam, HungUp, StickyTitle, L("Fake \"son\": 010-4127-8830. Hung up!", "가짜 '아들': 010-4127-8830. 끊음!"), "board_sticky_p", new Vector2(0.41f, 0.77f), -4f),
                EchoNote(2, Truth.Scam, TooLate, StickyTitle, L("Fake \"son\": 010-4127-8830. Next time, decide sooner.", "가짜 '아들': 010-4127-8830. 다음엔 빨리 결정!"), "board_sticky_p", new Vector2(0.41f, 0.77f), -4f),

                // The son was real: he thanks, or asks again.
                EchoCard(2, Truth.Legit, Sent, L("The rent reached Hyunwoo. Mr. Choi is recovering in hospital.", "월세는 현우 씨에게 잘 들어갔다. 최 사장님은 병원에서 회복 중이다.")),
                EchoCard(2, Truth.Legit, Kept, L("The \"landlord's son\" was real. You sent him the rent this morning, a day late.", "'집주인 아들'은 진짜였다. 오늘 아침 하루 늦게 월세를 보냈다.")),
                hyunwoo,
                EchoSms(2, Truth.Legit, Sent, Day2LegitNumber, Oct(7, "18:02"), L("Thanks for the rent, Jiwoo-ssi! Dad says hi from the ward :) – Hyunwoo", "월세 잘 받았어요, 지우 씨! 아버지가 병실에서 안부 전하세요^^ - 현우"), true),
                EchoSms(2, Truth.Legit, Kept, Day2LegitNumber, Oct(7, "17:20"),
                        L("Hi Jiwoo-ssi, it's Hyunwoo from the first floor. I understand you were careful. Dad's note is on your board and in the chat. Please send the rent when you can.",
                          "지우 씨, 1층 현우예요. 조심하신 거 이해해요. 아버지 쪽지랑 단톡방 글 확인해 보시고, 편하실 때 월세 보내 주세요."), true),
                RentPaid(Truth.Legit, Kept, L("CHOI HYUNWOO", "최현우"), Oct(8, "09:20"), L("October rent (late)", "10월 월세 (지연)")),
                EchoChat(2, Truth.Legit, Any, "villa", Landlord, "pt_landlord", Oct(8, "10:15"), L("Thank you all for the get-well messages. The hospital food is terrible, haha", "쾌유 메시지 다들 고마워요. 병원 밥은 정말 맛없네요 ㅎㅎ")),
                EchoNote(2, Truth.Legit, Any, StickyTitle, L("Mr. Choi in hospital till next week. Rent goes to Hyunwoo (it's real!)", "최 사장님 다음 주까지 입원. 월세는 현우 씨에게 (진짜!)"), "board_sticky_y", new Vector2(0.41f, 0.77f), 3f),
            }.Concat(Day1Notes()).ToList();
        }
    }
}
