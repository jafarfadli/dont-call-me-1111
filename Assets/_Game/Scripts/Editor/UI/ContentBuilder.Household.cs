using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Jiwoo's life as one timeline, shown as it stands when the day being built starts: contacts,
    /// calls, texts, chats, the bank, parcels and mail on the phone; the paper, drawer, board and
    /// wallet in the room; and the world the apps look things up in. Nothing here depends on how an
    /// earlier day went (that is what echoes are for) or on today's truth (the variants add that).
    /// Every text is written in English and Korean (<see cref="L"/>).
    /// </summary>
    public static partial class ContentBuilder
    {
        const long EverydayAtDay1 = 1284300;
        const string Tracking = "HX-5520-1183-KR";
        static string TrackingSpoken => L("H X, five five two zero, one one eight three, K R", "에이치 엑스, 오오이공, 일일팔삼, 케이 알");

        // Names that clues and other days look things up by: keep them in one place.
        static string NuriBank => L("Nuri Bank", "누리은행");
        static string HanbitBank => L("Hanbit Bank", "한빛은행");
        static string Courier => L("Hangang Express", "한강익스프레스");
        static string LeaseTitle => L("Lease contract · Unit 302", "임대차 계약서 · 302호");
        static string SlipTitle => L("Delivery slip", "택배 송장");
        static string NumbersTitle => L("Important numbers", "중요한 번호");
        static string CardTitle => L("Nuri Bank check card", "누리은행 체크카드");
        static string StickyTitle => L("Sticky note", "메모");
        static string VillaChat => L("Mangwon Heights residents", "망원하이츠 입주민");
        static string Office => L("Office", "관리사무소");
        static string Landlord => L("Landlord Choi", "집주인 최 사장님");

        // Timestamps for sorting the phone's lists (newest first) once they are built.
        static readonly Dictionary<object, DateTime> stamps = new Dictionary<object, DateTime>();

        static DateTime Stamp(int month, int dom, string time)
        {
            var at = new DateTime(2026, month, dom);
            if (!string.IsNullOrEmpty(time) && TimeSpan.TryParse(time, out var t))
                at += t;
            return at;
        }

        static void NewestFirst<T>(List<T> list) =>
            list.Sort((a, b) => (stamps.TryGetValue(b, out var tb) ? tb : DateTime.MinValue).CompareTo(stamps.TryGetValue(a, out var ta) ? ta : DateTime.MinValue));

        // ================================================================ phone

        static PhoneContent HouseholdPhone()
        {
            stamps.Clear();
            var p = ScriptableObject.CreateInstance<PhoneContent>();
            p.ownerName = L("Kim Jiwoo", "김지우");
            p.ownerNumber = "010-6620-0921";
            p.dateLabel = DateLabel(today);
            p.startTime = startTime;

            p.contacts = new List<Contact>
            {
                new Contact { name = L("Mom", "엄마"), number = "010-2231-7745", memo = L("Park Hyejin", "박혜진"), portrait = "pt_mom" },
                new Contact { name = L("Dad", "아빠"), number = "010-4418-0902", memo = L("Kim Dongsu", "김동수"), portrait = "pt_dad" },
                new Contact { name = L("Minjun", "민준이"), number = "010-3321-5580", memo = L("Little brother · Daejeon", "남동생 · 대전"), portrait = "pt_minjun" },
                new Contact { name = L("Yuna", "유나"), number = "010-7780-1123", memo = L("Concert on 7 Nov!", "11월 7일 콘서트!"), portrait = "pt_yuna" },
                new Contact { name = Landlord, number = "010-5512-3380", memo = L("1F · rent due on the 7th", "1층 · 월세 매달 7일"), portrait = "pt_landlord" },
                new Contact { name = L("Taeho (Minjun's friend)", "태호 (민준이 친구)"), number = "010-9043-2217", memo = L("Met at Chuseok", "추석 때 만남"), portrait = "pt_taeho" },
                new Contact { name = L("Manager Han (cafe)", "한 매니저 (카페)"), number = "010-5530-8812", memo = L("Mangwon Roasters", "망원로스터스"), portrait = "pt_cafe" },
                new Contact { name = NuriBank, number = "1599-0000", memo = L("Customer centre, from the back of my card", "고객센터, 카드 뒷면 번호"), portrait = "app_bank" },
                new Contact { name = L("Mangwon Heights office", "망원하이츠 관리사무소"), number = "02-555-0192", memo = L("Building management", "건물 관리"), portrait = "app_contacts" },
            };

            // ---- calls
            void Call(int month, int dom, string time, string number, CallKind kind, string duration = "")
            {
                if (!Seen(month, dom, time))
                    return;
                var r = new CallRecord { number = number, kind = kind, when = At(month, dom, time), duration = duration };
                stamps[r] = Stamp(month, dom, time);
                p.recents.Add(r);
            }
            Call(10, 1, "10:15", "02-555-0181", CallKind.Incoming, "0:52");
            Call(10, 2, "14:12", "070-4852-1170", CallKind.Missed);
            Call(10, 4, "19:20", "010-7780-1123", CallKind.Outgoing, "8:31");
            Call(10, 5, "11:02", "010-5530-8812", CallKind.Incoming, "1:48");
            Call(10, 5, "21:40", "010-2231-7745", CallKind.Outgoing, "12:04");
            Call(10, 7, "12:10", "010-7780-1123", CallKind.Incoming, "2:05");
            Call(10, 7, "20:45", "010-3321-5580", CallKind.Incoming, "4:12");
            NewestFirst(p.recents);

            // ---- texts
            var threads = new Dictionary<string, SmsThread>();
            void Sms(string sender, int month, int dom, string time, string text, string link = null)
            {
                if (!Seen(month, dom, time))
                    return;
                if (!threads.TryGetValue(sender, out var t))
                {
                    t = new SmsThread { sender = sender };
                    threads[sender] = t;
                    p.sms.Add(t);
                }
                t.messages.Add(new SmsMessage { when = At(month, dom, time), text = text, link = link });
                stamps[t] = Stamp(month, dom, time);
            }
            Sms(L("Hangul Telecom", "한글텔레콤"), 10, 1, "09:00",
                L("[Hangul Telecom] Your September bill is ₩43,200, due 20 Oct.", "[한글텔레콤] 9월 이용요금은 43,200원입니다. 10월 20일까지 납부해 주세요."));
            Sms("010-5903-2271", 9, 30, "18:22",
                L("[Web발신] [Landlord notice] Your rent account has changed. Check the new account before paying: rent-notice.kr",
                  "[Web발신] [임대인 안내] 월세 입금 계좌가 변경되었습니다. 입금 전 새 계좌를 확인하세요: rent-notice.kr"), "rent-notice.kr");
            Sms("010-8812-4471", 10, 4, "23:12",
                L("[Web발신] [Nuri] Your account is restricted for security reasons. Verify within 24 hours:",
                  "[Web발신] [누리] 고객님의 계좌가 보안상의 이유로 제한되었습니다. 24시간 안에 인증하세요:"), "nuri-secure.kr/verify");
            Sms("1599-0000", 10, 5, "22:47",
                L("[Web발신] [Nuri Bank] New device registered: MacBook Air (Seoul). If this wasn't you, call 1599-0000.",
                  "[Web발신] [누리은행] 새 기기 등록: MacBook Air (서울). 본인이 아니면 1599-0000으로 연락 주세요."));
            Sms("1599-0000", 10, 6, "09:03",
                L("[Web발신] [Nuri Bank] Card payment ₩4,500 · Mangwon Roasters · approved.", "[Web발신] [누리은행] 체크카드 승인 4,500원 · 망원로스터스"));
            Sms(Courier, 10, 6, "08:30",
                L("[Hangang Express] Your parcel (desk lamp bulbs) arrives tomorrow 16:00-18:00. Driver Kim Taesik 010-7715-5521.",
                  "[한강익스프레스] 고객님의 택배(스탠드 전구)가 내일 16:00~18:00에 도착합니다. 배송기사 김태식 010-7715-5521"));
            Sms("010-7240-1182", 10, 6, "11:20",
                L("[Web발신] [International delivery] Customs duty unpaid for your parcel. Pay now or it will be returned: kr-customs.help/pay",
                  "[Web발신] [국제배송] 고객님 택배의 관세가 미납되었습니다. 지금 납부하지 않으면 반송됩니다: kr-customs.help/pay"), "kr-customs.help/pay");
            Sms("1599-0000", 10, 7, "12:40",
                L("[Web발신] [Nuri Bank] Card payment ₩8,900 · Hanbit Univ. cafeteria · approved.", "[Web발신] [누리은행] 체크카드 승인 8,900원 · 한빛대 학생식당"));
            Sms(Courier, 10, 7, "17:32",
                L("[Hangang Express] Delivered: desk lamp bulbs, left at the door of 302. Photo in the app.",
                  "[한강익스프레스] 배송 완료: 스탠드 전구, 302호 문 앞. 사진은 앱에서 확인하세요."));
            Sms("1599-0000", 10, 8, "09:10",
                L("[Web발신] [Nuri Bank] Card payment ₩4,500 · Mangwon Roasters · approved.", "[Web발신] [누리은행] 체크카드 승인 4,500원 · 망원로스터스"));
            NewestFirst(p.sms);

            // ---- chats
            ChatThread Thread(string id, string title, string avatar, bool group) => new ChatThread { id = id, title = title, avatar = avatar, group = group };
            void Chat(ChatThread t, int dom, string time, string sender, string avatar, string text, string photo = null)
            {
                if (!Seen(10, dom, time))
                    return;
                t.messages.Add(new ChatMessage { sender = sender, avatar = avatar, when = Oct(dom, time), text = text, photoCaption = photo });
                stamps[t] = Stamp(10, dom, time);
            }
            void Mine(ChatThread t, int dom, string time, string text)
            {
                if (!Seen(10, dom, time))
                    return;
                t.messages.Add(new ChatMessage { outgoing = true, when = Oct(dom, time), text = text });
                stamps[t] = Stamp(10, dom, time);
            }

            string mom = L("Mom", "엄마"), dad = L("Dad", "아빠"), minjun = L("Minjun", "민준이");
            var family = Thread("family", L("Our family", "우리 가족"), "photo_family", true);
            Chat(family, 5, "19:10", mom, "pt_mom", L("Jiwoo-ya, did you eat dinner?", "지우야, 저녁은 먹었니?"));
            Mine(family, 5, "19:14", L("Yes! Kimchi stew after my shift", "응! 알바 끝나고 김치찌개 먹었어"));
            Chat(family, 5, "20:02", dad, "pt_dad", null, L("Grandma's persimmons in Jeonju", "전주 할머니 댁 감"));
            Chat(family, 5, "22:30", minjun, "pt_minjun", L("I'm coming up to Seoul on Saturday for a concert. Noona, can I sleep over?", "누나 나 토요일에 콘서트 보러 서울 가는데 자고 가도 돼?"));
            Mine(family, 5, "22:41", L("Only if you bring snacks", "간식 사 오면 ㅇㅋ"));
            Chat(family, 6, "16:12", mom, "pt_mom", null, L("Doenjang stew on the stove", "보글보글 된장찌개"));
            Chat(family, 6, "16:12", mom, "pt_mom", L("Your dad cooked today! Come home for Mom's birthday on the 17th, okay?", "오늘은 아빠가 요리했어! 17일 엄마 생일에는 집에 올 거지?"));
            Chat(family, 7, "08:05", mom, "pt_mom", L("Good morning! Rent day today, don't forget :)", "좋은 아침! 오늘 월세 내는 날이야, 잊지 마^^"));
            Mine(family, 7, "08:20", L("I know, Mom...", "알아 엄마~"));
            Chat(family, 7, "21:15", minjun, "pt_minjun", L("Midterms start Monday. Send help (and snacks)", "월요일부터 중간고사… 살려줘 (간식도)"));
            Chat(family, 8, "12:02", dad, "pt_dad", null, L("Grandma's kimchi, delivered", "할머니 김치 도착"));
            Chat(family, 8, "12:03", dad, "pt_dad", L("Grandma made extra for you. Come and get it on the 17th.", "할머니가 네 것도 따로 담그셨다. 17일에 와서 가져가라."));

            var villa = Thread("villa", VillaChat, "app_contacts", true);
            Chat(villa, 5, "09:00", Office, "app_contacts",
                 L("Reminder: gas safety inspection on Wed 14 Oct, 10:00-17:00. Inspectors never ask for money.",
                   "안내: 10월 14일(수) 10:00~17:00 가스 안전점검이 있습니다. 점검원은 절대 돈을 요구하지 않습니다."));
            Chat(villa, 5, "10:31", L("201", "201호"), "pt_unknown",
                 L("Did anyone else get a call about a 'protected account'? Hung up on them.", "혹시 '보호계좌' 어쩌고 하는 전화 받으신 분 있나요? 바로 끊었어요."));
            Chat(villa, 6, "14:05", Office, "app_contacts",
                 L("Water tank cleaning on Thursday 10:00-12:00. Please keep some water aside.", "목요일 10:00~12:00 물탱크 청소가 있습니다. 미리 물을 받아 두세요."));
            Chat(villa, 8, "12:15", Office, "app_contacts", L("The water is back on. Thank you for your patience!", "단수가 끝났습니다. 협조해 주셔서 감사합니다!"));

            string yunaName = L("Yuna", "유나");
            var yuna = Thread("yuna", yunaName, "pt_yuna", false);
            Chat(yuna, 4, "18:55", yunaName, "pt_yuna", L("Tickets done!! Standing A-128 and A-129, 7 Nov", "티켓팅 성공!! 11월 7일 스탠딩 A-128, A-129"));
            Mine(yuna, 4, "19:01", L("I owe you 55,000 won, sending it on payday", "5만 5천 원 월급날 보낼게"));
            Chat(yuna, 4, "19:02", yunaName, "pt_yuna", L("No rush :)", "천천히 줘 :)"));
            Chat(yuna, 7, "12:14", yunaName, "pt_yuna", L("Tteokbokki after class tomorrow?", "내일 수업 끝나고 떡볶이 ㄱ?"));
            Mine(yuna, 7, "12:20", L("Yes!! 5pm?", "콜!! 5시?"));

            string han = L("Manager Han", "한 매니저");
            var cafe = Thread("cafe", L("Mangwon Roasters crew", "망원로스터스 크루"), "pt_cafe", true);
            Chat(cafe, 5, "21:00", han, "pt_cafe", L("Wages go out on the 10th as usual. Can someone cover Saturday morning?", "급여는 평소처럼 10일에 나가요. 토요일 오전 대타 가능한 분?"));
            Chat(cafe, 8, "10:30", han, "pt_cafe", L("Sunday opening shift still free, please! Double stamps for whoever takes it.", "일요일 오픈 근무 아직 비어 있어요ㅠㅠ 하시는 분 스탬프 두 배!"));

            p.chats = new List<ChatThread> { family, villa, yuna, cafe };
            NewestFirst(p.chats);

            // ---- bank
            long everyday = EverydayAtDay1;
            p.bank = new BankData
            {
                bankName = NuriBank,
                banks = new List<string> { NuriBank, HanbitBank, L("Daehan Bank", "대한은행"), L("Mirae Savings", "미래저축은행"), L("K Post", "우체국") },
            };
            void Tx(int month, int dom, string time, string counterparty, string memo, long amount)
            {
                if (!Seen(month, dom, time))
                    return;
                var t = new BankTransaction { when = At(month, dom, time), counterparty = counterparty, memo = memo, amount = amount };
                stamps[t] = Stamp(month, dom, time);
                p.bank.transactions.Add(t);
                // The balance on Day 1 already counts everything before it.
                if (Stamp(month, dom, time) > Stamp(10, 6, "16:20"))
                    everyday += amount;
            }
            string card = L("Card", "체크카드");
            Tx(9, 7, "10:12", L("CHOI YOUNGSIK", "최영식"), L("September rent", "9월 월세"), -450000);
            Tx(9, 20, "09:00", L("Hangul Telecom", "한글텔레콤"), L("Phone bill", "휴대폰 요금"), -43200);
            Tx(10, 1, "09:00", L("MANGWON ROASTERS", "(주)망원로스터스"), L("September wages", "9월 급여"), 486000);
            Tx(10, 3, "11:40", L("PARK HYEJIN", "박혜진"), L("Chuseok pocket money", "추석 용돈"), 100000);
            Tx(10, 5, "10:05", L("Hangang Mall", "한강몰"), L("Desk lamp bulbs", "스탠드 전구"), -12900);
            Tx(10, 5, "22:41", L("DAILY 24 Mangwon", "데일리24 망원점"), card, -5850);
            Tx(10, 6, "09:03", L("Mangwon Roasters", "망원로스터스"), card, -4500);
            Tx(10, 7, "12:40", L("Hanbit Univ. cafeteria", "한빛대 학생식당"), card, -8900);
            Tx(10, 8, "09:10", L("Mangwon Roasters", "망원로스터스"), card, -4500);
            NewestFirst(p.bank.transactions);
            p.bank.accounts = new List<BankAccount>
            {
                new BankAccount { name = L("Nuri Everyday", "누리 입출금통장"), number = "110-302-558814", balance = everyday },
                new BankAccount { name = L("Tuition savings", "등록금 적금"), number = "110-302-558990", balance = 3000000 },
            };
            void Notice(int dom, string time, string title, string body)
            {
                if (!Seen(10, dom, time))
                    return;
                var n = new BankNotice { when = Oct(dom, time), title = title, body = body };
                stamps[n] = Stamp(10, dom, time);
                p.bank.notices.Add(n);
            }
            Notice(5, "22:47", L("New device registered", "새 기기 등록"), L("MacBook Air · Seoul. If this wasn't you, call 1599-0000.", "MacBook Air · 서울. 본인이 아니면 1599-0000으로 연락 주세요."));
            Notice(6, "09:03", L("Card payment ₩4,500", "체크카드 승인 4,500원"), L("Mangwon Roasters · approved", "망원로스터스 · 승인"));
            Notice(6, "16:02", L("Signed in", "로그인"), L("This phone · Seoul. No other sign-ins today.", "이 휴대폰 · 서울. 오늘 다른 로그인 기록은 없습니다."));
            Notice(7, "12:40", L("Card payment ₩8,900", "체크카드 승인 8,900원"), L("Hanbit Univ. cafeteria · approved", "한빛대 학생식당 · 승인"));
            Notice(8, "09:10", L("Card payment ₩4,500", "체크카드 승인 4,500원"), L("Mangwon Roasters · approved", "망원로스터스 · 승인"));
            NewestFirst(p.bank.notices);

            // ---- parcels
            p.parcels = new List<Parcel>
            {
                new Parcel
                {
                    item = L("Desk lamp bulbs (2)", "스탠드 전구 (2개)"), seller = L("Hangang Mall", "한강몰"), tracking = "4410 2287 1934", courier = Courier,
                    driver = L("Kim Taesik", "김태식"), driverPhone = "010-7715-5521",
                    status = DayNumber <= 1 ? L("Out for delivery tomorrow", "내일 배송 예정") : DayNumber == 2 ? L("Out for delivery today", "오늘 배송 예정")
                                            : L("Delivered Wed 7 Oct, 17:32", "10월 7일(수) 17:32 배송 완료"),
                    window = DayNumber <= 2 ? L("Wed 7 Oct, 16:00-18:00", "10월 7일(수) 16:00~18:00") : "",
                },
                // Day 3's variants say where the monitor is by then.
                new Parcel
                {
                    item = L("27-inch monitor", "27인치 모니터"), seller = L("AliStar (overseas)", "알리스타 (해외)"), tracking = Tracking,
                    courier = L("Hangang Express (from Incheon)", "한강익스프레스 (인천 통관)"), overseas = true,
                    status = DayNumber <= 1 ? L("Shipped from Shenzhen, 3 Oct", "10월 3일 선전에서 출고") : L("Arrived in Korea (Incheon), 7 Oct", "10월 7일 국내 도착 (인천)"),
                },
                new Parcel
                {
                    item = L("Phone case", "휴대폰 케이스"), seller = L("AliStar (overseas)", "알리스타 (해외)"), status = L("Delivered 21 Sep", "9월 21일 배송 완료"),
                    tracking = "KR4410983312", courier = L("K Post", "우체국택배"), window = "", overseas = true,
                },
            };

            // ---- mail
            void Mail(int month, int dom, string time, string from, string address, string subject, string body, bool unread = false)
            {
                if (!Seen(month, dom, time))
                    return;
                var m = new MailItem { from = from, fromAddress = address, subject = subject, when = At(month, dom, time), body = body, unread = unread };
                stamps[m] = Stamp(month, dom, time);
                p.mails.Add(m);
            }
            Mail(9, 29, "22:10", L("AliStar", "알리스타"), "orders@alistar.com", L("Order confirmed: 27-inch monitor", "주문 완료: 27인치 모니터"),
                 L("Hi Kim Jiwoo,\n\nThanks for your order!\n\n27-inch IPS monitor × 1 · US$ 214.00\nShipping to Korea · US$ 9.00\n\nWe'll email you when it ships.",
                   "김지우 님, 주문해 주셔서 감사합니다!\n\n27인치 IPS 모니터 × 1 · US$ 214.00\n한국 배송비 · US$ 9.00\n\n발송되면 메일로 알려 드릴게요."));
            Mail(9, 30, "10:00", L("Hanbit University", "한빛대학교"), "scholarship@hanbit.ac.kr", L("Scholarship interview on 16 Oct", "10월 16일 장학금 면접 안내"),
                 L("Your scholarship interview is on Friday 16 October at 14:00, Student Centre room 204. Please bring your student ID.\n\nThe scholarship office never charges fees.",
                   "장학금 면접은 10월 16일(금) 14:00, 학생회관 204호에서 진행됩니다. 학생증을 지참해 주세요.\n\n장학팀은 어떠한 수수료도 받지 않습니다."), true);
            Mail(10, 2, "15:30", L("Daeyang Logistics careers", "대양물류 채용팀"), "careers@daeyang-logistics.co.kr", L("Application received: Winter internship", "동계 인턴십 지원 접수 완료"),
                 L("Thank you for applying to the Daeyang Logistics winter internship. We will contact you from our HR office (02-555-0240) if you are invited to an interview.",
                   "대양물류 동계 인턴십에 지원해 주셔서 감사합니다. 면접 대상자에게는 인사팀(02-555-0240)에서 연락드립니다."));
            Mail(10, 5, "20:15", L("Hanbit University IT", "한빛대학교 IT지원센터"), "it-help@hanbit.ac.kr", L("Your new laptop is registered", "새 노트북 등록 완료"),
                 L("Hi Kim Jiwoo,\n\nYour MacBook Air is now registered for campus Wi-Fi. Remember to sign in to your apps (e.g. banking) on the new device.\n\nHanbit IT Help Desk",
                   "김지우 님,\n\nMacBook Air가 교내 와이파이에 등록되었습니다. 새 기기에서 은행 앱 등에 다시 로그인해 주세요.\n\n한빛대학교 IT지원센터"));
            Mail(10, 5, "22:47", NuriBank, "notice@nuribank.co.kr", L("A new device was registered", "새 기기가 등록되었습니다"),
                 L("A new device (MacBook Air) was registered to your Nuri Bank account in Seoul.\n\nIf this was you, you don't need to do anything. If not, call 1599-0000.\n\nNuri Bank will never ask you to move your money to 'protect' it.",
                   "고객님의 누리은행 계정에 서울에서 새 기기(MacBook Air)가 등록되었습니다.\n\n본인이라면 따로 하실 일은 없습니다. 본인이 아니라면 1599-0000으로 연락 주세요.\n\n누리은행은 '보호'를 이유로 돈을 옮기라고 절대 요구하지 않습니다."));
            Mail(10, 7, "09:30", L("Hanbit University", "한빛대학교"), "notice@hanbit.ac.kr", L("Midterm timetable", "중간고사 시간표"),
                 L("Midterm exams run from Monday 13 October. Check your timetable on the student portal.", "중간고사는 10월 13일(월)부터 시작됩니다. 학생 포털에서 시간표를 확인하세요."));
            NewestFirst(p.mails);
            return p;
        }

        // ================================================================ world directory

        static WorldDirectory HouseholdDirectory()
        {
            var d = ScriptableObject.CreateInstance<WorldDirectory>();
            d.accounts = new List<DirAccount>
            {
                new DirAccount
                {
                    bank = NuriBank, number = "110-900-551207", holder = L("JEONG MIRAN", "정미란"),
                    reports = new List<string>
                    {
                        L("4 Oct · \"Said he was from the Nuri Bank protection team. I sent 2,000,000 won.\"", "10월 4일 · \"누리은행 보호팀이라고 해서 200만 원 보냈어요.\""),
                        L("29 Sep · \"Protected account story. The money was gone in minutes.\"", "9월 29일 · \"보호계좌 얘기. 몇 분 만에 돈이 빠져나갔어요.\""),
                    },
                },
                new DirAccount { bank = L("Daehan Bank", "대한은행"), number = "3333-12-7788901", holder = L("YOON JAEHEE", "윤재희") },
                new DirAccount { bank = NuriBank, number = "110-302-558814", holder = L("KIM JIWOO", "김지우") },
                new DirAccount { bank = NuriBank, number = "110-302-558990", holder = L("KIM JIWOO", "김지우") },
                new DirAccount { bank = NuriBank, number = "110-845-100233", holder = L("PARK HYEJIN", "박혜진") },
                new DirAccount { bank = NuriBank, number = "110-771-209944", holder = L("CHOI YOUNGSIK", "최영식") },
                new DirAccount { bank = NuriBank, number = "110-287-004411", holder = L("PARK TAEHO", "박태호") },
            };
            string unknown = L("Unknown", "알 수 없음"), internet = L("Unknown (internet phone)", "알 수 없음 (인터넷전화)");
            d.numbers = new List<DirNumber>
            {
                new DirNumber
                {
                    number = "070-8844-2019", owner = internet,
                    reports = new List<string>
                    {
                        L("5 Oct · \"Nuri Bank 'account protection team'. Wanted my savings moved.\"", "10월 5일 · \"누리은행 '계좌보호팀'. 예금을 옮기라고 함\""),
                        L("5 Oct · \"Knew the last digits of my account. Very convincing.\"", "10월 5일 · \"계좌 끝자리까지 알고 있어서 깜빡 속을 뻔\""),
                        L("4 Oct · \"Manager Jeon. Told me to stay on the line and not tell anyone.\"", "10월 4일 · \"전 과장. 전화 끊지 말고 아무한테도 말하지 말래요\""),
                        L("3 Oct · \"Protected account 110-900-551207, someone else's name on it.\"", "10월 3일 · \"보호계좌 110-900-551207, 다른 사람 명의\""),
                        L("2 Oct · \"Said someone logged in from Busan.\"", "10월 2일 · \"부산에서 누가 로그인했다고 함\""),
                        L("1 Oct · \"Scam. Hung up.\"", "10월 1일 · \"사기. 끊었음\""),
                        L("30 Sep · \"Bank impersonation.\"", "9월 30일 · \"은행 사칭\""),
                    },
                },
                new DirNumber { number = "1599-0000", owner = L("Nuri Bank customer centre", "누리은행 고객센터"), official = true },
                new DirNumber { number = "1588-5520", owner = L("Hangang Express customer centre", "한강익스프레스 고객센터"), official = true },
                new DirNumber { number = "1332", owner = L("Financial Supervisory Service fraud hotline", "금융감독원 불법금융 신고센터"), official = true },
                new DirNumber { number = "112", owner = L("Police", "경찰"), official = true },
                new DirNumber { number = "02-555-0112", owner = L("Mapo Police Station", "마포경찰서"), official = true },
                new DirNumber { number = "02-555-0181", owner = L("Mapo City Gas", "마포도시가스"), official = true },
                new DirNumber { number = "02-555-0192", owner = L("Mangwon Heights office", "망원하이츠 관리사무소"), official = true },
                new DirNumber
                {
                    number = "070-4852-1170", owner = internet,
                    reports = new List<string>
                    {
                        L("2 Oct · \"'Customs': pay duty for a parcel I never ordered.\"", "10월 2일 · \"'세관'이라며 주문한 적도 없는 택배 관세를 내라고 함\""),
                        L("1 Oct · \"Customs scam, asked for a transfer.\"", "10월 1일 · \"세관 사칭, 이체 요구\""),
                        L("28 Sep · \"Fake customs clearance.\"", "9월 28일 · \"가짜 통관 안내\""),
                        L("25 Sep · \"Scam.\"", "9월 25일 · \"사기\""),
                    },
                },
                new DirNumber
                {
                    number = "010-8812-4471", owner = unknown,
                    reports = new List<string>
                    {
                        L("Sun · \"Fake Nuri 'restricted account' text with a link.\"", "일요일 · \"가짜 누리은행 '계좌 제한' 문자, 링크 포함\""),
                        L("Sat · \"Smishing link nuri-secure.kr\"", "토요일 · \"스미싱 링크 nuri-secure.kr\""),
                        L("Sat · \"Spam.\"", "토요일 · \"스팸\""),
                    },
                },
                new DirNumber
                {
                    number = "010-5903-2271", owner = unknown,
                    reports = new List<string>
                    {
                        L("1 Oct · \"Fake 'rent account changed' text.\"", "10월 1일 · \"가짜 '월세 계좌 변경' 문자\""),
                        L("30 Sep · \"Smishing, rent-notice.kr\"", "9월 30일 · \"스미싱, rent-notice.kr\""),
                    },
                },
                new DirNumber
                {
                    number = "010-7240-1182", owner = unknown,
                    reports = new List<string>
                    {
                        L("6 Oct · \"'Unpaid customs duty' text with a link.\"", "10월 6일 · \"'관세 미납' 문자, 링크 포함\""),
                        L("6 Oct · \"Smishing.\"", "10월 6일 · \"스미싱\""),
                    },
                },
            };
            d.pages = new List<WebPage>
            {
                new WebPage
                {
                    url = "nuribank.co.kr/help", title = L("Nuri Bank customer centre", "누리은행 고객센터"), site = NuriBank, official = true,
                    aliases = new List<string> { "nuri bank", "누리은행", "누리", "nuri", "bank", "은행", "customer centre", "고객센터", "protection team", "보호팀", "protected account", "보호계좌", "safe account", "안전계좌" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Need help with your account or card? We're here 24 hours a day.", "계좌나 카드 관련 도움이 필요하신가요? 24시간 상담합니다.") },
                        new WebBlock { kind = WebBlockKind.Contact, text = L("Customer centre (24h)", "고객센터 (24시간)"), value = "1599-0000" },
                        new WebBlock { kind = WebBlockKind.Warning, text = L("Nuri Bank never asks you to move money to a 'protected' or 'safe' account. There is no such account. If someone asks, hang up and call 1599-0000 yourself.",
                                                                              "누리은행은 '보호계좌'나 '안전계좌'로 돈을 옮기라고 절대 요구하지 않습니다. 그런 계좌는 없습니다. 누군가 요구하면 전화를 끊고 직접 1599-0000으로 전화하세요.") },
                        new WebBlock { kind = WebBlockKind.Notice, text = L("Our staff call you from 1599-0000. If you're unsure, hang up and call us back.", "누리은행 직원은 1599-0000으로 전화합니다. 의심되면 끊고 다시 걸어 주세요.") },
                    },
                },
                new WebPage
                {
                    url = "nuri-secure.kr/verify", title = L("Nuri Bank Security Verification", "누리은행 보안인증"), site = "nuri-secure.kr", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Your account has been restricted. Enter your card details to lift the restriction within 24 hours.", "고객님의 계좌가 제한되었습니다. 24시간 안에 카드 정보를 입력해 제한을 해제하세요.") },
                        new WebBlock { kind = WebBlockKind.Form, text = L("Card number, expiry date and PIN", "카드번호, 유효기간, 비밀번호"), value = L("Verify now", "지금 인증하기") },
                    },
                },
                new WebPage
                {
                    url = "rent-notice.kr", title = L("Rent account change notice", "월세 계좌 변경 안내"), site = "rent-notice.kr", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Your landlord has registered a new rent account. Sign in with your bank details to see it.", "임대인이 새 월세 계좌를 등록했습니다. 은행 정보로 로그인하면 확인할 수 있습니다.") },
                        new WebBlock { kind = WebBlockKind.Form, text = L("Bank, account number and PIN", "은행, 계좌번호, 비밀번호"), value = L("See my new account", "새 계좌 확인하기") },
                    },
                },
                new WebPage
                {
                    url = "kr-customs.help/pay", title = L("Korea Customs · pay unpaid duty", "관세청 · 미납 관세 납부"), site = "kr-customs.help", official = false,
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Your parcel is held for unpaid customs duty (₩3,200). Pay by card now or it will be returned.", "고객님의 택배가 관세(3,200원) 미납으로 보류 중입니다. 지금 카드로 납부하지 않으면 반송됩니다.") },
                        new WebBlock { kind = WebBlockKind.Form, text = L("Card number, expiry date and CVC", "카드번호, 유효기간, CVC"), value = L("Pay ₩3,200", "3,200원 결제") },
                    },
                },
                new WebPage
                {
                    url = "fss.or.kr/fraud", title = L("Financial Supervisory Service: report financial fraud", "금융감독원: 금융사기 신고"), site = L("FSS", "금융감독원"), official = true,
                    aliases = new List<string> { "fss", "financial supervisory service", "금융감독원", "금감원", "1332", "voice phishing", "보이스피싱", "safe account", "안전계좌", "fraud", "사기" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Contact, text = L("Fraud hotline", "불법금융 신고센터"), value = "1332" },
                        new WebBlock { kind = WebBlockKind.Warning, text = L("No bank, prosecutor or government office will ask you to transfer money to a 'safe account' or an 'inspection account'.",
                                                                              "은행, 검찰, 정부기관은 '안전계좌'나 '검사계좌'로 돈을 이체하라고 절대 요구하지 않습니다.") },
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Sent money to a scammer? Call your bank or 112 at once: a transfer can be frozen if you act within minutes.",
                                                                                "사기범에게 돈을 보냈다면 즉시 은행이나 112에 신고하세요. 몇 분 안에 신고하면 지급정지가 가능합니다.") },
                    },
                },
                new WebPage
                {
                    url = "police.go.kr/mapo", title = L("Mapo Police Station", "마포경찰서"), site = L("Mapo Police", "마포경찰서"), official = true,
                    aliases = new List<string> { "mapo police", "police", "경찰", "mapo police station", "마포경찰서" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Contact, text = L("Main number", "대표번호"), value = "02-555-0112" },
                        new WebBlock { kind = WebBlockKind.Contact, text = L("Emergency", "긴급신고"), value = "112" },
                        new WebBlock { kind = WebBlockKind.Notice, text = L("Police never ask for transfers, 'safe accounts' or your bank details over the phone.", "경찰은 전화로 이체, '안전계좌', 금융정보를 절대 요구하지 않습니다.") },
                    },
                },
                new WebPage
                {
                    url = "mapocitygas.co.kr", title = L("Mapo City Gas: safety inspections", "마포도시가스: 안전점검 안내"), site = L("Mapo City Gas", "마포도시가스"), official = true,
                    aliases = new List<string> { "gas", "가스", "mapo city gas", "마포도시가스", "inspection", "점검", "boiler", "보일러" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Contact, text = L("Customer centre", "고객센터"), value = "02-555-0181" },
                        new WebBlock { kind = WebBlockKind.Notice, text = L("Inspections are announced by your building two weeks ahead. Inspectors show an ID card and never collect money.",
                                                                             "점검은 2주 전에 관리사무소를 통해 안내됩니다. 점검원은 신분증을 제시하며 절대 돈을 받지 않습니다.") },
                    },
                },
                new WebPage
                {
                    url = "seoulhousing.go.kr/rent", title = L("Seoul Housing Centre: paying your rent safely", "서울주거포털: 안전한 월세 납부"), site = L("Seoul Housing Centre", "서울주거포털"), official = true,
                    aliases = new List<string> { "rent", "월세", "landlord", "집주인", "임대인", "housing", "주거", "lease", "계약서", "rent account", "월세 계좌", "tenant", "세입자" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Most leases say how the rent account can change. If yours says 'in writing', a phone call alone is not enough.",
                                                                                "대부분의 계약서에는 월세 계좌를 바꾸는 방법이 적혀 있습니다. '서면으로'라고 되어 있다면 전화 한 통만으로는 부족합니다.") },
                        new WebBlock { kind = WebBlockKind.Warning, text = L("A new rent account in the name of someone who isn't in your lease is the most common rental scam. Check the name before you send.",
                                                                              "계약서에 없는 사람 명의의 새 월세 계좌는 가장 흔한 임대 사기입니다. 보내기 전에 예금주를 확인하세요.") },
                        new WebBlock { kind = WebBlockKind.Notice, text = L("Unsure? Ask your landlord on the number you already have, or check the residents' notices.",
                                                                             "의심되면 이미 알고 있는 번호로 집주인에게 확인하거나 입주민 공지를 확인하세요.") },
                    },
                },
                new WebPage
                {
                    url = "customs.go.kr/import", title = L("Korea Customs Service: shopping from overseas", "관세청: 해외직구 안내"), site = L("Korea Customs Service", "관세청"), official = true,
                    aliases = new List<string> { "customs", "관세청", "세관", "duty", "관세", "import", "직구", "vat", "부가세", "korea customs", "overseas", "해외" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Personal orders up to US$150 are free of duty. Above that you pay duty and VAT, unless the seller collected them at checkout.",
                                                                                "개인 해외직구는 미화 150달러까지 면세입니다. 그 이상이면 관세와 부가세를 내야 하며, 판매자가 결제 때 미리 받은 경우는 예외입니다.") },
                        new WebBlock { kind = WebBlockKind.Notice, text = L("Duty is paid through your courier (their app or a virtual account in the courier's name) or on our UNI-PASS site.",
                                                                             "관세는 택배사(앱 또는 택배사 명의의 가상계좌)나 관세청 UNI-PASS에서 납부합니다.") },
                        new WebBlock { kind = WebBlockKind.Warning, text = L("Customs never asks you to pay into an account in a person's name, or through a link in a text.",
                                                                              "관세청은 개인 명의 계좌로 입금하거나 문자 속 링크로 납부하라고 요구하지 않습니다.") },
                    },
                },
                new WebPage
                {
                    url = "seouldaily.kr/2026/10/06/read-the-name", title = L("Before you send, read the name", "송금 전, 받는 사람 이름을 확인하세요"), site = L("Seoul Daily", "서울데일리"), official = true,
                    aliases = new List<string> { "seoul daily", "서울데일리", "news", "뉴스", "read the name", "protected account", "보호계좌", "voice phishing", "보이스피싱" },
                    blocks = new List<WebBlock>
                    {
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Callers claiming to be bank 'protection teams' are asking customers to move their savings to 'protected accounts'.",
                                                                                "은행 '보호팀'을 사칭한 전화가 고객들에게 예금을 '보호계좌'로 옮기라고 요구하고 있다.") },
                        new WebBlock { kind = WebBlockKind.Paragraph, text = L("Every banking app shows the account holder's name before a transfer goes through. A person's name on an 'official' account is the giveaway.",
                                                                                "모든 은행 앱은 이체 전에 예금주 이름을 보여 준다. '공식' 계좌라는데 개인 이름이 뜬다면 그것이 결정적 단서다.") },
                    },
                },
            };
            d.callbacks = new List<CallbackScript>
            {
                Callback("1599-0000", L("Nuri Bank customer centre", "누리은행 고객센터"), "pt_song", VoiceBank,
                    Say(L("Nuri Bank customer centre, this is Song Eunji speaking. How can I help?", "누리은행 고객센터 송은지입니다. 무엇을 도와드릴까요?")),
                    Say(L("Your account looks normal. If anyone asks you to move money, hang up and call us on this number.", "고객님 계좌에는 이상이 없습니다. 누가 돈을 옮기라고 하면 전화를 끊고 이 번호로 연락 주세요."))),
                Callback("1332", L("FSS fraud hotline", "금융감독원 신고센터"), "pt_oh", VoiceFss,
                    Say(L("Financial Supervisory Service, fraud hotline.", "금융감독원 불법금융 신고센터입니다.")),
                    Say(L("If someone asks you to move money to a 'safe account', it's a scam. Report the number to 112.", "누가 '안전계좌'로 돈을 옮기라고 하면 사기입니다. 그 번호를 112에 신고하세요."),
                        L("If someone asks you to move money to a safe account, it's a scam. Report the number to one one two.", "누가 안전계좌로 돈을 옮기라고 하면 사기입니다. 그 번호를 일일이에 신고하세요."))),
                Callback("112", L("Police 112", "경찰 112"), "pt_oh", VoicePolice,
                    Say(L("112, police. What's happening?", "112입니다. 무슨 일이세요?"), L("One one two, police. What's happening?", "일일이입니다. 무슨 일이세요?")),
                    Say(L("Tell us the number and the account. We can ask the bank to freeze a transfer if you call right away.", "번호와 계좌를 알려 주세요. 바로 신고하시면 은행에 지급정지를 요청할 수 있습니다."))),
                Callback("010-2231-7745", L("Mom", "엄마"), "pt_mom", VoiceMom,
                    Say(L("Jiwoo-ya! Did you eat?", "지우야! 밥은 먹었어?"), L("Jiwoo ya! Did you eat?", null)),
                    Say(L("Your dad cooked doenjang stew today. Come home on the 17th, okay?", "오늘 아빠가 된장찌개 끓였어. 17일에 집에 올 거지?"),
                        L("Your dad cooked doenjang stew today. Come home on the seventeenth, okay?", "오늘 아빠가 된장찌개 끓였어. 십칠 일에 집에 올 거지?"))),
                Callback("010-4418-0902", L("Dad", "아빠"), "pt_dad", VoiceDad,
                    Say(L("Jiwoo! Everything okay?", "지우야! 별일 없지?")),
                    Say(L("Call your mom, she misses you.", "엄마한테 전화 좀 해라, 보고 싶어 하더라."))),
                Callback("02-555-0192", L("Mangwon Heights office", "망원하이츠 관리사무소"), "app_contacts", VoicePolice,
                    Say(L("Mangwon Heights office.", "망원하이츠 관리사무소입니다.")),
                    Say(L("The gas inspection is on the 14th. They won't ask for any money.", "가스 점검은 14일이에요. 돈은 절대 안 받아요."),
                        L("The gas inspection is on the fourteenth. They won't ask for any money.", "가스 점검은 십사 일이에요. 돈은 절대 안 받아요."))),
                Callback("070-8844-2019", L("Manager Jeon", "전 과장"), "pt_jeon", VoiceJeon,
                    Say(L("Nuri Bank protection team... Miss Kim? Good, please go ahead with the transfer.", "누리은행 보호팀입니다… 김지우 씨? 네, 이체 진행해 주세요."))),
            };
            return d;
        }

        // ================================================================ room

        static RoomContent HouseholdRoom()
        {
            var r = ScriptableObject.CreateInstance<RoomContent>();
            r.newspaper = new NewspaperData
            {
                masthead = L("SEOUL DAILY", "서울데일리"),
                issue = L($"No. {12407 + DayNumber:N0}", $"제{12407 + DayNumber:N0}호"),
                dateLine = L(today.ToString("dddd, MMMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant(),
                             $"{today.Year}년 {today.Month}월 {today.Day}일 {KoWeekday(today)}"),
                price = L("1,000 won", "1,000원"),
                photo = Tex("news_photo"),
                caption = L("The transfer screen shows who really gets the money. Photo: Seoul Daily", "송금 화면에는 돈을 실제로 받는 사람이 나온다. 사진: 서울데일리"),
                ads = new List<NewsItem>
                {
                    new NewsItem { title = L("치킨 CHICKEN 24H", "24시 치킨"), text = L("Crispy & soy garlic. Free delivery over 20,000 won. 02-555-0147", "후라이드 & 간장마늘. 2만 원 이상 무료 배달. 02-555-0147") },
                    new NewsItem { title = L("HANBIT ACADEMY", "한빛학원"), text = L("TOEIC weekend class, 30% off for students. 02-555-0166", "토익 주말반, 대학생 30% 할인. 02-555-0166") },
                    new NewsItem { title = L("EASY MONEY!", "고수익 알바!"), text = L("Remote job: forward client deposits, keep 3%. No experience! 010-3309-2214", "재택 부업: 고객 입금액을 전달만 하면 3% 수당. 경력 무관! 010-3309-2214") },
                },
            };

            r.drawer = new List<DocumentData>
            {
                new DocumentData
                {
                    title = LeaseTitle, kind = DocumentKind.Contract, issuer = L("Mangwon Heights", "망원하이츠"),
                    fields = new List<DocField>
                    {
                        Field(L("Landlord", "임대인"), L("Choi Youngsik", "최영식")),
                        Field(L("Landlord's phone", "임대인 연락처"), "010-5512-3380", FactKind.Phone),
                        Field(L("Rent", "월세"), L("450,000 won a month, due on the 7th", "월 45만 원, 매달 7일 납부")),
                        Field(L("Rent account", "월세 계좌"), L("Nuri Bank 110-771-209944 (Choi Youngsik)", "누리은행 110-771-209944 (최영식)"), FactKind.Account),
                        Field(L("Deposit", "보증금"), L("10,000,000 won", "1,000만 원")),
                        Field(L("If absent, contact", "부재 시 연락처"), L("Choi Hyunwoo (son) 010-2280-6614", "최현우 (아들) 010-2280-6614"), FactKind.Phone),
                    },
                    body = L("Special terms\n1. The rent account changes only with a written notice from the landlord.\n2. The tenant allows the annual gas safety inspection.\n3. No smoking inside the unit.",
                             "특약사항\n1. 월세 계좌는 임대인의 서면 통지로만 변경한다.\n2. 임차인은 연 1회 가스 안전점검에 협조한다.\n3. 실내 흡연 금지."),
                },
                new DocumentData
                {
                    title = L("Gas bill · September", "도시가스 고지서 · 9월"), kind = DocumentKind.Bill, issuer = L("Mapo City Gas", "마포도시가스"), stamp = "stamp_paid",
                    fields = new List<DocField>
                    {
                        Field(L("Customer", "고객"), L("Kim Jiwoo · Mangwon Heights 302", "김지우 · 망원하이츠 302호")),
                        Field(L("Amount", "금액"), L("18,420 won", "18,420원")),
                        Field(L("Paid", "납부"), L("28 Sep (auto-pay)", "9월 28일 (자동이체)")),
                        Field(L("Customer centre", "고객센터"), "02-555-0181", FactKind.Phone),
                    },
                    body = L("Safety inspection this month: Wednesday 14 October. Our inspectors carry an ID card and never ask for money.",
                             "이달 안전점검: 10월 14일(수). 점검원은 신분증을 지참하며 절대 돈을 요구하지 않습니다."),
                },
                new DocumentData
                {
                    title = L("Mobile bill · September", "휴대폰 요금 고지서 · 9월"), kind = DocumentKind.Bill, issuer = L("Hangul Telecom", "한글텔레콤"),
                    fields = new List<DocField>
                    {
                        Field(L("Number", "번호"), "010-6620-0921"),
                        Field(L("Amount", "금액"), L("43,200 won", "43,200원")),
                        Field(L("Due", "납부기한"), L("20 October", "10월 20일")),
                        Field(L("Customer centre", "고객센터"), "02-555-0114", FactKind.Phone),
                    },
                },
                new DocumentData
                {
                    title = L("Receipt · 5 Oct", "영수증 · 10월 5일"), kind = DocumentKind.Receipt, issuer = L("DAILY 24 Mangwon", "데일리24 망원점"), image = Tex("doc_receipt"),
                    fields = new List<DocField>
                    {
                        Field(L("Total", "합계"), L("5,850 won", "5,850원")),
                        Field(L("Card", "카드"), L("Nuri ****0921", "누리 ****0921")),
                    },
                },
                new DocumentData
                {
                    title = L("Scholarship interview", "장학금 면접 안내"), kind = DocumentKind.Letter, issuer = L("Hanbit University", "한빛대학교"),
                    fields = new List<DocField>
                    {
                        Field(L("When", "일시"), L("Fri 16 Oct, 14:00", "10월 16일(금) 14:00")),
                        Field(L("Where", "장소"), L("Student Centre 204", "학생회관 204호")),
                        Field(L("Office", "장학팀"), "02-555-0200", FactKind.Phone),
                    },
                    body = L("Please bring your student ID. The scholarship office never charges a fee.", "학생증을 지참해 주세요. 장학팀은 어떠한 수수료도 받지 않습니다."),
                },
                new DocumentData
                {
                    title = SlipTitle, kind = DocumentKind.Stub, issuer = Courier,
                    fields = new List<DocField>
                    {
                        Field(L("Tracking", "운송장"), "4410 2287 1934", FactKind.Case),
                        Field(L("Driver", "배송기사"), L("Kim Taesik", "김태식")),
                        Field(L("Driver's phone", "기사 연락처"), "010-7715-5521", FactKind.Phone),
                        Field(L("Customer centre", "고객센터"), "1588-5520", FactKind.Phone),
                    },
                },
            };

            r.board = new List<BoardItem>
            {
                new BoardItem
                {
                    title = L("Happy Pharmacy calendar · October", "행복약국 달력 · 10월"), kind = BoardItemKind.Calendar, image = Tex("board_calendar"),
                    position = new Vector2(0.03f, 0.05f), width = 0.25f, rotation = -1.5f,
                    calendar = new List<CalendarEntry>
                    {
                        new CalendarEntry { day = 5, text = L("Substitute holiday", "대체공휴일") },
                        new CalendarEntry { day = 7, text = L("Rent", "월세") },
                        new CalendarEntry { day = 8, text = L("Dentist", "치과") },
                        new CalendarEntry { day = 13, text = L("Midterms start", "중간고사 시작") },
                        new CalendarEntry { day = 14, text = L("Gas check", "가스 점검") },
                        new CalendarEntry { day = 17, text = L("Mom's birthday", "엄마 생신") },
                    },
                },
                new BoardItem
                {
                    title = NumbersTitle, kind = BoardItemKind.Note, image = Tex("board_numbers"),
                    position = new Vector2(0.32f, 0.04f), width = 0.24f, rotation = 2.5f,
                    details = new List<DocField>
                    {
                        Field(L("Police", "경찰"), "112", FactKind.Phone),
                        Field(L("Fire / ambulance", "화재 · 구급"), "119", FactKind.Phone),
                        Field(L("Voice phishing (FSS)", "보이스피싱 (금감원)"), "1332", FactKind.Phone),
                        Field(NuriBank, "1599-0000", FactKind.Phone),
                        Field(L("Landlord", "집주인"), "010-5512-3380", FactKind.Phone),
                        Field(L("Mom", "엄마"), "010-2231-7745", FactKind.Phone),
                        Field(L("Dad", "아빠"), "010-4418-0902", FactKind.Phone),
                    },
                },
                new BoardItem
                {
                    title = L("Building notice", "관리사무소 안내"), kind = BoardItemKind.Notice, image = Tex("board_notice"),
                    position = new Vector2(0.6f, 0.05f), width = 0.25f, rotation = -2f,
                    details = new List<DocField>
                    {
                        Field(L("Gas inspection", "가스 안전점검"), L("Wed 14 Oct, 10:00-17:00", "10월 14일(수) 10:00~17:00")),
                        Field(L("Water tank cleaning", "물탱크 청소"), L("Thu 8 Oct, 10:00-12:00", "10월 8일(목) 10:00~12:00")),
                        Field(L("Note", "참고"), L("Inspectors never ask for money or bank details", "점검원은 돈이나 금융정보를 절대 요구하지 않습니다")),
                        Field(Office, "02-555-0192", FactKind.Phone),
                    },
                },
                new BoardItem
                {
                    title = StickyTitle, kind = BoardItemKind.Note, image = Tex("board_sticky_y"),
                    position = new Vector2(0.33f, 0.52f), width = 0.1f, rotation = -4f,
                    details = new List<DocField> { Field(L("Reminder", "할 일"), L("Phone bill: pay by the 20th (auto-pay?)", "휴대폰 요금: 20일까지 (자동이체?)")) },
                },
                new BoardItem
                {
                    title = StickyTitle, kind = BoardItemKind.Note, image = Tex("board_sticky_p"),
                    position = new Vector2(0.45f, 0.57f), width = 0.1f, rotation = 7f,
                    details = new List<DocField> { Field(L("Reminder", "할 일"), L("Mom's birthday on the 17th: call + gift", "17일 엄마 생신: 전화 + 선물")) },
                },
                new BoardItem
                {
                    title = L("Concert ticket", "콘서트 티켓"), kind = BoardItemKind.Ticket, image = Tex("board_ticket"),
                    position = new Vector2(0.58f, 0.55f), width = 0.24f, rotation = -3f,
                    details = new List<DocField>
                    {
                        Field(L("Show", "공연"), L("The Rainy Seasons, Hongdae", "비 오는 계절 단독 공연, 홍대")),
                        Field(L("When", "일시"), L("Sat 7 Nov, 19:00", "11월 7일(토) 19:00")),
                        Field(L("Seat", "좌석"), L("Standing A-128", "스탠딩 A-128")),
                    },
                },
                new BoardItem
                {
                    title = L("Photo booth strip", "인생네컷"), kind = BoardItemKind.Photo, image = Tex("board_strip"),
                    position = new Vector2(0.86f, 0.42f), width = 0.09f, rotation = 4f,
                    details = new List<DocField> { Field(L("With", "함께"), L("Yuna, 12 Sep", "유나, 9월 12일")) },
                },
                new BoardItem
                {
                    title = L("ID photo", "증명사진"), kind = BoardItemKind.Photo, image = Tex("photo_id"),
                    position = new Vector2(0.88f, 0.08f), width = 0.08f, rotation = -6f,
                },
            };

            r.wallet = new List<WalletCard>
            {
                new WalletCard
                {
                    title = CardTitle, front = Tex("card_nuri_front"), back = Tex("card_nuri_back"),
                    details = new List<DocField>
                    {
                        Field(L("Name", "이름"), "KIM JIWOO"),
                        Field(L("Card", "카드번호"), "9410 2231 7745 0921"),
                        Field(L("Customer centre", "고객센터"), "1599-0000", FactKind.Phone),
                    },
                },
                new WalletCard
                {
                    title = L("Student ID", "학생증"), front = Tex("card_student"),
                    details = new List<DocField> { Field(L("University", "학교"), L("Hanbit University", "한빛대학교")), Field(L("Student no.", "학번"), "2024-10573") },
                },
                new WalletCard
                {
                    title = L("Cafe stamp card", "카페 스탬프 카드"), front = Tex("card_cafe"),
                    details = new List<DocField> { Field(L("Stamps", "스탬프"), L("7 of 10", "10개 중 7개")) },
                },
            };

            r.rules = new List<RuleEntry>
            {
                new RuleEntry
                {
                    title = L("Call a number you looked up yourself", "직접 찾은 번호로 전화하기"),
                    text = L("Unsure? Hang up and call the number on your card or the official website, not the number the caller gives you.",
                             "의심되면 전화를 끊고, 상대가 알려 준 번호가 아니라 카드 뒷면이나 공식 홈페이지의 번호로 직접 전화하세요."),
                    learnedOn = L("Seoul Daily, 6 Oct", "서울데일리, 10월 6일"),
                },
                new RuleEntry
                {
                    title = L("Read the name on the account", "예금주 이름 확인하기"),
                    text = L("Before you send money, check the account holder's name on the transfer screen. A person's name on an 'official' account is a red flag.",
                             "돈을 보내기 전에 이체 화면에서 예금주 이름을 확인하세요. '공식' 계좌라는데 개인 이름이라면 위험 신호입니다."),
                    learnedOn = L("Seoul Daily, 6 Oct", "서울데일리, 10월 6일"),
                },
            };
            return r;
        }

        /// <summary>Adds a message to a Talk thread of a built phone (for a variant's own evidence).</summary>
        static void AddChat(PhoneContent p, string chatId, int dom, string time, string sender, string avatar, string text)
        {
            var t = p.chats.Find(c => c.id == chatId);
            if (t == null || !Seen(10, dom, time))
                return;
            t.messages.Add(new ChatMessage { sender = sender, avatar = avatar, when = Oct(dom, time), text = text });
            p.chats.Remove(t);
            p.chats.Insert(0, t);
        }

        /// <summary>Adds a text to a thread of a built phone, putting the thread on top.</summary>
        static void AddSms(PhoneContent p, string sender, int dom, string time, string text, string link = null)
        {
            if (!Seen(10, dom, time))
                return;
            var t = p.sms.Find(x => x.sender == sender);
            if (t == null)
                t = new SmsThread { sender = sender };
            else
                p.sms.Remove(t);
            p.sms.Insert(0, t);
            t.messages.Add(new SmsMessage { when = Oct(dom, time), text = text, link = link });
        }
    }
}
