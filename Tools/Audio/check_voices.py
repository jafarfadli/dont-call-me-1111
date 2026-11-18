"""Checks the ElevenLabs voice clips by listening to them with speech-to-text (ElevenLabs Scribe).

Each clip of a voice in tts.ELEVEN is transcribed and compared with its script. A line is flagged when the
transcript differs from the script (words added, dropped or misread) or when it is unusually slow or fast for its
length. Numbers are compared loosely: Scribe writes digits, the scripts often spell them out.
Transcripts are cached in Tools/Audio/voice_check.json by clip, so a re-run only pays for new clips.

Run: python3 Tools/Audio/check_voices.py [--all]      (--all also lists the lines that passed)
"""
import difflib
import json
import os
import re
import sys
import urllib.request
import uuid
import wave

import tts

CACHE = os.path.join(os.path.dirname(__file__), "voice_check.json")
KO_DIGITS = "공영일이삼사오육칠팔구십백천만"


def norm(text):
    """Letters only, lower case; numbers (digits or Korean number words in a run) become '#'."""
    t = text.lower()
    t = re.sub(r"\d[\d,.]*", "#", t)
    t = re.sub(f"[{KO_DIGITS}]{{2,}}", "#", t)
    t = re.sub(r"\b(zero|oh|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|"
               r"sixteen|seventeen|eighteen|nineteen|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|hundred|"
               r"thousand|million|and)\b", "#", t)
    t = re.sub(r"[^\w#]", "", t)
    return re.sub(r"#+", "#", t)


def transcribe(path, lang):
    key = os.environ.get("ELEVENLABS_API_KEY") or sys.exit("check_voices.py: ELEVENLABS_API_KEY is not set")
    b = "----" + uuid.uuid4().hex
    head = "".join(f'--{b}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'
                   for k, v in (("model_id", "scribe_v1"), ("language_code", "kor" if lang == "ko" else "eng")))
    with open(path, "rb") as f:
        audio = f.read()
    data = (head + f'--{b}\r\nContent-Disposition: form-data; name="file"; filename="a.wav"\r\n'
            "Content-Type: audio/wav\r\n\r\n").encode() + audio + f"\r\n--{b}--\r\n".encode()
    req = urllib.request.Request("https://api.elevenlabs.io/v1/speech-to-text", data=data,
                                 headers={"xi-api-key": key, "Content-Type": f"multipart/form-data; boundary={b}"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.load(r)["text"]


def seconds(path):
    with wave.open(path) as w:
        return w.getnframes() / w.getframerate()


def main():
    with open(os.path.join(tts.OUT, "manifest.json"), encoding="utf-8") as f:
        manifest = [m for m in json.load(f) if m["voice"] in tts.ELEVEN]
    cache = json.load(open(CACHE, encoding="utf-8")) if os.path.exists(CACHE) else {}
    rows = []
    for m in manifest:
        path = os.path.join(tts.ROOT, m["file"])
        lang = "ko" if any("가" <= c <= "힣" for c in m["text"]) else "en"
        if m["file"] not in cache:
            cache[m["file"]] = transcribe(path, lang)
            with open(CACHE, "w", encoding="utf-8") as f:
                json.dump(cache, f, indent=1, ensure_ascii=False)
        heard = cache[m["file"]]
        ratio = difflib.SequenceMatcher(None, norm(m["text"]), norm(heard)).ratio()
        rows.append((m, lang, heard, ratio, seconds(path) / max(1, len(m["text"]))))
    # Pace per character, against the median of the same voice.
    pace = {}
    for m, lang, *_ , spc in rows:
        pace.setdefault(m["voice"], []).append(spc)
    median = {v: sorted(xs)[len(xs) // 2] for v, xs in pace.items()}
    flagged = 0
    for m, lang, heard, ratio, spc in rows:
        slow = spc / median[m["voice"]]
        bad = ratio < 0.9 or not 0.7 < slow < 1.45
        flagged += bad
        if bad or "--all" in sys.argv:
            print(f"{'FLAG' if bad else 'ok  '} {ratio:.2f} pace x{slow:.2f}  {m['voice']}  {m['file']}\n"
                  f"     script: {m['text']}\n     heard : {heard}")
    print(f"{len(rows)} clips checked, {flagged} flagged")


if __name__ == "__main__":
    main()
