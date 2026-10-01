# Don't Call Me! — Technical Plan

Game concept and research: `references/idea.md`. This document describes what we build and how, for the first playable prototype.

**Goal.** Young adults in Korea who live on their own (students, first jobs) handle their own banking and are frequent targets of institution-impersonation, job and rent scams. In the game they feel a scam call's pressure in a safe place and learn to check a caller's claims against evidence before a real call arrives. The player should leave able to name the rules and, more importantly, having practised the checks that expose a scam: whose name is on the account, who really owns the number, what the landlord or the courier wrote themselves.

> The setting and the audience moved from Indonesia and Vietnam to Korea on 2026-09-30. `references/idea.md` still describes the earlier version; its structure (twin cases, newspaper as rulebook, evidence in the room) carries over.

> **Scope cut, 2026-10-01.** The prototype was reduced to what one investigation needs: three phone apps (Contacts, Chats, Nuri Bank), four things to use in the room (newspaper, wall calendar, desk drawer, and a computer with two lookups), calls only, a guided first day, and two, three and four clues on Days 1, 2 and 3. What was cut is listed in section 15.

> **Day 0 and the title, 2026-10-01.** The guide moved from Day 1 to a practice day before it, Day 0, whose case needs every tool once, so the player learns what each one is for before the week starts. The title got a new layout (a big Start button with Case Files, Settings and Quit under it). Text everywhere is near-black on paper in bold weights, so it stays readable in a small window. The phone was redrawn twice that day (pixel art, then a navy screen with answer and decline buttons) and put back as it was (the caller's portrait, slide to answer, no decline), then given a plain dark gradient background in place of the night skyline and larger call buttons.

> **Case pools, 2026-10-01.** Days 1 to 3 no longer have one fixed case each: every day brings one of its cases, picked when the day starts, and the case the player has finished least often comes first, so another week plays differently. Four cases were added, each a scam or its legit twin with a **single clue that is not on the computer**: in the newspaper, in the bank app, in the family chat and on the calendar (6.2).

## 1. Decisions

| Topic | Decision |
| --- | --- |
| Engine | Unity 6000.4.11f1, URP (`PC_RPAsset`), Input System, **UI Toolkit** (C# builder templates + USS theme) |
| Scenes | Home (title screen), Room (the day), End (the next morning). The same generated bedroom appears in all three |
| Platform | PC (macOS + Windows), mouse and keyboard |
| Camera | First person. The player walks with WASD and turns the view by clicking and dragging; the cursor stays visible. While any 2D panel is open, walking and turning are locked |
| World | One room: Kim Jiwoo's bedroom in a twenty-year-old villa in Mangwon-dong, Mapo, Seoul (`references/art/ref5.jpg`). No NPCs in the room; family and neighbours reach the player through the phone |
| Interactions | Four things in the room open a 2D panel: the newspaper, the wall calendar, the desk drawer and the computer. While the room can be used they wear a pulsing yellow outline, and the one under the cursor lights up |
| Phone | Carried (Tab raises and lowers it), held in a hand, with three apps: Contacts, Chats, Nuri Bank. An incoming call raises the phone by itself and can only be answered: slide to answer, no decline button, it rings until the player slides |
| Day structure | Seated at the desk → the phone rings → the caller introduces himself and makes the ask (**CASE OPENED**) → he holds the line while the player investigates against his deadline, with pressure building → the player acts → the next morning's paper reveals the truth |
| Tutorial | Day 0 is a guided practice call: a yellow note walks the player through looking around, the newspaper, answering and replying, then every tool in turn (the two lookups on the computer, the drawer, the calendar, the bank app, the chats, the contacts), saying what each is for and what it just showed, and last the verdict (2.1) |
| Conversations | Calls only. The transcript sits next to the phone. At key moments the player picks one of **two** responses, with a visible decision timer; while the caller holds, the player can put questions to him (each costs time) |
| Investigation | Callers never ask for a code or password. They ask for things legit callers also ask for (a transfer), so the verdict comes from checking evidence: who owns the account and the number (looked up on the computer), what the lease or the delivery slip says, what the landlord, the courier or family wrote in the chats, what Jiwoo noted on the calendar, what today's paper prints, what the bank history shows |
| Verdict | Given only on the call, in the **verdict panel** under the transcript while the caller holds: go along (send the money) or refuse (hang up). Each kind has a fixed colour (gold, red) so the colours never hint at the answer. The rest of the UI is for investigating: during a case the phone's hang-up button is off, and the bank app only shows balances and history |
| Voices | Callers are voiced with pre-generated TTS in the player's language (macOS `say` voices through a phone-line filter), one voice per character and language; the transcript reveals each line as it is spoken. The player's lines are text only |
| Menus | Title screen (the sticker logo, the green Start button, which reads Continue once a week is under way, Case Files, Settings, Quit, and a ringing phone whose slide also starts the game); pause on Esc (Resume, Settings, Main menu); Settings: language (English or 한국어), music, sound effects, voices, look sensitivity |
| Scenarios | Eight cases over four days (see 6). Day 0 is always "the unpaid gas bill" (the tutorial: a scam, eight checks, one per tool). Days 1 to 3 each bring one of their cases: Day 1 "the protected account" (always a scam, 2 clues on the computer), "the subscription fee" or "the extra zero"; Day 2 "the new rent account" (3 clues) or "Minjun's broken phone"; Day 3 "the held parcel" (4 clues) or "the dentist's deposit". Every case but "the protected account" is a scam or its legit twin at random, and the four newer ones have one clue each, in a place that is not the computer |
| Run | 4 days (Mon 5 – Thu 8 October: the tutorial, then three cases), saved as the player goes (Continue on the title screen, any day again from Case Files), ending with a week summary. A day's case is the one the player has finished least often, so a second week brings other cases. Each day's truth is revealed in the next morning's newspaper |
| Continuity | A day remembers the earlier ones without changing its case: the money that left stays gone, yesterday's case is on the desk newspaper, and **echoes** (chat messages, bank transactions, a saved contact, a line on the day card) react to how each day went |
| Content data | ScriptableObjects, written by an editor tool (`ContentBuilder`) from code |
| Language | English or Korean, chosen in Settings (title screen or pause menu; the first launch follows the system language). Everything exists in both: UI, dialogue and voices, documents, chats, lookup results and the desk newspaper prints. Korean names, places and won amounts in both. Banks, couriers, shops, companies, people and their numbers are fictional; the public numbers 112 (police), 119 (fire and ambulance) and 1332 (financial fraud hotline) are real |

## 2. A day in the game

```
Title screen (Home scene) → Start
  → DAY card over black: "DAY 0 · TUTORIAL · Monday, 5 October · 14:10 · Mangwon-dong, Seoul" and a line of intro
  → Jiwoo sits at the desk (the view is lower and turns within a range; no walking yet)
  → Day 0: the guide asks the player to look around and read the newspaper; the phone rings once it is closed.
    Days 1 to 3: yesterday's aftermath buzzes in (chat and bank notifications), then the phone rings
  → The phone jumps up and rings. Slide to answer (no decline)
  → Opening: the caller introduces himself and makes the ask (voiced, transcript beside the phone),
    with one two-choice decision on the way
  → CASE OPENED card: who he says he is, what he wants, his deadline, the player's job
  → Investigation: Jiwoo stands up. The caller holds the line until his deadline (in-game clock).
    The newspaper, calendar, drawer, computer and the phone's apps are free to use; the player can put
    questions to him (each costs time). Pressure builds: he pushes at set times, the music tightens,
    the screen edges redden, a ticking clock in the last minutes
  → The player gives the verdict on the call: send the money (go along) or hang up (refuse).
    If the deadline passes first, the caller hangs up (timeout)
  → "Call ended", fade to black
  → The next morning (End scene): the Seoul Daily reveals who really called, beside the case summary.
    The truth is held back for a few seconds: the paper lands with its headline hidden, the room goes
    dark with one spotlight on the spot of the stamp, a drumroll builds, then SCAM or REAL is stamped,
    then RIGHT or WRONG CALL, then the lights and the rest
    (money, time taken, clues found and where the missed ones were, the rule learned)
  → The next day, the same day again, or the main menu. After the last day: the week's summary
```

- A ringing call cannot be declined or ignored: the phone stays up and rings until the player slides.
- The game clock runs at 8 real seconds per minute and stops on the DAY and CASE OPENED cards and while paused. Every caller's deadline is 17:00. From Day 1 on that is about four real minutes of investigation; Day 0's call comes at 14:10, so the practice has more than twenty minutes and the deadline never presses.

### 2.1 Day 0: the guided day

Day 0 (`DayData.tutorial`) runs with `TutorialGuide`: a yellow note on the left of the screen, "HOW TO PLAY · n/13", one instruction at a time. Its case, "the unpaid gas bill", is written so that each tool gives one piece of the answer (6.2), and the guide takes the tools in the order the day's clues are listed.

| Step | The note says |
| --- | --- |
| 1 | Hold the mouse button and drag to look around |
| 2 | Every morning's paper warns about the trick going round. Things you can use glow: click the newspaper, read today's warning, close it |
| 3 | The phone is ringing: drag the green button to the right to answer |
| 4 | Pick a reply (click, or 1 or 2), then listen |
| 5 | This is the case: who the caller says they are and what they want. Click Start investigating |
| 6 | The computer shows who really owns a number: click the laptop, then her number under Check a phone number |
| 7 | The computer shows whose account it is, too: choose Check a bank account and click the account she gave |
| 8 | Your own papers are in the desk drawer: open it and find the gas bill |
| 9 | The calendar on the wall shows what day it is and what you wrote down |
| 10 | Your bank app shows what really left your account: on the phone, press Apps, then open Nuri Bank |
| 11 | People you know write to you themselves: open Chats and read the residents' chat |
| 12 | A number you saved shows its name when it calls: open Contacts. Is she there? |
| 13 | You have checked everything: give your verdict under the conversation |

- Steps 6 to 12 are data, not code: each clue of the day carries a `guide` line, and the guide shows the first clue not found yet. The seven clues after the newspaper may be found in any order; the note then moves to the next one still open, so nobody is stuck.
- Above the instruction, a green line with a tick repeats what the last place showed ("Her number is a prepaid phone opened three days ago, with four scam reports. It is not the gas company's."). That is the lesson of the day: what each tool is for.
- The note says how to get there from where the player is: "Close this first (Esc).", "Lower the phone first (Tab).", "Press Tab to raise your phone."
- The object a step is about blinks (the newspaper, the laptop, the drawer, the calendar). Room panels open further right while the note is on screen, so it never covers them.
- The call waits until the newspaper has been opened and closed (at most 75 seconds), and the game clock stays stopped until then, so reading costs no time.
- The next morning lists the eight checks under WHAT YOU CHECKED, and the rule is the game in one line: "Check before you pay".
- Days 1 to 3 run without the guide; a toast ("Investigate: the caller is holding the line…") opens the investigation instead.

## 3. The room

3.6 × 3.8 m, 2.4 m high: the bedroom of a villa in a humid part of Seoul that was never renovated (see `Tools/ArtGen/README.md` for how it is built). Old vinyl floor, damp-stained painted walls, cherry-brown door and mouldings, a PVC window over the bed, a wall air conditioner and the ondol thermostat by the door. Late-afternoon sun comes in through the window.

| Interactable | Where | 2D panel | Evidence it holds |
| --- | --- | --- | --- |
| Newspaper (`INT_Newspaper`) | Desk, folded | Full-page *Seoul Daily*, changes daily | The front page (Day 0: the warning story; later: yesterday's case), the warning box with the day's advice, local news, ads (one for checkfirst.kr) |
| Wall calendar (`Board_Calendar`) | Cork board over the desk | The pharmacy calendar and a list of what Jiwoo wrote on it, with today marked | Today's date (Monday 5 October is a substitute holiday), rent day and the amount, the dentist, the gas check; on the day it matters, the note she made from a real notice ("Mr. Choi in hospital this week. His son Hyunwoo collects the rent", "Monitor arrives! Hangang Express", "Monitor held at customs: duty 56,600 won") |
| Desk drawer (`INT_Drawer`) | Desk, top drawer | Documents as paper, one tab each, previous and next | Lease contract (the landlord, his number, the rent account and its holder, the son's name and number, "the account changes only in writing"), the September gas bill (18,420 won, stamped PAID by auto-pay on 28 September, with Mapo City Gas's one number, 02-555-0181), delivery slip (Hangang Express's one number, 1588-5520), a receipt |
| Computer (`Laptop`) | Desk | A browser open on CheckFirst (`checkfirst.kr`, fictional) with two tabs | **Check a phone number** and **Check a bank account**. Each answers with the owner or account holder, a line about the registration ("Prepaid phone, opened 6 days ago", "Personal account, opened 3 weeks ago", OFFICIAL NUMBER), the number of fraud reports and what they say, and the reminder that no reports does not mean safe |
| Phone | Carried (Tab) | Phone + transcript | See section 4 |

The wallet, the notebook and the notes on the cork board are still in the room as props, without panels. The desk chair stands pushed back from the desk: Jiwoo starts the day seated at the desk, stands up in front of it, and can walk between the desk and the chair.

**Highlight.** While the room can be used (no panel open, the phone down), `Interactor` gives every `Interactable` a thin pulsing yellow outline; the one under the cursor is also lit warm and shows its prompt ("Click · Use the computer"), and the one the guide points at blinks. `Interactable.SetHighlight` writes `_Highlight` on the object's renderers (RGB = added light, A = mark). The toon shader writes a negative ink id for marked objects in its DepthNormals pass, and the ink pass (`DCM_InkComposite`) draws the outline where marked meets unmarked, pulsing with the global `_DCM_HighlightPulse`.

## 4. The phone

Raised with **Tab** (or the phone button in the corner), shown on the right of the screen in Jiwoo's hand, with the transcript to its left during a call. While the phone is up, the player cannot walk.

The phone is drawn in the game's inked style: a copper rim round a dark bezel with a notch, held in a hand (fingers round the left edge, the thumb on the right). The home screen shows the clock, the date and three apps on a plain dark gradient (the same background as the call screens), with a status bar (clock, signal, battery) and a home bar. **Back** (arrow, Esc or right-click) goes one screen back; **Home** returns to the home screen.

| App | What it shows | What it is for |
| --- | --- | --- |
| Contacts | Names, numbers, memos ("Choi Youngsik · 1F · rent due on the 7th"); a contact's page with the number as a chip | Is the caller's number one Jiwoo saved? |
| Chats | Messenger threads: family, Mangwon Heights residents, Yuna, the café crew, Hangang Express (delivery notices), AliStar (order messages); on later days, direct messages from the landlord or his son | What did the landlord or the courier write themselves, and when? Unread threads carry a badge |
| Nuri Bank | Accounts and balances, recent transactions | What is there to lose, and what was already paid (Day 0: the gas bill left by auto-pay a week ago). Money that left on an earlier day stays gone |

**Copy chips.** Every number, account or amount a caller says or a document shows appears as a chip. Clicking a chip copies it ("Copied · paste it on the computer"). The computer's lookup field has a **Paste** button, and it offers the call's own number and account as one-click chips, so the player never has to type a 14-digit account number.

### 4.1 Calls

1. **Incoming.** The phone raises itself, rings and shakes. On the dark gradient the screen shows the caller's round portrait with a pulsing ring, "INCOMING CALL", the name if saved in contacts, otherwise "Unknown" and the number, and one control: **slide to answer** (a green knob on a dark track). There is no decline button and Tab does nothing until the player answers.
2. **In call.** The call screen (name or number, call timer, Apps, Speaker, hang-up) sits on the phone. The **transcript panel** sits to its left: the caller's lines with their portrait and time, the player's lines on the right. Lines type in; a "…" bubble shows while the caller is speaking. When a decision comes up, two response buttons and the **decision timer** appear at the bottom of the transcript.
3. **Investigating during a call.** The player can open any app (a green "Return to call · 01:42" bar stays on top) or lower the phone and walk to the computer or the drawer. With the phone down, a compact call HUD stays at the top of the screen: caller, the latest line as a subtitle and "Holding the line · Tab to ask or decide".
4. **Hanging up** is a verdict (refuse), and so is sending the money the caller asked for (go along). Both are given in the verdict panel (5.3); the phone's own hang-up button is off during a case ("Verdict on the call").

### 4.2 Chats

Chats are evidence to read; nobody has to be answered. Messages that earlier days left behind (echoes) arrive as notifications while Jiwoo sits down ("Chats · Hyunwoo (landlord's son): …") and mark their thread unread. Opening a thread counts as reading it (`ClueEvent.ChatRead`).

## 5. Conversations

### 5.1 Structure

A conversation is a small graph of nodes. Each node plays a few lines (caller, player or system), then either continues to the next node or stops at a **decision** with exactly two options. An option says the player's line, then leads to another node or to an **ending**.

A line can carry **facts** (an account number, an amount, a phone number), which become copy chips in the transcript, and can deliver evidence as it is spoken (a chat message, a deposit in the bank app).

Decisions come in three kinds:

- **Ask**: a question that makes the caller commit to a checkable fact ("Which account, and in whose name?"). Legit callers answer plainly; scammers answer too, but their facts fail the check.
- **Test**: a request only the real party can meet. Legit callers agree or answer; scammers push back or dodge.
- **Commit**: the verdict (go along or refuse).

The **verdict panel** ends a call, and each scenario maps its choices to endings through its actions:

| Verdict | Ending |
| --- | --- |
| Send the money (the Transfer action's account, bank and amount; the confirmation shows the holder's name, like the bank's own check) | Go along |
| Hang up | Refuse |

Each verdict is said aloud first (the player's line, e.g. "I'm not sending anything. Goodbye.").

### 5.2 Example: "The protected account" (Day 1, scam)

```
[Caller 070-8844-2019, voice Daniel]
  "Hello, is this Kim Jiwoo? This is Manager Jeon from Nuri Bank's account protection team."
  "Twenty minutes ago, someone logged into your account from an unregistered device in Busan."
DECISION (Ask, 45 s)
  A: "Busan? I've been home all day."           → "A fraud ring is emptying accounts in Mapo today..."
  B: "How do I know you're really from Nuri Bank?" → "I have your account ending in 8814 on my screen..."
HOLD
  "To keep your savings safe, we'll move them to a protected account until the security reset at five."
  "Please transfer 1,200,000 won to the protected account: Nuri Bank 110-900-551207."
  "Stay on the line and don't tell anyone..."   "I'll hold while you open your bank app."
CASE OPENED   deadline 17:00 ("before the security reset")
Questions     why can't I call the bank? · whose name is on the account? (Jeong Miran, "an officer")
              · why an 070 number? ("our team's direct line")
Beats         16:34 "Are you still there?" · 16:40 "I've opened a protection case for you"
              · 16:47 "They tried to log in again" · 16:53 "Seven minutes" · 16:57 "Three minutes!"
Actions       transfer to 110-900-551207 → go along (−₩1,200,000)
              hang up → refuse · 17:00 → timeout
Clues (2)     Computer → Check a bank account: JEONG MIRAN, a personal account, 2 fraud reports
              Computer → Check a phone number: not registered, an internet phone, 7 scam reports
```

### 5.3 The hold: questions, beats and the deadline

When a node is marked **holds**, the caller has made the ask and waits on the line. The CASE OPENED card appears (`caseInfo`: title, claimed identity, the ask, the deadline and the player's job) and the investigation starts.

- **Deadline**: an in-game time (`caseInfo.deadline`, 17:00). A widget under the clock counts down (amber at half, red and pulsing in the last quarter); at 0 the caller gives up and the timeout ending applies.
- **Questions** (`questions`): shown in the transcript while he holds (keys 1–4). Each can be asked once; the answer is voiced and may carry facts. Answers are written so they sound reasonable but give checkable facts away: the "protected account" is in an officer's *name*, the "new rent account" is "my wife's".
- **Pressure beats** (`beats`): lines he says at set in-game times. Beats already past when the hold starts are skipped unless they deliver something.
- Screen-edge pressure, the tension layer of the music and a ticking clock follow the time left.
- The phone can go down at any time (Tab, or a press on the room): the call HUD at the top shows his latest line and "Holding the line · Tab to ask or decide".
- **Verdict panel**: under the questions, a dark strip with YOUR VERDICT and two colour-coded buttons (gold "Send the money · ₩1,200,000 → Nuri Bank 110-900-551207", red "Hang up"). Each asks for a second click: the transfer with the recipient's name, or hanging up. The caller keeps talking over an open confirmation without closing it.

### 5.4 Patience and pressure

- Patience only drains while a decision is waiting, never while the caller is talking.
- Every decision node has its own patience time and pressure lines at thresholds. The **decision timer** shows it as a ring with seconds; it turns amber at 50%, red and pulsing at 25%.
- As patience drops, a pressure overlay darkens the screen edges and plays a heartbeat. It works whether the phone is up or down.
- At 0 the caller gives up: the scenario's **timeout ending** applies.

### 5.5 Endings and verdicts

Every ending has a verdict: **Go along** (did what the caller asked) or **Refuse** (hung up). Correctness follows from the truth; nobody authors it by hand:

| | Go along | Refuse | Timeout |
| --- | --- | --- | --- |
| Scam | Wrong | Right | Safe, but never decided (not right) |
| Legit | Right | Wrong | Wrong |

An ending also carries its consequence: a won amount and a line for the next morning's newspaper. The data model keeps a third verdict, **Verify** (calling a number the player looked up), for a later version with call-backs; no built day uses it.

## 6. Scenarios

### 6.1 Difficulty and design rules

A **clue** is a piece of evidence that points at the truth. Day 0 is practice: eight clues, one in every tool, each enough on its own, with the guide leading to them. From Day 1 a day brings one of its cases (6.2). Each day's first case needs one clue more than the day before and takes away one crutch.

| | Day 1 "the protected account" (Easy) | Day 2 "the new rent account" (Medium) | Day 3 "the held parcel" (Hard) |
| --- | --- | --- | --- |
| Clues | 2 | 3 | 4 |
| Where the clues are | Both on the computer, the first tool Day 0 taught | The lease in the drawer (or the number lookup), the account lookup, the residents' chat (legit: or the calendar note) | The courier's chat (or the calendar note), the shop's chat, the delivery slip (or the number lookup), the account lookup |
| What the computer says | Fraud reports on the account and the number: a lookup alone settles it | No reports on either. The names have to be compared with the lease | No reports, and the fake names sound official: the number is one digit off, the account holder a look-alike company |
| Truth | Always a scam | Scam or legit | Scam or legit |
| The day's newspaper | The matching warning ("there is no safe account") | A warning about new rent accounts: check whose name it is | A warning about parcel calls: compare the number and the account with the courier's own |

The other cases have **one clue each, and it is never on the computer**. There the number and the account come back clean and in the name the caller gives, in both truths, so a lookup cannot tell a scam from its twin. Each keeps its answer in a different place, so the player learns to use every tool instead of the browser every time:

| Case | The one clue is in | Why the computer cannot settle it |
| --- | --- | --- |
| Day 1 "the subscription fee" | Today's newspaper: the paper's own notice to its readers, in the box on the front page | The caller is a real man with an old phone and an old account. The question is whether he may still collect |
| Day 1 "the extra zero" | The bank app: did the money really come? | The buyer's phone and account are in the name she gives, whether she paid or not |
| Day 2 "Minjun's broken phone" | The family chat: what Minjun wrote there himself that afternoon | The phone and the account are a real "roommate's" (in the scam, a lent identity) |
| Day 3 "the dentist's deposit" | The calendar: what Jiwoo wrote about today's appointment | The number is a mobile phone in the name she gives; the account is a payment company's virtual account, which hides whose it is |

In these cases the box in the day's newspaper says how to check that kind of call without giving the answer (in the newspaper case the box is the notice itself), and the legit caller points at the evidence when asked while the scammer evades.

Rules for writing scenarios:

- No scenario is solved by a rule alone. Every scam asks for something its legit twin also asks for, and every twin pair shares the caller, the story and the ask.
- Nobody asks for a verification code, PIN or password. Money moves by transfer to an account the player can inspect.
- Every clue exists before the call starts, in a place the player can reach from the room or the phone.
- From Day 2 on, a single check can mislead: a clean lookup result, a caller who knows personal data, a real name on a fake account.
- A one-clue case has its clue in exactly one place. Nothing else in the day's evidence tells its twins apart.

### 6.2 The cases

| Day | Date | Case (`scenario`) | Truth | Clues |
| --- | --- | --- | --- | --- |
| 0 | Mon 5 Oct, a substitute holiday | The unpaid gas bill (`gas`): "Team Leader Baek" of Mapo City Gas's billing team says September's bill (₩18,420) is unpaid and the gas goes off at five unless it is transferred now | Scam (the guided day) | 8 |
| 1 | Tue 6 Oct | The protected account (`protected`): "Manager Jeon" of Nuri Bank's "account protection team" wants ₩1,200,000 moved to a protected account | Scam | 2 |
| 1 | | The subscription fee (`paper`): Oh Sangchul, manager of the Seoul Daily's Mapo branch, says the branch no longer collects at the door and asks for October to December (₩54,000) into an account in his own name | Scam or legit | 1 |
| 1 | | The extra zero (`sale`): Bae Sujin, who is buying Jiwoo's old monitor on Neighbour Market for ₩60,000, says she paid ₩600,000 by mistake and wants ₩540,000 back before her own rent is due | Scam or legit | 1 |
| 2 | Wed 7 Oct, rent day | The new rent account (`rent`): "Choi Hyunwoo, the landlord's son", father in hospital, this month's rent (₩450,000) to his account today | Scam or legit | 3 |
| 2 | | Minjun's broken phone (`brother`): a caller with Minjun's voice, on a number Jiwoo does not know, needs ₩280,000 for a screen repair, sent to his roommate Lee Dohyun | Scam or legit | 1 |
| 3 | Thu 8 Oct | The held parcel (`parcel`): "Yoon Seora, Hangang Express customs desk", the AliStar monitor is held for ₩56,600 duty and VAT | Scam or legit | 4 |
| 3 | | The dentist's deposit (`dentist`): Moon Jihye, coordinator at Saebom Dental, wants a ₩30,000 deposit for tonight's 18:30 appointment | Scam or legit | 1 |

The twins share the caller, the voice, the opening and the ask; only the evidence and a few answers differ:

| | Scam | Legit |
| --- | --- | --- |
| Day 2 caller | 010-4127-8830: not registered, a prepaid phone opened 6 days ago | 010-2280-6614: CHOI HYUNWOO, the son's number in the lease |
| Day 2 account | Hanbit 620-118-449027: SEO JIYEON, a personal account opened 12 days ago ("my wife's") | Nuri 110-771-202358: CHOI HYUNWOO |
| Day 2 landlord | Posts in the residents' chat at 13:40 and 15:52 from home: fixing a light, rent on the usual account | Posted at 08:10 that he is in hospital and Hyunwoo collects the rent on this account; Jiwoo noted it on the calendar |
| Day 3 caller | 1588-5502: "KR Clearance Service", a business line opened 4 days ago (one digit off) | 1588-5520: Hangang Express customer centre, OFFICIAL NUMBER, the number on the delivery slip |
| Day 3 account | Hanbit 620-557-301144: KR CUSTOMS CLEARANCE, a business account opened 5 days ago | Nuri 5620-44-018830: HANGANG EXPRESS (CUSTOMS), a virtual account |
| Day 3 courier chat | "Cleared customs (taxes paid by the seller). Delivery Fri 9 Oct. Nothing to pay." | "Held at customs: ₩56,600 due", with the same account |
| Day 3 shop chat | AliStar: import taxes **included** at checkout | AliStar: import taxes **not included** |
| Day 3 calendar | Friday 9: "Monitor arrives! Hangang Express" | Thursday 8: "Monitor held at customs: duty 56,600 won, pay Hangang Express by 17:00" |

The one-clue cases differ in one place only. What the computer shows is the same in both truths:

| | Scam | Legit | The computer, either way |
| --- | --- | --- | --- |
| The subscription fee: the box in today's paper, "To our readers in Mapo" | The branch has a new manager since 1 October; Oh Sangchul no longer works for the paper; the fee is paid by giro slip only, never by a transfer over the phone | From October the branch no longer collects at the door; this week Oh Sangchul phones every reader (his number) for 54,000 won, to Nuri 110-457-803312 in his name | 010-8265-3317: OH SANGCHUL, a mobile phone since 2012. Nuri 110-457-803312: OH SANGCHUL, a personal account opened 2014. No reports |
| The extra zero: the bank history | No deposit today | Today 16:07, BAE SUJIN, +₩600,000, "Monitor (Neighbour Market)" | 010-3378-9024: BAE SUJIN, a mobile phone since 2017. Hanbit 620-481-207356: BAE SUJIN, a personal account opened 2016. No reports |
| Minjun's broken phone: the family chat | 15:52, Minjun, from his own phone: a photo of his library seat, "phone's on silent, so text me, don't call" | 15:22, Minjun, from the dorm PC: the screen is dead, he will call from his roommate Dohyun's phone; Mom: "I'm in a meeting till six, ask your sister" | 010-9046-2715: LEE DOHYUN, a mobile phone since 2021. Hanbit 620-204-771035: LEE DOHYUN, a personal account opened 2021. No reports |
| The dentist's deposit: the calendar | Thursday 8: "Dentist 18:30 → MOVED to Thu 22nd. I called them this morning" (the picture shows the 8th crossed out and the 22nd circled) | Thursday 8: "Dentist 18:30, wisdom tooth. Deposit 30,000 won: they ring this afternoon, it comes off the bill" | 010-5127-4406: MOON JIHYE, a mobile phone since 2018. Nuri 5620-71-336029: GAON PAY (payment agency), a virtual account that does not show which shop is behind it. No reports |

Each of these cases sets its own scene on the day card (`DayVariant.intro`): the Seoul Daily Dad subscribed Jiwoo to, the monitor sold on Neighbour Market, the rent already sent after class (so the day is not about the landlord), the dentist on the calendar all week.

Day 0 is small on purpose (18,420 won, a far deadline) and every tool answers it:

| Tool | What it shows on Day 0 |
| --- | --- |
| Newspaper | Today's warning: nobody collects a bill by phone |
| Computer, phone number | 010-7359-2046: not registered, a prepaid phone opened three days ago, four scam reports |
| Computer, bank account | Hanbit Bank 620-339-104772: HAN SUNGMIN, a personal account, not Mapo City Gas |
| Drawer | The September gas bill, stamped PAID (auto-pay, 28 September), with the company's real number, 02-555-0181 |
| Calendar | Today is a substitute holiday: offices are closed |
| Nuri Bank | 28 September: Mapo City Gas, 18,420 won out. The bill is paid |
| Chats | The residents' chat: unit 401 got the same call, and the landlord warns that the gas company never phones for money |
| Contacts | The caller is not there; a saved number shows its name when it rings |

Clues per variant (`DayVariant.clues`; each lists the events that count as finding it, see 7.5):

- **Day 0**: `paper`, `caller_number`, `account_owner` (the account lookup, or the name on the send step), `bill`, `calendar`, `bank`, `chat`, `contacts`, each with its `guide` line.
- **Day 1, the protected account**: `account_owner` (the account lookup, or the name on the send step), `caller_number` (the number lookup).
- **Day 1, the subscription fee**: `notice` (the newspaper).
- **Day 1, the extra zero**: `deposit` (the bank app).
- **Day 2, the new rent account**: `number` (the lease in the drawer, or the number lookup), `account_owner`, `landlord` (the residents' chat; on the legit day also the calendar).
- **Day 2, Minjun's broken phone**: `family_chat` (the family chat).
- **Day 3, the held parcel**: `courier_notice` (the Hangang Express chat, or the calendar), `taxes` (the AliStar chat), `number` (the delivery slip, or the number lookup), `account_owner`.
- **Day 3, the dentist's deposit**: `calendar` (the calendar).

### 6.3 The household and what is always there

- **Kim Jiwoo** (22), third year at Hanbit University, barista at Mangwon Roasters on weekends, lives alone in Mangwon Heights 302.
- **Family**: Mom (Park Hyejin) and Dad (Kim Dongsu) in Suwon, brother **Minjun** (19, first year in Daejeon), Grandma in Jeonju.
- **Building**: landlord **Choi Youngsik** (1F) and his son **Choi Hyunwoo**; the residents' chat.
- **Friends**: Seo Yuna (concert), café manager Han.
- **The newer cases' people**: Oh Sangchul (58), who runs or ran the Seoul Daily's Mapo branch; Bae Sujin (31), the buyer of Jiwoo's old monitor; Lee Dohyun, Minjun's roommate (or the name on a lent account); Moon Jihye, coordinator at Saebom Dental (or the name on a lent phone).
- **Baseline evidence** (`ContentBuilder.Household`): eight contacts; six chat threads with a week of history; Nuri Bank everyday account 110-302-558814 (about ₩1.28 million) and tuition savings (₩3,000,000) with a month of transactions; the lease, the paid gas bill, a delivery slip and a receipt in the drawer; the calendar with six notes; a directory of accounts and numbers the computer can look up (the family's, the landlord's, the bank's 1599-0000, Mapo City Gas's 02-555-0181, Hangang Express's 1588-5520, 112, 1332).

## 7. The run

### 7.1 Picking the case and the truth

`DayData.variants` holds one `DayVariant` per truth of each of the day's cases (call, phone, room, directory, clues, papers, rule, and the day card's intro when the case has its own). A scam and its legit twin share a `scenario`; the ids are `scam` and `legit` for the day's first case and `paper_scam`, `sale_legit`, `brother_scam`, `dentist_legit` and so on for the others. `DaySetup.Pick` chooses when the day starts:

1. **The case.** The one the player has finished least often over all weeks (`GameRun.TimesPlayed`, kept in PlayerPrefs `DCM.Played` and not cleared by a new week); among equals, at random, each case as likely as the next. So Play again on a finished day brings another of the day's cases.
2. **The truth.** At random among the case's truths.
3. **One rule.** A run always has a legit caller: on the last day that has a legit variant, if every earlier day was a scam, only the legit variants are in the pool.

`forceVariant` on `DayDirector` plays one variant (by id) for testing, and `GameRun.ForceNextVariant(id)` does the same once for the next day started (PlayerPrefs `DCM.ForceVariant`, cleared when read); the UI tour uses it to get Day 1's first case.

### 7.2 Evidence each day

`DaySetup.Compose` builds the day on top of `ContentBuilder.Household`, one timeline of Jiwoo's week that each day shows as it stands at its start time:

1. The variant's own phone, room and directory (cloned, so play never edits the assets).
2. History from the saved earlier days: money that left on a call (the savings cover what the everyday account can't) and yesterday's front page on the desk newspaper (the 2D panel and the printed prop).
3. Echoes (`DayData.echoes`): each reacts to one earlier day, the case it played (`scenario`), its truth and its outcome. They never hold a clue for today's case. Those marked `notify` pop up as notifications while Jiwoo sits at the desk. Money that moved after a call (a fee paid a day late, an overpayment that did arrive) is an echo too, repeated in every later day's list so the balance stays right all week.

| After | Echoes |
| --- | --- |
| Day 0, sent the money | Jiwoo telling the residents' chat she paid the fake bill, and the landlord's "report it to 1332"; the day card line "18,420 won, gone"; the transfer in the bank history |
| Day 0, hung up | Jiwoo posting the caller's number in the residents' chat, and the landlord's "well done, 302" |
| Day 0, ran out of time | The day card line: the "gas company" gave up at five and the gas is still on |
| Day 1, sent the money | Mom in the family chat and Jiwoo's reply; the day card line "probably gone for good" |
| Day 1, hung up | Yuna's reaction; Jiwoo warning the residents' chat with the number, and a neighbour's thanks |
| Day 1, ran out of time | A neighbour's warning about the same number in the residents' chat |
| Day 2 scam | The landlord's warning in the residents' chat (and his thanks, if Jiwoo hung up); if the fake son got the money, a direct message from the landlord and the real rent paid again |
| Day 2 legit | Hyunwoo saved in contacts; his thanks, or his understanding reply and the rent paid a day late; the landlord posting from hospital |
| Day 1 "the subscription fee" | Scam: Dad in the family chat ("report it to 112, and read the front page next time", or "well done, I pay by giro"). Legit: Dad's thanks, or his "he's real, send him the fee tomorrow" and the fee paid on Wednesday morning |
| Day 1 "the extra zero" | Scam: Jiwoo telling Yuna, and Yuna's reply. Legit: the ₩600,000 deposit stays in the history, and, if Jiwoo kept it, the ₩540,000 returned on Wednesday morning |
| Day 2 "Minjun's broken phone" | The rent paid on Wednesday afternoon, and what Minjun and Mom wrote that afternoon. Scam: the real Minjun ("What money? My phone is fine", or "Good thing you asked"). Legit: "Phone's alive!", or "that really was me" and the roommate paid back that evening |

### 7.3 Saving

`GameRun` keeps a `DayRecord` per finished day in PlayerPrefs (`DCM.Run`): variant, outcome, money, clues, times, the caller and any transfer. Beside the week it keeps how often each case has been finished (`DCM.Played`: day, scenario, times), which a new week does not clear (7.1). The title screen's Start button is Start on a new week (Day 0) and Continue once a day is recorded (the first unplayed day). **Case Files** lists the four days with what happened on each: a recorded day can be played again (a confirmation warns that the days after it are cleared when it ends), the next day can be played, later days are closed, and New week clears everything. The morning after offers the next day, the same day again or the menu.

### 7.4 Newspaper

Each variant authors its own *Seoul Daily* (`RoomContent.newspaper`) and one front page per outcome for the next morning (`DayVariant.papers`):

- **Today's paper** (the desk panel). Day 0: the story behind today's scam. Days 1 to 3: yesterday's case as it turned out, taken from the saved day (Day 1 keeps its own warning story when no Day 0 was played, as when the Room scene is opened directly). Beside it the warning box, local news and ads.
- **The next morning** (End scene): the headline and story for what the player did, beside the case summary.
- **The desk prop.** The folded paper on the desk is a printed texture. `Content → Print Desk Newspapers` exports every front page that lies on the desk the next day (every outcome of every variant of Days 0 to 2) to `Tools/ArtGen/papers.json`, `tex_prints.py papers` prints them in the same style as the first day's paper to `Art/Textures/Papers` (`*_ko` in Korean), and each `EndPaper.print` points at its print; `DayDirector` puts it on the prop.

| Rule (shown the next morning and in the week summary) | Taught by |
| --- | --- |
| Check before you pay. A caller's story is only a story. The number, the account, your own papers, your calendar, your bank and your chats tell you what is true. Check them, then decide | Day 0 |
| Banks never "protect" your money. A bank never moves it to a "safe" or "protected" account. Look up the account and the caller's number first: a stranger's name on an "official" account means stop | Day 1 |
| A new rent account? Check the lease. No reports doesn't mean safe: check the caller's number and the account holder's name against your lease, and ask the landlord's own chat | Day 2 scam |
| Checking works both ways. When the number in your lease, the landlord's own message and the name on the account all match, the change is real. Check, then act | Day 2 legit |
| Is anything due? Read the courier's own notice and check who owns the number and the account. If the courier says nothing is due, nothing is due | Day 3 scam |
| Pay duty only to the courier. Real duty shows up in the courier's own notice, with an account in the courier's name. When they match the call, paying is safe | Day 3 legit |
| A real name is not permission. Someone collecting for a company you pay? Looking him up only proves that his name is real. The company's own notice says who may collect, and how | The subscription fee, scam |
| The company's own notice settles it. When it names the collector, the amount and the account, paying him is safe. Read the notice, then act | The subscription fee, legit |
| "I sent you too much"? Open your bank. Money that never arrived cannot be returned | The extra zero, scam |
| Return it to the name it came from. When the deposit is in your own history under the caller's name, the extra is theirs | The extra zero, legit |
| Ask them where you always talk. A voice can be copied and a lookup can come back clean: ask family in the chat you already share | Minjun's broken phone, scam |
| Their own message explains the strange number. The same check clears a real call. Check, then help | Minjun's broken phone, legit |
| What did you arrange yourself? A caller who knows your appointment is not proof. If your own notes don't match the call, don't pay | The dentist's deposit, scam |
| Your own note is evidence too. When it says the same as the caller (the time, the amount, the call itself), the request is real | The dentist's deposit, legit |

The wording follows the public warnings of Korea's Financial Supervisory Service and National Police Agency.

### 7.5 Clue tracking

`ClueTracker` marks a clue as found when the screen showing it is opened during the day. Panels and apps report what the player looked at through `ClueEvents`:

| Event | Raised when | Target |
| --- | --- | --- |
| `PanelOpened` | A room panel opens | The panel's name, e.g. `Calendar` |
| `DocumentViewed` | A drawer document is shown | The document's title |
| `NumberChecked` | The computer looks something up | The phone number or account |
| `RecipientShown` | The verdict's send step shows who gets the money | The account |
| `ChatRead` | A chat thread opens | The thread's id |
| `BankOpened` | The bank app opens | — |
| `ContactsOpened` | The contacts app opens | — |

Each `ClueDef` lists the events that count. Finding clues does not change the outcome and the player is not told during the day; the next morning's case summary lists every clue, found (✓) or missed (with where it was).

### 7.6 Summary

After Day 3, **Your week** lists each day's case and truth (Day 0 included), what the player did, right or wrong and clues found, then right calls, money lost to scams, the savings before and after, a rating (Scam-proof, Careful, At risk) and the rules learned. Buttons: New week, Main menu.

## 8. Architecture

### 8.1 Folders and assemblies

```
Assets/_Game/
  Scripts/
    Data/       ScriptableObject definitions (day, conversation, phone, room, directory, voice bank),
                Loc + LocKo (language, Korean UI strings), Facts (numbers and money)
    Flow/       DayDirector, DaySetup, GameRun (save), CallDirector, ClueTracker, TutorialGuide, SceneFlow
    Player/     FirstPersonController
    Gameplay/   Interactor, Interactable, TitleCamera
    Rendering/  DCMLook
    Audio/      GameSettings (volumes, look sensitivity), MusicPlayer (loop + tension layer), VoicePlayer
    UI/         UI Toolkit views: Core (UIKit builders, shared elements, skin, clock, clipboard, SFX),
                Panels (newspaper, drawer, calendar, computer), Phone (three apps, call screens),
                Call (transcript, call HUD, verdict panel), Hud (HUD, tutorial card, toasts),
                Day (DAY card, CASE OPENED, deadline, fade), Menus (title, pause, settings, next morning),
                UIManager, UITour (development)
    Editor/     Art pipeline, UI pipeline, content builder, audio pipeline, scene pipeline, GameCapture
  UI/
    Fonts/      OFL fonts with their licences, generated font assets
    Sprites/    Generated 9-slice frames, icons, portraits, document images, the phone and the hand,
                call buttons, status icons, app tiles, the title logo and the menu icons
    Uss/        Theme.uss plus Phone, Panels, Call and Menus style sheets
    Settings/   Panel settings, text settings, theme style sheet, UISkin (every UI texture by name)
  Audio/
    Music/      Generated loops (Tools/Audio/music.py)
    Voices/     Generated voice clips per voice + manifest.json (Tools/Audio/tts.py)
  Data/
    Day0/       One truth: phone, directory, room, the call, the day (clues, next-morning papers, rule)
    Day1..Day3/ Each day's asset plus a call, phone, room and directory per variant
                (Day1_*, Day1_Paper_Scam_*, Day1_Sale_Legit_*, Day2_Scam_*, Day2_Brother_Legit_*, Day3_Dentist_Scam_*, …)
    Day*/ko/    The same in Korean (Day0_ko, Day2_Scam_Call_ko, …)
    VoiceBank   Every voice clip by voice and text
  Resources/DayCatalog  The days of the run, in order (found without scene references); DayCatalog_ko in Korean
  Art/          The room model, textures, materials, shaders; Textures/Papers holds the desk newspaper prints
  Prefabs/
  Scenes/Home.unity, Room.unity, End.unity
```

Assemblies: `DontCallMe` (runtime), `DontCallMe.Editor`.

### 8.2 Game flow

Three scenes; `SceneFlow` loads them and hands the day's result from Room to End.

```
Home (TitleScreen) → Room (DayDirector + CallDirector) → End (EndScreen) → Room again or Home
```

`DayDirector` runs a day. `Prepare` (called by `UIManager` as it wakes up, so the phone and room show the day's content from the first frame) takes the day from `GameRun.PendingDay` (or its own field, Day 1, when the Room scene is opened directly; a run starts at `GameRun.FirstDay`, 0), picks the case and the truth and composes the evidence (7.1, 7.2). Then: the DAY card with the case's intro and the echoes' lines, the seated start (a tutorial day: the guide; other days: the echoes buzz in), the forced call, the CASE OPENED card when `CallDirector` reports the hold, the investigation against the deadline (beats, music tension, screen-edge pressure, ticking), and the fade to the End scene with a `DayResult` (outcome, ending, money, time taken, clues found), recorded in `GameRun`. `CallDirector` plays the call: voiced lines, decisions, the hold and its questions, the verdict and the endings.

### 8.3 Main classes

| Class | Responsibility |
| --- | --- |
| `DayDirector` | The phases of a day (Waiting, Intro, Seated, OnCall, CaseCard, Investigating, Ending, Done), the deadline, the pressure, the record |
| `DaySetup` | Picks the variant (the day's case, then its truth); composes the day's phone, room and directory from the variant, the history and the echoes |
| `GameRun` | The saved run: one `DayRecord` per finished day; how often each case has been finished; the variant a tool asked for |
| `CallDirector` | Plays a `ConversationData`: ring, lines with voice, decisions and patience, the hold, questions, the verdict's actions, endings |
| `ClueTracker` | Which of today's clues the player has seen (7.5) |
| `TutorialGuide` | The tutorial day's guide: the step for the current state (before the hold from the phase, during it from the first clue not found), what the last place showed, the spotlight, when the call may start (2.1) |
| `UIManager` | The UI document: panels, the phone, the transcript, HUD, cards, pause and confirmations; locks the player's input while a panel is open |
| `Interactor`, `Interactable` | What is under the cursor, the prompt, opening its panel; the outline and light on usable objects |
| `Loc` | The language and the UI's Korean strings (10) |

### 8.4 Input

- Actions in `Assets/InputSystem_Actions.inputactions`: `Drag` (drag to look), `Phone` (Tab), `Back` (Esc, right mouse button in panels), `Interact` (E, plain press) and the mouse click. `1`–`4` pick a decision option or a question.
- Looking: hold a mouse button and drag to turn. Drags shorter than a few pixels count as clicks, and presses that start on the UI never turn the view. UI that is hidden or faded out never catches the pointer, so a press anywhere on the room always looks or interacts.
- A press on the room while the phone is up puts the phone away and the same drag turns the view; a ringing phone stays up. A click on the dimmed room around a panel closes it, and clicking the call HUD brings the phone back.
- `UIManager` keeps the open panel. Opening one calls `FirstPersonController.SetInputLocked(true)`; closing it unlocks. Esc goes back one screen in the phone, then closes the panel.
- `Interactor` raycasts from the mouse cursor and takes the nearest interactable on the ray, anywhere in the room: furniture and props on the way never block it, so whatever glows can be clicked. It shows the prompt and opens the panel on click or E. A click that ended a drag (`FirstPersonController.PressWasDrag`) is ignored; a press only becomes a drag once the pointer is 10 px away from where it went down. `Interactor` and `Interactable` live in their own files: Unity only serialises a component whose class matches its file name.

## 9. Data model

```
DayCatalog (SO)        days[]   in Resources, loaded by name (DayCatalog_ko in Korean)
DayData (SO)           day, tutorial, dateLabel, shortLabel, place, startTime, intro, ringDelay, nextDateLabel, builtWith
                       variants[]   DayVariant { id (scam | legit | paper_scam | …), scenario (the case), intro,
                                    conversation, phone, room, directory,
                                    clues[] { id, text, where, guide (tutorial day), when[] { ClueEvent, target } },
                                    papers[] { outcome, headline, subhead, body, verdictNote, print },
                                    ruleTitle, rule, ruleSource }
                       echoes[]     DayEcho { afterDay, scenario (that day's case; empty for any),
                                    truth (Any | Scam | Legit), outcomes (flags),
                                    kind (Chat | BankTransaction | Contact | DayCard),
                                    from, sender, avatar, when, title, text, amount, outgoing, notify }

ConversationData (SO)  caller { displayName, number, inContacts, portrait, voice }, isScam
                       nodes[]      { id, lines[], decision?, next, holds }
                                    Line { speaker, text, spoken (what the voice says), facts[], deliver[] }
                                    Decision { kind (Ask | Test | Commit), patienceSeconds, a, b, pressure[] }
                       caseInfo     { caseTitle, claimedIdentity, ask, deadline, deadlineReason, objective }
                       verdict      { goAlong, goAlongDetail, refuse, refuseDetail, goAlongLine, refuseLine }
                       questions[]  { label, playerLine, answer Line[], endingId? }
                       beats[]      { at (in-game time), line }
                       endings[]    { id, verdict, timedOut, moneyDelta, lines Line[], consequence }
                       actions[]    { kind (Transfer | HangUp), target (account), bank, amount, endingId }

PhoneContent (SO)      contacts[]   { name, number, memo, portrait }
                       chats[]      ChatThread { id, title, avatar, group,
                                    messages[] { sender, avatar, when, text, outgoing, photoCaption, facts[] } }
                       bank         { bankName, accounts[] { name, number, balance },
                                    transactions[] { when, counterparty, memo, amount } }

RoomContent (SO)       newspaper    { masthead, issue, dateLine, headline, subhead, photo, caption, body[],
                                    warningTitle, warningText, local[], ads[], print }
                       drawer[]     DocumentData { title, kind, issuer, fields[] { label, value, isFact, factKind },
                                    body, stamp, image }
                       calendar     { title, image, todayLabel, today, entries[] { day, label, text } }

WorldDirectory (SO)    what the computer can look up
                       accounts[]   { bank, number, holder, note, reports[] }
                       numbers[]    { number, owner, note, official, reports[] }

DayRecord (saved)      day, variant, scam, outcome, moneyDelta, savingsAfter, clues[], cluesTotal, decidedAt,
                       minutesTaken, callSeconds, callerNumber, callTime, transferTo / Bank / Account
Cases played (saved)   day, scenario, times   kept across weeks, for picking the least-played case
VoiceBank (SO)         entries[] { voice, text, AudioClip }   looked up by voice + spoken text
```

- `Fact { kind (Phone | Account | Name | Case | Url | Amount | Text), value }` is what a copy chip holds.
- `spoken` lets the voice read an account number digit by digit while the transcript shows `110-900-551207`. Voice clips are found by voice and text, so a changed line only needs the voice pipeline run again.
- Without a clip, a line stays on screen for a time that follows its length.

## 10. UI

All panels are UI Toolkit: one `UIDocument` with a panel settings asset scaled for 1920 × 1080 and one theme (`Assets/_Game/UI/Uss/Theme.uss` plus per-area sheets). Templates are small C# builders (`UIKit`, `PhoneScreen`, `RoomPanel`) that only set classes and content, so a new screen or document is a few lines of layout and all styling stays in USS. The look follows `references/UI`: cream paper with hand-inked outlines, dusty blue insets, pixel-art portraits, chunky square buttons with a drop shadow.

| Panel | Notes |
| --- | --- |
| Newspaper | Paper sheet, *Seoul Daily* masthead, front page with a halftone photo, warning box, local news, ads |
| Drawer | Documents as paper on a wooden tray: a tab per document, previous and next; numbers and accounts as chips; a PAID stamp as an image (납부완료 in Korean) |
| Calendar | The pharmacy calendar's picture beside a paper list: today's date, then each handwritten note with its day (past days greyed, today highlighted). One truth of the dentist case swaps the picture for one with the 8th crossed out and the 22nd circled |
| Computer | A laptop screen with a browser window (tab, address bar `https://checkfirst.kr`): two tabs, a field with Paste and Check, chips "from the call and your notes", and a result card (owner or holder, registration note, OFFICIAL NUMBER badge, report count in red or green, the reports, the "no reports doesn't mean safe" tip). A number pasted into the wrong tab switches tabs by itself |
| Phone | Inked frame (copper rim, dark bezel, notch) + status bar (clock, signal, battery) + home screen + three apps, held by a hand (palm behind the phone, fingers and thumb in front). Screens share list and detail templates; a long title ends in an ellipsis |
| Incoming call | The caller's round portrait with a pulsing ring on the phone's dark gradient, "INCOMING CALL", name or "Unknown", number, a shaking phone and slide to answer (no decline) |
| In call | The portrait, "ON CALL", the name, number and call timer, and three large buttons that stand out on the dark screen: Apps and Speaker (light slate) side by side, the red hang-up under them (dimmed during a case: "Verdict on the call"); a green "Return to call" bar in the apps |
| Transcript | Chat log left of the phone: portraits, bubbles with times, typing indicator, fact chips, two decision buttons, decision timer |
| Call HUD | Top centre while the phone is down: caller, latest line, "Holding the line · Tab to ask or decide" |
| Decision timer | Ring with seconds; amber at 50%, red and pulsing at 25%; pressure overlay at the screen edges |
| HUD | Top left: day, date and clock, with the deadline under it. Bottom right: the phone button with a notification badge (hidden while the phone is up). Bottom left: the control hints. The interact prompt at the cursor |
| Tutorial card | Day 0: a yellow note on the left, HOW TO PLAY, the step number, a green line with what the last place showed, and one instruction. It sits over the case file's shade and beside the room panels |
| DAY card | Black card: DAY 0 (with a yellow TUTORIAL tag) or DAY 1…, the date, time and place, a line of intro (the day's, or the case's own) and the echoes' lines; click or wait |
| CASE OPENED | Paper case file with a tab (CASE #01), the caller's portrait, "Says they are", "Wants you to", "Deadline", "Your job" and Start investigating |
| Deadline | Under the clock: deadline time, minutes left and a bar; amber at half, red and pulsing in the last quarter |
| Hold | The transcript footer while the caller holds: the questions still open (1–4) and the verdict panel |
| Verdict | Dark strip, YOUR VERDICT, gold / red buttons with an icon, a label and a detail line; a coloured confirmation step (send: amount and recipient's name; hang up) |
| Title screen | Over the room at dusk (slow camera drift): the DON'T CALL ME! sticker logo (cream letters, red halftone shadow, a ringing red telephone), the green Start button (Continue once a week is under way), Case Files, Settings and Quit with sticker icons, and a ringing phone whose slide also starts the game; lo-fi music |
| Case Files | Paper card on the title screen: one row per day (portrait, day and date, case title, SCAM or REAL, what the player did, clues, right or wrong) with Play or Play again, then New week and Back |
| Pause and Settings | Paper card over the frozen room: Resume, Settings, Main menu; the language (English or 한국어; during a day the switch asks first, then restarts the day in the new language) and sliders for music, sound effects, voices and look sensitivity (saved) |
| Next morning | The Seoul Daily front page for the outcome beside the case summary: SCAM or REAL and RIGHT / WRONG CALL or TOO LATE stamps, money kept or lost, decided at, time taken, clues with ✓ or where they were, the rule learned, the next day, Play again and Main menu. Staged for suspense (`EndScreen`): paper and case card arrive with the headline, the stamps and everything under them hidden and the buttons off; at 2.3 s the screen goes dark except for a spotlight that narrows on the empty spot of the first stamp (a dark sheet with a clear hole, anchored in the stamps' row) while a snare drumroll quickens and swells for 3.2 s over a low drone and a heartbeat; at 5.5 s the truth stamp comes down in the light with a boom, a rimshot and a cymbal, and the headline appears; the light glides to the second spot under a short roll, and 1.3 s after the truth the verdict stamp lands with a bright phrase for a right call or a sinking one for a wrong call; then the lights come up with the story, the numbers, the clues one by one, the rule and the buttons. The morning's music only starts after the verdict |
| Toasts | Top right, informative ones never catch the pointer |

Fonts (all OFL, in `Assets/_Game/UI/Fonts` with their licences): Nanum Gothic for the UI (Bold is the base weight; ExtraBold for clocks, timers and counters, its digits all have one width), Do Hyeon for headings, names and the title menu, Gaegu Bold for handwriting, DM Serif Display and Crimson Text (SemiBold for body text) with Nanum Myeongjo Bold for the newspaper. Nanum Gothic, Do Hyeon, Gaegu and Nanum Myeongjo have Hangul; the Latin-only serifs fall back to Nanum Myeongjo for Korean text.

**Readable text.** The game is often played in a small window, where thin strokes break up and grey ink fades into the paper. So: body text is near-black (`--ink` #1A1620, `--ink-soft`, `--muted` #453C42, no mid-greys on paper) or cream on the dark screens; no Regular weights for running text (Nanum Gothic Bold, Crimson Text SemiBold, Nanum Myeongjo Bold, Gaegu Bold); numbers that must be read exactly (phone numbers, accounts, amounts) are in Nanum Gothic; instructions the player acts on (the case file's "Your job", the rule learned) use the same plain font as the rows around them, handwriting is kept for Jiwoo's own notes; the control hints sit on a dark strip. Room panels open a little right of centre, clear of the day tag, the deadline and (on Day 0) the guide's note; their close button sits beside the top right corner, a small gap from the edge, so it never covers the page.

**Language.** `Loc` (Data) holds the language (PlayerPrefs `DCM.Language`) and translates the UI's own strings: code passes the English text, `Loc.T("…")` or `Loc.F("… {0}", …)`, and gets it back in Korean from the table in `LocKo.cs` (a missing entry falls back to English and warns in the editor). The content is not translated at run time: `ContentBuilder` writes every day twice, the Korean assets in a `ko` folder next to the English ones, and `DayCatalog.Load()` picks `Resources/DayCatalog_ko` when the language is Korean. Pictures with writing on them (the calendar, the receipt) have Korean versions (`*_ko` sprites), and the desk newspaper has Korean prints. Korean wraps between words (the panel's text settings use the modern Hangul line-breaking rules). Changing the language reloads the title screen, or restarts the current day after a confirmation, with the same case and truth (not another of the day's cases). Chat text avoids emoji: the UI fonts have none.

## 11. Art and audio

**Look.** Warm, cosy and lived-in, so the calls feel intrusive. Comic-book ink outlines and hatching over hard-banded toon shading, a retro print grade, late-afternoon sun through the window and dust in the sunbeam. People are pixel art in the style of `references/art/ref4.png`, in the room's photos and in the callers' portraits.

**3D and textures.** The bedroom, its props and textures are generated from code in `Tools/ArtGen` (Python for textures, Blender for the model, a Unity editor pipeline for materials, prefab and scene). See `Tools/ArtGen/README.md`. Hand-made Blender models can still replace a generated part: keep the object name, and the pipeline remaps its materials.

**2D.** UI frames, icons, caller portraits, document images, the phone and the hand that holds it, the round call buttons and the slider's track, app tiles, the title logo and the title menu's sticker icons are generated by `Tools/ArtGen/ui_art.py` in one style: flat colour, a wobbly ink line, a soft offset shadow, halftone and paper grain for the retro print feel. Only people (portraits and the room's photos) are pixel art. The UI pipeline imports the sprites.

**Audio.** See `Tools/Audio/README.md`.
- Voices: every voiced line is exported from the content in both languages (`Tools/Audio/voice_lines.json`, 376 lines), spoken by a macOS `say` voice per caller and language (eight callers; the table is in `Tools/Audio/README.md`), shaped like a phone line (band-pass, compression, line hiss) and imported into the `VoiceBank`. The files go in `Audio/Voices/<voice>/<hash>.wav`. A scam and its legit twin share the voice.
- Music: generated loops (`Tools/Audio/music.py`): lo-fi keys for the title, a calm bed and a tension layer of the same length for the investigation (crossfaded by the time left), and a bright morning loop for the next morning, which waits for the verdict to be stamped.
- SFX (synthesised in `Sfx`, replaceable by clips of the same name): ringtone, vibration, slide-to-answer, hang-up tone, message sound, typing, paper rustle, click, heartbeat, timer tick, stamp, and the reveal's five: `suspense` (the long drumroll), `reveal` (the hit), `roll` (the short drumroll), `reveal_good` and `reveal_bad` (the verdict).

## 12. Testing

- **UI tour** (`Tools > Don't Call Me > UI > Capture UI Tour`): enters Play mode, screenshots every room panel and phone app into `Temp/UITour`, makes both lookups on the computer, then plays Day 1's call twice (the Room scene opens on Day 1, and the tour asks for its first case, "the protected account") (questions and hang up; send the money) and checks each step (the lookups show the right names and reports, the call rings, the hold starts, the endings say what was done). Failed checks are logged as `[UITour] FAILED`. It runs in English (it finds buttons by their English labels) and puts the chosen language back afterwards.
- **Offline compile**: both assemblies compile outside the editor with Unity's own compiler settings (the snippet is in `CLAUDE.md`), so a change can be checked while Unity is busy.
- **Play-through** (Day 0, played with a call screen that had answer and decline buttons, since replaced by the original slide to answer): Day 0 played end to end in English and in Korean at 1920 × 1080 and 1280 × 720: the guide's thirteen steps, declining the call and its ringing again, each of the eight checks, both verdicts, the next morning, and Day 1 with Day 0's echoes; then Day 3 into the week summary with four days, Case Files (Play, Play again, New week), and the UI tour (27 screenshots, no failed checks).
- **Play-through** (after the scope cut): Days 1–3 played end to end in English and in Korean, covering both truths of Days 2 and 3, all three outcomes (send the money, hang up, out of time), the week summary, Day 1's call starting by itself when the newspaper is never read, and a saved week from before the cut continuing into Day 3.
- **Play-through** (case pools): each of the four new cases played from the DAY card to the next morning by stepping the paused game, in English or Korean: "the subscription fee" (scam hung up, legit paid), "the extra zero" (legit sent back, scam out of time), "Minjun's broken phone" (scam sent, legit hung up) and "the dentist's deposit" (scam hung up, legit paid) with the week summary. In each, the clue was not found after the computer's two lookups and the other tools, and was found on opening its one place. The evidence screen of every new variant was captured in both languages; the days after every outcome of the new cases were composed and their day card, chats, bank history and desk paper read; the pick was tallied (cases even, truths even, only legit variants on Day 3 after three scams); and the UI tour ran with no failed checks.
- **Playtests with the target audience** (young adults living alone, at least five people), measuring: right verdicts on Day 1 vs Day 3; whether they look up the account and the number without being told on Days 2 and 3; whether they can name the rules after one run; where they get stuck on controls or UI.
- Later: a content validator (every clue's target exists in that day's content, every option leads somewhere, both truths of a twin share the ask) and EditMode tests for `DaySetup` and `ClueTracker`.

## 13. Build phases

| Phase | Work | State |
| --- | --- | --- |
| 1. Room and movement | Generated bedroom, first-person controller, colliders | Done |
| 2. UI templates | UI Toolkit theme and assets, interactor, room panels, phone, incoming call, transcript, decision timer, call HUD | Done |
| 3. Conversations | Day flow (seated start, CASE OPENED, the hold with questions, beats and the deadline), voiced lines, title / pause / settings / next-morning scenes | Done |
| 4. Day loop | `GameRun` (save), `DaySetup` (truth, history, echoes), clue tracking, reveal, the week summary; Days 2 and 3 with scam and legit twins | Done |
| 5. Korean | Language option, Korean content, voices, prints and line breaking | Done |
| 6. Scope cut | Three apps, four room objects, the computer's two lookups, the calendar, 2 / 3 / 4 clues, the guide, highlights, the hand | Done (2026-10-01) |
| 7. Day 0 and the title | The guided practice day that uses every tool, Case Files, the title's Start button and menu rows, readable text. A pixel-art phone and title, then a navy call screen with answer and decline buttons, were tried and dropped the same day: the phone is the original one (portrait, slide to answer), later given a plain dark gradient background and larger call buttons | Done (2026-10-01) |
| 8. Case pools | A day brings one of several cases (least played first); four one-clue cases whose clue is in the newspaper, the bank app, the family chat or the calendar; echoes per case | Done (2026-10-01) |
| 9. Polish and playtest | Target-audience playtests, timing and difficulty tuning, ambience | Next |

### 13.1 How to build and try it

- **Content**: `Tools > Don't Call Me > Content > Build Days` writes `Assets/_Game/Data/Day0..Day3` with their Korean copies in each day's `ko` folder, `Resources/DayCatalog` and `Resources/DayCatalog_ko`; `Print Desk Newspapers` prints the desk paper for each front page in both languages (Python, see `Tools/ArtGen/README.md`). When `ContentBuilder.Version` is newer than the one recorded in `Day1.asset` (`builtWith`), the editor rebuilds the content, the prints and the voices by itself after the next compile.
- **Audio**: `Tools > Don't Call Me > Audio > Run Voice Pipeline` (export lines, TTS, import) and `Generate Music`.
- **Scenes**: `Tools > Don't Call Me > Scenes > Build Home and End Scenes` (also sets the build order Home, Room, End); the Room scene's UI, directors and interactables come from `UI > 4. Set Up Game UI in Room Scene`.
- **Play**: open Home and press Play (Start begins a week at the guided Day 0, Continue picks up the saved week, Case Files plays any day that is open), or open Room to start straight at the day set on `Flow/DayDirector` (Day 1; set `forceVariant` to a variant id such as `scam`, `paper_legit` or `sale_scam` to test a case and truth; from code, `GameRun.ForceNextVariant(id)` before `SceneFlow.PlayDay(n)`). To test a later day with echoes, write earlier `DayRecord`s into PlayerPrefs `DCM.Run`. To play in Korean, pick 한국어 in Settings or set PlayerPrefs `DCM.Language` to 1 (0 is English).
- **Check**: the UI tour (12). `DontCallMe.Editor.Tools.GameCapture.Capture(path)` renders a 1920 × 1080 screenshot of the paused game even when the Game view is hidden.

## 14. Risks

| Risk | Mitigation |
| --- | --- |
| Players can't find the investigation tools, or don't know what they are for | Day 0 uses every tool once on a real case and says what each one showed; usable objects glow; the computer offers the call's number and account as one-click chips |
| The lookup gives the answer away every day | Only "the protected account" has fraud reports. In the rent and parcel cases the lookup returns a clean result and a name, which only means something next to the lease, the slip or the chats. In the four one-clue cases the lookup is the same in both truths, and the answer is in the newspaper, the bank app, the family chat or the calendar |
| Checking everything is always right, so there's no tension | The caller's deadline, his pressure lines, and questions that cost time |
| Players refuse every call | A run always has one legit caller, and refusing a legit call has a visible cost in the next paper and the next day's echoes |
| Few cases after the tutorial | Days 1 to 3 draw from seven cases (thirteen truths), least played first, so a second and a third week play differently. A new case is one more file in `ContentBuilder` and one entry in its day's variants (6.2) |

## 15. Cut from the prototype

Removed on 2026-10-01 to keep one clear loop (call → look up → compare → verdict). Each can come back once the core is tested with players:

- **Phone**: the Phone app (recents, keypad, calling out and call-backs, the Verify verdict), Messages (SMS), the CheckFirst app (now the computer), Browser, Parcels, Mail, and transfers from the bank app.
- **Room**: the wallet, the cork board's notes and the notebook as panels (they remain as props).
- **Conversations**: chat scenarios (a scam that arrives as a chat), the sample chat and the demo call.
- **Run**: the 5-day run drawn from a pool (Easy, Medium, Medium, Hard, Hard, seeded, with constraints), ambient filler evidence per day, the debug menu.
- **Scenario ideas** not built: gas inspection and its "boiler emergency" twin, "Mom's broken phone" (the brother on a friend's phone is now Day 2's "Minjun's broken phone"), the courier at the door, the held card payment, "Investigator Kang" and Detective Oh, the money-mule job and the real internship. Their evidence was designed around the tools above (this file's history before 2026-10-01 has the full lists) and would need rewriting for the four tools the prototype keeps.

Also later: a mobile build.
