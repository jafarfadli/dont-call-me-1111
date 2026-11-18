using System.Collections.Generic;
using DontCallMe.Data;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// The trick cards: one per way the callers lie. Every false claim names its trick
    /// (<see cref="Via"/>); stamping it FALSE wins the card, which the player keeps across weeks and
    /// finds in Case Files. Several cases share a trick (the new account turns up in four), so
    /// the collection grows slower than the cases do.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string TrickBorrowedName = "borrowed_name";
        const string TrickBankCall = "bank_call";
        const string TrickBreakIn = "break_in";
        const string TrickSafeAccount = "safe_account";
        const string TrickPaidBill = "paid_bill";
        const string TrickNewAccount = "new_account";
        const string TrickStandIn = "stand_in";
        const string TrickEmergency = "emergency";
        const string TrickBorrowedPhone = "borrowed_phone";
        const string TrickExtraZero = "extra_zero";
        const string TrickHeldParcel = "held_parcel";
        const string TrickLeakedList = "leaked_list";
        const string TrickDeposit = "deposit";
        const string TrickBorrowedFace = "borrowed_face";
        const string TrickStranded = "stranded";

        /// <summary>The trick behind the claim when it is false (twins share the claim, so it is set in both truths).</summary>
        static ClaimDef Via(this ClaimDef claim, string trick)
        {
            claim.trick = trick;
            return claim;
        }

        static List<TrickDef> Tricks() => new List<TrickDef>
        {
            Trick(TrickBorrowedName, L("The borrowed name", "빌린 이름"),
                  L("The caller uses the name of a company you really deal with: your gas company, your courier, your newspaper, your clinic.",
                    "가스 회사, 택배사, 신문사, 병원처럼 실제로 거래하는 곳의 이름을 댄다."),
                  L("Look the number up, and compare it with the one on your own bill, slip or contacts.",
                    "번호를 조회하고, 고지서와 송장, 연락처에 적힌 번호와 비교하라.")),
            Trick(TrickBankCall, L("The call from your bank", "은행의 전화"),
                  L("Someone from your bank's \"security team\" rings about your money.",
                    "'은행 보안팀'이라며 내 돈 문제로 전화가 온다."),
                  L("Your bank calls from its own number. Hang up and ring the one in your contacts.",
                    "은행은 자기 대표번호로 전화한다. 끊고 연락처에 있는 번호로 직접 걸어라.")),
            Trick(TrickBreakIn, L("The break-in alarm", "침입 경보"),
                  L("Someone has got into your account from far away, and you must act right now.",
                    "누군가 먼 곳에서 내 계좌에 들어왔으니 지금 당장 움직여야 한다고 한다."),
                  L("Fear is the tool. Your own bank history shows whether anything happened.",
                    "겁이 수단이다. 정말 무슨 일이 있었는지는 내 은행 내역이 보여 준다.")),
            Trick(TrickSafeAccount, L("The safe account", "안전계좌"),
                  L("To keep your savings safe, they must go to a \"protected\" account for now.",
                    "예금을 지키려면 잠시 '보호계좌'로 옮겨야 한다고 한다."),
                  L("No bank and no police have such an account. Moving money to keep it safe means stop.",
                    "그런 계좌는 어느 은행에도, 경찰에도 없다. 지키려면 옮기라는 말이 나오면 멈춰라.")),
            Trick(TrickPaidBill, L("The bill you already paid", "이미 낸 요금"),
                  L("A bill is \"unpaid\", and something gets cut off today unless you pay now.",
                    "요금이 '미납'이라며 지금 내지 않으면 오늘 끊긴다고 한다."),
                  L("Your bank history and the bill itself know whether it was paid.",
                    "냈는지 안 냈는지는 은행 내역과 고지서가 안다.")),
            Trick(TrickNewAccount, L("The new account", "새 계좌"),
                  L("This time the money goes to a different account: a person's name, or a company nobody has heard of.",
                    "이번에는 다른 계좌로 보내라고 한다. 개인 이름이거나 처음 듣는 회사다."),
                  L("Look up who owns the account. Real companies change accounts in writing, not in a phone call.",
                    "계좌 주인을 조회하라. 진짜 회사는 계좌를 바꿀 때 전화가 아니라 글로 알린다.")),
            Trick(TrickStandIn, L("The stand-in", "대리인"),
                  L("Someone rings for a person you trust: their son, their assistant, the new manager.",
                    "믿는 사람 대신 전화했다고 한다. 아들, 비서, 새 담당자."),
                  L("Check the stand-in's number against your own papers, or ask the person themselves.",
                    "대리인의 번호를 내 서류와 비교하거나, 그 사람에게 직접 물어라.")),
            Trick(TrickEmergency, L("The sudden emergency", "갑작스러운 사고"),
                  L("A hospital, a broken phone, an accident: something happened this morning and money is needed now.",
                    "입원, 고장 난 폰, 사고. 오늘 아침 일이 생겨 지금 돈이 필요하다고 한다."),
                  L("Look for what they, or someone close to them, wrote today. People post, text and write in chats.",
                    "그 사람이나 가까운 사람이 오늘 남긴 말을 찾아라. 사람들은 글을 올리고 메시지를 보낸다.")),
            Trick(TrickBorrowedPhone, L("The borrowed phone", "빌린 폰"),
                  L("Family calls from a friend's phone, because theirs is broken or lost.",
                    "가족이 폰이 고장 나서 친구 폰으로 전화한다고 한다."),
                  L("Write to their own number or the family chat. If their phone answers, it isn't broken.",
                    "본인 번호나 가족 채팅방에 물어라. 본인 폰이 답하면 고장 난 게 아니다.")),
            Trick(TrickExtraZero, L("The extra zero", "잘못 보낸 돈"),
                  L("Someone sent you too much by mistake, and needs the difference back right away.",
                    "실수로 돈을 더 보냈으니 차액을 바로 돌려 달라고 한다."),
                  L("Look at your own bank history, not at their screenshot. Send back only what really arrived.",
                    "상대의 캡처가 아니라 내 은행 내역을 보라. 정말 들어온 돈만 돌려줘라.")),
            Trick(TrickHeldParcel, L("The held parcel", "보류된 택배"),
                  L("Your parcel is stuck at customs until you pay a fee.",
                    "택배가 세관에 묶여 있어 수수료를 내야 풀린다고 한다."),
                  L("The courier's and the shop's own messages say whether anything is due.",
                    "낼 돈이 있는지는 택배사와 쇼핑몰이 직접 보낸 메시지가 말해 준다.")),
            Trick(TrickLeakedList, L("The leaked list", "유출된 명단"),
                  L("The caller knows your name, your order or your appointment, so it must be real.",
                    "내 이름, 주문, 예약까지 알고 있으니 진짜라고 믿게 만든다."),
                  L("Lists leak. Knowing your details proves nothing: check what they say against your own notes.",
                    "명단은 유출된다. 내 정보를 안다는 건 증거가 아니다. 상대의 말을 내 메모와 맞춰 보라.")),
            Trick(TrickDeposit, L("The small deposit", "작은 예약금"),
                  L("A small payment now holds your place: an appointment, a booking, a delivery.",
                    "자리를 잡아 두려면 지금 소액을 내라고 한다. 진료 예약, 접수, 배송."),
                  L("Is there anything to hold? Check your own calendar first, and pay only where you booked.",
                    "잡아 둘 게 있기는 한가? 먼저 내 달력을 보고, 예약한 곳에만 직접 내라.")),
            Trick(TrickBorrowedFace, L("The borrowed face", "빌린 얼굴"),
                  L("A few seconds of someone's public videos are enough to put their face on a caller, live, on a video call.",
                    "공개된 영상 몇 초면 영상통화 속 상대에게 그 사람의 얼굴을 실시간으로 씌울 수 있다."),
                  L("Watch the lips against the voice, and ask for a slow wave in front of the face: a pasted face stays on top of the hand.",
                    "입 모양이 목소리와 맞는지 보고, 얼굴 앞에서 천천히 손을 흔들어 달라고 하라. 씌운 얼굴은 손 위에 그대로 떠 있다.")),
            Trick(TrickStranded, L("Stranded abroad", "해외에서 발 묶임"),
                  L("A friend is stuck abroad: bag stolen, cards gone, a hotel or a ticket to pay before a deadline.",
                    "친구가 해외에서 발이 묶였다고 한다. 가방을 도둑맞고 카드도 없어 마감 전에 호텔비나 표값을 내야 한다고."),
                  L("Travel makes the story easy to believe and hard to check. Look at what they posted from their own phone today.",
                    "여행 중이라는 말은 믿기 쉽고 확인하기 어렵다. 오늘 본인 폰으로 올린 글을 보라.")),
        };

        static TrickDef Trick(string id, string name, string how, string tell) => new TrickDef { id = id, name = name, how = how, tell = tell };
    }
}
