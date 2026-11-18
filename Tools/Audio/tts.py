"""Voice lines for the callers, spoken by ElevenLabs voices (or macOS `say` voices) and shaped like a phone line.

Input : Tools/Audio/voice_lines.json, written by Unity (Tools → Don't Call Me → Audio → Export Voice Lines):
        [{"voice": "Daniel", "text": "Hello, is this Kim Jiwoo? ..."}, ...]
Output: Assets/_Game/Audio/Voices/<voice>/<hash>.wav  (22.05 kHz mono)
        Assets/_Game/Audio/Voices/manifest.json         [{"voice", "text", "file"}]  → Unity's VoiceBank

A caller whose voice is in ELEVEN is spoken by that ElevenLabs voice (needs ELEVENLABS_API_KEY in the
environment when a line has to be rendered; clips already made need no key). Any other voice goes through `say`.
Each line then goes through a telephone band-pass (300 Hz to 3.4 kHz with a presence lift),
light compression and a little line hiss, and is trimmed and levelled. Lines already generated with
the same voice and text are kept, so re-running only renders what changed.

Run: python3 Tools/Audio/tts.py [--force]
"""
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import wave

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LINES = os.path.join(os.path.dirname(__file__), "voice_lines.json")
OUT = os.path.join(ROOT, "Assets", "_Game", "Audio", "Voices")
SR = 22050

# Words per minute per voice: the scammer talks a little fast, officials a little slower.
RATES = {"Daniel": 186, "Samantha": 178, "Karen": 176, "Moira": 170, "Tessa": 172, "Rishi": 176, "Fred": 180, "Reed": 178, "Shelley": 176,
         "Eddy": 186, "Grandpa": 172,
         # Korean voices (the game's Korean dialogue): the Korean variants speak slowly at the default rate.
         "Yuna": 185, "Rocko (Korean (South Korea))": 240, "Reed (Korean (South Korea))": 232, "Sandy (Korean (South Korea))": 232,
         "Shelley (Korean (South Korea))": 232, "Flo (Korean (South Korea))": 232, "Grandma (Korean (South Korea))": 215,
         "Grandpa (Korean (South Korea))": 215, "Eddy (Korean (South Korea))": 240}


# The game's voice ids (the conversation's caller.voice, named after the macOS voices they replaced) spoken by
# ElevenLabs voices. One English and one native Korean voice per caller.
ELEVEN = {
    "Samantha": "XrExE9yKIg1WjnnlVkGX",                          # Matilda: Team Leader Baek, gas billing (Day 0)
    "Sandy (Korean (South Korea))": "iWLjl1zCuqXRkW6494ve",      # Jisoo
    # Chosen, not rendered yet (about 18,600 characters): uncomment when the account's limit allows.
    # "Daniel": "cjVigY5qzO86Huf0OWal",                          # Eric: Manager Jeon, the protected account
    # "Rocko (Korean (South Korea))": "s07IwTCOrCDCaETjUVjx",    # Hyunbin
    # "Grandpa (English (US))": "pqHfZKP75CvOlQylNhV4",          # Bill: Oh Sangchul, the subscription fee
    # "Grandpa (Korean (South Korea))": "AgA5UKT9OrVg7jxVBZd4",  # Sung-ho
    # "Tessa": "cgSgspJ2msm6clMCkdW9",                           # Jessica: Bae Sujin, the extra zero
    # "Flo (Korean (South Korea))": "5I7B1di44aCL15NkP0jn",      # Kanna
    # "Reed (English (US))": "iP95p4xoKVk53GoZ742B",             # Chris: Choi Hyunwoo, the rent
    # "Reed (Korean (South Korea))": "CxErO97xpQgQXYmapDKX",     # Theo
    # "Eddy (English (US))": "TX3LPaxmHKxFdv7VOQHJ",             # Liam: Minjun
    # "Eddy (Korean (South Korea))": "m3gJBS8OofDJfycyA2Ip",     # Taehyung
    # "Shelley (English (UK))": "Xb7hH8MSUJpSbSDYk0k2",          # Alice: Yoon Seora, the parcel
    # "Yuna": "mYk0rAapHek2oTw18z8x",                            # Salang
    # "Karen": "EXAVITQu4vr4xnSDxMaL",                           # Sarah: Moon Jihye, the dentist
    # "Shelley (Korean (South Korea))": "QPFsEL6IBxlT15xfiD6C",  # Hana Lee
}
ELEVEN_MODEL = "eleven_multilingual_v2"
ELEVEN_SETTINGS = {"stability": 0.4, "similarity_boost": 0.8, "style": 0.3}
ELEVEN_TAKE = 1   # bump to render every ElevenLabs line again (new settings)


def slug(voice):
    return "".join(c for c in voice if c.isalnum()) or "voice"


def file_for(voice, text):
    key = f"{voice}|{text}"
    if voice in ELEVEN:
        key += f"|{ELEVEN[voice]}|{ELEVEN_MODEL}|{ELEVEN_TAKE}"
    h = hashlib.sha1(key.encode("utf-8")).hexdigest()[:12]
    return os.path.join(OUT, slug(voice), h + ".wav")


def say(voice, text, path_aiff):
    rate = RATES.get(voice) or RATES.get(voice.split(" ")[0], 180)
    subprocess.run(["say", "-v", voice, "-r", str(rate), "-o", path_aiff, text], check=True)


def eleven(voice, text):
    """The line as ElevenLabs speaks it, mono float at SR."""
    key = os.environ.get("ELEVENLABS_API_KEY")
    if not key:
        sys.exit("tts.py: ELEVENLABS_API_KEY is not set, and lines for ElevenLabs voices need rendering. "
                 "Run it from a shell that has the key (Unity's menu does not).")
    lang = "ko" if any("\uac00" <= c <= "\ud7a3" for c in text) else "en"
    body = json.dumps({"text": text, "model_id": ELEVEN_MODEL, "language_code": lang,
                       "voice_settings": ELEVEN_SETTINGS}).encode()
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{ELEVEN[voice]}?output_format=pcm_{SR}"
    for attempt in range(4):
        req = urllib.request.Request(url, data=body, headers={"xi-api-key": key, "Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                return np.frombuffer(r.read(), "<i2").astype(np.float64) / 32768
        except urllib.error.HTTPError as e:
            detail = e.read()[:300].decode("utf-8", "replace")
            if e.code in (429, 500, 502, 503) and attempt < 3:
                time.sleep(2 + 4 * attempt)
                continue
            sys.exit(f"tts.py: ElevenLabs {e.code} for {voice!r}: {detail}")


def to_wav(path_aiff, path_wav):
    subprocess.run(["afconvert", "-f", "WAVE", "-d", f"LEI16@{SR}", "-c", "1", path_aiff, path_wav], check=True)


def read(path):
    with wave.open(path) as w:
        x = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(np.float64) / 32768
        if w.getnchannels() > 1:
            x = x.reshape(-1, w.getnchannels()).mean(1)
        return x


def write(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def phone_line(x, seed):
    """Telephone band, a presence lift, gentle compression, a touch of line hiss."""
    n = len(x)
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(n, 1 / SR)
    band = np.clip((f - 250) / 120, 0, 1) * np.clip((3500 - f) / 300, 0, 1)
    presence = 1 + 0.5 * np.exp(-((f - 1900) / 700) ** 2)
    y = np.fft.irfft(spec * band * presence, n)
    y /= np.max(np.abs(y)) + 1e-9
    y = np.tanh(y * 2.2) / np.tanh(2.2)            # compression and a little grit
    hiss = np.random.default_rng(seed).standard_normal(n) * 0.004
    return y + hiss


def trim(x, threshold=0.012, keep=0.06):
    loud = np.where(np.abs(x) > threshold)[0]
    if len(loud) == 0:
        return x
    a = max(0, loud[0] - int(keep * SR))
    b = min(len(x), loud[-1] + int(keep * SR))
    x = x[a:b].copy()
    fade = int(0.01 * SR)
    x[:fade] *= np.linspace(0, 1, fade)
    x[-fade:] *= np.linspace(1, 0, fade)
    return x


def main():
    force = "--force" in sys.argv
    with open(LINES, encoding="utf-8") as f:
        lines = json.load(f)
    manifest = []
    made = kept = 0
    with tempfile.TemporaryDirectory() as tmp:
        for i, item in enumerate(lines):
            voice, text = item["voice"], item["text"].strip()
            path = file_for(voice, text)
            if force or not os.path.exists(path):
                if voice in ELEVEN:
                    spoken = eleven(voice, text)
                else:
                    aiff, raw = os.path.join(tmp, "l.aiff"), os.path.join(tmp, "l.wav")
                    say(voice, text, aiff)
                    to_wav(aiff, raw)
                    spoken = read(raw)
                x = trim(phone_line(trim(spoken), seed=i)) * 0.85
                write(path, x)
                made += 1
            else:
                kept += 1
            manifest.append({"voice": voice, "text": text, "file": os.path.relpath(path, ROOT).replace(os.sep, "/")})
    with open(os.path.join(OUT, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=1, ensure_ascii=False)
    # Clips no line uses any more (the text or voice changed) are generated files: remove them.
    used = {os.path.join(ROOT, m["file"]) for m in manifest}
    removed = 0
    for voice_dir in os.listdir(OUT):
        folder = os.path.join(OUT, voice_dir)
        if not os.path.isdir(folder):
            continue
        for name in os.listdir(folder):
            path = os.path.join(folder, name)
            if name.endswith(".wav") and path not in used:
                os.remove(path)
                if os.path.exists(path + ".meta"):
                    os.remove(path + ".meta")
                removed += 1
    print(f"{made} generated, {kept} kept, {removed} removed, {len(manifest)} in {os.path.relpath(OUT, ROOT)}/manifest.json")


if __name__ == "__main__":
    main()
