# Don't Call Me! — Technical Plan

Game concept and research: `references/idea.md`. This document describes what we build and how, for the first playable prototype.

**Goal.** Young adults in Korea who live on their own (students, first jobs) handle their own banking and are frequent targets of institution-impersonation, job and rent scams. In the game they feel a scam call's pressure in a safe place and learn to check a caller's claims against evidence before a real call arrives. The player should leave able to name the rules and, more importantly, having practised the checks that expose a scam: whose name is on the account, which number is really official, what the family chat says right now.

> The setting and the audience moved from Indonesia and Vietnam to Korea on 2026-09-30. `references/idea.md` still describes the earlier version; its structure (twin cases, newspaper as rulebook, evidence in the room) carries over.

## 1. Decisions

| Topic | Decision |
| --- | --- |
| Engine | Unity 6000.4.11f1, URP (`PC_RPAsset`), Input System, **UI Toolkit** (C# builder templates + USS theme) |
| Scenes | Home (title screen), Room (the day), End (the next morning). The same generated bedroom appears in all three |
| Platform | PC (macOS + Windows), mouse and keyboard |
| Camera | First person. The player walks with WASD and turns the view by clicking and dragging; the cursor stays visible. While any 2D panel is open, walking and turning are locked |
| World | One room: Kim Jiwoo's bedroom in a twenty-year-old villa in Mangwon-dong, Mapo, Seoul (`references/art/ref5.jpg`). No NPCs in the room; family and neighbours reach the player through the phone |
| Interactions | Every interactable opens a 2D panel: newspaper, desk drawer, cork board, wallet, notebook. The phone holds nine apps |
| Phone | Carried (Tab raises and lowers it). An incoming call raises the phone by itself and can only be answered: slide to answer, no decline button, it rings until the player slides |
| Day structure | Seated at the desk → the phone rings → the caller introduces himself and makes the ask (**CASE OPENED**) → he holds the line while the player investigates against his deadline, with pressure building → the player acts → the next morning's paper reveals the truth |
| Conversations | Calls and chats. The transcript sits next to the phone. At key moments the player picks one of **two** responses, with a visible decision timer; while the caller holds, the player can put questions to him (each costs time) |
| Investigation | Callers never ask for a code or password. They ask for things legit callers also ask for (a transfer, a visit, a confirmation), so the verdict comes from checking evidence: the name on the account, the official number, the family chat, the paper trail |
| Verdict | Given only on the call, in the **verdict panel** under the transcript while the caller holds: go along (send the money, agree) or refuse (hang up). Each kind has a fixed colour (gold, red) so the colours never hint at the answer. The rest of the UI (room panels, phone apps) is for investigating: during a case the phone's hang-up button is off, the bank app can check a recipient but not send, and Call buttons can't call out |
| Voices | Callers are voiced with pre-generated English TTS (macOS `say` voices through a phone-line filter), one voice per character; the transcript reveals each line as it is spoken. The player's lines are text only; chats have no voice |
| Menus | Title screen (Start, Settings, Quit, and a ringing phone whose slide also starts the game); pause on Esc (Resume, Settings, Main menu); Settings: music, sound effects, voices, look sensitivity |
| Scenarios | A pool of scenarios in twin pairs (one legit, one scam, same ask), each tagged Easy, Medium or Hard by the number of clues needed. Built so far: Day 1 "the protected account" (always a scam), Day 2 "the new rent account" and Day 3 "the held parcel", each a scam or its legit twin at random (see 7.0) |
| Prototype run | Built: 3 days (Tue 6 – Thu 8 October), saved as the player goes (Continue on the title screen), ending with a week summary. Planned: 5 days, one random scenario per day: Easy, Medium, Medium, Hard, Hard. Each day's truth is revealed in the next morning's newspaper |
| Continuity | A day remembers the earlier ones without changing its case: the money that left stays gone, yesterday's call is in the log, yesterday's case is on the desk newspaper, the rules pile up in the notebook, and **echoes** (texts, chats, notes on the board, a line on the day card) react to how each day went |
| Content data | ScriptableObjects, validated by an editor tool |
| Language | English UI and dialogue; Korean names, places and won amounts. Banks, couriers, shops, companies, people and their numbers are fictional; the public numbers 112 (police), 119 (fire and ambulance) and 1332 (financial fraud hotline) are real |

## 2. A day in the game

```
Title screen (Home scene) → Start
  → DAY card over black: "DAY 1 · Tuesday, 6 October · 16:20 · Mangwon-dong, Seoul" and a line of intro
  → Jiwoo sits at the desk (the view is lower and turns within a range; no walking yet)
  → A few seconds later the phone jumps up and rings. Slide to answer (no decline)
  → Opening: the caller introduces himself and makes the ask (voiced, transcript beside the phone);
    usually one two-choice decision on the way
  → CASE OPENED card: who he says he is, what he wants, his deadline, the player's job
  → Investigation: Jiwoo stands up. The caller holds the line until his deadline (in-game clock).
    Room panels and the phone apps are free to use; the player can put questions to him (each costs time).
    Pressure builds: he pushes at set times, texts arrive, the music tightens, the screen edges redden,
    a ticking clock in the last minutes
  → The player gives the verdict on the call: send the money (go along) or hang up (refuse).
    If the deadline passes first, the caller hangs up (timeout)
  → "Call ended", fade to black
  → The next morning (End scene): the Seoul Daily reveals who really called, beside the case summary
    (verdict stamp, money, time taken, clues found and where the missed ones were, the rule learned)
  → The next day, the same day again, or the main menu. After the last day: the week's summary
```

- A ringing call cannot be declined or ignored: the phone stays up and rings until the player slides. Chats can wait, but their decision timer starts once the thread is opened.
- The game clock runs at 8 real seconds per minute and stops on the DAY and CASE OPENED cards and while paused. Day 1's caller wants the money before 17:00, about 4½ real minutes of investigation.
- Day 1 adds short prompts for the controls (look, pause, investigate).

## 3. The room

3.6 × 3.8 m, 2.4 m high: the bedroom of a villa in a humid part of Seoul that was never renovated (see `Tools/ArtGen/README.md` for how it is built). Old vinyl floor, damp-stained painted walls, cherry-brown door and mouldings, a PVC window over the bed, a wall air conditioner and the ondol thermostat by the door. Late-afternoon sun comes in through the window.

| Interactable | Where | 2D panel | Evidence it holds |
| --- | --- | --- | --- |
| Newspaper (`INT_Newspaper`) | Desk, folded | Full-page *Seoul Daily*, changes daily | Yesterday's reveal, warning column, local news (police busts, inspection dates, customs rules), ads (some are red herrings) |
| Wallet (`INT_Wallet`) | Desk | Wallet open, cards flip | Nuri Bank check card (front: name and last digits; back: customer centre 1599-0000), student ID (Hanbit University), café stamp card, a detective's business card on the day it matters |
| Desk drawer (`INT_Drawer`) | Desk, top drawer | Documents as paper cards, flick through | Lease contract (landlord, rent account, "account changes only in writing"), Mapo City Gas bill (official number), phone bill, receipts, scholarship letter, parcel stubs |
| Cork board (`INT_BulletinBoard`) | Over the desk | Board close-up, items open on click | Pharmacy calendar with appointments, important numbers, building management notices, sticky notes, concert ticket, photo strip |
| Notebook (`INT_Rulebook`) | Desk, next to the newspaper | Notebook with two tabs | **Rules**: every rule read in the papers so far. **Case**: today's caller, their claims and every number, account and name heard or found, as chips to copy |
| Phone | Carried (Tab) | Phone + transcript | See section 4. Its charger lies on the bed |

In the model each interactable is a group named `INT_*` with its own box collider; parts inside a group are named without the prefix.

## 4. The phone

Raised with **Tab** (or the phone button in the corner), shown on the right of the screen with the transcript to its left during a conversation. While the phone is up, the player cannot walk.

The home screen shows nine apps in a 3 × 3 grid, a status bar (clock, signal, battery) and a home bar. **Back** (arrow, Esc or right-click) goes one screen back; **Home** returns to the grid.

| App | What it shows | What the player can do there |
| --- | --- | --- |
| Phone | Recents (incoming, outgoing, missed; number or name, time, duration) and a keypad | Dial any number. Official numbers answer with that day's scripted line; other numbers ring out or reach the scammer again |
| Contacts | Names, numbers, memos ("Landlord Choi — 1F", "Taeho — Minjun's friend") | Call or message a contact |
| Messages | SMS threads: bank and delivery notices, notices from offices, spam, scam texts with links | Tap a link: it opens in Browser with its full address visible |
| Talk | Messenger chats: Family, Mangwon Heights residents, Yuna, café crew, and unknown profiles (with the "not in your friends list" banner) | Read, reply in chat scenarios, open a profile (ID, since when) |
| Nuri Bank | Accounts and balances, transactions (date, counterparty, memo, amount), notifications (logins, held payments) | **Transfer**: pick a bank, enter or paste an account and an amount; the app shows the **account holder's name** before the player confirms |
| CheckFirst | A fraud-report lookup (fictional, modelled on public lookup services) | Search a phone or account number: report count, recent report snippets, and the reminder that *no reports does not mean safe* |
| Browser | Search results and pages: official sites with their numbers and notices, lookup forms (case lookup, customs tracking, business registry), news, and the fake sites behind scam links | Search by typing or by tapping suggestion chips taken from the current case |
| Parcels | Orders with status, tracking number, courier, driver and phone, delivery window | Track a number: unknown numbers return "not found" |
| Mail | Inbox: university, job applications, receipts, account notices | Read; attachments show as named files |

**Copy chips.** Every number, account, name, case number or web address a caller says or a document shows appears as a chip. Clicking a chip copies it; every input field has a **Paste** button. The player never has to type a 14-digit account number.

### 4.1 Calls

1. **Incoming.** The phone raises itself, rings and shakes. The screen shows the caller's pixel portrait (a silhouette for unknown numbers), the name if saved in contacts, otherwise "Unknown" and the number, and one control: **slide to answer**. There is no decline button and Tab does nothing until the player answers.
2. **In call.** The call screen (name or number, call timer, hang-up button) sits on the phone. The **transcript panel** sits to its left: the caller's lines with their portrait and time, the player's lines on the right. Lines type in; a "…" bubble shows while the caller is speaking. When a decision comes up, two response buttons and the **decision timer** appear at the bottom of the transcript.
3. **Investigating during a call.** The player can open any app (a green "Return to call · 01:42" bar stays on top) or lower the phone and walk to the drawer or the board. With the phone down, a compact call HUD stays at the top of the screen: caller, the latest line as a subtitle, the decision timer and "Decision waiting — Tab".
4. **Hanging up** is a verdict (refuse), and so is completing the transfer the caller asked for (go along). During a case call both are given in the verdict panel (5.3), not on the phone.

### 4.2 Chats

A notification sound and a toast ("Talk · 엄마: Jiwoo-ya, are you home?") announce a chat scenario; the badge on the phone button lights up. The thread opens in Talk (or Messages for SMS). Messages arrive with a typing indicator; decisions appear as two reply chips with the same decision timer. The player can leave the thread, check evidence and come back.

## 5. Conversations

### 5.1 Structure

A conversation is a small graph of nodes. Each node plays a few lines (caller, player or system), then either continues to the next node or stops at a **decision** with exactly two options. An option says the player's line, then leads to another node or to an **ending**.

A line can deliver evidence as it is spoken (a text arrives, a deposit lands in the bank app) and **facts** (an account number, a case number, a name), which become copy chips in the transcript and the notebook's Case tab.

Decisions come in three kinds, and a good scenario uses at least two:

- **Ask**: a question that makes the caller commit to a checkable fact ("What's the case number?", "Which account, and in whose name?"). Legit callers answer plainly; scammers answer too, but their facts fail the check.
- **Test**: a request only the real party can meet ("I'll call the number on my card and ask for you", "What did Grandma make at Chuseok?"). Legit callers agree or answer; scammers push back or dodge.
- **Commit**: the verdict (go along or refuse).

Besides the decisions, the **verdict panel** ends a call conversation, and each scenario maps its choices to endings through its actions:

| Verdict | Ending |
| --- | --- |
| Send the money (the Transfer action's account, bank and amount; the confirmation shows the holder's name, like the bank's own check) | Go along |
| Hang up | Refuse |

Each verdict is said aloud first (the player's line, e.g. "I'm not sending anything. Goodbye."). While a case call is on, the phone can't call out: the player checks numbers (card, board, CheckFirst, the official site) but doesn't ring them. Chats keep the phone as the way to act: a transfer in the bank app, or a call to a number the player looked up (a Call action), which verifies.

### 5.2 Example: M6 "The protected account" (Day 1, scam)

```
[Caller 070-8844-2019, voice Daniel]
  "Hello, is this Kim Jiwoo? This is Manager Jeon from Nuri Bank's account protection team."
  "Twenty minutes ago, someone logged into your account from an unregistered device in Busan."
DECISION (Ask, 45 s)
  A: "Busan? I've been home all day."           → "A fraud ring is emptying accounts in Mapo today..."
  B: "How do I know you're really from Nuri Bank?" → "I have your account ending in 8814 on my screen..."
                                                     "...we call from our direct line."   fact: 070-8844-2019
HOLD
  "To keep your savings safe, we'll move them to a protected account until the security reset at five."
  "Please transfer 1,200,000 won to the protected account: Nuri Bank 110-900-551207."
  "Stay on the line and don't tell anyone..."   "I'll hold while you open your bank app."
CASE OPENED   deadline 17:00 ("before the security reset")
Questions     why can't I call the bank? · whose name is on the account? (Jeong Miran, "an officer")
              · your employee number? (red herring) · "I'd like to check with the bank first" ("I AM Nuri Bank")
Beats         16:34 "Are you still there?" · 16:40 "I've sent the case number" + text with nuri-secure.kr
              · 16:47 "They tried to log in again" · 16:53 "Seven minutes" · 16:57 "Three minutes!"
Actions       transfer to 110-900-551207 → go along (−₩1,200,000)
              hang up → refuse · 17:00 → timeout
Clues (7)     the recipient check shows JEONG MIRAN · 070-8844-2019 has seven reports · the official page says
              there is no protected account · the card back and the board give 1599-0000 · the bank's alerts
              show no Busan sign-in · the texted link is nuri-secure.kr · today's paper warns about it
Red herrings  "account ending 8814" (true: leaked data) · the employee number · Monday's real "new device" alert
```

### 5.3 The hold: questions, beats and the deadline

When a node is marked **holds**, the caller has made the ask and waits on the line. The CASE OPENED card appears (`caseInfo`: title, claimed identity, the ask, the deadline and the player's job) and the investigation starts.

- **Deadline**: an in-game time (`caseInfo.deadline`, e.g. 17:00). A widget under the clock counts down (amber at half, red and pulsing in the last quarter); at 0 the caller gives up and the timeout ending applies.
- **Questions** (`questions`): shown in the transcript while he holds (keys 1–4). Each can be asked once; the answer is voiced and may deliver facts. An answer can also end the call (`endingId`). Answers are written so they sound reasonable but give checkable facts away: the "protected account" is in an officer's *name*, the call centre "can't see protection cases".
- **Pressure beats** (`beats`): lines he says at set in-game times, which can deliver evidence (the "case number" text with a fake link). Beats already past when the hold starts are skipped unless they deliver something.
- Screen-edge pressure, the tension layer of the music and a ticking clock follow the time left.
- The phone can go down at any time (Tab, or a press on the room): the call HUD at the top shows his latest line and "Holding the line · Tab to ask or decide".
- **Verdict panel**: under the questions, a dark strip with YOUR VERDICT and two colour-coded buttons (gold "Send the money · ₩1,200,000 to Nuri Bank 110-900-551207", red "Hang up"). Each asks for a second click: the transfer with the recipient's name, or hanging up. The caller keeps talking over an open confirmation without closing it.

### 5.4 Patience and pressure

- Patience only drains while a decision is waiting, never while the caller is talking.
- Every decision node has its own patience time and pressure lines at thresholds (for example, 66% and 33%). The **decision timer** shows it as a ring with seconds; it turns amber at 50%, red and pulsing at 25%.
- As patience drops, a pressure overlay darkens the screen edges, desaturates slightly and plays a heartbeat. It works whether the phone is up or down.
- At 0 the caller gives up: the scenario's **timeout ending** applies.

### 5.5 Endings and verdicts

Every ending has a verdict: **Go along** (did what the caller asked), **Refuse** (said no or hung up) or **Verify** (called a number the player looked up; chats only, since a case call's verdict panel offers go along or refuse). Correctness follows from the truth; nobody authors it by hand:

| | Go along | Verify | Refuse | Timeout |
| --- | --- | --- | --- | --- |
| Scam | Wrong | Right | Right | Safe, but not reported (partial) |
| Legit | Right | Right, with the scenario's delay cost | Wrong | Wrong |

Verify is only reached by calling a number the player found themselves; calling the number the caller offered verifies nothing. An ending also carries its consequence: a won amount, a line for the next morning's newspaper and any evidence it leaves behind (a transfer in the bank app, a frozen-account notice, a missed parcel).

## 6. Scenarios

### 6.1 Difficulty and design rules

A **clue** is a piece of evidence the player needs to see to reach the right verdict.

| | Easy | Medium | Hard |
| --- | --- | --- | --- |
| Clues needed | 2 | 3 | 4 |
| Where the clues are | One or two sources, one of them on the phone | Room and phone, one cross-check (a number on a bill vs the caller ID) | Room and phone, two cross-checks, one fact the player must ask the caller for |
| Red herrings | None | 1 | 2 (one that looks like a clean bill of health, one that makes the scam plausible) |
| Decisions | 1–2 | 2–3 | 3 |
| Patience per decision | 90 s | 60 s | 40 s, stronger pressure lines |
| Caller | Plain script | Plausible, knows one personal detail | Knows your name, school or last purchase; legit callers sound alarming |
| Hint in the newspaper beforehand | Yes, the matching rule | Yes, the matching rule | No (the warning column is about something else) |

Rules for writing scenarios:

- No scenario is solved by a rule alone. Every scam asks for something its legit twin also asks for, and every twin pair shares the ask (a transfer, a visit, a confirmation).
- Nobody asks for a verification code, PIN or password. Money moves by transfer to an account the player can inspect, by forwarding a deposit, or through a link.
- Every clue exists before the call or arrives during it, in a place the player can reach within one decision timer.
- Every Medium and Hard scenario has a red herring, so a single check can mislead: a clean CheckFirst result, a caller who knows personal data, a real notification that primes suspicion.

### 6.2 Pool

Built: M6 (Day 1). Day 2 reworks M5 ⇄ H3 so both share the caller ("Choi Hyunwoo, the landlord's son"), the story and the ask (the rent on a new account). Day 3 pairs M2 with a new legit twin, the same "customs desk" call about a real overseas order (see 7.0).

Twins are marked with ⇄. **Bold** = first batch (12 scenarios; the run needs 11).

| ID | Difficulty | Channel | Truth | Category | Scenario | Ask |
| --- | --- | --- | --- | --- | --- | --- |
| **E1** | Easy | Call | Legit | Utility | Mapo City Gas schedules the safety inspection (⇄ M3) | Be home on the 14th or leave the boiler cupboard open |
| **E2** | Easy | Chat | Scam | Family | "Mom" on a new Talk profile: phone broke, send money to the repair shop (⇄ M4) | Transfer ₩200,000 |
| **M1** | Medium | Call | Legit | Courier | Hangang Express driver can't get in: the door code changed (⇄ M2) | Come down or say where to leave it |
| **M2** | Medium | SMS + call | Scam | Courier | "Customs": overseas parcel held for unpaid duty (⇄ M1) | Transfer ₩38,000 or pay through the link |
| **M3** | Medium | Call | Scam | Utility | "Gas safety centre": boiler leak found, pay the part in advance (⇄ E1) | Transfer ₩89,000 to the technician |
| **M4** | Medium | Chat | Legit | Family | Brother Minjun on his friend's Talk: phone dead, wallet lost at Seoul Station (⇄ E2) | Transfer ₩27,600 for the train |
| **M5** | Medium | Call | Legit | Housing | Landlord Choi: pay rent to his son's account from now on (⇄ H3) | Transfer ₩450,000 rent |
| **M6** | Medium | Call | Scam | Bank | "Nuri Bank protection team": move your money to a protected account (⇄ H1) | Transfer ₩1,200,000 |
| **H1** | Hard | Call | Legit | Bank | Nuri Bank fraud centre: a ₩890,000 card payment is on hold, was it you? (⇄ M6) | Confirm yes or no |
| **H2** | Hard | Call + SMS | Scam | Authority | "Investigator Kang", prosecutors' office: your name is in a laundering case, move savings for inspection (⇄ H4) | Transfer ₩3,000,000, tell no one |
| **H3** | Hard | SMS + call | Scam | Housing | "Landlord's son": father in hospital, rent goes to my account today (⇄ M5) | Transfer ₩450,000 now |
| **H4** | Hard | Call | Legit | Authority | Detective Oh, Mapo Police: your number was on a busted ring's list, come and give a statement (⇄ H2) | Agree to visit, say whether you lost money |
| H5 | Hard | Call | Scam | Job | Remote "accounting assistant" job: forward client deposits minus commission (⇄ M7) | Forward ₩970,000 of a deposit that just landed |
| M7 | Medium | Call | Legit | Job | Daeyang Logistics HR invites you to an internship interview (⇄ H5) | Confirm the time, bring your student ID |

### 6.3 Evidence per scenario

For each scenario: the clues the verdict needs (✓) and the red herrings (✗), with the place that shows them. "Verify" is what the official number says when the player calls it.

**E1 · Gas inspection (legit).** Caller 02-555-0181, Mapo City Gas safety team.
- ✓ Board: building notice, gas inspection Wed 14 Oct 10:00–17:00, "inspectors never ask for money".
- ✓ Drawer: gas bill, Mapo City Gas customer centre 02-555-0181 (= caller ID). Calendar: "Gas check" on the 14th.
- Refusing: the inspection is missed; the paper reports unchecked boilers in old villas.

**E2 · "Mom's broken phone" (scam).** Talk profile "엄마 ♥" with a new ID, "not in your friends list" banner.
- ✓ Talk: Family chat, Mom posted a photo of dinner 8 minutes ago from her usual profile.
- ✓ Bank transfer screen: Daehan Bank 3333-12-7788901 belongs to YOON JAEHEE, not Mom or a shop.
- Verify: calling Mom's saved number → "My phone is fine! Don't send anything."

**M1 · Courier at the door (legit).** Caller 010-7715-5521.
- ✓ Parcels: order "Desk lamp bulbs", out for delivery, driver Kim Taesik 010-7715-5521 (= caller ID).
- ✓ Messages: Hangang Express SMS this morning, "arriving today 16:00–18:00".
- ✓ Talk: residents' chat, landlord posted yesterday that the front door code changed.
- ✗ Messages: a spam SMS from last week, "parcel address incomplete, update here".
- Refusing: the parcel goes back to the depot.

**M2 · Customs duty (scam).** SMS "[KR Customs] parcel KR44-5270-1183 held, duty ₩38,000, pay by 18:00: unitrack-kr.help/pay", then a call from 070-4852-1170.
- ✓ Parcels: no overseas order; tracking KR4452701183 returns "not found".
- ✓ Browser: the official customs site (unitrack.customs.go.kr) says duties are never collected by phone or to personal accounts; the SMS domain is not the official one.
- ✓ Bank transfer screen: Mirae Savings 012-44-190288 belongs to HAN SUNGMIN; CheckFirst: 4 reports on 070-4852-1170.
- ✗ Mail: an AliStar receipt from September (Jiwoo does order from abroad; that parcel was delivered).

**M3 · Boiler "emergency" (scam).** Caller 02-555-0188, "Mapo City Gas safety centre".
- ✓ Board: the notice dates the inspection to the 14th (today is earlier) and says inspectors never ask for money.
- ✓ Drawer: the gas bill's number is 02-555-0181, not 0188.
- ✓ Bank transfer screen: Hanbit Bank 620-204-118830 belongs to KWON TAEHO, a person.
- ✗ Talk: a neighbour asks the residents' chat if anyone smells gas (they burnt dinner).

**M4 · Minjun on a friend's phone (legit).** Talk message from Park Taeho's profile.
- ✓ Talk: Family chat an hour ago, Minjun: "phone at 3%, going up to Seoul for the concert".
- ✓ Contacts: "Taeho (Minjun's friend)"; transfer screen: Nuri 110-287-004411 belongs to PARK TAEHO.
- ✓ Test decision: asked what Grandma made at Chuseok, he answers "sesame songpyeon, like always" (the Chuseok photo on the board and in the family chat).
- ✗ The rule from E2's paper ("family asking for money on another account? call first"): calling Minjun's number fails, his phone is dead.
- Refusing: Minjun misses the last train home.

**M5 · New rent account (legit).** Caller 010-5512-3380, saved as "Landlord Choi".
- ✓ Contacts and the numbers note on the board: the caller ID is the landlord's number.
- ✓ Talk: residents' chat, this morning the landlord posted the new account for everyone.
- ✓ Bank transfer screen: Nuri 110-771-202358 belongs to CHOI HYUNWOO, the son named in the lease's contact line.
- ✗ Messages: last week's phishing SMS "[Landlord notice] rent account changed" from an unknown number.
- Refusing: rent is late; the landlord knocks the next day.

**M6 · "Protected account" (scam).** See 5.2.

**H1 · Held card payment (legit).** Caller 1599-0000, Nuri Bank fraud centre, agent Song Eunji. She reads the card's last digits (0921) and asks only whether the payment was Jiwoo's.
- ✓ Wallet: the card's customer centre is 1599-0000 (= caller ID) and it ends in 0921.
- ✓ Nuri Bank: notification two minutes before the call, "Payment held: ₩890,000, Gangnam Digital Plaza".
- ✓ Drawer: today's receipts (Daily 24 ₩5,850, café ₩4,500); Mail: no order from Gangnam Digital Plaza.
- ✓ Test decision: she agrees to a call back on the card's number.
- ✗ Mail: "Your cart is waiting" from Gangnam Digital Plaza (Jiwoo looked at a laptop last week).
- ✗ Messages: last week's fake "Nuri account protection" SMS, which primes the player to distrust bank calls.
- Refusing: the payment goes through overnight.

**H2 · "Investigator Kang" (scam).** Caller 02-555-3144, then an SMS link to "the case file" at spo-case.kr.
- ✓ Browser: the prosecutors' official site lists 02-555-3114 (one digit apart) and says it never asks for transfers or "safe accounts".
- ✓ Browser: the official case lookup finds no case 2026-형제-48213 (the number comes from an Ask decision).
- ✓ Nuri Bank: Jiwoo has no Mirae Savings account (the "account opened in your name"); the SMS domain is not the official one.
- ✓ Bank transfer screen: Daehan Bank 3333-08-551920 belongs to LIM DOHYUN, a person.
- ✗ CheckFirst: 0 reports on that account (it is new).
- ✗ The caller knows Jiwoo's birthday and student number.
- Verify: calling 02-555-3114 → "There is no Investigator Kang in that division. Please report it to 112."

**H3 · "Landlord's son" (scam).** SMS from 010-4127-8830, then a call from the same number.
- ✓ Contacts: "Hyunwoo (landlord's son)" is saved with 010-2280-6614.
- ✓ Talk: the landlord posted in the residents' chat two hours ago about the water tank cleaning (he is not in hospital).
- ✓ Drawer: lease: rent is due on the 25th and "the account changes only in writing from the landlord".
- ✓ Verify: the landlord's real number answers, "I'm fine, and my son didn't send that."
- ✗ Bank transfer screen: Hanbit Bank 620-118-449027 does belong to CHOI HYUNWOO (a stolen name).
- ✗ Talk: the real son posted in the residents' chat last month, so he exists and helps with the building.

**H4 · Detective Oh (legit).** Caller 02-555-0112, Mapo Police Station anti-phishing team.
- ✓ Browser: Mapo Police Station's official number is 02-555-0112 (= caller ID).
- ✓ Newspaper: today's local news, "Mapo police arrest voice-phishing ring".
- ✓ Phone: a missed call on 2 Oct from 070-4852-1170; CheckFirst: that number has reports.
- ✓ Test decision: she invites a call back to the station's main number and asks for no money or account details.
- ✗ Newspaper: the warning column is about fake police calls.
- ✗ She asks to confirm Jiwoo's date of birth for the statement form.
- Refusing: the police file the case without her statement.

**H5 · Money-mule job (scam).** Caller 010-3309-2214, "Manager Lee, Daeho Trading". During the call, ₩1,000,000 lands in Jiwoo's account from PARK SOONJA.
- ✓ Browser: the business registry lists Daeho Trading as closed in 2025; the JobBridge posting has been removed.
- ✓ Nuri Bank: the deposit's memo reads "safe account", a victim's money.
- ✓ CheckFirst: 2 reports on Mirae Savings 012-66-118420, "money mule recruiter".
- ✓ Ask decision: he can't explain why clients don't pay the company directly.
- ✗ Mail: Jiwoo did apply on JobBridge last week.
- ✗ The deposit is real money, in her own bank app.
- Going along: the account is frozen as a mule account.

**M7 · Internship interview (legit).** Caller 02-555-0240, Daeyang Logistics HR.
- ✓ Mail: application confirmation from Daeyang Logistics; the interview invitation arrives during the call.
- ✓ Browser: the company's official number is 02-555-0240.
- ✓ Calendar: Thursday the 15th is free.
- ✗ She asks for a date of birth for the visitor pass.

### 6.4 The household and what is always there

- **Kim Jiwoo** (22), third year at Hanbit University, barista at Mangwon Roasters on weekends, lives alone in Mangwon Heights 302.
- **Family**: Mom (Park Hyejin) and Dad (Kim Dongsu) in Suwon, brother **Minjun** (19, first year in Daejeon), Grandma in Jeonju.
- **Building**: landlord **Choi Youngsik** (1F) and his son **Choi Hyunwoo**; the residents' chat.
- **Friends**: Seo Yuna (concert), café manager Han.
- **Baseline evidence**: contacts, the family and residents' chats with a week of history, Nuri Bank checking account 110-302-558814 (about ₩1.28 million) and tuition savings (₩3,000,000), a month of transactions, the lease, bills and receipts, the board, the card.

## 7. The run

### 7.0 The run as built: 3 days

| Day | Date | Case | Truth | Difficulty |
| --- | --- | --- | --- | --- |
| 1 | Tue 6 Oct | The protected account (M6): "Manager Jeon" of Nuri Bank wants ₩1,200,000 moved | Scam (the tutorial) | Medium |
| 2 | Wed 7 Oct, rent day | The new rent account: "Choi Hyunwoo, the landlord's son", father in hospital, rent (₩450,000) to his account today | Scam or legit | Medium |
| 3 | Thu 8 Oct | The held parcel: "Yoon Seora, Hangang Express customs desk", the AliStar monitor is held for ₩56,600 duty and VAT | Scam or legit | Hard |

**Picking the truth.** `DayData.variants` holds one `DayVariant` per truth (call, phone, room, directory, clues, papers, rule). `DaySetup.Pick` rolls one when the day starts, with one rule: a run always has a legit caller, so on the last day that has a legit twin, if every earlier day was a scam, it is legit. The twins share the caller, the voice, the opening and the ask; only the evidence and a few answers differ:

| | Scam | Legit |
| --- | --- | --- |
| Day 2 caller | 010-4127-8830 ("my work phone") | 010-2280-6614, the son's number in the lease |
| Day 2 account | Hanbit 620-118-449027, SEO JIYEON ("my wife's") | Nuri 110-771-202358, CHOI HYUNWOO |
| Day 2 evidence | The landlord posts in the residents' chat from home; no notice; CheckFirst: 2 reports | The landlord posts that he's in hospital and gives the account; his written note is pinned on the board |
| Day 3 caller | 1588-5502 (one digit off) | 1588-5520, on the courier's site and the delivery slip |
| Day 3 account | Hanbit 620-557-301144, KR CUSTOMS CLEARANCE | Nuri 5620-44-018830, HANGANG EXPRESS (CUSTOMS) |
| Day 3 evidence | Parcels: cleared, taxes paid; AliStar email: taxes included; the payment text is a web-sent text from 1588-5502 | Parcels: held, same duty and account; email: taxes not included; the payment text comes from Hangang Express |

Red herrings sit in both variants: last week's "rent account changed" smishing text, Monday's "unpaid customs duty" text, the AliStar data leak in Thursday's paper (it explains how a scammer knows the order, and makes the real call look suspicious).

**Evidence per day** (`DaySetup.Compose`, on top of `ContentBuilder.Household`, one timeline of Jiwoo's week that each day shows as it stands at its start time):

1. The variant's own phone, room and directory (cloned, so play never edits the assets).
2. History from the saved earlier days: money that left on a call (the savings cover what the everyday account can't), yesterday's call in the log, yesterday's front page on the desk newspaper (the 2D panel and the printed prop), every rule learned so far in the notebook.
3. Echoes (`DayData.echoes`): each reacts to one earlier day, its truth and its outcome. They never hold a clue for today's case. Some pop up as notifications while Jiwoo sits at the desk.

| After | Echoes |
| --- | --- |
| Day 1, sent the money | Mom in the family chat, the bank's fraud report, a police SMS, a sticky note "READ THE NAME" (stays all week), the day card line |
| Day 1, hung up | Yuna and the residents' chat about the call, a sticky note with the number |
| Day 1, ran out of time | Two missed calls from the scammer, another smishing text, a sticky note "decide" |
| Day 2 scam | The landlord's warning in the residents' chat; the real rent paid (again, if the fake son got it); his text if Jiwoo lost the money |
| Day 2 legit | Hyunwoo saved in contacts; his thanks, or his polite reminder and the rent paid a day late; the landlord posting from hospital |

**Saving.** `GameRun` keeps a `DayRecord` per finished day in PlayerPrefs (`DCM.Run`): variant, outcome, money, clues, times, the caller and any transfer. The title screen offers Continue (the first unplayed day) and New week; the morning after offers the next day, the same day again (later days are dropped when it ends) or the menu. After Day 3, **Your week** lists each day's truth, what the player did, right or wrong and clues found, the savings before and after, a rating (Scam-proof, Careful, At risk) and the rules learned.

**Desk newspaper.** The folded paper on the desk is a printed texture. `Content → Print Desk Newspapers` exports every front page that lies on the desk the next day to `Tools/ArtGen/papers.json`, `tex_prints.py papers` prints them in the Day 1 paper's style to `Art/Textures/Papers`, and each `EndPaper.print` points at its print; `DayDirector` puts it on the prop. The board's new notes are blank sticky notes and a notebook page with the text written over them in the panel (`BoardItem.handwriting`).

### 7.1 Picking scenarios (planned 5-day run)

`RunBuilder` fills the slots `[Easy, Medium, Medium, Hard, Hard]` from the pool with a seeded random generator:

- No scenario twice in a run.
- At least 1 legit and at least 2 scams, so refusing everything never wins.
- The same category (Utility, Family, Courier, Housing, Bank, Authority, Job) never appears on two days in a row.
- The seed appears on the Summary and can be set in the debug menu to replay a run.

### 7.2 Evidence each day

The evidence for a day is assembled from four layers:

1. **Baseline**: always there (see 6.4).
2. **Ambient**: a few random filler items per day (promo texts, café transactions, chat small talk, calendar entries), so the room changes daily and the clue is never the only new thing.
3. **Scenario**: the items today's scenario adds, including red herrings.
4. **History**: what earlier days left behind: calls in the call log, transfers, frozen-account notices.

Times in evidence are written relatively (`{today} 09:12`, `{yesterday}`, `{today+1}`) and resolved to that day's date.

### 7.3 Newspaper

The *Seoul Daily*, composed each morning by `NewspaperComposer`:

- **Front page.** Day 1: a local story introducing Mangwon-dong. Day 2 onwards: the reveal of yesterday's scenario, with the headline, what happened because of the player's choice, "What gave it away" listing every clue with ✓ (the player saw it) or ✗, and a **Lesson** box with the rule.
- **Warning column.** Easy and Medium days: the rule today's scenario tests. Hard days: a rule from another category, which can mislead.
- **Local news.** Today's scenario articles (a police bust, an inspection date) plus one or two filler stories.
- **Ads.** Delivery food, academies, and on some days a too-good job ad.

Every rule the player reads is copied into the notebook.

| Rule | First taught by |
| --- | --- |
| Banks never move your money to "protect" it | M6, H1 reveal |
| Unsure? Hang up and call a number you looked up yourself, not the one the caller gives | Day 1 paper |
| Prosecutors, police and the financial supervisor never ask for transfers or "inspection accounts" | H2, H4 reveal |
| Before you send, read the name on the account: a person's name on an "official" account is a red flag | Day 1 paper |
| No reports on a number or account doesn't make it safe; new accounts are clean | H2 reveal |
| Family asking for money from a new number or profile? Call their usual number first | E2 |
| Check the address of a payment link; official sites use their own domains | M2 |
| Never let anyone move money through your account: forwarding deposits makes you a money mule | H5 |
| Real inspections are announced in advance and never charge on the spot | E1, M3 |
| A legit caller is fine with you calling back | Any Test decision |

The wording follows the public warnings of Korea's Financial Supervisory Service and National Police Agency; the sources are linked when the final newspaper text is written.

### 7.4 Clue tracking

`ClueTracker` marks a clue as found when the panel or app screen showing it is opened while the scenario is active (the transfer screen counts once the holder's name is shown). Panels and apps report what the player looked at through `ClueEvents` (panel opened, card flipped, board item, document, recipient check, number looked up, page opened, bank alerts, text or mail read); each `ClueDef` lists the events that count. Finding clues does not change the outcome and the player is not told during the day; the next morning's case summary lists every clue, found or not, with where it was.

### 7.5 Summary

Opened after the final morning's newspaper:

- A row per day: scenario, difficulty, the player's verdict, the truth, right or wrong, clues found (e.g. 2/4).
- Savings at the start vs the end.
- A rating based on right verdicts and money kept, such as "Scam-proof", "Careful" or "At risk".
- **Lessons learned**: the rules from the run, each marked applied (✓) or missed (✗).
- Buttons: Play again (new seed), Quit.

## 8. Architecture

### 8.1 Folders and assemblies

```
Assets/_Game/
  Scripts/
    Core/       Plain C# rules, no MonoBehaviour, unit-tested
    Data/       ScriptableObject definitions (content, phone content, conversations)
    Flow/       GameFlow, DayDirector, DebugMenu
    Player/     FirstPersonController
    Gameplay/   Interactor, Interactable
    Audio/      GameSettings (volumes, look sensitivity), MusicPlayer (loop + tension layer), VoicePlayer
    UI/         UI Toolkit views: Core (UIKit builders, shared elements, clipboard, SFX),
                Panels, Phone (nine apps, call screens), Call (transcript, call HUD), Hud,
                Day (DAY card, CASE OPENED, deadline, fade), Menus (title, pause, settings, next morning)
    Editor/     Art pipeline, UI pipeline, content builder, audio pipeline, scene pipeline, GameCapture,
                (later) ContentValidator and the scenario preview window
  UI/
    Fonts/      OFL fonts with their licences, generated font assets
    Sprites/    Generated 9-slice frames, icons, portraits and document images
    Uss/        Theme.uss plus Phone, Panels and Call style sheets
    Settings/   Panel settings, theme style sheet, UISkin (every UI texture by name)
  Audio/
    Music/      Generated loops (Tools/Audio/music.py)
    Voices/     Generated voice clips per voice + manifest.json (Tools/Audio/tts.py)
  Data/
    Day1/       Day 1: phone, directory, room, the call, the day (clues, next-morning papers, rule)
    Day2/ Day3/ Each day's asset plus a call, phone, room and directory per variant (Day2_Scam_*, Day2_Legit_*)
    Samples/    A sample chat for testing (F2/F3 in development builds)
    VoiceBank   Every voice clip by voice and text
    Run/  Baseline/  Ambient/  Rules/  Scenarios/<id>/
  Resources/DayCatalog  The days of the run, in order (found without scene references)
  Art/Textures/Papers/  The desk newspaper printed for each front page
  Prefabs/ Art/ Audio/
  Scenes/Home.unity, Room.unity, End.unity
  Tests/EditMode/
```

Assemblies: `DontCallMe` (runtime), `DontCallMe.Editor`, `DontCallMe.Tests.EditMode`.

### 8.2 Game flow

Three scenes; `SceneFlow` loads them (with a fade) and hands the day's result from Room to End.

```
Home (TitleScreen) → Room (DayDirector + CallDirector) → End (EndScreen) → Room again or Home
```

`DayDirector` runs a day. `Prepare` (called by `UIManager` as it wakes up, so the phone and room show the day's content from the first frame) takes the day from `GameRun.PendingDay` (or its own field when the Room scene is opened directly), picks the truth and composes the evidence (7.0). Then: the DAY card with the echoes' lines, the seated start while the echoes buzz in, the forced call, the CASE OPENED card when `CallDirector` reports the hold, the investigation against the deadline (beats, music tension, screen-edge pressure, ticking), and the fade to the End scene with a `DayResult` (outcome, ending, money, time taken, clues found), recorded in `GameRun`. `CallDirector` plays conversations: voiced lines, decisions, the hold and its questions, and the endings. `forceVariant` on `DayDirector` plays one truth for testing.

### 8.3 Core classes (plain C#)

| Class | Responsibility |
| --- | --- |
| `RunBuilder` | Seeded scenario pick with the constraints in 7.1 |
| `RunState` | Savings, current day, the results of each day |
| `ConversationRunner` | Walks the node graph, emits lines, facts and evidence deliveries, holds decisions, drains patience, fires pressure lines and the timeout |
| `VerdictJudge` | Right, wrong or partial from truth + verdict (table in 5.4) |
| `ClueTracker` | Which of today's clues the player has seen |
| `EvidenceComposer` | Builds the day's evidence per source from the four layers in 7.2 |
| `NewspaperComposer` | Builds the day's newspaper from the previous result, today's scenario and filler |
| `TimeTokens` | Resolves `{today}`-style tokens to dates |

### 8.4 Input

- Actions in `Assets/InputSystem_Actions.inputactions`: `Drag` (drag to look), `Phone` (Tab), `Back` (Esc, right mouse button in panels), `Interact` (E, plain press) and the mouse click. `1` and `2` pick a decision option and `N` opens the notebook.
- Looking: hold a mouse button and drag to turn. Drags shorter than a few pixels count as clicks, and presses that start on the UI never turn the view. UI that is hidden or faded out never catches the pointer (the transcript is set to `visibility: hidden` once it fades), so a press anywhere on the room always looks or interacts.
- A press on the room while the phone is up puts the phone away and the same drag turns the view; a ringing phone stays up. A click on the dimmed room around a panel closes it, and clicking the call HUD brings the phone back.
- `UIManager` keeps a stack of open panels. The first panel calls `FirstPersonController.SetInputLocked(true)`; closing the last one unlocks it. Esc goes back one screen in the phone, then closes the top panel.
- `Interactor` raycasts from the mouse cursor (up to 2.5 m) against the `INT_*` colliders, shows a prompt such as "Click · Read the newspaper" and opens the panel on click or E. A click that ended a drag (`FirstPersonController.PressWasDrag`) is ignored.

## 9. Data model

```
EvidenceItem        source   (Newspaper, Wallet, Drawer, Board, Notebook, CallLog, Contacts,
                              Messages, Talk, Bank, CheckFirst, Browser, Parcels, Mail)
                    group    thread, section or page, e.g. "Family", "Transactions"
                    title, body, meta (time/amount, supports {today} tokens)
                    facts    Fact[] { kind (Phone | Account | Name | Case | Url | Amount), value }
                    image    optional sprite (bill scan, stamp, card art, portrait)

RoomBaseline (SO)   EvidenceItem[] items
AmbientPool (SO)    EvidenceItem[] items, int perDay
RuleData (SO)       headline, ruleText, sourceName, sourceUrl

Directory (SO)      the world's facts the apps look up
  accounts          { bank, number, holderName, reports[] }         → transfer name check, CheckFirst
  numbers           { number, owner, isOfficial, reports[] }        → CheckFirst, caller ID
  pages             { url, title, queryAliases[], blocks[] }        → Browser
  parcels           { tracking, item, status, courier, driverPhone }→ Parcels tracking
  callbacks         { number, lines[] }                             → default answer when dialled

ScenarioData (SO)
  identity      title, category, difficulty, channel (Call | Chat | Sms), isScam, twin
  caller        displayName, number, inContacts, portrait, voiceId
  evidence      EvidenceItem[] added, EvidenceItem[] redHerrings, Directory additions
  clues         Clue[] { EvidenceItem item, string note }   item may be baseline or scenario
  newspaper     EvidenceItem[] todayArticles, RuleData hintRule (Easy/Medium only)
  conversation  Node[] nodes (first node = start)
                  Node     { id, Line[] lines, Decision? decision, float patienceSeconds,
                             Pressure[] pressure }
                  Line     { speaker (Caller | Player | System), text, AudioClip clip,
                             Fact[] facts, EvidenceItem[] deliver }
                  Decision { kind (Ask | Test | Commit), Option a, Option b }
                  Option   { label, playerLine, nextNodeId | endingId }
                  Pressure { atPatiencePercent, text, AudioClip clip }
  actions       ActionTrigger[] { kind (Transfer | Call | Link | HangUp), target, endingId,
                                  Line[] reply }   e.g. the official line's Verify script
  endings       Ending[] { id, verdict (GoAlong | Refuse | Verify), moneyDelta,
                           consequenceText, EvidenceItem[] leaveBehind }
                hangUpEndingId, timeoutEndingId
  reveal        headline, bodyIfRight, bodyIfWrong, RuleData lesson

RunConfig (SO)      Difficulty[] daySlots = [Easy, Medium, Medium, Hard, Hard]
                    startingSavings = ₩4,280,000 (checking + tuition savings)
                    ScenarioData[] pool
```

- `nextNodeId` and `endingId` are strings, but a custom property drawer shows them as dropdowns of the scenario's own node and ending ids, so writers pick rather than type.
- Audio clips are optional. Without a clip, a line stays on screen for `text.Length / 15` seconds.

**What is built today (Day 1).** The conversation part of ScenarioData is `ConversationData`; a day is `DayData`:

```
ConversationData (SO)  caller { displayName, number, portrait, voice }, claims[]
                       nodes[]      { id, lines[], decision?, next, holds }
                                    Line { speaker, text, spoken (what the voice says), facts[], deliver[] }
                       caseInfo     { caseTitle, claimedIdentity, ask, deadline, deadlineReason, objective }
                       questions[]  { label, playerLine, answer Line[], endingId? }
                       beats[]      { at (in-game time), line }
                       endings[]    { id, verdict, timedOut, moneyDelta, lines Line[], consequence }
                       actions[]    { kind, target, endingId, answeredBy, portrait, voice }
DayData (SO)           day, dateLabel, shortLabel, place, startTime, intro, ringDelay, nextDateLabel
                       variants[]   DayVariant { id (scam | legit), conversation, phone, room, directory,
                                    clues[] { id, text, where, when[] { ClueEvent, target } },
                                    papers[] { outcome, headline, subhead, body, verdictNote, print },
                                    ruleTitle, rule, ruleSource }
                       echoes[]     DayEcho { afterDay, truth (Any | Scam | Legit), outcomes (flags), kind (Sms | Chat |
                                    Mail | BankNotice | BankTransaction | MissedCall | BoardNote | Contact | DayCard),
                                    from, sender, avatar, when, title, text, link, amount, image, position, notify }
DayCatalog (SO)        days[]   in Resources, loaded by name
DayRecord (saved)      day, variant, scam, outcome, moneyDelta, savingsAfter, clues[], decidedAt, minutesTaken,
                       callerNumber, callTime, callSeconds, transferTo / Bank / Account
VoiceBank (SO)         entries[] { voice, text, AudioClip }   looked up by voice + spoken text
```

`spoken` lets the voice read an account number digit by digit while the transcript shows `110-900-551207`. Voice clips are found by voice and text, so a changed line only needs the voice pipeline run again.

## 10. UI

All panels are UI Toolkit: one `UIDocument` with a panel settings asset scaled for 1920 × 1080 and one theme (`Assets/_Game/UI/Uss/Theme.uss` plus per-area sheets). Templates are small C# builders (`UIKit`, `PhoneScreen`, `RoomPanel`) that only set classes and content, so a new app screen or document is a few lines of layout and all styling stays in USS. The look follows `references/UI`: cream paper with hand-inked outlines, dusty blue insets, pixel-art portraits, chunky square buttons with a drop shadow.

| Panel | Notes |
| --- | --- |
| Newspaper | Paper sheet with a torn edge, *Seoul Daily* masthead, front page, warning column, local news, ads |
| Drawer | Documents as paper cards on a wooden tray; previous/next and click to flick; a PAID or OVERDUE stamp as an image |
| Cork board | Board close-up; each item enlarges on click (calendar month, numbers note, notices) |
| Wallet | Card front/back flip, other cards in a row |
| Notebook | Two tabs: Rules (handwritten, newest first) and Case (claims and facts as copy chips) |
| Phone | Frame + status bar + home grid + nine apps. Screens share list and detail templates |
| Incoming call | Round pixel portrait with a pulsing ring, name or "Unknown", number, a shaking phone and slide to answer (no decline) |
| In call | Call screen with call timer and hang-up (off during a case: "Verdict on the call"); "Return to call" bar in other apps |
| Transcript | Chat log left of the phone: portraits, bubbles with times, typing indicator, fact chips, two decision buttons, decision timer |
| Call HUD | Top centre while the phone is down: caller, latest line, decision timer, "Decision waiting — Tab" |
| Decision timer | Ring with seconds; amber at 50%, red and pulsing at 25%; pressure overlay at the screen edges |
| HUD | Bottom right: notebook and phone buttons with a notification badge; interact prompt at the cursor |
| DAY card | Black card: DAY 1, the date, time and place, a line of intro; click or wait |
| CASE OPENED | Paper case file with a tab (CASE #01), the caller's portrait, "Says they are", "Wants you to", "Deadline", "Your job", an OPEN stamp and Start investigating |
| Deadline | Under the clock: deadline time, minutes left and a bar; amber at half, red and pulsing in the last quarter |
| Hold | The transcript footer while the caller holds: the questions still open (1–4) and the verdict panel |
| Verdict | Dark strip, YOUR VERDICT, gold / red buttons with an icon, a label and a detail line; a coloured confirmation step (send: amount and recipient's name; hang up) |
| Title screen | The DON'T CALL ME! sticker logo over the room at dusk (slow camera drift), the menu, a ringing phone whose slide starts the game, lo-fi music |
| Pause and Settings | Paper card over the frozen room: Resume, Settings, Main menu; sliders for music, sound effects, voices and look sensitivity (saved) |
| Next morning | The Seoul Daily front page for the outcome beside the case summary: SCAM or REAL and RIGHT / BEST / WRONG CALL or TOO LATE stamps, money kept or lost, decided at, time taken, clues ✓/✗ with where, the rule learned, Play again and Main menu |
| Toasts | Top right, informative ones never catch the pointer |

Fonts (all OFL, in `Assets/_Game/UI/Fonts` with their licences): Nanum Gothic for the UI, Do Hyeon for headings, Gaegu for handwriting, DM Serif Display and Crimson Text with Nanum Myeongjo for the newspaper, DotGothic16 for the clock and timers.

Options in the pause menu: mouse sensitivity, text size (100% / 125% / 150%), text speed, master, voice and SFX volume.

## 11. Art and audio

**Look.** Warm, cosy and lived-in, so the calls feel intrusive. Comic-book ink outlines and hatching over hard-banded toon shading, a retro print grade, late-afternoon sun through the window and dust in the sunbeam. People are pixel art in the style of `references/art/ref4.png`, in the room's photos and in the callers' portraits.

**3D and textures.** The bedroom, its props and textures are generated from code in `Tools/ArtGen` (Python for textures, Blender for the model, a Unity editor pipeline for materials, prefab and scene). See `Tools/ArtGen/README.md`. Hand-made Blender models can still replace a generated part: keep the object name, and the pipeline remaps its materials.

**2D.** UI frames, icons, caller portraits, menu glyphs and the title logo are generated by `Tools/ArtGen/ui_art.py`; the UI pipeline imports them as 9-slice sprites.

**Audio.** See `Tools/Audio/README.md`.
- Voices: every voiced line is exported from the content (`Tools/Audio/voice_lines.json`), spoken by a macOS `say` voice per character (the caller Daniel, the bank Samantha, the fraud hotline Karen, police Tessa, Mom Moira, Dad Rishi), shaped like a phone line (band-pass, compression, line hiss) and imported into the `VoiceBank`. The files go in `Audio/Voices/<voice>/<hash>.wav`.
- Music: generated loops (`Tools/Audio/music.py`): lo-fi keys for the title, a calm bed and a tension layer of the same length for the investigation (crossfaded by the time left), and a bright morning loop for the next morning.
- SFX: ringtone, vibration, slide-to-answer, hang-up tone, message sound, typing, paper rustle, drawer, heartbeat, timer tick.
- Ambience: alley sounds through the window (delivery scooters, cicadas, a vendor truck's loudspeaker), the air conditioner and the dehumidifier.

## 12. Testing

**EditMode tests (Core):**
- `RunBuilder` respects every constraint over 1,000 seeds, and the same seed gives the same run.
- `ConversationRunner`: paths, patience draining only at decisions, pressure thresholds, timeout, fact and evidence delivery, action triggers.
- `VerdictJudge`: the full table in 5.4.
- `EvidenceComposer`: layers, determinism per seed, history carry-over.
- `NewspaperComposer`: no reveal on Day 1, clue ✓/✗, the hint rule only on Easy and Medium days.
- `ClueTracker`, `TimeTokens`.

**Content validator** (`Tools > Don't Call Me > Validate Content`):
- The clue count matches the difficulty (2, 3, 4) and every Medium or Hard scenario has its red herrings.
- Every clue item is present that day (baseline, scenario or today's articles), and every account or number a clue relies on exists in the directory.
- Every decision has exactly two options, and every option points to an existing node or ending.
- Every node is reachable, the graph has no cycles, and every path ends.
- At least one ending is right for the scenario's truth. The hang-up and timeout endings exist.
- No line asks for a code, PIN or password.
- The pool can fill every slot under the run constraints, and twins point at each other.

**Scenario preview window.** An editor window that plays a scenario's conversation as text with clickable choices, so writers can check a scenario without entering Play mode.

**Playtests with the target audience** (young adults living alone, at least five people), measuring:
- Right verdicts in Days 1–2 vs Days 4–5.
- Whether they check the account name and the official number without being told.
- Whether they can name three rules after one run without looking.
- Where they get stuck on controls or UI.

## 13. Build phases

Each phase ends with something playable.

| Phase | Work | Playable result |
| --- | --- | --- |
| 1. Room and movement | Generated bedroom, first-person controller, colliders | Walk the room (done) |
| 2. UI templates | UI Toolkit theme and assets, interactor, room panels, phone with nine apps, incoming call with slide to answer, transcript, decision timer, call HUD, demo content | Open every panel and app, take the demo call, investigate, verify or send a transfer (done, see 13.1) |
| 3. Conversations | Day flow (seated start, CASE OPENED, the hold with questions, beats and the deadline), voiced lines, title / pause / settings / next-morning scenes; later `ConversationRunner` in Core | Day 1 (M6) playable end to end with four outcomes (done) |
| 4. Room evidence | Panels fed by `EvidenceComposer`; directory lookups | Solve M6 with the account-name check |
| 5. Day loop | `GameRun` (save), `DaySetup` (truth, history, echoes), clue tracking, reveal, the week summary; later `RunBuilder` and the pool | A 3-day run with scam and legit variants and continuity (done); the 5-day run from the pool |
| 6. Content | Household baseline, ambient pool, the 12 first-batch scenarios, validator, preview window | Varied runs that pass validation |
| 7. Audio and polish art | TTS voices, SFX, ambience, UI animation polish | Looks and sounds finished |
| 8. Polish and playtest | Day 1 onboarding, options menu, target-audience playtests, patience and difficulty tuning | Prototype ready |

### 13.1 How to build and try it

- **Content**: `Tools > Don't Call Me > Content > Build Days` writes `Assets/_Game/Data/Day1..Day3`, the sample chat in `Data/Samples` and `Resources/DayCatalog`; `Print Desk Newspapers` prints the desk paper for each front page (Python, see `Tools/ArtGen/README.md`). When `ContentBuilder.Version` is newer than the one recorded in `Day1.asset`, the editor rebuilds the content, the prints and the voices by itself after the next compile.
- **Audio**: `Tools > Don't Call Me > Audio > Run Voice Pipeline` (export lines, TTS, import) and `Generate Music`.
- **Scenes**: `Tools > Don't Call Me > Scenes > Build Home and End Scenes` (also sets the build order Home, Room, End); the Room scene's UI and directors come from `UI > 4. Set Up Game UI in Room Scene`.
- **Play**: open Home and press Play (Continue picks up the saved week), or open Room to start straight at the day set on `Flow/DayDirector` (Day 1; set `forceVariant` to `scam` or `legit` to test a truth). To test a later day with echoes, write earlier `DayRecord`s into PlayerPrefs `DCM.Run`. `F2` (demo call) and `F3` (sample chat) work in development builds when no day is running.
- **Check**: `Tools > Don't Call Me > UI > Capture UI Tour` screenshots every panel and app and plays calls to each ending. `DontCallMe.Editor.Tools.GameCapture.Capture(path)` renders a 1920 × 1080 screenshot of the paused game even when the Game view is hidden.

## 14. Debug menu

F1 opens it in development builds only:
- Set the seed.
- Jump to a day.
- Force a scenario into today.
- Freeze patience.
- Show the clue checklist.
- Skip to Summary.

## 15. Risks

| Risk | Mitigation |
| --- | --- |
| Players can't find the investigation tools | Copy chips, suggestion chips in Browser and CheckFirst, Day 1 prompts that walk through one account-name check |
| The clue is obvious because it is the only new item | Ambient filler every day; red herrings on Medium and Hard |
| Checking everything is always right, so there's no tension | Decision timers; Verify only counts with a number the player found; legit callers who are refused cost something |
| Branching dialogue is hard to write in the Inspector | Node-id dropdowns, validator, preview window |
| Random runs are too easy or too hard | Pool constraints, per-difficulty patience values, playtest data |
| Players refuse every call | At least one legit scenario per run, and refusing a legit call has a visible cost in the next paper |

## 16. After the prototype

- Korean text and voices (the UI fonts already cover Hangul).
- More days, with the difficulty mix growing each day.
- Chat scenarios with photos and voice notes.
- A mobile build.
