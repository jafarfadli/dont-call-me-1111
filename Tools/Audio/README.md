# Audio — voices and music, generated from code

Both run on macOS with Python 3 and `numpy`. The Unity menus under **Tools → Don't Call Me → Audio** run the scripts and import the results. If Unity cannot find a Python with numpy, set the EditorPrefs key `DCM.Python` to its path.

## Voices

Every voiced line comes from the content assets, so the text never lives in two places.

1. **Export**: Unity collects every line a caller speaks (the opening, the answers to questions, pressure lines, the timed beats and the closing lines), in English and in Korean, and writes `Tools/Audio/voice_lines.json` (`[{ "voice", "text" }]`). The text is the line's `spoken` field when it has one (for numbers read digit by digit), else its `text`.
2. **Generate**: `python3 Tools/Audio/tts.py` speaks each line with the macOS `say` voice, converts it with `afconvert`, then shapes it like a phone line: a 300 Hz to 3.4 kHz band with a presence lift, gentle compression, a little line hiss, trimmed and levelled. Clips land in `Assets/_Game/Audio/Voices/<voice>/<hash>.wav`, listed in `manifest.json`. Lines already generated are kept; clips no line uses any more are removed. `--force` renders everything again.
3. **Import**: Unity sets the clips to mono Vorbis and fills `Assets/_Game/Data/VoiceBank.asset`, which the game searches by voice and text.

**Tools → Don't Call Me → Audio → Run Voice Pipeline** does all three. Run it after changing any line.

| Character | English voice | wpm | Korean voice | wpm |
| --- | --- | --- | --- | --- |
| Team Leader Baek, "Mapo City Gas billing team" (Day 0, the tutorial) | Samantha | 178 | Sandy (Korean (South Korea)) | 232 |
| Manager Jeon, "Nuri Bank's account protection team" (Day 1) | Daniel | 186 | Rocko (Korean (South Korea)) | 240 |
| Choi Hyunwoo, "the landlord's son" (Day 2, both truths) | Reed (English (US)) | 178 | Reed (Korean (South Korea)) | 232 |
| Yoon Seora, "Hangang Express customs desk" (Day 3, both truths) | Shelley (English (UK)) | 176 | Yuna | 185 |

A scam and its legit twin share the caller's voice, so the voice never gives the truth away.

The Korean voices ship with macOS (`say -v '?' | grep ko_KR` lists them). The `Korean (South Korea)` variants speak slowly at their default rate, so they run faster. Korean lines write numbers the way they are read aloud in their `spoken` field (`일일공, 구공공…`), as the English ones do.

A character's voice is set on the conversation's caller (`caller.voice`, from `VoiceBilling`, `VoiceJeon`, `VoiceHyunwoo` and `VoiceCustoms` in `ContentBuilder`). Speeds are in `RATES` in `tts.py`, which also keeps the speeds of the voices earlier versions used for call-backs (Karen, Tessa, Moira, Rishi and the Korean Shelley, Flo, Grandma, Grandpa), ready for a new character.

The four days have 190 voiced lines: 19, 23, 27 and 26 per language.

## Music

`python3 Tools/Audio/music.py` (or **Generate Music**) writes four loops to `Assets/_Game/Audio/Music`:

| Loop | Where | What |
| --- | --- | --- |
| `home_lofi` | Title screen | Warm FM keys, bass, brushed drums with swing, a kalimba motif, vinyl crackle, 84 BPM |
| `investigate_calm` | Investigation | Minor pads, soft clock ticks, a low pulse, 72 BPM |
| `investigate_tense` | Investigation, layered | Same length: a driving bass ostinato, heartbeat kick, fast ticks, dissonant swells; faded in as the deadline nears |
| `morning` | Next morning | Bright keys and plucks, no drums |

Every loop wraps its release tail back to the start so it loops cleanly, and is levelled to a target loudness so the layers sit together. `MusicPlayer` plays a main loop and an optional tension layer in sync; the Music setting and the pause menu (ducked) control its volume.
