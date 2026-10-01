# Don't Call Me! — context for Claude sessions

Read this first. It sums up the project, how the user works and how to build and test, so a new session can continue without the old conversation. The full design lives in `docs/technical-plan.md` (the source of truth; keep it updated when behaviour changes). Asset pipelines are in `Tools/ArtGen/README.md` and `Tools/Audio/README.md`.

## Working with the user

- Reply to the user in Indonesian, short and direct. Code, comments, docs, commit messages: normal English.
- Work directly. No superpowers/brainstorming/spec/writing-plans ceremony, no `docs/superpowers/` files. Ask only blocking questions.
- Quality over speed: do not cut features for time. One coder (the user); teammates do art and content.
- Commit only when asked. Never commit `.DS_Store` files. Ask before `git push` and before deleting assets.
- All brands, people, accounts and phone numbers are fictional. Only 112 (police), 119 (fire/ambulance) and 1332 (FSS fraud hotline) are real.
- The user may be play-testing in Unity. Check `IsPlaying` before touching Unity, and do not stop a session that is in use.

## The game

Unity 6000.4.11f1, URP, Input System, UI Toolkit (C# builders plus USS). It is a first-person scam-call detective game. Jiwoo, a student living alone in Mangwon-dong, Seoul, gets a call each afternoon. The player investigates the room and the phone (nine apps) while the caller holds the line, then gives a verdict on the call: gold "go along" or red "hang up". The next morning's Seoul Daily reveals the truth.

- **Scenes:** Home (title), Room (the day), End (next morning, week summary).
- **Run:** 3 days, Tue 6 – Thu 8 Oct 2026.
  - Day 1 "protected account" is always a scam.
  - Day 2 "new rent account" and Day 3 "held parcel" are each scam or legit at random, with at least one legit day per run.
  - Days remember earlier days: money, the call log, yesterday's paper on the desk, the rules in the notebook, and "echoes" (texts, chats, board notes).
  - The plan describes a later 5-day run drawn from a pool.
- **Languages:** English and Korean (한국어), chosen in Settings on the title screen or the pause menu. Switching mid-day asks first, then restarts the day.

## Code map (`Assets/_Game/Scripts`)

- `Data/`:
  - `DayData` / `DayVariant` / `DayEcho` / `EndPaper`.
  - `RoomContent`, `PhoneContent`, `ConversationData`, `WorldDirectory`, `VoiceBank`.
  - `DayCatalog`: `Load()` picks `Resources/DayCatalog_ko` in Korean.
  - `Loc` + `LocKo` (language, UI strings).
  - `Facts`: number and money helpers.
- `Flow/`:
  - `DayDirector`: phases Waiting → Seated → OnCall → CaseCard → hold → verdict.
  - `DaySetup`: picks the variant and composes a day from history and echoes.
  - `GameRun`: save in PlayerPrefs `DCM.Run`.
  - Also `CallDirector`, `ClueTracker`, `SceneFlow`.
- `UI/`:
  - `UIManager`: panels, pause, confirms.
  - `Phone/` (apps), `Call/` (transcript, verdict panel), `Panels/` (room panels), `Menus/` (title, settings, end, week), `Day/`, `Hud/`, `Core/` (UIKit, UISkin).
- `Editor/`:
  - `UI/ContentBuilder*.cs`: all game content, see below.
  - `UI/UIPipeline.cs`: sprites, font assets, panel settings and text settings, UISkin, scene UI.
  - `UI/UITourMenu.cs`: screenshot tour, English only.
  - `Audio/AudioPipeline.cs`, `Art/RoomArtPipeline.cs`, `Scenes/ScenePipeline.cs`.
  - `Tools/GameCapture.cs`: renders the paused game to a PNG even when the Game view is hidden.

## Content and localization rules

- **Content is code.** `ContentBuilder` (Household, Day1–3) builds every asset under `Assets/_Game/Data/Day1..Day3`. It runs twice: once in English, and once in Korean into each day's `ko/` folder (`*_ko.asset`, listed in `Resources/DayCatalog_ko`).
  - Write every string as `L("English", "한국어")`.
  - Shared names that clues point at are properties (`NuriBank`, `Landlord`, `StickyTitle`…), so a clue and its target always match.
  - Do not hand-edit the generated assets; change the builder and rebuild.
  - Bump `ContentBuilder.Version` when built content changes, so open projects rebuild by themselves.
- **UI strings in C#** use `Loc.T("English")` or `Loc.F("… {0}", x)`. Add the Korean to `Data/LocKo.cs`. A missing entry falls back to English and logs `[Loc] No Korean` in the editor.
- **Voices** are macOS `say` voices, one per character per language. The Korean ones are the "Korean (South Korea)" variants (Rocko, Sandy, Shelley, Flo, Grandma, Grandpa, Reed) and Yuna. For numbers read aloud, the Korean `spoken` field uses digit words (`일일공, 구공공…`). Chats have no voice.
- **Pictures with writing on them** have Korean twins `UI/Sprites/*_ko.png` (board notes, notice, receipt, calendar, card back). `ContentBuilder.Tex()` picks `_ko` automatically in Korean.
- **Desk newspaper prints** are `Art/Textures/Papers/T_Paper_D*_*.png`, with `_ko` versions.
- **Fonts:**
  - The UI fonts are Nanum Gothic (UI), Do Hyeon, Gaegu (handwriting), Crimson Text, DM Serif Display, Nanum Myeongjo and DotGothic16. Latin-only fonts fall back to Nanum.
  - **No emoji**, no U+2212 minus, no `é`, no `✓`: they render as boxes. `♥` is fine.
  - Korean wraps per word through `UI/Settings/PTS_Game.asset` (modern Hangul line breaking).
- **Python:** use `/Library/Frameworks/Python.framework/Versions/Current/bin/python3`, which has numpy, PIL and fontTools.

## Rebuild steps (in Unity, menus under Tools → Don't Call Me)

1. **Content → Build Days** (`ContentBuilder.Build()`): both languages, and it exports `Tools/ArtGen/papers.json`.
2. **Content → Print Desk Newspapers.** Or run `python3 Tools/ArtGen/tex_prints.py papers`, then Build again to link the prints.
3. **Voices:** `AudioPipeline.ExportLines()` → `python3 Tools/Audio/tts.py` (shell; keeps existing clips) → `AudioPipeline.ImportVoices()`. Or use **Audio → Run Voice Pipeline**, which blocks Unity for a while.
4. **UI art:** `python3 Tools/ArtGen/ui_art.py` → **UI → Run UI Pipeline**. Pass 3 is `BuildSettings()` (skin and text settings) if you only need that part.

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

**Unity through MCP** (unity-mcp `Unity_RunCommand`, `Unity_ManageEditor`):

- **Domain reloads:** MCP answers "Unity not detected" for a few seconds during a reload. Retry once. The live log is usually `~/Library/Logs/Unity/Editor-prev.log` (look for "Tundra build success" or "error CS").
- **RunCommand quirks:**
  - The class must be `internal class CommandScript : IRunCommand`.
  - No System.Reflection and no `System.Diagnostics.Stopwatch`.
  - `result.Log` ignores `{0:F0}`-style formats, so use string interpolation.
- **Driving Play mode while the editor sits in the background:**
  - Pause it with `EditorApplication.isPaused = true`.
  - Loop `EditorApplication.Step()` with `Thread.Sleep(20)`, because UI Toolkit schedules run on real time.
  - Screenshot with `DontCallMe.Editor.Tools.GameCapture.Capture("Temp/X/name.png")`.
  - Click by sending `ClickEvent` and, for Buttons, also `NavigationSubmitEvent` to the element.
  - Answer a call with `(ui.PhoneView.Top as IncomingCallView)?.Slider.Complete()`.
  - Pick a reply with `ui.Transcript.Pick(i)`.
  - Open a panel with `ui.OpenPanel(PanelId.Board)`.
  - Open a phone app with `ui.PhoneView.OpenApp<BankApp>()`.
  - Verdict buttons have the classes `verdict-btn--gold` / `verdict-btn--red` / `verdict__go`.
- **Leave the user's state as you found it:**
  - Back up PlayerPrefs `DCM.Run` (their saved week) and `DCM.Language` (0 = English, 1 = Korean) before a test, and restore them afterwards.
  - Keep `PlayerSettings.runInBackground = false`.
  - The Room scene's `DayDirector` stays on Day 1 with an empty `forceVariant`.
- **Editing C# during Play mode** recompiles mid-play. Finish the play test, exit Play mode, then edit.
- **Known harmless warning:** "The referenced script (Unknown) on this Behaviour is missing!" when Room loads. It predates the localization work.

## State as of 2026-10-01

- `main` = `jafar` = `afced1a` ("Add a Korean language option…"). It is committed locally but **not pushed**: `origin/main` is still `d0516f6` and `origin/jafar` is `63bbc7e`. The user has not yet confirmed the push.
- Verified in Unity:
  - A full Korean run: Day 1–3, the week summary, and the phone apps, panels and verdicts.
  - Switching language from the pause menu.
  - Korean voices are in `VoiceBank` (184 clips: 93 English + 91 Korean).
- Not localized yet: the 3D room's own textures (the board notes on the wall, the laptop lock screen). They are small background details; only the desk newspaper swaps to a Korean print. The sample chat (`ContentBuilder.Samples.cs`, F3 in dev builds) is English only.
- Next per the plan: the 5-day run drawn from the scenario pool (`docs/technical-plan.md` §6–7).
