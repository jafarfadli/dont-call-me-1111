"""Printed and illustrated textures for the bedroom: newspaper, pharmacy calendar,
notes, family photos, labels, posters, clock, TV, books, rug, leaves and the view
of Seoul behind the window. Atlas layouts live in atlas.py."""
import datetime
import math
import os

import numpy as np
from PIL import Image

import atlas
import pixel_people as pp
from texlib import OUT, INK, Canvas, fbm, halftone, hexc, mix, multiply, save, shade, wobble

PAPER = hexc("EEE5CF")
NEWS_INK = (42, 38, 44)
BLUE_PEN = hexc("2C4A9A")
RED_PEN = hexc("C23B32")
GOTHIC = "AppleSDGothicNeo.ttc"   # index 0 regular, 6 bold
MYUNGJO = "AppleMyungjo.ttf"


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


FILLER = ("the bank said customers should never share codes by phone and must call back on the number printed "
          "on their card police warned residents of Mapo and Seodaemun about callers who claim to be prosecutors "
          "couriers or relatives asking for money transfers codes or app installs a spokesperson added that victims "
          "often report too late so the first minutes matter most building managers were asked to post the warning "
          "in lifts and at the entrance of every villa").split()


def text_block(cv, x, y, width, height, font, color, seed, line_h, first_indent=True, vocab=FILLER):
    """Fill a column with filler copy, justified by spacing words."""
    rng = np.random.default_rng(seed)
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


def gothic(cv, size, bold=False):
    return cv.font(GOTHIC, size, index=6 if bold else 0)


# ---------------------------------------------------------------- newspaper

def phone_transfer_photo(w, h):
    """Grey photo for the front page: a hand holding a phone on the transfer confirmation screen."""
    cv = Canvas(w, h, ss=2, bg=(200, 200, 200))
    for i in range(12):
        cv.rect(0, h * i / 12, w, h * (i + 1) / 12 + 1, fill=(212 - i * 7,) * 3)
    cv.poly([(0, h * 0.72), (w, h * 0.6), (w, h), (0, h)], fill=(118, 118, 118))
    hand, edge = (150, 150, 150), (40, 40, 40)
    px0, py0, px1, py1 = w * 0.38, h * 0.08, w * 0.66, h * 0.98
    cv.poly([(px1 - 10, h), (px1 + w * 0.02, h * 0.62), (px1 + w * 0.12, h * 0.55), (px1 + w * 0.2, h * 0.7), (w * 0.9, h)],
            fill=hand, outline=edge, width=3)
    cv.rect(px0, py0, px1, py1, fill=(34, 34, 34), outline=(12, 12, 12), width=3)
    sx0, sy0, sx1 = px0 + 10, py0 + 22, px1 - 10
    cv.rect(sx0, sy0, sx1, py1 - 22, fill=(238, 238, 238))
    cx = (sx0 + sx1) / 2
    cv.text((cx, sy0 + 40), "받는 분", gothic(cv, 18), (80, 80, 80), anchor="mm")
    cv.text((cx, sy0 + 82), "정미란", gothic(cv, 40, True), (20, 20, 20), anchor="mm")
    cv.text((cx, sy0 + 126), "1,200,000원", gothic(cv, 22, True), (40, 40, 40), anchor="mm")
    cv.rect(sx0 + 20, sy0 + 170, sx1 - 20, sy0 + 214, fill=(110, 110, 110))
    cv.text((cx, sy0 + 192), "보내기", gothic(cv, 22, True), (240, 240, 240), anchor="mm")
    for fx, fy in ((px0 - 4, h * 0.5), (px0 - 8, h * 0.62), (px0 - 6, h * 0.74)):
        cv.ellipse(fx, fy, w * 0.035, h * 0.045, fill=hand, outline=edge, width=2)
    cv.poly([(px1 + w * 0.12, h * 0.55), (px1 - w * 0.02, h * 0.42), (px1 - w * 0.05, h * 0.46), (px1 + w * 0.05, h * 0.6)],
            fill=hand, outline=edge, width=2)
    return cv.result()


def newspaper(w=1024, h=1400):
    cv = Canvas(w, h, ss=2, bg=PAPER)
    m = 40
    size = 80
    title = cv.font("Georgia Bold.ttf", size)
    while cv.text_width("SEOUL DAILY", title) > w * 0.62:
        size -= 2
        title = cv.font("Georgia Bold.ttf", size)
    cv.text((w / 2, m + 46), "SEOUL DAILY", title, NEWS_INK, anchor="mm")
    cv.text((m + 10, m + 46), "서울데일리", cv.font(MYUNGJO, 26), NEWS_INK, anchor="lm")
    cv.text((w - m - 10, m + 46), "제 12,408호", cv.font(MYUNGJO, 22), NEWS_INK, anchor="rm")
    rule(cv, m, w - m, m + 6, NEWS_INK, 2.5)
    rule(cv, m, w - m, m + 12, NEWS_INK, 1)
    rule(cv, m, w - m, m + 96, NEWS_INK, 1)
    small = cv.font("Georgia.ttf", 17)
    cv.text((m, m + 106), "TUESDAY, OCTOBER 6, 2026", small, NEWS_INK)
    cv.text((w / 2, m + 106), "2026년 10월 6일 화요일", gothic(cv, 17), NEWS_INK, anchor="ma")
    cv.text((w - m, m + 106), "1,000원", gothic(cv, 17), NEWS_INK, anchor="ra")
    rule(cv, m, w - m, m + 132, NEWS_INK, 2.5)

    head = cv.font("DIN Condensed Bold.ttf", 112)
    cv.text((m, m + 150), "BEFORE YOU SEND,", head, NEWS_INK)
    cv.text((m, m + 262), "READ THE NAME", head, NEWS_INK)
    sub = cv.font("Georgia Italic.ttf", 26)
    cv.text((m, m + 382), "Fake \"bank staff\" ask for transfers. Check whose account it is.", sub, NEWS_INK)

    px0, py0, pw, ph = m, m + 430, 600, 400
    photo = halftone(phone_transfer_photo(pw, ph), cell=4, dark=NEWS_INK, light=PAPER)
    cv.img.paste(photo.resize((pw * 2, ph * 2), Image.LANCZOS), (px0 * 2, py0 * 2))
    cv.rect(px0, py0, px0 + pw, py0 + ph, outline=NEWS_INK, width=1.5)
    cap = cv.font("Georgia Italic.ttf", 15)
    cv.text((px0, py0 + ph + 8), "The transfer screen shows who really gets the money. Photo: Seoul Daily", cap, NEWS_INK)
    body = cv.font("Times.ttc", 15)
    col_w = (pw - 20) / 2
    for i in range(2):
        text_block(cv, px0 + i * (col_w + 20), py0 + ph + 40, col_w, h - (py0 + ph + 40) - 150, body, NEWS_INK, 500 + i, 18)

    sx = px0 + pw + 30
    sw = w - m - sx
    cv.text((sx + 14, py0 + 12), "INSIDE TODAY", cv.font("DIN Condensed Bold.ttf", 30), NEWS_INK)
    items = [("Gas inspection", "Inspectors never ask for money"), ("Fake prosecutors", "There is no such thing as a safe account"),
             ("Parcel texts", "Check a payment link's address"), ("Subway fares", "Base fare stays at 1,550 won"),
             ("Weather", "Clear, 21°C, humid evening")]
    it_h, it_b = cv.font("Georgia Bold.ttf", 19), cv.font("Georgia.ttf", 15)
    yy = py0 + 58
    for a, b in items:
        cv.text((sx + 14, yy), a, it_h, NEWS_INK)
        for ln in wrap_words(cv, b, it_b, sw - 28):
            yy += 20
            cv.text((sx + 14, yy), ln, it_b, NEWS_INK)
        yy += 36
        rule(cv, sx + 14, sx + sw - 14, yy - 12, NEWS_INK, 0.8)
    cv.rect(sx, py0, sx + sw, yy, outline=NEWS_INK, width=1.5)
    text_block(cv, sx, yy + 30, sw, h - (yy + 30) - 150, body, NEWS_INK, 600, 18)

    ay = h - 130
    cv.rect(m, ay, w - m, h - m, outline=NEWS_INK, width=2.5)
    cv.text((m + 150, ay + 12), "치킨 배달 CHICKEN 24H", gothic(cv, 38, True), NEWS_INK)
    cv.text((m + 150, ay + 60), "Crispy & soy garlic · Free delivery over 20,000 won · 02-555-0147", cv.font("Georgia.ttf", 18),
            NEWS_INK)
    # Drumstick logo.
    fx, fy, ang = m + 72, ay + 46, -0.6
    ca, sa = math.cos(ang), math.sin(ang)
    bone = [(fx + ca * 18 - sa * s, fy + sa * 18 + ca * s) for s in (-5, 5)]
    bone += [(fx + ca * 52 - sa * s, fy + sa * 52 + ca * s) for s in (5, -5)]
    cv.poly(bone, fill=PAPER, outline=NEWS_INK, width=2)
    for s in (-1, 1):
        cv.ellipse(fx + ca * 56 - sa * s * 7, fy + sa * 56 + ca * s * 7, 8, 8, fill=PAPER, outline=NEWS_INK, width=2)
    cv.ellipse(fx - ca * 8, fy - sa * 8, 32, 24, fill=NEWS_INK, rot=ang)
    return save(paper_tone(cv.result(), 111, 0.07), "T_Newspaper")


# ---------------------------------------------------------------- calendar

HOLIDAYS = {3: "개천절", 5: "대체공휴일", 9: "한글날"}   # 3 Oct is a Saturday, so Monday 5 Oct is a substitute holiday
LUNAR_OCT_1 = (8, 21)          # lunar 8/21 on 1 Oct 2026; lunar 9/1 falls on 11 Oct


def lunar(day):
    d = LUNAR_OCT_1[1] + day - 1
    return (8, d) if d <= 30 else (9, d - 30)


def sun_moon_peaks(w, h):
    """Irworobongdo-style art for the calendar: five peaks, sun and moon, pines and waves."""
    cv = Canvas(w, h, ss=2, bg=hexc("E9D9B4"))
    peaks = [(0.1, 0.52, 0.2), (0.28, 0.3, 0.22), (0.5, 0.18, 0.25), (0.72, 0.3, 0.22), (0.9, 0.52, 0.2)]
    for px, top, half in peaks:
        cx, ty = w * px, h * top
        cv.poly([(cx - w * half, h * 0.8), (cx, ty), (cx + w * half, h * 0.8)], fill=hexc("3E6E6A"), outline=INK, width=2)
        cv.poly([(cx, ty), (cx + w * half * 0.5, h * 0.52), (cx + w * half, h * 0.8), (cx + w * half * 0.3, h * 0.8)],
                fill=hexc("2F5856"))
    cv.ellipse(w * 0.8, h * 0.16, w * 0.055, w * 0.055, fill=hexc("C8453A"), outline=INK, width=2)
    cv.ellipse(w * 0.2, h * 0.16, w * 0.05, w * 0.05, fill=hexc("F2ECDD"), outline=INK, width=2)
    for k in range(4):
        y = h * (0.82 + k * 0.045)
        pts = [(w * t, y + math.sin(t * 40 + k) * 4) for t in np.linspace(0, 1, 60)]
        cv.poly(pts + [(w, h), (0, h)], fill=hexc("4F6F98") if k % 2 == 0 else hexc("6E8FB5"))
        cv.line(pts, INK, 1.5, caps=False)
    for sx in (0.07, 0.93):
        tx = w * sx
        lean = 8 if sx < 0.5 else -8
        cv.line([(tx, h * 0.86), (tx + lean, h * 0.42)], hexc("7A4A2E"), 8)
        for k in range(4):
            cv.ellipse(tx + lean * 0.5 + (k - 1.5) * 10, h * (0.42 + k * 0.08), 24, 10, fill=hexc("4E6B3A"), outline=INK,
                       width=1.5)
    cv.rect(1.5, 1.5, w - 1.5, h - 1.5, outline=INK, width=3)
    return cv.result()


def calendar(w=768, h=1152):
    cv = Canvas(w, h, ss=2, bg=hexc("F7F2E6"))
    red, blue = hexc("C8392F"), hexc("2F5A9A")
    cv.rect(0, 0, w, 70, fill=hexc("2F5A4F"))
    cv.text((w / 2, 36), "행복약국  HAPPY PHARMACY", gothic(cv, 30, True), (250, 244, 228), anchor="mm")
    art = sun_moon_peaks(w - 60, 400)
    cv.img.paste(art.resize(((w - 60) * cv.ss, 400 * cv.ss), Image.LANCZOS), (30 * cv.ss, 90 * cv.ss))
    cv.text((w / 2, 505), "서울시 마포구 성미산로 12  ·  02-555-0118  ·  약은 약사에게", gothic(cv, 14), INK, anchor="ma")
    cv.text((40, 530), "10", cv.font("Arial Black.ttf", 64), red)
    cv.text((140, 548), "OCTOBER", cv.font("Arial Black.ttf", 30), INK)
    cv.text((w - 40, 548), "2026", cv.font("Arial Black.ttf", 30), INK, anchor="ra")
    cv.text((140, 590), "시월  ·  음력 8월 - 9월", gothic(cv, 16), INK)
    days = ["일", "월", "화", "수", "목", "금", "토"]
    gx0, gy0 = 30, 640
    cw, chh = (w - 60) / 7, 88
    for i, dname in enumerate(days):
        col = red if i == 0 else (blue if i == 6 else INK)
        cv.text((gx0 + cw * i + cw / 2, gy0), dname, gothic(cv, 20, True), col, anchor="ma")
    rule(cv, gx0, w - 30, gy0 + 30, INK, 1.2)
    fn, fp = cv.font("Arial Black.ttf", 40), gothic(cv, 11)
    first = datetime.date(2026, 10, 1)
    start_col = (first.weekday() + 1) % 7

    def cell_xy(day):
        idx = start_col + day - 1
        return gx0 + cw * (idx % 7) + cw / 2, gy0 + 42 + (idx // 7) * chh

    for day in range(1, 32):
        c = (start_col + day - 1) % 7
        x, y = cell_xy(day)
        col = red if (c == 0 or day in HOLIDAYS) else (blue if c == 6 else INK)
        cv.text((x, y), str(day), fn, col, anchor="ma")
        lm, ld = lunar(day)
        cv.text((x, y + 50), HOLIDAYS.get(day, "%d.%d" % (lm, ld)), fp, col, anchor="ma")
    for r in range(1, 6):
        rule(cv, gx0, w - 30, gy0 + 36 + r * chh, mix(INK, (247, 242, 230), 0.75), 0.8)

    hand = cv.font("Noteworthy.ttc", 17, index=1)
    rng = np.random.default_rng(5)
    x, y = cell_xy(8)
    cv.line(wobble([(x + 36 * math.cos(a), y + 26 + 30 * math.sin(a)) for a in np.linspace(0, 6.6, 40)], 1.2, rng),
            BLUE_PEN, 2.2)
    cv.text((x - 40, y + 62), "Dentist", hand, BLUE_PEN)
    x, y = cell_xy(14)
    cv.text((x - 40, y + 62), "Gas check", hand, BLUE_PEN)
    x, y = cell_xy(17)
    for k in range(5):
        a1 = -math.pi / 2 + k * 2 * math.pi / 5
        a2 = a1 + 2 * math.pi * 2 / 5
        cv.line([(x + 30 + 12 * math.cos(a1), y + 4 + 12 * math.sin(a1)), (x + 30 + 12 * math.cos(a2), y + 4 + 12 * math.sin(a2))],
                RED_PEN, 2)
    cv.text((x - 38, y + 62), "Mom b-day", hand, RED_PEN)
    x, y = cell_xy(25)
    cv.text((x - 30, y + 62), "Rent", hand, BLUE_PEN)
    cv.line([(x + 22, y + 8), (x + 30, y + 18), (x + 46, y - 6)], BLUE_PEN, 3)
    return save(paper_tone(cv.result(), 222, 0.05), "T_Calendar")


# ---------------------------------------------------------------- notes, photos, labels

def lined_paper(cv, x0, y0, x1, y1, bg, line_col, spacing=26, margin_col=None):
    cv.rect(x0, y0, x1, y1, fill=bg)
    y = y0 + spacing * 1.6
    while y < y1 - 6:
        rule(cv, x0 + 6, x1 - 6, y, line_col, 1.0)
        y += spacing
    if margin_col:
        cv.line([(x0 + 52, y0 + 4), (x0 + 52, y1 - 4)], margin_col, 1.2, caps=False)


def notes_atlas():
    """Papers on the cork board and in the drawer (layout: atlas.NOTES)."""
    size = atlas.SIZES["notes"][0]
    R = atlas.NOTES
    cv = Canvas(size, size, ss=2, bg=(250, 250, 250))
    hand = cv.font("Noteworthy.ttc", 26, index=1)
    hand_s = cv.font("Noteworthy.ttc", 21, index=1)
    marker = cv.font("MarkerFelt.ttc", 30)

    x0, y0, x1, y1 = R["numbers"]
    lined_paper(cv, x0, y0, x1, y1, hexc("F8F4E8"), hexc("A9C3DA"), 30, hexc("E0A0A0"))
    cv.text((x0 + 70, y0 + 18), "IMPORTANT NUMBERS", marker, RED_PEN)
    rows = [("Police", "112"), ("Fire / Ambulance", "119"), ("Voice phishing (FSS)", "1332"), ("Nuri Bank", "1599-0000"),
            ("Landlord", "010-5512-3380"), ("Mom", "010-2231-7745"), ("Dad", "010-4418-0902")]
    y = y0 + 64
    for a, b in rows:
        cv.text((x0 + 66, y), a, hand, BLUE_PEN)
        cv.text((x1 - 14, y), b, hand, BLUE_PEN, anchor="ra")
        y += 60
    cv.line([(x0 + 300, y0 + 238), (x1 - 12, y0 + 234)], RED_PEN, 2.2)

    x0, y0, x1, y1 = R["notice"]
    cx = (x0 + x1) / 2
    cv.rect(x0, y0, x1, y1, fill=hexc("FBFBF6"))
    cv.text((cx, y0 + 30), "관리사무소 안내", gothic(cv, 28, True), INK, anchor="ma")
    cv.text((cx, y0 + 70), "BUILDING NOTICE", cv.font("AmericanTypewriter.ttc", 18), INK, anchor="ma")
    rule(cv, x0 + 48, x1 - 48, y0 + 100, INK, 1.5)
    tb = cv.font("AmericanTypewriter.ttc", 17)
    lines = ["Gas safety inspection:", "Wed 14 Oct, 10:00 - 17:00", "", "Inspectors never ask for money", "or bank details.",
             "", "Strange visitors or calls?", "Call the office: 02-555-0192"]
    y = y0 + 118
    for ln in lines:
        cv.text((x0 + 48, y), ln, tb, INK)
        y += 30
    cv.text((x1 - 48, y0 + 400), "Management office", tb, INK, anchor="ra")
    cv.ellipse(x1 - 90, y0 + 455, 38, 38, outline=hexc("B8453A"), width=3)
    cv.text((x1 - 90, y0 + 455), "관리", gothic(cv, 20, True), hexc("B8453A"), anchor="mm")

    x0, y0, x1, y1 = R["sticky_y"]
    cv.rect(x0, y0, x1, y1, fill=hexc("F7E27A"))
    cv.text((x0 + 22, y0 + 40), "Phone bill", hand, INK)
    cv.text((x0 + 22, y0 + 80), "pay by 20th", hand, INK)
    cv.text((x0 + 22, y0 + 130), "auto-pay?", hand_s, RED_PEN)
    x0, y0, x1, y1 = R["sticky_p"]
    cv.rect(x0, y0, x1, y1, fill=hexc("F5B3C2"))
    cv.text((x0 + 22, y0 + 40), "Mom b-day", hand, INK)
    cv.text((x0 + 22, y0 + 80), "17th!", hand, INK)
    cv.text((x0 + 22, y0 + 124), "call + gift", hand_s, INK)

    x0, y0, x1, y1 = R["strip"]
    strip = pp.photo_strip((x1 - x0, y1 - y0))
    cv.img.paste(strip.resize(((x1 - x0) * cv.ss, (y1 - y0) * cv.ss), Image.NEAREST), (x0 * cv.ss, y0 * cv.ss))

    x0, y0, x1, y1 = R["receipt"]
    cv.rect(x0, y0, x1, y1, fill=hexc("F6F4EE"))
    mono = cv.font("Courier New Bold.ttf", 17)
    mono_s = cv.font("Courier New.ttf", 14)
    cx = (x0 + x1) / 2
    cv.text((cx, y0 + 18), "DAILY 24", cv.font("Arial Black.ttf", 26), INK, anchor="ma")
    cv.text((cx, y0 + 56), "망원점  02-555-0133", gothic(cv, 14), INK, anchor="ma")
    cv.text((cx, y0 + 78), "2026-10-05  22:41", mono_s, INK, anchor="ma")
    y = y0 + 110
    items = [("Triangle gimbap", "1,500"), ("Banana milk", "1,800"), ("Cup ramyun", "1,300"), ("Chocolate", "1,200"),
             ("Plastic bag", "50")]
    for a, b in [("-" * 22, "")] + items + [("-" * 22, "")]:
        cv.text((x0 + 14, y), a, mono_s, INK)
        cv.text((x1 - 14, y), b, mono_s, INK, anchor="ra")
        y += 24
    cv.text((x0 + 14, y + 4), "TOTAL", mono, INK)
    cv.text((x1 - 14, y + 2), "5,850원", gothic(cv, 18, True), INK, anchor="ra")
    y += 40
    for ln in ("CARD   Nuri Bank", "NO.    9410-****-****-0921", "APPROVAL 38120477"):
        cv.text((x0 + 14, y), ln, mono_s, INK)
        y += 22
    rng = np.random.default_rng(12)
    bx = x0 + 30
    while bx < x1 - 30:
        wd = rng.choice([1.2, 2.0, 3.0])
        cv.rect(bx, y + 18, bx + wd, y + 78, fill=INK)
        bx += wd + rng.choice([1.5, 2.5, 3.5])
    cv.text((cx, y + 96), "감사합니다  THANK YOU", gothic(cv, 14, True), INK, anchor="ma")

    x0, y0, x1, y1 = R["ticket"]
    stub = x0 + 360
    cv.rect(x0, y0, x1, y1, fill=hexc("E9DDC0"))
    cv.rect(x0, y0, stub, y1, fill=hexc("2F4A6E"))
    cream = (242, 232, 210)
    cv.text((x0 + 20, y0 + 30), "THE RAINY SEASONS", cv.font("DIN Condensed Bold.ttf", 36), cream)
    cv.text((x0 + 20, y0 + 76), "LIVE IN HONGDAE", cv.font("DIN Condensed Bold.ttf", 26), hexc("E8B45A"))
    cv.text((x0 + 20, y0 + 124), "2026.11.07 SAT  19:00", cv.font("Courier New Bold.ttf", 18), cream)
    cv.text((x0 + 20, y0 + 160), "STANDING  A-128", cv.font("Courier New Bold.ttf", 18), cream)
    cv.text((x0 + 20, y0 + 200), "비 오는 계절 단독 공연", gothic(cv, 16, True), cream)
    for k in range(10):
        cv.dot(stub, y0 + 12 + k * 25, 3, hexc("E9DDC0"))
    cv.text(((stub + x1) / 2, y0 + 60), "55,000원", gothic(cv, 20, True), INK, anchor="ma")
    cv.text(((stub + x1) / 2, y0 + 112), "ADMIT", cv.font("DIN Condensed Bold.ttf", 26), INK, anchor="ma")
    cv.text(((stub + x1) / 2, y0 + 142), "ONE", cv.font("DIN Condensed Bold.ttf", 26), INK, anchor="ma")
    return save(paper_tone(cv.result(), 333, 0.05), "T_Notes_Atlas")


def photos_atlas():
    """Family photos (layout: atlas.PHOTOS); the ID photo sits in the middle of its quadrant."""
    size = atlas.SIZES["photos"][0]
    h = size // 2
    out = Image.new("RGB", (size, size))
    out.paste(pp.graduation_kr((h, h)), (0, 0))
    out.paste(pp.wedding_kr((h, h)), (h, 0))
    out.paste(pp.family_kr((h, h)), (0, h))
    out.paste(pp.id_photo_kr((h, h)), (h, h))
    return save(out, "T_Photos_Atlas")


def labels_atlas():
    """Printed labels and screens for small props (layout: atlas.LABELS)."""
    size = atlas.SIZES["labels"][0]
    R = atlas.LABELS
    cv = Canvas(size, size, ss=2, bg=(245, 245, 245))
    cream = (250, 240, 220)

    x0, y0, x1, y1 = R["ramyun"]
    cx = (x0 + x1) / 2
    cv.rect(x0, y0, x1, y1, fill=hexc("C23A2E"))
    cv.rect(x0, y0 + 360, x1, y1, fill=hexc("E9C04E"))
    cv.text((cx, y0 + 120), "라면", gothic(cv, 120, True), cream, anchor="mm")
    cv.text((cx, y0 + 232), "HOT & SPICY RAMYUN", cv.font("Arial Black.ttf", 30), cream, anchor="mm")
    cv.text((cx, y0 + 290), "매운맛 · 65g", gothic(cv, 26, True), cream, anchor="mm")
    cv.ellipse(cx, y0 + 430, 120, 40, fill=hexc("F2EAD8"), outline=INK, width=3)
    for k in range(9):
        cv.line([(cx - 96 + k * 22, y0 + 410), (cx - 86 + k * 22, y0 + 435), (cx - 96 + k * 22, y0 + 450)], hexc("E0B45A"), 5)
    cv.ellipse(cx - 46, y0 + 415, 18, 12, fill=hexc("C23A2E"), outline=INK, width=2)
    cv.ellipse(cx + 44, y0 + 420, 20, 10, fill=hexc("5E8A4E"), outline=INK, width=2)

    x0, y0, x1, y1 = R["laptop"]
    cx = (x0 + x1) / 2
    for i in range(16):
        t = i / 15
        cv.rect(x0, y0 + (y1 - y0) * i / 16, x1, y0 + (y1 - y0) * (i + 1) / 16 + 1, fill=mix(hexc("2F4A6E"), hexc("C8653E"), t * 0.8))
    for k in range(5):
        px = x0 + 40 + k * 110
        cv.poly([(px - 90, y1), (px, y1 - 70 - (k % 2) * 30), (px + 90, y1)], fill=mix(hexc("1E2A40"), hexc("2F4A6E"), k * 0.15))
    cv.text((cx, y0 + 88), "10:32", cv.font("Arial Rounded Bold.ttf", 72), (240, 236, 228), anchor="mm")
    cv.text((cx, y0 + 146), "Tuesday, October 6", cv.font("Arial.ttf", 22), (220, 226, 236), anchor="mm")

    x0, y0, x1, y1 = R["thermostat"]
    cv.rect(x0, y0, x1, y1, fill=hexc("E8E4DA"))
    cv.rect(x0 + 30, y0 + 28, x0 + 300, y0 + 150, fill=hexc("2A3A36"), outline=INK, width=3)
    cv.text((x0 + 165, y0 + 66), "난방  HEAT", gothic(cv, 20, True), hexc("9FE0C0"), anchor="mm")
    cv.text((x0 + 165, y0 + 116), "22.5°C", cv.font("DIN Condensed Bold.ttf", 50), hexc("9FE0C0"), anchor="mm")
    for k, lab in enumerate(("온수", "난방", "외출", "예약")):
        bx, by = x0 + 330, y0 + 22 + k * 56
        cv.rect(bx, by, bx + 150, by + 44, fill=hexc("D5D0C4"), outline=INK, width=1.5)
        cv.text((bx + 75, by + 22), lab, gothic(cv, 20, True), INK, anchor="mm")
    cv.rect(x0 + 30, y0 + 172, x0 + 300, y0 + 232, fill=hexc("D5D0C4"), outline=INK, width=1.5)
    cv.text((x0 + 165, y0 + 202), "▲    ▼", gothic(cv, 24, True), INK, anchor="mm")

    x0, y0, x1, y1 = R["remote"]
    cv.rect(x0, y0, x1, y1, fill=hexc("2A2A30"))
    cv.ellipse(x0 + 56, y0 + 36, 14, 14, fill=hexc("D8413A"))
    for r in range(4):
        for c in range(3):
            cv.ellipse(x0 + 88 + c * 40, y0 + 78 + r * 34, 13, 10, fill=hexc("6A6A74"))
    for i, col in enumerate(("D8413A", "4FAF5A", "E3C040", "3A78C8")):
        cv.rect(x0 + 64 + i * 34, y0 + 224, x0 + 88 + i * 34, y0 + 240, fill=hexc(col))

    x0, y0, x1, y1 = R["card_front"]
    cv.rect(x0 - 8, y0 - 48, x1 + 8, y1 + 56, fill=hexc("F0F0F0"))
    for i in range(20):
        cv.rect(x0, y0 + (y1 - y0) * i / 20, x1, y0 + (y1 - y0) * (i + 1) / 20 + 1,
                fill=mix(hexc("2F5A4F"), hexc("4F8A76"), i / 19))
    cv.text((x0 + 14, y0 + 12), "누리은행", gothic(cv, 20, True), (255, 255, 255))
    cv.text((x0 + 128, y0 + 15), "NURI BANK", cv.font("Arial Black.ttf", 14), (235, 245, 238))
    cv.rect(x0 + 16, y0 + 54, x0 + 52, y0 + 80, fill=hexc("E3C170"), outline=INK, width=1.2)
    cv.text((x0 + 16, y0 + 94), "9410 2231 7745 0921", cv.font("Courier New Bold.ttf", 15), (255, 255, 255))
    cv.text((x0 + 16, y0 + 124), "KIM JIWOO   CHECK", cv.font("Arial Bold.ttf", 13), (230, 245, 238))

    x0, y0, x1, y1 = R["card_back"]
    cv.rect(x0 - 8, y0 - 48, x1 + 8, y1 + 56, fill=hexc("F0F0F0"))
    cv.rect(x0, y0, x1, y1, fill=hexc("3C6E60"))
    cv.rect(x0, y0 + 18, x1, y0 + 46, fill=(20, 20, 24))
    cv.rect(x0 + 12, y0 + 60, x0 + 180, y0 + 84, fill=(245, 245, 240))
    cv.text((x0 + 16, y0 + 64), "Kim Jiwoo", cv.font("Noteworthy.ttc", 15, index=1), BLUE_PEN)
    cv.text((x0 + 12, y0 + 94), "고객센터 CUSTOMER CENTER", gothic(cv, 11, True), (255, 255, 255))
    cv.text((x0 + 12, y0 + 112), "1599-0000", cv.font("Arial Black.ttf", 24), hexc("FFE28A"))

    x0, y0, x1, y1 = R["rulebook"]
    cv.rect(x0 - 32, y0 - 86, x1 + 32, y1 + 70, fill=hexc("B98D5C"))
    cv.rect(x0, y0, x1, y1, fill=hexc("FBF6E8"), outline=INK, width=2)
    cv.text(((x0 + x1) / 2, y0 + 26), "RULES", cv.font("Arial Bold.ttf", 16), INK, anchor="mm")
    cv.text(((x0 + x1) / 2, y0 + 66), "Stay safe", cv.font("Noteworthy.ttc", 26, index=1), BLUE_PEN, anchor="mm")

    x0, y0, x1, y1 = R["tissue"]
    cv.rect(x0, y0, x1, y1, fill=hexc("E9D2DA"))
    rng = np.random.default_rng(4)
    for _ in range(26):
        x, y = rng.uniform(x0 + 8, x1 - 8), rng.uniform(y0 + 8, y1 - 8)
        for pt in range(5):
            a = pt * 1.2566
            cv.ellipse(x + math.cos(a) * 9, y + math.sin(a) * 9, 8, 6, fill=(255, 255, 255), rot=a)
        cv.dot(x, y, 4, hexc("E9A447"))
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    cv.rect(cx - 128, cy - 35, cx + 128, cy + 35, fill=(255, 255, 255), outline=INK, width=2)
    cv.text((cx, cy), "순수 TISSUE", gothic(cv, 30, True), hexc("B45A76"), anchor="mm")

    x0, y0, x1, y1 = R["aircon"]
    cv.rect(x0, y0, x1, y1, fill=hexc("EDE8DF"))
    cv.rect(x0 + 48, y0 + 82, x0 + 248, y0 + 172, fill=hexc("20242A"), outline=INK, width=3)
    cv.text((x0 + 148, y0 + 127), "24°", cv.font("DIN Condensed Bold.ttf", 64), hexc("7CE0A8"), anchor="mm")
    cv.text((x0 + 368, y0 + 102), "COOLWIND", cv.font("Arial Black.ttf", 30), hexc("3E5476"), anchor="mm")
    cv.text((x0 + 368, y0 + 147), "에어컨  INVERTER", gothic(cv, 20, True), hexc("3E5476"), anchor="mm")
    return save(cv.result(), "T_Labels_Atlas")


# ---------------------------------------------------------------- posters, rug, clock, TV, books, leaves

def posters_atlas():
    """Four fictional posters, 512 x 1024 each (layout: atlas.POSTERS)."""
    w, h = atlas.SIZES["posters"]
    cv = Canvas(w, h, ss=2, bg=(240, 236, 228))
    rng = np.random.default_rng(8)
    # 1: concert poster, screen-print style
    x0 = 0
    cv.rect(x0, 0, x0 + 512, h, fill=hexc("E9DDC0"))
    cv.ellipse(x0 + 256, 330, 190, 190, fill=hexc("C8453A"))
    for k in range(9):
        cv.line([(x0 + 60 + k * 45, 120), (x0 + 40 + k * 45, 560)], hexc("3E5476"), 5)
    cv.poly([(x0 + 150, 330), (x0 + 256, 200), (x0 + 362, 330)], fill=hexc("2F3A52"), outline=INK, width=3)
    cv.line([(x0 + 256, 330), (x0 + 256, 470), (x0 + 236, 490)], INK, 8)
    cv.text((x0 + 256, 640), "THE RAINY", cv.font("DIN Condensed Bold.ttf", 96), hexc("2F3A52"), anchor="mm")
    cv.text((x0 + 256, 730), "SEASONS", cv.font("DIN Condensed Bold.ttf", 96), hexc("2F3A52"), anchor="mm")
    cv.text((x0 + 256, 820), "LIVE IN HONGDAE", cv.font("Arial Black.ttf", 30), hexc("C8453A"), anchor="mm")
    cv.text((x0 + 256, 870), "2026.11.07  SAT  19:00  ·  ROLLING HALL", cv.font("Arial Bold.ttf", 16), INK, anchor="mm")
    cv.text((x0 + 256, 920), "비 오는 계절 단독 공연", gothic(cv, 22, True), INK, anchor="mm")
    # 2: retro movie poster
    x0 = 512
    for i in range(24):
        t = i / 23
        cv.rect(x0, h * i / 24, x0 + 512, h * (i + 1) / 24 + 1, fill=mix(hexc("2A2F4A"), hexc("B8553E"), t))
    for k in range(7):
        bx = x0 + 20 + k * 70
        bh = rng.uniform(160, 320)
        cv.rect(bx, 700 - bh, bx + 55, 700, fill=hexc("1E2236"))
        for wy in range(int(700 - bh) + 12, 690, 22):
            if rng.random() < 0.55:
                cv.rect(bx + 10, wy, bx + 20, wy + 10, fill=hexc("E9C04E"))
    cv.rect(x0, 700, x0 + 512, h, fill=hexc("1E2236"))
    cv.poly([(x0 + 150, 760), (x0 + 190, 715), (x0 + 330, 715), (x0 + 370, 760), (x0 + 380, 800), (x0 + 140, 800)],
            fill=hexc("E3B04A"), outline=INK, width=3)
    cv.rect(x0 + 235, 700, x0 + 285, 716, fill=hexc("F2ECDD"), outline=INK, width=2)
    for wx in (x0 + 185, x0 + 335):
        cv.ellipse(wx, 800, 24, 24, fill=INK)
    cv.text((x0 + 256, 120), "MIDNIGHT", cv.font("DIN Condensed Bold.ttf", 110), hexc("F2ECDD"), anchor="mm")
    cv.text((x0 + 256, 225), "TAXI", cv.font("DIN Condensed Bold.ttf", 110), hexc("E3B04A"), anchor="mm")
    cv.text((x0 + 256, 300), "심야택시", gothic(cv, 40, True), hexc("F2ECDD"), anchor="mm")
    cv.text((x0 + 256, 930), "IN THEATRES THIS WINTER", cv.font("Arial Bold.ttf", 20), hexc("F2ECDD"), anchor="mm")
    # 3: Jeju travel poster
    x0 = 1024
    cv.rect(x0, 0, x0 + 512, h, fill=hexc("A9C4D4"))
    cv.ellipse(x0 + 380, 190, 70, 70, fill=hexc("F2D27A"))
    cv.poly([(x0 - 20, 620), (x0 + 256, 330), (x0 + 540, 620)], fill=hexc("5E7A5A"), outline=INK, width=3)
    cv.poly([(x0 + 200, 390), (x0 + 256, 330), (x0 + 312, 390)], fill=hexc("E9E4D8"))
    cv.rect(x0, 620, x0 + 512, h, fill=hexc("3E6E9A"))
    for k in range(6):
        y = 650 + k * 40
        cv.line([(x0 + t * 512, y + math.sin(t * 30 + k) * 6) for t in np.linspace(0, 1, 50)], hexc("E9EEF2"), 3, caps=False)
    for k in range(5):
        cv.ellipse(x0 + 80 + k * 90, 880, 34, 30, fill=hexc("E8883A"), outline=INK, width=2)
        cv.ellipse(x0 + 90 + k * 90, 852, 12, 6, fill=hexc("5E8A4E"), rot=-0.5)
    cv.text((x0 + 256, 110), "JEJU", cv.font("Arial Black.ttf", 110), hexc("2F3A52"), anchor="mm")
    cv.text((x0 + 256, 960), "제주 · ISLAND OF WIND", gothic(cv, 28, True), hexc("F2ECDD"), anchor="mm")
    # 4: class timetable
    x0 = 1536
    cv.rect(x0, 0, x0 + 512, h, fill=hexc("F6F1E4"))
    cv.text((x0 + 256, 70), "2026 2학기 시간표", gothic(cv, 40, True), INK, anchor="mm")
    days = ["", "월", "화", "수", "목", "금"]
    cw, rh = 512 / 6, 110
    for i, dname in enumerate(days):
        cv.text((x0 + cw * i + cw / 2, 140), dname, gothic(cv, 24, True), INK, anchor="mm")
    classes = {(1, 0): ("Stats", "C8653E"), (3, 0): ("Stats", "C8653E"), (2, 1): ("Korean Lit", "4F6F98"),
               (4, 1): ("Korean Lit", "4F6F98"), (1, 2): ("Design", "E3B04A"), (5, 3): ("Seminar", "5E8A4E"),
               (2, 4): ("English", "A8473A"), (4, 3): ("Design", "E3B04A")}
    for r in range(6):
        cv.text((x0 + cw / 2, 200 + r * rh + rh / 2), str(r + 1), gothic(cv, 24, True), INK, anchor="mm")
        for c in range(1, 6):
            bx, by = x0 + cw * c + 4, 170 + r * rh + 4
            cv.rect(bx, by, bx + cw - 8, by + rh - 8, outline=mix(INK, (246, 241, 228), 0.6), width=1)
            if (c, r) in classes:
                name, col = classes[(c, r)]
                cv.rect(bx + 3, by + 3, bx + cw - 11, by + rh - 11, fill=hexc(col))
                cv.text((bx + (cw - 8) / 2, by + (rh - 8) / 2), name, cv.font("Noteworthy.ttc", 17, index=1), (250, 246, 236),
                        anchor="mm")
    img = cv.result()
    arr = np.asarray(img).astype(np.float32) * (0.93 + 0.08 * fbm(w, h, 88))[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), "T_Posters_Atlas")


def rug_round(size=1024):
    cv = Canvas(size, size, ss=2, mode="RGBA", bg=(0, 0, 0, 0))
    c = size / 2
    rings = [(0.5, "8C3E32"), (0.46, "E3CFA2"), (0.42, "3E5476"), (0.3, "E3CFA2"), (0.26, "B8863A"), (0.16, "8C3E32"),
             (0.08, "E3CFA2")]
    for r, col in rings:
        cv.ellipse(c, c, size * r, size * r, fill=hexc(col) + (255,), outline=INK + (255,), width=2.5, n=160)
    for k in range(16):
        a = k * math.pi / 8
        cv.ellipse(c + math.cos(a) * size * 0.36, c + math.sin(a) * size * 0.36, 22, 10, fill=hexc("E3CFA2") + (255,),
                   outline=INK + (255,), width=1.5, rot=a)
    for k in range(40):
        a = k * math.pi / 20
        cv.dot(c + math.cos(a) * size * 0.48, c + math.sin(a) * size * 0.48, 5, hexc("E3CFA2") + (255,))
    img = cv.result()
    weave = np.asarray(Image.open(os.path.join(OUT, "T_Ink_Fabric.png")).convert("L").resize((size, size))).astype(np.float32) / 255
    arr = np.asarray(img).astype(np.float32)
    arr[..., :3] *= (0.3 + 0.7 * weave)[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA"), "T_Rug_Round")


def clock_face(size=512):
    cv = Canvas(size, size, ss=3, bg=hexc("F3ECD8"))
    c = size / 2
    cv.ellipse(c, c, c - 4, c - 4, fill=hexc("F4EDDA"), outline=hexc("3E5476"), width=24)
    for i in range(60):
        a = i * math.pi / 30
        r0 = c - 34 if i % 5 else c - 48
        cv.line([(c + math.cos(a) * r0, c + math.sin(a) * r0), (c + math.cos(a) * (c - 24), c + math.sin(a) * (c - 24))],
                INK, 4 if i % 5 == 0 else 1.6)
    f = cv.font("Arial Black.ttf", 50)
    for n in range(1, 13):
        a = n * math.pi / 6 - math.pi / 2
        cv.text((c + math.cos(a) * (c - 92), c + math.sin(a) * (c - 92)), str(n), f, INK, anchor="mm")
    cv.text((c, c + 72), "행운시계", gothic(cv, 28, True), hexc("3E5476"), anchor="mm")
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


# ---------------------------------------------------------------- the view from the window

def backdrop(w=2048, h=1024):
    rng = np.random.default_rng(41)
    BLUE = hexc("48485E")
    cv = Canvas(w, h, ss=2, bg=(255, 255, 255))
    for i in range(40):
        t = i / 39
        cv.rect(0, h * 0.75 * i / 40, w, h * 0.75 * (i + 1) / 40 + 1, fill=mix(hexc("A6B6C7"), hexc("EFDBBE"), t))
    cv.rect(0, h * 0.74, w, h, fill=hexc("EFDBBE"))
    ridge = [(x, 470 - 120 * math.exp(-((x - 700) / 380) ** 2) - 60 * math.exp(-((x - 1500) / 300) ** 2)
              + 12 * math.sin(x * 0.02)) for x in range(0, w + 16, 16)]
    cv.poly([(0, h)] + ridge + [(w, h)], fill=hexc("8FA0A6"), outline=BLUE, width=2)
    for cx, cy, s in ((260, 160, 1.3), (1100, 110, 1.0), (1760, 190, 1.4)):
        blobs = [(-70, 0, 50), (-25, -22, 52), (30, -12, 48), (75, 6, 36), (0, 12, 50)]
        for bx, by, br in blobs:
            cv.ellipse(cx + bx * s, cy + by * s, br * s, br * s * 0.62, fill=hexc("F2E9D8"), outline=BLUE, width=2)
        for bx, by, br in blobs:
            cv.ellipse(cx + bx * s, cy + by * s + 2, br * s - 3, br * s * 0.62 - 3, fill=hexc("F2E9D8"))
    x = -30
    num = 101
    while x < w:
        bw = rng.uniform(150, 240)
        bh = rng.uniform(260, 420)
        top = 720 - bh
        col = [hexc("D8D2C6"), hexc("C6CDD2"), hexc("DCCBB6")][int(rng.integers(0, 3))]
        cv.rect(x, top, x + bw, 720, fill=col, outline=BLUE, width=2)
        for wy in np.arange(top + 18, 710, 24):
            cv.rect(x + 10, wy, x + bw - 10, wy + 5, fill=shade(col, 0.82))
        cv.text((x + bw / 2, top + 36), str(num), cv.font("Arial Black.ttf", 34), hexc("B8453A"), anchor="mm")
        num += 1
        x += bw + rng.uniform(40, 120)
    cx = 1320
    cv.rect(cx - 6, 390, cx + 6, 470, fill=hexc("C8453A"), outline=BLUE, width=1.5)
    cv.rect(cx - 24, 410, cx + 24, 422, fill=hexc("C8453A"), outline=BLUE, width=1.5)
    cv.rect(cx - 40, 470, cx + 40, 720, fill=hexc("E3D8C6"), outline=BLUE, width=2)
    x = -60
    while x < w:
        bw = rng.uniform(300, 440)
        top = rng.uniform(640, 720)
        brick = [hexc("A8563E"), hexc("9A4E3A"), hexc("B5654A")][int(rng.integers(0, 3))]
        cv.rect(x, top, x + bw, h, fill=brick, outline=BLUE, width=2)
        for yy in np.arange(top + 10, h, 14):
            cv.line([(x, yy), (x + bw, yy)], shade(brick, 0.85), 1.2, caps=False)
        for fl in range(3):
            wy = top + 40 + fl * 110
            for wx in (x + bw * 0.15, x + bw * 0.6):
                cv.rect(wx, wy, wx + bw * 0.22, wy + 60, fill=hexc("4F5E82"), outline=BLUE, width=2)
                for k in range(5):
                    cv.line([(wx + k * bw * 0.055, wy), (wx + k * bw * 0.055, wy + 60)], BLUE, 1.2, caps=False)
        cv.ellipse(x + bw * 0.8, top - 26, 34, 26, fill=hexc("5E8A6E"), outline=BLUE, width=2)
        cv.rect(x + bw * 0.8 - 34, top - 26, x + bw * 0.8 + 34, top, fill=hexc("5E8A6E"), outline=BLUE, width=2)
        x += bw + rng.uniform(10, 40)
    for pole_x in (380, 1640):
        cv.rect(pole_x - 9, 300, pole_x + 9, h, fill=hexc("9E9890"), outline=BLUE, width=2)
        cv.rect(pole_x - 70, 330, pole_x + 70, 344, fill=hexc("9E9890"), outline=BLUE, width=2)
    for k, off in enumerate((-60, -20, 20, 60, 90)):
        for (a, b) in ((-200, 380), (380, 1640), (1640, 2300)):
            y0 = 324 + k * 3
            pts = [(a + off + (b - a) * t, y0 + math.sin(math.pi * t) * (70 + k * 8)) for t in np.linspace(0, 1, 40)]
            cv.line(pts, BLUE, 1.6, caps=False)
    img = cv.result()
    arr = np.asarray(img).astype(np.float32) * (0.96 + 0.05 * fbm(w, h, 66))[..., None]
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), "T_Backdrop_Street")


def run():
    return [newspaper(), calendar(), notes_atlas(), photos_atlas(), labels_atlas(), posters_atlas(), rug_round(),
            clock_face(), tv_screen(), book_spines(), leaves_atlas(), backdrop()]


if __name__ == "__main__":
    for p in run():
        print(p)
