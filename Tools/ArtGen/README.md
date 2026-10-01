# ArtGen — the bedroom, generated from code

Everything you see in `Assets/_Game/Scenes/Room.unity` comes from the scripts in this folder: textures from Python, the room model from Blender, and materials, prefab and scene from a Unity editor pipeline. Edit the scripts and rebuild; do not hand-edit the generated assets, because the next rebuild overwrites them.

The room is a small bedroom in a twenty-year-old Seoul villa, after `references/art/ref5.jpg`: damp-stained painted walls, old vinyl floor, cherry-brown door and mouldings, a sliding window over the bed. The earlier Bekasi living room is kept in `legacy/living_room` for reference and is no longer built.

## Requirements

- Python 3 with `numpy` and `Pillow`
- Blender 5.2 (`/Applications/Blender.app`)
- Unity 6000.4 with this project open

## Rebuild

1. **Textures** — writes `Assets/_Game/Art/Textures/T_*.png`
   ```
   python3 Tools/ArtGen/tex_surfaces.py
   python3 Tools/ArtGen/tex_prints.py
   ```
2. **Room model** — writes `Assets/_Game/Art/Models/SM_Bedroom.fbx`
   ```
   /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/ArtGen/blender/build_room.py
   ```
   Add `-- --preview <folder>` to also render quick Workbench previews of the layout.
3. **Unity** — menu **Tools → Don't Call Me → Art → Run Full Pipeline**. The numbered menu items run single steps (textures, materials, model, prefab, renderer, scene).

To check the look, use **Tools → Don't Call Me → Art → Capture Audit Views**. It renders every camera under `AuditCameras` in the Room scene to `Temp/AuditViews/`.

**Tools → Don't Call Me → Art → Remove Stale Art...** lists generated assets the pipeline no longer uses (materials missing from `materials.json`, textures no material uses, older room models and prefabs, such as the living room's) and deletes them after you confirm.

## The 2D UI

The UI (UI Toolkit) takes its art from this folder too. It reuses printed art from the room, so rebuild the textures first.

1. **UI art** — writes `Assets/_Game/UI/Sprites` (9-slice frames, buttons, chips, the phone frame, the hand that holds it, app icons, glyphs, pixel portraits of every caller, chat avatars, documents, menu glyphs, the title logo and its shade) and `slices.json` with the 9-slice borders. Pictures with writing on them also come as `*_ko` versions written in Korean (handwriting in Gaegu, from the game's fonts); the Korean days use the calendar and the receipt
   ```
   python3 Tools/ArtGen/ui_art.py
   ```
2. **Unity** — menu **Tools → Don't Call Me → UI → Run UI Pipeline**. It imports the sprites with their borders, builds the font assets (with Hangul fallbacks), the panel settings (with text settings that wrap Korean between words) and the `UISkin`, and sets up the Room scene: EventSystem, `GameUI`, `Flow` (`DayDirector`, `CallDirector`, the music player), the `Interactor` on the player and an `Interactable` with a box collider on each object the player can use. Those are listed in `UIPipeline.Interactables`: the newspaper, the wall calendar, the desk drawer and the laptop. The wallet, the cork board and the notebook (`UIPipeline.Scenery`) stay as props. The art pipeline's scene step runs that last part as well.
3. **Content** — **Tools → Don't Call Me → Content → Build Days** rewrites `Assets/_Game/Data/Day1..Day3` (per day and truth: the phone's contents, the directory of numbers and accounts the computer looks up, the room's newspaper, documents and calendar, the call, the clues, the next morning's papers and the rule, plus the echoes of earlier days) and `Resources/DayCatalog`. Every day is written twice: in English and, in a `ko` folder next to it, in Korean (listed in `Resources/DayCatalog_ko`).
   **Tools → Don't Call Me → Content → Print Desk Newspapers** writes `Tools/ArtGen/papers.json` (every front page that lies on the desk the next day), runs `python3 Tools/ArtGen/tex_prints.py papers` to print each one in the Day 1 paper's style to `Assets/_Game/Art/Textures/Papers/` (768 × 1050) and links the prints to their papers. Korean front pages (`"lang": "ko"`) print as a Korean edition, `T_Paper_D*_ko` (AppleMyungjo masthead, Apple SD Gothic Neo headline, Korean columns); the Korean Day 1 room gets its own print of the opening paper. `Remove Stale Art` leaves that folder alone.
4. **Audio and scenes** — voices and music come from `Tools/Audio` (see its README). **Tools → Don't Call Me → Scenes → Build Home and End Scenes** builds the title and next-morning scenes around the same bedroom (the End scene in morning light) and sets the build order Home, Room, End.

**Tools → Don't Call Me → UI → Capture UI Tour** enters Play mode in English and saves a screenshot of every room panel and phone app, both lookups on the computer, and Day 1's call played twice (hang up, then send the money) to `Temp/UITour`. Failed checks are logged as `[UITour] FAILED`.

| What | Where |
| --- | --- |
| Colours, fonts, sizes, animation | `Assets/_Game/UI/Uss/*.uss` (colour tokens at the top of `Theme.uss`) |
| Frames, icons, portraits, cards and documents | `ui_art.py` |
| Screen layouts | C# builders in `Assets/_Game/Scripts/UI` (`Panels/` for the room, `Phone/` for the apps and call screens, `Call/` for the transcript) |
| Jiwoo's week (contacts, chats, bank, the lease and other papers, the calendar's notes, who owns which number and account) | `Assets/_Game/Scripts/Editor/UI/ContentBuilder.Household.cs` |
| Each day's call, evidence, clues, papers and echoes | `ContentBuilder.Day1.cs`, `ContentBuilder.Day2.cs`, `ContentBuilder.Day3.cs` |
| The hand that holds the phone (fingers behind it, thumb in front) | `ui_art.py` (`phone_hand`) |
| The title logo and menu glyphs | `ui_art.py` (`title_logo`, `menu_glyphs`) |

## Where to change things

| What | Where |
| --- | --- |
| Colours, ink detail, hatching and glints per material | `materials.json` (names match the Blender and Unity materials) |
| Walls, window, door, mouldings, switches, air conditioner, ceiling light | `blender/bedroom_shell.py` |
| Furniture (bed, wardrobe, shelves, desk, chair, TV cabinet, ...) | `blender/bedroom_furniture.py` |
| Where everything stands, desk items, cork board, posters, photos | `blender/bedroom_props.py` |
| The view from the window (neighbour's rooftop, power lines, city) | `blender/bedroom_exterior.py`, backdrop painting in `tex_prints.backdrop` |
| Reusable small props (paper, tape, frames, drawers, plants, fan, cat, interactables) | `blender/props.py` |
| Printed art (newspaper, calendar, notes, photos, labels, posters, rug, clock) | `tex_prints.py` |
| Atlas layouts shared by the textures and the UVs | `atlas.py` |
| Surfaces (wood, plaster, vinyl floor, bedding, brick) and the wall paint with its stains | `tex_surfaces.py` (`WALL_FEATURES` places leaks, mould, grime and poster ghosts per wall) |
| Pixel-art people | `pixel_people.py` |
| Window fill light, contact shadows, hatching scale | `DCMLook` component on `Lighting/WindowFill` (set in `RoomArtPipeline.BuildScene`) |
| Sun, ambient light, player spawn, audit cameras, colliders | `RoomArtPipeline.BuildScene` and `AddColliders` |
| Outline thickness and paper grain | `Assets/_Game/Rendering/M_InkComposite.mat` (set in `RoomArtPipeline.SetupRenderer`) |
| Colour grading | `Assets/_Game/Rendering/VP_Room.asset` (set in `RoomArtPipeline.BuildVolumeProfile`) |

## How the look works

- `DontCallMe/Toon` shades in hard bands: shade (ambient), window fill, sunlight with hard shadows, plus banded SSAO for contact shadows and world-space hatching in shade.
- The `DCM Ink` renderer feature on `PC_Renderer` draws outlines from depth, normals and a per-material ink id stored in the normals alpha, so every material boundary gets a line.
- Things the player can use glow: `Interactable.SetHighlight` sets `_Highlight` on the object's renderers (RGB adds warm light, A marks it). A marked object writes a negative ink id, and the ink pass draws a pulsing yellow outline along its silhouette (`_HighlightColor` and `_HighlightWidth` on `M_InkComposite`).
- Dust motes (`DontCallMe/SunMote`) sample the sun's shadow map and only glow inside the sunbeam.
- The palette is retro: dusty blue paint gone yellow under the ceiling, honey vinyl floor, cherry-brown trim, blue gingham bedding, floral curtains, a navy-red-ochre rug. The volume adds a faded print grade (lifted blacks, blue shadows, warm highlights, muted greens, film grain) and the ink is dark brown on aged paper.
- Late-afternoon sun comes low through the west window, over the bed and onto the floor. Looking out, the neighbour's rooftop is backlit, as it would be.

## Player

`Player` in the Room scene is a first-person rig: `CharacterController` + `FirstPersonController` (`Assets/_Game/Scripts/Player`), camera at eye height, starting by the door. WASD or left stick to walk, Shift to walk faster. To look around, hold a mouse button and drag (the cursor hides while turning and comes back where you clicked); the right stick also turns the view. Drag sensitivity, dead zone and drag direction are on the component. Open panels and the raised phone call `SetInputLocked(true)` to stop walking, and presses that start on the UI never turn the view.

Walls, floor, ceiling and the closed door have mesh colliders. Furniture gets knee-high box colliders: they stop the player but leave a clear line of sight (and of click) to things standing on it.

## Conventions

- Blender coordinates are metres, Z up, room centre at the origin, north `+Y`; the window wall is west (`-X`). In Unity that becomes `(x, z, y)`, north `+Z`.
- Groups named `INT_*` are the desk's and the wall's things: `INT_Newspaper`, `INT_Drawer` (top desk drawer, with the bills inside), `INT_Wallet`, `INT_Rulebook`, `INT_BulletinBoard` (cork board). Parts inside a group are named without the prefix. The player can use four objects: `INT_Newspaper`, `INT_Drawer`, `Board_Calendar` (the calendar on the cork board) and `Laptop`; the UI pipeline gives each a box collider and an `Interactable`.
- Wall items live in groups made by `bedroom_shell.wall_group`: local `-Y` points into the room and local `X` runs left to right as seen from inside.
- People in pictures are pixel art in the style of `references/art/ref4.png`, drawn by `pixel_people.py` (Jiwoo, her parents and grandmother): height-field lighting at 8x resolution, box-downsampled to a small grid and reduced to a small palette.
- Brands, shops, people and their phone numbers are fictional. The public numbers on the notes (police 112, fire and ambulance 119, the financial fraud hotline 1332) are the real ones, on purpose.
