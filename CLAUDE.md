# Don't Call Me! — context for Claude sessions

Read this first. It sums up the project, how the user works and how to build and test, so a new session can continue without the old conversation. The full design lives in `docs/technical-plan.md` (the source of truth; keep it updated when behaviour changes). Asset pipelines are in `Tools/ArtGen/README.md` and `Tools/Audio/README.md`.

## Working with the user

- Reply to the user in Indonesian, short and direct. Code, comments, docs, commit messages: normal English.
- Work directly. No superpowers/brainstorming/spec/writing-plans ceremony, no `docs/superpowers/` files. Ask only blocking questions.
- Quality over speed. One coder (the user); teammates do art and content.
- **The user commits.** Do not commit unless asked. Never commit `.DS_Store` files. Ask before `git push` and before deleting assets.
- All brands, people, accounts and phone numbers are fictional. Only 112 (police), 119 (fire/ambulance) and 1332 (FSS fraud hotline) are real.
- The user may be play-testing in Unity. Check `IsPlaying` before touching Unity, and do not stop a session that is in use.

## The game

Unity 6000.4.11f1, URP, Input System, UI Toolkit (C# builders plus USS). It is a first-person scam-call detective game. Jiwoo, a student living alone in Mangwon-dong, Seoul, gets a call each afternoon. While the caller holds the line the player investigates, then gives a verdict on the call: gold "send the money" or red "hang up". The next morning's Seoul Daily reveals the truth.

The scope was cut on 2026-10-01 (plan §15 lists what was removed). What the player has:

- **In the room** (each opens a 2D panel, each glows with a yellow outline): the newspaper, the desk drawer and the computer on the desk, and the wall calendar on the cork board by the door (south wall, about 3 m from the desk, moved there on 2026-11-17 so the player walks). Things are used from 2 m (`Interactor.Reach`); pointing at something farther says "(walk over)" and a click walks Jiwoo there around the furniture (`FirstPersonController.WalkToward`, `Player/WalkPath`) and opens it on arrival; any key or drag takes over. The computer is a browser on CheckFirst with two lookups, **Check a phone number** and **Check a bank account**; each gives the owner's name, a registration note and the fraud reports.
- **On the phone** (Tab, held in a hand): Contacts, Chats, Nuri Bank. All three are read-only evidence. A call can only be answered: slide to answer, no decline. Space (or the transcript's Skip button) cuts a caller's line short.
- **The case notebook** (left edge, N folds it; plan 7.5): the caller's claims (at most three) arrive as they are said, at "?". Two to four things per case can be circled in the panels and apps; each is filed under its claim, or under several when it settles them all (`MarkDef.also`). A caller's answer can be circled too (`MarkPlace.Call`): on a Write day through a small Circle button under it, since a click on the line pins it. The bank history shows "Today · Nothing received yet today" when no money came in today (circleable). A claim can be stamped TRUE or FALSE once something is filed under it. A clue counts when one of its marks is circled. The next morning the paper checks the notebook in red. Help drops one step a day (`DayData.help`: Guided, Hints, Plain, Write), shown on the DAY card as stars (`level`) and a NEW TODAY box (`newToday`). On Day 3 (Write) the notebook starts with three empty lines: the player pins the caller's lines from the transcript (`Pinnable`), claim or not. The design page: the "The Case Notebook" artifact (claude.ai/artifact/Uag1fGpdxkmXRRf7PqDfET).
- **Scenes:** Home (title: Start / Continue / Choose a day, Choose a day, Case Files, Settings, Quit), Room (the day), End (next morning, week summary).
- **Day select** (plan 7.3): a day finished once opens the next one for good, in any week (`GameRun.Unlocked`; progress per day in `DCM.Played`). The title's Choose a day (`DaySelectView`) shows every day as a calendar page and plays any open one; days this week has not played are filled in from their last finished record (`GameRun.FillBefore`). The green button plays the suggested day (`GameRun.Suggested`) and opens the day select once every open day is done, so the tutorial is never forced again. Case Files' This week is only a record now. **Typing DEMO on the title** opens every day on that machine for demos (`GameRun.Demo`, PlayerPrefs `DCM.Demo`); typing it again closes them.
- **Kept across weeks** (plan 7.7): the truths seen of every case (Case Files → Cases: "1 of 2 truths seen", the unseen truth face down) and 15 trick cards (Case Files → Trick cards), each won by stamping a false claim that uses its trick FALSE.
- **Playtest log** (plan 7.8): `PlaytestLog` writes JSON Lines (every look, circle, stamp, pin, question, verdict, and a `day_end` summary) to `playtest/` in the persistent data folder (the editor: `Temp/Playtest`; a browser: PlayerPrefs `DCM.Playtest`). Case Files → **Save playtest log** opens the folder or downloads it; `python3 Tools/Playtest/summarize.py <logs>` prints the numbers as Markdown.
- **Run:** 5 days, Mon 5 – Fri 9 Oct 2026 (`GameRun.FirstDay` is 0).
  - Day 0 "unpaid gas bill": the tutorial (`DayData.tutorial`, `TutorialGuide`, 13 steps). Always a scam, with eight clues, one per tool: newspaper, number lookup, account lookup, the gas bill in the drawer, the calendar (a holiday), the bank app (already paid), the residents' chat, contacts. Small sum (₩18,420) and a far deadline (the call comes at 14:10).
  - **Days 1 to 3 each bring one of several cases** (the user asked for variety on 2026-10-01, with clues that are not always on the computer, and only one or two per case). `DaySetup.Pick` takes the case the player has finished least often (PlayerPrefs `DCM.Played`, kept across weeks), among equals the first in the day's list (so a first week always plays each day's first case), then a truth at random.
    - Day 1: "protected account" (`protected`: always a scam, 3 clues: two on the computer and the paper's description of the call), "subscription fee" (`paper`: 1 clue, the paper's own notice in the newspaper box), "extra zero" (`sale`: 1 clue, the bank app's history: in the scam the "nothing received yet today" row).
    - Day 2: "new rent account" (`rent`: 3 clues, no fraud reports: compare names with the lease and the residents' chat), "Minjun's broken phone" (`brother`: 2 clues, Mom's message in the family chat and his answer to a question only he can answer).
    - Day 3: "held parcel" (`parcel`: 4 clues: courier chat, shop chat, delivery slip or number lookup, account lookup), "dentist's deposit" (`dentist`: 2 clues, the calendar and whether she knows what the booking is for).
    - Day 4 (Hangul Day, `Write`, four stars): "Yuna's stolen bag" (`yuna`, variant ids `scam` / `legit`), the first **video call** (plan 7.5.1). Yuna, in Fukuoka with her cousin Somin, video-calls from Somin's number: bag snatched, ₩650,000 for the hotel to Somin's account. Scam: a face pasted on by software. 4 clues: the face and the wave (both circled on the video), her answer to "what did we eat yesterday" and Yuna's own chat (a ramen-queue photo at 15:58, or her 15:47 message from the hotel PC). The computer shows a real Kim Somin in both truths.
    - Every case but `protected` is a scam or its legit twin. In the four one-clue cases the computer shows the same clean result in both truths, on purpose: the answer is in one other place only.
  - A run always has at least one legit day, by Day 3 at the latest (`DaySetup.LegitBy`): Day 4 is never forced. Days remember earlier days: money, yesterday's paper on the desk, and "echoes" (chat messages, bank transactions, a saved contact, a day-card line), which name the case they follow.
- **Languages:** English and Korean (한국어), chosen in Settings on the title screen or the pause menu. Switching mid-day asks first, then restarts the day.

## Code map (`Assets/_Game/Scripts`)

- `Data/`:
  - `DayData` / `DayVariant` / `DayEcho` / `EndPaper`, `ClueEvent`.
  - `RoomContent` (newspaper, drawer, calendar), `PhoneContent` (contacts, chats, bank), `WorldDirectory` (accounts and numbers the computer looks up), `ConversationData`, `VoiceBank`.
  - `DayCatalog`: `Load()` picks `Resources/DayCatalog_ko` in Korean.
  - `Loc` + `LocKo` (language, UI strings).
  - `Facts`: number and money helpers.
- `Flow/`:
  - `DayDirector`: phases Waiting → Intro → Seated → OnCall → CaseCard → Investigating → Ending.
  - `DaySetup`: picks the variant (the day's case, least played first, then its truth) and composes a day from history and echoes.
  - `GameRun`: save in PlayerPrefs `DCM.Run`; `FirstDay` (0) and `PendingDay` (−1 when the Room scene is opened directly); cases played, their truths seen, the trick cards won and each day's progress (`DCM.Played`, `TimesPlayed`, `MarkPlayed`, `SawTruth`, `CatchTrick`, `Unlocked`, `Suggested`, `FillBefore`); `ForceNextVariant(id)` for tools and tests (`DCM.ForceVariant`, read once).
  - `TutorialGuide`: the tutorial day's guide. Before the hold the step follows the phase; during it, it shows the `guide` line of the first clue not found yet, with what the last one taught. The call waits for the newspaper to be read (75 s at most).
  - `CaseNotebook`: the day's claims heard, marks circled and stamps.
  - `PlaytestLog`: the playtest log.
  - Also `CallDirector`, `ClueTracker`, `SceneFlow`.
- `Gameplay/`: `Interactor` (cursor raycast, prompt, highlight, `Use`: open in reach or walk over first, `InReach`, `InView`) and `Interactable`. `Player/`: `FirstPersonController` (walk, drag look with a short ease, sitting and standing, a click's walk) and `WalkPath` (A* route across the floor). **One MonoBehaviour per file**, named like the class: two in one file broke the scene's script reference and silently killed all interaction.
- `UI/`:
  - `UIManager`: panels, the phone, pause, confirms.
  - `Panels/RoomPanels.cs`: `NewspaperPanel`, `DrawerPanel`, `CalendarPanel`, `ComputerPanel` (`PanelId`).
  - `Phone/`: `CommsApps.cs` (Contacts, Chats), `MoneyApps.cs` (Bank), `CallViews.cs` (`CallArt`, `IncomingCallView`, `InCallView`, which shows a video call's face), `VideoFeed.cs` (the face: lips, blinks, the wave, a faked face's tells), `PhoneController.cs` (the hand, the status bar, the screen stack).
  - `Call/` (transcript, verdict panel), `Hud/` (HUD, `TutorialCard`, `NotebookView`), `Menus/` (`TitleScreen`, `DaySelectView`, `CaseFilesView` with its three pages, `CollectionViews.cs`: `CaseBook` and `TrickCards`, settings, end, week), `Day/`, `Core/` (UIKit, UISkin, GameClock, Clipboard, ClueEvents, `Markable`, `Pinnable`).
  - `UITour.cs`: the screenshot tour, English only.
- `Editor/`:
  - `UI/ContentBuilder*.cs`: all game content, see below. One file per case: `Day0`, `Day1` (protected account), `Day1Paper`, `Day1Sale`, `Day2` (rent), `Day2Brother`, `Day3` (parcel), `Day3Dentist`, plus `Household` (the shared timeline) and the core.
  - `UI/UIPipeline.cs`: sprites, font assets, panel settings and text settings, UISkin, scene UI. Its `Interactables` table says which scene objects can be used.
  - `Audio/AudioPipeline.cs`, `Art/RoomArtPipeline.cs`, `Scenes/ScenePipeline.cs`.
  - `Tools/GameCapture.cs`: renders the paused game to a PNG even when the Game view is hidden.
  - `Build/WebBuild.cs`: the browser build and its zip, see Builds.
- Shaders (`Assets/_Game/Art/Shaders`): `DCM_Toon` (property `_Highlight`: RGB adds light, A marks the object) and `DCM_InkComposite` (ink lines, plus the yellow outline around marked objects).

## Content and localization rules

- **Content is code.** `ContentBuilder` (Household, Day0–3) builds every asset under `Assets/_Game/Data/Day0..Day3`. It runs twice: once in English, and once in Korean into each day's `ko/` folder (`*_ko.asset`, listed in `Resources/DayCatalog_ko`).
  - Write every string as `L("English", "한국어")`.
  - Shared names that clues point at are properties (`NuriBank`, `Landlord`, `LeaseTitle`, `VillaChat`…), so a clue and its target always match.
  - Do not hand-edit the generated assets; change the builder and rebuild.
  - Bump `ContentBuilder.Version` when built content changes, so open projects rebuild by themselves (the rebuild also prints the papers and runs the voices, which blocks the editor for minutes when there are many new lines: to avoid that, build, print and run the voices by hand first, set `builtWith` on `Day1.asset`, then bump).
  - **A new case** is a file with a `XVariant(bool scam)`, its call, its papers and its echoes, added to its day's `variants`. `StoreVariant(id, scenario, dir, prefix, …)` takes the variant id (unique in the day: `paper_scam`) and the case (`CasePaper`); twins share the case, the caller, the voice and the ask. Use `v.intro` when the day card needs the case's own scene. Echoes go in the next day's list wrapped in `OfCase(CaseX, …)`; later days add the same echoes through `StillThere(...)` (chats, notes and money stay, the day card line does not, nothing notifies again). Labels like "Yesterday" are relative to the day being built, so echoes are functions called per day. A calendar note a case adds that must stay is an `EchoNote` (it can swap the calendar's picture). Helpers: `AddChat` (with a photo caption), `AddTx` (a bank line and the balance), `Note` / `Renote` (calendar), `FrontPage` (yesterday's page when nothing was played), `News`.
  - **Claims and marks:** `c.claims` is at most three `Claim(id, L("\"In the caller's words\""), truth, L(red-pen note))` (every claim of a legit caller is true; a scam has at least one false). The line that makes a claim gets `.Says(id)`. `v.marks` lists what can be circled, each under a claim: `MarkField`, `MarkLine`, `MarkImage` (drawer), `MarkNote` (calendar, by its text), `MarkCheck` (computer), `MarkChat`, `MarkTx`, `MarkBalance` (bank), `MarkContact`, `MarkNews`. Keep the exact strings in variables shared by the evidence and its mark. Clues use `Circled(markId)`. Add decoys (true, about a claim, deciding nothing) so filing gives nothing away. An item that settles two claims files under both: `.Also("claim")`. A caller's answer that settles a claim: `MarkCall(id, claim, line)` (keep the line in a property shared with the `Caller(...)`). Every claim names its trick with `.Via(TrickX)` (constants and cards in `ContentBuilder.Tricks.cs`; a new kind of lie needs a new card, in both languages). A claim's line must be the caller's (Day 3's players pin it from the transcript). **A video call** sets `c.video = new VideoCall { face = "vc_<name>", faked = scam, faceNote, waveNote }`, gives the "wave" answer `.Act("wave")`, and marks the face and the wave with `MarkVideo(id, claim, "face" | "wave")`; add the face to `VIDEO_FACES` in `Tools/ArtGen/ui_art.py` and run `ui_art.py video_faces`, then UI pass 1 and 3. After a rebuild run **Content → Check Notebook Content** (`ContentCheck.Run()`): every mark must be on screen, in both languages, and every claim that can be false (here or in its twin) needs a mark under it that a clue counts. From Day 1 on, never mark the day's own headline or body: yesterday's case replaces the front page whenever an earlier day was played; use the warning box, a local item or an ad.
- **Clues** are found when one of their marks is circled (`ClueEvent.Marked`), and the account clues also by `RecipientShown` (the verdict's send step). The other `ClueEvents` (`PanelOpened`, `DocumentViewed`, `NumberChecked`, `ChatRead`, `BankOpened`, `ContactsOpened`) still fire; Day 0's paper clue also counts `PanelOpened`, since it is read before the call, when there is no notebook yet. Each day's first case has 2, 3 and 4 clues; the other cases have 1 (the user wants 1 or 2 for new ones, and not on the computer). On the tutorial day every clue also has a `guide` line (`Guided(Clue(...), L(...))`), and the guide walks them in list order.
- **UI strings in C#** use `Loc.T("English")` or `Loc.F("… {0}", x)`. Add the Korean to `Data/LocKo.cs`. A missing entry falls back to English and logs `[Loc] No Korean` in the editor.
- **Voices** are moving from macOS `say` to ElevenLabs (`ELEVEN` in `Tools/Audio/tts.py`: one English and one native Korean ElevenLabs voice per caller; key in `ELEVENLABS_API_KEY`, pay as you go; check clips with `Tools/Audio/check_voices.py`, see `Tools/Audio/README.md`). Done: Day 0 (Matilda / Jisoo). Day 4's Yuna speaks with Flo for now (`Flo (English (US))` / `Flo (Korean (South Korea))`, the second shared with the extra-zero buyer's Korean voice): give her her own voice ids before choosing ElevenLabs voices for her. Chosen for the rest (2026-11-16): Jeon Eric / Hyunbin, Oh Sangchul Bill / Sung-ho, Bae Sujin Jessica / Kanna, Choi Hyunwoo Chris / Theo, Minjun Liam / Taehyung, Yoon Seora Alice / Salang, Moon Jihye Sarah / Hana Lee. The game's voice ids keep the macOS names, one per caller per language: Samantha / Sandy (Korean) (Day 0), Daniel / Rocko (protected account), Grandpa (US) / Grandpa (Korean) (subscription fee), Tessa / Flo (Korean) (extra zero), Reed (US) / Reed (Korean) (rent), Eddy (US) / Eddy (Korean) (Minjun), Shelley (UK) / Yuna (parcel), Karen / Shelley (Korean) (dentist). Speeds are in `RATES` in `Tools/Audio/tts.py`. For numbers read aloud, the Korean `spoken` field uses digit words (`일일공, 구공공…`). Chats have no voice.
- **Pictures with writing on them** have Korean twins `UI/Sprites/*_ko.png` (calendar, receipt, the PAID stamp). `ContentBuilder.Tex()` picks `_ko` automatically in Korean. `board_calendar_moved` is the calendar with the dentist crossed out on the 8th and circled on the 22nd (the dentist case's scam truth).
- **Callers' portraits** are `pt_*` sprites from `PORTRAITS` in `ui_art.py` (`pt_branch`, `pt_buyer`, `pt_dental` for the newer cases). A caller Jiwoo cannot see (Minjun on a strange number) uses `pt_unknown`.
- **Desk newspaper prints** are `Art/Textures/Papers/T_Paper_D*_*.png`, with `_ko` versions.
- **Fonts:**
  - The UI fonts are Nanum Gothic (UI; Bold is the base weight, ExtraBold for clocks, timers and counters: its digits share one width), Do Hyeon (headings, names, the title menu), Gaegu Bold (handwriting), Crimson Text (SemiBold for body), DM Serif Display and Nanum Myeongjo Bold. The Latin-only serifs fall back to Nanum Myeongjo.
  - **Keep text readable in a small window:** near-black ink on paper (`--ink`, `--ink-soft`, `--muted`; no mid-greys), no Regular weights for running text, exact numbers (phone numbers, accounts) in Nanum Gothic, and plain Nanum Gothic for anything the player acts on (handwriting only for Jiwoo's own notes).
  - **No pixel art in the UI** except people's portraits: the user tried a pixel-art phone and title on 2026-10-01 and asked for the inked style back (new layout kept, retro colours and print texture).
  - **No emoji**, no U+2212 minus, no `é`, no `✓`: they render as boxes. `♥` is fine.
  - Korean wraps per word through `UI/Settings/PTS_Game.asset` (modern Hangul line breaking).
- **Python:** use `/Library/Frameworks/Python.framework/Versions/Current/bin/python3`, which has numpy, PIL and fontTools.

## Rebuild steps (in Unity, menus under Tools → Don't Call Me)

1. **Content → Build Days** (`ContentBuilder.Build()`): both languages, and it exports `Tools/ArtGen/papers.json`.
2. **Content → Print Desk Newspapers.** Or run `python3 Tools/ArtGen/tex_prints.py papers`, then Build again to link the prints.
3. **Voices:** `AudioPipeline.ExportLines()` → `python3 Tools/Audio/tts.py` (shell; keeps existing clips, removes unused ones) → `AudioPipeline.ImportVoices()`. Or use **Audio → Run Voice Pipeline**, which blocks Unity for a while.
4. **UI art:** `python3 Tools/ArtGen/ui_art.py` → **UI → Run UI Pipeline**. Pass 3 is `BuildSettings()` (skin and text settings) if you only need that part; pass 4 (`SetupScene()`) puts the UI, the directors and the interactables in the Room scene. A style sheet that names a new font asset must be reimported after the asset exists.

## Builds

- **Mac:** `BuildPipeline.BuildPlayer` (Home, Room, End) for `StandaloneOSX`; the last one went to `~/Downloads/DontCallMe.app`. A standalone keeps its own PlayerPrefs (`com.Unity-Technologies.com.unity.template.urp-blank`), apart from the editor's.
- **Web (itch.io):** **Build → WebGL for itch.io** (`DontCallMe.Editor.Build.WebBuild.Build()`) writes `Builds/WebGL` and `Builds/DontCallMe-WebGL.zip` (index.html at the root, about 85 MB; `Builds/` is ignored by git). Upload the zip as an HTML game.
  - From the menu the editor switches to WebGL and back, which reimports everything twice. Without touching the open editor: clone the project (`cp -Rc` of Assets, Packages, ProjectSettings and Library into a temp folder) and run `Unity -batchmode -quit -projectPath <clone> -buildTarget WebGL -executeMethod DontCallMe.Editor.Build.WebBuild.Build -logFile <log>`. About 4 minutes the first time, half a minute after that.
  - The page is `Assets/WebGLTemplates/DontCallMe/index.html`: the canvas fills the frame, so itch.io's viewport size and fullscreen button decide the size.
  - What differs in a browser: the build starts on the **PC** quality level (`WebBuild.UsePcQuality`; the URP template's default for WebGL is Mobile, which has no ink lines); no Quit row on the title; dragging to look does not lock the cursor (`FirstPersonController.LockCursorOnDrag`); music loops are decoded on load instead of streamed (the audio importer takes no WebGL override, so the default settings are the web's and Standalone has the override); the save lives in the browser's IndexedDB. In browser fullscreen the first Esc leaves fullscreen.
  - All 376 voice clips are decoded when the Room scene loads (about 350 MB in the browser; the tab uses about 1 GB). Loading only the day's lines would cut that, if it ever matters.
  - Testing it without a window: serve `Builds/WebGL` with `python3 -m http.server`, start Chrome with `--headless=new --remote-debugging-port=… --enable-gpu --use-angle=metal`, and drive the page over the DevTools protocol (mouse, keys, screenshots). Pick the tab by its URL and call `Page.bringToFront` first: a screenshot of a background tab never returns.

## Testing

**Offline compile.** No Unity focus is needed. Unity's own `.rsp` files are reused, with outputs sent to a temp dir:

```bash
O=/tmp/dcm-csc; mkdir -p $O; cd <repo>
python3 - "$O" <<'EOF'
import glob, os, sys
O = sys.argv[1]
dag = os.path.dirname(glob.glob("Library/Bee/artifacts/*.dag/DontCallMe.rsp")[0])
src = sorted(glob.glob("Assets/_Game/Scripts/**/*.cs", recursive=True))
for name, files in (("DontCallMe", [f for f in src if "/Editor/" not in f]),
                    ("DontCallMe.Editor", [f for f in src if "/Editor/" in f])):
    lines = []
    for l in open(os.path.join(dag, name + ".rsp")).read().splitlines():
        if (l.startswith('"Assets/') and l.endswith('.cs"')) or l.startswith(("-out:", "-refout:")):
            continue
        lines.append(l.replace(f"{dag}/DontCallMe.ref.dll", f"{O}/DontCallMe.dll"))
    lines += [f'-out:"{O}/{name}.dll"'] + ['"%s"' % f for f in files]
    open(os.path.join(O, name + ".rsp"), "w").write("\n".join(lines) + "\n")
EOF
U=/Applications/Unity/Hub/Editor/6000.4.11f1/Unity.app/Contents
for a in DontCallMe DontCallMe.Editor; do $U/Resources/Scripting/NetCoreRuntime/dotnet $U/Resources/Scripting/DotNetSdkRoslyn/csc.dll @$O/$a.rsp > $O/$a.log 2>&1; echo "$a exit $?"; grep " error " $O/$a.log | head; done
```

**UI tour.** `DontCallMe.Editor.UI.UITourMenu.Capture()` (or the menu UI → Capture UI Tour) screenshots every panel and app into `Temp/UITour` and plays Day 1's call twice; failed checks log `[UITour] FAILED`. It expects 27 screenshots and 0 failures. It needs the Room scene (which opens on Day 1) on Day 1's first case: from the menu it asks for it itself; when Play mode is already running, call `GameRun.ForceNextVariant("scam")` and load the Room scene (`GameRun.PendingDay = -1; SceneFlow.Load(SceneFlow.Room)`) before `Capture()`. It can be stepped like any play test: step until `FindAnyObjectByType<UITour>().done`. Its screenshots use `ScreenCapture`, which writes nothing while the Game view is hidden (the checks still run); `GameCapture` does not have that limit.

**Unity through MCP** (unity-mcp `Unity_RunCommand`, `Unity_ManageEditor`):

- **Domain reloads:** MCP answers "Unity not detected" for up to 20 seconds during a reload (entering Play mode reloads too). Retry once. The live log is usually `~/Library/Logs/Unity/Editor-prev.log` (look for "Tundra build success" or "error CS").
- **RunCommand quirks:**
  - The class must be `internal class CommandScript : IRunCommand`.
  - No System.Reflection and no `System.Diagnostics.Stopwatch`.
  - `result.Log` ignores `{0:F0}`-style formats, so use string interpolation.
  - A warning logged during the command (a failed tour check) makes the call report an error although it ran.
- **The live Game view is not 16:9** (about 2077 × 992 panel pixels on the user's screen), while `GameCapture` lays the UI out again at 1920 × 1080. Never place UI from `worldBound` pixels: anchor it in the layout (a child of what it belongs to), or the capture and the game disagree.
- **Driving Play mode while the editor sits in the background:**
  - Pause it with `EditorApplication.isPaused = true`.
  - Loop `EditorApplication.Step()` with `Thread.Sleep(20)`, because UI Toolkit schedules run on real time. Keep one command under about 4,000 steps.
  - Screenshot with `DontCallMe.Editor.Tools.GameCapture.Capture("Temp/X/name.png")`; add `, 1280, 720` to see what a small Game view shows.
  - Click by sending `ClickEvent` and, for Buttons, also `NavigationSubmitEvent` to the element.
  - Answer a call with `(ui.PhoneView.Top as IncomingCallView)?.Slider.Complete()`.
  - On the title screen: type with `InputSystem.QueueTextEvent(Keyboard.current, c); InputSystem.Update();` (the DEMO switch). The Start button has the class `start-btn`, the menu rows `title-row` (Choose a day, Case Files, Settings, Quit in that order; Choose a day is left out when Start itself opens it). The day select's pages have the class `day-pick` (`day-pick--next`, `day-pick--locked`) with a `day-pick__btn`; `SceneFlow.ShowDays = true` before loading Home opens it at once.
  - Pick a reply with `ui.Transcript.Pick(i)`; `director.HasDecision` and `director.IsRinging` tell when.
  - Start investigating: find the Button whose text is `Loc.T("Start investigating")`.
  - Clues found so far: `day.Clues.Count` / `day.Clues.Total` (`DayDirector.Clues`).
  - Walking and clicks in the room: a queued mouse button never reaches `wasReleasedThisFrame` while the editor is in the background, so drive a click through `FindAnyObjectByType<Interactor>().Use(interactable)` (opens it in reach, or walks over and opens it on arrival; step until `ui.OpenPanelView` is set). `player.WalkToward(point)` walks anywhere (`player.AutoWalking` while it goes); `Interactor.InReach(PanelId)` / `InView(PanelId)`. Queued mouse positions do work, for hovering (`InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = p }); InputSystem.Update();`).
  - Open a panel with `ui.OpenPanel(PanelId.Computer)`. On the computer, tabs have the class `check__tab` and the one-click chips `chip`; drawer documents are `drawer__tab` buttons.
  - Raise or lower the phone with `ui.PhoneView.Raise(true)`; the Apps button on the call screen is the first `call__small`'s `call-btn`, and `return-bar` goes back to the call.
  - Open a phone app with `ui.PhoneView.OpenApp<ChatsApp>()`, a thread with `phone.Push(phone.App<ChatsApp>().OpenThread("villa"))` (ids: `family`, `villa`, `yuna`, `cafe`, `hangang`, `alistar`).
  - Verdict buttons have the classes `verdict-btn--gold` / `verdict-btn--red` / `verdict__go`.
  - A later day without playing the earlier ones: write their records into PlayerPrefs `DCM.Run` before entering Play mode (`{"days":[{"day":0,"variant":"scam","scam":true,"outcome":1,…}]}`; outcome 0 = went along, 1 = refused, 3 = timeout), then press Continue.
  - Another day, case or truth: `GameRun.ForceNextVariant("brother_scam"); SceneFlow.PlayDay(2);` (ids: `scam`, `legit`, `paper_scam`, `paper_legit`, `sale_scam`, `sale_legit`, `brother_scam`, `brother_legit`, `dentist_scam`, `dentist_legit`). A forced variant skips the legit rule.
  - The call's opening goes faster with `Time.timeScale = 4f` while stepping (set it back to 1 for captures and the investigation; scene loads reset it).
  - What a later day shows after any outcome, without playing: `DaySetup.Compose(day, variant, records, catalog)` in Edit mode, then read `plan.dayCardLines`, `plan.phone` and `plan.room`.
  - Force the deadline: `GameClock.Start("16:59")` during the hold.
- **Leave the user's state as you found it:**
  - Back up PlayerPrefs `DCM.Run` (their saved week), `DCM.Played` (which cases and truths they have finished, their trick cards and which days are open) and `DCM.Language` (0 = English, 1 = Korean, unset = system language) before a test, and restore them afterwards; delete `DCM.ForceVariant` and `DCM.Demo` if a test left them set. Every finished day overwrites `DCM.Run` and counts in `DCM.Played`, and the UI tour writes `DCM.Language`. Restore them whenever you leave Play mode to edit, not only at the end: the user may press Play in between.
  - Keep `PlayerSettings.runInBackground = false`, and the Input System's `backgroundBehavior = ResetAndDisableNonBackgroundDevices` and `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`.
  - The Room scene's `DayDirector` stays on Day 1 with an empty `forceVariant`. Leave the Home scene open.
- **Editing C# during Play mode** recompiles mid-play. Finish the play test, exit Play mode, then edit.
- **Font assets** under `UI/Fonts/Generated` change by themselves in Play mode (dynamic atlases gain glyphs). That is noise, not a change to review.

## State as of 2026-10-01

- **Since 2026-11-16, not committed yet:** the ElevenLabs voices for Day 0 (`tts.py`, `check_voices.py`), the skip button, and the case notebook (content version 9: claims, marks, red pen, Day 0 rebuilt around three claims, a Mapo City Gas contact). Verified in Unity by stepping the paused game: the rent case from the call to the next morning (circling in the drawer and the chat, stamps, the red pen), Day 0 up to circling a lookup with the guide, the parcel case in Korean, `ContentCheck` (186 marks, 0 problems) and the UI tour (27 screenshots, 0 failures). Then (content version 10) Day 3 writing its own claims and the DAY card's stars and NEW TODAY, verified the same way: the parcel case in English from the DAY card to the red pen (pinning a claim and a non-claim line, the fourth pin refused, Take out, an unpinned claim's evidence under "Other notes", "You never wrote this one down"), the dentist case's DAY card and a pin in Korean, the UI tour again (27, 0). The red pen's notes had collapsed to one letter a line (`.end-claim` now stretches its rows). Then (content version 11) trick cards, truths seen and the playtest log: Day 1's protected account played to the morning with two false claims stamped (two new cards under the red pen), Case Files' three pages captured, the log summarized by `summarize.py`, `ContentCheck` (0 problems), the UI tour (27, 0). Not tried: the log's download in a browser build. Then (2026-11-16, no content change) the day select: days open for good, Choose a day on the title, DAY n OPENED on the morning, the week's summary going back to the day select, verified by stepping (plan §12). Then (content version 12) Day 4, the video call (plan 7.5.1 and §12), and the Day 3 newspaper prints its desk needs. Then (2026-11-17, content version 13) reach and walking: the cork board moved to the wall by the door (`bedroom_props.py`, by a subagent "art team"; the timetable and a photo string fill the wall above the desk), a 2 m reach with click-to-walk, the view's drag ease, padded trigger click boxes, the guide's walk hints and "the calendar by the door" in the content (plan §3 Reach and walking, §12). Then (2026-11-18, content version 14) the protected case's `news_script` mark moved from a front-page paragraph (replaced by Day 0's next-morning page on the normal path) to a local item, and `ContentCheck` now flags any mark on a day's own headline or body after Day 0; checked by composing Day 1 after a Day 0 record in both languages. A scenario brief of every case is in Figma (file "Don't call me 1111", page "Level Scenarios"), with a proposal on each open question. Then (2026-11-18, content version 15) those proposals, all approved by the user: multi-claim marks, circleable caller answers, the bank's "nothing received yet today" row, hints for every claim, Minjun's answer from Mom, the Stranded abroad card, week 1 plays each day's first case, the legit rule by Day 3, lasting chats and notes, `pt_courier` (plan's "Level review" note at the top and §12). Next: the other callers' voices (needs a higher ElevenLabs limit or a decision on paying, Yuna included; the user is discussing it).
- `main` = `origin/main` = `c1b7f6d` holds the scope cut, Day 0 (the tutorial case), the title layout, Case Files, the readable-text pass, the staged reveal (spotlight and drumroll), the phone's dark gradient background and larger call buttons, and the case pools with four new cases (content version 8, 376 voice clips). A teammate also pushes to `main`: check `git log origin/main`.
- **Not committed yet** (the user commits): the browser build. `Editor/Build/WebBuild.cs`, `Assets/WebGLTemplates/DontCallMe`, the title without a Quit row and the look drag without a cursor lock in a browser (`TitleScreen.cs`, `FirstPersonController.cs`), the music's import settings (`AudioPipeline.ImportMusic`, four `.wav.meta`), and WebGL's default quality level (`ProjectSettings/QualitySettings.asset`). `~/Downloads/DontCallMe-WebGL.zip` is that build.
- Verified for the browser build in headless Chrome (1280 wide, 60 fps): Day 0 from the title to the next morning in English (look drag, newspaper, slide to answer, replies by key, the computer by chip and by typing, Esc, Tab, Chats, the verdict), the save after a reload and after a browser restart, Korean on the title and through Day 1's call and case file, and what the page plays (music as one gapless looping buffer once the first click wakes the audio, both investigation layers started on the same sample, the caller's clips). Not checked: the sound by ear, other browsers, Days 2 and 3.
- **The phone is the committed one** (caller's portrait, slide to answer, no decline, the hand), with those two changes. A pixel-art phone and a navy call screen with a decline button were both tried on 2026-10-01 and the user asked for the original back. Do not restyle the phone unless asked.
- Verified in Unity for the case pools, by stepping the paused game: each new case from the DAY card to the next morning in English or Korean (both truths between them, all three outcomes, the week summary); in every one the clue was only found in its one place, not after the computer's lookups; the evidence screen of every new variant in both languages; the days after every outcome of the new cases (composed in Edit mode); the pick's odds and the legit rule; the UI tour (0 failures). Voices were only checked by clip and pace (audio does not run while the editor is paused): listen to the new callers in a normal Play session.
- The next morning's reveal is staged for suspense (`EndScreen`: constants `PaperAt`, `CardAt`, `SuspenseAt`, `VerdictAfter`, `RestAfter`; a spotlight on the stamps' spot (`end-spot`, anchored in the stamps' row) and a drumroll: sounds `Sfx.Suspense`, `Roll`, `Reveal`, `RevealGood`, `RevealBad`; `MusicPlayer.Hold()` / `PlayMain()` keep the music back until the verdict).
- Known small things: an echo's chat message counts as unread the next day even when it repeats what was written the day before (older ones no longer count).
- Not localized: the 3D room's own textures (the notes on the wall, the laptop lock screen). Only the desk newspaper swaps to a Korean print.
- The living room of the first version (`SM_LivingRoom.fbx`, `Tools/ArtGen/legacy`) is still in the repo.
- Next per the plan (§13): playtests with the target audience, then tuning. More cases can be added the same way (ideas: a clue in the drawer, in the residents' chat, in Contacts).
