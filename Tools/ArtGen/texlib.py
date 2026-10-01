"""Drawing helpers for the comic-style texture generator.

Everything is drawn on a supersampled canvas and downsampled, so ink lines stay
smooth. Noise helpers are periodic, so tileable textures have no seams.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Textures")
# System fonts, then the game's own OFL fonts (Gaegu for Korean handwriting).
FONT_DIRS = ["/System/Library/Fonts/Supplemental", "/System/Library/Fonts", os.path.join(ROOT, "Assets", "_Game", "UI", "Fonts")]

INK = (40, 32, 48)


def hexc(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(len(a)))


def shade(c, k):
    return tuple(max(0, min(255, int(round(v * k)))) for v in c)


def font_path(name):
    for base in FONT_DIRS:
        p = os.path.join(base, name)
        if os.path.exists(p):
            return p
    raise FileNotFoundError(name)


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    img.save(path, optimize=True)
    return path


# ---------------------------------------------------------------- noise

def periodic_noise(w, h, cycles, seed):
    """Smooth tileable noise in 0..1. `cycles` ~ number of blobs across the width."""
    rng = np.random.default_rng(seed)
    n = rng.standard_normal((h, w))
    f = np.fft.fft2(n)
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w * (h / w)
    r = np.sqrt(fx ** 2 + fy ** 2)
    filt = np.exp(-(r / max(cycles, 1e-3)) ** 2)
    out = np.real(np.fft.ifft2(f * filt))
    out -= out.min()
    out /= max(out.max(), 1e-9)
    return out


def fbm(w, h, seed, octaves=((4, 0.5), (9, 0.25), (20, 0.15), (45, 0.1))):
    acc = np.zeros((h, w))
    total = 0.0
    for i, (cycles, amp) in enumerate(octaves):
        acc += periodic_noise(w, h, cycles, seed + i * 17) * amp
        total += amp
    return acc / total


def to_img(arr, mode="L"):
    a = np.clip(arr * 255.0, 0, 255).astype(np.uint8)
    return Image.fromarray(a, mode)


def multiply(img, arr):
    """Multiply an RGB(A) image by a 0..1 array."""
    a = np.asarray(img).astype(np.float32)
    k = arr[..., None] if a.ndim == 3 else arr
    if a.ndim == 3 and a.shape[2] == 4:
        a[..., :3] *= k
    else:
        a *= k
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), img.mode)


# ---------------------------------------------------------------- canvas

class Canvas:
    """Supersampled drawing surface. Coordinates are in output pixels."""

    def __init__(self, w, h, ss=3, mode="RGB", bg=(255, 255, 255)):
        self.w, self.h, self.ss, self.mode = w, h, ss, mode
        self.img = Image.new(mode, (w * ss, h * ss), bg)
        self.d = ImageDraw.Draw(self.img)
        self._fonts = {}

    def P(self, pts):
        s = self.ss
        return [(x * s, y * s) for x, y in pts]

    def font(self, name, size, index=0):
        key = (name, size, index)
        if key not in self._fonts:
            self._fonts[key] = ImageFont.truetype(font_path(name), int(size * self.ss), index=index)
        return self._fonts[key]

    def line(self, pts, fill, width=1.0, caps=True):
        if len(pts) < 2:
            return
        P = self.P(pts)
        w = max(1, int(round(width * self.ss)))
        self.d.line(P, fill=fill, width=w, joint="curve")
        if caps:
            r = w / 2.0
            for x, y in (P[0], P[-1]):
                self.d.ellipse([x - r, y - r, x + r, y + r], fill=fill)

    def poly(self, pts, fill=None, outline=None, width=1.0):
        if fill is not None:
            self.d.polygon(self.P(pts), fill=fill)
        if outline is not None:
            self.line(list(pts) + [pts[0]], outline, width)

    def rect(self, x0, y0, x1, y1, fill=None, outline=None, width=1.0):
        self.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], fill, outline, width)

    def ellipse(self, cx, cy, rx, ry, fill=None, outline=None, width=1.0, rot=0.0, n=72):
        pts = ellipse_pts(cx, cy, rx, ry, rot, n)
        self.poly(pts, fill, outline, width)

    def dot(self, x, y, r, fill):
        s = self.ss
        self.d.ellipse([(x - r) * s, (y - r) * s, (x + r) * s, (y + r) * s], fill=fill)

    def text(self, xy, s, font, fill, anchor="la", spacing=0):
        x, y = xy
        self.d.text((x * self.ss, y * self.ss), s, font=font, fill=fill, anchor=anchor, spacing=spacing * self.ss)

    def text_width(self, s, font):
        return self.d.textlength(s, font=font) / self.ss

    def result(self):
        return self.img.resize((self.w, self.h), Image.LANCZOS)


def ellipse_pts(cx, cy, rx, ry, rot=0.0, n=72):
    c, s = math.cos(rot), math.sin(rot)
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        x, y = rx * math.cos(a), ry * math.sin(a)
        pts.append((cx + x * c - y * s, cy + x * s + y * c))
    return pts


def resample(pts, step):
    """Resample a polyline at roughly even spacing."""
    out = [pts[0]]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        seg = math.hypot(x1 - x0, y1 - y0)
        n = max(1, int(seg / step))
        for i in range(1, n + 1):
            t = i / n
            out.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t))
    return out


def wobble(pts, amp, rng, step=4.0, freq=0.05):
    """Hand-drawn jitter: offset a polyline sideways by smooth random noise."""
    pts = resample(pts, step)
    if len(pts) < 3 or amp <= 0:
        return pts
    phases = rng.uniform(0, 2 * math.pi, 3)
    freqs = [freq, freq * 2.3, freq * 5.1]
    amps = [1.0, 0.45, 0.2]
    out = []
    dist = 0.0
    for i, (x, y) in enumerate(pts):
        if i > 0:
            dist += math.hypot(x - pts[i - 1][0], y - pts[i - 1][1])
        j0 = max(0, i - 1)
        j1 = min(len(pts) - 1, i + 1)
        tx, ty = pts[j1][0] - pts[j0][0], pts[j1][1] - pts[j0][1]
        tl = math.hypot(tx, ty) or 1.0
        nx, ny = -ty / tl, tx / tl
        o = sum(a * math.sin(dist * f + p) for a, f, p in zip(amps, freqs, phases)) * amp / 1.65
        out.append((x + nx * o, y + ny * o))
    return out


def ink_stroke(cv, pts, color, width, rng, amp=0.8, taper=True):
    """A pen stroke: wobbly path, width tapering at both ends."""
    pts = wobble(pts, amp, rng, step=3.0)
    n = len(pts)
    if n < 2:
        return
    for i in range(n - 1):
        t = i / max(n - 2, 1)
        k = math.sin(math.pi * min(max(t, 0.0), 1.0)) ** 0.5 if taper else 1.0
        w = max(0.4, width * (0.35 + 0.65 * k))
        cv.line([pts[i], pts[i + 1]], color, w, caps=True)


def tiled(w, h, fn):
    """Call fn(ox, oy) for the 9 wrap offsets so shapes crossing an edge repeat."""
    for ox in (-w, 0, w):
        for oy in (-h, 0, h):
            fn(ox, oy)


def halftone(img, cell=6, angle=15, dark=(40, 36, 44), light=(236, 228, 208)):
    """Newspaper halftone: grayscale image to dots."""
    g = np.asarray(img.convert("L")).astype(np.float32) / 255.0
    h, w = g.shape
    ss = 3
    out = Image.new("RGB", (w * ss, h * ss), light)
    d = ImageDraw.Draw(out)
    a = math.radians(angle)
    ca, sa = math.cos(a), math.sin(a)
    diag = int(math.hypot(w, h)) + cell
    for i in range(-diag, diag, cell):
        for j in range(-diag, diag, cell):
            x = i * ca - j * sa
            y = i * sa + j * ca
            if 0 <= x < w and 0 <= y < h:
                v = 1.0 - g[int(y), int(x)]
                r = math.sqrt(v) * cell * 0.62
                if r > 0.3:
                    d.ellipse([(x - r) * ss, (y - r) * ss, (x + r) * ss, (y + r) * ss], fill=dark)
    return out.resize((w, h), Image.LANCZOS)
