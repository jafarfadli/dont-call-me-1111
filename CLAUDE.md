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

- **In the room** (each opens a 2D panel, each glows with a yellow outline): the newspaper, the wall calendar, the desk drawer, and the computer. The computer is a browser on CheckFirst with two lookups, **Check a phone number** and **Check a bank account**; each gives the owner's name, a registration note and the fraud reports.
- **On the phone** (Tab, held in a hand): Contacts, Chats, Nuri Bank. All three are read-only evidence. A call can only be answered: slide to answer, no decline.
- **Scenes:** Home (title: Start or Continue, Case Files, Settings, Quit), Room (the day), End (next morning, week summary).
- **Run:** 4 days, Mon 5 – Thu 8 Oct 2026 (`GameRun.FirstDay` is 0).
  - Day 0 "unpaid gas bill": the tutorial (`DayData.tutorial`, `TutorialGuide`, 13 steps). Always a scam, with eight clues, one per tool: newspaper, number lookup, account lookup, the gas bill in the drawer, the calendar (a holiday), the bank app (already paid), the residents' chat, contacts. Small sum (₩18,420) and a far deadline (the call comes at 14:10).
  - **Days 1 to 3 each bring one of several cases** (the user asked for variety on 2026-10-01, with clues that are not always on the computer, and only one or two per case). `DaySetup.Pick` takes the case the player has finished least often (PlayerPrefs `DCM.Played`, kept across weeks), then a truth at random.
    - Day 1: "protected account" (`protected`: always a scam, 2 clues, both on the computer), "subscription fee" (`paper`: 1 clue, the paper's own notice in the newspaper box), "extra zero" (`sale`: 1 clue, the bank app's history).
    - Day 2: "new rent account" (`rent`: 3 clues, no fraud reports: compare names with the lease and the residents' chat), "Minjun's broken phone" (`brother`: 1 clue, the family chat).
    - Day 3: "held parcel" (`parcel`: 4 clues: courier chat, shop chat, delivery slip or number lookup, account lookup), "dentist's deposit" (`dentist`: 1 clue, the calendar).
    - Every case but `protected` is a scam or its legit twin. In the four one-clue cases the computer shows the same clean result in both truths, on purpose: the answer is in one other place only.
  - A run always has at least one legit day. Days remember earlier days: money, yesterday's paper on the desk, and "echoes" (chat messages, bank transactions, a saved contact, a day-card line), which name the case they follow.
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
  - `GameRun`: save in PlayerPrefs `DCM.Run`; `FirstDay` (0) and `PendingDay` (−1 when the Room scene is opened directly); cases played (`DCM.Played`, `TimesPlayed`, `MarkPlayed`); `ForceNextVariant(id)` for tools and tests (`DCM.ForceVariant`, read once).
  - `TutorialGuide`: the tutorial day's guide. Before the hold the step follows the phase; during it, it shows the `guide` line of the first clue not found yet, with what the last one taught. The call waits for the newspaper to be read (75 s at most).
  - Also `CallDirector`, `ClueTracker`, `SceneFlow`.
- `Gameplay/`: `Interactor` (cursor raycast, prompt, highlight) and `Interactable`. **One MonoBehaviour per file**, named like the class: two in one file broke the scene's script reference and silently killed all interaction.
- `UI/`:
  - `UIManager`: panels, the phone, pause, confirms.
  - `Panels/RoomPanels.cs`: `NewspaperPanel`, `DrawerPanel`, `CalendarPanel`, `ComputerPanel` (`PanelId`).
  - `Phone/`: `CommsApps.cs` (Contacts, Chats), `MoneyApps.cs` (Bank), `CallViews.cs` (`CallArt`, `IncomingCallView`, `InCallView`), `PhoneController.cs` (the hand, the status bar, the screen stack).
  - `Call/` (transcript, verdict panel), `Hud/` (HUD, `TutorialCard`), `Menus/` (`TitleScreen`, `CaseFilesView`, settings, end, week), `Day/`, `Core/` (UIKit, UISkin, GameClock, Clipboard, ClueEvents).
  - `UITour.cs`: the screenshot tour, English only.
- `Editor/`:
  - `UI/ContentBuilder*.cs`: all game content, see below. One file per case: `Day0`, `Day1` (protected account), `Day1Paper`, `Day1Sale`, `Day2` (rent), `Day2Brother`, `Day3` (parcel), `Day3Dentist`, plus `Household` (the shared timeline) and the core.
  - `UI/UIPipeline.cs`: sprites, font assets, panel settings and text settings, UISkin, scene UI. Its `Interactables` table says which scene objects can be used.
  - `Audio/AudioPipeline.cs`, `Art/RoomArtPipeline.cs`, `Scenes/ScenePipeline.cs`.
  - `Tools/GameCapture.cs`: renders the paused game to a PNG even when the Game view is hidden.
- Shaders (`Assets/_Game/Art/Shaders`): `DCM_Toon` (property `_Highlight`: RGB adds light, A marks the object) and `DCM_InkComposite` (ink lines, plus the yellow outline around marked objects).

## Content and localization rules

- **Content is code.** `ContentBuilder` (Household, Day0–3) builds every asset under `Assets/_Game/Data/Day0..Day3`. It runs twice: once in English, and once in Korean into each day's `ko/` folder (`*_ko.asset`, listed in `Resources/DayCatalog_ko`).
  - Write every string as `L("English", "한국어")`.
  - Shared names that clues point at are properties (`NuriBank`, `Landlord`, `LeaseTitle`, `VillaChat`…), so a clue and its target always match.
  - Do not hand-edit the generated assets; change the builder and rebuild.
  - Bump `ContentBuilder.Version` when built content changes, so open projects rebuild by themselves (the rebuild also prints the papers and runs the voices, which blocks the editor for minutes when there are many new lines: to avoid that, build, print and run the voices by hand first, set `builtWith` on `Day1.asset`, then bump).
  - **A new case** is a file with a `XVariant(bool scam)`, its call, its papers and its echoes, added to its day's `variants`. `StoreVariant(id, scenario, dir, prefix, …)` takes the variant id (unique in the day: `paper_scam`) and the case (`CasePaper`); twins share the case, the caller, the voice and the ask. Use `v.intro` when the day card needs the case's own scene. Echoes go in the next day's list wrapped in `OfCase(CaseX, …)`; money that moves after the call (`EchoBank`) must also be in every later day's list. Helpers: `AddChat` (with a photo caption), `AddTx` (a bank line and the balance), `Note` / `Renote` (calendar), `FrontPage` (yesterday's page when nothing was played), `News`.
- **Clues** are found through `ClueEvents`: `PanelOpened` (panel name), `DocumentViewed` (drawer document title), `NumberChecked` (number or account looked up on the computer), `RecipientShown` (the verdict's send step), `ChatRead` (thread id), `BankOpened`, `ContactsOpened`. Each day's first case has 2, 3 and 4 clues; the other cases have 1 (the user wants 1 or 2 for new ones, and not on the computer). On the tutorial day every clue also has a `guide` line (`Guided(Clue(...), L(...))`), and the guide walks them in list order.
- **UI strings in C#** use `Loc.T("English")` or `Loc.F("… {0}", x)`. Add the Korean to `Data/LocKo.cs`. A missing entry falls back to English and logs `[Loc] No Korean` in the editor.
- **Voices** are macOS `say` voices, one per caller per language: Samantha / Sandy (Korean) (Day 0), Daniel / Rocko (protected account), Grandpa (US) / Grandpa (Korean) (subscription fee), Tessa / Flo (Korean) (extra zero), Reed (US) / Reed (Korean) (rent), Eddy (US) / Eddy (Korean) (Minjun), Shelley (UK) / Yuna (parcel), Karen / Shelley (Korean) (dentist). Speeds are in `RATES` in `Tools/Audio/tts.py`. For numbers read aloud, the Korean `spoken` field uses digit words (`일일공, 구공공…`). Chats have no voice.
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
  - On the title screen: the Start button has the class `start-btn`, the menu rows `title-row` (Case Files, Settings, Quit in that order), Case Files' buttons `week-row__btn`.
  - Pick a reply with `ui.Transcript.Pick(i)`; `director.HasDecision` and `director.IsRinging` tell when.
  - Start investigating: find the Button whose text is `Loc.T("Start investigating")`.
  - Clues found so far: `day.Clues.Count` / `day.Clues.Total` (`DayDirector.Clues`).
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
  - Back up PlayerPrefs `DCM.Run` (their saved week), `DCM.Played` (which cases they have finished) and `DCM.Language` (0 = English, 1 = Korean, unset = system language) before a test, and restore them afterwards; delete `DCM.ForceVariant` if a test left it set. Every finished day overwrites `DCM.Run` and counts in `DCM.Played`, and the UI tour writes `DCM.Language`. Restore them whenever you leave Play mode to edit, not only at the end: the user may press Play in between.
  - Keep `PlayerSettings.runInBackground = false`, and the Input System's `backgroundBehavior = ResetAndDisableNonBackgroundDevices` and `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`.
  - The Room scene's `DayDirector` stays on Day 1 with an empty `forceVariant`. Leave the Home scene open.
- **Editing C# during Play mode** recompiles mid-play. Finish the play test, exit Play mode, then edit.
- **Font assets** under `UI/Fonts/Generated` change by themselves in Play mode (dynamic atlases gain glyphs). That is noise, not a change to review.

## State as of 2026-10-01

- `main` = `origin/main` = `7d8ecfe` holds the scope cut, Day 0 (the tutorial case), the title layout, Case Files, the readable-text pass and the first staged reveal. A teammate also pushes to `main`: check `git log origin/main`.
- **Not committed yet** (the user commits):
  - The reveal's spotlight and drumroll, the phone's dark gradient background and larger call buttons (`EndScreen.cs`, `Sfx.cs`, `Menus.uss`, `Phone.uss`, `UIPipeline.cs`, `ui_art.py`, `spotlight.png`, `wallpaper.png`, `circle_dark.png`).
  - **Case pools and four new cases** (content version 8): `DayVariant.scenario` / `intro`, `DayEcho.scenario`, the pick in `DaySetup`, `GameRun`'s played counts and forced variant, the day card's intro, `ContentBuilder.Day1Paper.cs`, `Day1Sale.cs`, `Day2Brother.cs`, `Day3Dentist.cs`, the new assets under `Data/Day1..Day3` (and `ko`), 36 new desk prints, 186 new voice clips (376 in all, eight voice folders new), the sprites `pt_branch`, `pt_buyer`, `pt_dental`, `board_calendar_moved` (and `_ko`).
- **The phone is the committed one** (caller's portrait, slide to answer, no decline, the hand), with the two changes above. A pixel-art phone and a navy call screen with a decline button were both tried on 2026-10-01 and the user asked for the original back. Do not restyle the phone unless asked.
- Verified in Unity for the case pools, by stepping the paused game: each new case from the DAY card to the next morning in English or Korean (both truths between them, all three outcomes, the week summary); in every one the clue was only found in its one place, not after the computer's lookups; the evidence screen of every new variant in both languages; the days after every outcome of the new cases (composed in Edit mode); the pick's odds and the legit rule; the UI tour (0 failures). Voices were only checked by clip and pace (audio does not run while the editor is paused): listen to the new callers in a normal Play session.
- The next morning's reveal is staged for suspense (`EndScreen`: constants `PaperAt`, `CardAt`, `SuspenseAt`, `VerdictAfter`, `RestAfter`; a spotlight on the stamps' spot (`end-spot`, anchored in the stamps' row) and a drumroll: sounds `Sfx.Suspense`, `Roll`, `Reveal`, `RevealGood`, `RevealBad`; `MusicPlayer.Hold()` / `PlayMain()` keep the music back until the verdict).
- Known small things: an echo's chat message counts as unread the next day even when it repeats what was written the day before; a calendar note that a case adds (the monitor's buyer on Saturday) is only there on that case's day.
- Not localized: the 3D room's own textures (the notes on the wall, the laptop lock screen). Only the desk newspaper swaps to a Korean print.
- The living room of the first version (`SM_LivingRoom.fbx`, `Tools/ArtGen/legacy`) is still in the repo.
- Next per the plan (§13): playtests with the target audience, then tuning. More cases can be added the same way (ideas: a clue in the drawer, in the residents' chat, in Contacts).
