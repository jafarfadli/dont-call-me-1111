# Don't Call Me! — Technical Plan

Game concept and research: `references/idea.md`. This document describes what we build and how, for the first playable prototype.

**Goal.** Young adults in Korea who live on their own (students, first jobs) handle their own banking and are frequent targets of institution-impersonation, job and rent scams. In the game they feel a scam call's pressure in a safe place and learn to check a caller's claims against evidence before a real call arrives. The player should leave able to name the rules and, more importantly, having practised the checks that expose a scam: whose name is on the account, who really owns the number, what the landlord or the courier wrote themselves.

> The setting and the audience moved from Indonesia and Vietnam to Korea on 2026-09-30. `references/idea.md` still describes the earlier version; its structure (twin cases, newspaper as rulebook, evidence in the room) carries over.

> **Scope cut, 2026-10-01.** The prototype was reduced to what one investigation needs: three phone apps (Contacts, Chats, Nuri Bank), four things to use in the room (newspaper, wall calendar, desk drawer, and a computer with two lookups), calls only, a guided first day, and two, three and four clues on Days 1, 2 and 3. What was cut is listed in section 15.

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
| Tutorial | Day 1 is guided: a card walks the player through looking around, reading the paper, answering, replying, the two lookups on the computer and the verdict (2.1) |
| Conversations | Calls only. The transcript sits next to the phone. At key moments the player picks one of **two** responses, with a visible decision timer; while the caller holds, the player can put questions to him (each costs time) |
| Investigation | Callers never ask for a code or password. They ask for things legit callers also ask for (a transfer), so the verdict comes from checking evidence: who owns the account and the number (looked up on the computer), what the lease or the delivery slip says, what the landlord or the courier wrote in the chats, what Jiwoo noted on the calendar |
| Verdict | Given only on the call, in the **verdict panel** under the transcript while the caller holds: go along (send the money) or refuse (hang up). Each kind has a fixed colour (gold, red) so the colours never hint at the answer. The rest of the UI is for investigating: during a case the phone's hang-up button is off, and the bank app only shows balances and history |
| Voices | Callers are voiced with pre-generated TTS in the player's language (macOS `say` voices through a phone-line filter), one voice per character and language; the transcript reveals each line as it is spoken. The player's lines are text only |
| Menus | Title screen (Start, Settings, Quit, and a ringing phone whose slide also starts the game); pause on Esc (Resume, Settings, Main menu); Settings: language (English or 한국어), music, sound effects, voices, look sensitivity |
| Scenarios | Three cases, one per day, each harder than the one before: Day 1 "the protected account" (always a scam, 2 clues), Day 2 "the new rent account" (3 clues) and Day 3 "the held parcel" (4 clues), the last two a scam or its legit twin at random (see 6) |
| Run | 3 days (Tue 6 – Thu 8 October), saved as the player goes (Continue on the title screen), ending with a week summary. Each day's truth is revealed in the next morning's newspaper |
| Continuity | A day remembers the earlier ones without changing its case: the money that left stays gone, yesterday's case is on the desk newspaper, and **echoes** (chat messages, bank transactions, a saved contact, a line on the day card) react to how each day went |
| Content data | ScriptableObjects, written by an editor tool (`ContentBuilder`) from code |
| Language | English or Korean, chosen in Settings (title screen or pause menu; the first launch follows the system language). Everything exists in both: UI, dialogue and voices, documents, chats, lookup results and the desk newspaper prints. Korean names, places and won amounts in both. Banks, couriers, shops, companies, people and their numbers are fictional; the public numbers 112 (police), 119 (fire and ambulance) and 1332 (financial fraud hotline) are real |

## 2. A day in the game

```
Title screen (Home scene) → Start
  → DAY card over black: "DAY 1 · Tuesday, 6 October · 16:20 · Mangwon-dong, Seoul" and a line of intro
  → Jiwoo sits at the desk (the view is lower and turns within a range; no walking yet)
  → Day 1: the guide asks the player to look around and read the newspaper; the phone rings once it is closed.
    Days 2 and 3: yesterday's aftermath buzzes in (chat and bank notifications), then the phone rings
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
  → The next morning (End scene): the Seoul Daily reveals who really called, beside the case summary
    (verdict stamp, money, time taken, clues found and where the missed ones were, the rule learned)
  → The next day, the same day again, or the main menu. After the last day: the week's summary
```

- A ringing call cannot be declined or ignored: the phone stays up and rings until the player slides.
- The game clock runs at 8 real seconds per minute and stops on the DAY and CASE OPENED cards and while paused. Every caller's deadline is 17:00, about four real minutes of investigation.

### 2.1 The first day's guide

Day 1 runs with `TutorialGuide`: a yellow note on the left of the screen, "HOW TO PLAY · n/7", one instruction at a time.

| Step | The card says |
| --- | --- |
| 1 | Hold the mouse button and drag to look around |
| 2 | Things you can use glow: click the newspaper, read today's warning, close it |
| 3 | The phone is ringing: drag the green button to answer |
| 4 | Pick a reply (click, or 1 or 2), then listen |
| 5 | This is the case: read what he wants, then Start investigating |
| 6 | Walk to the glowing laptop; check the account he gave, then his number |
| 7 | Raise the phone (Tab) and give the verdict: send the money, or hang up |

- The step shown follows what is happening in the game, not a script, so a player who skips ahead or does things in another order is never stuck.
- The object a step is about blinks (the newspaper in step 2, the laptop in step 6).
- The call waits until the newspaper has been opened and closed (at most 75 seconds), and the game clock stays stopped until then, so reading costs no time.
- Days 2 and 3 run without the guide; a toast ("Investigate: the caller is holding the line…") opens the investigation instead.

## 3. The room

3.6 × 3.8 m, 2.4 m high: the bedroom of a villa in a humid part of Seoul that was never renovated (see `Tools/ArtGen/README.md` for how it is built). Old vinyl floor, damp-stained painted walls, cherry-brown door and mouldings, a PVC window over the bed, a wall air conditioner and the ondol thermostat by the door. Late-afternoon sun comes in through the window.

| Interactable | Where | 2D panel | Evidence it holds |
| --- | --- | --- | --- |
| Newspaper (`INT_Newspaper`) | Desk, folded | Full-page *Seoul Daily*, changes daily | The front page (Day 1: the warning story; later: yesterday's case), the warning box with the day's advice, local news, ads (one for checkfirst.kr) |
| Wall calendar (`Board_Calendar`) | Cork board over the desk | The pharmacy calendar and a list of what Jiwoo wrote on it, with today marked | Rent day and the amount, the dentist, the gas check; on the day it matters, the note she made from a real notice ("Mr. Choi in hospital this week. His son Hyunwoo collects the rent", "Monitor arrives! Hangang Express", "Monitor held at customs: duty 56,600 won") |
| Desk drawer (`INT_Drawer`) | Desk, top drawer | Documents as paper, one tab each, previous and next | Lease contract (the landlord, his number, the rent account and its holder, the son's name and number, "the account changes only in writing"), gas bill, delivery slip (Hangang Express's one number, 1588-5520), a receipt |
| Computer (`Laptop`) | Desk | A browser open on CheckFirst (`checkfirst.kr`, fictional) with two tabs | **Check a phone number** and **Check a bank account**. Each answers with the owner or account holder, a line about the registration ("Prepaid phone, opened 6 days ago", "Personal account, opened 3 weeks ago", OFFICIAL NUMBER), the number of fraud reports and what they say, and the reminder that no reports does not mean safe |
| Phone | Carried (Tab) | Phone + transcript | See section 4 |

The wallet, the notebook and the notes on the cork board are still in the room as props, without panels.

**Highlight.** While the room can be used (no panel open, the phone down), `Interactor` gives every `Interactable` a pulsing yellow outline; the one under the cursor is also lit warm and shows its prompt ("Click · Use the computer"), and the one the guide points at blinks. `Interactable.SetHighlight` writes `_Highlight` on the object's renderers (RGB = added light, A = mark). The toon shader writes a negative ink id for marked objects in its DepthNormals pass, and the ink pass (`DCM_InkComposite`) draws the outline where marked meets unmarked, pulsing with the global `_DCM_HighlightPulse`.

## 4. The phone

Raised with **Tab** (or the phone button in the corner), shown on the right of the screen in Jiwoo's hand, with the transcript to its left during a call. While the phone is up, the player cannot walk.

The home screen shows the clock, the date and three apps, with a status bar (clock, signal, battery) and a home bar. **Back** (arrow, Esc or right-click) goes one screen back; **Home** returns to the home screen.

| App | What it shows | What it is for |
| --- | --- | --- |
| Contacts | Names, numbers, memos ("Choi Youngsik · 1F · rent due on the 7th"); a contact's page with the number as a chip | Is the caller's number one Jiwoo saved? |
| Chats | Messenger threads: family, Mangwon Heights residents, Yuna, the café crew, Hangang Express (delivery notices), AliStar (order messages); on later days, direct messages from the landlord or his son | What did the landlord or the courier write themselves, and when? Unread threads carry a badge |
| Nuri Bank | Accounts and balances, recent transactions | What is there to lose. Money that left on an earlier day stays gone |

**Copy chips.** Every number, account or amount a caller says or a document shows appears as a chip. Clicking a chip copies it ("Copied · paste it on the computer"). The computer's lookup field has a **Paste** button, and it offers the call's own number and account as one-click chips, so the player never has to type a 14-digit account number.

### 4.1 Calls

1. **Incoming.** The phone raises itself, rings and shakes. The screen shows the caller's pixel portrait (a silhouette for unknown numbers), the name if saved in contacts, otherwise "Unknown" and the number, and one control: **slide to answer**. There is no decline button and Tab does nothing until the player answers.
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

A **clue** is a piece of evidence that points at the truth. Each day needs one more than the day before, and takes away one crutch.

| | Day 1 (Easy) | Day 2 (Medium) | Day 3 (Hard) |
| --- | --- | --- | --- |
| Clues | 2 | 3 | 4 |
| Where the clues are | Both on the computer; the guide walks through them | The lease in the drawer (or the number lookup), the account lookup, the residents' chat (legit: or the calendar note) | The courier's chat (or the calendar note), the shop's chat, the delivery slip (or the number lookup), the account lookup |
| What the computer says | Fraud reports on the account and the number: a lookup alone settles it | No reports on either. The names have to be compared with the lease | No reports, and the fake names sound official: the number is one digit off, the account holder a look-alike company |
| Truth | Always a scam | Scam or legit | Scam or legit |
| The day's newspaper | The matching warning ("there is no safe account") | A warning about new rent accounts: check whose name it is | A warning about parcel calls: compare the number and the account with the courier's own |

Rules for writing scenarios:

- No scenario is solved by a rule alone. Every scam asks for something its legit twin also asks for, and every twin pair shares the caller, the story and the ask.
- Nobody asks for a verification code, PIN or password. Money moves by transfer to an account the player can inspect.
- Every clue exists before the call starts, in a place the player can reach from the room or the phone.
- From Day 2 on, a single check can mislead: a clean lookup result, a caller who knows personal data, a real name on a fake account.

### 6.2 The three cases

| Day | Date | Case | Truth |
| --- | --- | --- | --- |
| 1 | Tue 6 Oct | The protected account: "Manager Jeon" of Nuri Bank's "account protection team" wants ₩1,200,000 moved to a protected account | Scam (the guided day) |
| 2 | Wed 7 Oct, rent day | The new rent account: "Choi Hyunwoo, the landlord's son", father in hospital, this month's rent (₩450,000) to his account today | Scam or legit |
| 3 | Thu 8 Oct | The held parcel: "Yoon Seora, Hangang Express customs desk", the AliStar monitor is held for ₩56,600 duty and VAT | Scam or legit |

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

Clues per variant (`DayVariant.clues`; each lists the events that count as finding it, see 7.5):

- **Day 1**: `account_owner` (the account lookup, or the name on the send step), `caller_number` (the number lookup).
- **Day 2**: `number` (the lease in the drawer, or the number lookup), `account_owner`, `landlord` (the residents' chat; on the legit day also the calendar).
- **Day 3**: `courier_notice` (the Hangang Express chat, or the calendar), `taxes` (the AliStar chat), `number` (the delivery slip, or the number lookup), `account_owner`.

### 6.3 The household and what is always there

- **Kim Jiwoo** (22), third year at Hanbit University, barista at Mangwon Roasters on weekends, lives alone in Mangwon Heights 302.
- **Family**: Mom (Park Hyejin) and Dad (Kim Dongsu) in Suwon, brother **Minjun** (19, first year in Daejeon), Grandma in Jeonju.
- **Building**: landlord **Choi Youngsik** (1F) and his son **Choi Hyunwoo**; the residents' chat.
- **Friends**: Seo Yuna (concert), café manager Han.
- **Baseline evidence** (`ContentBuilder.Household`): eight contacts; six chat threads with a week of history; Nuri Bank everyday account 110-302-558814 (about ₩1.28 million) and tuition savings (₩3,000,000) with a month of transactions; the lease, a gas bill, a delivery slip and a receipt in the drawer; the calendar with six notes; a directory of accounts and numbers the computer can look up (the family's, the landlord's, the bank's 1599-0000, Hangang Express's 1588-5520, 112, 1332).

## 7. The run

### 7.1 Picking the truth

`DayData.variants` holds one `DayVariant` per truth (call, phone, room, directory, clues, papers, rule). `DaySetup.Pick` rolls one when the day starts, with one rule: a run always has a legit caller, so on the last day that has a legit twin, if every earlier day was a scam, it is legit. `forceVariant` on `DayDirector` plays one truth for testing.

### 7.2 Evidence each day

`DaySetup.Compose` builds the day on top of `ContentBuilder.Household`, one timeline of Jiwoo's week that each day shows as it stands at its start time:

1. The variant's own phone, room and directory (cloned, so play never edits the assets).
2. History from the saved earlier days: money that left on a call (the savings cover what the everyday account can't) and yesterday's front page on the desk newspaper (the 2D panel and the printed prop).
3. Echoes (`DayData.echoes`): each reacts to one earlier day, its truth and its outcome. They never hold a clue for today's case. Those marked `notify` pop up as notifications while Jiwoo sits at the desk.

| After | Echoes |
| --- | --- |
| Day 1, sent the money | Mom in the family chat and Jiwoo's reply; the day card line "probably gone for good" |
| Day 1, hung up | Yuna's reaction; Jiwoo warning the residents' chat with the number, and a neighbour's thanks |
| Day 1, ran out of time | A neighbour's warning about the same number in the residents' chat |
| Day 2 scam | The landlord's warning in the residents' chat (and his thanks, if Jiwoo hung up); if the fake son got the money, a direct message from the landlord and the real rent paid again |
| Day 2 legit | Hyunwoo saved in contacts; his thanks, or his understanding reply and the rent paid a day late; the landlord posting from hospital |

### 7.3 Saving

`GameRun` keeps a `DayRecord` per finished day in PlayerPrefs (`DCM.Run`): variant, outcome, money, clues, times, the caller and any transfer. The title screen offers Continue (the first unplayed day) and New week; the morning after offers the next day, the same day again (later days are dropped when it ends) or the menu.

### 7.4 Newspaper

Each variant authors its own *Seoul Daily* (`RoomContent.newspaper`) and one front page per outcome for the next morning (`DayVariant.papers`):

- **Today's paper** (the desk panel). Day 1: the story behind today's scam. Days 2 and 3: yesterday's case as it turned out, taken from the saved day. Beside it the warning box, local news and ads.
- **The next morning** (End scene): the headline and story for what the player did, beside the case summary.
- **The desk prop.** The folded paper on the desk is a printed texture. `Content → Print Desk Newspapers` exports every front page that lies on the desk the next day to `Tools/ArtGen/papers.json`, `tex_prints.py papers` prints them in the Day 1 paper's style to `Art/Textures/Papers` (`*_ko` in Korean), and each `EndPaper.print` points at its print; `DayDirector` puts it on the prop.

| Rule (shown the next morning and in the week summary) | Taught by |
| --- | --- |
| Banks never "protect" your money. A bank never moves it to a "safe" or "protected" account. Look up the account and the caller's number first: a stranger's name on an "official" account means stop | Day 1 |
| A new rent account? Check the lease. No reports doesn't mean safe: check the caller's number and the account holder's name against your lease, and ask the landlord's own chat | Day 2 scam |
| Checking works both ways. When the number in your lease, the landlord's own message and the name on the account all match, the change is real. Check, then act | Day 2 legit |
| Is anything due? Read the courier's own notice and check who owns the number and the account. If the courier says nothing is due, nothing is due | Day 3 scam |
| Pay duty only to the courier. Real duty shows up in the courier's own notice, with an account in the courier's name. When they match the call, paying is safe | Day 3 legit |

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

Each `ClueDef` lists the events that count. Finding clues does not change the outcome and the player is not told during the day; the next morning's case summary lists every clue, found (✓) or missed (with where it was).

### 7.6 Summary

After Day 3, **Your week** lists each day's case and truth, what the player did, right or wrong and clues found, then right calls, money lost to scams, the savings before and after, a rating (Scam-proof, Careful, At risk) and the rules learned. Buttons: New week, Main menu.

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
    Sprites/    Generated 9-slice frames, icons, portraits, the hand, document images
    Uss/        Theme.uss plus Phone, Panels and Call style sheets
    Settings/   Panel settings, text settings, theme style sheet, UISkin (every UI texture by name)
  Audio/
    Music/      Generated loops (Tools/Audio/music.py)
    Voices/     Generated voice clips per voice + manifest.json (Tools/Audio/tts.py)
  Data/
    Day1/       Day 1: phone, directory, room, the call, the day (clues, next-morning papers, rule)
    Day2/ Day3/ Each day's asset plus a call, phone, room and directory per variant (Day2_Scam_*, Day2_Legit_*)
    Day*/ko/    The same in Korean (Day1_ko, Day2_Scam_Call_ko, …)
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

`DayDirector` runs a day. `Prepare` (called by `UIManager` as it wakes up, so the phone and room show the day's content from the first frame) takes the day from `GameRun.PendingDay` (or its own field when the Room scene is opened directly), picks the truth and composes the evidence (7.1, 7.2). Then: the DAY card with the echoes' lines, the seated start (Day 1: the guide; later days: the echoes buzz in), the forced call, the CASE OPENED card when `CallDirector` reports the hold, the investigation against the deadline (beats, music tension, screen-edge pressure, ticking), and the fade to the End scene with a `DayResult` (outcome, ending, money, time taken, clues found), recorded in `GameRun`. `CallDirector` plays the call: voiced lines, decisions, the hold and its questions, the verdict and the endings.

### 8.3 Main classes

| Class | Responsibility |
| --- | --- |
| `DayDirector` | The phases of a day (Waiting, Intro, Seated, OnCall, CaseCard, Investigating, Ending, Done), the deadline, the pressure, the record |
| `DaySetup` | Picks the variant; composes the day's phone, room and directory from the variant, the history and the echoes |
| `GameRun` | The saved run: one `DayRecord` per finished day |
| `CallDirector` | Plays a `ConversationData`: ring, lines with voice, decisions and patience, the hold, questions, the verdict's actions, endings |
| `ClueTracker` | Which of today's clues the player has seen (7.5) |
| `TutorialGuide` | Day 1's guide: the step for the current state, the spotlight, when the call may start (2.1) |
| `UIManager` | The UI document: panels, the phone, the transcript, HUD, cards, pause and confirmations; locks the player's input while a panel is open |
| `Interactor`, `Interactable` | What is under the cursor, the prompt, opening its panel; the outline and light on usable objects |
| `Loc` | The language and the UI's Korean strings (10) |

### 8.4 Input

- Actions in `Assets/InputSystem_Actions.inputactions`: `Drag` (drag to look), `Phone` (Tab), `Back` (Esc, right mouse button in panels), `Interact` (E, plain press) and the mouse click. `1`–`4` pick a decision option or a question.
- Looking: hold a mouse button and drag to turn. Drags shorter than a few pixels count as clicks, and presses that start on the UI never turn the view. UI that is hidden or faded out never catches the pointer, so a press anywhere on the room always looks or interacts.
- A press on the room while the phone is up puts the phone away and the same drag turns the view; a ringing phone stays up. A click on the dimmed room around a panel closes it, and clicking the call HUD brings the phone back.
- `UIManager` keeps the open panel. Opening one calls `FirstPersonController.SetInputLocked(true)`; closing it unlocks. Esc goes back one screen in the phone, then closes the panel.
- `Interactor` raycasts from the mouse cursor (up to 2.5 m) against the interactables' colliders, shows the prompt and opens the panel on click or E. A click that ended a drag (`FirstPersonController.PressWasDrag`) is ignored. `Interactor` and `Interactable` live in their own files: Unity only serialises a component whose class matches its file name.

## 9. Data model

```
DayCatalog (SO)        days[]   in Resources, loaded by name (DayCatalog_ko in Korean)
DayData (SO)           day, dateLabel, shortLabel, place, startTime, intro, ringDelay, nextDateLabel, builtWith
                       variants[]   DayVariant { id (scam | legit), conversation, phone, room, directory,
                                    clues[] { id, text, where, when[] { ClueEvent, target } },
                                    papers[] { outcome, headline, subhead, body, verdictNote, print },
                                    ruleTitle, rule, ruleSource }
                       echoes[]     DayEcho { afterDay, truth (Any | Scam | Legit), outcomes (flags),
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
| Drawer | Documents as paper on a wooden tray: a tab per document, previous and next; numbers and accounts as chips; a PAID stamp as an image |
| Calendar | The pharmacy calendar's picture beside a paper list: today's date, then each handwritten note with its day (past days greyed, today highlighted) |
| Computer | A laptop screen with a browser window (tab, address bar `https://checkfirst.kr`): two tabs, a field with Paste and Check, chips "from the call and your notes", and a result card (owner or holder, registration note, OFFICIAL NUMBER badge, report count in red or green, the reports, the "no reports doesn't mean safe" tip). A number pasted into the wrong tab switches tabs by itself |
| Phone | Frame + status bar + home screen + three apps, held by a hand (fingers behind the phone, thumb in front). Screens share list and detail templates |
| Incoming call | Round pixel portrait with a pulsing ring, name or "Unknown", number, a shaking phone and slide to answer (no decline) |
| In call | Call screen with call timer, Apps, Speaker and hang-up (off during a case: "Verdict on the call"); "Return to call" bar in the apps |
| Transcript | Chat log left of the phone: portraits, bubbles with times, typing indicator, fact chips, two decision buttons, decision timer |
| Call HUD | Top centre while the phone is down: caller, latest line, "Holding the line · Tab to ask or decide" |
| Decision timer | Ring with seconds; amber at 50%, red and pulsing at 25%; pressure overlay at the screen edges |
| HUD | Top left: day, date and clock, with the deadline under it. Bottom right: the phone button with a notification badge (hidden while the phone is up). Bottom left: the control hints. The interact prompt at the cursor |
| Tutorial card | Day 1: a yellow note on the left, HOW TO PLAY, the step number and one instruction |
| DAY card | Black card: DAY 1, the date, time and place, a line of intro and the echoes' lines; click or wait |
| CASE OPENED | Paper case file with a tab (CASE #01), the caller's portrait, "Says they are", "Wants you to", "Deadline", "Your job" and Start investigating |
| Deadline | Under the clock: deadline time, minutes left and a bar; amber at half, red and pulsing in the last quarter |
| Hold | The transcript footer while the caller holds: the questions still open (1–4) and the verdict panel |
| Verdict | Dark strip, YOUR VERDICT, gold / red buttons with an icon, a label and a detail line; a coloured confirmation step (send: amount and recipient's name; hang up) |
| Title screen | The DON'T CALL ME! sticker logo over the room at dusk (slow camera drift), the menu, a ringing phone whose slide starts the game, lo-fi music |
| Pause and Settings | Paper card over the frozen room: Resume, Settings, Main menu; the language (English or 한국어; during a day the switch asks first, then restarts the day in the new language) and sliders for music, sound effects, voices and look sensitivity (saved) |
| Next morning | The Seoul Daily front page for the outcome beside the case summary: SCAM or REAL and RIGHT / WRONG CALL or TOO LATE stamps, money kept or lost, decided at, time taken, clues with ✓ or where they were, the rule learned, the next day, Play again and Main menu |
| Toasts | Top right, informative ones never catch the pointer |

Fonts (all OFL, in `Assets/_Game/UI/Fonts` with their licences): Nanum Gothic for the UI, Do Hyeon for headings, Gaegu for handwriting, DM Serif Display and Crimson Text with Nanum Myeongjo for the newspaper, DotGothic16 for the clock and timers. Nanum Gothic, Do Hyeon, Gaegu and Nanum Myeongjo have Hangul; the Latin-only fonts fall back to Nanum Myeongjo or Nanum Gothic for Korean text.

**Language.** `Loc` (Data) holds the language (PlayerPrefs `DCM.Language`) and translates the UI's own strings: code passes the English text, `Loc.T("…")` or `Loc.F("… {0}", …)`, and gets it back in Korean from the table in `LocKo.cs` (a missing entry falls back to English and warns in the editor). The content is not translated at run time: `ContentBuilder` writes every day twice, the Korean assets in a `ko` folder next to the English ones, and `DayCatalog.Load()` picks `Resources/DayCatalog_ko` when the language is Korean. Pictures with writing on them (the calendar, the receipt) have Korean versions (`*_ko` sprites), and the desk newspaper has Korean prints. Korean wraps between words (the panel's text settings use the modern Hangul line-breaking rules). Changing the language reloads the title screen, or restarts the current day after a confirmation. Chat text avoids emoji: the UI fonts have none.

## 11. Art and audio

**Look.** Warm, cosy and lived-in, so the calls feel intrusive. Comic-book ink outlines and hatching over hard-banded toon shading, a retro print grade, late-afternoon sun through the window and dust in the sunbeam. People are pixel art in the style of `references/art/ref4.png`, in the room's photos and in the callers' portraits.

**3D and textures.** The bedroom, its props and textures are generated from code in `Tools/ArtGen` (Python for textures, Blender for the model, a Unity editor pipeline for materials, prefab and scene). See `Tools/ArtGen/README.md`. Hand-made Blender models can still replace a generated part: keep the object name, and the pipeline remaps its materials.

**2D.** UI frames, icons, caller portraits, the hand that holds the phone, menu glyphs and the title logo are generated by `Tools/ArtGen/ui_art.py`; the UI pipeline imports them as sprites.

**Audio.** See `Tools/Audio/README.md`.
- Voices: every voiced line is exported from the content in both languages (`Tools/Audio/voice_lines.json`, 152 lines), spoken by a macOS `say` voice per caller and language (English: Daniel, Reed, Shelley; Korean: Rocko, Reed and Yuna), shaped like a phone line (band-pass, compression, line hiss) and imported into the `VoiceBank`. The files go in `Audio/Voices/<voice>/<hash>.wav`. A scam and its legit twin share the voice.
- Music: generated loops (`Tools/Audio/music.py`): lo-fi keys for the title, a calm bed and a tension layer of the same length for the investigation (crossfaded by the time left), and a bright morning loop for the next morning.
- SFX: ringtone, vibration, slide-to-answer, hang-up tone, message sound, typing, paper rustle, click, heartbeat, timer tick.

## 12. Testing

- **UI tour** (`Tools > Don't Call Me > UI > Capture UI Tour`): enters Play mode, screenshots every room panel and phone app into `Temp/UITour`, makes both lookups on the computer, then plays Day 1's call twice (questions and hang up; send the money) and checks each step (the lookups show the right names and reports, the call rings, the hold starts, the endings say what was done). Failed checks are logged as `[UITour] FAILED`. It runs in English (it finds buttons by their English labels) and puts the chosen language back afterwards.
- **Offline compile**: both assemblies compile outside the editor with Unity's own compiler settings (the snippet is in `CLAUDE.md`), so a change can be checked while Unity is busy.
- **Play-through** (after the scope cut): Days 1–3 played end to end in English and in Korean, covering both truths of Days 2 and 3, all three outcomes (send the money, hang up, out of time), the week summary, Day 1's call starting by itself when the newspaper is never read, and a saved week from before the cut continuing into Day 3.
- **Playtests with the target audience** (young adults living alone, at least five people), measuring: right verdicts on Day 1 vs Day 3; whether they look up the account and the number without being told on Days 2 and 3; whether they can name the rules after one run; where they get stuck on controls or UI.
- Later: a content validator (every clue's target exists in that day's content, every option leads somewhere, both truths of a twin share the ask) and EditMode tests for `DaySetup` and `ClueTracker`.

## 13. Build phases

| Phase | Work | State |
| --- | --- | --- |
| 1. Room and movement | Generated bedroom, first-person controller, colliders | Done |
| 2. UI templates | UI Toolkit theme and assets, interactor, room panels, phone, incoming call with slide to answer, transcript, decision timer, call HUD | Done |
| 3. Conversations | Day flow (seated start, CASE OPENED, the hold with questions, beats and the deadline), voiced lines, title / pause / settings / next-morning scenes | Done |
| 4. Day loop | `GameRun` (save), `DaySetup` (truth, history, echoes), clue tracking, reveal, the week summary; Days 2 and 3 with scam and legit twins | Done |
| 5. Korean | Language option, Korean content, voices, prints and line breaking | Done |
| 6. Scope cut | Three apps, four room objects, the computer's two lookups, the calendar, 2 / 3 / 4 clues, the guide, highlights, the hand | Done (2026-10-01) |
| 7. Polish and playtest | Target-audience playtests, timing and difficulty tuning, ambience | Next |

### 13.1 How to build and try it

- **Content**: `Tools > Don't Call Me > Content > Build Days` writes `Assets/_Game/Data/Day1..Day3` with their Korean copies in each day's `ko` folder, `Resources/DayCatalog` and `Resources/DayCatalog_ko`; `Print Desk Newspapers` prints the desk paper for each front page in both languages (Python, see `Tools/ArtGen/README.md`). When `ContentBuilder.Version` is newer than the one recorded in `Day1.asset`, the editor rebuilds the content, the prints and the voices by itself after the next compile.
- **Audio**: `Tools > Don't Call Me > Audio > Run Voice Pipeline` (export lines, TTS, import) and `Generate Music`.
- **Scenes**: `Tools > Don't Call Me > Scenes > Build Home and End Scenes` (also sets the build order Home, Room, End); the Room scene's UI, directors and interactables come from `UI > 4. Set Up Game UI in Room Scene`.
- **Play**: open Home and press Play (Continue picks up the saved week; New week starts at the guided Day 1), or open Room to start straight at the day set on `Flow/DayDirector` (Day 1; set `forceVariant` to `scam` or `legit` to test a truth). To test a later day with echoes, write earlier `DayRecord`s into PlayerPrefs `DCM.Run`. To play in Korean, pick 한국어 in Settings or set PlayerPrefs `DCM.Language` to 1 (0 is English).
- **Check**: the UI tour (12). `DontCallMe.Editor.Tools.GameCapture.Capture(path)` renders a 1920 × 1080 screenshot of the paused game even when the Game view is hidden.

## 14. Risks

| Risk | Mitigation |
| --- | --- |
| Players can't find the investigation tools | The Day 1 guide walks through one full check; usable objects glow; the computer offers the call's number and account as one-click chips |
| The lookup gives the answer away every day | Only Day 1 has fraud reports. From Day 2 the lookup returns a clean result and a name, which only means something next to the lease, the slip or the chats |
| Checking everything is always right, so there's no tension | The caller's deadline, his pressure lines, and questions that cost time |
| Players refuse every call | A run always has one legit caller, and refusing a legit call has a visible cost in the next paper and the next day's echoes |
| Three days are few | Each day has two truths, so a second week plays differently; more cases can reuse the same four tools (15) |

## 15. Cut from the prototype

Removed on 2026-10-01 to keep one clear loop (call → look up → compare → verdict). Each can come back once the core is tested with players:

- **Phone**: the Phone app (recents, keypad, calling out and call-backs, the Verify verdict), Messages (SMS), the CheckFirst app (now the computer), Browser, Parcels, Mail, and transfers from the bank app.
- **Room**: the wallet, the cork board's notes and the notebook as panels (they remain as props).
- **Conversations**: chat scenarios (a scam that arrives as a chat), the sample chat and the demo call.
- **Run**: the 5-day run drawn from a pool (Easy, Medium, Medium, Hard, Hard, seeded, with constraints), ambient filler evidence per day, the debug menu.
- **Scenario ideas** not built: gas inspection and its "boiler emergency" twin, "Mom's broken phone" and the brother on a friend's phone, the courier at the door, the held card payment, "Investigator Kang" and Detective Oh, the money-mule job and the real internship. Their evidence was designed around the tools above (this file's history before 2026-10-01 has the full lists) and would need rewriting for the four tools the prototype keeps.

Also later: a mobile build.
