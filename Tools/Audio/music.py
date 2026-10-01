"""Music loops for the game, synthesised from code (numpy only).

  home_lofi.wav          title screen: warm lo-fi keys, bass, brushed drums, vinyl crackle
  investigate_calm.wav   the investigation bed: minor pads, soft clock ticks, low pulse
  investigate_tense.wav  the pressure layer, same length as the calm bed so they play in sync:
                         driving bass ostinato, heartbeat kick, fast ticks, dissonant swells
  morning.wav            the next morning (End scene): bright, drum-less keys

Every loop is rendered with its release tail wrapped back to the start, so it loops without a click.

Run: python3 Tools/Audio/music.py
"""
import math
import os
import wave

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Game", "Audio", "Music")
SR = 44100
rng = np.random.default_rng(11)

NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7, "G#": 8,
        "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def hz(name):
    """'A4' → 440."""
    pitch, octave = name[:-1], int(name[-1])
    midi = 12 * (octave + 1) + NOTE[pitch]
    return 440.0 * 2 ** ((midi - 69) / 12)


def env(n, attack, decay, sustain, release, hold):
    """ADSR envelope, lengths in seconds; hold = time before release."""
    t = np.arange(n) / SR
    e = np.zeros(n)
    a = max(attack, 1e-4)
    e = np.where(t < a, t / a, e)
    d_mask = (t >= a) & (t < a + decay)
    e = np.where(d_mask, 1 - (1 - sustain) * (t - a) / max(decay, 1e-4), e)
    e = np.where((t >= a + decay) & (t < hold), sustain, e)
    r_mask = t >= hold
    level = sustain if hold >= a + decay else 1.0
    e = np.where(r_mask, level * np.exp(-(t - hold) / max(release, 1e-4)), e)
    return e


class Mix:
    def __init__(self, seconds, tail=4.0):
        self.loop = int(seconds * SR)
        self.buf = np.zeros((self.loop + int(tail * SR), 2))

    def add(self, start, sig, pan=0.0, gain=1.0):
        i = int(start * SR)
        if i >= len(self.buf):
            return
        sig = sig[: len(self.buf) - i] * gain
        left = math.cos((pan + 1) * math.pi / 4)
        right = math.sin((pan + 1) * math.pi / 4)
        self.buf[i:i + len(sig), 0] += sig * left
        self.buf[i:i + len(sig), 1] += sig * right

    def looped(self):
        out = self.buf[: self.loop].copy()
        tail = self.buf[self.loop:]
        n = min(len(tail), self.loop)
        out[:n] += tail[:n]
        return out


# ---------------------------------------------------------------- instruments

def keys(freq, dur, vel=0.5, bright=1.0):
    """Electric piano: two-operator FM with a fast decaying index, slight detune and tremolo."""
    n = int((dur + 2.5) * SR)
    t = np.arange(n) / SR
    idx = bright * (1.6 * np.exp(-t * 3.2) + 0.25)
    out = np.zeros(n)
    for det, g in ((1.0, 0.6), (1.0035, 0.4)):
        f = freq * det
        mod = np.sin(2 * np.pi * f * t) * idx
        out += g * np.sin(2 * np.pi * f * t + mod)
    out += 0.12 * np.sin(2 * np.pi * freq * 4.0 * t) * np.exp(-t * 9)   # tine click
    out *= env(n, 0.004, 1.2, 0.35, 0.6, dur) * (1 + 0.08 * np.sin(2 * np.pi * 4.8 * t))
    return out * vel


def pluck(freq, vel=0.4):
    """Kalimba / music box: sine with a quick decay and a glassy 3rd harmonic."""
    n = int(1.8 * SR)
    t = np.arange(n) / SR
    s = np.sin(2 * np.pi * freq * t) + 0.35 * np.sin(2 * np.pi * freq * 3.01 * t) * np.exp(-t * 7)
    return s * np.exp(-t * 3.2) * np.minimum(1, t / 0.002) * vel


def bass(freq, dur, vel=0.6):
    n = int((dur + 0.5) * SR)
    t = np.arange(n) / SR
    s = np.sin(2 * np.pi * freq * t) + 0.25 * np.sin(4 * np.pi * freq * t)
    s = np.tanh(1.6 * s) / np.tanh(1.6)
    return s * env(n, 0.01, 0.25, 0.7, 0.12, dur) * vel


def pad(freqs, dur, vel=0.3, dark=0.5):
    """Detuned saw-ish pad through a gentle low-pass, slow attack."""
    n = int((dur + 3.0) * SR)
    t = np.arange(n) / SR
    s = np.zeros(n)
    for f in freqs:
        for det in (-0.004, 0.0, 0.005):
            ph = 2 * np.pi * f * (1 + det) * t
            s += sum(np.sin(k * ph) / k for k in range(1, 6)) * 0.3
    s = lowpass(s, 900 + 2200 * (1 - dark))
    return s * env(n, 1.4, 0.8, 0.8, 1.6, dur) * vel / max(1, len(freqs))


def kick(vel=0.9, low=48):
    n = int(0.45 * SR)
    t = np.arange(n) / SR
    f = low + 90 * np.exp(-t * 32)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return (np.sin(ph) * np.exp(-t * 9) + 0.25 * rng.standard_normal(n) * np.exp(-t * 200)) * vel


def snare(vel=0.5):
    n = int(0.3 * SR)
    t = np.arange(n) / SR
    noise = highpass(rng.standard_normal(n), 900)
    body = np.sin(2 * np.pi * 185 * t) * np.exp(-t * 28)
    return (lowpass(noise, 5000) * np.exp(-t * 16) * 0.7 + body * 0.5) * vel


def hat(vel=0.25, length=0.05):
    n = int(length * 2 * SR)
    t = np.arange(n) / SR
    return highpass(rng.standard_normal(n), 6500) * np.exp(-t / length * 5) * vel


def tick(vel=0.35, freq=2100):
    """Clock tick / woodblock."""
    n = int(0.08 * SR)
    t = np.arange(n) / SR
    return (np.sin(2 * np.pi * freq * t) * np.exp(-t * 90) + 0.3 * rng.standard_normal(n) * np.exp(-t * 400)) * vel


def heartbeat(vel=0.8):
    return kick(vel, low=38) + np.pad(kick(vel * 0.7, low=36), (int(0.2 * SR), 0))[: int(0.45 * SR)]


def swell(freqs, dur, vel=0.25):
    """String-ish cluster that swells up and cuts off."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    s = np.zeros(n)
    for f in freqs:
        vib = 1 + 0.004 * np.sin(2 * np.pi * 5.5 * t + f)
        ph = 2 * np.pi * np.cumsum(f * vib) / SR
        s += sum(np.sin(k * ph) / (k * k) for k in range(1, 5))
    shape = (t / dur) ** 2.2 * np.minimum(1, (dur - t) / 0.08)
    return lowpass(s, 2600) * shape * vel / len(freqs)


def vinyl(seconds, level=0.02):
    n = int(seconds * SR)
    hiss = lowpass(rng.standard_normal(n), 3000) * level * 0.35
    clicks = np.zeros(n)
    for i in rng.integers(0, n, size=int(seconds * 7)):
        clicks[i] = rng.uniform(0.2, 1.0) * rng.choice([-1, 1])
    clicks = lowpass(clicks, 4000) * level * 6
    return hiss + clicks


# ---------------------------------------------------------------- filters

def lowpass(x, cutoff):
    """Two passes of a one-pole low-pass (12 dB/oct)."""
    a = math.exp(-2 * math.pi * cutoff / SR)
    for _ in range(2):
        x = _onepole(x, a, 1 - a)
    return x


def _onepole(x, a, b):
    from itertools import accumulate
    return np.fromiter(accumulate(x * b, lambda acc, v: a * acc + v), dtype=np.float64, count=len(x))


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


# ---------------------------------------------------------------- tracks

def chord(names):
    return [hz(n) for n in names]


def home_lofi():
    bpm, bars = 84, 8
    beat = 60 / bpm
    mix = Mix(bars * 4 * beat)
    prog = [("F3", "A3", "C4", "E4", "G4"), ("E3", "G3", "B3", "D4"), ("D3", "F3", "A3", "C4", "E4"), ("C3", "E3", "G3", "B3", "D4"),
            ("F3", "A3", "C4", "E4"), ("E3", "G#3", "B3", "D4"), ("A2", "C3", "E3", "G3", "B3"), ("D3", "F3", "A3", "C4")]
    roots = ["F2", "E2", "D2", "C2", "F2", "E2", "A1", "D2"]
    melody = {0: ["C5", None, "A4", None, "G4", None, None, "E4"], 2: ["D5", None, "C5", "A4", None, None, "G4", None],
              4: ["E5", None, "D5", None, "C5", None, "A4", None], 6: ["C5", "B4", None, "G4", None, "E4", None, None]}
    swing = 0.58
    for bar in range(bars):
        t0 = bar * 4 * beat
        for i, f in enumerate(chord(prog[bar])):
            mix.add(t0 + 0.012 * i, keys(f, 3.6 * beat, vel=0.2), pan=-0.3 + 0.15 * i)
        mix.add(t0 + 2.5 * beat, keys(chord(prog[bar])[-1] * 2, 0.8 * beat, vel=0.08, bright=0.6), pan=0.4)
        r = hz(roots[bar])
        mix.add(t0, bass(r, 1.6 * beat, 0.55))
        mix.add(t0 + 2.5 * beat, bass(r, 0.9 * beat, 0.42))
        mix.add(t0 + 3.5 * beat, bass(r * 1.5, 0.45 * beat, 0.3))
        for b in range(4):
            mix.add(t0 + b * beat, kick(0.75 if b in (0, 2) else 0.0))
            if b in (1, 3):
                mix.add(t0 + b * beat, snare(0.32), pan=0.1)
            for h in range(2):
                off = (swing if h else 0) * beat
                mix.add(t0 + b * beat + off, hat(0.12 if h else 0.18), pan=0.35)
        if bar in melody:
            for k, note in enumerate(melody[bar]):
                if note:
                    mix.add(t0 + k * beat * 0.5 + (0.03 if k % 2 else 0), pluck(hz(note), 0.2), pan=0.2)
    out = mix.looped()
    out += vinyl(len(out) / SR, 0.02)[:, None]
    return master(out, warmth=5200, loudness=0.17)


def investigate(tense):
    bpm, bars = 72, 8
    beat = 60 / bpm
    mix = Mix(bars * 4 * beat, tail=5.0)
    prog = [("A2", "E3", "G3", "B3", "C4"), ("F2", "C3", "E3", "A3", "B3"), ("D2", "A2", "C3", "F3", "E4"), ("E2", "B2", "D3", "G#3", "A3")] * 2
    for bar in range(bars):
        t0 = bar * 4 * beat
        if not tense:
            mix.add(t0, pad(chord(prog[bar]), 4 * beat, 0.32, dark=0.6), pan=0.0)
            mix.add(t0, bass(hz(prog[bar][0]) / 2, 3.8 * beat, 0.35))
            for b in range(4):
                mix.add(t0 + b * beat, tick(0.16, 2300 if b % 2 == 0 else 1900), pan=0.5 if b % 2 else -0.5)
            if bar % 2 == 1:
                mix.add(t0 + 3 * beat, pluck(hz("E5") if bar % 4 == 1 else hz("F5"), 0.08), pan=0.3)
        else:
            root = hz(prog[bar][0]) / 2
            for e in range(8):
                accent = 1.0 if e % 2 == 0 else 0.7
                mix.add(t0 + e * beat / 2, bass(root * (2 if e in (3, 7) else 1), 0.4 * beat, 0.42 * accent))
            for b in range(4):
                mix.add(t0 + b * beat, heartbeat(0.55 if b in (0, 2) else 0.0))
            for s in range(16):
                mix.add(t0 + s * beat / 4, tick(0.13 if s % 4 else 0.22, 2600), pan=0.4 if s % 2 else -0.4)
            top = chord(("B4", "C5", "F5")) if bar % 2 == 0 else chord(("G#4", "A4", "D#5"))
            mix.add(t0, swell(top, 4 * beat, 0.3), pan=0.0)
    return master(mix.looped(), warmth=6000 if tense else 3800, loudness=0.16 if tense else 0.12)


def morning():
    bpm, bars = 76, 8
    beat = 60 / bpm
    mix = Mix(bars * 4 * beat)
    prog = [("C3", "G3", "B3", "E4"), ("A2", "E3", "G3", "C4"), ("F2", "C3", "E3", "A3"), ("G2", "D3", "F3", "B3"),
            ("C3", "G3", "B3", "E4", "D4"), ("E2", "B2", "D3", "G3"), ("F2", "C3", "E3", "A3", "D4"), ("G2", "D3", "F3", "A3", "B3")]
    tune = ["E5", "G5", "D5", "C5", "E5", "B4", "C5", "D5"]
    for bar in range(bars):
        t0 = bar * 4 * beat
        for i, f in enumerate(chord(prog[bar])):
            mix.add(t0 + 0.02 * i, keys(f, 3.8 * beat, vel=0.18, bright=0.8), pan=-0.25 + 0.15 * i)
        mix.add(t0, bass(hz(prog[bar][0]) / 2 * 2, 3.5 * beat, 0.35))
        mix.add(t0 + 1.5 * beat, pluck(hz(tune[bar]), 0.16), pan=0.25)
        mix.add(t0 + 3 * beat, pluck(hz(tune[(bar + 3) % 8]) / 2 * 2, 0.1), pan=-0.2)
    out = mix.looped()
    out += vinyl(len(out) / SR, 0.01)[:, None]
    return master(out, warmth=5600, loudness=0.15)


def master(x, warmth, loudness):
    """Warm low-pass, then level to a target RMS so the layers sit together, then soft-limit."""
    x = np.stack([lowpass(x[:, 0], warmth), lowpass(x[:, 1], warmth)], axis=1)
    x *= loudness / (np.sqrt(np.mean(x ** 2)) + 1e-9)
    return np.tanh(x * 1.3) / 1.3


def write(name, data):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"{path}  {len(data) / SR:.1f} s")


if __name__ == "__main__":
    write("home_lofi", home_lofi())
    write("investigate_calm", investigate(False))
    write("investigate_tense", investigate(True))
    write("morning", morning())
