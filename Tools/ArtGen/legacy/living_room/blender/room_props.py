"""Small props: table-top items, bulletin board, frames, clock, plants, shoes,
helmet, umbrella, cables and the drawer contents."""
import math

import bmesh

from dcm import (box, cable, cushion, cylinder, extrude_shape, group, join, lathe, mesh_object, plane, smooth, sphere,
                 uv_job)

# Atlas regions (u0, v0, u1, v1).
NOTES = {"numbers": (0.0, 0.5, 0.5, 1.0), "rt": (0.5, 0.5, 1.0, 1.0), "sticky_y": (0.0, 0.25, 0.25, 0.5),
         "sticky_p": (0.25, 0.25, 0.5, 0.5), "receipt": (0.5, 0.0, 0.75, 0.5), "card": (0.75, 0.25, 1.0, 0.5),
         "drawing": (0.0, 0.0, 0.5, 0.25), "snapshot": (0.75, 0.0, 1.0, 0.25)}
PHOTOS = {"graduation": (0.0, 0.5, 0.5, 1.0), "wedding": (0.5, 0.5, 1.0, 1.0), "family": (0.0, 0.0, 0.5, 0.5),
          "dimas": (0.5, 0.0, 1.0, 0.5)}
LABELS = {"tin": (0.0, 0.5, 0.5, 1.0), "galon": (0.5, 0.75, 1.0, 1.0), "tissue": (0.5, 0.5, 1.0, 0.75),
          "remote": (0.0, 0.25, 0.25, 0.5), "card_front": (0.25, 0.25, 0.5, 0.5), "card_back": (0.5, 0.25, 0.75, 0.5),
          "rulebook": (0.75, 0.25, 1.0, 0.5), "fan": (0.0, 0.0, 0.5, 0.25), "mat": (0.5, 0.0, 1.0, 0.25)}
LEAVES = {"monstera": (0.0, 0.5, 0.5, 1.0), "pothos": (0.5, 0.5, 1.0, 1.0), "frond": (0.0, 0.0, 0.5, 0.5),
          "snake": (0.5, 0.0, 1.0, 0.5)}


def card(name, parent, w, h, pos, rot, mat, rect, bend=0.0):
    """Upright printed item facing local -Y."""
    p = plane(name, w, h, pos, mat, parent, rot=(90 + rot[0], rot[1], rot[2]), nx=4 if bend else 1, ny=1, bend=bend)
    uv_job(p, "planar_local", rect=rect)
    return p


def flat(name, parent, w, h, pos, rot_z, mat, rect, thickness=0.0):
    """Item lying flat, printed face up."""
    if thickness > 0:
        b = box(name, (w, h, thickness), pos, mat, parent, rot=(0, 0, rot_z), bev=min(0.002, thickness * 0.3))
    else:
        b = plane(name, w, h, pos, mat, parent, rot=(0, 0, rot_z))
    uv_job(b, "planar", axis="Z", rect=rect)
    return b


def leaf_card(name, parent, size, pos, rot, rect, curl=0.02):
    p = plane(name, size, size, pos, "M_Leaves", parent, rot=rot, nx=4, ny=4, bend=curl)
    uv_job(p, "planar_local", rect=rect)
    return p


# ---------------------------------------------------------------- coffee table

def coffee_table_items(table):
    top = 0.4265
    g = group("TableItems", table)
    doily = plane("Doily", 0.36, 0.36, (0.02, 0.0, top + 0.0005), "M_Doily", g, rot=(0, 0, 12))
    vase(g, (0.02, 0.0, top + 0.001))

    news = group("INT_Newspaper", g, (-0.31, 0.02, top), (0, 0, 7))
    base = box("News_Back", (0.305, 0.415, 0.004), (0.003, -0.004, 0.002), "M_Paper_White", news, bev=0.001)
    front = box("News_Front", (0.3, 0.41, 0.004), (0, 0, 0.006), "M_Newspaper", news, bev=0.001)
    uv_job(front, "planar", axis="Z", rect=(0, 0, 1, 1))
    glasses(news, (0.02, 0.1, 0.009), 25)

    wallet(g, (-0.05, -0.215, top), -16)
    rulebook(g, (0.405, -0.115, top), 4)
    tea_set(g, (0.2, -0.17, top))
    biscuit_tin(g, (0.33, 0.155, top))
    remote = box("Remote", (0.05, 0.17, 0.02), (0.1, 0.225, top + 0.01), "M_Plastic_Black", g, rot=(0, 0, 72), bev=0.008)
    rtop = plane("Remote_Face", 0.044, 0.16, (0.1, 0.225, top + 0.0205), "M_Labels", g, rot=(0, 0, 72))
    uv_job(rtop, "planar_local", rect=LABELS["remote"], axis_local="Z")


def vase(parent, pos):
    g = group("Vase", parent, pos)
    lathe("Vase_Body", [(0, 0), (0.036, 0), (0.042, 0.012), (0.055, 0.06), (0.05, 0.1), (0.028, 0.145), (0.022, 0.17),
                        (0.032, 0.19), (0.03, 0.197), (0.018, 0.185), (0, 0.175)], (0, 0, 0), "M_Ceramic_Blue", g, segs=28)
    lathe("Vase_Band", [(0.0555, 0.055), (0.057, 0.055), (0.057, 0.07), (0.0555, 0.07)], (0, 0, 0), "M_Gold", g, segs=28)
    stems = []
    heads = [((0.0, 0.0, 0.42), "M_Flower_Red"), ((0.09, 0.03, 0.36), "M_Flower_Yellow"), ((-0.08, 0.05, 0.37), "M_Flower_Pink"),
             ((0.05, -0.08, 0.38), "M_Flower_Pink"), ((-0.05, -0.07, 0.35), "M_Flower_Yellow"), ((0.1, -0.02, 0.31), "M_Flower_Red"),
             ((-0.1, -0.01, 0.3), "M_Flower_Red")]
    for i, ((x, y, z), mat) in enumerate(heads):
        stems.append(cable("Vase_Stem%d" % i, [(0, 0, 0.16), (x * 0.3, y * 0.3, 0.24), (x * 0.9, y * 0.9, z - 0.03), (x, y, z)],
                           0.0025, "M_PlantStem", g, res=6, bevel_res=1))
        fg = group("Flower%d" % i, g, (x, y, z), (math.degrees(math.atan2(y, 1)) * 0.5, -math.degrees(math.atan2(x, 1)) * 0.5, i * 40))
        for k in range(6):
            a = k * math.pi / 3
            sphere("Petal%d_%d" % (i, k), 0.022, (math.cos(a) * 0.022, math.sin(a) * 0.022, 0), mat, fg, rot=(0, 0, math.degrees(a)),
                   seg=10, rings=6, scale=(1.0, 0.55, 0.22))
        sphere("FlowerCore%d" % i, 0.011, (0, 0, 0.004), "M_Flower_Yellow" if mat != "M_Flower_Yellow" else "M_Flower_Red", fg, seg=10, rings=6)
    join("Vase_Stems", stems)
    for k in range(5):
        a = k * 1.3
        leaf_card("Vase_Leaf%d" % k, g, 0.07, (math.cos(a) * 0.05, math.sin(a) * 0.05, 0.25 + k * 0.012),
                  (60, 0, math.degrees(a) + 90), LEAVES["pothos"], curl=0.005)


def glasses(parent, pos, rot_z):
    g = group("ReadingGlasses", parent, pos, (0, 0, rot_z))
    parts = []
    for sx in (-1, 1):
        ring = [(sx * 0.032 + math.cos(a) * 0.025, math.sin(a) * 0.019, 0.004) for a in [k * 2 * math.pi / 23 for k in range(24)]]
        parts.append(cable("Glasses_Rim%d" % sx, ring, 0.0022, "M_Metal_Black", g, res=4, bevel_res=1))
        parts.append(cable("Glasses_Temple%d" % sx, [(sx * 0.056, 0.0, 0.004), (sx * 0.06, 0.06, 0.003), (sx * 0.058, 0.12, 0.002),
                                                     (sx * 0.05, 0.135, 0.0)], 0.0018, "M_Metal_Black", g, res=4, bevel_res=1))
    parts.append(cable("Glasses_Bridge", [(-0.008, 0.0, 0.005), (0, 0.004, 0.008), (0.008, 0.0, 0.005)], 0.0018, "M_Metal_Black", g))
    join("Glasses_Frame", parts)
    for sx in (-1, 1):
        cylinder("Glasses_Lens%d" % sx, 0.023, 0.002, (sx * 0.032, 0, 0.003), "M_Glass_Cup", g, verts=20, anchor="center",
                 rot=(0, 0, 0))


def wallet(parent, pos, rot_z):
    g = group("INT_Wallet", parent, pos, (0, 0, rot_z))
    box("Wallet_Body", (0.115, 0.095, 0.02), (0, 0, 0.01), "M_Leather", g, bev=0.007, segs=3)
    box("Wallet_Flap", (0.112, 0.012, 0.006), (0, -0.045, 0.021), "M_Leather", g, bev=0.002)
    stitch = [(math.cos(a) * 0.05, math.sin(a) * 0.04, 0.0203) for a in [k * 2 * math.pi / 40 for k in range(41)]]
    cable("Wallet_Stitch", [(x * 1.03 if abs(x) > 0.04 else x, y, z) for x, y, z in stitch], 0.0007, "M_Paper_Envelope", g, res=2,
          bevel_res=0)
    cardp = box("Wallet_Card", (0.085, 0.054, 0.001), (0.012, 0.035, 0.012), "M_Labels", g, rot=(0, 0, 4), bev=0)
    uv_job(cardp, "planar", axis="Z", rect=LABELS["card_front"])
    box("Wallet_Snap", (0.014, 0.014, 0.003), (0.0, -0.04, 0.0245), "M_Metal_Chrome", g, bev=0.001)


def rulebook(parent, pos, rot_z):
    g = group("INT_Rulebook", parent, pos, (0, 0, rot_z))
    box("Rulebook_Cover", (0.152, 0.212, 0.014), (0, 0, 0.007), "M_Kraft", g, bev=0.004)
    box("Rulebook_Pages", (0.144, 0.206, 0.01), (0.004, 0, 0.007), "M_Paper_White", g, bev=0.001)
    lab = plane("Rulebook_Label", 0.1, 0.07, (0.0, 0.03, 0.0142), "M_Labels", g)
    uv_job(lab, "planar", axis="Z", rect=(0.781, 0.326, 0.969, 0.424))
    box("Rulebook_Band", (0.012, 0.214, 0.016), (0.05, 0, 0.007), "M_Plastic_Black", g, bev=0.002)
    pen = group("Pen", g, (-0.02, -0.05, 0.0175), (0, 90, 30))
    lathe("Pen_Body", [(0, 0), (0.0045, 0.0), (0.0045, 0.13), (0.002, 0.14), (0, 0.142)], (0, 0, -0.07), "M_Plastic_Blue", pen, segs=10)
    lathe("Pen_Cap", [(0, 0), (0.0052, 0.0), (0.0052, 0.045), (0, 0.048)], (0, 0, -0.078), "M_Plastic_White", pen, segs=10)


def tea_set(parent, pos):
    g = group("TeaSet", parent, pos)
    lathe("Saucer", [(0, 0), (0.035, 0), (0.04, 0.004), (0.07, 0.01), (0.072, 0.014), (0.066, 0.013), (0.04, 0.008), (0, 0.008)],
          (0, 0, 0), "M_Ceramic_White", g, segs=32)
    lathe("TeaGlass", [(0, 0.008), (0.03, 0.008), (0.032, 0.012), (0.04, 0.09), (0.041, 0.1), (0.036, 0.1), (0.035, 0.09),
                       (0.028, 0.016), (0, 0.016)], (0, 0, 0), "M_Glass_Cup", g, segs=28)
    lathe("Tea", [(0, 0.016), (0.028, 0.016), (0.034, 0.075), (0, 0.075)], (0, 0, 0), "M_Tea", g, segs=24)
    lathe("TeaLid", [(0, 0.1), (0.044, 0.1), (0.045, 0.104), (0.03, 0.114), (0.01, 0.118), (0.008, 0.13), (0.012, 0.136),
                     (0, 0.14)], (0, 0, 0.001), "M_Ceramic_White", g, segs=28)
    lathe("TeaLidBand", [(0.0405, 0.1045), (0.0455, 0.1045), (0.0455, 0.107), (0.0405, 0.107)], (0, 0, 0.001), "M_Ceramic_Blue", g,
          segs=28)
    sp = group("Spoon", g, (0.05, -0.03, 0.012), (0, -4, 30))
    sphere("Spoon_Bowl", 0.012, (0, 0, 0), "M_Metal_Chrome", sp, scale=(1.0, 0.7, 0.25), seg=12, rings=6)
    box("Spoon_Handle", (0.08, 0.006, 0.002), (0.05, 0, 0.002), "M_Metal_Chrome", sp, bev=0.001)


def biscuit_tin(parent, pos):
    g = group("BiscuitTin", parent, pos)
    cylinder("Tin_Body", 0.094, 0.07, (0, 0, 0), "M_Biscuit_Tin", g, verts=40, bev=0.003)
    lathe("Tin_Lid", [(0, 0.066), (0.098, 0.066), (0.099, 0.08), (0.095, 0.085), (0, 0.085)], (0, 0, 0), "M_Biscuit_Tin", g, segs=40)
    lathe("Tin_Rim", [(0.096, 0.066), (0.1, 0.066), (0.1, 0.071), (0.096, 0.071)], (0, 0, 0), "M_Gold", g, segs=40)
    art = cylinder("Tin_Art", 0.092, 0.0008, (0, 0, 0.0852), "M_Labels", g, verts=40)
    uv_job(art, "planar", axis="Z", rect=LABELS["tin"])


# ---------------------------------------------------------------- bufet contents

def bufet_items(bufet_group, drawer_group):
    top = 0.78
    g = group("BufetItems", bufet_group)
    runner = box("Bufet_Runner", (1.3, 0.34, 0.003), (0.05, -0.02, top + 0.0015), "M_Fabric_Cream", g, bev=0)
    stb = group("SetTopBox", g, (-0.52, -0.05, top + 0.003))
    box("STB_Body", (0.18, 0.12, 0.035), (0, 0, 0.0175), "M_Plastic_Black", stb, bev=0.004)
    sphere("STB_LED", 0.003, (0.07, -0.061, 0.022), "M_LED_Green", stb, seg=8, rings=4)
    plate_stand(g, (-0.72, 0.02, top + 0.003), 0.11, "M_Ceramic_Blue")
    frame(g, "Frame_Dimas", (0.5, 0.0, top + 0.003), (0, 0, -12), 0.16, 0.2, 0.02, "M_Gold", PHOTOS["dimas"], standing=True)
    pothos(g, (0.76, 0.05, top + 0.003))
    shelf = 0.44
    for i, x in enumerate((-0.22, 0.2)):
        plate_stand(g, (x, 0.1, shelf), 0.1, "M_Ceramic_White" if i else "M_Ceramic_Blue")
    for i in range(4):
        x = -0.08 + i * 0.055
        cup(g, "Cup%d" % i, (x, -0.02 + (i % 2) * 0.03, shelf))
    for i, (mat, dz, rz) in enumerate((("M_Magazine_B", 0.0, 2), ("M_Magazine_A", 0.03, -3), ("M_Magazine_C", 0.06, 5))):
        box("Album%d" % i, (0.3, 0.24, 0.03), (-0.14, 0.02, 0.105 + 0.015 + dz), mat, g, rot=(0, 0, rz), bev=0.004)
    books(g, (0.22, 0.12, 0.105))
    drawer_contents(drawer_group)


def plate_stand(parent, pos, r, mat):
    g = group("PlateStand", parent, pos)
    lathe("Plate", [(0, 0), (r * 0.6, 0), (r, 0.012), (r * 1.02, 0.016), (r * 0.62, 0.008), (0, 0.008)], (0, 0.0, r + 0.01),
          mat, g, rot=(80, 0, 0), segs=36)
    lathe("PlateCenter", [(0, 0.0081), (r * 0.5, 0.0081), (r * 0.5, 0.0085), (0, 0.0085)], (0, 0.0, r + 0.01), "M_Gold", g,
          rot=(80, 0, 0), segs=36)
    stand = [box("Stand_Base", (0.06, 0.05, 0.006), (0, 0.01, 0.003), "M_Wood_Dark", g, bev=0.002),
             box("Stand_Back", (0.012, 0.006, r * 1.3), (0, 0.035, r * 0.62), "M_Wood_Dark", g, rot=(-12, 0, 0), bev=0.002)]
    join("Stand", stand)


def cup(parent, name, pos):
    g = group(name, parent, pos)
    lathe(name + "_Body", [(0, 0), (0.02, 0), (0.024, 0.006), (0.03, 0.05), (0.028, 0.052), (0.025, 0.05), (0.02, 0.008), (0, 0.008)],
          (0, 0, 0), "M_Ceramic_White", g, segs=20)
    ring = [(0.03 + 0.012 * math.cos(a), 0, 0.028 + 0.013 * math.sin(a)) for a in [k * 2 * math.pi / 13 - math.pi / 2 for k in range(12)]]
    cable(name + "_Handle", ring, 0.0028, "M_Ceramic_White", g, res=4, bevel_res=1)


def books(parent, pos):
    g = group("Books", parent, pos)
    x = 0.0
    import random
    rnd = random.Random(3)
    for i in range(8):
        w = rnd.uniform(0.022, 0.04)
        h = rnd.uniform(0.2, 0.28)
        d = rnd.uniform(0.15, 0.19)
        b = box("Book%d" % i, (w, d, h), (x + w / 2, 0, h / 2), "M_Books", g, rot=(0, rnd.uniform(-2, 2), 0), bev=0.003)
        col = i % 14
        uv_job(b, "planar", axis="Y", rect=(col / 16, 0.05, (col + 1) / 16, 0.95))
        x += w + 0.002
    lean = box("BookLean", (0.03, 0.17, 0.24), (x + 0.05, 0, 0.115), "M_Books", g, rot=(0, 22, 0), bev=0.003)
    uv_job(lean, "planar", axis="Y", rect=(9 / 16, 0.05, 10 / 16, 0.95))


def drawer_contents(dg):
    g = group("DrawerContents", dg, (0, 0.2, -0.05))
    env = [("M_Paper_Envelope", 0.0, 3), ("M_Paper_White", 0.004, -5), ("M_Paper_Envelope", 0.008, 9)]
    for i, (mat, dz, rz) in enumerate(env):
        box("Bill%d" % i, (0.22, 0.11, 0.003), (-0.05 + i * 0.03, -0.02 + i * 0.02, dz), mat, g, rot=(0, 0, rz), bev=0)
    flat("Bill_Receipt", g, 0.065, 0.13, (0.12, 0.02, 0.012), 14, "M_Notes", NOTES["receipt"], thickness=0.001)
    box("Bill_Stub", (0.07, 0.04, 0.002), (0.13, -0.08, 0.004), "M_Plastic_Red", g, rot=(0, 0, -20), bev=0)


def pothos(parent, pos):
    g = group("Pothos", parent, pos)
    lathe("Pothos_Pot", [(0, 0), (0.065, 0), (0.08, 0.12), (0.084, 0.13), (0.078, 0.13), (0.07, 0.115), (0, 0.115)], (0, 0, 0),
          "M_Terracotta", g, segs=24)
    cylinder("Pothos_Soil", 0.072, 0.004, (0, 0, 0.112), "M_Soil", g, verts=20)
    for k in range(8):
        a = k * 0.8
        r = 0.04 + (k % 3) * 0.02
        leaf_card("Pothos_Leaf%d" % k, g, 0.09, (math.cos(a) * r, math.sin(a) * r, 0.16 + (k % 4) * 0.03),
                  (55 + (k % 3) * 10, 0, math.degrees(a) + 90), LEAVES["pothos"], curl=0.01)
    vines = []
    for v, (dx, length) in enumerate(((-0.05, 0.55), (0.04, 0.4))):
        pts = [(dx, -0.06, 0.12), (dx - 0.01, -0.16, 0.08), (dx - 0.02, -0.2, -0.1), (dx + 0.02, -0.21, -0.1 - length * 0.6),
               (dx, -0.215, -0.1 - length)]
        vines.append(cable("Pothos_Vine%d" % v, pts, 0.003, "M_PlantStem", g))
        for k in range(6):
            t = k / 5
            z = -0.05 - length * t
            leaf_card("Pothos_VLeaf%d_%d" % (v, k), g, 0.07, (dx + (0.02 if k % 2 else -0.02), -0.225, z),
                      (95, 0, 180 + (20 if k % 2 else -20)), LEAVES["pothos"], curl=0.008)
    join("Pothos_Vines", vines)


# ---------------------------------------------------------------- wall items

def frame(parent, name, pos, rot, w, h, border, mat, rect, standing=False, mount=True, photo_mat="M_Photos", depth=0.025):
    """Picture frame facing local -Y; origin at its back centre."""
    g = group(name, parent, pos, rot)
    zc = h / 2 if standing else 0.0
    parts = [
        box(name + "_T", (w, depth, border), (0, -depth / 2, zc + h / 2 - border / 2), mat, g, bev=0.004),
        box(name + "_B", (w, depth, border), (0, -depth / 2, zc - h / 2 + border / 2), mat, g, bev=0.004),
        box(name + "_L", (border, depth, h - 2 * border), (-w / 2 + border / 2, -depth / 2, zc), mat, g, bev=0.004),
        box(name + "_R", (border, depth, h - 2 * border), (w / 2 - border / 2, -depth / 2, zc), mat, g, bev=0.004),
        box(name + "_Back", (w - 0.01, 0.006, h - 0.01), (0, -0.003, zc), "M_Kraft", g, bev=0),
    ]
    if standing:
        parts.append(box(name + "_Strut", (0.03, 0.006, h * 0.7), (0, 0.04, zc - h * 0.12), "M_Kraft", g, rot=(-22, 0, 0), bev=0))
    join(name + "_Frame", parts)
    iw, ih = w - 2 * border + 0.004, h - 2 * border + 0.004
    photo = plane(name + "_Image", iw, ih, (0, -depth * 0.55, zc), photo_mat, g, rot=(90, 0, 0))
    uv_job(photo, "planar_local", rect=rect)
    if mount and not standing:
        nail_z = zc + h / 2 + 0.08
        cable(name + "_Wire", [(-w * 0.3, -0.004, zc + h / 2 - 0.05), (0, -0.004, nail_z), (w * 0.3, -0.004, zc + h / 2 - 0.05)],
              0.0012, "M_Metal_Black", g, res=2, bevel_res=0)
        cylinder(name + "_Nail", 0.004, 0.012, (0, 0.004, nail_z), "M_Metal_Chrome", g, rot=(90, 0, 0), verts=8)
    return g


def bulletin_board(parent):
    g = group("INT_BulletinBoard", parent, (0.55, -2.5 + 0.012, 1.45), (0, 0, 180))
    W, H = 1.0, 0.7
    cork = plane("Board_Cork", W - 0.06, H - 0.06, (0, -0.004, 0), "M_Cork", g, rot=(90, 0, 0))
    uv_job(cork, "planar_local", rect=(0, 0, 2.0, 1.4))
    parts = [
        box("Board_FrameT", (W, 0.024, 0.035), (0, -0.012, H / 2 - 0.0175), "M_Wood_Light", g, bev=0.005),
        box("Board_FrameB", (W, 0.024, 0.035), (0, -0.012, -H / 2 + 0.0175), "M_Wood_Light", g, bev=0.005),
        box("Board_FrameL", (0.035, 0.024, H - 0.07), (-W / 2 + 0.0175, -0.012, 0), "M_Wood_Light", g, bev=0.005),
        box("Board_FrameR", (0.035, 0.024, H - 0.07), (W / 2 - 0.0175, -0.012, 0), "M_Wood_Light", g, bev=0.005),
        box("Board_Back", (W - 0.02, 0.008, H - 0.02), (0, 0.006, 0), "M_Wood_Dark", g, bev=0),
    ]
    join("Board_Frame", parts)
    items = [
        ("Board_Calendar", 0.28, 0.42, (-0.28, -0.03), -1.5, "M_Calendar", (0, 0, 1, 1), "M_Pin_Red"),
        ("Board_Numbers", 0.2, 0.2, (-0.02, 0.17), 2.5, "M_Notes", NOTES["numbers"], "M_Pin_Blue"),
        ("Board_RTNotice", 0.2, 0.2, (0.25, 0.165), -2.0, "M_Notes", NOTES["rt"], "M_Pin_Yellow"),
        ("Board_Snapshot", 0.09, 0.09, (0.41, 0.2), 6.0, "M_Notes", NOTES["snapshot"], "M_Pin_Green"),
        ("Board_StickyY", 0.08, 0.08, (-0.05, -0.03), -4.0, "M_Notes", NOTES["sticky_y"], "M_Pin_Red"),
        ("Board_StickyP", 0.08, 0.08, (0.06, -0.07), 7.0, "M_Notes", NOTES["sticky_p"], "M_Pin_Blue"),
        ("Board_Receipt", 0.065, 0.13, (0.17, -0.07), -5.0, "M_Notes", NOTES["receipt"], "M_Pin_Green"),
        ("Board_Card", 0.09, 0.09, (0.33, -0.05), 3.0, "M_Notes", NOTES["card"], "M_Pin_Yellow"),
        ("Board_Drawing", 0.24, 0.12, (0.1, -0.235), -2.0, "M_Notes", NOTES["drawing"], "M_Pin_Red"),
    ]
    for i, (name, w, h, (x, z), r, mat, rect, pin) in enumerate(items):
        y = -0.006 - i * 0.0012
        card(name, g, w, h, (x, y, z), (0, r, 0), mat, rect, bend=0.004 if w > 0.15 else 0.0)
        px = x + math.sin(math.radians(-r)) * (h / 2 - 0.018)
        pz = z + h / 2 - 0.018
        pg = group(name + "_Pin", g, (px, y - 0.002, pz), (90, 0, 0))
        cylinder(name + "_PinHead", 0.009, 0.01, (0, 0, 0.004), pin, pg, verts=12, bev=0.002)
        cylinder(name + "_PinNeck", 0.004, 0.006, (0, 0, 0.0), pin, pg, verts=8)
    return g


def wall_clock(parent):
    g = group("WallClock", parent, (0.55, -2.5 + 0.005, 2.38), (0, 0, 180))
    r = 0.17
    lathe("Clock_Rim", [(0, 0), (r, 0), (r + 0.004, 0.012), (r, 0.04), (r - 0.018, 0.042), (r - 0.02, 0.03), (0, 0.03)], (0, 0, 0),
          "M_Plastic_Red", g, rot=(90, 0, 0), segs=48)
    face = cylinder("Clock_Face", r - 0.02, 0.001, (0, -0.031, 0), "M_ClockFace", g, rot=(90, 0, 0), verts=48, anchor="center")
    uv_job(face, "planar", axis="Z", rect=(0, 0, 1, 1))
    hands = group("Clock_Hands", g, (0, -0.036, 0))
    hour, minute = 7.66, 40.0
    ha = -math.radians(hour * 30)
    ma = -math.radians(minute * 6)
    box("Clock_HourHand", (0.012, 0.003, 0.08), (math.sin(-ha) * 0.035, 0, math.cos(ha) * 0.035), "M_Plastic_Black", hands,
        rot=(0, math.degrees(-ha), 0), bev=0.001)
    box("Clock_MinuteHand", (0.008, 0.003, 0.12), (math.sin(-ma) * 0.052, -0.002, math.cos(ma) * 0.052), "M_Plastic_Black", hands,
        rot=(0, math.degrees(-ma), 0), bev=0.001)
    box("Clock_SecondHand", (0.003, 0.002, 0.13), (0.0, -0.004, -0.05), "M_Plastic_Red", hands, rot=(0, 25, 0), bev=0)
    cylinder("Clock_Cap", 0.008, 0.008, (0, -0.004, 0), "M_Gold", hands, rot=(90, 0, 0), verts=12, anchor="center")
    return g


def wall_art(parent):
    east = group("EastWallPhotos", parent, (2.99, 0.05, 0), (0, 0, -90))
    frame(east, "Frame_Graduation", (0, 0, 1.6), (-4, 0, 0), 0.56, 0.56, 0.05, "M_Wood_Dark", PHOTOS["graduation"])
    frame(east, "Frame_Wedding", (-0.72, 0, 1.52), (-4, 0, 1.5), 0.42, 0.42, 0.04, "M_Gold", PHOTOS["wedding"])
    frame(east, "Frame_Family", (0.72, 0, 1.55), (-4, 0, -1.0), 0.42, 0.42, 0.04, "M_Wood_Teak", PHOTOS["family"])
    west = group("WestWallPainting", parent, (-2.99, 0.0, 0), (0, 0, 90))
    frame(west, "Painting", (0, 0, 1.8), (-3, 0, 0), 1.08, 0.76, 0.06, "M_Gold", (0, 0, 1, 1), photo_mat="M_Painting", depth=0.04)
    wall_clock(parent)
    bulletin_board(parent)


# ---------------------------------------------------------------- side table, plants, entry

def side_table_items(table):
    top = 0.56
    g = group("SideItems", table)
    from room_furniture import table_lamp
    table_lamp(g, (0.08, 0.06, top))
    tb = group("TissueBox", g, (-0.1, -0.1, top), (0, 0, 15))
    body = box("Tissue_Box", (0.24, 0.12, 0.09), (0, 0, 0.045), "M_Labels", tb, bev=0.006)
    uv_job(body, "planar", axis="Y", rect=LABELS["tissue"])
    c = cushion("Tissue_Sheet", (0.08, 0.05, 0.05), 0.6, (0, 0, 0.1), "M_Paper_White", tb, rot=(10, 0, 0), cuts=3, levels=1)
    for i, (mat, dz, rz) in enumerate((("M_Paper_Envelope", 0.0, -8), ("M_Paper_White", 0.003, 4))):
        box("SideEnvelope%d" % i, (0.22, 0.11, 0.003), (0.05, -0.14 + i * 0.01, top + 0.0015 + dz), mat, g, rot=(0, 0, rz), bev=0)
    for i, (mat, dz) in enumerate((("M_Magazine_C", 0.0), ("M_Magazine_A", 0.012))):
        box("SideMag%d" % i, (0.28, 0.21, 0.01), (0.0, 0.0, 0.155 + dz), mat, g, rot=(0, 0, 90 + i * 7), bev=0.002)
    return g


def pot(parent, name, pos, r_top, r_bot, h, mat="M_Terracotta", saucer=True):
    g = group(name, parent, pos)
    lathe(name + "_Pot", [(0, 0), (r_bot, 0), (r_top, h - 0.03), (r_top + 0.014, h - 0.028), (r_top + 0.014, h),
                          (r_top - 0.01, h), (r_top - 0.012, h - 0.02), (0, h - 0.02)], (0, 0, 0.012 if saucer else 0), mat, g,
          segs=32)
    if saucer:
        lathe(name + "_Saucer", [(0, 0), (r_bot + 0.03, 0), (r_bot + 0.045, 0.02), (r_bot + 0.035, 0.02), (r_bot + 0.02, 0.008),
                                 (0, 0.008)], (0, 0, 0), mat, g, segs=32)
    cylinder(name + "_Soil", r_top - 0.012, 0.004, (0, 0, h - 0.035 + (0.012 if saucer else 0)), "M_Soil", g, verts=24)
    return g


def monstera(parent, pos):
    g = pot(parent, "Monstera", pos, 0.19, 0.14, 0.34)
    soil_z = 0.33
    stems = []
    leaves = [(0.0, 0.75, 0.46, 0, 30), (0.5, 0.95, 0.42, 30, 20), (-0.6, 0.6, 0.4, -40, 45), (1.8, 0.55, 0.38, 60, 60),
              (2.6, 0.85, 0.44, 100, 35), (3.6, 0.65, 0.4, 160, 50), (4.4, 1.05, 0.36, 200, 25), (5.4, 0.5, 0.34, 250, 65)]
    for i, (a, height, size, yaw, tilt) in enumerate(leaves):
        r = 0.18 + 0.08 * (i % 3)
        x, y = math.cos(a) * r, math.sin(a) * r
        z = soil_z + height
        stems.append(cable("Monstera_Stem%d" % i, [(0, 0, soil_z), (x * 0.3, y * 0.3, soil_z + height * 0.5), (x * 0.8, y * 0.8, z - 0.05),
                                                   (x, y, z)], 0.006, "M_PlantStem", g, res=6, bevel_res=1))
        leaf_card("Monstera_Leaf%d" % i, g, size, (x + math.cos(a) * size * 0.35, y + math.sin(a) * size * 0.35, z - size * 0.05),
                  (tilt, 0, math.degrees(a) + 90), LEAVES["monstera"], curl=0.04)
    join("Monstera_Stems", stems)
    return g


def snake_plant(parent, pos):
    g = pot(parent, "SnakePlant", pos, 0.13, 0.1, 0.28, mat="M_Ceramic_Blue", saucer=False)
    base_z = 0.26
    import random
    rnd = random.Random(11)
    for i in range(9):
        a = i * 2.4
        r = 0.02 + 0.05 * (i % 3) / 2
        h = rnd.uniform(0.5, 0.85)
        w = rnd.uniform(0.055, 0.075)
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        rows = []
        n = 8
        for j in range(n + 1):
            t = j / n
            half = w / 2 * (1 - t ** 2.2) * (0.8 + 0.4 * math.sin(t * math.pi))
            lean = 0.12 * t * t
            twist = 0.25 * t
            row = []
            for s in (-1, 1):
                x = s * half * math.cos(twist)
                y = s * half * math.sin(twist) + lean
                row.append(bm.verts.new((x, y, t * h)))
            rows.append(row)
        for j in range(n):
            f = bm.faces.new((rows[j][0], rows[j][1], rows[j + 1][1], rows[j + 1][0]))
            for loop, (u, v) in zip(f.loops, ((0, j / n), (1, j / n), (1, (j + 1) / n), (0, (j + 1) / n))):
                u0, v0, u1, v1 = LEAVES["snake"]
                loop[uv].uv = (u0 + (u1 - u0) * u, v0 + (v1 - v0) * v)
        mesh_object("Snake_Leaf%d" % i, bm, "M_Leaves", g, (math.cos(a) * r, math.sin(a) * r, base_z), (0, 0, math.degrees(a) + 90))
    return g


def cactus_pot(parent, pos):
    g = pot(parent, "Cactus", pos, 0.05, 0.04, 0.08, saucer=False)
    lathe("Cactus_Body", [(0, 0), (0.035, 0.0), (0.045, 0.03), (0.042, 0.06), (0.028, 0.085), (0, 0.095)], (0, 0, 0.065),
          "M_PlantStem", g, segs=12, angle=25)
    sphere("Cactus_Flower", 0.01, (0.0, 0.0, 0.162), "M_Flower_Pink", g, seg=8, rings=5, scale=(1, 1, 0.6))
    return g


def sandal(parent, name, pos, rot_z, strap_mat, scale=1.0):
    g = group(name, parent, pos, (0, 0, rot_z))
    outline = []
    for i in range(24):
        t = i / 24 * 2 * math.pi
        x = 0.045 * math.cos(t) * (1.0 + 0.18 * math.sin(t))
        y = 0.125 * math.sin(t)
        outline.append((x * scale, y * scale))
    extrude_shape(name + "_Sole", outline, 0.014, (0, 0, 0), "M_Plastic_White", g, bev=0.003)
    extrude_shape(name + "_Base", outline, 0.006, (0, 0, -0.004), strap_mat, g, bev=0.002)
    for s in (-1, 1):
        cable(name + "_Strap%d" % s, [(0, 0.075 * scale, 0.016), (s * 0.018, 0.035, 0.04), (s * 0.04 * scale, -0.01, 0.016)],
              0.006, strap_mat, g, res=5, bevel_res=1)
    return g


def entry(parent):
    from room_furniture import shoe_rack
    rack = shoe_rack(parent, (2.7, 2.19, 0), (0, 0, 0))
    sandal(rack, "Sandal_A_L", (-0.16, 0.0, 0.397), 4, "M_Sandal_Blue")
    sandal(rack, "Sandal_A_R", (-0.06, 0.02, 0.397), -3, "M_Sandal_Blue")
    sandal(rack, "Sandal_B_L", (0.08, 0.0, 0.397), 8, "M_Sandal_Red", 0.9)
    sandal(rack, "Sandal_B_R", (0.17, -0.01, 0.397), 2, "M_Sandal_Red", 0.9)
    for s, x in ((-1, -0.08), (1, 0.06)):
        c = cushion("Shoe%d" % s, (0.1, 0.27, 0.08), 0.25, (x, 0.0, 0.27), "M_Plastic_Black", rack, rot=(0, 0, s * 4), cuts=3, levels=2)
    sandal(parent, "Sandal_Floor_L", (1.72, 2.05, 0.004), 160, "M_Sandal_Blue")
    sandal(parent, "Sandal_Floor_R", (1.84, 2.1, 0.004), 195, "M_Sandal_Blue")
    hg = group("Helmet", parent, (2.93, 1.95, 1.55), (0, 0, -90))
    cable("Helmet_Hook", [(0, 0.06, 0.0), (0, 0.0, 0.0), (0, -0.06, 0.02), (0, -0.07, 0.05)], 0.005, "M_Metal_Chrome", hg)
    lathe("Helmet_Shell", [(0, 0.2), (0.07, 0.19), (0.12, 0.15), (0.14, 0.09), (0.142, 0.03), (0.13, 0.0), (0.12, 0.0),
                           (0.125, 0.03), (0.122, 0.09), (0.1, 0.15), (0.06, 0.18), (0, 0.185)], (0, -0.1, -0.09), "M_Helmet", hg,
          rot=(12, 0, 0), segs=32, angle=35)
    box("Helmet_Visor", (0.2, 0.01, 0.07), (0, -0.23, -0.02), "M_Glass_Cup", hg, rot=(-12, 0, 0), bev=0.004)
    cable("Helmet_Strap", [(-0.1, -0.1, -0.08), (-0.05, -0.12, -0.2), (0.05, -0.12, -0.2), (0.1, -0.1, -0.08)], 0.005,
          "M_Rubber_Black", hg)
    ug = group("Umbrella", parent, (2.92, 2.43, 0.0), (8, -6, 0))
    lathe("Umbrella_Canopy", [(0, 0.08), (0.02, 0.08), (0.045, 0.3), (0.04, 0.6), (0.02, 0.72), (0, 0.74)], (0, 0, 0),
          "M_Umbrella", ug, segs=10, angle=20)
    cylinder("Umbrella_Tip", 0.004, 0.08, (0, 0, 0.0), "M_Metal_Chrome", ug, verts=8)
    cylinder("Umbrella_Shaft", 0.006, 0.1, (0, 0, 0.74), "M_Metal_Chrome", ug, verts=8)
    cable("Umbrella_Handle", [(0, 0, 0.83), (0, 0, 0.9), (0.02, 0, 0.93), (0.05, 0, 0.9), (0.05, 0, 0.87)], 0.011, "M_Wood_Dark", ug)
    box("Umbrella_Strap", (0.03, 0.05, 0.02), (0.0, 0.0, 0.45), "M_Umbrella", ug, bev=0.006)


def cables(parent):
    g = group("Cables", parent)
    parts = []
    parts.append(cable("Cable_TV", [(-2.83, 0.02, 1.0), (-2.95, 0.05, 0.92), (-2.982, 0.1, 0.5), (-2.982, 0.1, 0.03),
                                    (-2.982, -0.5, 0.012), (-2.986, -1.0, 0.03), (-2.99, -1.05, 0.3)], 0.0055, "M_Cable", g))
    parts.append(cable("Cable_STB", [(-2.8, -0.52, 0.8), (-2.975, -0.52, 0.75), (-2.98, -0.6, 0.2), (-2.97, -0.7, 0.015)], 0.005,
                       "M_Cable", g))
    tg = group("PowerStrip", g, (-2.82, -0.95, 0.0), (0, 0, 20))
    box("Strip_Body", (0.26, 0.055, 0.035), (0, 0, 0.0175), "M_Plastic_White", tg, bev=0.008)
    for i in range(3):
        box("Strip_Plug%d" % i, (0.03, 0.035, 0.04), (-0.08 + i * 0.075, 0, 0.05), "M_Plastic_Black", tg, bev=0.006)
    sphere("Strip_LED", 0.004, (0.115, -0.028, 0.03), "M_LED_Red", tg, seg=8, rings=4)
    parts.append(cable("Cable_Strip", [(-2.93, -0.99, 0.015), (-2.96, -1.03, 0.015), (-2.985, -1.05, 0.12), (-2.99, -1.05, 0.3)], 0.0035,
                       "M_Cable", g))
    parts.append(cable("Cable_Fan", [(-2.4, 1.85, 0.03), (-2.2, 2.1, 0.012), (-2.0, 2.4, 0.012), (-1.96, 2.48, 0.1), (-1.95, 2.49, 0.3)],
                       0.0055, "M_Cable", g))
    parts.append(cable("Cable_Charger", [(2.99, -1.85, 0.3), (2.97, -1.8, 0.2), (2.95, -1.62, 0.45), (2.86, -1.5, 0.56),
                                         (2.7, -1.42, 0.562)], 0.004, "M_Plastic_White", g))
    join("Cables_Mesh", parts)
    box("Charger_Plug", (0.03, 0.012, 0.008), (2.69, -1.42, 0.564), "M_Plastic_White", g, rot=(0, 0, 25), bev=0.003)



def cat(parent, pos, rot_z):
    """Orange tabby asleep in the sun patch."""
    g = group("Cat", parent, pos, (0, 0, rot_z))
    sphere("Cat_Body", 0.14, (0.02, 0.0, 0.085), "M_Cat", g, seg=32, rings=16, scale=(1.35, 1.0, 0.6))
    sphere("Cat_Haunch", 0.09, (0.1, 0.045, 0.1), "M_Cat", g, seg=24, rings=12, scale=(1.0, 1.0, 0.85))
    for sx in (-1, 1):
        sphere("Cat_Paw%d" % sx, 0.024, (-0.17, -0.02 + sx * 0.035, 0.022), "M_Cat_Light", g, seg=14, rings=7, scale=(1.5, 0.9, 0.7))
    head = group("Cat_Head", g, (-0.14, -0.06, 0.115), (0, 14, -35))
    sphere("Cat_Skull", 0.075, (0, 0, 0), "M_Cat", head, seg=28, rings=14, scale=(1.0, 1.12, 0.86))
    sphere("Cat_Muzzle", 0.031, (-0.06, 0.0, -0.022), "M_Cat_Light", head, seg=18, rings=9, scale=(0.9, 1.35, 0.75))
    sphere("Cat_Nose", 0.008, (-0.087, 0.0, -0.009), "M_Flower_Pink", head, seg=8, rings=5)
    for sy in (-1, 1):
        cylinder("Cat_Ear%d" % sy, 0.032, 0.062, (0.012, sy * 0.045, 0.045), "M_Cat", head, rot=(sy * -20, 8, 0), verts=3,
                 r2=0.002)
        cylinder("Cat_EarIn%d" % sy, 0.02, 0.042, (0.0, sy * 0.045, 0.05), "M_Flower_Pink", head, rot=(sy * -20, 8, 0), verts=3,
                 r2=0.001)
        eye = [(-0.066 + 0.006 * math.sin(a), sy * (0.03 + 0.014 * math.cos(a)), 0.016 - 0.007 * math.sin(a))
               for a in [k * math.pi / 8 for k in range(9)]]
        cable("Cat_Eye%d" % sy, eye, 0.0026, "M_Cable", head, res=3, bevel_res=1)
        for k in range(3):
            cable("Cat_Whisker%d_%d" % (sy, k), [(-0.075, sy * 0.022, -0.02 + k * 0.006),
                                                 (-0.1, sy * (0.06 + k * 0.004), -0.018 + k * 0.012)], 0.0014, "M_Cat_Light",
                  head, res=2, bevel_res=0)
    tail = [(0.18, 0.06, 0.04), (0.215, -0.05, 0.035), (0.12, -0.145, 0.03), (-0.02, -0.165, 0.028), (-0.13, -0.13, 0.025),
            (-0.19, -0.08, 0.022)]
    cable("Cat_Tail", tail, 0.021, "M_Cat", g, res=8, bevel_res=2)
    sphere("Cat_TailTip", 0.02, (-0.19, -0.08, 0.022), "M_Cat_Light", g, seg=12, rings=6)
    return g


def mirror(parent, pos):
    g = group("WallMirror", parent, pos, (0, 0, -90))
    rim = []
    for i in range(48):
        a = 2 * math.pi * i / 48
        rim.append((0.2 * math.cos(a), -0.012, 0.27 * math.sin(a)))
    cable("Mirror_Frame", rim + [rim[0]], 0.018, "M_Wood_Teak", g, res=4, bevel_res=2)
    face = cylinder("Mirror_Back", 0.19, 0.01, (0, -0.004, 0), "M_Mirror", g, rot=(90, 0, 0), verts=48, anchor="center")
    face.scale = (1.0, 1.4, 1.0)
    glass = cylinder("Mirror_Glass", 0.19, 0.002, (0, -0.011, 0), "M_Glass", g, rot=(90, 0, 0), verts=48, anchor="center")
    glass.scale = (1.0, 1.4, 1.0)
    cable("Mirror_Wire", [(-0.08, -0.004, 0.22), (0, -0.004, 0.36), (0.08, -0.004, 0.22)], 0.0012, "M_Metal_Black", g, res=2,
          bevel_res=0)
    cylinder("Mirror_Nail", 0.004, 0.012, (0, 0.004, 0.36), "M_Metal_Chrome", g, rot=(90, 0, 0), verts=8)
    shelf = group("Mirror_Shelf", g, (0, -0.06, -0.36))
    box("MirrorShelf_Board", (0.34, 0.11, 0.018), (0, 0, 0), "M_Wood_Teak", shelf, bev=0.005)
    for sx in (-1, 1):
        box("MirrorShelf_Bracket%d" % sx, (0.015, 0.09, 0.06), (sx * 0.13, 0.01, -0.04), "M_Wood_Teak", shelf, bev=0.003)
    box("Comb", (0.12, 0.012, 0.03), (-0.06, -0.01, 0.024), "M_Plastic_Black", shelf, rot=(0, 0, 8), bev=0.003)
    lathe("PerfumeBottle", [(0, 0), (0.018, 0), (0.02, 0.05), (0.008, 0.06), (0.008, 0.075), (0, 0.075)], (0.07, 0.0, 0.009),
          "M_Glass_Cup", shelf, segs=12)
    lathe("PerfumeCap", [(0, 0), (0.01, 0), (0.01, 0.02), (0, 0.02)], (0.07, 0.0, 0.084), "M_Gold", shelf, segs=10)
    return g


def trash_bin(parent, pos):
    g = group("TrashBin", parent, pos)
    lathe("Bin_Body", [(0, 0), (0.1, 0), (0.125, 0.3), (0.13, 0.31), (0.118, 0.31), (0.11, 0.29), (0.09, 0.02), (0, 0.02)], (0, 0, 0),
          "M_Plastic_Teal", g, segs=28)
    sphere("Bin_Paper", 0.05, (0.02, 0.01, 0.29), "M_Paper_White", g, seg=10, rings=6, scale=(1.1, 0.9, 0.8))
    sphere("Bin_Paper2", 0.035, (-0.04, -0.03, 0.3), "M_Paper_Envelope", g, seg=10, rings=6)
    return g


def build(root, furniture):
    g = group("Props", root)
    coffee_table_items(furniture["table"])
    bufet_items(furniture["bufet"], furniture["drawer"])
    side_table_items(furniture["side"])
    wall_art(g)
    monstera(g, (2.6, -2.12, 0))
    snake_plant(g, (0.45, 2.27, 0))
    cactus_pot(g, (-1.45, 2.47, 0.852))
    entry(g)
    cables(g)
    cat(g, (-0.3, -0.75, 0.0), 35)
    mirror(g, (2.99, 1.55, 1.5))
    trash_bin(g, (2.45, -1.9, 0.0))
    return g