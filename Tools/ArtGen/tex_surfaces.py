"""Surface textures: ink detail maps, the vinyl floor, painted walls, fabrics, cork,
paper grain and brick. All but the wall atlas tile seamlessly."""
import math

import numpy as np
from PIL import Image

import atlas
from texlib import (INK, Canvas, fbm, hexc, mix, periodic_noise, resample, save, tiled, to_img,
                    wobble)


def _stroke_path(cv, pts, color, width, taper=True):
    n = len(pts)
    for i in range(n - 1):
        t = i / max(n - 2, 1)
        k = math.sin(math.pi * min(max(t, 0.0), 1.0)) ** 0.5 if taper else 1.0
        cv.line([pts[i], pts[i + 1]], color, max(0.4, width * (0.35 + 0.65 * k)))


def ink_wood(seed=11, size=1024):
    """Grayscale wood grain in ink: broken grain strokes, knots, pores."""
    rng = np.random.default_rng(seed)
    W = H = size
    cv = Canvas(W, H, ss=2, mode="L", bg=255)
    knots = [(rng.uniform(0, W), rng.uniform(0, H), rng.uniform(14, 24)) for _ in range(2)]

    n_lines = 24
    for i in range(n_lines):
        y0 = (i + rng.uniform(0.2, 0.8)) * H / n_lines
        a1, p1, k1 = rng.uniform(4, 11), rng.uniform(0, 6.28), int(rng.integers(1, 3))
        a2, p2, k2 = rng.uniform(1.5, 4), rng.uniform(0, 6.28), int(rng.integers(3, 6))

        def yfun(x, y0=y0, a1=a1, p1=p1, k1=k1, a2=a2, p2=p2, k2=k2):
            y = y0 + a1 * math.sin(2 * math.pi * k1 * x / W + p1) + a2 * math.sin(2 * math.pi * k2 * x / W + p2)
            for kx, ky, kr in knots:
                dx = (x - kx + W / 2) % W - W / 2
                dy = (y0 - ky + H / 2) % H - H / 2
                infl = math.exp(-(dx / (kr * 3.4)) ** 2) * math.exp(-(dy / (kr * 2.4)) ** 2)
                y += (1 if dy >= 0 else -1) * infl * kr * 1.7
            return y

        x = rng.uniform(0, 60)
        while x < W:
            length = rng.uniform(90, 380)
            xs = np.arange(x, min(x + length, W + 30), 5.0)
            if len(xs) >= 3:
                pts = wobble([(float(v), yfun(float(v))) for v in xs], 0.5, rng, step=5.0)
                g = int(rng.uniform(70, 150))
                wd = rng.uniform(0.9, 2.1)
                tiled(W, H, lambda ox, oy, pts=pts, g=g, wd=wd: _stroke_path(
                    cv, [(px + ox, py + oy) for px, py in pts], g, wd))
            x += length + rng.uniform(8, 60)

    for kx, ky, kr in knots:
        for ring in range(3):
            rx, ry = kr * (1.0 + ring * 0.7), kr * (0.42 + ring * 0.3)
            g = 80 + ring * 25
            tiled(W, H, lambda ox, oy, rx=rx, ry=ry, g=g: cv.ellipse(kx + ox, ky + oy, rx, ry, outline=g, width=1.4))
        tiled(W, H, lambda ox, oy: cv.ellipse(kx + ox, ky + oy, kr * 0.45, kr * 0.22, fill=70))

    for _ in range(420):
        x, y = rng.uniform(0, W), rng.uniform(0, H)
        if rng.random() < 0.7:
            r, g = rng.uniform(0.6, 1.4), int(rng.uniform(80, 170))
            tiled(W, H, lambda ox, oy, x=x, y=y, r=r, g=g: cv.dot(x + ox, y + oy, r, g))
        else:
            ln, g = rng.uniform(4, 14), int(rng.uniform(90, 160))
            tiled(W, H, lambda ox, oy, x=x, y=y, ln=ln, g=g: cv.line(
                [(x + ox, y + oy), (x + ln + ox, y + rng.uniform(-1, 1) + oy)], g, 0.9))

    img = cv.result()
    tone = 0.93 + 0.07 * fbm(W, H, seed + 3)
    arr = np.asarray(img).astype(np.float32) / 255.0 * tone
    return save(to_img(arr), "T_Ink_Wood")


def ink_plaster(seed=21, size=1024):
    """Painted plaster: faint mottling, stipple, a few hairline cracks."""
    rng = np.random.default_rng(seed)
    cv = Canvas(size, size, ss=2, mode="L", bg=255)
    for _ in range(1500):
        x, y, r = rng.uniform(0, size), rng.uniform(0, size), rng.uniform(0.5, 1.2)
        g = int(rng.uniform(160, 215))
        tiled(size, size, lambda ox, oy, x=x, y=y, r=r, g=g: cv.dot(x + ox, y + oy, r, g))
    for _ in range(70):
        x, y, r = rng.uniform(0, size), rng.uniform(0, size), rng.uniform(0.8, 1.6)
        g = int(rng.uniform(95, 135))
        tiled(size, size, lambda ox, oy, x=x, y=y, r=r, g=g: cv.dot(x + ox, y + oy, r, g))
    for _ in range(9):
        x, y = rng.uniform(0, size), rng.uniform(0, size)
        ang = rng.uniform(0, 6.28)
        pts = [(x, y)]
        for _ in range(int(rng.integers(5, 14))):
            ang += rng.uniform(-0.7, 0.7)
            step = rng.uniform(3, 7)
            x, y = x + math.cos(ang) * step, y + math.sin(ang) * step
            pts.append((x, y))
        g = int(rng.uniform(115, 150))
        tiled(size, size, lambda ox, oy, pts=pts, g=g: _stroke_path(cv, [(a + ox, b + oy) for a, b in pts], g, 0.9))
    img = cv.result()
    tone = 1.0 - 0.055 * fbm(size, size, seed + 5)
    arr = np.asarray(img).astype(np.float32) / 255.0 * tone
    return save(to_img(arr), "T_Ink_Plaster")


def ink_fabric(seed=31, size=512):
    """Woven cloth: fine warp/weft lines and slubs."""
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    period = 8.0
    warp = 0.5 + 0.5 * np.cos(2 * math.pi * xx / period)
    weft = 0.5 + 0.5 * np.cos(2 * math.pi * yy / period + math.pi * (np.floor(xx / period) % 2))
    weave = 1.0 - 0.07 * warp * weft - 0.04 * warp
    slub = 1.0 - 0.05 * periodic_noise(size, size, 40, seed)
    arr = weave * slub
    img = to_img(arr)
    cv = Canvas(size, size, ss=2, mode="L", bg=255)
    cv.img.paste(img.resize((size * 2, size * 2), Image.BILINEAR))
    for _ in range(25):
        x, y = rng.uniform(0, size), rng.uniform(0, size)
        ln, g = rng.uniform(3, 8), int(rng.uniform(120, 170))
        tiled(size, size, lambda ox, oy, x=x, y=y, ln=ln, g=g: cv.line(
            [(x + ox, y + oy), (x + ln + ox, y + ln * 0.3 + oy)], g, 0.8))
    return save(cv.result(), "T_Ink_Fabric")


def curtain_floral(seed=61, size=1024):
    """Cream curtain with terracotta flowers and olive leaves."""
    rng = np.random.default_rng(seed)
    BG, PET, PET2, LEAF = hexc("E4CD9C"), hexc("A9472F"), hexc("4A6690"), hexc("7A7038")
    cv = Canvas(size, size, ss=3, bg=BG)
    pts = []
    tries = 0
    while len(pts) < 17 and tries < 4000:
        tries += 1
        x, y = rng.uniform(0, size), rng.uniform(0, size)
        ok = True
        for px, py in pts:
            dx = abs(x - px)
            dy = abs(y - py)
            dx, dy = min(dx, size - dx), min(dy, size - dy)
            if math.hypot(dx, dy) < 215:
                ok = False
                break
        if ok:
            pts.append((x, y))

    def leaf(ox, oy, x, y, ang, ln, wd):
        tip = (x + math.cos(ang) * ln, y + math.sin(ang) * ln)
        nx, ny = -math.sin(ang), math.cos(ang)
        mid = (x + math.cos(ang) * ln * 0.5, y + math.sin(ang) * ln * 0.5)
        shape = [(x + ox, y + oy), (mid[0] + nx * wd + ox, mid[1] + ny * wd + oy), (tip[0] + ox, tip[1] + oy),
                 (mid[0] - nx * wd + ox, mid[1] - ny * wd + oy)]
        smooth = resample(shape + [shape[0]], 3.0)
        cv.poly(smooth, fill=LEAF, outline=INK, width=2.2)
        cv.line([(x + ox, y + oy), (tip[0] + ox, tip[1] + oy)], INK, 1.2)

    for (x, y) in pts:
        r = rng.uniform(34, 50)
        rot = rng.uniform(0, 6.28)
        leaves = [(rot + rng.uniform(0.6, 1.2), rng.uniform(55, 80)), (rot + math.pi + rng.uniform(-0.4, 0.4), rng.uniform(50, 75))]
        petal_col = PET if rng.random() < 0.65 else PET2
        centre_col = PET2 if petal_col == PET else PET

        def flower(ox, oy, x=x, y=y, r=r, rot=rot, leaves=leaves, pc=petal_col, cc=centre_col):
            for ang, ln in leaves:
                sx, sy = x + math.cos(ang) * r * 0.6, y + math.sin(ang) * r * 0.6
                leaf(ox, oy, sx, sy, ang, ln, ln * 0.28)
            for k in range(5):
                ang = rot + k * 2 * math.pi / 5
                cv.ellipse(x + math.cos(ang) * r * 0.55 + ox, y + math.sin(ang) * r * 0.55 + oy, r * 0.5, r * 0.4,
                           fill=pc, outline=INK, width=2.2, rot=ang)
            cv.ellipse(x + ox, y + oy, r * 0.3, r * 0.3, fill=cc, outline=INK, width=2.0)
            for k in range(6):
                ang = rot + k * 1.05
                cv.dot(x + math.cos(ang) * r * 0.14 + ox, y + math.sin(ang) * r * 0.14 + oy, 1.6, INK)
        tiled(size, size, flower)

    for _ in range(60):
        x, y = rng.uniform(0, size), rng.uniform(0, size)
        tiled(size, size, lambda ox, oy, x=x, y=y: [cv.dot(x + ox + dx, y + oy + dy, 2.2, PET2)
                                                     for dx, dy in ((0, 0), (6, 3), (2, 7))])
    img = cv.result()
    weave = np.asarray(Image.open(ink_fabric_path()).convert("L").resize((size, size))).astype(np.float32) / 255
    arr = np.asarray(img).astype(np.float32) * (0.35 + 0.65 * weave)[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), "T_Curtain_Floral")


def ink_fabric_path():
    import os
    from texlib import OUT
    return os.path.join(OUT, "T_Ink_Fabric.png")


def cork(seed=81, size=512):
    rng = np.random.default_rng(seed)
    base = np.array(hexc("B58355"), np.float32)
    n = fbm(size, size, seed, octaves=((8, 0.4), (30, 0.3), (90, 0.3)))
    arr = base[None, None, :] * (0.82 + 0.3 * n)[..., None]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    cv = Canvas(size, size, ss=2)
    cv.img.paste(img.resize((size * 2, size * 2), Image.BILINEAR))
    for _ in range(2600):
        x, y, r = rng.uniform(0, size), rng.uniform(0, size), rng.uniform(0.6, 2.2)
        col = hexc("7E5431") if rng.random() < 0.6 else hexc("D6A676")
        tiled(size, size, lambda ox, oy, x=x, y=y, r=r, col=col: cv.dot(x + ox, y + oy, r, col))
    return save(cv.result(), "T_Cork")


def paper_grain(seed=91, size=512):
    """R: fine paper grain around 0.5. G, B: smooth noise for line wobble."""
    rng = np.random.default_rng(seed)
    fine = periodic_noise(size, size, 170, seed) * 0.55 + periodic_noise(size, size, 60, seed + 1) * 0.3
    fine += rng.random((size, size)) * 0.15
    fine = (fine - fine.mean()) / (fine.std() * 5.0) + 0.5
    g = periodic_noise(size, size, 5, seed + 2)
    b = periodic_noise(size, size, 5, seed + 3)
    arr = np.stack([np.clip(fine, 0, 1), g, b], axis=-1)
    return save(Image.fromarray((arr * 255).astype(np.uint8), "RGB"), "T_PaperGrain")


def ink_tabby(seed=101, size=512):
    """Bold wavy tabby stripes for the cat (grayscale multiply)."""
    rng = np.random.default_rng(seed)
    cv = Canvas(size, size, ss=2, mode="L", bg=255)
    n = 7
    for i in range(n):
        y0 = (i + 0.5) * size / n + rng.uniform(-8, 8)
        a1, p1 = rng.uniform(6, 14), rng.uniform(0, 6.28)
        pts = [(x, y0 + a1 * math.sin(2 * math.pi * 2 * x / size + p1)) for x in np.arange(-10, size + 11, 8.0)]
        width = rng.uniform(10, 16)
        tiled(size, size, lambda ox, oy, pts=pts, width=width: cv.line([(a + ox, b + oy) for a, b in pts], 120, width))
        for _ in range(3):
            x = rng.uniform(0, size)
            y = y0 + rng.uniform(-size / n * 0.45, size / n * 0.45)
            tiled(size, size, lambda ox, oy, x=x, y=y: cv.ellipse(x + ox, y + oy, 14, 5, fill=150))
    img = cv.result()
    arr = np.asarray(img).astype(np.float32) / 255.0 * (0.94 + 0.06 * fbm(size, size, seed + 1))
    return save(to_img(arr), "T_Ink_Tabby")


def floor_vinyl(seed=121, size=2048, planks=12):
    """Old Korean vinyl floor (jangpan) printed as honey wood planks, 12 cm boards."""
    rng = np.random.default_rng(seed)
    S = size
    pw = S / planks
    BASE, DARK, LIGHT, JOINT = hexc("C08A50"), hexc("98652F"), hexc("D9A86A"), hexc("6A4424")
    grain = periodic_noise(S, S, 3, seed + 1)
    arr = np.zeros((S, S, 3), np.float32)
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    boards = []
    for i in range(planks):
        y = 0.0
        offset = rng.uniform(0, S)
        cuts = [0.0]
        while cuts[-1] < S:
            cuts.append(cuts[-1] + rng.uniform(0.45, 0.95) * S)
        for a, b in zip(cuts, cuts[1:]):
            boards.append((i, (a + offset) % S, b - a, rng.uniform(0.9, 1.1), rng.uniform(0, 100)))
    col = np.zeros((S, S), np.float32)
    tone = np.ones((S, S), np.float32)
    streak = np.zeros((S, S), np.float32)
    for i, start, length, k, ph in boards:
        x0, x1 = int(i * pw), int((i + 1) * pw)
        span = (yy[:, x0:x1] - start) % S
        mask = span < length
        tone[:, x0:x1] = np.where(mask, k, tone[:, x0:x1])
        wav = np.sin((xx[:, x0:x1] - x0) / pw * 6.28 * 2.5 + np.sin(yy[:, x0:x1] / S * 6.28 * 2 + ph) * 0.7 + ph)
        wav += 0.6 * np.sin((xx[:, x0:x1] - x0) / pw * 6.28 * 5.5 + ph * 1.7)
        streak[:, x0:x1] = np.where(mask, wav, streak[:, x0:x1])
    fine = periodic_noise(S, S, 220, seed + 2)
    t = np.clip(0.5 + 0.22 * streak + 0.3 * (fine - 0.5) + 0.25 * (grain - 0.5), 0, 1)
    base = np.array(BASE, np.float32)
    arr = np.where(t[..., None] < 0.5,
                   np.array(DARK, np.float32) + (base - np.array(DARK, np.float32)) * (t[..., None] / 0.5),
                   base + (np.array(LIGHT, np.float32) - base) * ((t[..., None] - 0.5) / 0.5))
    arr *= tone[..., None]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    cv = Canvas(S, S, ss=1)
    cv.img.paste(img)
    for i in range(planks + 1):
        x = i * pw
        cv.rect(x - 1.5, 0, x + 1.5, S, fill=JOINT)
    for i, start, length, k, ph in boards:
        x0 = i * pw
        for yv in (start, (start + length) % S):
            cv.rect(x0, yv - 1.5, x0 + pw, yv + 1.5, fill=JOINT)
    for _ in range(1600):
        x, y = rng.uniform(0, S), rng.uniform(0, S)
        cv.dot(x, y, rng.uniform(0.6, 1.6), hexc("7C5230") if rng.random() < 0.7 else hexc("E6BE86"))
    for _ in range(40):
        x, y = rng.uniform(0, S), rng.uniform(0, S)
        ln = rng.uniform(20, 90)
        a = rng.uniform(-0.3, 0.3)
        cv.line([(x, y), (x + math.sin(a) * ln, y + math.cos(a) * ln)], hexc("E4BD85"), 1.2)
    return save(cv.result(), "T_Floor_Vinyl")


def bedding(seed=141, size=512):
    """Blue gingham comforter with thin red lines."""
    CREAM, BLUE, RED = np.array(hexc("E6DAC2"), np.float32), np.array(hexc("4F6F98"), np.float32), np.array(hexc("A8473A"), np.float32)
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    cell = size / 8
    bx = ((xx // (cell / 2)) % 2 == 0).astype(np.float32)
    by = ((yy // (cell / 2)) % 2 == 0).astype(np.float32)
    mixv = (bx + by) / 2.0
    arr = CREAM[None, None] * (1 - mixv[..., None] * 0.62) + BLUE[None, None] * (mixv[..., None] * 0.62)
    red = ((np.abs((xx % cell) - cell * 0.75) < 1.6) | (np.abs((yy % cell) - cell * 0.75) < 1.6)).astype(np.float32)
    arr = arr * (1 - red[..., None] * 0.85) + RED[None, None] * red[..., None] * 0.85
    weave = np.asarray(Image.open(ink_fabric_path()).convert("L").resize((size, size))).astype(np.float32) / 255
    arr *= (0.55 + 0.45 * weave)[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), "T_Bedding")


def ink_brick(seed=151, size=512):
    """Grayscale running-bond brick courses for the neighbouring villa."""
    rng = np.random.default_rng(seed)
    cv = Canvas(size, size, ss=2, mode="L", bg=235)
    rows, cols = 8, 4
    bh, bw = size / rows, size / cols
    for r in range(rows):
        off = bw / 2 if r % 2 else 0
        for c in range(-1, cols + 1):
            x0 = c * bw + off
            g = int(rng.uniform(175, 235))
            tiled(size, size, lambda ox, oy, x0=x0, r=r, g=g: cv.rect(x0 + 3 + ox, r * bh + 3 + oy, x0 + bw - 3 + ox,
                                                                     (r + 1) * bh - 3 + oy, fill=g))
    for _ in range(900):
        x, y = rng.uniform(0, size), rng.uniform(0, size)
        tiled(size, size, lambda ox, oy, x=x, y=y: cv.dot(x + ox, y + oy, rng.uniform(0.6, 1.4), 140))
    img = cv.result()
    arr = np.asarray(img).astype(np.float32) / 255.0 * (0.95 + 0.05 * fbm(size, size, seed + 1))
    return save(to_img(arr), "T_Ink_Brick")


# ---------------------------------------------------------------- painted walls

PX_PER_M = 400
WALL_H = 2.4
PAINT = hexc("B3C3C6")        # dusty blue-grey emulsion
YELLOWED = hexc("B8A274")     # old paint gone yellow under the ceiling
WATER = hexc("9C8158")        # tide lines of dried leaks
DAMP = hexc("7F8A6C")         # rising damp near the floor
MOLD = hexc("3B3A30")
GRIME = hexc("6E6558")

# Each wall as seen from inside, left to right, in metres from its left edge (s)
# and height above the floor (z). The layout matches blender/bedroom_shell.py.
WALL_FEATURES = {
    "west": {   # exterior wall with the window; left edge = SW corner
        "length": 3.8, "exterior": True, "seed": 1,
        "blotches": [(3.72, 2.3, 0.5, 0.45), (0.06, 2.32, 0.35, 0.3), (1.15, 2.36, 0.28, 0.12)],
        "drips": [(0.78, 2.02, 0.75), (0.66, 2.02, 0.45), (2.08, 0.95, 0.38), (3.42, 0.95, 0.42), (3.33, 0.95, 0.25)],
        "mold": [(3.8, 2.4, 0.45), (3.8, 0.0, 0.35), (2.05, 0.9, 0.1), (3.45, 0.9, 0.12)],
        "damp": [(0.0, 0.62, 0.52)],
    },
    "north": {  # exterior wall behind bed, shelf and desk; left edge = NW corner
        "length": 3.9, "exterior": True, "seed": 2,
        "blotches": [(0.1, 2.3, 0.45, 0.42), (3.3, 2.38, 0.5, 0.14)],
        "drips": [(0.3, 2.1, 0.5)],
        "mold": [(0.0, 2.4, 0.4), (0.0, 0.0, 0.25)],
        "rubs": [(0.17, 1.23, 0.93, 0.08)],
        "ghosts": [(2.1, 1.62, 2.52, 2.1)],
    },
    "east": {   # interior wall; left edge = NE corner
        "length": 3.8, "exterior": False, "seed": 3,
        "blotches": [(3.75, 2.36, 0.4, 0.16)],
        "ghosts": [(1.55, 1.25, 1.95, 1.82)],
        "scuffs": [(2.9, 3.6, 0.12, 0.5)],
    },
    "south": {  # interior wall with the door; left edge = SE corner
        "length": 3.9, "exterior": False, "seed": 4,
        "blotches": [(3.85, 2.34, 0.3, 0.22)],
        "grime": [(1.58, 1.3, 0.22, 0.3), (0.35, 1.05, 0.12, 0.35)],
        "damp": [(3.9, 0.45, 0.4)],
        "scuffs": [(1.9, 3.1, 0.08, 0.3)],
    },
}


def _lerp(arr, col, k):
    """Blend arr (H, W, 3) toward a colour by k (H, W)."""
    c = np.array(col, np.float32)[None, None, :]
    return arr + (c - arr) * np.clip(k, 0, 1)[..., None]


def wall_band(name, spec):
    """One wall at PX_PER_M: paint, yellowing, leaks, damp, mold, grime and ghosts."""
    rng = np.random.default_rng(spec["seed"])
    W, H = int(spec["length"] * PX_PER_M), int(WALL_H * PX_PER_M)
    seed = 700 + spec["seed"] * 10
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    s = xx / PX_PER_M
    z = (H - 1 - yy) / PX_PER_M
    n1 = fbm(W, H, seed, octaves=((3, 0.5), (8, 0.3), (24, 0.2)))
    n2 = fbm(W, H, seed + 1, octaves=((6, 0.5), (16, 0.3), (40, 0.2)))
    arr = np.array(PAINT, np.float32)[None, None, :] * (0.965 + 0.07 * n1)[..., None]

    # Yellowing under the ceiling, heavier on exterior walls and in the corners.
    corner = np.exp(-(s / 0.5) ** 2) + np.exp(-((spec["length"] - s) / 0.5) ** 2)
    depth = 0.32 if spec["exterior"] else 0.22
    top = np.clip((z - (WALL_H - depth - 0.45 * (n2 - 0.5) - 0.12 * corner)) / 0.4, 0, 1) ** 1.6
    arr = _lerp(arr, YELLOWED, top * (0.35 + 0.25 * corner + 0.2 * n1))

    ink = []    # strokes drawn afterwards: (points, colour, width)
    for (cs, cz, rx, rz) in spec.get("blotches", []):
        d = np.sqrt(((s - cs) / rx) ** 2 + ((z - cz) / rz) ** 2) + (n2 - 0.5) * 0.7
        inside = np.clip((1.0 - d) * 4.0, 0, 1)
        arr = _lerp(arr, YELLOWED, inside * 0.35)
        for k, ring in enumerate((1.0, 0.72, 0.5)):
            line = np.exp(-((d - ring) / (0.018 + 0.01 * k)) ** 2)
            arr = _lerp(arr, WATER, line * (0.55 - 0.12 * k))
        for _ in range(int(3 + rx * 8)):
            a = rng.uniform(0, 2 * math.pi)
            r = rng.uniform(0.2, 0.9)
            x0, z0 = cs + math.cos(a) * rx * r, cz + math.sin(a) * rz * r * 0.8
            ln = rng.uniform(0.05, 0.14)
            ink.append(([(x0, z0), (x0 + ln * 0.8, z0 + ln * 0.55)], WATER, 1.6))

    for (ds, dz, ln) in spec.get("drips", []):
        # Streaks from a leak: wide and faint at the source, a thin run below.
        fall = np.clip((dz - z) / ln, 0, 1)
        wob = np.sin(z * 7.0 + ds * 13) * 0.01 + (n1 - 0.5) * 0.03
        width = 0.05 * (1.0 - fall) + 0.008
        across = np.exp(-((s - ds - wob) / width) ** 2)
        along = np.clip((dz - z) / 0.02, 0, 1) * (1.0 - fall) ** 0.5
        arr = _lerp(arr, WATER, across * along * 0.4)
        edge = np.exp(-((np.abs(s - ds - wob) - width) / 0.004) ** 2) * along * (1.0 - fall)
        arr = _lerp(arr, WATER, edge * 0.35)

    for (ds, top_z, width) in spec.get("damp", []):
        edge = top_z * (0.75 + 0.25 * np.exp(-((s - ds) / width) ** 2)) + (n2 - 0.5) * 0.12
        reach = np.exp(-((s - ds) / (width * 1.4)) ** 2)
        wet = np.clip((edge - z) / 0.1, 0, 1) * reach
        arr = _lerp(arr, DAMP, wet * 0.4)
        line = np.exp(-((z - edge) / 0.012) ** 2) * reach
        arr = _lerp(arr, WATER, line * 0.6)

    for (rs0, rs1, rz, rh) in spec.get("rubs", []):
        band = np.clip(np.minimum(s - rs0, rs1 - s) / 0.08, 0, 1) * np.exp(-((z - rz) / rh) ** 2)
        arr = _lerp(arr, GRIME, band * np.clip(n2 * 1.6 - 0.45, 0, 1) * 0.3)

    for (gs, gz, rs, rz) in spec.get("grime", []):
        blob = np.exp(-((s - gs) / rs) ** 2 - ((z - gz) / rz) ** 2) * np.clip(n1 * 1.8 - 0.5, 0, 1)
        arr = _lerp(arr, GRIME, blob * 0.28)

    for (x0, z0, x1, z1) in spec.get("ghosts", []):
        # Where a poster hung for years the paint stayed clean; dust outlines the edge.
        box = np.clip(np.minimum.reduce([s - x0, x1 - s, z - z0, z1 - z]) / 0.006, 0, 1)
        clean = np.array(PAINT, np.float32)[None, None, :] * (1.0 + 0.03 * n1)[..., None]
        arr = arr + (clean - arr) * (box * 0.85)[..., None]
        edge = np.exp(-(np.minimum.reduce([np.abs(s - x0), np.abs(s - x1)]) / 0.004) ** 2) * ((z > z0) & (z < z1))
        edge = np.maximum(edge, np.exp(-(np.minimum(np.abs(z - z0), np.abs(z - z1)) / 0.004) ** 2) * ((s > x0) & (s < x1)))
        arr = _lerp(arr, GRIME, edge * 0.35)

    for (ms, mz, r) in spec.get("mold", []):
        haze = np.exp(-(((s - ms) / (r * 1.1)) ** 2 + ((z - mz) / (r * 0.8)) ** 2)) * np.clip(n2 * 1.5 - 0.2, 0, 1)
        arr = _lerp(arr, mix(MOLD, DAMP, 0.5), haze * 0.3)

    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    cv = Canvas(W, H, ss=2)
    cv.img.paste(img.resize((W * 2, H * 2), Image.BILINEAR))

    def P(sm, zm):
        return sm * PX_PER_M, (WALL_H - zm) * PX_PER_M

    for (x0, z0, x1, z1) in spec.get("ghosts", []):
        for (tx, tz) in ((x0, z1), (x1, z1), (x0, z0), (x1, z0)):
            cx, cy = P(tx, tz)
            cv.ellipse(cx, cy, 13, 5, fill=hexc("D9CBA4"), rot=rng.uniform(-0.8, 0.8), n=4)

    for (ms, mz, r) in spec.get("mold", []):
        # Black mould in damp corners: a grey haze with speckles, densest at the corner.
        for _ in range(int(700 * r)):
            a = rng.uniform(0, 2 * math.pi)
            d = abs(rng.normal(0, r * 0.5))
            px, pz = ms + math.cos(a) * d * 1.3, mz + math.sin(a) * d
            if not (0 <= px <= spec["length"] and 0 <= pz <= WALL_H):
                continue
            x, y = P(px, pz)
            k = math.exp(-(d / r) ** 2)
            cv.dot(x, y, rng.uniform(0.5, 1.3) * (0.5 + k), mix(PAINT, MOLD, 0.15 + 0.5 * k * rng.uniform(0.4, 1.0)))

    for (x0, x1, z0, z1) in spec.get("scuffs", []):
        for _ in range(9):
            sx, sz = rng.uniform(x0, x1), rng.uniform(z0, z1)
            ln = rng.uniform(0.03, 0.12)
            a, b = P(sx, sz), P(sx + ln, sz + rng.uniform(-0.01, 0.01))
            cv.line([a, b], mix(PAINT, GRIME, 0.7), rng.uniform(1.0, 2.2))

    for pts, col, wd in ink:
        cv.line([P(a, b) for a, b in pts], mix(PAINT, col, 0.8), wd)
    return cv.result()


def wall_paint():
    """T_Wall_Paint: one band per wall (see atlas.WALLS), old paint with the marks of a
    twenty-year-old room in a humid city."""
    aw, ah = atlas.SIZES["walls"]
    out = Image.new("RGB", (aw, ah), PAINT)
    for name, spec in WALL_FEATURES.items():
        x0, y0, x1, y1 = atlas.WALLS[name]
        band = wall_band(name, spec).resize((x1 - x0, y1 - y0), Image.LANCZOS)
        out.paste(band, (x0, y0))
    return save(out, "T_Wall_Paint")


def run():
    return [ink_wood(), ink_plaster(), ink_fabric(), curtain_floral(), cork(), paper_grain(), ink_tabby(), floor_vinyl(),
            bedding(), ink_brick(), wall_paint()]


if __name__ == "__main__":
    for p in run():
        print(p)
