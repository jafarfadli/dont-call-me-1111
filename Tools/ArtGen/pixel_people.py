"""Pixel-art people in the style of references/art/ref4.png: realistic proportions,
muted colours, blocky 3-4 tone shading, no outlines.

Each picture is painted at 8x resolution with simple height-field lighting
(key light from the upper left), then box-downsampled to its pixel grid and
reduced to a small palette, so shading lands in chunky pixel steps."""
import math

import numpy as np
from PIL import Image, ImageDraw

LIGHT = np.array([-0.55, -0.5, 0.67], np.float32)
LIGHT /= np.linalg.norm(LIGHT)


def rgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def box_blur(a, r):
    """Separable box blur with edge clamping; three passes approximate a gaussian."""
    r = max(1, int(r))
    out = a.astype(np.float32)
    for _ in range(3):
        for axis in (0, 1):
            pad = [(0, 0), (0, 0)]
            pad[axis] = (r + 1, r)
            p = np.pad(out, pad, mode="edge")
            c = np.cumsum(p, axis=axis)
            if axis == 0:
                out = (c[2 * r + 1:, :] - c[:-2 * r - 1, :]) / (2 * r + 1)
            else:
                out = (c[:, 2 * r + 1:] - c[:, :-2 * r - 1]) / (2 * r + 1)
    return out


def normals(z):
    gy, gx = np.gradient(z)
    n = np.dstack([-gx, -gy, np.ones_like(z)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n


def lambert(z, ambient=0.34):
    n = normals(z)
    ndl = np.clip(n @ LIGHT, 0.0, 1.0)
    return ambient + (1.0 - ambient) * ndl


def ramp(s, dark, base, light):
    """Map shading 0..1 to a three-colour ramp."""
    s = np.clip(s, 0.0, 1.0)[..., None]
    lo = dark + (base - dark) * np.clip(s / 0.55, 0, 1)
    hi = base + (light - base) * np.clip((s - 0.55) / 0.45, 0, 1)
    return np.where(s < 0.55, lo, hi)


def tones(base, warm=True):
    """Shadow / base / light for a colour, shadows shifted cool-red like the reference."""
    base = np.asarray(base, np.float32)
    dark = base * np.array([0.62, 0.52, 0.52] if warm else [0.55, 0.56, 0.66], np.float32)
    light = np.minimum(base * np.array([1.12, 1.1, 1.02], np.float32) + 8, 255)
    return dark, base, light


class Painter:
    def __init__(self, w, h, bg):
        self.w, self.h = w, h
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.rgb[:] = bg
        yy, xx = np.mgrid[0:h, 0:w]
        self.xx = xx.astype(np.float32)
        self.yy = yy.astype(np.float32)

    def paint(self, mask, color):
        m = np.clip(mask, 0.0, 1.0)[..., None]
        self.rgb = self.rgb * (1.0 - m) + np.asarray(color, np.float32) * m

    def ellipse(self, cx, cy, rx, ry, soft=1.2):
        d = np.sqrt(((self.xx - cx) / rx) ** 2 + ((self.yy - cy) / ry) ** 2)
        return np.clip((1.0 - d) * min(rx, ry) / soft, 0.0, 1.0)

    def polygon(self, pts):
        img = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(img).polygon([(float(x), float(y)) for x, y in pts], fill=255)
        return np.asarray(img).astype(np.float32) / 255.0

    def line(self, pts, width):
        img = Image.new("L", (self.w, self.h), 0)
        d = ImageDraw.Draw(img)
        d.line([(float(x), float(y)) for x, y in pts], fill=255, width=max(1, int(round(width))), joint="curve")
        return np.asarray(img).astype(np.float32) / 255.0

    def shaded(self, mask, base, blur, extra=None, warm=True, ambient=0.34):
        """Paint a shape lit as a soft inflated volume."""
        z = np.sqrt(np.clip(box_blur(mask, blur), 0, 1)) * blur * 1.6
        if extra is not None:
            z = z + extra
        s = lambert(z, ambient)
        dark, mid, light = tones(base, warm)
        self.paint(mask, ramp(s, dark, mid, light))
        return s

    def pixelate(self, gw, gh, colors=56, noise=0.012, seed=0):
        img = Image.fromarray(np.clip(self.rgb, 0, 255).astype(np.uint8))
        small = np.asarray(img.resize((gw, gh), Image.BOX)).astype(np.float32)
        rng = np.random.default_rng(seed)
        small *= 1.0 + rng.normal(0.0, noise, (gh, gw, 1))
        small = Image.fromarray(np.clip(small, 0, 255).astype(np.uint8))
        q = small.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, kmeans=3, dither=Image.Dither.NONE)
        return q.convert("RGB")


def bezier(p0, p1, p2, n=16):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in np.linspace(0, 1, n)]


# ---------------------------------------------------------------- person

def person(p, hx, hy, R, spec, seed=0):
    """Draw one bust. (hx, hy) is the face centre, R the half height of the face."""
    rng = np.random.default_rng(seed)
    female = spec.get("sex", "m") == "f"
    skin = rgb(spec.get("skin", "C98E66"))
    hair = rgb(spec.get("hair", "2A211C"))
    rx = R * (0.7 if female else 0.76)
    ys = hy + R * 1.32
    bottom = p.h + 10

    # torso and clothes
    sw = R * (1.3 if female else 1.5)
    left = [(hx - R * 0.42, ys - R * 0.12)] + bezier((hx - R * 0.42, ys - R * 0.12), (hx - sw * 0.95, ys - R * 0.08),
                                                     (hx - sw, ys + R * 0.55)) + [(hx - sw * 1.08, bottom)]
    right = [(hx + sw * 1.08, bottom)] + bezier((hx + sw, ys + R * 0.55), (hx + sw * 0.95, ys - R * 0.08),
                                                (hx + R * 0.42, ys - R * 0.12))
    torso = p.polygon(left + right)
    outfit = spec.get("outfit", "shirt")
    cloth = rgb(spec.get("cloth", "8FA3C4"))
    folds = np.zeros_like(torso)
    folds += 3.0 * np.sin((p.xx - hx) / (R * 0.23) + np.sin(p.yy / (R * 0.5)))
    p.shaded(torso, cloth, R * 0.45, extra=folds * torso * 0.4)

    if outfit in ("shirt", "shirt_tie", "school", "koko", "batik"):
        for side in (-1, 1):
            collar = p.polygon([(hx + side * R * 0.08, ys + R * 0.05), (hx + side * R * 0.52, ys - R * 0.18),
                                (hx + side * R * 0.62, ys + R * 0.12), (hx + side * R * 0.22, ys + R * 0.5)])
            ccol = rgb(spec.get("collar", "")) if spec.get("collar") else cloth * 1.06
            p.shaded(collar, ccol, R * 0.12)
        if outfit in ("shirt_tie", "school"):
            tie = p.polygon([(hx - R * 0.1, ys + R * 0.18), (hx + R * 0.1, ys + R * 0.18), (hx + R * 0.16, bottom),
                             (hx - R * 0.16, bottom)])
            p.shaded(tie, rgb(spec.get("tie", "2E3346")), R * 0.08)
            knot = p.polygon([(hx - R * 0.12, ys + R * 0.02), (hx + R * 0.12, ys + R * 0.02), (hx + R * 0.08, ys + R * 0.24),
                              (hx - R * 0.08, ys + R * 0.24)])
            p.shaded(knot, rgb(spec.get("tie", "2E3346")) * 1.1, R * 0.06)
        if outfit == "batik":
            motif = ((np.sin(p.xx / (R * 0.09)) * np.sin(p.yy / (R * 0.09))) > 0.55).astype(np.float32) * torso
            motif *= (p.yy > ys + R * 0.3)
            p.paint(motif * 0.85, rgb("D9B77A"))
        if outfit == "koko":
            trim = p.polygon([(hx - R * 0.06, ys + R * 0.1), (hx + R * 0.06, ys + R * 0.1), (hx + R * 0.06, bottom),
                              (hx - R * 0.06, bottom)])
            p.paint(trim * 0.8, rgb("C9A861"))
    elif outfit == "toga":
        facing = rgb(spec.get("facing", "8C2F2A"))
        for side in (-1, 1):
            band = p.polygon([(hx + side * R * 0.2, ys - R * 0.05), (hx + side * R * 0.55, ys - R * 0.1),
                              (hx + side * R * 0.75, bottom), (hx + side * R * 0.38, bottom)])
            p.shaded(band, facing, R * 0.1)
        v = p.polygon([(hx - R * 0.3, ys - R * 0.05), (hx + R * 0.3, ys - R * 0.05), (hx, ys + R * 0.55)])
        p.shaded(v, rgb("E9E1D2"), R * 0.08)
    elif outfit == "kebaya":
        lace = ((np.sin(p.xx / (R * 0.05)) + np.sin(p.yy / (R * 0.05))) > 1.2).astype(np.float32) * torso
        p.paint(lace * 0.35, cloth * 0.82)
        v = p.polygon([(hx - R * 0.34, ys - R * 0.1), (hx + R * 0.34, ys - R * 0.1), (hx, ys + R * 0.95)])
        p.shaded(v * torso, rgb(spec.get("inner", "B7473A")), R * 0.1)
        p.paint(p.ellipse(hx, ys + R * 0.72, R * 0.09, R * 0.09), rgb("D8B25A"))
    elif outfit == "beskap":
        for k in range(4):
            p.paint(p.ellipse(hx + R * 0.35, ys + R * (0.55 + k * 0.42), R * 0.05, R * 0.05), rgb("C9A24E"))
        closed = p.polygon([(hx - R * 0.42, ys - R * 0.12), (hx + R * 0.42, ys - R * 0.12), (hx + R * 0.4, ys + R * 0.2),
                            (hx - R * 0.4, ys + R * 0.2)])
        p.shaded(closed, cloth * 1.05, R * 0.08)

    elif outfit in ("sweater", "cardigan"):
        rib = (np.sin(p.xx / (R * 0.05)) > 0.2).astype(np.float32) * torso * 0.12
        p.paint(rib, cloth * 0.8)
        band = np.clip(p.ellipse(hx, ys - R * 0.02, R * 0.55, R * 0.26) - p.ellipse(hx, ys - R * 0.08, R * 0.38, R * 0.17), 0, 1)
        p.shaded(band * torso, cloth * 0.9, R * 0.06)
        if outfit == "cardigan":
            v = p.polygon([(hx - R * 0.3, ys - R * 0.08), (hx + R * 0.3, ys - R * 0.08), (hx + R * 0.05, bottom), (hx - R * 0.05, bottom)])
            p.shaded(v * torso, rgb(spec.get("inner", "E3D8C4")), R * 0.08)
            for k in range(3):
                p.paint(p.ellipse(hx + R * 0.12, ys + R * (0.75 + k * 0.45), R * 0.045, R * 0.045), rgb("E8DCC2"))
    elif outfit in ("blazer", "uniform"):
        shirt = p.polygon([(hx - R * 0.34, ys - R * 0.1), (hx + R * 0.34, ys - R * 0.1), (hx + R * 0.1, bottom), (hx - R * 0.1, bottom)])
        p.shaded(shirt * torso, rgb("E6E0D4"), R * 0.08)
        for side in (-1, 1):
            lapel = p.polygon([(hx + side * R * 0.36, ys - R * 0.12), (hx + side * R * 0.72, ys + R * 0.15),
                               (hx + side * R * 0.3, ys + R * 1.3), (hx + side * R * 0.12, ys + R * 0.95)])
            p.shaded(lapel * torso, cloth * 1.12, R * 0.08)
        if outfit == "uniform":
            for side in (-1, 1):
                p.shaded(p.ellipse(hx + side * R * 0.13, ys + R * 0.12, R * 0.13, R * 0.08), rgb(spec.get("tie", "A8473A")), R * 0.04)
            p.shaded(p.ellipse(hx, ys + R * 0.12, R * 0.06, R * 0.06), rgb(spec.get("tie", "A8473A")) * 0.85, R * 0.03)
    elif outfit == "hanbok_f":
        skirt = rgb(spec.get("inner", "B8453A"))
        p.shaded((p.yy > ys + R * 0.95).astype(np.float32) * torso, skirt, R * 0.2)
        for side in (-1, 1):
            strip = p.polygon([(hx + side * R * 0.34, ys - R * 0.12), (hx + side * R * 0.46, ys - R * 0.1),
                               (hx - side * R * 0.05, ys + R * 0.72), (hx - side * R * 0.16, ys + R * 0.7)])
            p.paint(strip * torso, rgb("F1ECE2"))
        ribbon = p.polygon([(hx + R * 0.02, ys + R * 0.62), (hx + R * 0.14, ys + R * 0.6), (hx + R * 0.1, bottom), (hx - R * 0.04, bottom)])
        p.shaded(ribbon, rgb("A8322C"), R * 0.05)
        p.shaded(p.ellipse(hx + R * 0.06, ys + R * 0.62, R * 0.12, R * 0.07), rgb("A8322C"), R * 0.04)
    elif outfit == "hanbok_m":
        v = p.polygon([(hx - R * 0.3, ys - R * 0.1), (hx + R * 0.3, ys - R * 0.1), (hx + R * 0.2, bottom), (hx - R * 0.2, bottom)])
        p.shaded(v * torso, rgb("EAE4D6"), R * 0.08)
        for side in (-1, 1):
            vest = p.polygon([(hx + side * R * 0.3, ys - R * 0.08), (hx + side * sw * 0.8, ys + R * 0.2), (hx + side * sw * 0.9, bottom),
                              (hx + side * R * 0.22, bottom)])
            p.shaded(vest * torso, rgb(spec.get("vest", "34466A")), R * 0.1)

    # neck with chin shadow
    neck = p.polygon([(hx - R * 0.3, hy + R * 0.45), (hx + R * 0.3, hy + R * 0.45), (hx + R * 0.34, ys + R * 0.12),
                      (hx - R * 0.34, ys + R * 0.12)])
    if outfit in ("toga",):
        neck *= (p.yy < ys + R * 0.35)
    s = p.shaded(neck, skin * 0.9, R * 0.2)
    shadow = np.clip(1.0 - (p.yy - (hy + R * 0.7)) / (R * 0.35), 0, 1) * neck
    p.paint(shadow * 0.45, tones(skin)[0])

    style = spec.get("style", "short")
    if style in ("long", "bob"):
        length = ys + R * 1.0 if style == "long" else hy + R * 0.62
        back = np.maximum(p.ellipse(hx, hy - R * 0.08, rx * 1.2, R * 1.08),
                          p.polygon([(hx - rx * 1.2, hy - R * 0.1), (hx + rx * 1.2, hy - R * 0.1), (hx + rx * 1.3, length),
                                     (hx - rx * 1.3, length)]))
        p.shaded(back, hair * 0.85, R * 0.3, warm=False)

    # ears
    for side in (-1, 1):
        ear = p.ellipse(hx + side * rx * 0.97, hy + R * 0.06, R * 0.14, R * 0.24)
        p.shaded(ear, skin * 0.95, R * 0.07)

    # face: egg-shaped ellipsoid with nose, brows, cheeks and eye sockets in the height field
    dx, dy = p.xx - hx, p.yy - hy
    v = dy / R
    taper = 1.0 - 0.3 * np.clip(v, 0, 1) ** 1.5
    u = dx / (rx * taper)
    d2 = u * u + v * v
    face = np.clip((1.0 - d2) * R * 0.25, 0, 1)
    z = np.sqrt(np.clip(1.0 - d2, 0, 1)) * R * 0.62
    g = lambda x0, y0, sx, sy: np.exp(-(((dx - x0) / sx) ** 2 + ((dy - y0) / sy) ** 2))
    z += R * 0.13 * g(0, R * 0.12, R * 0.07, R * 0.28)
    z += R * 0.12 * g(0, R * 0.34, R * 0.1, R * 0.08)
    z -= R * 0.08 * (g(-R * 0.33, -R * 0.02, R * 0.16, R * 0.1) + g(R * 0.33, -R * 0.02, R * 0.16, R * 0.1))
    z += R * 0.05 * g(0, -R * 0.19, R * 0.45, R * 0.06)
    z += R * 0.04 * (g(-R * 0.42, R * 0.26, R * 0.18, R * 0.16) + g(R * 0.42, R * 0.26, R * 0.18, R * 0.16))
    z += R * 0.05 * g(0, R * 0.82, R * 0.2, R * 0.12)
    z -= R * 0.03 * g(0, R * 0.57, R * 0.22, R * 0.06)
    s = lambert(z, 0.26)
    edge = np.clip((1.0 - d2) * 4.0, 0, 1)
    s *= 0.7 + 0.3 * edge
    dark, mid, light = tones(skin)
    p.paint(face, ramp(s, dark, mid, light))
    nose_shadow = p.polygon([(hx + R * 0.02, hy - R * 0.12), (hx + R * 0.1, hy - R * 0.1), (hx + R * 0.16, hy + R * 0.36),
                             (hx + R * 0.02, hy + R * 0.42)])
    p.paint(nose_shadow * face * 0.55, dark)
    p.paint(p.ellipse(hx - R * 0.05, hy + R * 0.3, R * 0.05, R * 0.06) * 0.5, light)
    cheek = np.clip((dx / rx - 0.45) * 2.2, 0, 1) * face
    p.paint(cheek * 0.55, dark)
    under = p.polygon([(hx - R * 0.14, hy + R * 0.66), (hx + R * 0.14, hy + R * 0.66), (hx + R * 0.1, hy + R * 0.76),
                       (hx - R * 0.1, hy + R * 0.76)])
    p.paint(under * 0.35, dark)
    if spec.get("age") == "old":
        for side in (-1, 1):
            fold = p.line(bezier((hx + side * R * 0.14, hy + R * 0.36), (hx + side * R * 0.3, hy + R * 0.48),
                                 (hx + side * R * 0.26, hy + R * 0.64)), R * 0.035)
            p.paint(fold * face * 0.5, dark)
            bag = p.ellipse(hx + side * R * 0.33, hy + R * 0.1, R * 0.12, R * 0.035)
            p.paint(bag * 0.35, dark)
    if spec.get("stubble"):
        jaw = np.clip((v - 0.38) * 3, 0, 1) * face * (1 - g(0, R * 0.57, R * 0.2, R * 0.06))
        speck = (rng.random(face.shape) < 0.35).astype(np.float32)
        p.paint(jaw * (0.25 + 0.2 * speck), rgb("5E5A62"))
    if spec.get("mustache"):
        m = p.polygon([(hx - R * 0.26, hy + R * 0.52), (hx, hy + R * 0.44), (hx + R * 0.26, hy + R * 0.52), (hx + R * 0.2, hy + R * 0.56),
                       (hx, hy + R * 0.5), (hx - R * 0.2, hy + R * 0.56)])
        p.paint(m * 0.9, hair)

    # features
    eye_y = hy - R * 0.02
    tired = spec.get("expression") == "tired"
    ink = rgb("2A1C18")
    for side in (-1, 1):
        ex = hx + side * R * 0.31
        socket = p.ellipse(ex, eye_y - R * 0.01, R * 0.19, R * 0.11)
        p.paint(socket * face * 0.35, dark)
        white = p.ellipse(ex + side * R * 0.03, eye_y + R * 0.01, R * 0.13, R * 0.05)
        p.paint(white * 0.85, rgb("D2C4B2"))
        iris = p.ellipse(ex, eye_y + R * 0.01, R * 0.065, R * 0.06)
        p.paint(iris * np.clip(white * 1.5, 0, 1), ink)
        lid = p.line(bezier((ex - R * 0.15, eye_y + R * 0.02), (ex, eye_y - R * (0.08 if not tired else 0.055)),
                            (ex + R * 0.16, eye_y + R * 0.02)), R * (0.07 if tired else 0.055))
        p.paint(lid, ink)
        if tired:
            p.paint(p.line(bezier((ex - R * 0.11, eye_y + R * 0.1), (ex, eye_y + R * 0.13), (ex + R * 0.12, eye_y + R * 0.09)),
                           R * 0.035) * 0.6, dark)
        brow = p.line(bezier((hx + side * R * 0.12, hy - R * (0.2 if not tired else 0.18)),
                             (hx + side * R * 0.3, hy - R * (0.29 if not tired else 0.24)),
                             (hx + side * R * 0.52, hy - R * 0.2)), R * 0.085)
        p.paint(brow, hair * 0.75)
        nostril = p.ellipse(hx + side * R * 0.07, hy + R * 0.4, R * 0.04, R * 0.025)
        p.paint(nostril * 0.9, dark * 0.7)
    smile = spec.get("expression") == "smile"
    p.paint(p.line(bezier((hx - R * 0.12, hy + R * 0.5), (hx, hy + R * 0.48), (hx + R * 0.12, hy + R * 0.5)), R * 0.05) * 0.4, dark)
    mouth = p.line(bezier((hx - R * 0.18, hy + R * (0.57 if smile else 0.59)), (hx, hy + R * (0.64 if smile else 0.58)),
                          (hx + R * 0.18, hy + R * (0.57 if smile else 0.6))), R * 0.06)
    p.paint(mouth, dark * 0.62)
    lip = p.ellipse(hx - R * 0.02, hy + R * 0.66, R * 0.1, R * 0.03)
    p.paint(lip * 0.45, light)
    if spec.get("lipstick"):
        p.paint(p.ellipse(hx, hy + R * 0.6, R * 0.13, R * 0.05) * 0.6, rgb("A8453E"))
    if spec.get("glasses"):
        for side in (-1, 1):
            ex = hx + side * R * 0.32
            ring = p.ellipse(ex, eye_y, R * 0.17, R * 0.12) - p.ellipse(ex, eye_y, R * 0.14, R * 0.095)
            p.paint(np.clip(ring, 0, 1), rgb("3A2E2A"))
        p.paint(p.line([(hx - R * 0.16, eye_y), (hx + R * 0.16, eye_y)], R * 0.03), rgb("3A2E2A"))

    # hair and head wear
    if style == "perm":
        curls = np.zeros_like(p.xx)
        for k in range(22):
            a = math.pi * (1.08 + 0.84 * k / 21)
            for ring, scale in ((1.0, 1.0), (0.7, 0.82)):
                cx = hx + math.cos(a) * rx * 1.05 * ring
                cy = hy - R * 0.12 + math.sin(a) * R * 0.98 * ring
                curls = np.maximum(curls, p.ellipse(cx, cy, R * 0.2 * scale, R * 0.19 * scale))
        for side in (-1, 1):
            for k in range(3):
                curls = np.maximum(curls, p.ellipse(hx + side * rx * 1.02, hy - R * 0.05 + k * R * 0.18, R * 0.16, R * 0.15))
        curls *= (p.yy < hy + R * 0.42)
        bump = sum(p.ellipse(hx + math.cos(a) * rx * 0.6, hy - R * 0.5 + math.sin(a) * R * 0.3, R * 0.15, R * 0.15)
                   for a in np.linspace(0, 6.28, 9)) * R * 0.05
        p.shaded(curls, hair, R * 0.12, extra=bump, warm=False)
    if style in ("long", "bob"):
        cap = p.ellipse(hx, hy - R * 0.12, rx * 1.12, R * 1.0)
        bangs = hy - R * 0.28 + R * 0.04 * np.sin(dx / (R * 0.06) + seed) - np.clip(np.abs(dx) - rx * 0.78, 0, None) * 1.2
        cap = cap * (p.yy < bangs)
        streaks = 2.0 * np.sin(p.xx / (R * 0.045) + np.sin(p.yy / (R * 0.25)))
        p.shaded(cap, hair, R * 0.22, extra=streaks * cap, warm=False)
        side_len = hy + (R * 1.15 if style == "long" else R * 0.6)
        for side in (-1, 1):
            lock = p.polygon([(hx + side * rx * 0.8, hy - R * 0.4), (hx + side * rx * 1.18, hy - R * 0.3),
                              (hx + side * rx * 1.2, side_len), (hx + side * rx * 0.86, side_len - R * 0.1)])
            p.shaded(lock, hair, R * 0.1, warm=False)
    if style in ("short", "mortar", "peci", "bun", "blangkon"):
        ang = np.arctan2(dy, dx)
        tuft = 1.0 + 0.05 * np.sin(ang * 9 + seed) + 0.03 * np.sin(ang * 17 + seed * 2)
        rr = np.sqrt((dx / (rx * 1.12)) ** 2 + ((dy + R * 0.1) / (R * 1.04)) ** 2)
        top = np.clip((tuft - rr) * R * 0.2, 0, 1)
        fringe = 0.06 * R * np.sin(dx / (R * 0.07) + seed) * np.clip(1 - np.abs(dx) / rx, 0, 1)
        hairline = hy - R * 0.38 + fringe + np.abs(dx) * 0.18 - np.clip(np.abs(dx) - rx * 0.72, 0, None) * 1.7
        if spec.get("part", True):
            hairline = hairline + R * 0.1 * np.clip((dx + rx * 0.2) / rx, 0, 1)
        cap = top * (p.yy < hairline)
        if style == "bun":
            cap = np.maximum(cap, p.ellipse(hx, hy - R * 0.62, rx * 1.12, R * 0.62) * (p.yy < hy - R * 0.28))
        streaks = 2.2 * np.sin(p.xx / (R * 0.05) + np.sin(p.yy / (R * 0.2)) * 2.0)
        p.shaded(cap, hair, R * 0.25, extra=streaks * cap, warm=False)
        for side in (-1, 1):
            burn = p.polygon([(hx + side * rx * 0.86, hy - R * 0.3), (hx + side * rx * 1.0, hy - R * 0.3),
                              (hx + side * rx * 0.98, hy + R * 0.08), (hx + side * rx * 0.9, hy + R * 0.08)])
            p.paint(burn * 0.9, hair)
    if style == "bun":
        bun = p.ellipse(hx + rx * 0.2, hy - R * 1.02, R * 0.36, R * 0.26)
        p.shaded(bun, hair, R * 0.12, warm=False)
        for k in range(7):
            p.paint(p.ellipse(hx + rx * 0.62 + k * R * 0.02, hy - R * 0.88 + k * R * 0.13, R * 0.045, R * 0.045), rgb("EDE6D6"))
    if style == "peci":
        peci = p.polygon([(hx - rx * 1.02, hy - R * 0.42), (hx - rx * 0.96, hy - R * 1.08), (hx + rx * 0.96, hy - R * 1.08),
                          (hx + rx * 1.02, hy - R * 0.42)])
        p.shaded(peci, rgb("2A2628"), R * 0.18, warm=False)
        p.paint(p.line([(hx - rx * 0.9, hy - R * 0.98), (hx + rx * 0.5, hy - R * 1.02)], R * 0.04) * 0.4, rgb("5A5256"))
    if style == "blangkon":
        cap = p.polygon([(hx - rx * 1.12, hy - R * 0.1), (hx - rx * 1.14, hy - R * 0.6)] +
                        bezier((hx - rx * 1.1, hy - R * 0.75), (hx, hy - R * 1.35), (hx + rx * 1.1, hy - R * 0.75)) +
                        [(hx + rx * 1.14, hy - R * 0.6), (hx + rx * 1.12, hy - R * 0.1), (hx + rx * 0.9, hy - R * 0.32),
                         (hx, hy - R * 0.42), (hx - rx * 0.9, hy - R * 0.32)])
        p.shaded(cap, rgb("5A3A26"), R * 0.2)
        dots = ((np.sin(p.xx / (R * 0.06)) * np.sin(p.yy / (R * 0.06))) > 0.6).astype(np.float32) * cap
        p.paint(dots * 0.7, rgb("C9A36A"))
    if style == "mortar":
        board = p.polygon([(hx - rx * 1.55, hy - R * 0.95), (hx, hy - R * 1.28), (hx + rx * 1.55, hy - R * 0.95), (hx, hy - R * 0.7)])
        p.shaded(board, rgb("26242A"), R * 0.08, warm=False)
        skull = p.polygon([(hx - rx * 1.0, hy - R * 0.45), (hx - rx * 0.95, hy - R * 0.92), (hx + rx * 0.95, hy - R * 0.92),
                           (hx + rx * 1.0, hy - R * 0.45)])
        p.shaded(skull, rgb("2C2A30"), R * 0.15, warm=False)
        p.paint(p.line([(hx, hy - R * 1.0), (hx + rx * 1.2, hy - R * 0.95), (hx + rx * 1.28, hy - R * 0.3)], R * 0.05), rgb("D2A84A"))
    if spec.get("mortar") and style != "hijab":
        board = p.polygon([(hx - rx * 1.6, hy - R * 1.02), (hx, hy - R * 1.36), (hx + rx * 1.6, hy - R * 1.02), (hx, hy - R * 0.76)])
        p.shaded(board, rgb("26242A"), R * 0.08, warm=False)
        skull = p.polygon([(hx - rx * 0.98, hy - R * 0.5), (hx - rx * 0.92, hy - R * 0.95), (hx + rx * 0.92, hy - R * 0.95),
                           (hx + rx * 0.98, hy - R * 0.5)])
        p.shaded(skull, rgb("2C2A30"), R * 0.15, warm=False)
        p.paint(p.line([(hx, hy - R * 1.08), (hx + rx * 1.5, hy - R * 1.04), (hx + rx * 1.62, hy - R * 0.45)], R * 0.07), rgb("D8AE4E"))
    if style == "hijab":
        col = rgb(spec.get("hijab", "8C4A3E"))
        drape = spec.get("drape", 0.75)
        outer = np.maximum(p.ellipse(hx, hy - R * 0.02, rx * 1.36, R * 1.24),
                           p.polygon([(hx - rx * 1.3, hy + R * 0.2), (hx + rx * 1.3, hy + R * 0.2), (hx + sw * 0.9, ys + R * drape),
                                      (hx, ys + R * (drape + 0.25)), (hx - sw * 0.9, ys + R * drape)]))
        opening = p.ellipse(hx, hy + R * 0.1, rx * 0.93, R * 0.9, soft=2.0)
        mask = np.clip(outer - opening, 0, 1)
        folds = 2.5 * np.sin((p.xx - hx) / (R * 0.18) + (p.yy - hy) / (R * 0.9))
        p.shaded(mask, col, R * 0.35, extra=folds * mask * np.clip((p.yy - hy) / R, 0, 1))
        p.paint(p.ellipse(hx + R * 0.05, hy + R * 1.02, R * 0.06, R * 0.06), rgb("D8B25A"))
        if spec.get("mortar"):
            board = p.polygon([(hx - rx * 1.6, hy - R * 1.05), (hx, hy - R * 1.4), (hx + rx * 1.6, hy - R * 1.05), (hx, hy - R * 0.78)])
            p.shaded(board, rgb("26242A"), R * 0.08, warm=False)
            p.paint(p.line([(hx, hy - R * 1.1), (hx + rx * 1.5, hy - R * 1.06), (hx + rx * 1.62, hy - R * 0.45)], R * 0.07),
                    rgb("D8AE4E"))


# ---------------------------------------------------------------- pictures

def backdrop_studio(p, top, bottom, spot=0.18):
    t = (p.yy / p.h)[..., None]
    p.rgb[:] = rgb(top) * (1 - t) + rgb(bottom) * t
    r = np.sqrt(((p.xx - p.w * 0.45) / p.w) ** 2 + ((p.yy - p.h * 0.4) / p.h) ** 2)
    p.rgb *= (1.0 + spot * np.clip(1 - r * 2.2, 0, 1))[..., None]
    mottle = box_blur(np.random.default_rng(3).random((p.h, p.w)).astype(np.float32), p.w // 40)
    p.rgb *= (0.94 + 0.12 * (mottle - mottle.mean()) / (mottle.std() + 1e-6) * 0.25 + 0.06)[..., None]


def grade(img, tint=None, sepia=False, vignette=0.25, contrast=1.0):
    a = np.asarray(img).astype(np.float32)
    if sepia:
        g = a.mean(axis=2, keepdims=True)
        a = g * np.array([1.12, 0.98, 0.78], np.float32)
    if contrast != 1.0:
        mean = a.mean(axis=(0, 1), keepdims=True)
        a = (a - mean) * contrast + mean
    if tint is not None:
        a *= np.asarray(tint, np.float32)
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    r = np.sqrt(((xx - w / 2) / w) ** 2 + ((yy - h / 2) / h) ** 2)
    a *= (1.0 - vignette * np.clip(r * 1.6 - 0.25, 0, 1) ** 1.5)[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def render(draw, grid, size, colors=56, seed=0, **grade_kw):
    """Paint at 8x the pixel grid, pixelate, grade, then scale up with hard pixels."""
    gw, gh = grid
    p = Painter(gw * 8, gh * 8, (0, 0, 0))
    draw(p)
    small = p.pixelate(gw, gh, colors=colors, seed=seed)
    small = grade(small, **grade_kw)
    return small.resize(size, Image.NEAREST)


RATNA = dict(sex="f", skin="C68E68", style="hijab", hijab="7E3B32", outfit="kebaya", cloth="C9B28E", inner="7E3B32",
             age="old", expression="smile", glasses=True)
HARJO = dict(sex="m", skin="B97F5B", hair="2B2420", style="peci", outfit="batik", cloth="6B4A34", mustache=True, age="old",
             expression="smile")
SARI = dict(sex="f", skin="CF9A72", style="hijab", hijab="4B6488", outfit="kebaya", cloth="9FB1C7", inner="4B6488",
            expression="smile", lipstick=True)
SARI_WISUDA = dict(SARI, hijab="E4D9C4", outfit="toga", cloth="25232A", facing="8C2F2A", mortar=True, drape=0.45)
DIMAS = dict(sex="m", skin="C48A62", hair="231B17", style="short", outfit="school", cloth="E7E1D6", tie="4A3830",
             expression="tired")


def graduation(size=(512, 512)):
    def draw(p):
        backdrop_studio(p, "5B7396", "2F3E58")
        person(p, p.w * 0.5, p.h * 0.4, p.h * 0.15, SARI_WISUDA, seed=1)
        scroll = p.polygon([(p.w * 0.62, p.h * 0.98), (p.w * 0.82, p.h * 0.74), (p.w * 0.86, p.h * 0.77), (p.w * 0.66, p.h * 1.0)])
        p.shaded(scroll, rgb("E6DCC6"), p.h * 0.02)
        p.paint(p.line([(p.w * 0.72, p.h * 0.88), (p.w * 0.76, p.h * 0.84)], p.h * 0.02), rgb("9C3B2E"))
    return render(draw, (64, 64), size, seed=11, tint=(1.03, 1.0, 0.96))


def wedding(size=(512, 512)):
    groom = dict(sex="m", skin="B98260", hair="231B17", style="blangkon", outfit="beskap", cloth="2B2A33", expression="neutral")
    bride = dict(sex="f", skin="CF9C76", hair="1E1714", style="bun", outfit="kebaya", cloth="E3D6BC", inner="9C3B2E",
                 expression="smile", lipstick=True)

    def draw(p):
        backdrop_studio(p, "A08A6A", "5E4A36", spot=0.25)
        person(p, p.w * 0.33, p.h * 0.36, p.h * 0.13, groom, seed=2)
        person(p, p.w * 0.66, p.h * 0.4, p.h * 0.125, bride, seed=3)
    return render(draw, (64, 64), size, colors=24, seed=12, sepia=True, vignette=0.45, contrast=1.35)


def family(size=(512, 512), grid=(64, 64)):
    def draw(p):
        t = (p.yy / p.h)[..., None]
        p.rgb[:] = rgb("B9C6CE") * (1 - t) + rgb("E2D2B4") * t
        wall = (p.yy > p.h * 0.18).astype(np.float32)
        p.paint(wall, rgb("E3D3B6"))
        p.paint(((p.xx > p.w * 0.08) & (p.xx < p.w * 0.3) & (p.yy > p.h * 0.24) & (p.yy < p.h * 0.5)).astype(np.float32),
                rgb("5C6E86"))
        p.paint((p.yy > p.h * 0.62).astype(np.float32), rgb("A8503E"))
        p.paint(p.ellipse(p.w * 0.9, p.h * 0.6, p.w * 0.1, p.h * 0.14), rgb("6E7A46"))
        dimas = dict(DIMAS, outfit="koko", cloth="E4DCCB", expression="smile")
        people = [(0.2, 0.42, 0.095, dict(HARJO, outfit="batik")), (0.43, 0.47, 0.09, RATNA),
                  (0.64, 0.44, 0.09, SARI), (0.85, 0.42, 0.092, dimas)]
        for i, (x, y, r, spec) in enumerate(people):
            person(p, p.w * x, p.h * y, p.h * r, spec, seed=10 + i)
    return render(draw, grid, size, seed=13, tint=(1.04, 1.0, 0.95))


def pas_foto(size=(512, 512)):
    def draw(p):
        backdrop_studio(p, "B0463A", "8E3328", spot=0.08)
        person(p, p.w * 0.5, p.h * 0.4, p.h * 0.2, DIMAS, seed=4)
    return render(draw, (64, 64), size, seed=14, vignette=0.12)


# ---------------------------------------------------------------- Korean household

JIWOO = dict(sex="f", skin="E5BD9C", hair="2B1E19", style="bob", outfit="sweater", cloth="C79A45", expression="smile")
MOM = dict(sex="f", skin="E0B592", hair="30231D", style="perm", outfit="cardigan", cloth="9C4A3C", inner="E6DCC8",
           expression="smile", age="old")
DAD = dict(sex="m", skin="D6A47E", hair="26201C", style="short", outfit="sweater", cloth="3E5476", glasses=True,
           expression="smile", age="old")
HALMEONI = dict(sex="f", skin="D9AC88", hair="9C9894", style="perm", outfit="cardigan", cloth="7A5A8A", inner="E1D6C4",
                age="old", expression="smile")


def bouquet(p, cx, cy, s):
    wrap = p.polygon([(cx - s * 0.9, cy - s * 0.2), (cx + s * 0.9, cy - s * 0.2), (cx + s * 0.25, cy + s * 1.6), (cx - s * 0.25, cy + s * 1.6)])
    p.shaded(wrap, rgb("E8DCC8"), s * 0.2)
    cols = ["C8514A", "E4B24C", "E59AA5", "F1EBDD", "B8453A"]
    for k in range(9):
        a = k * 2.39
        r = s * 0.55 * math.sqrt(k / 9)
        p.shaded(p.ellipse(cx + math.cos(a) * r, cy - s * 0.3 + math.sin(a) * r * 0.6, s * 0.3, s * 0.27), rgb(cols[k % 5]), s * 0.1)


def graduation_kr(size=(512, 512)):
    def draw(p):
        backdrop_studio(p, "6E7F96", "39465C")
        person(p, p.w * 0.5, p.h * 0.4, p.h * 0.15, dict(JIWOO, outfit="toga", cloth="25232A", facing="3E5476", mortar=True), seed=21)
        bouquet(p, p.w * 0.36, p.h * 0.84, p.h * 0.1)
    return render(draw, (64, 64), size, seed=31, tint=(1.03, 1.0, 0.96))


def wedding_kr(size=(512, 512)):
    groom = dict(DAD, glasses=False, age="adult", outfit="hanbok_m", cloth="E6DFCF", vest="34466A", hair="221A16")
    bride = dict(MOM, age="adult", style="bun", hair="221A16", outfit="hanbok_f", cloth="E9C25A", inner="B8453A", lipstick=True)

    def draw(p):
        backdrop_studio(p, "A48C6C", "6A553E", spot=0.2)
        for k in range(6):
            x0 = p.w * k / 6
            p.paint(((p.xx > x0 + 2) & (p.xx < x0 + p.w / 6 - 2) & (p.yy < p.h * 0.62)).astype(np.float32) * 0.18, rgb("E8DCC0"))
            p.paint(p.ellipse(x0 + p.w / 12, p.h * 0.3, p.w * 0.03, p.w * 0.03) * 0.5, rgb("B8453A"))
        person(p, p.w * 0.34, p.h * 0.38, p.h * 0.13, groom, seed=22)
        person(p, p.w * 0.66, p.h * 0.41, p.h * 0.125, bride, seed=23)
    return render(draw, (64, 64), size, colors=24, seed=32, sepia=True, vignette=0.45, contrast=1.35)


def family_kr(size=(512, 512), grid=(64, 64)):
    def draw(p):
        t = (p.yy / p.h)[..., None]
        p.rgb[:] = rgb("A9BCCB") * (1 - t) + rgb("E6D6B8") * t
        rng = np.random.default_rng(5)
        for x, col in ((0.1, "C8653E"), (0.5, "D9913F"), (0.9, "B8453A")):
            trunk = p.polygon([(p.w * (x - 0.012), p.h * 0.62), (p.w * (x - 0.02), p.h * 0.3), (p.w * (x + 0.02), p.h * 0.3),
                               (p.w * (x + 0.012), p.h * 0.62)])
            p.shaded(trunk, rgb("5A4030"), p.w * 0.01)
            canopy = np.zeros_like(p.xx)
            for _ in range(9):
                cx = p.w * (x + rng.uniform(-0.12, 0.12))
                cy = p.h * (0.2 + rng.uniform(-0.08, 0.08))
                canopy = np.maximum(canopy, p.ellipse(cx, cy, p.w * rng.uniform(0.05, 0.08), p.h * rng.uniform(0.04, 0.07)))
            p.shaded(canopy, rgb(col), p.w * 0.025)
        p.paint((p.yy > p.h * 0.62).astype(np.float32), rgb("8C7A55"))
        people = [(0.18, 0.44, 0.09, DAD), (0.4, 0.47, 0.085, HALMEONI), (0.62, 0.46, 0.087, MOM), (0.84, 0.43, 0.088, JIWOO)]
        for i, (x, y, r, spec) in enumerate(people):
            person(p, p.w * x, p.h * y, p.h * r, spec, seed=40 + i)
    return render(draw, grid, size, seed=33, tint=(1.04, 1.0, 0.95))


def id_photo_kr(size=(512, 512)):
    def draw(p):
        backdrop_studio(p, "BFD0DC", "9DB3C4", spot=0.06)
        person(p, p.w * 0.5, p.h * 0.4, p.h * 0.2, dict(JIWOO, outfit="blazer", cloth="2F3A52", expression="neutral"), seed=24)
    return render(draw, (64, 64), size, seed=34, vignette=0.1)


def photo_strip(size=(256, 512)):
    """Four-cut photo booth strip of Jiwoo and a friend."""
    friend = dict(sex="f", skin="E2B794", hair="4A2E22", style="long", outfit="sweater", cloth="7FA0B8", expression="smile")
    w, h = size
    strip = Image.new("RGB", size, (240, 236, 228))
    frame_w, frame_h = w - 24, (h - 70) // 4
    bgs = [("E7B7B9", "D99AA0"), ("BFD3E0", "9FB8CC"), ("E9D9A8", "D8C080"), ("C9D8B8", "A9BE95")]
    for k in range(4):
        def draw(p, k=k):
            backdrop_studio(p, bgs[k][0], bgs[k][1], spot=0.1)
            tilt = (k - 1.5) * 0.02
            person(p, p.w * (0.3 + tilt), p.h * 0.5, p.h * 0.2, dict(JIWOO, expression="smile" if k != 2 else "neutral"), seed=50 + k)
            person(p, p.w * (0.72 - tilt), p.h * 0.52, p.h * 0.19, friend, seed=60 + k)
        gw = 40
        gh = max(8, int(gw * frame_h / frame_w))
        img = render(draw, (gw, gh), (frame_w, frame_h), colors=40, seed=70 + k, vignette=0.05)
        strip.paste(img, (12, 12 + k * (frame_h + 6)))
    d = ImageDraw.Draw(strip)
    try:
        from PIL import ImageFont
        f = ImageFont.truetype("/System/Library/Fonts/AppleSDGothicNeo.ttc", 16, index=6)
        d.text((w / 2, h - 30), "PHOTO BOOTH  2026.09.12", font=f, fill=(90, 80, 88), anchor="mm")
    except OSError:
        pass
    return strip
