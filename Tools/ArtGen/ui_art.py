"""2D art for the UI: inked paper frames (9-slice), chat bubbles, buttons, the phone,
app and HUD icons, pixel-art caller portraits, wallet cards, board close-ups and the
newspaper photo. Everything lands in Assets/_Game/UI/Sprites; the Unity UI pipeline
sets the 9-slice borders listed in SLICES.

Run: python3 Tools/ArtGen/ui_art.py   (after tex_surfaces.py and tex_prints.py)
"""
import json
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

import atlas
import pixel_people as pp
import tex_prints
from texlib import OUT as TEX_OUT, ROOT, Canvas, fbm, halftone, hexc, mix, shade, wobble

UI_OUT = os.path.join(ROOT, "Assets", "_Game", "UI", "Sprites")

INK = hexc("2B2430")
PAPER = hexc("F2E4C4")
PAPER_LIGHT = hexc("F8EFDA")
PAPER_DARK = hexc("DCC7A0")
BLUE = hexc("5D7292")
BLUE_DARK = hexc("4A5C79")
GREEN = hexc("5E9E55")
SAGE = hexc("CFE0C0")
RED = hexc("C9483A")
AMBER = hexc("E3A53C")
TAN = hexc("C79D6B")
PHONE = hexc("2A2630")
SHADOW = (40, 26, 30, 120)

SLICES = {}   # sprite name -> (left, top, right, bottom) 9-slice border in pixels


def save(img, name, slices=None):
    os.makedirs(UI_OUT, exist_ok=True)
    path = os.path.join(UI_OUT, name + ".png")
    img.save(path, optimize=True)
    if slices:
        SLICES[name] = slices
    return path


def rrect(x0, y0, x1, y1, r, n=10):
    """Rounded rectangle outline points (clockwise from the top-left corner)."""
    pts = []
    for cx, cy, a0 in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
        for i in range(n + 1):
            a = math.radians(a0 + 90 * i / n)
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def paper_fill(w, h, base, seed, grain=0.06, fibres=True):
    """Tinted paper: mottled tone plus fine grain and a few fibres (RGB array)."""
    n = fbm(w, h, seed, octaves=((3, 0.5), (9, 0.3), (40, 0.2)))
    fine = np.random.default_rng(seed).random((h, w))
    arr = np.array(base, np.float32)[None, None, :] * (1 - grain + grain * 1.4 * n + 0.03 * (fine - 0.5))[..., None]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    if fibres:
        d = ImageDraw.Draw(img)
        rng = np.random.default_rng(seed + 1)
        for _ in range(int(w * h / 900)):
            x, y = rng.uniform(0, w), rng.uniform(0, h)
            a = rng.uniform(0, math.pi)
            ln = rng.uniform(3, 9)
            d.line([(x, y), (x + math.cos(a) * ln, y + math.sin(a) * ln)], fill=shade(base, 0.9), width=1)
    return img


def framed(w, h, fill, radius=14, ink_w=3.5, shadow=(6, 7), seed=0, inner_line=None, wob=0.9, fill_img=None, margin=10):
    """RGBA frame: hard offset shadow, filled rounded rectangle, wobbly ink outline."""
    ss = 3
    img = Image.new("RGBA", (w * ss, h * ss), (0, 0, 0, 0))
    x0, y0 = margin, margin
    x1, y1 = w - margin - shadow[0], h - margin - shadow[1]
    rng = np.random.default_rng(seed)

    def P(pts):
        return [(x * ss, y * ss) for x, y in pts]

    d = ImageDraw.Draw(img)
    if shadow != (0, 0):
        d.polygon(P([(x + shadow[0], y + shadow[1]) for x, y in rrect(x0, y0, x1, y1, radius)]), fill=SHADOW)
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).polygon(P(rrect(x0, y0, x1, y1, radius)), fill=255)
    if fill_img is None:
        fill_img = paper_fill(w, h, fill, seed + 5)
    fill_big = fill_img.resize(img.size, Image.BILINEAR).convert("RGBA")
    img.paste(fill_big, (0, 0), mask)
    d = ImageDraw.Draw(img)
    if inner_line:
        inset, col, width = inner_line
        pts = wobble(rrect(x0 + inset, y0 + inset, x1 - inset, y1 - inset, max(2, radius - inset * 0.6)), wob * 0.6, rng, step=3)
        d.line(P(pts + [pts[0]]), fill=col, width=int(width * ss), joint="curve")
    pts = wobble(rrect(x0, y0, x1, y1, radius), wob, rng, step=3)
    d.line(P(pts + [pts[0]]), fill=INK + (255,), width=int(ink_w * ss), joint="curve")
    return img.resize((w, h), Image.LANCZOS)


# ---------------------------------------------------------------- frames and buttons

def frames():
    save(framed(160, 160, PAPER, seed=1, inner_line=(7, PAPER_DARK + (255,), 1.5)), "frame_paper", (44, 44, 50, 50))
    save(framed(160, 160, PAPER_LIGHT, radius=10, ink_w=3, shadow=(4, 5), seed=2), "frame_card", (36, 36, 40, 40))
    blue = framed(160, 160, BLUE, radius=6, ink_w=2.5, shadow=(0, 0), seed=3, margin=4,
                  fill_img=paper_fill(160, 160, BLUE, 33, grain=0.1, fibres=False))
    save(blue, "frame_inset_blue", (22, 22, 22, 22))
    save(framed(160, 160, hexc("E9DCBD"), radius=6, ink_w=2.5, shadow=(0, 0), seed=4, margin=4), "frame_inset_paper", (22, 22, 22, 22))
    # Transcript frame: thick cream band between two ink lines, like references/UI/refUI1.
    t = framed(200, 200, PAPER, radius=8, ink_w=4, shadow=(7, 8), seed=5, inner_line=(16, INK + (255,), 2.5))
    save(t, "frame_transcript", (56, 56, 62, 62))
    save(framed(120, 90, hexc("E7D8B6"), radius=8, ink_w=3, shadow=(0, 0), seed=6, margin=4), "frame_input", (26, 26, 26, 26))


def button(name, fill, seed, pressed=False, shadow_col=None):
    w = h = 96
    ss = 3
    img = Image.new("RGBA", (w * ss, h * ss), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(seed)
    dy = 0 if pressed else 6
    x0, y0, x1, y1 = 6, 6 + (4 if pressed else 0), w - 6, h - 12 + (4 if pressed else 0)

    def P(pts):
        return [(x * ss, y * ss) for x, y in pts]

    sc = shadow_col or shade(fill, 0.62)
    if dy:
        base = rrect(x0, y0 + dy, x1, y1 + dy, 12)
        d.polygon(P(base), fill=sc + (255,))
        d.line(P(wobble(base, 0.6, rng, step=3) + [base[0]]), fill=INK + (255,), width=int(3.2 * ss), joint="curve")
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).polygon(P(rrect(x0, y0, x1, y1, 12)), fill=255)
    img.paste(paper_fill(w, h, fill, seed + 3, grain=0.05).resize(img.size).convert("RGBA"), (0, 0), mask)
    d = ImageDraw.Draw(img)
    hl = rrect(x0 + 6, y0 + 5, x1 - 6, y0 + 16, 5)
    d.polygon(P(hl), fill=mix(fill, (255, 255, 255), 0.3) + (255,))
    pts = wobble(rrect(x0, y0, x1, y1, 12), 0.6, rng, step=3)
    d.line(P(pts + [pts[0]]), fill=INK + (255,), width=int(3.2 * ss), joint="curve")
    save(img.resize((w, h), Image.LANCZOS), name, (26, 24, 26, 30))


def buttons():
    for name, col, seed in (("button", PAPER, 11), ("button_green", hexc("7FB06C"), 12), ("button_red", hexc("D6604F"), 13),
                            ("button_blue", hexc("7F95B5"), 14), ("button_amber", hexc("E9BD5E"), 15)):
        button(name, col, seed)
        button(name + "_down", col, seed, pressed=True)
    save(framed(96, 56, hexc("F4E0AE"), radius=16, ink_w=2.5, shadow=(3, 4), seed=16, margin=5), "chip", (22, 20, 24, 24))
    save(framed(96, 56, hexc("CFE0C0"), radius=16, ink_w=2.5, shadow=(3, 4), seed=17, margin=5), "chip_green", (22, 20, 24, 24))
    save(framed(96, 56, hexc("F3C8B6"), radius=16, ink_w=2.5, shadow=(3, 4), seed=18, margin=5), "chip_red", (22, 20, 24, 24))


def bubble(name, fill, tail_right, seed):
    """Speech bubble with its tail on the left edge near the top, inside an unstretched slice."""
    w, h = 180, 110
    ss = 3
    img = Image.new("RGBA", (w * ss, h * ss), (0, 0, 0, 0))
    rng = np.random.default_rng(seed)
    x0, y0, x1, y1 = 30, 6, w - 12, h - 12

    def P(pts):
        return [(x * ss, y * ss) for x, y in pts]

    body = rrect(x0, y0, x1, y1, 16)
    tl = 33                     # rrect: 11 points per corner; the top-left corner starts here
    shape = body[:tl] + [(x0, y0 + 42), (x0 - 22, y0 + 26), (x0, y0 + 20)] + body[tl:]
    d = ImageDraw.Draw(img)
    d.polygon(P([(x + 4, y + 5) for x, y in shape]), fill=SHADOW)
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).polygon(P(shape), fill=255)
    img.paste(paper_fill(w, h, fill, seed + 2, grain=0.05).resize(img.size).convert("RGBA"), (0, 0), mask)
    d = ImageDraw.Draw(img)
    outline = wobble(shape, 0.6, rng, step=3)
    d.line(P(outline + [outline[0]]), fill=INK + (255,), width=int(3 * ss), joint="curve")
    out = img.resize((w, h), Image.LANCZOS)
    if tail_right:
        save(out.transpose(Image.FLIP_LEFT_RIGHT), name, (26, 52, 48, 28))
    else:
        save(out, name, (48, 52, 26, 28))


def bubbles():
    bubble("bubble_in", hexc("F4E7C8"), False, 21)
    bubble("bubble_out", SAGE, True, 22)


# ---------------------------------------------------------------- phone, slider, call buttons

def phone_frame():
    """Phone body with a transparent screen; the UI draws the screen underneath."""
    w, h = 520, 1040
    ss = 2
    img = Image.new("RGBA", (w * ss, h * ss), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(31)

    def P(pts):
        return [(x * ss, y * ss) for x, y in pts]

    body = rrect(14, 12, w - 22, h - 24, 62, n=16)
    d.polygon(P([(x + 8, y + 10) for x, y in body]), fill=SHADOW)
    d.polygon(P(body), fill=TAN + (255,))
    inner = rrect(26, 24, w - 34, h - 36, 52, n=16)
    d.polygon(P(inner), fill=PHONE + (255,))
    screen = rrect(40, 40, w - 48, h - 52, 40, n=16)
    d.polygon(P(screen), fill=(0, 0, 0, 0))
    # Knock the screen out to transparency.
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).polygon(P(screen), fill=255)
    img.putalpha(Image.fromarray(np.minimum(np.asarray(img.split()[3]), 255 - np.asarray(mask)).astype(np.uint8)))
    d = ImageDraw.Draw(img)
    notch = rrect(w / 2 - 70, 34, w / 2 + 62, 66, 14)
    d.polygon(P(notch), fill=PHONE + (255,))
    d.ellipse([(w / 2 + 36) * ss, 44 * ss, (w / 2 + 48) * ss, 56 * ss], fill=hexc("3C3A48") + (255,))
    for y0, y1 in ((190, 270), (300, 380)):
        side = rrect(4, y0, 16, y1, 5)
        d.polygon(P(side), fill=shade(TAN, 0.85) + (255,))
        d.line(P(side + [side[0]]), fill=INK + (255,), width=int(2.5 * ss))
    # A thin highlight along the top of the tan ring, following its corners (it used to be a
    # straight band that covered the dark bezel and stuck out past the outline).
    hl = Image.new("L", img.size, 0)
    hd = ImageDraw.Draw(hl)
    hd.polygon(P(rrect(17, 15, w - 25, h - 27, 59, n=16)), fill=255)
    hd.polygon(P(rrect(22, 20, w - 30, h - 32, 55, n=16)), fill=0)
    hd.rectangle([0, 150 * ss, w * ss, h * ss], fill=0)
    img.paste(Image.new("RGBA", img.size, mix(TAN, (255, 255, 255), 0.3) + (255,)), (0, 0), hl)
    d = ImageDraw.Draw(img)
    pts = wobble(body, 0.8, rng, step=4)
    d.line(P(pts + [pts[0]]), fill=INK + (255,), width=int(5 * ss), joint="curve")
    d.line(P(screen + [screen[0]]), fill=hexc("121015") + (255,), width=int(3 * ss), joint="curve")
    save(img.resize((w, h), Image.LANCZOS), "phone_frame")


SKIN = hexc("E5BD9C")
SKIN_SHADE = hexc("CDA081")
NAIL = hexc("F3DACB")
SLEEVE = hexc("C79A45")


def limb(cx, cy, length, thick, angle, n=12):
    """Outline of a capsule: its centre, the distance between the cap centres, thickness, angle in degrees."""
    a = math.radians(angle)
    ux, uy = math.cos(a), math.sin(a)
    r = thick / 2
    pts = []
    for i in range(n + 1):
        t = a - math.pi / 2 + math.pi * i / n
        pts.append((cx + ux * length / 2 + math.cos(t) * r, cy + uy * length / 2 + math.sin(t) * r))
    for i in range(n + 1):
        t = a + math.pi / 2 + math.pi * i / n
        pts.append((cx - ux * length / 2 + math.cos(t) * r, cy - uy * length / 2 + math.sin(t) * r))
    return pts


def phone_hand():
    """Jiwoo's right hand holding the phone, as two pictures in one 660 x 1180 box (the phone's
    440 x 880 box starts 110 px in): hand_back goes behind the phone (palm, wrist, sweater
    sleeve), hand_front over it (four fingertips round the left edge, the thumb on the right)."""
    w, h, ox = 660, 1180, 110
    K = INK + (255,)

    def finger(cv, cx, cy, length, thick, angle, nail=True):
        cv.poly(limb(cx, cy, length, thick, angle), fill=SKIN_SHADE + (255,))
        cv.poly(limb(cx - 1, cy - 4, length - 4, thick - 9, angle), fill=SKIN + (255,))
        cv.poly(limb(cx, cy, length, thick, angle), outline=K, width=5)
        if nail:
            a = math.radians(angle)
            nx, ny = cx + math.cos(a) * (length / 2 + thick * 0.08), cy + math.sin(a) * (length / 2 + thick * 0.08)
            cv.poly(limb(nx, ny - 2, thick * 0.16, thick * 0.5, angle), fill=NAIL + (255,), outline=K, width=3)

    # ---- behind the phone
    cv = Canvas(w, h, ss=2, mode="RGBA", bg=(0, 0, 0, 0))
    arm = [(ox + 150, 930), (ox + 372, 915), (ox + 470, h + 20), (ox + 196, h + 20)]
    cv.poly(arm, fill=SKIN + (255,), outline=K, width=5)
    palm = rrect(ox + 44, 600, ox + 452, 1004, 118, n=14)
    cv.poly(palm, fill=SKIN_SHADE + (255,))
    cv.poly([(x - 2, y - 7) for x, y in rrect(ox + 50, 600, ox + 446, 1000, 114, n=14)], fill=SKIN + (255,))
    cv.poly(palm, outline=K, width=5)
    # The crease where the thumb's base folds over the palm.
    cv.line([(ox + 300, 900), (ox + 352, 946), (ox + 398, 968)], SKIN_SHADE + (255,), 5)
    sleeve = [(ox + 132, 1040), (ox + 398, 1014), (ox + 500, h + 20), (ox + 170, h + 20)]
    cv.poly(sleeve, fill=SLEEVE + (255,), outline=K, width=5)
    cuff = [(ox + 124, 1022), (ox + 402, 994), (ox + 414, 1040), (ox + 136, 1068)]
    cv.poly(cuff, fill=shade(SLEEVE, 0.86) + (255,), outline=K, width=5)
    for k in range(1, 9):
        t = k / 9
        x0, y0 = ox + 124 + (402 - 124) * t, 1022 + (994 - 1022) * t
        cv.line([(x0, y0 + 3), (x0 + 11, y0 + 43)], shade(SLEEVE, 0.68) + (255,), 3)
    save(cv.result(), "hand_back")

    # ---- over the phone
    cv = Canvas(w, h, ss=2, mode="RGBA", bg=(0, 0, 0, 0))
    for y, length, thick in ((494, 34, 56), (562, 40, 58), (632, 36, 56), (698, 26, 50)):
        finger(cv, ox - 14 + length / 2, y, length, thick, 4)
    # The thumb lies along the right edge, its pad on the bezel.
    finger(cv, ox + 432, 690, 112, 66, -104)
    save(cv.result(), "hand_front")


def call_buttons():
    for name, col in (("circle_green", GREEN), ("circle_red", RED), ("circle_dark", hexc("4B4652"))):
        s = 160
        cv = Canvas(s, s, ss=3, mode="RGBA", bg=(0, 0, 0, 0))
        cv.ellipse(s / 2 + 5, s / 2 + 7, 64, 64, fill=SHADOW)
        cv.ellipse(s / 2, s / 2, 64, 64, fill=col + (255,))
        cv.ellipse(s / 2 - 14, s / 2 - 22, 36, 18, fill=mix(col, (255, 255, 255), 0.3) + (255,), rot=-0.5)
        cv.ellipse(s / 2, s / 2, 64, 64, outline=INK + (255,), width=4.5)
        save(cv.result(), name)
    w, h = 400, 104
    track = framed(w, h, hexc("1F1C24"), radius=46, ink_w=3.5, shadow=(0, 0), seed=41, margin=6,
                   fill_img=Image.new("RGB", (w, h), hexc("1F1C24")))
    arr = np.asarray(track).copy()
    arr[..., 3] = (arr[..., 3].astype(np.float32) * 0.9).astype(np.uint8)
    save(Image.fromarray(arr), "slider_track", (52, 50, 52, 50))


def vignette():
    w, h = 480, 270
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    a = np.clip((r - 0.55) / 0.6, 0, 1) ** 1.6
    rgba = np.zeros((h, w, 4), np.float32)
    rgba[..., 0], rgba[..., 1], rgba[..., 2] = 70, 8, 12
    rgba[..., 3] = a * 235
    save(Image.fromarray(rgba.astype(np.uint8), "RGBA"), "vignette")


# ---------------------------------------------------------------- icons

def glyph_canvas(s=128):
    return Canvas(s, s, ss=4, mode="RGBA", bg=(0, 0, 0, 0))


def handset(cv, cx, cy, sc, col, rot=0.0):
    """Classic phone handset glyph."""
    ca, sa = math.cos(rot), math.sin(rot)

    def T(x, y):
        return cx + (x * ca - y * sa) * sc, cy + (x * sa + y * ca) * sc

    pts = [T(x, y) for x, y in ((-1.0, -0.55), (-0.62, -0.95), (-0.35, -0.65), (-0.52, -0.38), (0.38, 0.52), (0.65, 0.35),
                                (0.95, 0.62), (0.55, 1.0), (-0.1, 0.85), (-0.85, 0.1))]
    cv.poly(pts, fill=col)


def app_tile(name, bg, draw_glyph):
    s = 128
    cv = glyph_canvas(s)
    body = rrect(10, 10, s - 16, s - 16, 26)
    cv.poly([(x + 5, y + 6) for x, y in body], fill=SHADOW)
    cv.poly(body, fill=bg + (255,))
    cv.poly(rrect(18, 14, s - 24, 34, 12), fill=mix(bg, (255, 255, 255), 0.22) + (255,))
    draw_glyph(cv, (s - 6) / 2, (s - 6) / 2)
    cv.line(body + [body[0]], INK + (255,), 4.5)
    save(cv.result(), name)


def icons():
    W = (250, 244, 232, 255)
    K = INK + (255,)

    def g_contacts(cv, cx, cy):
        cv.ellipse(cx, cy - 14, 16, 17, fill=W)
        cv.poly([(cx - 30, cy + 34), (cx - 26, cy + 10), (cx, cy + 2), (cx + 26, cy + 10), (cx + 30, cy + 34)], fill=W)

    def g_talk(cv, cx, cy):
        cv.ellipse(cx, cy - 4, 34, 27, fill=hexc("3A2E22") + (255,))
        cv.poly([(cx - 14, cy + 16), (cx - 22, cy + 32), (cx + 2, cy + 20)], fill=hexc("3A2E22") + (255,))
        cv.text((cx, cy - 4), "TALK", cv.font("Arial Black.ttf", 15), hexc("F4D24A") + (255,), anchor="mm")

    def g_bank(cv, cx, cy):
        cv.poly([(cx - 34, cy - 10), (cx, cy - 34), (cx + 34, cy - 10)], fill=W)
        for k in range(4):
            x = cx - 24 + k * 16
            cv.rect(x - 4, cy - 6, x + 4, cy + 22, fill=W)
        cv.rect(cx - 36, cy + 24, cx + 36, cy + 32, fill=W)
        cv.text((cx, cy - 16), "N", cv.font("Arial Black.ttf", 16), hexc("2F7A6A") + (255,), anchor="mm")

    def g_check(cv, cx, cy):
        shield = [(cx - 26, cy - 26), (cx, cy - 34), (cx + 26, cy - 26), (cx + 24, cy + 4), (cx, cy + 32), (cx - 24, cy + 4)]
        cv.poly(shield, fill=W)
        cv.ellipse(cx - 2, cy - 4, 12, 12, outline=hexc("B8453A") + (255,), width=5)
        cv.line([(cx + 7, cy + 5), (cx + 17, cy + 15)], hexc("B8453A") + (255,), 6)

    app_tile("app_contacts", hexc("D9853B"), g_contacts)
    app_tile("app_talk", hexc("F2CB3A"), g_talk)
    app_tile("app_bank", hexc("2F7A6A"), g_bank)
    app_tile("app_checkfirst", hexc("B8453A"), g_check)

    # Line glyphs for buttons, drawn in ink on transparent.
    def glyph(name, draw, s=96):
        cv = Canvas(s, s, ss=4, mode="RGBA", bg=(0, 0, 0, 0))
        draw(cv, s / 2, s / 2)
        save(cv.result(), name)

    lw = 7
    glyph("ic_back", lambda cv, x, y: cv.line([(x + 14, y - 24), (x - 14, y), (x + 14, y + 24)], K, lw))
    glyph("ic_close", lambda cv, x, y: (cv.line([(x - 20, y - 20), (x + 20, y + 20)], K, lw), cv.line([(x + 20, y - 20), (x - 20, y + 20)], K, lw)))
    glyph("ic_copy", lambda cv, x, y: (cv.poly(rrect(x - 22, y - 26, x + 10, y + 12, 5), outline=K, width=5),
                                        cv.poly(rrect(x - 10, y - 12, x + 22, y + 26, 5), fill=(250, 244, 232, 255), outline=K, width=5)))
    glyph("ic_send", lambda cv, x, y: cv.poly([(x - 24, y - 22), (x + 26, y), (x - 24, y + 22), (x - 14, y)], fill=hexc("4A5C9E") + (255,)))
    glyph("ic_hangup", lambda cv, x, y: handset(cv, x, y + 4, 26, (250, 244, 232, 255), rot=2.36))
    glyph("ic_answer", lambda cv, x, y: handset(cv, x, y, 26, (250, 244, 232, 255), rot=0.0))
    glyph("ic_speaker", lambda cv, x, y: (cv.poly([(x - 22, y - 8), (x - 10, y - 8), (x + 4, y - 22), (x + 4, y + 22), (x - 10, y + 8),
                                                   (x - 22, y + 8)], fill=(250, 244, 232, 255)),
                                           cv.line([(x + 12, y - 12), (x + 18, y), (x + 12, y + 12)], (250, 244, 232, 255), 4)))
    glyph("ic_apps", lambda cv, x, y: [cv.poly(rrect(x - 24 + (i % 2) * 28, y - 24 + (i // 2) * 28, x - 4 + (i % 2) * 28, y - 4 + (i // 2) * 28, 5),
                                               fill=(250, 244, 232, 255)) for i in range(4)])
    glyph("ic_phone", lambda cv, x, y: (cv.poly(rrect(x - 18, y - 32, x + 18, y + 32, 7), fill=hexc("8FB6DB") + (255,), outline=K, width=5),
                                         cv.line([(x - 6, y + 24), (x + 6, y + 24)], K, 4)))
    glyph("ic_star", lambda cv, x, y: cv.poly([(x + 26 * math.cos(-math.pi / 2 + k * math.pi / 5) * (1 if k % 2 == 0 else 0.45),
                                                y + 26 * math.sin(-math.pi / 2 + k * math.pi / 5) * (1 if k % 2 == 0 else 0.45)) for k in range(10)],
                                              fill=hexc("E3A53C") + (255,), outline=K, width=4))
    glyph("ic_lock", lambda cv, x, y: (cv.poly(rrect(x - 22, y - 4, x + 22, y + 28, 5), fill=hexc("C99A5C") + (255,), outline=K, width=5),
                                        cv.line([(x - 12, y - 4), (x - 12, y - 16), (x - 4, y - 26), (x + 4, y - 26), (x + 12, y - 16), (x + 12, y - 4)], K, 6)))


# ---------------------------------------------------------------- menus and title

def menu_glyphs():
    """Ink glyphs for the pause menu, settings rows, the day and case cards and the summary."""
    K = INK + (255,)
    P = (250, 244, 232, 255)
    R = hexc("C9483A") + (255,)
    G = hexc("4F8F47") + (255,)

    def glyph(name, draw, s=96):
        cv = Canvas(s, s, ss=4, mode="RGBA", bg=(0, 0, 0, 0))
        draw(cv, s / 2, s / 2)
        save(cv.result(), name)

    def music(cv, x, y):
        cv.line([(x - 12, y + 18), (x - 12, y - 22), (x + 20, y - 30), (x + 20, y + 10)], K, 6)
        cv.line([(x - 12, y - 10), (x + 20, y - 18)], K, 6)
        cv.ellipse(x - 20, y + 20, 10, 8, fill=K, rot=-0.4)
        cv.ellipse(x + 12, y + 12, 10, 8, fill=K, rot=-0.4)

    def speaker(cv, x, y):
        cv.poly([(x - 28, y - 9), (x - 15, y - 9), (x - 1, y - 24), (x - 1, y + 24), (x - 15, y + 9), (x - 28, y + 9)], fill=K)
        for r in (12, 22):
            pts = [(x + 2 + r * math.cos(a), y + r * math.sin(a)) for a in [(-0.9 + 1.8 * i / 12) for i in range(13)]]
            cv.line(pts, K, 5)

    def mic(cv, x, y):
        cv.poly(rrect(x - 10, y - 30, x + 10, y + 6, 10), fill=K)
        pts = [(x + 18 * math.cos(a), y - 4 + 18 * math.sin(a)) for a in [(0.1 + 2.94 * i / 14) for i in range(15)]]
        cv.line(pts, K, 5)
        cv.line([(x, y + 14), (x, y + 26)], K, 5)
        cv.line([(x - 12, y + 27), (x + 12, y + 27)], K, 5)

    def mouse(cv, x, y):
        cv.poly(rrect(x - 16, y - 26, x + 16, y + 26, 15), outline=K, width=5)
        cv.line([(x, y - 26), (x, y - 8)], K, 4)
        cv.line([(x - 16, y - 8), (x + 16, y - 8)], K, 4)
        cv.line([(x - 34, y), (x - 24, y - 7)], K, 4)
        cv.line([(x - 34, y), (x - 24, y + 7)], K, 4)
        cv.line([(x + 34, y), (x + 24, y - 7)], K, 4)
        cv.line([(x + 34, y), (x + 24, y + 7)], K, 4)

    def clock(cv, x, y):
        cv.ellipse(x, y, 30, 30, fill=P, outline=K, width=6)
        cv.line([(x, y - 18), (x, y), (x + 13, y + 8)], K, 6)

    def pause(cv, x, y):
        cv.poly(rrect(x - 20, y - 26, x - 6, y + 26, 4), fill=K)
        cv.poly(rrect(x + 6, y - 26, x + 20, y + 26, 4), fill=K)

    def play(cv, x, y):
        cv.poly([(x - 16, y - 26), (x + 24, y), (x - 16, y + 26)], fill=K)

    def gear(cv, x, y):
        teeth = []
        for i in range(16):
            a = i * math.pi / 8
            r = 31 if i % 2 == 0 else 23
            teeth.append((x + r * math.cos(a - 0.12), y + r * math.sin(a - 0.12)))
            teeth.append((x + r * math.cos(a + 0.12), y + r * math.sin(a + 0.12)))
        cv.poly(teeth, fill=K)
        cv.ellipse(x, y, 10, 10, fill=P)

    def exit_door(cv, x, y):
        cv.line([(x - 4, y - 28), (x - 26, y - 28), (x - 26, y + 28), (x - 4, y + 28)], K, 6)
        cv.line([(x - 12, y), (x + 26, y)], K, 6)
        cv.line([(x + 14, y - 12), (x + 26, y), (x + 14, y + 12)], K, 6)

    def check(cv, x, y):
        cv.line([(x - 24, y + 2), (x - 8, y + 20), (x + 26, y - 20)], G, 10)

    def cross(cv, x, y):
        cv.line([(x - 20, y - 20), (x + 20, y + 20)], R, 10)
        cv.line([(x + 20, y - 20), (x - 20, y + 20)], R, 10)

    def case(cv, x, y):
        cv.poly([(x - 32, y - 20), (x - 12, y - 20), (x - 6, y - 12), (x + 32, y - 12), (x + 32, y + 26), (x - 32, y + 26)],
                fill=hexc("C99A5C") + (255,), outline=K, width=5)
        cv.line([(x - 32, y - 4), (x + 32, y - 4)], K, 4)

    def globe(cv, x, y):
        cv.ellipse(x, y, 29, 29, outline=K, width=5)
        cv.ellipse(x, y, 12, 29, outline=K, width=4)
        cv.line([(x - 29, y), (x + 29, y)], K, 4)
        for dy in (-15, 15):
            half = math.sqrt(29 ** 2 - dy ** 2) - 3
            cv.line([(x - half, y + dy), (x + half, y + dy)], K, 3)

    for name, fn in (("ic_music", music), ("ic_sfx", speaker), ("ic_voice", mic), ("ic_mouse", mouse), ("ic_clock", clock),
                     ("ic_pause", pause), ("ic_play", play), ("ic_gear", gear), ("ic_exit", exit_door), ("ic_check", check),
                     ("ic_cross", cross), ("ic_case", case), ("ic_globe", globe)):
        glyph(name, fn)


def gradients():
    """Left-to-right shade behind the title menu, so text stays readable over the room."""
    w, h = 512, 8
    x = np.linspace(0, 1, w)
    alpha = (0.82 * (1 - x) ** 1.6 * 255).astype(np.uint8)
    img = np.zeros((h, w, 4), np.uint8)
    img[..., :3] = (20, 14, 22)
    img[..., 3] = alpha[None, :]
    save(Image.fromarray(img, "RGBA"), "grad_left")


def title_logo():
    """The title, DON'T / CALL ME!, as a chunky inked sticker: cream letters with a thick ink line,
    a red offset shadow with halftone dots, and a ringing red handset jumping off the corner."""
    ss = 2
    font_file = os.path.join(ROOT, "Assets", "_Game", "UI", "Fonts", "DoHyeon-Regular.ttf")
    cream, red, dark_red = (248, 239, 218, 255), hexc("C9483A") + (255,), hexc("8E2E24") + (255,)
    ink = INK + (255,)
    stroke = 13 * ss
    shadow = (24 * ss, 26 * ss)

    def word(text, size, angle, face=None, fill=None, shadow_fill=None):
        font = ImageFont.truetype(face or font_file, size * ss)
        probe = ImageDraw.Draw(Image.new("L", (8, 8)))
        x0, y0, x1, y1 = probe.textbbox((0, 0), text, font=font, stroke_width=stroke)
        pad = 30 * ss
        w, h = x1 - x0 + 2 * pad + shadow[0], y1 - y0 + 2 * pad + shadow[1]
        at = (pad - x0, pad - y0)
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        sx, sy = at[0] + shadow[0], at[1] + shadow[1]
        d.text((sx, sy), text, font=font, fill=shadow_fill or red, stroke_width=stroke, stroke_fill=ink)
        # Halftone dots on the part of the shadow the letters leave visible, denser towards the bottom.
        shadow_fill = Image.new("L", (w, h), 0)
        ImageDraw.Draw(shadow_fill).text((sx, sy), text, font=font, fill=255)
        front = Image.new("L", (w, h), 0)
        ImageDraw.Draw(front).text(at, text, font=font, fill=255, stroke_width=stroke)
        dots = Image.new("L", (w, h), 0)
        dd = ImageDraw.Draw(dots)
        step = 11 * ss
        for yy in range(0, h, step):
            for xx in range(((yy // step) % 2) * step // 2, w, step):
                r = (1.4 + 3.0 * (yy / h)) * ss
                dd.ellipse([xx - r, yy - r, xx + r, yy + r], fill=255)
        mask = np.minimum(np.asarray(shadow_fill), np.asarray(dots)).astype(np.float32)
        mask *= 1.0 - np.asarray(front, dtype=np.float32) / 255.0
        dot_layer = Image.new("RGBA", (w, h), dark_red)
        dot_layer.putalpha(Image.fromarray(mask.astype(np.uint8)))
        img.alpha_composite(dot_layer)
        d.text(at, text, font=font, fill=fill or cream, stroke_width=stroke, stroke_fill=ink)
        return img.rotate(angle, resample=Image.BICUBIC, expand=True)

    top = word("DON'T", 205, 4)
    call = word("CALL", 300, -3)
    me = word("ME!", 300, -5)
    gap = -95 * ss
    bottom_w = call.width + gap + me.width
    # A retro desk telephone (the ☎ dingbat) ringing off the corner; a drawn handset if the font is missing.
    phone_face = next((f for f in ("/System/Library/Fonts/Supplemental/ZapfDingbats.ttf", "/System/Library/Fonts/Apple Symbols.ttf")
                       if os.path.exists(f)), None)
    phone = word("\u260E", 150, 14, face=phone_face, fill=red, shadow_fill=INK + (255,)) if phone_face else None
    W = max(top.width + (phone.width if phone else 0) + 40 * ss, bottom_w + 60 * ss)
    H = top.height + max(call.height, me.height) - 95 * ss
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.alpha_composite(top, (20 * ss, 30 * ss))
    y = top.height - 75 * ss
    x = (W - bottom_w) // 2 + 30 * ss
    out.alpha_composite(call, (x, y))
    out.alpha_composite(me, (x + call.width + gap, y - 18 * ss))
    cv = Canvas(W // ss, H // ss, ss=ss, mode="RGBA", bg=(0, 0, 0, 0))
    if phone is not None:
        px, py = top.width + 10 * ss, 0
        out.alpha_composite(phone, (px, py))
        hx, hy = (px + phone.width * 0.48) / ss, (py + phone.height * 0.5) / ss
    else:
        hx, hy = top.width // ss + 95, top.height // ss * 0.5
        handset(cv, hx, hy, 70, red, rot=-0.5)
    # Ringing lines on both sides.
    for r in (88, 112):
        for base in (-0.95, 2.35):
            pts = [(hx + r * math.cos(base + 0.62 * i / 10), hy + r * math.sin(base + 0.62 * i / 10)) for i in range(11)]
            cv.line(pts, ink, 9)
    out.alpha_composite(cv.img.resize((W, H)))
    out = out.crop(out.getbbox())
    out = out.resize((out.width // ss, out.height // ss), Image.LANCZOS)
    save(out, "title_logo")


# ---------------------------------------------------------------- portraits

BG_POOL = {"blue": ("5E6F8E", "3E4B63"), "teal": ("4E7A74", "30524F"), "rose": ("B98A86", "8A5F5E"), "sand": ("C9B08A", "9A8060"),
           "olive": ("7E8A5E", "59623F"), "dusk": ("5C5270", "3A3448")}

PORTRAITS = {
    "pt_jiwoo": (pp.JIWOO, "rose"),
    "pt_mom": (pp.MOM, "sand"),
    "pt_dad": (pp.DAD, "blue"),
    "pt_minjun": (dict(sex="m", skin="E0B592", hair="221A16", style="short", outfit="sweater", cloth="6F8A5A", expression="smile"), "olive"),
    "pt_yuna": (dict(sex="f", skin="E2B794", hair="4A2E22", style="long", outfit="sweater", cloth="7FA0B8", expression="smile"), "rose"),
    "pt_landlord": (dict(sex="m", skin="D2A07A", hair="B7B2AA", style="short", outfit="cardigan", cloth="7A5A40", inner="E6DCC8",
                         age="old", expression="smile", glasses=True), "sand"),
    "pt_hyunwoo": (dict(sex="m", skin="D8A57E", hair="231C18", style="short", outfit="shirt", cloth="9AAFC7", stubble=True), "blue"),
    "pt_cafe": (dict(sex="m", skin="D9A982", hair="2B211B", style="short", outfit="shirt", cloth="3E4B5E", glasses=True), "sand"),
    "pt_jeon": (dict(sex="m", skin="D6A47E", hair="1F1A18", style="short", outfit="blazer", cloth="2F3A52", expression="neutral",
                     glasses=True), "dusk"),
    "pt_hr": (dict(sex="f", skin="E6BE9E", hair="3A2A22", style="long", outfit="blazer", cloth="B89A72", expression="smile"), "teal"),
}


def portraits():
    for name, (spec, bg) in PORTRAITS.items():
        top, bottom = BG_POOL[bg]

        def draw(p, spec=spec, top=top, bottom=bottom, name=name):
            pp.backdrop_studio(p, top, bottom, spot=0.12)
            pp.person(p, p.w * 0.5, p.h * 0.42, p.h * 0.2, spec, seed=sum(map(ord, name)) % 97)

        img = pp.render(draw, (48, 48), (192, 192), colors=40, seed=7, vignette=0.12)
        save(img, name)
    # Unknown caller: a dark silhouette on a grey card.
    s = 192
    img = Image.new("RGB", (48, 48), hexc("4B4652"))
    d = ImageDraw.Draw(img)
    d.ellipse([16, 9, 32, 27], fill=hexc("2A2630"))
    d.polygon([(8, 48), (11, 36), (18, 31), (30, 31), (37, 36), (40, 48)], fill=hexc("2A2630"))
    d.text((21, 13), "?", fill=hexc("8C8698"))
    save(img.resize((s, s), Image.NEAREST), "pt_unknown")
    # Notice channels in Chats: the courier (a parcel on navy) and the shop (a star on orange).
    cv = Canvas(s, s, ss=3, bg=hexc("2F4A6E"))
    cv.poly(rrect(48, 62, 144, 140, 8), fill=hexc("D9A766"), outline=INK, width=5)
    cv.line([(48, 92), (144, 92)], INK, 4)
    cv.rect(86, 62, 106, 104, fill=hexc("F4EAD2"), outline=INK, width=3)
    save(cv.result(), "av_courier")
    cv = Canvas(s, s, ss=3, bg=hexc("E0703A"))
    star = [(96 + 58 * math.cos(-math.pi / 2 + k * math.pi / 5) * (1 if k % 2 == 0 else 0.45),
             100 + 58 * math.sin(-math.pi / 2 + k * math.pi / 5) * (1 if k % 2 == 0 else 0.45)) for k in range(10)]
    cv.poly(star, fill=hexc("FBF4E4"), outline=INK, width=5)
    save(cv.result(), "av_shop")


# ---------------------------------------------------------------- documents and the calendar

def crop_atlas(tex, key, atlas_name, out_name, scale=1.0):
    return crop_image(Image.open(os.path.join(TEX_OUT, tex + ".png")).convert("RGB"), key, atlas_name, out_name, scale)


def crop_image(img, key, atlas_name, out_name, scale=1.0):
    x0, y0, x1, y1 = atlas.ATLASES[atlas_name][key]
    part = img.crop((x0, y0, x1, y1))
    if scale != 1.0:
        part = part.resize((int(part.width * scale), int(part.height * scale)), Image.LANCZOS)
    return save(part, out_name)


def documents():
    crop_atlas("T_Notes_Atlas", "receipt", "notes", "doc_receipt")
    crop_atlas("T_Photos_Atlas", "family", "photos", "photo_family")
    cal = Image.open(os.path.join(TEX_OUT, "T_Calendar.png")).convert("RGB")
    save(cal, "board_calendar")
    # The same close-ups written in Korean; ContentBuilder picks "<name>_ko" for the Korean days.
    crop_image(tex_prints.notes_image("ko"), "receipt", "notes", "doc_receipt_ko")
    save(tex_prints.calendar_image(lang="ko"), "board_calendar_ko")

    # Stamps.
    for name, text, col in (("stamp_paid", "PAID", hexc("B8453A")), ("stamp_overdue", "OVERDUE", hexc("B8453A"))):
        cv = Canvas(300, 130, ss=3, mode="RGBA", bg=(0, 0, 0, 0))
        cv.poly(rrect(8, 8, 292, 122, 16), outline=col + (230,), width=7)
        cv.poly(rrect(20, 20, 280, 110, 10), outline=col + (200,), width=3)
        cv.text((150, 66), text, cv.font("Arial Black.ttf", 52 if len(text) < 6 else 40), col + (230,), anchor="mm")
        img = cv.result()
        arr = np.asarray(img).astype(np.float32)
        arr[..., 3] *= 0.75 + 0.25 * np.random.default_rng(3).random(arr.shape[:2])
        save(Image.fromarray(arr.astype(np.uint8), "RGBA").rotate(-8, expand=True, resample=Image.BICUBIC), name)

    # Newspaper photo, the same one printed on the desk's paper.
    from tex_prints import NEWS_INK, PAPER as NEWS_PAPER, phone_transfer_photo
    save(halftone(phone_transfer_photo(600, 380), cell=4, dark=NEWS_INK, light=NEWS_PAPER), "news_photo")


# ---------------------------------------------------------------- tiles

def tiles():
    wood = Image.open(os.path.join(TEX_OUT, "T_Floor_Vinyl.png")).convert("RGB").crop((0, 0, 512, 512)).resize((256, 256), Image.LANCZOS)
    save(wood, "tile_wood")


# ---------------------------------------------------------------- phone wallpaper

def wallpaper():
    """Pixel-art dusk over the Han river: the phone's home screen."""
    gw, gh = 92, 200
    img = Image.new("RGB", (gw, gh))
    px = img.load()
    top, mid, low = np.array(hexc("2E2F52"), float), np.array(hexc("B8657A"), float), np.array(hexc("F0B27A"), float)
    for y in range(gh):
        t = y / (gh * 0.62)
        c = top + (mid - top) * min(t, 1) * 1.0 if t < 1 else mid + (low - mid) * min((t - 1) * 3, 1)
        for x in range(gw):
            d = ((x * 7 + y * 13) % 5) / 40.0     # ordered dither
            px[x, y] = tuple(int(v * (0.97 + d)) for v in c)
    rng = np.random.default_rng(5)
    for _ in range(40):
        x, y = int(rng.integers(0, gw)), int(rng.integers(0, int(gh * 0.35)))
        px[x, y] = (250, 240, 220)
    d = ImageDraw.Draw(img)
    # Top left, clear of the home clock and date and of the caller's portrait on call screens.
    d.ellipse([6, 9, 14, 17], fill=hexc("F8E6C0"))
    ridge = [(0, 128), (14, 118), (26, 122), (40, 108), (52, 116), (66, 104), (80, 114), (92, 110), (92, 140), (0, 140)]
    d.polygon(ridge, fill=hexc("4A3F5E"))
    d.rectangle([44, 96, 46, 124], fill=hexc("3A3150"))
    d.rectangle([42, 94, 48, 97], fill=hexc("3A3150"))
    d.point((45, 92), fill=hexc("F4D27A"))
    for k in range(14):
        x0 = k * 7 + int(rng.integers(0, 3))
        h = int(rng.integers(10, 26))
        d.rectangle([x0, 140 - h, x0 + 6, 140], fill=hexc("2F2A44"))
        for wy in range(140 - h + 2, 139, 3):
            for wx in (x0 + 1, x0 + 4):
                if rng.random() < 0.45:
                    d.point((wx, wy), fill=hexc("F4C66A"))
    d.rectangle([0, 140, gw, gh], fill=hexc("35406A"))
    for y in range(142, gh, 3):
        for x in range(0, gw, 2):
            if rng.random() < 0.18:
                d.point((x, y), fill=hexc("F0B27A") if y < 160 else hexc("8A8FB5"))
    d.line([(0, 150), (92, 146)], fill=hexc("2A2640"), width=2)
    for x in range(4, 92, 10):
        d.line([(x, 146), (x, 152)], fill=hexc("2A2640"))
        d.point((x, 145), fill=hexc("F4D27A"))
    save(img.resize((gw * 5, gh * 5), Image.NEAREST), "wallpaper")


def run():
    frames()
    buttons()
    bubbles()
    phone_frame()
    phone_hand()
    call_buttons()
    vignette()
    icons()
    portraits()
    documents()
    tiles()
    wallpaper()
    menu_glyphs()
    gradients()
    title_logo()
    with open(os.path.join(UI_OUT, "slices.json"), "w") as f:
        json.dump({"slices": [{"name": k, "border": list(v)} for k, v in sorted(SLICES.items())]}, f, indent=1)
    return UI_OUT


if __name__ == "__main__":
    print(run())
