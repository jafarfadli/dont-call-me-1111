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
- **On the phone** (Tab, held in a hand): Contacts, Chats, Nuri Bank. All three are read-only evidence.
- **Scenes:** Home (title), Room (the day), End (next morning, week summary).
- **Run:** 3 days, Tue 6 – Thu 8 Oct 2026, each harder than the last.
  - Day 1 "protected account": always a scam, 2 clues, both on the computer. It runs with a tutorial (`TutorialGuide`, 7 steps).
  - Day 2 "new rent account": scam or legit, 3 clues, no fraud reports (compare names with the lease and the residents' chat).
  - Day 3 "held parcel": scam or legit, 4 clues (courier chat, shop chat, delivery slip or number lookup, account lookup).
  - A run always has at least one legit day. Days remember earlier days: money, yesterday's paper on the desk, and "echoes" (chat messages, bank transactions, a saved contact, a day-card line).
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
  - `DaySetup`: picks the variant and composes a day from history and echoes.
  - `GameRun`: save in PlayerPrefs `DCM.Run`.
  - `TutorialGuide`: Day 1's guide. The step follows the game state; the call waits for the newspaper to be read (75 s at most).
  - Also `CallDirector`, `ClueTracker`, `SceneFlow`.
- `Gameplay/`: `Interactor` (cursor raycast, prompt, highlight) and `Interactable`. **One MonoBehaviour per file**, named like the class: two in one file broke the scene's script reference and silently killed all interaction.
- `UI/`:
  - `UIManager`: panels, the phone, pause, confirms.
  - `Panels/RoomPanels.cs`: `NewspaperPanel`, `DrawerPanel`, `CalendarPanel`, `ComputerPanel` (`PanelId`).
  - `Phone/`: `CommsApps.cs` (Contacts, Chats), `MoneyApps.cs` (Bank), `CallViews.cs`, `PhoneController.cs` (the hand).
  - `Call/` (transcript, verdict panel), `Hud/` (HUD, `TutorialCard`), `Menus/` (title, settings, end, week), `Day/`, `Core/` (UIKit, UISkin, GameClock, Clipboard, ClueEvents).
  - `UITour.cs`: the screenshot tour, English only.
- `Editor/`:
  - `UI/ContentBuilder*.cs`: all game content, see below.
  - `UI/UIPipeline.cs`: sprites, font assets, panel settings and text settings, UISkin, scene UI. Its `Interactables` table says which scene objects can be used.
  - `Audio/AudioPipeline.cs`, `Art/RoomArtPipeline.cs`, `Scenes/ScenePipeline.cs`.
  - `Tools/GameCapture.cs`: renders the paused game to a PNG even when the Game view is hidden.
- Shaders (`Assets/_Game/Art/Shaders`): `DCM_Toon` (property `_Highlight`: RGB adds light, A marks the object) and `DCM_InkComposite` (ink lines, plus the yellow outline around marked objects).

## Content and localization rules

- **Content is code.** `ContentBuilder` (Household, Day1–3) builds every asset under `Assets/_Game/Data/Day1..Day3`. It runs twice: once in English, and once in Korean into each day's `ko/` folder (`*_ko.asset`, listed in `Resources/DayCatalog_ko`).
  - Write every string as `L("English", "한국어")`.
  - Shared names that clues point at are properties (`NuriBank`, `Landlord`, `LeaseTitle`, `VillaChat`…), so a clue and its target always match.
  - Do not hand-edit the generated assets; change the builder and rebuild.
  - Bump `ContentBuilder.Version` when built content changes, so open projects rebuild by themselves.
- **Clues** are found through `ClueEvents`: `PanelOpened` (panel name), `DocumentViewed` (drawer document title), `NumberChecked` (number or account looked up on the computer), `RecipientShown` (the verdict's send step), `ChatRead` (thread id), `BankOpened`. Keep the count per day at 2, 3 and 4.
- **UI strings in C#** use `Loc.T("English")` or `Loc.F("… {0}", x)`. Add the Korean to `Data/LocKo.cs`. A missing entry falls back to English and logs `[Loc] No Korean` in the editor.
- **Voices** are macOS `say` voices, one per caller per language: Daniel / Rocko (Day 1), Reed (US) / Reed (Korean) (Day 2), Shelley (UK) / Yuna (Day 3). For numbers read aloud, the Korean `spoken` field uses digit words (`일일공, 구공공…`). Chats have no voice.
- **Pictures with writing on them** have Korean twins `UI/Sprites/*_ko.png` (calendar, receipt). `ContentBuilder.Tex()` picks `_ko` automatically in Korean.
- **Desk newspaper prints** are `Art/Textures/Papers/T_Paper_D*_*.png`, with `_ko` versions.
- **Fonts:**
  - The UI fonts are Nanum Gothic (UI), Do Hyeon, Gaegu (handwriting), Crimson Text, DM Serif Display, Nanum Myeongjo and DotGothic16. Latin-only fonts fall back to Nanum.
  - **No emoji**, no U+2212 minus, no `é`, no `✓`: they render as boxes. `♥` is fine.
  - Korean wraps per word through `UI/Settings/PTS_Game.asset` (modern Hangul line breaking).
- **Python:** use `/Library/Frameworks/Python.framework/Versions/Current/bin/python3`, which has numpy, PIL and fontTools.

## Rebuild steps (in Unity, menus under Tools → Don't Call Me)

1. **Content → Build Days** (`ContentBuilder.Build()`): both languages, and it exports `Tools/ArtGen/papers.json`.
2. **Content → Print Desk Newspapers.** Or run `python3 Tools/ArtGen/tex_prints.py papers`, then Build again to link the prints.
3. **Voices:** `AudioPipeline.ExportLines()` → `python3 Tools/Audio/tts.py` (shell; keeps existing clips, removes unused ones) → `AudioPipeline.ImportVoices()`. Or use **Audio → Run Voice Pipeline**, which blocks Unity for a while.
4. **UI art:** `python3 Tools/ArtGen/ui_art.py` → **UI → Run UI Pipeline**. Pass 3 is `BuildSettings()` (skin and text settings) if you only need that part; pass 4 (`SetupScene()`) puts the UI, the directors and the interactables in the Room scene.

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

**UI tour.** `DontCallMe.Editor.UI.UITourMenu.Capture()` (or the menu UI → Capture UI Tour) screenshots every panel and app into `Temp/UITour` and plays Day 1's call twice; failed checks log `[UITour] FAILED`. It expects 27 screenshots and 0 failures.

**Unity through MCP** (unity-mcp `Unity_RunCommand`, `Unity_ManageEditor`):

- **Domain reloads:** MCP answers "Unity not detected" for up to 20 seconds during a reload (entering Play mode reloads too). Retry once. The live log is usually `~/Library/Logs/Unity/Editor-prev.log` (look for "Tundra build success" or "error CS").
- **RunCommand quirks:**
  - The class must be `internal class CommandScript : IRunCommand`.
  - No System.Reflection and no `System.Diagnostics.Stopwatch`.
  - `result.Log` ignores `{0:F0}`-style formats, so use string interpolation.
  - A warning logged during the command (a failed tour check) makes the call report an error although it ran.
- **Driving Play mode while the editor sits in the background:**
  - Pause it with `EditorApplication.isPaused = true`.
  - Loop `EditorApplication.Step()` with `Thread.Sleep(20)`, because UI Toolkit schedules run on real time. Keep one command under about 4,000 steps.
  - Screenshot with `DontCallMe.Editor.Tools.GameCapture.Capture("Temp/X/name.png")`.
  - Click by sending `ClickEvent` and, for Buttons, also `NavigationSubmitEvent` to the element.
  - Answer a call with `(ui.PhoneView.Top as IncomingCallView)?.Slider.Complete()`.
  - Pick a reply with `ui.Transcript.Pick(i)`; `director.HasDecision` and `director.IsRinging` tell when.
  - Start investigating: find the Button whose text is `Loc.T("Start investigating")`.
  - Open a panel with `ui.OpenPanel(PanelId.Computer)`. On the computer, tabs have the class `check__tab` and the one-click chips `chip`; drawer documents are `drawer__tab` buttons.
  - Open a phone app with `ui.PhoneView.OpenApp<ChatsApp>()`, a thread with `phone.Push(phone.App<ChatsApp>().OpenThread("villa"))` (ids: `family`, `villa`, `yuna`, `cafe`, `hangang`, `alistar`).
  - Verdict buttons have the classes `verdict-btn--gold` / `verdict-btn--red` / `verdict__go`.
  - Another day or truth: `SceneFlow.PlayDay(n)` and repeat until `day.Plan.variant.id` is the one wanted; replace an earlier day's record with `GameRun.Record(...)` first when the legit rule would force the truth.
  - Force the deadline: `GameClock.Start("16:59")` during the hold.
- **Leave the user's state as you found it:**
  - Back up PlayerPrefs `DCM.Run` (their saved week) and `DCM.Language` (0 = English, 1 = Korean, unset = system language) before a test, and restore them afterwards. Every finished day overwrites `DCM.Run`, and the UI tour writes `DCM.Language`.
  - Keep `PlayerSettings.runInBackground = false`, and the Input System's `backgroundBehavior = ResetAndDisableNonBackgroundDevices` and `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`.
  - The Room scene's `DayDirector` stays on Day 1 with an empty `forceVariant`. Leave the Home scene open.
- **Editing C# during Play mode** recompiles mid-play. Finish the play test, exit Play mode, then edit.
- **Font assets** under `UI/Fonts/Generated` change by themselves in Play mode (dynamic atlases gain glyphs). That is noise, not a change to review.

## State as of 2026-10-01

- The scope cut is committed on `main` (`dcec57f`): three apps, four room objects, the computer, the tutorial, the highlight, the hand, content version 6, 152 voice clips. Check `git status` and `git log origin/main` for what came after; a teammate also pushes to `main`.
- After that commit: the desk chair stands back from the desk, clicks pick the nearest usable object anywhere in the room (a press becomes a drag only after 10 px), the highlight line is thin, and the transcript scrolls inside its frame.
- Verified in Unity after the cut: Days 1–3 end to end in English and Korean, both truths of Days 2 and 3, all three outcomes, the week summary, a saved week from before the cut continuing into Day 3, and the UI tour (0 failures).
- The assets of the cut features were deleted on 2026-10-01 with the user's go-ahead (old app icons, board, wallet and notebook art, unused portraits, dead rules in `Phone.uss`, empty voice folders, the sample chat), and their generators were removed from `ui_art.py`. The living room of the first version (`SM_LivingRoom.fbx`, `Tools/ArtGen/legacy`) is still there.
- Not localized: the 3D room's own textures (the notes on the wall, the laptop lock screen). Only the desk newspaper swaps to a Korean print.
- Next per the plan (§13): playtests with the target audience, then tuning.
