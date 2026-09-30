"""Bedroom shell: floor, ceiling, painted walls, the sliding window with its curtain
box, the door, cherry mouldings, switches, the ondol thermostat, sockets, the wall
air conditioner and the ceiling light.

The room is a small bedroom in a twenty-year-old Seoul villa. Plan (metres):
west wall = window over the bed, north wall = bed head, shelf and desk,
east wall = shelves and mirror, south wall = door and TV cabinet.
"""
import math

import atlas
from dcm import box, cable, cylinder, group, join, plane, uv_job
from props import curtain

ROOM_X = 1.8          # interior half width (west-east)
ROOM_Y = 1.9          # interior half depth (south-north)
ROOM_H = 2.4
WALL_T = 0.15

WINDOW = (0.15, 1.55, 0.95, 2.1)    # y0, y1, z0, z1 in the west wall
DOOR = (0.65, 1.5, 0.0, 2.05)       # x0, x1, z0, z1 in the south wall
TRANSOM_Z = 1.74

WALLS = {   # name: (position of the inner face, rotation) for wall_group
    "north": ((0.0, ROOM_Y, 0.0), 0),
    "south": ((0.0, -ROOM_Y, 0.0), 180),
    "west": ((-ROOM_X, 0.0, 0.0), 90),
    "east": ((ROOM_X, 0.0, 0.0), -90),
}


def wall_group(name, parent, wall):
    """Group on the inner face of a wall. Local -Y points into the room and local X runs
    left to right as seen from inside, with 0 at the middle of the wall."""
    pos, rot = WALLS[wall]
    return group(name, parent, pos, (0, 0, rot))


def wall_rects(lo, hi, height, openings):
    """Split a wall into rectangles around its openings (a0, a1, z0, z1)."""
    cuts = sorted(set([lo, hi] + [o[0] for o in openings] + [o[1] for o in openings]))
    rects = []
    for a, b in zip(cuts, cuts[1:]):
        if b - a < 1e-6:
            continue
        mid = (a + b) / 2
        holes = sorted((o[2], o[3]) for o in openings if o[0] < mid < o[1])
        z = 0.0
        for h0, h1 in holes:
            if h0 > z + 1e-6:
                rects.append((a, b, z, h0))
            z = max(z, h1)
        if z < height - 1e-6:
            rects.append((a, b, z, height))
    return rects


def build_wall(name, parent, axis, fixed, lo, hi, openings, band, flip):
    """Wall slab along `axis` with holes; its paint comes from one band of the wall atlas."""
    parts = []
    for i, (a, b, z0, z1) in enumerate(wall_rects(lo, hi, ROOM_H, openings)):
        if axis == "x":
            size, pos = (b - a, WALL_T, z1 - z0), ((a + b) / 2, fixed, (z0 + z1) / 2)
        else:
            size, pos = (WALL_T, b - a, z1 - z0), (fixed, (a + b) / 2, (z0 + z1) / 2)
        parts.append(box("%s_%d" % (name, i), size, pos, "M_Wall", parent, bev=0))
    wall = join(name, parts)
    uv_job(wall, "planar", axis="Y" if axis == "x" else "X", rect=atlas.region("walls", band), flip_u=flip)
    return wall


# ---------------------------------------------------------------- window

def window(parent):
    """PVC double-track sliding window with a fixed transom, insect screen and stone sill.
    Built facing local -Y in a group on the wall's centre plane (local X = world Y)."""
    x0, x1, z0, z1 = WINDOW
    g = group("Window", parent, (-ROOM_X - WALL_T / 2, 0, 0), (0, 0, 90))
    fw, fd = 0.05, 0.11
    parts = [
        box("WinFrame_L", (fw, fd, z1 - z0), (x0 + fw / 2, -0.01, (z0 + z1) / 2), "M_PVC", g, bev=0.004),
        box("WinFrame_R", (fw, fd, z1 - z0), (x1 - fw / 2, -0.01, (z0 + z1) / 2), "M_PVC", g, bev=0.004),
        box("WinFrame_T", (x1 - x0, fd, fw), ((x0 + x1) / 2, -0.01, z1 - fw / 2), "M_PVC", g, bev=0.004),
        box("WinFrame_B", (x1 - x0, fd, 0.04), ((x0 + x1) / 2, -0.01, z0 + 0.02), "M_PVC", g, bev=0.004),
        box("WinFrame_Transom", (x1 - x0, fd, 0.04), ((x0 + x1) / 2, -0.01, TRANSOM_Z), "M_PVC", g, bev=0.004),
    ]
    for k in range(3):
        x = x0 + fw + (x1 - x0 - 2 * fw) * (k + 1) / 4
        parts.append(box("WinTransom_Bar%d" % k, (0.022, 0.03, z1 - fw - TRANSOM_Z - 0.02), (x, -0.02, (z1 - fw + TRANSOM_Z + 0.02) / 2),
                         "M_PVC", g, bev=0.003))
    for k, y in enumerate((-0.035, 0.005, 0.045)):
        parts.append(box("WinTrack%d" % k, (x1 - x0 - 2 * fw, 0.006, 0.012), ((x0 + x1) / 2, y + 0.018, z0 + 0.046), "M_PVC", g, bev=0.001))
    join("Window_Frame", parts)
    box("Window_TransomGlass", (x1 - x0 - 2 * fw, 0.004, z1 - fw - TRANSOM_Z - 0.02), ((x0 + x1) / 2, -0.01, (z1 - fw + TRANSOM_Z + 0.02) / 2),
        "M_Glass", g, bev=0)

    # Two sashes on separate tracks: the south one closed, the north one slid over it.
    iw = x1 - x0 - 2 * fw
    sw = iw / 2 + 0.03
    sz0, sz1 = z0 + 0.052, TRANSOM_Z - 0.022
    sh = sz1 - sz0
    for side, left, y, slide in (("S", x0 + fw, -0.035, 0.0), ("N", x0 + fw + iw - sw, 0.005, -0.42)):
        sg = group("Sash_" + side, g, (left + slide, y, sz0))
        st, dp = 0.045, 0.032
        sp = [
            box("Sash%s_StileA" % side, (st, dp, sh), (st / 2, 0, sh / 2), "M_PVC", sg, bev=0.004),
            box("Sash%s_StileB" % side, (st, dp, sh), (sw - st / 2, 0, sh / 2), "M_PVC", sg, bev=0.004),
            box("Sash%s_RailB" % side, (sw, dp, st), (sw / 2, 0, st / 2), "M_PVC", sg, bev=0.004),
            box("Sash%s_RailT" % side, (sw, dp, st), (sw / 2, 0, sh - st / 2), "M_PVC", sg, bev=0.004),
        ]
        join("Sash%s_Frame" % side, sp)
        box("Sash%s_Glass" % side, (sw - 2 * st + 0.01, 0.004, sh - 2 * st + 0.01), (sw / 2, 0, sh / 2), "M_Glass", sg, bev=0)
        pull_x = sw - st / 2 if side == "S" else st / 2
        box("Sash%s_Pull" % side, (0.016, 0.01, 0.16), (pull_x, -dp / 2 - 0.004, sh * 0.48), "M_Plastic_Gray", sg, bev=0.004)
        if side == "S":
            lg = group("Sash_Latch", sg, (sw - st / 2, -dp / 2 - 0.006, sh * 0.62))
            cylinder("Latch_Base", 0.012, 0.008, (0, 0, 0), "M_Metal_Chrome", lg, rot=(90, 0, 0), verts=14, anchor="center")
            box("Latch_Lever", (0.05, 0.008, 0.012), (-0.018, -0.006, 0.004), "M_Metal_Chrome", lg, rot=(0, -20, 0), bev=0.003)

    # Insect screen on the outer track over the open part.
    scr = group("InsectScreen", g, (x1 - fw - sw, 0.045, sz0))
    sp = [
        box("Screen_StileA", (0.03, 0.02, sh), (0.015, 0, sh / 2), "M_Metal_Gray", scr, bev=0.002),
        box("Screen_StileB", (0.03, 0.02, sh), (sw - 0.015, 0, sh / 2), "M_Metal_Gray", scr, bev=0.002),
        box("Screen_RailB", (sw, 0.02, 0.03), (sw / 2, 0, 0.015), "M_Metal_Gray", scr, bev=0.002),
        box("Screen_RailT", (sw, 0.02, 0.03), (sw / 2, 0, sh - 0.015), "M_Metal_Gray", scr, bev=0.002),
    ]
    join("Screen_Frame", sp)
    box("Screen_Mesh", (sw - 0.05, 0.002, sh - 0.05), (sw / 2, 0, sh / 2), "M_Screen", scr, bev=0)

    # Stone sill on the room side, concrete ledge outside.
    box("Window_Sill", (x1 - x0 + 0.1, WALL_T / 2 + 0.03, 0.03), ((x0 + x1) / 2, -WALL_T / 2 + 0.01, z0 - 0.015), "M_Stone_Sill", g,
        bev=0.006)
    box("Window_Ledge", (x1 - x0 + 0.12, 0.12, 0.05), ((x0 + x1) / 2, WALL_T / 2 + 0.05, z0 - 0.03), "M_Concrete", g, bev=0.008)

    # Curtain box under the ceiling, rod with rings, two tied-back curtains.
    cb0, cb1 = x0 - 0.35, ROOM_Y
    depth = 0.17
    ry = -WALL_T / 2 - 0.1
    box("CurtainBox_Front", ((cb1 - cb0), 0.018, 0.2), ((cb0 + cb1) / 2, -WALL_T / 2 - depth, ROOM_H - 0.1), "M_Wood_Cherry", g, bev=0.004)
    box("CurtainBox_End", (0.018, depth, 0.2), (cb0 + 0.009, -WALL_T / 2 - depth / 2, ROOM_H - 0.1), "M_Wood_Cherry", g, bev=0.003)
    rod_z = ROOM_H - 0.13
    cylinder("Curtain_Rod", 0.011, cb1 - cb0 - 0.06, (cb0 + 0.03, ry, rod_z), "M_Metal_Chrome", g, rot=(0, 90, 0), verts=14)
    rings = []
    for k in range(22):
        x = cb0 + 0.1 + (cb1 - cb0 - 0.2) * k / 21
        pts = [(x, ry + math.cos(a) * 0.02, rod_z + math.sin(a) * 0.02) for a in [i * 2 * math.pi / 12 for i in range(13)]]
        rings.append(cable("Curtain_Ring%d" % k, pts, 0.0025, "M_Metal_Chrome", g, res=2, bevel_res=1))
    join("Curtain_Rings", rings)
    top = rod_z - 0.03
    curtain("Curtain_S", g, cb0 + 0.06, x0 + 0.55, top, 0.98, ry - 0.005, tie_x=x0 + 0.02, tie_z=1.32, folds=6)
    curtain("Curtain_N", g, cb1 - 0.1, x1 - 0.5, top, 0.98, ry - 0.005, tie_x=x1 - 0.03, tie_z=1.32, folds=6)
    return g


# ---------------------------------------------------------------- door

def door(parent):
    """Cherry-film door with frame, casing, threshold, lever handle and door stop.
    Built in a group on the south wall's centre plane, local -Y toward the room, local X = -world X."""
    x0, x1, _, z1 = DOOR
    g = group("Door", parent, (0, -ROOM_Y - WALL_T / 2, 0), (0, 0, 180))
    lx0, lx1 = -x1, -x0
    jamb, depth = 0.045, WALL_T + 0.02
    parts = [
        box("DoorJamb_L", (jamb, depth, z1), (lx0 + jamb / 2, 0, z1 / 2), "M_Wood_Cherry", g),
        box("DoorJamb_R", (jamb, depth, z1), (lx1 - jamb / 2, 0, z1 / 2), "M_Wood_Cherry", g),
        box("DoorJamb_Head", (lx1 - lx0, depth, jamb), ((lx0 + lx1) / 2, 0, z1 - jamb / 2), "M_Wood_Cherry", g),
        box("DoorStop_L", (0.012, 0.03, z1 - jamb), (lx0 + jamb + 0.006, 0.03, (z1 - jamb) / 2), "M_Wood_Cherry", g, bev=0.002),
        box("DoorStop_R", (0.012, 0.03, z1 - jamb), (lx1 - jamb - 0.006, 0.03, (z1 - jamb) / 2), "M_Wood_Cherry", g, bev=0.002),
        box("DoorStop_T", (lx1 - lx0 - 2 * jamb, 0.03, 0.012), ((lx0 + lx1) / 2, 0.03, z1 - jamb - 0.006), "M_Wood_Cherry", g, bev=0.002),
    ]
    cw, ct = 0.065, 0.014
    cy = -WALL_T / 2 - ct / 2
    parts += [
        box("DoorCasing_L", (cw, ct, z1 + cw), (lx0 - cw / 2 + 0.01, cy, (z1 + cw) / 2), "M_Wood_Cherry", g, bev=0.004),
        box("DoorCasing_R", (cw, ct, z1 + cw), (lx1 + cw / 2 - 0.01, cy, (z1 + cw) / 2), "M_Wood_Cherry", g, bev=0.004),
        box("DoorCasing_T", (lx1 - lx0 + 2 * cw - 0.02, ct, cw), ((lx0 + lx1) / 2, cy, z1 + cw / 2), "M_Wood_Cherry", g, bev=0.004),
        box("Door_Threshold", (lx1 - lx0, depth, 0.022), ((lx0 + lx1) / 2, 0, 0.011), "M_Wood_Cherry", g, bev=0.004),
    ]
    join("Door_Frame", parts)

    # Leaf: hinged on the east jamb, latch on the west, closed.
    leaf_w = lx1 - lx0 - 2 * jamb - 0.006
    leaf_h = z1 - jamb - 0.022 - 0.008
    leaf = group("Door_Leaf", g, (lx0 + jamb + 0.003, -0.006, 0.026))
    box("Door_Slab", (leaf_w, 0.04, leaf_h), (leaf_w / 2, 0, leaf_h / 2), "M_Wood_Cherry", leaf, bev=0.005)
    panels = []
    for k, (pz0, pz1) in enumerate(((0.16, 0.8), (0.98, leaf_h - 0.16))):
        pw, ph = leaf_w - 0.26, pz1 - pz0
        for face, fy in (("R", -0.021), ("H", 0.021)):
            panels.append(box("Door_Panel%s%d" % (face, k), (pw, 0.006, ph), (leaf_w / 2, fy, (pz0 + pz1) / 2), "M_Wood_Cherry_Dark", leaf,
                              bev=0.012, segs=1))
    join("Door_Panels", panels)
    hx = leaf_w - 0.07
    hw = []
    for face, s in (("R", -1), ("H", 1)):
        hw.append(cylinder("Door_Rose%s" % face, 0.028, 0.01, (hx, s * 0.025, 1.0), "M_Metal_Chrome", leaf, rot=(90, 0, 0), verts=24,
                           anchor="center", bev=0.003))
        hw.append(cylinder("Door_LeverNeck%s" % face, 0.009, 0.035, (hx, s * 0.04, 1.0), "M_Metal_Chrome", leaf, rot=(90, 0, 0), verts=12,
                           anchor="center"))
        hw.append(box("Door_Lever%s" % face, (0.12, 0.018, 0.018), (hx - 0.055, s * 0.055, 1.0), "M_Metal_Chrome", leaf, bev=0.007))
    join("Door_Handle", hw)
    cylinder("Door_LockButton", 0.006, 0.01, (hx, -0.058, 1.0), "M_Metal_Chrome", leaf, rot=(90, 0, 0), verts=10, anchor="center")
    for k, hz in enumerate((0.2, leaf_h - 0.2)):
        cylinder("Door_Hinge%d" % k, 0.008, 0.1, (-0.003, -0.018, hz - 0.05), "M_Gold", leaf, verts=10)
    ds = group("DoorStop", leaf, (leaf_w - 0.12, -0.022, 0.0))
    box("DoorStop_Plate", (0.05, 0.004, 0.06), (0, 0.0, 0.05), "M_Metal_Chrome", ds, bev=0.002)
    cylinder("DoorStop_Arm", 0.006, 0.04, (0, -0.018, 0.0), "M_Metal_Chrome", ds, verts=10)
    box("DoorStop_Foot", (0.03, 0.02, 0.008), (0, -0.018, 0.004), "M_Rubber_Black", ds, bev=0.003)
    return g


# ---------------------------------------------------------------- trims, electrics, AC, light

def trims(parent):
    """Cherry skirting and crown moulding, the look of every Korean home built around 2005."""
    g = group("Trims", parent)
    sk_h, sk_t = 0.07, 0.012
    runs = [
        ("SkirtNorth", "x", ROOM_Y - sk_t / 2, -ROOM_X, ROOM_X),
        ("SkirtSouth_A", "x", -ROOM_Y + sk_t / 2, -ROOM_X, DOOR[0] - 0.055),
        ("SkirtSouth_B", "x", -ROOM_Y + sk_t / 2, DOOR[1] + 0.055, ROOM_X),
        ("SkirtEast", "y", ROOM_X - sk_t / 2, -ROOM_Y, ROOM_Y),
        ("SkirtWest", "y", -ROOM_X + sk_t / 2, -ROOM_Y, ROOM_Y),
    ]
    parts = []
    for name, axis, fixed, a, b in runs:
        if axis == "x":
            parts.append(box(name, (b - a, sk_t, sk_h), ((a + b) / 2, fixed, sk_h / 2), "M_Wood_Cherry", g, bev=0.003))
        else:
            parts.append(box(name, (sk_t, b - a, sk_h), (fixed, (a + b) / 2, sk_h / 2), "M_Wood_Cherry", g, bev=0.003))
    join("Skirting", parts)
    c_h, c_d = 0.045, 0.022
    parts = [
        box("Crown_N", (2 * ROOM_X, c_d, c_h), (0, ROOM_Y - c_d / 2, ROOM_H - c_h / 2), "M_Wood_Cherry", g, bev=0.008, segs=2),
        box("Crown_S", (2 * ROOM_X, c_d, c_h), (0, -ROOM_Y + c_d / 2, ROOM_H - c_h / 2), "M_Wood_Cherry", g, bev=0.008, segs=2),
        box("Crown_E", (c_d, 2 * ROOM_Y, c_h), (ROOM_X - c_d / 2, 0, ROOM_H - c_h / 2), "M_Wood_Cherry", g, bev=0.008, segs=2),
        box("Crown_W", (c_d, 2 * ROOM_Y, c_h), (-ROOM_X + c_d / 2, 0, ROOM_H - c_h / 2), "M_Wood_Cherry", g, bev=0.008, segs=2),
    ]
    join("Crown", parts)
    return g


def socket(name, parent, x, z):
    """Korean (type F) socket on a wall group."""
    s = group(name, parent, (x, 0, z))
    box(name + "_Plate", (0.075, 0.012, 0.075), (0, -0.006, 0), "M_Plastic_White", s, bev=0.004)
    cylinder(name + "_Well", 0.021, 0.006, (0, -0.013, 0), "M_Plastic_White", s, rot=(90, 0, 0), verts=20, anchor="center")
    for dx in (-0.009, 0.009):
        cylinder(name + "_Hole%d" % int(dx * 1000), 0.0028, 0.004, (dx, -0.016, 0), "M_Plastic_Black", s, rot=(90, 0, 0), verts=8,
                 anchor="center")
    return s


def electrics(parent):
    g = group("Electrical", parent)
    south = wall_group("SouthWallElectrics", g, "south")
    # Local x on the south wall is -world x: the switch sits just west of the door, on the latch side.
    sw = group("LightSwitch", south, (-0.42, 0, 1.2))
    box("Switch_Plate", (0.085, 0.012, 0.125), (0, -0.006, 0), "M_Plastic_White", sw, bev=0.005)
    for k, dx in enumerate((-0.018, 0.018)):
        box("Switch_Rocker%d" % k, (0.03, 0.01, 0.085), (dx, -0.014, 0), "M_Plastic_White", sw, rot=(6 if k else -6, 0, 0), bev=0.004)
    th = group("OndolThermostat", south, (-0.42, 0, 1.45))
    box("Thermostat_Body", (0.15, 0.024, 0.085), (0, -0.012, 0), "M_Plastic_White", th, bev=0.008)
    face = plane("Thermostat_Face", 0.136, 0.068, (0, -0.0245, 0), "M_Labels", th, rot=(90, 0, 0))
    uv_job(face, "planar_local", rect=atlas.region("labels", "thermostat"))
    cable("Thermostat_Wire", [(0.07, -0.004, -0.03), (0.08, -0.004, -0.08), (0.085, -0.004, -0.2)], 0.0018, "M_Plastic_White", th)
    socket("Socket_TV", south, 0.3, 0.25)
    north = wall_group("NorthWallElectrics", g, "north")
    socket("Socket_Desk", north, 1.3, 0.25)
    socket("Socket_Bed", north, -0.62, 0.25)
    west = wall_group("WestWallElectrics", g, "west")
    socket("Socket_AC", west, -0.92, 2.2)
    return g


def air_conditioner(parent):
    """Wall-mounted split unit high on the west (outside) wall, left of the window."""
    west = wall_group("AirConditioner", parent, "west")
    g = group("AC", west, (-1.42, 0, 2.17))
    w, h, d = 0.84, 0.27, 0.2
    box("AC_Body", (w, d, h), (0, -d / 2, 0), "M_Plastic_White", g, bev=0.03, segs=3)
    box("AC_Intake", (w - 0.06, 0.02, 0.012), (0, -d * 0.4, h / 2 + 0.0), "M_Plastic_Gray", g, bev=0.004)
    box("AC_Vent", (w - 0.1, 0.03, 0.04), (0, -d + 0.02, -h / 2 + 0.035), "M_Plastic_Black", g, bev=0.006)
    box("AC_Louver", (w - 0.12, 0.05, 0.012), (0, -d - 0.005, -h / 2 + 0.03), "M_Plastic_White", g, rot=(-30, 0, 0), bev=0.004)
    panel = plane("AC_Display", 0.16, 0.08, (w / 2 - 0.13, -d - 0.0005, 0.03), "M_Labels", g, rot=(90, 0, 0))
    uv_job(panel, "planar_local", rect=atlas.region("labels", "aircon"))
    cable("AC_Power", [(w / 2 - 0.03, -0.03, -h / 2 + 0.02), (w / 2 + 0.05, -0.01, -0.08), (0.5, -0.008, 0.03)], 0.004, "M_Plastic_White", g)
    return west


def ceiling_light(parent):
    """Square LED panel, the standard Korean room light."""
    g = group("CeilingLight", parent, (0.0, 0.1, ROOM_H))
    box("Light_Frame", (0.56, 0.56, 0.05), (0, 0, -0.025), "M_Plastic_White", g, bev=0.01)
    box("Light_Diffuser", (0.5, 0.5, 0.012), (0, 0, -0.05), "M_LampDiffuser", g, bev=0.004)
    return g


def build(root):
    g = group("Architecture", root)
    floor = box("Floor", (2 * ROOM_X + 2 * WALL_T, 2 * ROOM_Y + 2 * WALL_T, 0.1), (0, 0, -0.05), "M_Floor", g, bev=0)
    uv_job(floor, "box", scale=1 / 1.44)
    box("Ceiling", (2 * ROOM_X + 2 * WALL_T, 2 * ROOM_Y + 2 * WALL_T, 0.12), (0, 0, ROOM_H + 0.06), "M_Ceiling", g, bev=0)
    span = ROOM_X + WALL_T
    build_wall("Wall_North", g, "x", ROOM_Y + WALL_T / 2, -span, span, [], "north", False)
    build_wall("Wall_South", g, "x", -ROOM_Y - WALL_T / 2, -span, span, [DOOR], "south", True)
    build_wall("Wall_West", g, "y", -ROOM_X - WALL_T / 2, -ROOM_Y, ROOM_Y, [WINDOW], "west", False)
    build_wall("Wall_East", g, "y", ROOM_X + WALL_T / 2, -ROOM_Y, ROOM_Y, [], "east", True)
    window(g)
    door(g)
    trims(g)
    electrics(g)
    air_conditioner(g)
    ceiling_light(g)
    return g
