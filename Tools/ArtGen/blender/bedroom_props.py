"""Furnishing the bedroom: where every piece of furniture stands, the desk with the
interactables (newspaper, wallet, rulebook, drawer of bills), the cork board with
the calendar, posters and photos taped to the walls, shelf contents, floor things
and the cat."""
import math
import random

import atlas
import bedroom_furniture as bf
from bedroom_shell import ROOM_X, ROOM_Y, wall_group
from dcm import box, cable, cushion, cylinder, group, join, lathe, plane, sphere, uv_job
from props import (book_stack, books_row, bills, card, cat, cork_board, flat, frame, mug, newspaper, pinned, pothos, pot,
                   rulebook, slipper, standing_fan, taped, trash_bin, wallet)

DESK_TOP = 0.74


# ---------------------------------------------------------------- small props

def laptop(parent, pos, rot_z):
    """Open laptop showing its lock screen."""
    g = group("Laptop", parent, pos, (0, 0, rot_z))
    W, D = 0.32, 0.22
    box("Laptop_Base", (W, D, 0.016), (0, 0, 0.008), "M_Metal_Gray", g, bev=0.006)
    box("Laptop_Keys", (W - 0.04, D * 0.45, 0.002), (0, 0.025, 0.0165), "M_Plastic_Black", g, bev=0)
    box("Laptop_Pad", (0.09, 0.055, 0.001), (0, -0.065, 0.0162), "M_Plastic_Gray", g, bev=0)
    lid = group("Laptop_Lid", g, (0, D / 2 - 0.004, 0.016), (-12, 0, 0))
    box("Laptop_LidShell", (W, 0.01, 0.21), (0, 0.004, 0.105), "M_Metal_Gray", lid, bev=0.005)
    scr = plane("Laptop_Screen", W - 0.03, 0.18, (0, -0.0015, 0.108), "M_Labels", lid, rot=(90, 0, 0))
    uv_job(scr, "planar_local", rect=atlas.region("labels", "laptop"))
    cable("Laptop_Charger", [(W / 2, 0.06, 0.008), (W / 2 + 0.05, 0.08, 0.004), (W / 2 + 0.08, 0.2, 0.002), (W / 2 + 0.07, 0.27, -0.02),
                             (W / 2 + 0.06, 0.28, -0.25)], 0.0028, "M_Cable", g)
    return g


def desk_lamp(parent, pos, rot_z):
    """Two-arm desk lamp leaning over the desk."""
    g = group("DeskLamp", parent, pos, (0, 0, rot_z))
    lathe("Lamp_Base", [(0, 0), (0.07, 0), (0.07, 0.015), (0.05, 0.03), (0, 0.03)], (0, 0, 0), "M_Plastic_Red", g, segs=28)
    p1 = (0, 0.0, 0.03)
    p2 = (0, -0.06, 0.3)
    p3 = (0, -0.22, 0.38)
    for k, (a, b) in enumerate(((p1, p2), (p2, p3))):
        for s in (-1, 1):
            cable("Lamp_Arm%d_%d" % (k, s), [(a[0] + s * 0.008, a[1], a[2]), (b[0] + s * 0.008, b[1], b[2])], 0.004, "M_Plastic_Red", g,
                  res=2, bevel_res=1)
    sphere("Lamp_Joint", 0.014, p2, "M_Metal_Chrome", g, seg=10, rings=6)
    head = group("Lamp_Head", g, p3, (-35, 0, 0))
    lathe("Lamp_Shade", [(0.012, 0.03), (0.03, 0.02), (0.06, -0.04), (0.068, -0.075), (0.064, -0.075), (0.055, -0.042), (0.027, 0.012),
                         (0.009, 0.024)], (0, 0, 0), "M_Plastic_Red", head, segs=28, angle=35)
    sphere("Lamp_Bulb", 0.028, (0, 0, -0.045), "M_LampDiffuser", head, seg=12, rings=6)
    cable("Lamp_Cord", [(0, 0.05, 0.01), (0.02, 0.12, 0.004), (0.06, 0.2, 0.0), (0.05, 0.26, -0.2)], 0.0025, "M_Cable", g)
    return g


def pen_cup(parent, pos):
    g = group("PenCup", parent, pos)
    lathe("PenCup_Body", [(0, 0), (0.035, 0), (0.036, 0.1), (0.032, 0.1), (0.031, 0.006), (0, 0.006)], (0, 0, 0), "M_Ceramic_Blue", g,
          segs=20)
    rnd = random.Random(5)
    for k, mat in enumerate(("M_Plastic_Black", "M_Plastic_Blue", "M_Plastic_Red", "M_Pencil", "M_Pencil")):
        a = k * 1.3
        tilt_x, tilt_y = rnd.uniform(-12, 12), rnd.uniform(-12, 12)
        cylinder("Pen%d" % k, 0.0045, 0.15, (math.cos(a) * 0.015, math.sin(a) * 0.015, 0.01), mat, g, rot=(tilt_x, tilt_y, 0), verts=8)
    box("Ruler", (0.02, 0.004, 0.2), (-0.012, 0.01, 0.1), "M_Plastic_Clear", g, rot=(8, -6, 0), bev=0.001)
    return g


def banana_milk(parent, pos):
    """Squat jar-shaped milk bottle, foil lid, no label."""
    g = group("BananaMilk", parent, pos)
    lathe("Milk_Jar", [(0, 0), (0.02, 0), (0.03, 0.02), (0.033, 0.055), (0.028, 0.085), (0.017, 0.1), (0.017, 0.11), (0, 0.11)], (0, 0, 0),
          "M_Plastic_Yellow", g, segs=24, angle=40)
    cylinder("Milk_Foil", 0.018, 0.004, (0, 0, 0.11), "M_Metal_Chrome", g, verts=16)
    return g


def cup_ramyun(parent, pos):
    """Empty cup ramyun with its lid half open and chopsticks across."""
    g = group("CupRamyun", parent, pos)
    body = lathe("Ramyun_Cup", [(0, 0), (0.045, 0), (0.058, 0.1), (0.061, 0.1), (0.061, 0.104), (0.056, 0.104), (0.043, 0.006), (0, 0.006)],
                 (0, 0, 0), "M_Labels", g, segs=28)
    uv_job(body, "cyl", rect=atlas.region("labels", "ramyun"))
    lid = group("Ramyun_LidHinge", g, (-0.058, 0, 0.104), (0, -115, 0))
    cylinder("Ramyun_Lid", 0.06, 0.002, (0.058, 0, 0), "M_Metal_Chrome", lid, verts=24)
    for k in range(2):
        cylinder("Chopstick%d" % k, 0.0032, 0.22, (-0.1, -0.012 + k * 0.024, 0.108), "M_Wood_Light", g, rot=(0, 90, 3 - k * 6), verts=8)
    return g


def tissue_box(parent, pos, rot_z):
    g = group("TissueBox", parent, pos, (0, 0, rot_z))
    b = box("Tissue_Box", (0.24, 0.12, 0.09), (0, 0, 0.045), "M_Labels", g, bev=0.006)
    uv_job(b, "planar", axis="Y", rect=atlas.region("labels", "tissue"))
    cushion("Tissue_Sheet", (0.08, 0.05, 0.05), 0.6, (0, 0, 0.1), "M_Paper_White", g, rot=(10, 0, 0), cuts=3, levels=1)
    return g


def remote(parent, pos, rot_z):
    g = group("Remote", parent, pos, (0, 0, rot_z))
    box("Remote_Body", (0.05, 0.17, 0.02), (0, 0, 0.01), "M_Plastic_Black", g, bev=0.008)
    face = plane("Remote_Face", 0.044, 0.16, (0, 0, 0.0205), "M_Labels", g)
    uv_job(face, "planar_local", rect=atlas.region("labels", "remote"))
    return g


def lotion(parent, pos):
    g = group("Lotion", parent, pos)
    lathe("Lotion_Bottle", [(0, 0), (0.03, 0), (0.032, 0.012), (0.03, 0.14), (0.014, 0.16), (0, 0.16)], (0, 0, 0), "M_Plastic_Pink", g,
          segs=20, angle=40)
    lathe("Lotion_Pump", [(0, 0), (0.012, 0), (0.012, 0.03), (0.005, 0.035), (0.005, 0.05), (0, 0.05)], (0, 0, 0.16), "M_Plastic_White",
          g, segs=12)
    box("Lotion_Nozzle", (0.03, 0.008, 0.008), (0.012, 0, 0.206), "M_Plastic_White", g, bev=0.003)
    return g


def piggy_bank(parent, pos, rot_z):
    g = group("PiggyBank", parent, pos, (0, 0, rot_z))
    sphere("Piggy_Body", 0.06, (0, 0, 0.058), "M_Plastic_Pink", g, seg=20, rings=10, scale=(1.25, 1.0, 0.95))
    cylinder("Piggy_Snout", 0.022, 0.02, (-0.078, 0, 0.062), "M_Plastic_Pink", g, rot=(0, 90, 0), verts=14, anchor="center", bev=0.004)
    for s in (-1, 1):
        cylinder("Piggy_Ear%d" % s, 0.018, 0.025, (-0.035, s * 0.035, 0.108), "M_Plastic_Pink", g, rot=(s * 15, -10, 0), verts=3, r2=0.002)
        sphere("Piggy_Eye%d" % s, 0.006, (-0.068, s * 0.022, 0.085), "M_Plastic_Black", g, seg=6, rings=4)
        for f in (-1, 1):
            cylinder("Piggy_Foot%d%d" % (s, f), 0.012, 0.02, (f * 0.04, s * 0.03, 0.0), "M_Plastic_Pink", g, verts=10)
    box("Piggy_Slot", (0.035, 0.004, 0.002), (0, 0, 0.115), "M_Plastic_Black", g, bev=0)
    return g


def polaroid(name, parent, x, z, spin, rect, seed=0, w=0.1):
    """Instant photo taped to a wall group."""
    h = w * 1.2
    taped(name, parent, w, h, x, z, spin, "M_Paper_White", (0.0, 0.0, 0.02, 0.02), corners=1, seed=seed)
    ca, sa = math.cos(math.radians(-spin)), math.sin(math.radians(-spin))
    ox, oz = 0.0, h * 0.08
    card(name + "_Photo", parent, w * 0.86, w * 0.86, (x + ox * ca - oz * sa, -0.003, z + ox * sa + oz * ca), (0, spin, 0), "M_Photos", rect)


def crop(key, x0, y0, x1, y1):
    """Sub-rectangle of a photos-atlas entry, in fractions of that entry."""
    u0, v0, u1, v1 = atlas.region("photos", key)
    return (u0 + (u1 - u0) * x0, v0 + (v1 - v0) * y0, u0 + (u1 - u0) * x1, v0 + (v1 - v0) * y1)


def poster_rect(key):
    return atlas.region("posters", key)


# ---------------------------------------------------------------- areas

def desk_area(fg):
    desk, top_drawer = bf.desk(fg, (0.92, ROOM_Y - 0.285, 0), (0, 0, 0))
    bf.chair(fg, (0.86, 1.0, 0), (0, 0, 168))
    items = group("DeskItems", desk, (0, 0, DESK_TOP))
    laptop(items, (0.06, 0.0, 0), 0)
    desk_lamp(items, (-0.43, 0.19, 0), 25)
    book_stack(items, "DeskBooks", (-0.23, 0.17, 0), 4, seed=4, rot_z=88, size=((0.13, 0.17), (0.18, 0.23)))
    pen_cup(items, (0.29, 0.2, 0))
    pothos(items, "DeskPlant", (0.41, 0.17, 0), r=0.05, h=0.085, trail=0.0, leaves=7, seed=2)
    mug(items, "Mug", (0.44, -0.18, 0), rot_z=40)
    banana_milk(items, (0.2, -0.2, 0))
    newspaper(items, (-0.29, -0.1, 0), 6)
    wallet(items, (0.3, -0.17, 0), -18)
    rulebook(items, (0.34, 0.0, 0), 4)
    inside = group("Drawer_Inside", top_drawer, (0, 0.22, -0.06))
    bills(inside)
    return desk


def board_and_walls(g):
    north = wall_group("NorthWallDecor", g, "north")
    board = cork_board(north, "INT_BulletinBoard", (0.92, 0, 1.4), 0.9, 0.66)
    N = lambda k: atlas.region("notes", k)
    pinned(board, [
        ("Board_Calendar", 0.28, 0.42, (-0.25, -0.02), -1.5, "M_Calendar", (0, 0, 1, 1), "M_Pin_Red"),
        ("Board_Numbers", 0.2, 0.2, (0.0, 0.16), 2.5, "M_Notes", N("numbers"), "M_Pin_Blue"),
        ("Board_Notice", 0.2, 0.2, (0.26, 0.15), -2.0, "M_Notes", N("notice"), "M_Pin_Yellow"),
        ("Board_StickyY", 0.08, 0.08, (-0.01, -0.06), -4.0, "M_Notes", N("sticky_y"), "M_Pin_Red"),
        ("Board_StickyP", 0.08, 0.08, (0.09, -0.12), 7.0, "M_Notes", N("sticky_p"), "M_Pin_Blue"),
        ("Board_Ticket", 0.18, 0.09, (0.25, -0.06), -3.0, "M_Notes", N("ticket"), "M_Pin_Green"),
        ("Board_Strip", 0.055, 0.11, (0.37, -0.17), 4.0, "M_Notes", N("strip"), "M_Pin_Yellow"),
        ("Board_IDPhoto", 0.036, 0.048, (0.14, 0.0), -6.0, "M_Photos", atlas.region("photos", "id"), "M_Pin_Green"),
    ])
    # Posters over the bed, instant photos over the bookshelf and the board.
    taped("Poster_Concert", north, 0.36, 0.72, -1.46, 1.63, -1.5, "M_Posters", poster_rect("concert"), seed=1)
    taped("Poster_Movie", north, 0.36, 0.72, -1.02, 1.6, 1.2, "M_Posters", poster_rect("movie"), seed=2)
    polaroid("Polaroid_A", north, -1.25, 2.12, 5, crop("family", 0.05, 0.1, 0.95, 0.95), seed=3)
    polaroid("Polaroid_B", north, -0.12, 1.86, -4, crop("graduation", 0.15, 0.2, 0.85, 0.95), seed=4)
    polaroid("Polaroid_C", north, 0.02, 2.02, 6, crop("family", 0.4, 0.3, 1.0, 0.95), seed=5)
    polaroid("Polaroid_D", north, 0.2, 1.84, -2, crop("wedding", 0.1, 0.25, 0.9, 1.0), seed=6)
    polaroid("Polaroid_E", north, 0.62, 1.9, 3, crop("graduation", 0.0, 0.0, 1.0, 1.0), seed=7)
    polaroid("Polaroid_F", north, 1.12, 1.92, -5, crop("family", 0.0, 0.0, 0.6, 0.7), seed=8)

    east = wall_group("EastWallDecor", g, "east")
    taped("Poster_Jeju", east, 0.36, 0.72, -0.4, 1.52, 1.0, "M_Posters", poster_rect("jeju"), seed=9)
    taped("Timetable", east, 0.26, 0.447, -0.98, 1.35, -1.0, "M_Posters", poster_rect("timetable"), seed=10)
    bf.floor_mirror(east, (0.25, -0.22, 0), (0, 0, 0))
    rail = bf.hook_rail(east, (1.32, 0, 1.62), (0, 0, 0))
    bf.backpack(rail, (-0.16, -0.07, 0.02), (0, 0, 4))

    south = wall_group("SouthWallDecor", g, "south")
    clock = group("WallClock", south, (0.7, 0, 1.95))
    r = 0.15
    lathe("Clock_Rim", [(0, 0), (r, 0), (r + 0.004, 0.012), (r, 0.04), (r - 0.018, 0.042), (r - 0.02, 0.03), (0, 0.03)], (0, 0, 0),
          "M_Plastic_Blue", clock, rot=(90, 0, 0), segs=48)
    face = cylinder("Clock_Face", r - 0.02, 0.001, (0, -0.031, 0), "M_ClockFace", clock, rot=(90, 0, 0), verts=48, anchor="center")
    uv_job(face, "planar", axis="Z", rect=(0, 0, 1, 1))
    hands = group("Clock_Hands", clock, (0, -0.036, 0))
    for name, length, width, angle, dy in (("Hour", 0.07, 0.011, 16 * 30 + 10, 0), ("Minute", 0.105, 0.007, 20 * 6, -0.002)):
        a = math.radians(angle)
        box("Clock_%sHand" % name, (width, 0.003, length), (math.sin(a) * length * 0.4, dy, math.cos(a) * length * 0.4), "M_Plastic_Black",
            hands, rot=(0, math.degrees(a), 0), bev=0.001)
    a = math.radians(205)
    box("Clock_SecondHand", (0.003, 0.002, 0.12), (math.sin(a) * 0.035, -0.004, math.cos(a) * 0.035), "M_Plastic_Red", hands,
        rot=(0, 205, 0), bev=0)
    cylinder("Clock_Cap", 0.008, 0.008, (0, -0.004, 0), "M_Gold", hands, rot=(90, 0, 0), verts=12, anchor="center")


def shelves(fg):
    shelf, levels = bf.bookshelf(fg, (0.04, ROOM_Y - 0.155, 0), (0, 0, 0))
    books_row(shelf, "ShelfBooksA", (-0.29, -0.03, levels[0]), 0.4, seed=11)
    box("ShelfBoxA", (0.16, 0.22, 0.14), (0.2, 0.02, levels[0] + 0.07), "M_Kraft", shelf, bev=0.004)
    books_row(shelf, "ShelfBooksB", (-0.29, -0.03, levels[1]), 0.58, seed=12, lean_last=False)
    books_row(shelf, "ShelfBooksC", (-0.29, -0.03, levels[2]), 0.34, seed=13)
    piggy_bank(shelf, (0.19, -0.02, levels[2]), -30)
    pothos(shelf, "ShelfPlant", (-0.15, -0.02, levels[3]), r=0.065, h=0.11, trail=0.34, leaves=9, seed=6, mat="M_Ceramic_White")
    frame(shelf, "Frame_Graduation", (0.14, 0.02, levels[3]), (0, 0, -8), 0.19, 0.24, 0.02, "M_Wood_Dark",
          crop("graduation", 0.125, 0.0, 0.875, 1.0), standing=True)

    corner, tiers = bf.corner_shelf(fg, (ROOM_X - 0.18, 1.55, 0), (0, 0, -90))
    box("CShelf_ShoeBox", (0.34, 0.22, 0.12), (-0.08, 0.0, tiers[0] + 0.06), "M_Cover_Red", corner, rot=(0, 0, 3), bev=0.004)
    box("CShelf_Basket", (0.2, 0.24, 0.16), (0.18, 0.0, tiers[0] + 0.08), "M_Kraft", corner, bev=0.006)
    books_row(corner, "CShelfBooksA", (-0.27, -0.05, tiers[1]), 0.34, seed=21)
    sp = group("Speaker", corner, (0.18, 0.0, tiers[1]), (0, 0, -10))
    box("Speaker_Body", (0.1, 0.1, 0.16), (0, 0, 0.08), "M_Plastic_Black", sp, bev=0.008)
    cylinder("Speaker_Cone", 0.032, 0.004, (0, -0.051, 0.1), "M_Plastic_Gray", sp, rot=(90, 0, 0), verts=20, anchor="center")
    book_stack(corner, "CShelfMags", (-0.1, 0.0, tiers[2]), 3, seed=22, rot_z=4)
    pot(corner, "CShelfCactus", (0.17, 0.0, tiers[2]), 0.045, 0.036, 0.075, saucer=False)
    lathe("Cactus_Body", [(0, 0), (0.03, 0.0), (0.04, 0.03), (0.037, 0.06), (0.024, 0.085), (0, 0.095)], (0.17, 0.0, tiers[2] + 0.06),
          "M_PlantStem", corner, segs=12, angle=25)
    books_row(corner, "CShelfBooksB", (-0.27, -0.05, tiers[3]), 0.52, seed=23, lean_last=False)
    pothos(corner, "CShelfPlant", (-0.14, -0.02, tiers[4]), r=0.06, h=0.1, trail=0.4, leaves=8, seed=9)
    box("CShelf_Box", (0.2, 0.2, 0.14), (0.14, 0.0, tiers[4] + 0.07), "M_Cover_Blue", corner, rot=(0, 0, -6), bev=0.004)


def tv_corner(fg):
    cab = bf.tv_cabinet(fg, (-0.7, -ROOM_Y + 0.21, 0), (0, 0, 180))
    top = 0.5
    bf.crt_tv(cab, (0.1, 0.02, top), (0, 0, 0))
    tissue_box(cab, (-0.3, 0.07, top), 12)
    lotion(cab, (-0.4, -0.1, top))
    cup_ramyun(cab, (-0.2, -0.1, top))
    remote(cab, (0.38, -0.1, top), 70)
    frame(cab, "Frame_Wedding", (0.38, 0.1, top), (0, 0, -14), 0.14, 0.17, 0.018, "M_Gold", crop("wedding", 0.11, 0.0, 0.89, 1.0),
          standing=True)


def floor_things(fg):
    rug = cylinder("Rug_Round", 0.62, 0.008, (0.45, -0.3, 0.0), "M_Rug_Round", fg, verts=64)
    uv_job(rug, "planar", axis="Z", rect=(0, 0, 1, 1))
    slipper(fg, "Slipper_L", (-0.14, 0.12, 0.008), -62, "M_Slipper")
    slipper(fg, "Slipper_R", (-0.02, 0.05, 0.008), -75, "M_Slipper")
    trash_bin(fg, "TrashBin", (1.6, 1.02, 0.0), mat="M_Plastic_Teal")
    bf.dehumidifier(fg, (-1.58, -1.66, 0), (0, 0, 90))
    standing_fan(fg, "StandingFan", (-0.5, 1.62, 0), (0, 0, -41))
    strip = group("PowerStrip", fg, (1.28, 1.72, 0.0), (0, 0, 8))
    box("Strip_Body", (0.26, 0.055, 0.035), (0, 0, 0.0175), "M_Plastic_White", strip, bev=0.008)
    for i in range(2):
        box("Strip_Plug%d" % i, (0.03, 0.035, 0.04), (-0.08 + i * 0.075, 0, 0.05), "M_Plastic_Black", strip, bev=0.006)
    sphere("Strip_LED", 0.004, (0.115, -0.028, 0.03), "M_LED_Red", strip, seg=8, rings=4)
    cable("Strip_Cord", [(0.13, 0.01, 0.015), (0.16, 0.08, 0.01), (0.02, 0.14, 0.02), (0.02, 0.165, 0.25)], 0.0035, "M_Cable", strip)


def build(root):
    g = group("Furnishing", root)
    fg = group("Furniture", g)
    bed = bf.bed(fg, (-1.25, 0.83, 0), (0, 0, 0))
    cat(bed, (0.1, -0.3, bf.BED_TOP + 0.035), 70)
    # Phone charger from the socket by the bed head, over the side rail; the phone itself is in the player's hand.
    cable("Phone_Charger", [(0.63, 1.04, 0.25), (0.61, 1.03, 0.05), (0.56, 0.95, 0.012), (0.55, 0.75, 0.012), (0.545, 0.64, 0.25),
                            (0.52, 0.6, 0.5), (0.47, 0.6, bf.BED_TOP + 0.02), (0.4, 0.64, bf.BED_TOP + 0.012), (0.36, 0.7, bf.BED_TOP + 0.01)],
          0.0025, "M_Plastic_White", bed)
    box("Phone_ChargerPlug", (0.012, 0.03, 0.007), (0.35, 0.72, bf.BED_TOP + 0.01), "M_Plastic_White", bed, rot=(0, 0, 30), bev=0.002)
    bf.wardrobe(fg, (-ROOM_X + 0.3, -0.8, 0), (0, 0, 90))
    desk_area(fg)
    shelves(fg)
    tv_corner(fg)
    floor_things(fg)
    board_and_walls(g)
    return g
