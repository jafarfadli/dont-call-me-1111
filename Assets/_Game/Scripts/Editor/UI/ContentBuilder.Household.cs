using System;
using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Jiwoo's life as one timeline, shown as it stands when the day being built starts: contacts,
    /// chats and the bank on the phone; the paper, the drawer and the calendar in the room; and
    /// what the computer's CheckFirst knows about numbers and accounts. Nothing here depends on how
    /// an earlier day went (that is what echoes are for) or on today's truth (the variants add
    /// that). Every text is written in English and Korean (<see cref="L"/>).
    /// </summary>
    public static partial class ContentBuilder
    {
        const long EverydayAtDay1 = 1284300;
        const string Tracking = "HX-5520-1183-KR";
        static string TrackingSpoken => L("H X, five five two zero, one one eight three, K R", "에이치 엑스, 오오이공, 일일팔삼, 케이 알");

        // Chat thread ids.
        const string FamilyChat = "family";
        const string VillaChat = "villa";
        const string CourierChat = "hangang";
        const string ShopChat = "alistar";

        // Names that clues and other days look things up by: keep them in one place.
        static string NuriBank => L("Nuri Bank", "누리은행");
        static string HanbitBank => L("Hanbit Bank", "한빛은행");
        static string Courier => L("Hangang Express", "한강익스프레스");
        static string LeaseTitle => L("Lease contract · Unit 302", "임대차 계약서 · 302호");
        static string SlipTitle => L("Delivery slip", "택배 송장");
        static string GasBillTitle => L("Gas bill · September", "도시가스 고지서 · 9월");
        static string VillaTitle => L("Mangwon Heights residents", "망원하이츠 입주민");
        static string Office => L("Office", "관리사무소");
        static string Landlord => L("Landlord Choi", "집주인 최 사장님");

        // Where clues are found, as the next morning's summary words it.
        static string AtAccountCheck => L("Computer → Check a bank account", "컴퓨터 → 계좌 조회");
        static string AtNumberCheck => L("Computer → Check a phone number", "컴퓨터 → 전화번호 조회");
        static string AtLease => L("Desk drawer → Lease contract", "책상 서랍 → 임대차 계약서");
        static string AtVillaChat => L("Chats → Mangwon Heights residents", "채팅 → 망원하이츠 입주민");
        static string AtCalendar => L("the calendar on the wall", "벽에 걸린 달력");
        static string AtPaper => L("the newspaper on the desk", "책상 위 신문");
        static string AtGasBill => L("Desk drawer → Gas bill", "책상 서랍 → 도시가스 고지서");
        static string AtBank => L("Phone → Nuri Bank", "휴대폰 → 누리은행");
        static string AtContacts => L("Phone → Contacts", "휴대폰 → 연락처");

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
                new Contact { name = Landlord, number = "010-5512-3380", memo = L("Choi Youngsik · 1F · rent due on the 7th", "최영식 · 1층 · 월세 매달 7일"), portrait = "pt_landlord" },
                new Contact { name = L("Manager Han (cafe)", "한 매니저 (카페)"), number = "010-5530-8812", memo = L("Mangwon Roasters", "망원로스터스"), portrait = "pt_cafe" },
                new Contact { name = NuriBank, number = "1599-0000", memo = L("Customer centre, the number on my card", "고객센터, 카드에 적힌 번호"), portrait = "app_bank" },
                new Contact { name = L("Mangwon Heights office", "망원하이츠 관리사무소"), number = "02-555-0192", memo = L("Building management", "건물 관리"), portrait = "app_contacts" },
            };

            // ---- chats
            ChatThread Thread(string id, string title, string avatar, bool group) => new ChatThread { id = id, title = title, avatar = avatar, group = group };
            void Chat(ChatThread t, int month, int dom, string time, string sender, string avatar, string text, string photo = null)
            {
                if (!Seen(month, dom, time))
                    return;
                t.messages.Add(new ChatMessage { sender = sender, avatar = avatar, when = At(month, dom, time), text = text, photoCaption = photo });
                stamps[t] = Stamp(month, dom, time);
            }
            void Mine(ChatThread t, int dom, string time, string text)
            {
                if (!Seen(10, dom, time))
                    return;
                t.messages.Add(new ChatMessage { outgoing = true, when = Oct(dom, time), text = text });
                stamps[t] = Stamp(10, dom, time);
            }

            string mom = L("Mom", "엄마"), dad = L("Dad", "아빠"), minjun = L("Minjun", "민준이");
            var family = Thread(FamilyChat, L("Our family", "우리 가족"), "photo_family", true);
            Chat(family, 10, 3, "11:42", mom, "pt_mom", L("Sent you a little Chuseok pocket money. Eat something nice!", "추석 용돈 조금 보냈어. 맛있는 거 사 먹어!"));
            Mine(family, 3, "11:50", L("Thank you Mom!! ♥", "엄마 고마워요!! ♥"));
            Chat(family, 10, 4, "20:10", dad, "pt_dad", L("Tomorrow is a holiday. Get some rest, and lock your door.", "내일은 쉬는 날이다. 푹 쉬고 문단속 잘해라."));
            Chat(family, 10, 5, "19:10", mom, "pt_mom", L("Jiwoo-ya, did you eat dinner?", "지우야, 저녁은 먹었니?"));
            Mine(family, 5, "19:14", L("Yes! Kimchi stew after my shift", "응! 알바 끝나고 김치찌개 먹었어"));
            Chat(family, 10, 5, "22:30", minjun, "pt_minjun", L("I'm coming up to Seoul on Saturday for a concert. Noona, can I sleep over?", "누나 나 토요일에 콘서트 보러 서울 가는데 자고 가도 돼?"));
            Mine(family, 5, "22:41", L("Only if you bring snacks", "간식 사 오면 ㅇㅋ"));
            Chat(family, 10, 6, "16:12", mom, "pt_mom", null, L("Doenjang stew on the stove", "보글보글 된장찌개"));
            Chat(family, 10, 6, "16:12", mom, "pt_mom", L("Your dad cooked today! Come home for Mom's birthday on the 17th, okay?", "오늘은 아빠가 요리했어! 17일 엄마 생일에는 집에 올 거지?"));
            Chat(family, 10, 7, "08:05", mom, "pt_mom", L("Good morning! Rent day today, don't forget :)", "좋은 아침! 오늘 월세 내는 날이야, 잊지 마^^"));
            Mine(family, 7, "08:20", L("I know, Mom...", "알아 엄마~"));
            Chat(family, 10, 7, "21:15", minjun, "pt_minjun", L("Midterms start Monday. Send help (and snacks)", "월요일부터 중간고사… 살려줘 (간식도)"));
            Chat(family, 10, 8, "12:03", dad, "pt_dad", L("Grandma made extra kimchi for you. Come and get it on the 17th.", "할머니가 네 김치도 따로 담그셨다. 17일에 와서 가져가라."));

            var villa = Thread(VillaChat, VillaTitle, "app_contacts", true);
            Chat(villa, 10, 5, "09:00", Office, "app_contacts",
                 L("Reminder: gas safety inspection on Wed 14 Oct, 10:00-17:00. Inspectors never ask for money.",
                   "안내: 10월 14일(수) 10:00~17:00 가스 안전점검이 있습니다. 점검원은 절대 돈을 요구하지 않습니다."));
            Chat(villa, 10, 5, "10:31", L("201", "201호"), "pt_unknown",
                 L("Did anyone else get a call about a 'protected account'? Hung up on them.", "혹시 '보호계좌' 어쩌고 하는 전화 받으신 분 있나요? 바로 끊었어요."));
            Chat(villa, 10, 5, "11:48", L("401", "401호"), "pt_unknown",
                 L("Now someone \"from the gas company\" called me about an unpaid bill. They wanted a transfer today!", "이번엔 '가스 회사'라면서 요금이 미납됐다는 전화가 왔어요. 오늘 안에 이체하라던데요!"));
            Chat(villa, 10, 5, "12:05", Landlord, "pt_landlord",
                 L($"Don't send anything! Mapo City Gas never phones for money. The real number is on your bill: {GasOfficialNumber}. - Choi, 1F",
                   $"절대 보내지 마세요! 마포도시가스는 전화로 돈을 요구하지 않아요. 진짜 번호는 고지서에 있는 {GasOfficialNumber}이에요. - 1층 최"));
            Chat(villa, 10, 6, "14:05", Office, "app_contacts",
                 L("Water tank cleaning on Thursday 10:00-12:00. Please keep some water aside.", "목요일 10:00~12:00 물탱크 청소가 있습니다. 미리 물을 받아 두세요."));
            Chat(villa, 10, 8, "12:15", Office, "app_contacts", L("The water is back on. Thank you for your patience!", "단수가 끝났습니다. 협조해 주셔서 감사합니다!"));

            string yunaName = L("Yuna", "유나");
            var yuna = Thread("yuna", yunaName, "pt_yuna", false);
            Chat(yuna, 10, 4, "18:55", yunaName, "pt_yuna", L("Tickets done!! Standing A-128 and A-129, 7 Nov", "티켓팅 성공!! 11월 7일 스탠딩 A-128, A-129"));
            Mine(yuna, 4, "19:01", L("I owe you 55,000 won, sending it on payday", "5만 5천 원 월급날 보낼게"));
            Chat(yuna, 10, 7, "12:14", yunaName, "pt_yuna", L("Tteokbokki after class tomorrow?", "내일 수업 끝나고 떡볶이 ㄱ?"));
            Mine(yuna, 7, "12:20", L("Yes!! 5pm?", "콜!! 5시?"));

            string han = L("Manager Han", "한 매니저");
            var cafe = Thread("cafe", L("Mangwon Roasters crew", "망원로스터스 크루"), "pt_cafe", true);
            Chat(cafe, 10, 2, "18:00", han, "pt_cafe", L("Thanks for covering Chuseok week, everyone. We are closed on Monday the 5th.", "추석 주간 근무 다들 고생했어요. 5일 월요일은 휴무입니다."));
            Chat(cafe, 10, 5, "21:00", han, "pt_cafe", L("Wages go out on the 10th as usual. Can someone cover Saturday morning?", "급여는 평소처럼 10일에 나가요. 토요일 오전 대타 가능한 분?"));
            Chat(cafe, 10, 8, "10:30", han, "pt_cafe", L("Sunday opening shift still free, please! Double stamps for whoever takes it.", "일요일 오픈 근무 아직 비어 있어요ㅠㅠ 하시는 분 스탬프 두 배!"));

            // The courier's and the shop's notice channels. Day 3's variants add where the monitor is by then.
            var courier = Thread(CourierChat, Courier, "av_courier", false);
            Chat(courier, 9, 21, "14:20", Courier, "av_courier",
                 L("[Hangang Express] Delivered: phone case, left at the door of 302.", "[한강익스프레스] 배송 완료: 휴대폰 케이스, 302호 문 앞."));
            Chat(courier, 10, 6, "08:30", Courier, "av_courier",
                 L("[Hangang Express] Your parcel (desk lamp bulbs) arrives tomorrow, 16:00-18:00. Questions? Customer centre 1588-5520.",
                   "[한강익스프레스] 고객님의 택배(스탠드 전구)가 내일 16:00~18:00에 도착합니다. 문의: 고객센터 1588-5520"));
            Chat(courier, 10, 7, "17:32", Courier, "av_courier",
                 L("[Hangang Express] Delivered: desk lamp bulbs, left at the door of 302.", "[한강익스프레스] 배송 완료: 스탠드 전구, 302호 문 앞."));

            string shopName = L("AliStar", "알리스타");
            var shop = Thread(ShopChat, shopName, "av_shop", false);
            Chat(shop, 9, 29, "22:10", shopName, "av_shop",
                 L("Thanks for your order, Kim Jiwoo!\n27-inch IPS monitor × 1: US$ 214.00\nShipping to Korea: US$ 9.00",
                   "김지우 님, 주문해 주셔서 감사합니다!\n27인치 IPS 모니터 × 1: US$ 214.00\n한국 배송비: US$ 9.00"));
            Chat(shop, 10, 3, "09:15", shopName, "av_shop",
                 L($"Your monitor has left our warehouse in Shenzhen. In Korea it travels with Hangang Express: {Tracking}.",
                   $"모니터가 선전 창고에서 출고되었습니다. 국내 배송은 한강익스프레스가 맡습니다: {Tracking}"));

            p.chats = new List<ChatThread> { family, villa, yuna, cafe, courier, shop };
            NewestFirst(p.chats);

            // ---- bank
            long everyday = EverydayAtDay1;
            var day1 = Stamp(10, 6, "16:20");
            p.bank = new BankData { bankName = NuriBank };
            void Tx(int month, int dom, string time, string counterparty, string memo, long amount)
            {
                var at = Stamp(month, dom, time);
                if (!Seen(month, dom, time))
                {
                    // Not spent yet: the balance on Day 1 counts it, the balance of an earlier day does not.
                    if (at <= day1)
                        everyday -= amount;
                    return;
                }
                var t = new BankTransaction { when = At(month, dom, time), counterparty = counterparty, memo = memo, amount = amount };
                stamps[t] = at;
                p.bank.transactions.Add(t);
                // The balance on Day 1 already counts everything before it.
                if (at > day1)
                    everyday += amount;
            }
            string card = L("Card", "체크카드");
            Tx(9, 7, "10:12", L("CHOI YOUNGSIK", "최영식"), L("September rent · Nuri 110-771-209944", "9월 월세 · 누리 110-771-209944"), -450000);
            Tx(9, 20, "09:00", L("Hangul Telecom", "한글텔레콤"), L("Phone bill", "휴대폰 요금"), -43200);
            Tx(9, 28, "09:00", GasCompany, L("September gas bill · auto-pay", "9월 가스 요금 · 자동이체"), -GasBill);
            Tx(9, 29, "22:10", L("AliStar", "알리스타"), L("27-inch monitor", "27인치 모니터"), -307000);
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
            return p;
        }

        /// <summary>Adds a message to a chat of a built phone (for a variant's own evidence), putting the chat on top.</summary>
        static void AddChat(PhoneContent p, string chatId, int dom, string time, string sender, string avatar, string text)
        {
            var t = p.chats.Find(c => c.id == chatId);
            if (t == null || !Seen(10, dom, time))
                return;
            t.messages.Add(new ChatMessage { sender = sender, avatar = avatar, when = Oct(dom, time), text = text });
            p.chats.Remove(t);
            p.chats.Insert(0, t);
        }

        // ================================================================ what CheckFirst knows

        static WorldDirectory HouseholdDirectory()
        {
            var d = ScriptableObject.CreateInstance<WorldDirectory>();
            string personal = L("Personal account", "개인 계좌");
            d.accounts = new List<DirAccount>
            {
                new DirAccount
                {
                    bank = NuriBank, number = ProtectedAccount, holder = L("JEONG MIRAN", "정미란"), note = L("Personal account, opened 3 weeks ago", "개인 계좌 · 개설 3주"),
                    reports = new List<string>
                    {
                        L("4 Oct · \"Said he was from the Nuri Bank protection team. I sent 2,000,000 won.\"", "10월 4일 · \"누리은행 보호팀이라고 해서 200만 원 보냈어요.\""),
                        L("29 Sep · \"Protected account story. The money was gone in minutes.\"", "9월 29일 · \"보호계좌 얘기. 몇 분 만에 돈이 빠져나갔어요.\""),
                    },
                },
                new DirAccount
                {
                    bank = HanbitBank, number = GasScamAccount, holder = L("HAN SUNGMIN", "한성민"), note = L("Personal account, opened 5 days ago", "개인 계좌 · 개설 5일"),
                    reports = new List<string>
                    {
                        L("5 Oct · \"Paid a 'gas bill' into this account. The gas company knew nothing about it.\"", "10월 5일 · \"'가스 요금'이라고 해서 이 계좌로 보냈는데 가스 회사는 모르는 일이래요.\""),
                        L("4 Oct · \"'Collection account' of a fake billing team.\"", "10월 4일 · \"가짜 요금팀의 '수납 계좌'\""),
                    },
                },
                new DirAccount { bank = NuriBank, number = "110-302-558814", holder = L("KIM JIWOO", "김지우"), note = personal },
                new DirAccount { bank = NuriBank, number = "110-302-558990", holder = L("KIM JIWOO", "김지우"), note = L("Savings account", "적금 계좌") },
                new DirAccount { bank = NuriBank, number = "110-845-100233", holder = L("PARK HYEJIN", "박혜진"), note = personal },
                new DirAccount { bank = NuriBank, number = "110-771-209944", holder = L("CHOI YOUNGSIK", "최영식"), note = L("Personal account, opened 2011", "개인 계좌 · 2011년 개설") },
            };
            string mobile = L("Mobile phone", "휴대전화");
            d.numbers = new List<DirNumber>
            {
                new DirNumber
                {
                    number = "070-8844-2019", owner = L("Not registered", "미등록"), note = L("Internet phone (070), opened 9 days ago", "인터넷전화(070) · 개통 9일"),
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
                new DirNumber
                {
                    number = GasScamNumber, owner = L("Not registered", "미등록"), note = L("Prepaid phone, opened 3 days ago", "선불폰 · 개통 3일"),
                    reports = new List<string>
                    {
                        L("5 Oct · \"Said my gas would be cut off today. Wanted a transfer.\"", "10월 5일 · \"오늘 가스가 끊긴다며 이체하라고 함\""),
                        L("5 Oct · \"'Mapo City Gas billing team'. The real company never calls.\"", "10월 5일 · \"'마포도시가스 요금팀'. 진짜 회사는 전화 안 함\""),
                        L("4 Oct · \"Unpaid bill story, account in a man's name.\"", "10월 4일 · \"요금 미납 얘기, 계좌는 남자 이름\""),
                        L("3 Oct · \"Scam. Hung up.\"", "10월 3일 · \"사기. 끊었음\""),
                    },
                },
                new DirNumber { number = GasOfficialNumber, owner = L("Mapo City Gas customer centre", "마포도시가스 고객센터"), note = L("The number printed on the gas bill", "가스 고지서에 적힌 번호"), official = true },
                new DirNumber { number = "1599-0000", owner = L("Nuri Bank customer centre", "누리은행 고객센터"), note = L("The bank's only customer number", "은행의 유일한 고객 상담 번호"), official = true },
                new DirNumber { number = "1588-5520", owner = L("Hangang Express customer centre", "한강익스프레스 고객센터"), note = L("Customer centre and customs desk", "고객센터 · 통관팀"), official = true },
                new DirNumber { number = "1332", owner = L("Financial Supervisory Service fraud hotline", "금융감독원 불법금융 신고센터"), official = true },
                new DirNumber { number = "112", owner = L("Police", "경찰"), official = true },
                new DirNumber { number = "02-555-0192", owner = L("Mangwon Heights office", "망원하이츠 관리사무소"), official = true },
                new DirNumber { number = "010-5512-3380", owner = L("CHOI YOUNGSIK", "최영식"), note = L("Mobile phone, since 2009", "휴대전화 · 2009년 가입") },
                new DirNumber { number = Day2LegitNumber, owner = L("CHOI HYUNWOO", "최현우"), note = L("Mobile phone, since 2016", "휴대전화 · 2016년 가입") },
                new DirNumber { number = "010-2231-7745", owner = L("PARK HYEJIN", "박혜진"), note = mobile },
                new DirNumber { number = "010-4418-0902", owner = L("KIM DONGSU", "김동수"), note = mobile },
                new DirNumber { number = "010-6620-0921", owner = L("KIM JIWOO", "김지우"), note = mobile },
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
                caption = L("Before any transfer, the bank shows who really gets the money. Photo: Seoul Daily", "이체 전, 은행은 돈을 실제로 받는 사람을 보여 준다. 사진: 서울데일리"),
                ads = new List<NewsItem>
                {
                    new NewsItem { title = L("치킨 CHICKEN 24H", "24시 치킨"), text = L("Crispy & soy garlic. Free delivery over 20,000 won. 02-555-0147", "후라이드 & 간장마늘. 2만 원 이상 무료 배달. 02-555-0147") },
                    new NewsItem { title = L("CHECKFIRST.KR", "체크퍼스트"), text = L("Who owns that number? Whose account is it? Look it up before you send.", "그 번호의 주인은? 그 계좌의 예금주는? 보내기 전에 조회하세요. checkfirst.kr") },
                    new NewsItem { title = L("HANBIT ACADEMY", "한빛학원"), text = L("TOEIC weekend class, 30% off for students. 02-555-0166", "토익 주말반, 대학생 30% 할인. 02-555-0166") },
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
                        Field(L("Rent account", "월세 계좌"), "110-771-209944", FactKind.Account),
                        Field(L("Account holder", "예금주"), L("Choi Youngsik (Nuri Bank)", "최영식 (누리은행)")),
                        Field(L("If absent, contact", "부재 시 연락처"), L("Choi Hyunwoo (son)", "최현우 (아들)")),
                        Field(L("Son's phone", "아들 연락처"), Day2LegitNumber, FactKind.Phone),
                    },
                    body = L("Special terms\n1. The rent account changes only with a written notice from the landlord.\n2. The tenant allows the annual gas safety inspection.\n3. No smoking inside the unit.",
                             "특약사항\n1. 월세 계좌는 임대인의 서면 통지로만 변경한다.\n2. 임차인은 연 1회 가스 안전점검에 협조한다.\n3. 실내 흡연 금지."),
                },
                new DocumentData
                {
                    title = GasBillTitle, kind = DocumentKind.Bill, issuer = GasCompany, stamp = L("stamp_paid", "stamp_paid_ko"),
                    fields = new List<DocField>
                    {
                        Field(L("Customer", "고객"), L("Kim Jiwoo · Mangwon Heights 302", "김지우 · 망원하이츠 302호")),
                        Field(L("Amount", "금액"), L("18,420 won", "18,420원")),
                        Field(L("Paid", "납부"), L("28 Sep (auto-pay)", "9월 28일 (자동이체)")),
                        Field(L("Customer centre", "고객센터"), GasOfficialNumber, FactKind.Phone),
                    },
                    body = L("Safety inspection this month: Wednesday 14 October. Our inspectors carry an ID card and never ask for money.",
                             "이달 안전점검: 10월 14일(수). 점검원은 신분증을 지참하며 절대 돈을 요구하지 않습니다."),
                },
                new DocumentData
                {
                    title = SlipTitle, kind = DocumentKind.Stub, issuer = Courier,
                    fields = new List<DocField>
                    {
                        Field(L("Parcel", "품목"), L("Phone case (delivered 21 Sep)", "휴대폰 케이스 (9월 21일 배송 완료)")),
                        Field(L("Tracking", "운송장"), "HX-4410-2287-KR"),
                        Field(L("Customer centre", "고객센터"), "1588-5520", FactKind.Phone),
                    },
                    body = L("Hangang Express has one number for everything, deliveries and customs: 1588-5520.", "한강익스프레스의 배송·통관 문의 번호는 하나입니다: 1588-5520"),
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
            };

            r.calendar = new CalendarData
            {
                title = L("Happy Pharmacy calendar · October", "행복약국 달력 · 10월"),
                image = Tex("board_calendar"),
                todayLabel = L($"Today: {DateLabel(today)}", $"오늘: {DateLabel(today)}"),
                today = today.Day,
            };
            Note(r, 5, L("Substitute holiday", "대체공휴일"));
            Note(r, 7, L("Rent day! 450,000 won to Mr. Choi", "월세 날! 최 사장님께 45만 원"));
            Note(r, 8, L("Dentist, 18:30", "치과 18:30"));
            Note(r, 13, L("Midterms start", "중간고사 시작"));
            Note(r, 14, L("Gas safety check, 10:00-17:00", "가스 안전점검 10:00~17:00"));
            Note(r, 17, L("Mom's birthday: call + gift", "엄마 생신: 전화 + 선물"));
            return r;
        }

        /// <summary>Something Jiwoo wrote on the calendar, kept in date order.</summary>
        static void Note(RoomContent room, int dom, string text)
        {
            var at = new DateTime(2026, 10, dom);
            string label = L($"{at.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture)} {dom}", $"{dom}일 ({KoWeekday(at).Substring(0, 1)})");
            var entries = room.calendar.entries;
            int i = 0;
            while (i < entries.Count && entries[i].day <= dom)
                i++;
            entries.Insert(i, new CalendarEntry { day = dom, label = label, text = text });
        }
    }
}
