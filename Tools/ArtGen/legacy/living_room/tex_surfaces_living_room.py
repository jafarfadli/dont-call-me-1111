"""Living-room surfaces: terrace tiles, retro cement floor tiles, batik and the
striped door curtain. Snapshot kept for reference; see README.md."""
import os
import sys

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

import math

import numpy as np
from PIL import Image

from texlib import (INK, Canvas, fbm, hexc, mix, periodic_noise, resample, save, shade, tiled, to_img,
                    wobble)


from tex_surfaces import _stroke_path, ink_fabric_path  # noqa: E402,F401


def floor_tiles(seed=41, tiles=4, tile_px=512):
    """Cream 40x40 ceramic tiles, 4x4 per texture (1.6 m), inked joints."""
    rng = np.random.default_rng(seed)
    S = tiles * tile_px
    base = np.array(hexc("E8DEC5"), dtype=np.float32)
    arr = np.zeros((S, S, 3), np.float32)
    mott = fbm(S, S, seed + 1, octaves=((5, 0.45), (12, 0.3), (30, 0.15), (90, 0.1)))
    for ty in range(tiles):
        for tx in range(tiles):
            k = 1.0 + rng.uniform(-0.03, 0.03)
            tint = base * k + rng.uniform(-3, 3, 3)
            arr[ty * tile_px:(ty + 1) * tile_px, tx * tile_px:(tx + 1) * tile_px] = tint
    arr *= (0.955 + 0.07 * mott)[..., None]

    # soft bevel: lighter top/left, darker bottom/right inside each tile
    yy, xx = np.mgrid[0:S, 0:S]
    lx, ly = xx % tile_px, yy % tile_px
    bevel = np.ones((S, S), np.float32)
    bw = 7.0
    bevel += 0.035 * np.clip(1 - lx / bw, 0, 1) + 0.035 * np.clip(1 - ly / bw, 0, 1)
    bevel -= 0.05 * np.clip(1 - (tile_px - 1 - lx) / bw, 0, 1) + 0.05 * np.clip(1 - (tile_px - 1 - ly) / bw, 0, 1)
    arr *= bevel[..., None]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")

    cv = Canvas(S, S, ss=2)
    cv.img.paste(img.resize((S * 2, S * 2), Image.BILINEAR))
    speck_dark, speck_light = hexc("C9BB9C"), hexc("F6F0E2")
    for _ in range(14000):
        x, y, r = rng.uniform(0, S), rng.uniform(0, S), rng.uniform(0.6, 1.8)
        cv.dot(x, y, r, speck_dark if rng.random() < 0.55 else speck_light)

    grout = hexc("B7AA90")
    g = 4.0
    for k in range(tiles + 1):
        p = k * tile_px
        cv.rect(p - g, -10, p + g, S + 10, fill=grout)
        cv.rect(-10, p - g, S + 10, p + g, fill=grout)

    line = hexc("7C6C58")
    for k in range(tiles + 1):
        p = k * tile_px
        for axis in (0, 1):
            pts = [(p + g, -20), (p + g, S + 20)] if axis == 0 else [(-20, p + g), (S + 20, p + g)]
            path = wobble(pts, 0.7, rng, step=8.0, freq=0.02)
            tiled(S, S, lambda ox, oy, path=path: _stroke_path(cv, [(a + ox, b + oy) for a, b in path], line, 1.5,
                                                              taper=False))

    for _ in range(6):
        tx, ty = int(rng.integers(0, tiles)), int(rng.integers(0, tiles))
        cx = tx * tile_px + g + 2 + rng.uniform(0, 1) * (tile_px - 2 * g - 20)
        cy = ty * tile_px + g + 2
        cv.poly([(cx, cy), (cx + rng.uniform(5, 10), cy), (cx + 3, cy + rng.uniform(3, 6))], fill=hexc("A59478"))
    for _ in range(2):
        x, y = rng.uniform(40, S - 40), rng.uniform(40, S - 40)
        ang = rng.uniform(0, 6.28)
        pts = [(x, y)]
        for _ in range(9):
            ang += rng.uniform(-0.5, 0.5)
            x, y = x + math.cos(ang) * 9, y + math.sin(ang) * 9
            pts.append((x, y))
        _stroke_path(cv, pts, hexc("9A8B72"), 0.9)
    return save(cv.result(), "T_Floor_Tiles")


def floor_tegel(seed=111, tiles=8, tile_px=256):
    """Old patterned cement tiles ("tegel"): 20 cm tiles whose corner arcs join into
    circles across four tiles, a blue centre flower, worn and a little stained."""
    rng = np.random.default_rng(seed)
    S = tiles * tile_px
    CREAM, RED, BLUE, BROWN = hexc("E3D3B2"), hexc("A95A46"), hexc("4A5E7C"), hexc("5E3F2C")
    base = np.array(CREAM, np.float32)
    mott = fbm(S, S, seed + 1, octaves=((4, 0.45), (11, 0.3), (28, 0.15), (80, 0.1)))
    arr = np.ones((S, S, 3), np.float32) * base
    arr *= (0.93 + 0.1 * mott)[..., None]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")
    cv = Canvas(S, S, ss=2)
    cv.img.paste(img.resize((S * 2, S * 2), Image.BILINEAR))
    t = tile_px
    R = t * 0.43
    for ty in range(tiles + 1):
        for tx in range(tiles + 1):
            cx, cy = tx * t, ty * t
            cv.ellipse(cx, cy, R, R, fill=RED, outline=BROWN, width=2.2, n=96)
            cv.ellipse(cx, cy, R * 0.72, R * 0.72, fill=CREAM, outline=BROWN, width=1.6, n=96)
            cv.ellipse(cx, cy, R * 0.5, R * 0.5, outline=BROWN, width=1.2, n=72)
            cv.ellipse(cx, cy, R * 0.16, R * 0.16, fill=RED, outline=BROWN, width=1.2, n=48)
            for k in range(8):
                a = k * math.pi / 4
                cv.dot(cx + math.cos(a) * R * 0.86, cy + math.sin(a) * R * 0.86, 3.2, BROWN)
    for ty in range(tiles):
        for tx in range(tiles):
            cx, cy = (tx + 0.5) * t, (ty + 0.5) * t
            for k in range(4):
                a = k * math.pi / 2 + math.pi / 4
                cv.ellipse(cx + math.cos(a) * t * 0.11, cy + math.sin(a) * t * 0.11, t * 0.1, t * 0.05, fill=BLUE, outline=BROWN,
                           width=1.4, rot=a)
            cv.ellipse(cx, cy, t * 0.045, t * 0.045, fill=hexc("C7963E"), outline=BROWN, width=1.2)
    line = hexc("6E5644")
    for k in range(tiles + 1):
        p = k * t
        cv.rect(p - 1.6, -4, p + 1.6, S + 4, fill=line)
        cv.rect(-4, p - 1.6, S + 4, p + 1.6, fill=line)
    wear = fbm(S, S, seed + 7, octaves=((6, 0.6), (18, 0.4)))
    out = np.asarray(cv.result()).astype(np.float32)
    fade = np.clip((wear - 0.55) * 2.5, 0, 1)[..., None]
    out = out * (1 - 0.35 * fade) + np.array(CREAM, np.float32) * 0.35 * fade
    out = out * 0.84 + np.array(CREAM, np.float32) * 0.16
    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    cv = Canvas(S, S, ss=1)
    cv.img.paste(img)
    for _ in range(9000):
        x, y, r = rng.uniform(0, S), rng.uniform(0, S), rng.uniform(0.5, 1.4)
        cv.dot(x, y, r, hexc("8C7458") if rng.random() < 0.6 else hexc("F2E8D4"))
    for _ in range(5):
        x, y = rng.uniform(20, S - 20), rng.uniform(20, S - 20)
        ang = rng.uniform(0, 6.28)
        pts = [(x, y)]
        for _ in range(10):
            ang += rng.uniform(-0.5, 0.5)
            x, y = x + math.cos(ang) * 8, y + math.sin(ang) * 8
            pts.append((x, y))
        _stroke_path(cv, pts, hexc("7A6450"), 0.9)
    return save(cv.result(), "T_Floor_Tegel")


def batik_kawung(seed=51, size=1024, cell=256):
    """Kawung batik in sogan colours: brown ground, cream petals, indigo accents, wax crackle."""
    rng = np.random.default_rng(seed)
    BG, PETAL, LINE, ACC = hexc("5A3522"), hexc("E6D2A8"), hexc("2A1810"), hexc("3D5078")
    CRACK = hexc("472818")
    cv = Canvas(size, size, ss=3, bg=BG)
    for _ in range(46):
        x, y = rng.uniform(0, size), rng.uniform(0, size)
        ang = rng.uniform(0, 6.28)
        pts = [(x, y)]
        for _ in range(int(rng.integers(6, 18))):
            ang += rng.uniform(-0.9, 0.9)
            x, y = x + math.cos(ang) * 10, y + math.sin(ang) * 10
            pts.append((x, y))
        tiled(size, size, lambda ox, oy, pts=pts: _stroke_path(cv, [(a + ox, b + oy) for a, b in pts], CRACK, 0.9))

    d = cell * math.sqrt(2) / 4.0
    a, b = cell * 0.33, cell * 0.2
    n = size // cell
    for i in range(n):
        for j in range(n):
            cx, cy = i * cell, j * cell
            for k in range(4):
                ang = math.pi / 4 + k * math.pi / 2
                px, py = cx + math.cos(ang) * d, cy + math.sin(ang) * d

                def petal(ox, oy, px=px, py=py, ang=ang):
                    cv.ellipse(px + ox, py + oy, a, b, fill=PETAL, outline=LINE, width=3.0, rot=ang)
                    cv.ellipse(px + ox, py + oy, a * 0.68, b * 0.58, outline=LINE, width=1.5, rot=ang)
                    sx, sy = px + math.cos(ang) * a * 0.1, py + math.sin(ang) * a * 0.1
                    cv.ellipse(sx + ox, sy + oy, a * 0.2, b * 0.18, fill=ACC, outline=LINE, width=1.2, rot=ang)
                    for t in range(30):
                        th = 2 * math.pi * t / 30
                        ex, ey = a * 1.13 * math.cos(th), b * 1.2 * math.sin(th)
                        c, s = math.cos(ang), math.sin(ang)
                        cv.dot(px + ex * c - ey * s + ox, py + ex * s + ey * c + oy, 1.3, PETAL)
                tiled(size, size, petal)

    for i in range(n):
        for j in range(n):
            for (cx, cy, r) in ((i * cell, j * cell, cell * 0.06), ((i + 0.5) * cell, (j + 0.5) * cell, cell * 0.045)):
                tiled(size, size, lambda ox, oy, cx=cx, cy=cy, r=r: cv.ellipse(
                    cx + ox, cy + oy, r, r, fill=ACC, outline=LINE, width=1.8, rot=0.785, n=4))
            for (cx, cy) in (((i + 0.5) * cell, j * cell), (i * cell, (j + 0.5) * cell)):
                tiled(size, size, lambda ox, oy, cx=cx, cy=cy: cv.ellipse(
                    cx + ox, cy + oy, cell * 0.05, cell * 0.05, fill=PETAL, outline=LINE, width=1.5, n=4))
    return save(cv.result(), "T_Batik_Kawung")


def door_curtain_stripes(seed=71, size=512):
    """Striped cloth for the doorway curtain."""
    TEAL, CREAM, MUS, MAR = hexc("34507A"), hexc("E6D5B0"), hexc("A8743A"), hexc("9C3B2E")
    seq = [(TEAL, 70), (CREAM, 12), (MUS, 10), (CREAM, 12), (TEAL, 70), (MAR, 22), (CREAM, 8), (MAR, 22),
           (TEAL, 70), (CREAM, 12), (MUS, 10), (CREAM, 12), (TEAL, 70), (MAR, 22), (CREAM, 8), (MAR, 22)]
    total = sum(w for _, w in seq)
    scale = size / total
    cv = Canvas(size, size, ss=2, bg=TEAL)
    x = 0.0
    for col, w in seq:
        cv.rect(x, -2, x + w * scale, size + 2, fill=col)
        x += w * scale
    x = 0.0
    for col, w in seq:
        cv.line([(x, -2), (x, size + 2)], shade(INK, 1.0), 0.9, caps=False)
        x += w * scale
    img = cv.result()
    weave = np.asarray(Image.open(ink_fabric_path()).convert("L").resize((size, size))).astype(np.float32) / 255
    arr = np.asarray(img).astype(np.float32) * (0.3 + 0.7 * weave)[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), "T_DoorCurtain_Stripes")



def run():
    return [floor_tiles(), floor_tegel(), batik_kawung(), door_curtain_stripes()]


if __name__ == "__main__":
    for p in run():
        print(p)
