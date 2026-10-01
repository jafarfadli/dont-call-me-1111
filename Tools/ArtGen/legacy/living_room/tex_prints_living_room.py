"""Printed and illustrated textures: newspaper, calendar, notes, photos, painting,
labels, clock, TV, books, rug, doily, leaves and the street backdrop."""
import os
import sys

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

import datetime
import math

import numpy as np
from PIL import Image

from texlib import (INK, Canvas, fbm, halftone, hexc, mix, multiply, resample, save, shade, wobble)

PAPER = hexc("EEE5CF")
NEWS_INK = (42, 38, 44)
BLUE_PEN = hexc("2C4A9A")
RED_PEN = hexc("C23B32")

rng_global = np.random.default_rng(7)


def paper_tone(img, seed, amount=0.08):
    w, h = img.size
    n = fbm(w, h, seed, octaves=((3, 0.5), (8, 0.3), (24, 0.2)))
    return multiply(img, 1.0 - amount * n)


def wrap_words(cv, text, font, width):
    words = text.split()
    lines, cur = [], ""
    for w in words:
        t = (cur + " " + w).strip()
        if cv.text_width(t, font) <= width:
            cur = t
        else:
            lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def text_block(cv, x, y, width, height, font, color, seed, line_h, first_indent=True):
    """Fill a column with filler copy, justified by spacing words."""
    rng = np.random.default_rng(seed)
    vocab = ("the bank said customers should never share codes by phone and must call back on the "
             "number printed on their card police warned residents of Bekasi about callers who claim to be "
             "officers couriers or relatives asking for money transfers codes or app installs a "
             "spokesperson added that victims often report too late so the first minutes matter most "
             "neighbourhood heads were asked to share the warning at weekly meetings and mosques").split()
    yy = y
    para_left = int(rng.integers(5, 9))
    while yy + line_h <= y + height:
        indent = 14 if (first_indent and para_left == 0) else 0
        words, cur_w = [], indent
        while True:
            w = vocab[int(rng.integers(0, len(vocab)))]
            ww = cv.text_width(w + " ", font)
            if cur_w + ww > width and words:
                break
            words.append(w)
            cur_w += ww
        para_left -= 1
        last = para_left < 0
        if last:
            words = words[:max(2, len(words) // 2)]
            para_left = int(rng.integers(5, 9))
        total = sum(cv.text_width(w, font) for w in words)
        gap = (width - indent - total) / max(len(words) - 1, 1) if not last else cv.text_width(" ", font)
        xx = x + indent
        for w in words:
            cv.text((xx, yy), w, font, color)
            xx += cv.text_width(w, font) + gap
        yy += line_h
        if last:
            yy += line_h * 0.4


def rule(cv, x0, x1, y, color, w=1.2):
    cv.line([(x0, y), (x1, y)], color, w, caps=False)


# ---------------------------------------------------------------- newspaper

def phone_otp_illustration(w, h):
    cv = Canvas(w, h, ss=2, bg=(210, 210, 210))
    cv.rect(0, 0, w, h, fill=(150, 150, 150))
    for i in range(8):
        cv.rect(0, h * i / 8, w, h * (i + 1) / 8, fill=(150 + i * 8,) * 3)
    hand = (120, 120, 120)
    cv.poly([(w * 0.25, h), (w * 0.3, h * 0.55), (w * 0.62, h * 0.5), (w * 0.72, h)], fill=hand, outline=(40, 40, 40), width=3)
    cv.rect(w * 0.33, h * 0.12, w * 0.63, h * 0.78, fill=(30, 30, 30), outline=(10, 10, 10), width=3)
    cv.rect(w * 0.35, h * 0.17, w * 0.61, h * 0.72, fill=(235, 235, 235))
    f = cv.font("DIN Condensed Bold.ttf", h * 0.1)
    cv.text((w * 0.48, h * 0.3), "OTP", f, (40, 40, 40), anchor="mm")
    f2 = cv.font("DIN Condensed Bold.ttf", h * 0.08)
    cv.text((w * 0.48, h * 0.45), "481927", f2, (40, 40, 40), anchor="mm")
    cv.rect(w * 0.38, h * 0.55, w * 0.58, h * 0.62, fill=(90, 90, 90))
    for fx in (0.3, 0.62):
        cv.ellipse(w * fx, h * 0.6, w * 0.05, h * 0.07, fill=hand, outline=(40, 40, 40), width=2)
    return cv.result()


def newspaper(w=1024, h=1400):
    cv = Canvas(w, h, ss=2, bg=PAPER)
    m = 40
    size = 78
    title = cv.font("Georgia Bold.ttf", size)
    while cv.text_width("BEKASI MORNING POST", title) > w - 2 * m - 20:
        size -= 2
        title = cv.font("Georgia Bold.ttf", size)
    cv.text((w / 2, m + 50), "BEKASI MORNING POST", title, NEWS_INK, anchor="mm")
    rule(cv, m, w - m, m + 6, NEWS_INK, 2.5)
    rule(cv, m, w - m, m + 12, NEWS_INK, 1)
    rule(cv, m, w - m, m + 96, NEWS_INK, 1)
    small = cv.font("Georgia.ttf", 17)
    cv.text((m, m + 106), "TUESDAY, 6 OCTOBER 2026", small, NEWS_INK)
    cv.text((w / 2, m + 106), "NO. 12,408  ·  EST. 1998", small, NEWS_INK, anchor="ma")
    cv.text((w - m, m + 106), "Rp 5.000", small, NEWS_INK, anchor="ra")
    rule(cv, m, w - m, m + 132, NEWS_INK, 2.5)

    head = cv.font("DIN Condensed Bold.ttf", 112)
    cv.text((m, m + 150), "BANK: WE NEVER ASK", head, NEWS_INK)
    cv.text((m, m + 262), "FOR YOUR CODE", head, NEWS_INK)
    sub = cv.font("Georgia Italic.ttf", 26)
    cv.text((m, m + 382), "Customers urged to hang up and call the number on their card", sub, NEWS_INK)

    px0, py0, pw, ph = m, m + 430, 600, 400
    photo = halftone(phone_otp_illustration(pw, ph), cell=6, dark=NEWS_INK, light=PAPER)
    cv.img.paste(photo.resize((pw * 2, ph * 2), Image.LANCZOS), (px0 * 2, py0 * 2))
    cv.rect(px0, py0, px0 + pw, py0 + ph, outline=NEWS_INK, width=1.5)
    cap = cv.font("Georgia Italic.ttf", 15)
    cv.text((px0, py0 + ph + 8), "A one-time code (OTP) is for your eyes only. Photo: BMP", cap, NEWS_INK)

    body = cv.font("Times.ttc", 15)
    col_w = (pw - 20) / 2
    for i in range(2):
        text_block(cv, px0 + i * (col_w + 20), py0 + ph + 40, col_w, h - (py0 + ph + 40) - 150, body, NEWS_INK, 100 + i, 18)
    sx = px0 + pw + 30
    sw = w - m - sx
    cv.rect(sx, py0, sx + sw, py0 + 470, outline=NEWS_INK, width=1.5)
    kick = cv.font("DIN Condensed Bold.ttf", 30)
    cv.text((sx + 14, py0 + 12), "INSIDE TODAY", kick, NEWS_INK)
    items = [("Planned outage", "Bekasi Timur, Thu 08.00-12.00"),
             ("Fake officers", "Police: we never ask for transfers"),
             ("Chilli prices", "Up 4% at Pasar Baru"),
             ("Futsal", "SMAN 3 wins city cup")]
    it_h = cv.font("Georgia Bold.ttf", 19)
    it_b = cv.font("Georgia.ttf", 15)
    yy = py0 + 58
    for a, b in items:
        cv.text((sx + 14, yy), a, it_h, NEWS_INK)
        for ln in wrap_words(cv, b, it_b, sw - 28):
            yy += 20
            cv.text((sx + 14, yy), ln, it_b, NEWS_INK)
        yy += 36
        rule(cv, sx + 14, sx + sw - 14, yy - 12, NEWS_INK, 0.8)
    text_block(cv, sx, py0 + 500, sw, h - (py0 + 500) - 150, body, NEWS_INK, 300, 18)

    ay = h - 130
    cv.rect(m, ay, w - m, h - m, outline=NEWS_INK, width=2.5)
    ad = cv.font("DIN Condensed Bold.ttf", 40)
    cv.text((m + 150, ay + 14), "SERVIS KIPAS & TV", ad, NEWS_INK)
    adb = cv.font("Georgia.ttf", 18)
    cv.text((m + 150, ay + 58), "Toko Jaya Elektronik · Jl. Juanda 12, Bekasi · Buka setiap hari", adb, NEWS_INK)
    fx, fy = m + 70, ay + 45
    cv.ellipse(fx, fy, 30, 30, outline=NEWS_INK, width=3)
    for k in range(3):
        ang = k * 2.094
        cv.ellipse(fx + math.cos(ang) * 14, fy + math.sin(ang) * 14, 14, 7, fill=NEWS_INK, rot=ang)
    cv.line([(fx, fy + 30), (fx, fy + 60)], NEWS_INK, 4)
    cv.line([(fx - 20, fy + 62), (fx + 20, fy + 62)], NEWS_INK, 4)
    img = paper_tone(cv.result(), 111, 0.07)
    return save(img, "T_Newspaper")


# ---------------------------------------------------------------- calendar

PASARAN = ["Legi", "Pahing", "Pon", "Wage", "Kliwon"]


def pasaran(d):
    # 17 August 1945 fell on Jumat Legi.
    return PASARAN[(d - datetime.date(1945, 8, 17)).days % 5]


def still_life(cv, x0, y0, w, h):
    bg1, bg2 = hexc("F3D9A6"), hexc("EFC98C")
    cv.rect(x0, y0, x0 + w, y0 + h, fill=bg1)
    for i in range(0, int(w), 36):
        cv.rect(x0 + i, y0, x0 + i + 18, y0 + h * 0.7, fill=bg2)
    table = hexc("B86A48")
    cv.rect(x0, y0 + h * 0.7, x0 + w, y0 + h, fill=table, outline=INK, width=2)
    cloth = hexc("F4EEE0")
    cv.poly([(x0 + w * 0.2, y0 + h * 0.7), (x0 + w * 0.8, y0 + h * 0.7), (x0 + w * 0.86, y0 + h * 0.96),
             (x0 + w * 0.14, y0 + h * 0.96)], fill=cloth, outline=INK, width=2)
    vx, vy = x0 + w * 0.5, y0 + h * 0.72
    vase = [(vx - 40, vy), (vx - 55, vy - 70), (vx - 30, vy - 120), (vx - 22, vy - 150), (vx + 22, vy - 150),
            (vx + 30, vy - 120), (vx + 55, vy - 70), (vx + 40, vy)]
    cv.poly(resample(vase + [vase[0]], 4), fill=hexc("3F7C86"), outline=INK, width=2.5)
    cv.line([(vx - 48, vy - 75), (vx + 48, vy - 75)], hexc("E7D39A"), 5)
    rng = np.random.default_rng(3)
    stems = []
    for k in range(7):
        ang = -math.pi / 2 + (k - 3) * 0.28 + rng.uniform(-0.08, 0.08)
        ln = rng.uniform(120, 190)
        ex, ey = vx + math.cos(ang) * ln, vy - 150 + math.sin(ang) * ln
        stems.append((ex, ey))
        cv.line([(vx, vy - 150), (ex, ey)], hexc("5E7E3C"), 3)
    cols = [hexc("D8553F"), hexc("F2B340"), hexc("E98AA0"), hexc("FFFFFF")]
    for k, (ex, ey) in enumerate(stems):
        c = cols[k % len(cols)]
        for p in range(6):
            a = p * 1.047
            cv.ellipse(ex + math.cos(a) * 16, ey + math.sin(a) * 16, 14, 9, fill=c, outline=INK, width=1.8, rot=a)
        cv.ellipse(ex, ey, 9, 9, fill=hexc("7A4A22"), outline=INK, width=1.6)
    cv.ellipse(x0 + w * 0.8, y0 + h * 0.66, 34, 28, fill=hexc("E6B94A"), outline=INK, width=2)
    cv.ellipse(x0 + w * 0.22, y0 + h * 0.67, 30, 24, fill=hexc("C9452F"), outline=INK, width=2)
    cv.rect(x0, y0, x0 + w, y0 + h, outline=INK, width=3)


def calendar(w=768, h=1152):
    cv = Canvas(w, h, ss=2, bg=hexc("F7F2E6"))
    red, green = hexc("C8392F"), hexc("2F7A4A")
    cv.rect(0, 0, w, 70, fill=hexc("8E2E2A"))
    f = cv.font("DIN Condensed Bold.ttf", 34)
    cv.text((w / 2, 38), "TOKO SUMBER REJEKI", f, (255, 246, 226), anchor="mm")
    still_life(cv, 30, 90, w - 60, 400)
    fs = cv.font("Arial.ttf", 14)
    cv.text((w / 2, 505), "Jl. Cut Mutia No. 8, Bekasi  ·  Telp. (021) 880-1234  ·  Grosir & Eceran", fs, INK, anchor="ma")
    ft = cv.font("Arial Black.ttf", 50)
    cv.text((40, 535), "OKTOBER", ft, red)
    cv.text((w - 40, 535), "2026", ft, INK, anchor="ra")
    fh = cv.font("Arial.ttf", 16)
    cv.text((40, 600), "Rabiul Akhir - Jumadil Awal 1448 H", fh, INK)
    days = ["MIN", "SEN", "SEL", "RAB", "KAM", "JUM", "SAB"]
    gx0, gy0 = 30, 640
    cw, chh = (w - 60) / 7, 88
    fd = cv.font("Arial Bold.ttf", 18)
    for i, dname in enumerate(days):
        col = red if i == 0 else (green if i == 5 else INK)
        cv.text((gx0 + cw * i + cw / 2, gy0), dname, fd, col, anchor="ma")
    rule(cv, gx0, w - 30, gy0 + 28, INK, 1.2)
    fn = cv.font("Arial Black.ttf", 40)
    fp = cv.font("Arial.ttf", 12)
    first = datetime.date(2026, 10, 1)
    start_col = (first.weekday() + 1) % 7
    for day in range(1, 32):
        idx = start_col + day - 1
        r, c = idx // 7, idx % 7
        x = gx0 + cw * c + cw / 2
        y = gy0 + 40 + r * chh
        col = red if c == 0 else (green if c == 5 else INK)
        cv.text((x, y), str(day), fn, col, anchor="ma")
        cv.text((x, y + 50), pasaran(datetime.date(2026, 10, day)), fp, shade(col, 1.0), anchor="ma")
    for r in range(1, 6):
        rule(cv, gx0, w - 30, gy0 + 34 + r * chh, mix(INK, (247, 242, 230), 0.75), 0.8)

    hand = cv.font("Noteworthy.ttc", 17, index=1)

    def cell_xy(day):
        idx = start_col + day - 1
        return gx0 + cw * (idx % 7) + cw / 2, gy0 + 40 + (idx // 7) * chh

    rng = np.random.default_rng(5)
    x, y = cell_xy(8)
    cv.poly(wobble([(x + 36 * math.cos(a), y + 26 + 30 * math.sin(a)) for a in np.linspace(0, 6.6, 40)], 1.2, rng),
            outline=BLUE_PEN, width=2.2)
    cv.text((x - 44, y + 60), "Kontrol dr", hand, BLUE_PEN)
    x, y = cell_xy(17)
    cv.text((x - 30, y + 62), "Arisan", hand, RED_PEN)
    x, y = cell_xy(20)
    cv.text((x - 44, y + 62), "Bayar PLN", hand, BLUE_PEN)
    cv.line([(x + 22, y + 8), (x + 30, y + 18), (x + 46, y - 6)], BLUE_PEN, 3)
    x, y = cell_xy(25)
    for k in range(5):
        a1 = -math.pi / 2 + k * 2 * math.pi / 5
        a2 = a1 + 2 * math.pi * 2 / 5
        cv.line([(x + 30 + 12 * math.cos(a1), y + 4 + 12 * math.sin(a1)),
                 (x + 30 + 12 * math.cos(a2), y + 4 + 12 * math.sin(a2))], RED_PEN, 2)
    cv.text((x - 36, y + 62), "Ultah Sari", hand, RED_PEN)
    img = paper_tone(cv.result(), 222, 0.05)
    return save(img, "T_Calendar")


# ---------------------------------------------------------------- notes atlas

def lined_paper(cv, x0, y0, x1, y1, bg, line_col, spacing=26, margin_col=None):
    cv.rect(x0, y0, x1, y1, fill=bg)
    y = y0 + spacing * 1.6
    while y < y1 - 6:
        rule(cv, x0 + 6, x1 - 6, y, line_col, 1.0)
        y += spacing
    if margin_col:
        cv.line([(x0 + 52, y0 + 4), (x0 + 52, y1 - 4)], margin_col, 1.2, caps=False)


def notes_atlas(size=1024):
    cv = Canvas(size, size, ss=2, bg=(250, 250, 250))
    hand = cv.font("Noteworthy.ttc", 26, index=1)
    hand_s = cv.font("Noteworthy.ttc", 21, index=1)
    marker = cv.font("MarkerFelt.ttc", 30)

    # A: important numbers, lined paper
    lined_paper(cv, 0, 0, 512, 512, hexc("F8F4E8"), hexc("A9C3DA"), 30, hexc("E0A0A0"))
    cv.text((70, 18), "NOMOR PENTING", marker, RED_PEN)
    rows = [("Polsek Bekasi Tim.", "(021) 884-2210"), ("PLN", "123"), ("Klinik Sehat", "(021) 889-0012"),
            ("Pak RT Harun", "0812-9000-4411"), ("Bank Nusa", "1500-888"), ("Dimas", "0813-2211-7788"),
            ("Sari", "0857-4410-2020")]
    y = 60
    for a, b in rows:
        cv.text((66, y), a, hand, BLUE_PEN)
        cv.text((500, y), b, hand, BLUE_PEN, anchor="ra")
        y += 60

    # B: RT notice, typed
    cv.rect(512, 0, 1024, 512, fill=hexc("FBFBF6"))
    tf = cv.font("AmericanTypewriter.ttc", 22)
    tb = cv.font("AmericanTypewriter.ttc", 17)
    cv.text((768, 30), "PENGUMUMAN", tf, INK, anchor="ma")
    cv.text((768, 60), "RT 04 / RW 07  Kel. Duren Jaya", tb, INK, anchor="ma")
    rule(cv, 560, 976, 92, INK, 1.5)
    lines = ["Kepada seluruh warga:", "1. Kerja bakti Minggu 11 Okt", "   pukul 07.00 WIB.",
             "2. Pemutakhiran data domisili", "   di Pos Polisi, bawa KK & KTP.", "3. WASPADA telepon yang",
             "   mengaku petugas & minta", "   transfer uang. Tutup telp,", "   hubungi nomor resmi.",
             "", "Ketua RT 04,        Harun S."]
    y = 108
    for ln in lines:
        cv.text((560, y), ln, tb, INK)
        y += 30
    cv.ellipse(900, 430, 46, 46, outline=hexc("5B5FB8"), width=3)
    cv.ellipse(900, 430, 36, 36, outline=hexc("5B5FB8"), width=1.5)
    st = cv.font("Arial Bold.ttf", 12)
    cv.text((900, 424), "RT 04", st, hexc("5B5FB8"), anchor="mm")
    cv.text((900, 440), "RW 07", st, hexc("5B5FB8"), anchor="mm")

    # C, D: sticky notes
    cv.rect(0, 512, 256, 768, fill=hexc("F7E27A"))
    cv.text((22, 560), "Bayar", hand, INK)
    cv.text((22, 600), "listrik  v", hand, INK)
    cv.text((22, 650), "tgl 20!", hand, RED_PEN)
    cv.rect(256, 512, 512, 768, fill=hexc("F5B3C2"))
    cv.text((278, 560), "Arisan", hand, INK)
    cv.text((278, 600), "Sabtu", hand, INK)
    cv.text((278, 640), "rumah Bu Nia", hand_s, INK)

    # E: receipt
    cv.rect(512, 512, 768, 1024, fill=hexc("F6F4EE"))
    mono = cv.font("Courier New Bold.ttf", 17)
    mono_s = cv.font("Courier New.ttf", 15)
    y = 530
    for ln, f in [("TOKO MAJU", mono), ("Jl. Kemang 3", mono_s), ("06/10/26 08:14", mono_s), ("-" * 22, mono_s),
                  ("Gula 1kg   18.000", mono_s), ("Teh celup  12.500", mono_s), ("Sabun       9.500", mono_s),
                  ("Kerupuk     5.000", mono_s), ("-" * 22, mono_s), ("TOTAL      45.000", mono),
                  ("TUNAI      50.000", mono_s), ("KEMBALI     5.000", mono_s), ("", mono_s), ("TERIMA KASIH", mono)]:
        cv.text((530, y), ln, f, INK)
        y += 26

    # F: business card
    cv.rect(768, 512, 1024, 768, fill=hexc("FFFFFF"))
    cv.rect(768, 512, 1024, 560, fill=hexc("3A8A7A"))
    cf = cv.font("Arial Bold.ttf", 22)
    cv.text((896, 536), "KLINIK SEHAT", cf, (255, 255, 255), anchor="mm")
    cs = cv.font("Arial.ttf", 15)
    for i, ln in enumerate(["dr. Wulan Pratiwi", "Praktik Umum", "Senin-Sabtu 08-20", "(021) 889-0012"]):
        cv.text((786, 580 + i * 36), ln, cs, INK)
    cv.ellipse(990, 740, 12, 12, fill=hexc("3A8A7A"))
    cv.rect(984, 737, 996, 743, fill=(255, 255, 255))
    cv.rect(987, 734, 993, 746, fill=(255, 255, 255))

    # G: grandchild's crayon drawing
    cv.rect(0, 768, 512, 1024, fill=hexc("FDFCF7"))
    rng = np.random.default_rng(9)
    crayon = lambda pts, col, w=5: cv.line(wobble(pts, 2.0, rng, step=6), col, w)
    crayon([(80, 990), (80, 880), (200, 820), (320, 880), (320, 990), (80, 990)], hexc("D2453A"))
    crayon([(170, 990), (170, 925), (230, 925), (230, 990)], hexc("2F6DC0"))
    crayon([(0, 995), (512, 995)], hexc("3E9A48"), 7)
    for k in range(12):
        a = k * 0.52
        crayon([(430 + 28 * math.cos(a), 840 + 28 * math.sin(a)), (430 + 52 * math.cos(a), 840 + 52 * math.sin(a))],
               hexc("F2B530"), 4)
    cv.ellipse(430, 840, 24, 24, fill=hexc("F2B530"))
    kid = cv.font("Chalkboard.ttc", 30)
    cv.text((40, 790), "NENEK <3", kid, hexc("8B3FB0"))

    # H: small family snapshot
    cv.rect(768, 768, 1024, 1024, fill=(255, 255, 255))
    import pixel_people as pp
    snap = pp.family((232, 200), grid=(58, 50))
    cv.img.paste(snap.resize((232 * cv.ss, 200 * cv.ss), Image.NEAREST), (780 * cv.ss, 780 * cv.ss))
    cv.text((896, 1000), "Lebaran 2025", cv.font("Noteworthy.ttc", 17, index=1), INK, anchor="mm")
    img = paper_tone(cv.result(), 333, 0.05)
    return save(img, "T_Notes_Atlas")


def photos_atlas(size=1024):
    """Family photos as pixel-art portraits (style: references/art/ref4.png)."""
    import pixel_people as pp
    h = size // 2
    atlas = Image.new("RGB", (size, size))
    atlas.paste(pp.graduation((h, h)), (0, 0))
    atlas.paste(pp.wedding((h, h)), (h, 0))
    atlas.paste(pp.family((h, h)), (0, h))
    atlas.paste(pp.pas_foto((h, h)), (h, h))
    return save(atlas, "T_Photos_Atlas")


# ---------------------------------------------------------------- painting

def palm(cv, x, y, height, lean, frond_col, trunk_col, rng, lw=2.2):
    pts = [(x + lean * (t ** 1.6) * height * 0.35, y - t * height) for t in np.linspace(0, 1, 16)]
    for i in range(len(pts) - 1):
        cv.line([pts[i], pts[i + 1]], trunk_col, max(3.0, height * 0.05 * (1 - i / 20)))
    top = pts[-1]
    for k in range(8):
        ang = -math.pi / 2 + (k - 3.5) * 0.42 + rng.uniform(-0.1, 0.1)
        ln = height * rng.uniform(0.35, 0.5)
        frond = [(top[0] + math.cos(ang) * ln * t, top[1] + math.sin(ang) * ln * t + (t ** 2) * ln * 0.45) for t in
                 np.linspace(0, 1, 12)]
        for i in range(1, len(frond)):
            fx, fy = frond[i]
            px, py = frond[i - 1]
            dx, dy = fx - px, fy - py
            L = math.hypot(dx, dy) or 1
            nx, ny = -dy / L, dx / L
            leaf_len = ln * 0.16 * (1 - i / 14)
            for s in (1, -1):
                cv.line([(fx, fy), (fx + nx * leaf_len * s + dx * 0.8, fy + ny * leaf_len * s + dy * 0.8 + leaf_len * 0.4)],
                        frond_col, lw * 1.3)
        cv.line(frond, shade(frond_col, 0.6), lw)


def painting(w=1024, h=683):
    rng = np.random.default_rng(12)
    cv = Canvas(w, h, ss=2, bg=(255, 255, 255))
    steps = 30
    for i in range(steps):
        t = i / (steps - 1)
        cv.rect(0, h * 0.62 * i / steps, w, h * 0.62 * (i + 1) / steps + 1, fill=mix(hexc("7FBEDC"), hexc("F7E3B4"), t))
    cv.ellipse(700, 150, 46, 46, fill=hexc("F9D776"), outline=INK, width=2)
    for cx, cy, s in ((200, 110, 1.0), (520, 80, 0.8), (880, 120, 0.9)):
        blobs = [(-60, 0, 42), (-20, -18, 44), (25, -10, 40), (60, 4, 32), (0, 10, 40)]
        for bx, by, br in blobs:
            cv.ellipse(cx + bx * s, cy + by * s, br * s, br * s * 0.75, fill=hexc("FFF8EC"))
        for bx, by, br in blobs:
            cv.ellipse(cx + bx * s, cy + by * s, br * s, br * s * 0.75, outline=hexc("9DB6CC"), width=1.4)
        for bx, by, br in blobs:
            cv.ellipse(cx + bx * s, cy + by * s + 1, br * s - 2, br * s * 0.75 - 2, fill=hexc("FFF8EC"))
    far, near = hexc("7C85B8"), hexc("5E6CA6")
    left = [(40, 470), (230, 260), (300, 205), (345, 200), (400, 250), (640, 470)]
    right = [(430, 470), (640, 285), (700, 240), (745, 240), (800, 290), (1010, 470)]
    cv.poly(left, fill=far, outline=INK, width=2)
    cv.poly(right, fill=near, outline=INK, width=2)
    for mx, my in ((320, 215), (722, 255)):
        for k in range(5):
            dx = (k - 2) * 26
            cv.line(wobble([(mx + dx * 0.3, my + 10), (mx + dx * 2.2, my + 170)], 3, rng), shade(far, 0.75), 1.6)
    for mx, my in ((323, 204), (722, 242)):
        cv.poly([(mx - 22, my + 4), (mx - 8, my - 4), (mx + 10, my - 3), (mx + 24, my + 4)], fill=hexc("E9EEF6"))
    hills = [(hexc("88B660"), 430, 0.018, 28), (hexc("6C9F4E"), 480, 0.012, 36)]
    for col, base, fq, amp in hills:
        pts = [(x, base + math.sin(x * fq + base) * amp) for x in range(0, w + 16, 16)]
        cv.poly([(0, h)] + pts + [(w, h)], fill=col, outline=INK, width=2)
    terr = [hexc("A9CF6A"), hexc("93C05A"), hexc("B9D97A")]
    for k in range(7):
        y0 = 520 + k * 24
        pts = [(x, y0 + math.sin(x * 0.009 + k) * 10) for x in range(0, w + 16, 16)]
        pts2 = [(x, y0 + 24 + math.sin(x * 0.009 + k + 0.6) * 10) for x in range(w, -16, -16)]
        cv.poly(pts + pts2, fill=terr[k % 3])
        cv.line(pts, shade(terr[k % 3], 0.6), 1.6)
    road = [(470, h), (560, h), (600, 600), (560, 540), (590, 500), (575, 500), (540, 540), (560, 600)]
    cv.poly(road, fill=hexc("E6CF98"), outline=INK, width=2)
    hx, hy = 760, 505
    cv.rect(hx, hy, hx + 60, hy + 40, fill=hexc("F2E6C8"), outline=INK, width=2)
    cv.poly([(hx - 10, hy), (hx + 30, hy - 30), (hx + 70, hy)], fill=hexc("C4543C"), outline=INK, width=2)
    cv.rect(hx + 24, hy + 16, hx + 36, hy + 40, fill=hexc("6A4A32"))
    palm(cv, 110, h - 10, 360, 0.5, hexc("3C7A44"), hexc("7A5A3A"), rng)
    palm(cv, 190, h - 5, 290, -0.4, hexc("4C8C4E"), hexc("86653F"), rng)
    palm(cv, 930, h - 20, 250, -0.6, hexc("3C7A44"), hexc("7A5A3A"), rng)
    cv.rect(0, h - 40, w, h, fill=hexc("4E7E3C"))
    img = cv.result()
    arr = np.asarray(img).astype(np.float32)
    canvas_tex = 0.94 + 0.06 * fbm(w, h, 55, octaves=((60, 0.5), (150, 0.5)))
    img = Image.fromarray(np.clip(arr * canvas_tex[..., None], 0, 255).astype(np.uint8))
    return save(img, "T_Painting")


# ---------------------------------------------------------------- small printed items

def clock_face(size=512):
    cv = Canvas(size, size, ss=3, bg=hexc("F3ECD8"))
    c = size / 2
    cv.ellipse(c, c, c - 4, c - 4, fill=hexc("F4EDDA"), outline=hexc("7E2A26"), width=24)
    for i in range(60):
        a = i * math.pi / 30
        r0 = c - 34 if i % 5 else c - 48
        cv.line([(c + math.cos(a) * r0, c + math.sin(a) * r0), (c + math.cos(a) * (c - 24), c + math.sin(a) * (c - 24))],
                INK, 4 if i % 5 == 0 else 1.6)
    f = cv.font("Arial Black.ttf", 50)
    for n in range(1, 13):
        a = n * math.pi / 6 - math.pi / 2
        cv.text((c + math.cos(a) * (c - 92), c + math.sin(a) * (c - 92)), str(n), f, INK, anchor="mm")
    fs = cv.font("DIN Condensed Bold.ttf", 26)
    cv.text((c, c + 70), "TOKO BERKAH JAYA", fs, hexc("7E2A26"), anchor="mm")
    cv.text((c, c - 64), "QUARTZ", cv.font("Georgia Italic.ttf", 18), INK, anchor="mm")
    return save(cv.result(), "T_ClockFace")


def tv_screen(w=512, h=288):
    cv = Canvas(w, h, ss=2, bg=hexc("1E242E"))
    for i in range(20):
        t = i / 19
        cv.rect(0, h * i / 20, w, h * (i + 1) / 20 + 1, fill=mix(hexc("2A3240"), hexc("161A22"), t))
    cv.poly([(w * 0.55, 0), (w * 0.72, 0), (w * 0.38, h), (w * 0.21, h)], fill=hexc("323C4C"))
    cv.poly([(w * 0.76, 0), (w * 0.8, 0), (w * 0.46, h), (w * 0.42, h)], fill=hexc("2E3746"))
    return save(cv.result(), "T_TV_Screen")


def book_spines(size=512):
    rng = np.random.default_rng(17)
    cv = Canvas(size, size, ss=2, bg=hexc("F2E8D2"))
    cols = [hexc(c) for c in ("8E2E2A", "2F5D6E", "D9A444", "3F6B3F", "6B4A8A", "C8643E", "2C3E62", "B9A26A",
                              "7A3A5A", "4E8C8C", "D8C8A0", "5A4030", "A83C3C", "3C5A3C")]
    cw = size / 16
    for i in range(14):
        x0, x1 = i * cw, (i + 1) * cw
        col = cols[i]
        cv.rect(x0, 0, x1, size, fill=col)
        for yb in rng.choice([40, 80, 420, 460], size=2, replace=False):
            cv.rect(x0, yb, x1, yb + 10, fill=hexc("E6C66A") if rng.random() < 0.5 else shade(col, 0.6))
        ty = rng.uniform(140, 200)
        cv.rect(x0 + 5, ty, x1 - 5, ty + 150, fill=shade(col, 1.25) if rng.random() < 0.5 else hexc("F0E6CC"))
        for k in range(6):
            yy = ty + 16 + k * 22
            cv.line([(x0 + 10, yy), (x1 - 10, yy)], INK, 2.2)
        cv.line([(x0, 0), (x0, size)], INK, 2, caps=False)
    for x in range(14 * int(cw), size, 3):
        cv.line([(x, 0), (x, size)], hexc("D7CBB0"), 0.8, caps=False)
    return save(cv.result(), "T_Book_Spines")


def labels_atlas(size=1024):
    cv = Canvas(size, size, ss=2, bg=(245, 245, 245))
    # A: biscuit tin lid
    cv.rect(0, 0, 512, 512, fill=hexc("B8322C"))
    cv.ellipse(256, 256, 240, 240, fill=hexc("C23A30"), outline=hexc("E3B94E"), width=14)
    for k in range(48):
        a = k * math.pi / 24
        cv.dot(256 + math.cos(a) * 214, 256 + math.sin(a) * 214, 5, hexc("E3B94E"))
    cv.ellipse(256, 290, 130, 70, fill=hexc("F2EEE4"), outline=INK, width=3)
    for (bx, by, kind) in ((200, 270, 0), (256, 300, 1), (312, 272, 0), (240, 250, 1), (292, 318, 0)):
        if kind == 0:
            cv.ellipse(bx, by, 34, 22, fill=hexc("D9A456"), outline=INK, width=2.4)
            for d in range(5):
                cv.dot(bx - 16 + d * 8, by, 2.2, hexc("8B5A2B"))
        else:
            cv.rect(bx - 30, by - 14, bx + 30, by + 14, fill=hexc("E8C27A"), outline=INK, width=2.4)
            cv.line([(bx - 24, by), (bx + 24, by)], hexc("B8322C"), 3)
    cv.rect(96, 110, 416, 180, fill=hexc("E3B94E"), outline=INK, width=3)
    cv.text((256, 146), "ROTI KALENG", cv.font("Georgia Bold.ttf", 40), hexc("7E1E1A"), anchor="mm")
    cv.text((256, 400), "ASSORTED BISCUITS", cv.font("DIN Condensed Bold.ttf", 30), hexc("F7E7BE"), anchor="mm")
    # B: water gallon label
    cv.rect(512, 0, 1024, 256, fill=hexc("2E7CC2"))
    for k in range(4):
        pts = [(x, 170 + k * 14 + math.sin(x * 0.03 + k) * 6) for x in range(512, 1025, 8)]
        cv.line(pts, hexc("8CC8F0") if k % 2 else (255, 255, 255), 4, caps=False)
    cv.text((768, 70), "TIRTA SEGAR", cv.font("Arial Black.ttf", 52), (255, 255, 255), anchor="mm")
    cv.text((768, 124), "AIR MINUM DALAM KEMASAN  19 L", cv.font("Arial Bold.ttf", 22), hexc("DDF0FF"), anchor="mm")
    # C: tissue box
    cv.rect(512, 256, 1024, 512, fill=hexc("F4C9CF"))
    rng = np.random.default_rng(4)
    for _ in range(26):
        x, y = rng.uniform(520, 1016), rng.uniform(264, 504)
        for p in range(5):
            a = p * 1.2566
            cv.ellipse(x + math.cos(a) * 9, y + math.sin(a) * 9, 8, 6, fill=(255, 255, 255), rot=a)
        cv.dot(x, y, 4, hexc("E9A447"))
    cv.rect(640, 350, 896, 420, fill=(255, 255, 255), outline=INK, width=2)
    cv.text((768, 385), "SOFT TISU", cv.font("Arial Rounded Bold.ttf", 34), hexc("C75A76"), anchor="mm")
    # D: TV remote
    cv.rect(0, 512, 256, 768, fill=hexc("2A2A30"))
    cv.ellipse(56, 548, 14, 14, fill=hexc("D8413A"))
    for r in range(4):
        for c in range(3):
            cv.ellipse(88 + c * 40, 590 + r * 34, 13, 10, fill=hexc("6A6A74"))
    for i, col in enumerate(("D8413A", "4FAF5A", "E3C040", "3A78C8")):
        cv.rect(64 + i * 34, 736, 88 + i * 34, 752, fill=hexc(col))
    # E: bank card front
    cv.rect(256, 512, 512, 768, fill=hexc("F0F0F0"))
    card = (264, 560, 504, 712)
    studio = lambda: None
    for i in range(20):
        t = i / 19
        cv.rect(card[0], card[1] + (card[3] - card[1]) * i / 20, card[2], card[1] + (card[3] - card[1]) * (i + 1) / 20 + 1,
                fill=mix(hexc("1F4E8C"), hexc("2E7BC0"), t))
    cv.text((278, 574), "BANK NUSA", cv.font("Arial Black.ttf", 22), (255, 255, 255))
    cv.rect(280, 614, 316, 640, fill=hexc("E3C170"), outline=INK, width=1.2)
    cv.text((280, 654), "5214 0081 3377 0921", cv.font("Courier New Bold.ttf", 15), (255, 255, 255))
    cv.text((280, 684), "RATNA SURYANI", cv.font("Arial Bold.ttf", 14), (230, 240, 255))
    # F: bank card back
    cv.rect(512, 512, 768, 768, fill=hexc("F0F0F0"))
    card = (520, 560, 760, 712)
    cv.rect(*card, fill=hexc("2A5E9C"))
    cv.rect(card[0], 578, card[2], 606, fill=(20, 20, 24))
    cv.rect(532, 620, 700, 644, fill=(245, 245, 240))
    cv.text((536, 624), "Ratna S.", cv.font("Noteworthy.ttc", 15, index=1), BLUE_PEN)
    cv.text((532, 654), "CUSTOMER CARE 24 JAM", cv.font("Arial Bold.ttf", 12), (255, 255, 255))
    cv.text((532, 672), "1500-888", cv.font("Arial Black.ttf", 24), hexc("FFE28A"))
    # G: rulebook label
    cv.rect(768, 512, 1024, 768, fill=hexc("B98D5C"))
    cv.rect(800, 590, 992, 690, fill=hexc("FBF6E8"), outline=INK, width=2)
    cv.text((896, 616), "CATATAN", cv.font("Arial Bold.ttf", 16), INK, anchor="mm")
    cv.text((896, 656), "Aturan aman", cv.font("Noteworthy.ttc", 26, index=1), BLUE_PEN, anchor="mm")
    # H: fan brand badge
    cv.rect(0, 768, 512, 1024, fill=hexc("E9E4DA"))
    cv.rect(96, 840, 416, 950, fill=hexc("2A5E9C"), outline=INK, width=3)
    cv.text((256, 895), "KIPASKU", cv.font("Arial Black.ttf", 56), (255, 255, 255), anchor="mm")
    # I: door mat
    cv.rect(512, 768, 1024, 1024, fill=hexc("B98A52"))
    for x in range(512, 1024, 6):
        cv.line([(x, 768), (x + 3, 1024)], hexc("A67A45"), 1.2, caps=False)
    cv.rect(532, 788, 1004, 1004, outline=hexc("5E3E22"), width=6)
    cv.text((768, 896), "SELAMAT DATANG", cv.font("Arial Black.ttf", 44), hexc("5E3E22"), anchor="mm")
    return save(cv.result(), "T_Labels_Atlas")


# ---------------------------------------------------------------- rug

def rug(w=1024, h=1536):
    rng = np.random.default_rng(21)
    FIELD, MAR, CREAM, MUS, NAVY = hexc("2F3E5E"), hexc("9E3B2D"), hexc("E3CFA2"), hexc("B8863A"), hexc("5A3A26")
    cv = Canvas(w, h, ss=2, bg=MAR)
    cv.rect(40, 40, w - 40, h - 40, fill=CREAM, outline=INK, width=2.5)
    step = 56
    for x in range(70, w - 60, step):
        for y in (68, h - 68):
            cv.ellipse(x + step / 2, y, 18, 18, fill=MAR, outline=INK, width=1.8, rot=0.785, n=4)
            cv.dot(x + step / 2, y, 4, MUS)
    for y in range(70, h - 60, step):
        for x in (68, w - 68):
            cv.ellipse(x, y + step / 2, 18, 18, fill=MAR, outline=INK, width=1.8, rot=0.785, n=4)
            cv.dot(x, y + step / 2, 4, MUS)
    cv.rect(98, 98, w - 98, h - 98, fill=MUS, outline=INK, width=2)
    cv.rect(110, 110, w - 110, h - 110, fill=FIELD, outline=INK, width=2.5)
    cx, cy = w / 2, h / 2
    for (hw, hh, col) in ((330, 480, MAR), (280, 410, CREAM), (225, 330, NAVY), (170, 250, MUS), (110, 165, MAR),
                          (55, 85, CREAM)):
        cv.poly([(cx, cy - hh), (cx + hw, cy), (cx, cy + hh), (cx - hw, cy)], fill=col, outline=INK, width=2.5)
    for k in range(8):
        a = k * math.pi / 4
        cv.ellipse(cx + math.cos(a) * 26, cy + math.sin(a) * 26, 14, 6, fill=MAR, outline=INK, width=1.4, rot=a)
    for (qx, qy) in ((110, 110), (w - 110, 110), (110, h - 110), (w - 110, h - 110)):
        for (hw, col) in ((150, MAR), (110, CREAM), (70, MUS)):
            cv.poly([(qx, qy - hw * 1.4), (qx + hw, qy), (qx, qy + hw * 1.4), (qx - hw, qy)], fill=col, outline=INK, width=2.2)
    cv.rect(0, 0, w, 110, fill=None)
    for _ in range(26):
        x, y = rng.uniform(150, w - 150), rng.uniform(150, h - 150)
        if abs(x - cx) / 340 + abs(y - cy) / 490 < 1.05:
            continue
        for k in range(4):
            a = k * math.pi / 4
            cv.ellipse(x, y, 16, 5, fill=CREAM, rot=a)
        cv.dot(x, y, 4, MUS)
    img = cv.result()
    weave = np.asarray(Image.open(__import__("os").path.join(__import__("texlib").OUT, "T_Ink_Fabric.png"))
                       .convert("L").resize((w, h))).astype(np.float32) / 255
    arr = np.asarray(img).astype(np.float32) * (0.25 + 0.75 * weave)[..., None]
    arr *= (0.94 + 0.08 * fbm(w, h, 77))[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), "T_Rug")


# ---------------------------------------------------------------- doily, leaves

def doily(size=512):
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    c = (size - 1) / 2
    dx, dy = (xx - c) / c, (yy - c) / c
    r = np.sqrt(dx ** 2 + dy ** 2)
    th = np.arctan2(dy, dx)
    edge = 0.93 + 0.05 * np.cos(th * 24)
    alpha = (r < edge).astype(np.float32)
    thread = np.ones_like(r)
    rings = [(0.12, 8), (0.26, 14), (0.42, 22), (0.58, 30), (0.74, 40), (0.86, 48)]
    for rr, count in rings:
        band = np.abs(r - rr) < 0.045
        hole = np.cos(th * count) > 0.35
        thread[band & hole] = 0
    thread[(np.abs(r - 0.35) < 0.012) | (np.abs(r - 0.66) < 0.012)] = 1
    hole_edge = (r > edge - 0.05) & (np.cos(th * 24) > 0.6) & (np.abs(r - (edge - 0.07)) < 0.02)
    thread[hole_edge] = 0
    a = alpha * thread
    col = np.stack([np.full_like(r, 247), np.full_like(r, 243), np.full_like(r, 232)], -1)
    col *= (0.9 + 0.1 * (1 - np.abs(np.cos(th * 48))))[..., None]
    rgba = np.concatenate([col, (a * 255)[..., None]], -1)
    return save(Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA"), "T_Doily")


def leaves_atlas(size=1024):
    cv = Canvas(size, size, ss=2, mode="RGBA", bg=(0, 0, 0, 0))
    G1, G2, VEIN = hexc("566A3A") + (255,), hexc("78864A") + (255,), hexc("B4B882") + (255,)
    INKA = INK + (255,)
    # A: monstera: broad heart, slits cut from the edge toward the midrib, a few holes
    cx, cy = 256, 250
    heart = []
    for i in range(120):
        t = i / 120 * 2 * math.pi
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        heart.append((cx + x * 14.5, cy - y * 14.5 + 30))
    cv.poly(heart, fill=G1, outline=INKA, width=3)
    CLEAR = (0, 0, 0, 0)
    for side in (-1, 1):
        for k in range(5):
            yk = cy - 90 + k * 62
            ex = cx + side * 250
            best = min(heart, key=lambda p: abs(p[1] - yk) + (0 if (p[0] - cx) * side > 0 else 1e6))
            tx, ty = cx + side * 48, yk - 38
            ang = math.atan2(ty - best[1], tx - best[0])
            nx, ny = -math.sin(ang), math.cos(ang)
            wedge = [(best[0] + nx * 13 - math.cos(ang) * 20, best[1] + ny * 13 - math.sin(ang) * 20),
                     (tx + nx * 3, ty + ny * 3), (tx - nx * 3, ty - ny * 3),
                     (best[0] - nx * 13 - math.cos(ang) * 20, best[1] - ny * 13 - math.sin(ang) * 20)]
            cv.poly(wedge, fill=CLEAR)
            cv.line([(best[0] + nx * 13, best[1] + ny * 13), (tx + nx * 3, ty + ny * 3), (tx - nx * 3, ty - ny * 3),
                     (best[0] - nx * 13, best[1] - ny * 13)], INKA, 2.4)
            vx, vy = cx + side * 20, yk - 12
            cv.line([(cx, vy - 22), (vx + side * 60, vy + 8)], VEIN, 2.5)
        for k in range(3):
            hx, hy = cx + side * 92, cy - 50 + k * 70
            cv.ellipse(hx, hy, 9, 15, fill=CLEAR, outline=INKA, width=2.0, rot=side * 0.5)
    cv.line([(cx, cy - 150), (cx, cy + 215)], VEIN, 5)
    # B: pothos heart leaf, variegated
    cx, cy = 768, 256
    heart = []
    for i in range(90):
        t = i / 90 * 2 * math.pi
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        heart.append((cx + x * 13, cy - y * 13 + 20))
    cv.poly(heart, fill=G2, outline=INKA, width=3)
    rng = np.random.default_rng(8)
    for _ in range(9):
        x0, y0 = cx + rng.uniform(-120, 120), cy + rng.uniform(-80, 140)
        cv.line(wobble([(x0, y0), (x0 + rng.uniform(-40, 40), y0 + rng.uniform(-70, -30))], 3, rng), hexc("E8DD8A") + (255,), 5)
    cv.line([(cx, cy - 140), (cx, cy + 190)], shade(G2[:3], 0.7) + (255,), 3)
    # C: palm frond leaflet card
    cx, cy = 256, 768
    cv.line([(40, 1000), (470, 540)], hexc("7A6A3A") + (255,), 6)
    for i in range(24):
        t = i / 24
        px, py = 40 + (470 - 40) * t, 1000 + (540 - 1000) * t
        ln = 150 * math.sin(math.pi * (0.15 + 0.85 * t)) + 20
        for s in (1, -1):
            ang = -0.83 + s * 1.05
            ex, ey = px + math.cos(ang) * ln, py + math.sin(ang) * ln + ln * 0.25
            cv.poly([(px, py), ((px + ex) / 2 + 5 * s, (py + ey) / 2 - 5), (ex, ey), ((px + ex) / 2 - 5 * s, (py + ey) / 2 + 5)],
                    fill=G1, outline=INKA, width=1.4)
    # D: snake plant band texture
    x0, x1 = 512, 1024
    cv.rect(x0, 512, x1, 1024, fill=hexc("3F5230") + (255,))
    for k in range(18):
        y = 520 + k * 28
        pts = [(x, y + math.sin((x - x0) * 0.08 + k) * 6) for x in range(x0, x1 + 8, 8)]
        cv.line(pts, hexc("8E9A62") + (255,), 5, caps=False)
    cv.rect(x0, 512, x0 + 40, 1024, fill=hexc("D8C85A") + (255,))
    cv.rect(x1 - 40, 512, x1, 1024, fill=hexc("D8C85A") + (255,))
    cv.line([(x0 + 40, 512), (x0 + 40, 1024)], INKA, 2, caps=False)
    cv.line([(x1 - 40, 512), (x1 - 40, 1024)], INKA, 2, caps=False)
    return save(cv.result(), "T_Leaves_Atlas")


# ---------------------------------------------------------------- street backdrop

def backdrop(w=2048, h=1024):
    rng = np.random.default_rng(31)
    BLUE = hexc("48485E")
    cv = Canvas(w, h, ss=2, bg=(255, 255, 255))
    steps = 40
    for i in range(steps):
        t = i / (steps - 1)
        cv.rect(0, h * 0.75 * i / steps, w, h * 0.75 * (i + 1) / steps + 1, fill=mix(hexc("A6B6C7"), hexc("EFDBBE"), t))
    cv.rect(0, h * 0.74, w, h, fill=hexc("EFDBBE"))
    for cx, cy, s in ((300, 180, 1.4), (900, 120, 1.1), (1500, 200, 1.6), (1900, 90, 0.9)):
        blobs = [(-70, 0, 50), (-25, -22, 52), (30, -12, 48), (75, 6, 36), (0, 12, 50)]
        for bx, by, br in blobs:
            cv.ellipse(cx + bx * s, cy + by * s, br * s, br * s * 0.62, fill=hexc("F2E9D8"), outline=BLUE, width=2)
        for bx, by, br in blobs:
            cv.ellipse(cx + bx * s, cy + by * s + 2, br * s - 3, br * s * 0.62 - 3, fill=hexc("F2E9D8"))
    far_cols = [hexc("C9C1B2"), hexc("AFBFCC"), hexc("D8BFA2"), hexc("BCC6CC")]
    x = 0
    while x < w:
        bw = rng.uniform(80, 200)
        bh = rng.uniform(120, 300)
        col = far_cols[int(rng.integers(0, len(far_cols)))]
        top = 700 - bh
        cv.rect(x, top, x + bw, 700, fill=col, outline=BLUE, width=1.6)
        for wy in np.arange(top + 16, 690, 26):
            for wx in np.arange(x + 12, x + bw - 14, 22):
                if rng.random() < 0.8:
                    cv.rect(wx, wy, wx + 9, wy + 12, fill=shade(col, 0.82))
        x += bw + rng.uniform(-10, 30)
    mx = 1250
    cv.ellipse(mx, 575, 80, 78, fill=hexc("8CC0A8"), outline=BLUE, width=2)
    cv.line([(mx, 497), (mx, 470)], BLUE, 2)
    cv.ellipse(mx, 466, 7, 7, fill=hexc("E3C170"), outline=BLUE, width=1.5)
    cv.rect(mx - 118, 560, mx + 118, 700, fill=hexc("E9E2F2"), outline=BLUE, width=2)
    cv.rect(mx - 124, 550, mx + 124, 566, fill=hexc("E9E2F2"), outline=BLUE, width=2)
    for ax in range(-90, 100, 45):
        cv.poly([(mx + ax - 14, 700), (mx + ax - 14, 612), (mx + ax, 596), (mx + ax + 14, 612), (mx + ax + 14, 700)],
                fill=hexc("C8C0E0"), outline=BLUE, width=1.5)
    cv.rect(mx + 150, 330, mx + 180, 700, fill=hexc("E9E2F2"), outline=BLUE, width=2)
    cv.ellipse(mx + 165, 330, 22, 20, fill=hexc("8CC0A8"), outline=BLUE, width=2)
    cv.line([(mx + 165, 310), (mx + 165, 285)], BLUE, 2)
    for px, ph, lean in ((180, 420, 0.3), (640, 360, -0.2), (1680, 440, 0.25)):
        palm(cv, px, 820, ph, lean, hexc("6E7F58"), hexc("8A6A56"), rng, lw=2.4)
    roof_cols = [hexc("B35A43"), hexc("A34E3B"), hexc("C06B4E")]
    wall_cols = [hexc("EAD3B5"), hexc("B9C8D2"), hexc("E6D6AE"), hexc("D8BFA8")]
    x = -40
    while x < w:
        bw = rng.uniform(260, 420)
        wall = wall_cols[int(rng.integers(0, 4))]
        roof = roof_cols[int(rng.integers(0, 3))]
        base = 1024
        top = rng.uniform(760, 820)
        cv.rect(x, top, x + bw, base, fill=wall, outline=BLUE, width=2)
        cv.poly([(x - 20, top + 4), (x + bw * 0.5, top - 90), (x + bw + 20, top + 4)], fill=roof, outline=BLUE, width=2)
        for k in range(1, 7):
            yy = top - 90 + k * 14
            half = (bw * 0.5 + 20) * k / 7
            cv.line([(x + bw * 0.5 - half, yy), (x + bw * 0.5 + half, yy)], shade(roof, 0.8), 1.2, caps=False)
        for wx in (x + bw * 0.2, x + bw * 0.62):
            cv.rect(wx, top + 50, wx + bw * 0.18, top + 150, fill=hexc("4F5E82"), outline=BLUE, width=2)
            cv.line([(wx + bw * 0.09, top + 50), (wx + bw * 0.09, top + 150)], BLUE, 1.5)
        x += bw + rng.uniform(20, 60)
    for pole_x in (420, 1560):
        cv.rect(pole_x - 9, 300, pole_x + 9, 1024, fill=hexc("A88470"), outline=BLUE, width=2)
        cv.rect(pole_x - 70, 330, pole_x + 70, 344, fill=hexc("A88470"), outline=BLUE, width=2)
        for ix in (-60, -20, 20, 60):
            cv.rect(pole_x + ix - 4, 318, pole_x + ix + 4, 330, fill=hexc("E9E2F2"), outline=BLUE, width=1.2)
    for k, off in enumerate((-60, -20, 20, 60)):
        for (x0, x1) in ((-200, 420), (420, 1560), (1560, 2300)):
            y0 = 324 + k * 3
            pts = [(x0 + off + (x1 - x0) * t, y0 + math.sin(math.pi * t) * (70 + k * 6)) for t in np.linspace(0, 1, 40)]
            cv.line(pts, BLUE, 1.6, caps=False)
    img = cv.result()
    arr = np.asarray(img).astype(np.float32) * (0.96 + 0.05 * fbm(w, h, 66))[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), "T_Backdrop_Street")


def run():
    return [newspaper(), calendar(), notes_atlas(), photos_atlas(), painting(), clock_face(), tv_screen(),
            book_spines(), labels_atlas(), rug(), doily(), leaves_atlas(), backdrop()]


if __name__ == "__main__":
    for p in run():
        print(p)
