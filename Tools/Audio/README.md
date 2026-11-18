# Audio — voices and music, generated from code

Both run on macOS with Python 3 and `numpy`. The Unity menus under **Tools → Don't Call Me → Audio** run the scripts and import the results. If Unity cannot find a Python with numpy, set the EditorPrefs key `DCM.Python` to its path.

## Voices

Every voiced line comes from the content assets, so the text never lives in two places.

1. **Export**: Unity collects every line a caller speaks (the opening, the answers to questions, pressure lines, the timed beats and the closing lines), in English and in Korean, and writes `Tools/Audio/voice_lines.json` (`[{ "voice", "text" }]`). The text is the line's `spoken` field when it has one (for numbers read digit by digit), else its `text`.
2. **Generate**: `python3 Tools/Audio/tts.py` speaks each line with the caller's ElevenLabs voice (the `ELEVEN` table: the game's voice id, still named after the macOS voice it replaced, to an ElevenLabs voice id; model `eleven_multilingual_v2`) or, for a voice not in that table, with the macOS `say` voice through `afconvert`, then shapes it like a phone line: a 300 Hz to 3.4 kHz band with a presence lift, gentle compression, a little line hiss, trimmed and levelled. Clips land in `Assets/_Game/Audio/Voices/<voice>/<hash>.wav`, listed in `manifest.json`. Lines already generated are kept; clips no line uses any more are removed. `--force` renders everything again.
   - ElevenLabs needs `ELEVENLABS_API_KEY` in the environment, but only when a line has to be rendered. Unity's menu runs Python without it, so after changing an ElevenLabs caller's lines run `tts.py` from a shell (`source ~/.zshrc` first if the key lives there), then **Import Voices**. The clip's name hashes the voice id, model and `ELEVEN_TAKE`, so changing a voice or bumping the take renders those lines again (each line is paid for once; about 20,500 characters for all 376 lines).
   - **Check**: `python3 Tools/Audio/check_voices.py` transcribes the ElevenLabs clips with ElevenLabs Scribe and flags lines whose transcript differs from the script, or whose pace is far from the voice's usual pace (transcripts are cached in `voice_check.json`). A Korean number word on its own (`오 분` against `5분`) can be flagged although it is right.
3. **Import**: Unity sets the clips to mono Vorbis and fills `Assets/_Game/Data/VoiceBank.asset`, which the game searches by voice and text.

**Tools → Don't Call Me → Audio → Run Voice Pipeline** does all three. Run it after changing any line.

| Character | English voice | wpm | Korean voice | wpm |
| --- | --- | --- | --- | --- |
| Team Leader Baek, "Mapo City Gas billing team" (Day 0, the tutorial) | Samantha | 178 | Sandy (Korean (South Korea)) | 232 |
| Manager Jeon, "Nuri Bank's account protection team" (Day 1, the protected account) | Daniel | 186 | Rocko (Korean (South Korea)) | 240 |
| Oh Sangchul, the Seoul Daily's branch manager (Day 1, the subscription fee, both truths) | Grandpa (English (US)) | 172 | Grandpa (Korean (South Korea)) | 215 |
| Bae Sujin, the monitor's buyer (Day 1, the extra zero, both truths) | Tessa | 172 | Flo (Korean (South Korea)) | 232 |
| Choi Hyunwoo, "the landlord's son" (Day 2, the new rent account, both truths) | Reed (English (US)) | 178 | Reed (Korean (South Korea)) | 232 |
| "Minjun" on a roommate's phone (Day 2, Minjun's broken phone, both truths) | Eddy (English (US)) | 186 | Eddy (Korean (South Korea)) | 240 |
| Yoon Seora, "Hangang Express customs desk" (Day 3, the held parcel, both truths) | Shelley (English (UK)) | 176 | Yuna | 185 |
| Moon Jihye, the dentist's coordinator (Day 3, the dentist's deposit, both truths) | Karen | 176 | Shelley (Korean (South Korea)) | 232 |

A scam and its legit twin share the caller's voice, so the voice never gives the truth away.

The Korean voices ship with macOS (`say -v '?' | grep ko_KR` lists them). The `Korean (South Korea)` variants speak slowly at their default rate, so they run faster. Korean lines write numbers the way they are read aloud in their `spoken` field (`일일공, 구공공…`), as the English ones do.

A character's voice is set on the conversation's caller (`caller.voice`, from `VoiceBilling`, `VoiceJeon`, `VoiceBranch`, `VoiceBuyer`, `VoiceHyunwoo`, `VoiceBrother`, `VoiceCustoms` and `VoiceDental` in `ContentBuilder`). Speeds are in `RATES` in `tts.py`: a voice is looked up by its full name, then by its first word (so `Grandpa (English (US))` uses `Grandpa`). Moira, Rishi and the Korean Grandma are still free for a new character.

The eight cases have 376 voiced lines. Per language: 19 (the gas bill), 23 (the protected account), 24 (the subscription fee), 23 (the extra zero), 27 (the rent), 25 (Minjun), 26 (the parcel) and 21 (the dentist).

## Music

`python3 Tools/Audio/music.py` (or **Generate Music**) writes four loops to `Assets/_Game/Audio/Music`:

| Loop | Where | What |
| --- | --- | --- |
| `home_lofi` | Title screen | Warm FM keys, bass, brushed drums with swing, a kalimba motif, vinyl crackle, 84 BPM |
| `investigate_calm` | Investigation | Minor pads, soft clock ticks, a low pulse, 72 BPM |
| `investigate_tense` | Investigation, layered | Same length: a driving bass ostinato, heartbeat kick, fast ticks, dissonant swells; faded in as the deadline nears |
| `morning` | Next morning | Bright keys and plucks, no drums |

Every loop wraps its release tail back to the start so it loops cleanly, and is levelled to a target loudness so the layers sit together. **Import Music** sets them to Vorbis, decoded when they load, with an override for the desktop that streams them from disk: the browser build uses the default settings (the importer takes no WebGL override), and a streamed clip there can neither loop without a gap nor start two layers together. `MusicPlayer` plays a main loop and an optional tension layer in sync; the Music setting and the pause menu (ducked) control its volume.
