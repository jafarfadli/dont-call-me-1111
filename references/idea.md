# Problem Statement

Mobile-banking users in Indonesia and Vietnam, especially parents and older adults, need a safe way to experience a scam call's pressure before a real one arrives, because scammers win in the first few minutes by triggering fear and urgency that no warning poster has prepared them for.


# Don't Call Me!

Don't Call Me is a 3D detective game in the style of Papers, Please: every phone call is a case, some callers are real, and the player decides which by checking the caller's claims against evidence.
The phone rings, a caller claims to be the bank, a courier, the police or a relative, and the player investigates: read today's newspaper, ask the family, compare the caller's number with the one on the bank card, check the apps. Then they give a verdict. Next morning the newspaper shows what each verdict led to and adds new rules.

## How a case works

A case runs from the ring to a verdict, and the verdict's consequence shows up the next morning, as the next day's entry does in Papers, Please.

1. **Ring.** Caller ID shows a number, and a name if it is saved in the contacts.
2. **Claims.** The caller says who they are, which organisation, why they call, and what they want. Each claim is written into the case file on screen.
3. **Investigate.** On speakerphone, the player walks the room and checks each claim against evidence: the newspaper, an NPC, the card, the apps, the bills. The caller grows impatient while the player looks.
4. **Question.** The player can ask a verifying question. Legit callers answer and accept a call back. Scammers dodge and push harder.
5. **Flag a contradiction.** Drag a claim onto the evidence that contradicts it, for example the caller's number onto the hotline printed on the card. A mismatch gets a red stamp.
6. **Verdict.** Trust: do what they ask. Verify: hang up and call the official number, safe but limited to two per day. Reject: hang up and report.
7. **Consequence.** Trusting a scam costs money. Rejecting a legit call also costs something, such as a returned package or a real fraud alert ignored.

The limit on Verify keeps the game from being solved by hanging up on everyone. The player has to read the evidence.

## Evidence in the room

Every claim a caller makes can be checked against at least one of these. The newspaper and the NPCs change each day; the rest stays in place.

| Evidence | Where | What it can prove | Example |
| --- | --- | --- | --- |
| Today's newspaper | Coffee table | Current scam warnings and real announcements | A planned power outage notice makes the electricity call legit |
| Bank card | Wallet on the table | The bank's official hotline | Caller's number doesn't match the hotline |
| Contacts and call log | Phone | Whether the number is known or has called before | The "courier" calls from the same number as yesterday's real delivery |
| SMS inbox | Phone | OTP messages and their warnings, real delivery notices | The OTP message says the bank will never ask for it |
| Banking app | Phone | Real transactions and account notices | No blocked account, no suspicious transfer |
| Shopping app | Phone | Pending orders, courier name, cash-on-delivery amount | The order exists and the amount matches |
| Family group chat | Phone | Where relatives are right now | The "hospitalised" son posted from school ten minutes ago |
| Wall calendar | Wall | Appointments, deliveries, interviews | Clinic check-up is written in for tomorrow |
| Bills and receipts | Fridge, drawer | Paid bills, lucky-draw stubs | Last month's electricity bill is stamped paid |
| Official numbers magnet | Fridge | Ward police, electricity, clinic numbers | Number to call back for Verify |
| Family member (NPC) | Sofa | What the family ordered or planned, a second opinion | "I ordered that parcel, Ma" |
| Neighbour (NPC) | At the door | Local officials by name, scams going around the area | "Officer Hùng does call people, but he never asks for money" |

## The rulebook

The newspaper plays the role of the rulebook in Papers, Please: each day's paper adds one or two rules, so the cases get harder as the player learns. Every rule comes from a real warning.

| Day | Headline in the paper | Rule it teaches | Based on |
| --- | --- | --- | --- |
| 1 | "Bank: we never ask for your code" | Nobody legit asks for an OTP, PIN or password by phone | [VTV](https://vtv.vn/nang-cao-canh-giac-truoc-25-kich-ban-lua-dao-tren-khong-gian-mang-nam-2026-100260908163954854.htm) |
| 1 | "Unsure? Call the number on your card" | A legit caller is fine with you calling back on the official number | Game rule |
| 2 | "Police warn of fake officers" | Police don't settle cases or take transfers over the phone | [VTV](https://vtv.vn/nang-cao-canh-giac-truoc-25-kich-ban-lua-dao-tren-khong-gian-mang-nam-2026-100260908163954854.htm), [SGGP](https://en.sggp.org.vn/online-scams-inflict-losses-exceeding-vnd6-trillion-in-vietnam-in-2025-post123051.html) |
| 2 | "Grandmother kept it secret, lost her savings" | A caller who asks you to hide it from family is a red flag | [Vietnam.vn](https://www.vietnam.vn/en/bao-ve-nguoi-cao-tuoi-truoc-cac-chieu-lua-dao-tinh-vi-tren-khong-gian-mang) |
| 3 | "New SIM rule is for new numbers only" | Old numbers don't need re-registration | [Komdigi](https://www.komdigi.go.id/berita/siaran-pers/detail/lindungi-masyarakat-dari-penipuan-digital-registrasi-sim-biometrik-diberlakukan-penuh-mulai-1-juli-2026) |
| 3 | "Refund scam hits retirees" | A real refund never asks for a fee first | [VTV](https://vtv.vn/nang-cao-canh-giac-truoc-25-kich-ban-lua-dao-tren-khong-gian-mang-nam-2026-100260908163954854.htm) |
| 4 | "AI can copy your child's voice" | A familiar voice is not proof; ask something only family knows, or call them back | [VTV](https://vtv.vn/nang-cao-canh-giac-truoc-25-kich-ban-lua-dao-tren-khong-gian-mang-nam-2026-100260908163954854.htm) |
| 4 | "Victims report too late" | If you were fooled, report within minutes, not the next day | [Kontan](https://keuangan.kontan.co.id/news/ojk-85-korban-penipuan-melapor-ke-iasc-setelah-12-jam-sejak-kejadian) |

## Twin cases

Each caller type appears twice: once real, once a scam. The two versions sound alike on purpose, so the player has to find the one detail that differs. The scam versions follow reported scripts; the legit versions are our own design.

| Caller | Legit version | Scam version | The clue that separates them | Cost of rejecting the legit one |
| --- | --- | --- | --- | --- |
| Bank officer (ID) | Fraud team asks if you made a Rp450,000 purchase; the receipt is on the table. They ask only yes or no and tell you to call the card hotline | "Account blocked in 10 minutes"; asks for the OTP | Caller number vs the card hotline; the banking app shows the real alert or none | The real fraud on the card keeps going |
| Courier (ID) | Asks for directions to the house; the order is in the shopping app and the cash-on-delivery amount matches | Package "stuck"; sends a link to an .apk to reschedule | Shopping app shows the order or doesn't; file type of the link | Package is returned to the seller |
| Police (VN) | Ward officer asks you to come to the station to update residence papers, in person | Investigator says your ID is in a laundering case; move savings to a "safe account", tell no one | Neighbour knows the officer by name; the real one asks for a visit, never money | A fine for missing the residence update |
| Family member (both) | Son calls from a friend's phone because his battery died; answers the family question at once; asks for nothing | Son's cloned voice: accident, send hospital money now | Family group chat location; a question only he can answer | The son waits hours for a pick-up |
| Electricity company (VN) | Notice of a planned outage tomorrow morning for maintenance; asks for nothing | Power cut tonight unless an "overdue debt" is paid by transfer | Newspaper lists the planned outage; last bill on the fridge is stamped paid | None, but the player loses trust points for rejecting a harmless call |
| Clinic or insurance (VN) | Clinic reminds you of tomorrow's check-up, which is on the calendar | Insurance "refund" that needs a fee first | Calendar entry; the rule that a refund never needs a fee | Missed check-up |
| Mobile operator (ID) | Offers a data package; you can say no and nothing happens | Number blocked in 24 hours unless you re-register with your ID and an OTP | Day 3 rule: re-registration is only for new numbers | None |
| Job recruiter (ID) | HR invites you to an interview at their office; the application email is in the inbox | Paid tasks from home, deposit to unlock higher pay | Email in the inbox; a real employer doesn't charge a fee | Missed interview |
| Prize (VN) | Supermarket says your lucky-draw stub won; collect in store with your ID | You won a prize, pay the "tax" by transfer | Stub on the fridge; where you collect it and whether money goes out first | Lost prize |

## A sample day for the demo

Day 1 in Ibu Ratna's house in Bekasi: four calls, two legit and two scams, with only the Day 1 rules in the paper. Calls 2 and 3 are twins played back to back, which is the moment to show the judges.

| Call | Case | Truth | Right verdict | Evidence that proves it |
| --- | --- | --- | --- | --- |
| 1 | Courier asks for directions | Legit | Trust | The order is in the shopping app, the cash amount matches |
| 2 | "Bank security": account blocked, read the OTP | Scam | Reject and report | Caller number doesn't match the card hotline; the OTP SMS says never share it |
| 3 | "Bank fraud team": did you spend Rp450,000? | Legit | Trust (answer "no") or Verify | Number matches the card hotline; the receipt on the table is Rp45,000 from another shop, so the purchase isn't hers |
| 4 | Courier: package stuck, install this app | Scam | Reject | The shopping app shows the order was delivered in call 1; the number differs from call 1's; the link is an .apk |

Next morning's newspaper reports the result of each verdict, then adds the Day 2 rules.

## Sources

- [VTV: 25 scam scenarios to watch in 2026](https://vtv.vn/nang-cao-canh-giac-truoc-25-kich-ban-lua-dao-tren-khong-gian-mang-nam-2026-100260908163954854.htm) (8 Sep 2026, police warning)
- [SGGP: Vietnam's most common scams of 2025](https://en.sggp.org.vn/online-scams-inflict-losses-exceeding-vnd6-trillion-in-vietnam-in-2025-post123051.html)
- [Vietnam.vn: Protecting the elderly from online scams](https://www.vietnam.vn/en/bao-ve-nguoi-cao-tuoi-truoc-cac-chieu-lua-dao-tinh-vi-tren-khong-gian-mang)
- [Komdigi: Biometric SIM registration from 1 July 2026](https://www.komdigi.go.id/berita/siaran-pers/detail/lindungi-masyarakat-dari-penipuan-digital-registrasi-sim-biometrik-diberlakukan-penuh-mulai-1-juli-2026)
- [Kontan: 85% of victims report to IASC after 12 hours](https://keuangan.kontan.co.id/news/ojk-85-korban-penipuan-melapor-ke-iasc-setelah-12-jam-sejak-kejadian)
