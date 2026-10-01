"""Voice lines for the callers, generated with the macOS `say` voices and shaped like a phone line.

Input : Tools/Audio/voice_lines.json, written by Unity (Tools → Don't Call Me → Audio → Export Voice Lines):
        [{"voice": "Daniel", "text": "Hello, is this Kim Jiwoo? ..."}, ...]
Output: Assets/_Game/Audio/Voices/<voice>/<hash>.wav  (22.05 kHz mono)
        Assets/_Game/Audio/Voices/manifest.json         [{"voice", "text", "file"}]  → Unity's VoiceBank

Each line goes through `say`, then a telephone band-pass (300 Hz to 3.4 kHz with a presence lift),
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
import wave

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LINES = os.path.join(os.path.dirname(__file__), "voice_lines.json")
OUT = os.path.join(ROOT, "Assets", "_Game", "Audio", "Voices")
SR = 22050

# Words per minute per voice: the scammer talks a little fast, officials a little slower.
RATES = {"Daniel": 186, "Samantha": 178, "Karen": 176, "Moira": 170, "Tessa": 172, "Rishi": 176, "Fred": 180, "Reed": 178, "Shelley": 176,
         # Korean voices (the game's Korean dialogue): the Korean variants speak slowly at the default rate.
         "Yuna": 185, "Rocko (Korean (South Korea))": 240, "Reed (Korean (South Korea))": 232, "Sandy (Korean (South Korea))": 232,
         "Shelley (Korean (South Korea))": 232, "Flo (Korean (South Korea))": 232, "Grandma (Korean (South Korea))": 215,
         "Grandpa (Korean (South Korea))": 215}


def slug(voice):
    return "".join(c for c in voice if c.isalnum()) or "voice"


def file_for(voice, text):
    h = hashlib.sha1(f"{voice}|{text}".encode("utf-8")).hexdigest()[:12]
    return os.path.join(OUT, slug(voice), h + ".wav")


def say(voice, text, path_aiff):
    rate = RATES.get(voice) or RATES.get(voice.split(" ")[0], 180)
    subprocess.run(["say", "-v", voice, "-r", str(rate), "-o", path_aiff, text], check=True)


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
                aiff, raw = os.path.join(tmp, "l.aiff"), os.path.join(tmp, "l.wav")
                say(voice, text, aiff)
                to_wav(aiff, raw)
                x = trim(phone_line(trim(read(raw)), seed=i)) * 0.85
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
