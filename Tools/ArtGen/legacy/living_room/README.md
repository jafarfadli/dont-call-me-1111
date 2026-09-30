# Retired: the Bekasi living room

The first version of the game took place in Ibu Ratna's living room in Bekasi. The setting moved to a Korean bedroom (`references/art/ref5.jpg`), and these scripts are the living room as it was when the switch happened. They are not part of the art pipeline and nothing in the project depends on them.

| File | What it made |
| --- | --- |
| `tex_prints_living_room.py` | Newspaper (Bekasi Morning Post), Javanese calendar, notes, family photos, painting, labels, rug, doily, street backdrop |
| `tex_surfaces_living_room.py` | Terrace tiles, retro cement floor tiles (tegel), kawung batik, striped door curtain |
| `blender/build_living_room.py` and `blender/room_*.py` | `SM_LivingRoom.fbx`: shell, kursi tamu set, bufet, props, terrace and street |
| `materials_living_room.json` | The material list the living room was built with |

To look at it again, run the two texture scripts, then Blender:

```
python3 Tools/ArtGen/legacy/living_room/tex_surfaces_living_room.py
python3 Tools/ArtGen/legacy/living_room/tex_prints_living_room.py
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/ArtGen/legacy/living_room/blender/build_living_room.py
```

The texture scripts write into `Assets/_Game/Art/Textures` under the same names the bedroom uses (`T_Newspaper`, `T_Calendar`, `T_Notes_Atlas` and so on), so they overwrite the bedroom's prints. Rerun `Tools/ArtGen/tex_prints.py` afterwards. The Unity pipeline only builds the bedroom; importing `SM_LivingRoom.fbx` needs the living-room materials, which are no longer in `materials.json`.
