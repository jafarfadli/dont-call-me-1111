"""Reusable prop builders: printed sheets, tape, frames, drawers, plants, fabric,
small desk items, the fan and the cat.

Conventions (see dcm.py): metres, Z up. Upright things face local -Y. Wall groups
are rotated so that local -Y points into the room (see bedroom_shell.wall_group).
"""
import math
import random

import bmesh

import atlas
from dcm import (box, cable, cushion, cylinder, extrude_shape, group, join, lathe, mesh_object, plane, smooth, solidify,
                 sphere, subsurf, uv_job)


# ---------------------------------------------------------------- paper

def card(name, parent, w, h, pos, rot, mat, rect, bend=0.0):
    """Upright printed sheet facing local -Y. rot = (tilt back, spin in its plane, yaw)."""
    p = plane(name, w, h, pos, mat, parent, rot=(90 + rot[0], rot[1], rot[2]), nx=4 if bend else 1, ny=1, bend=bend)
    uv_job(p, "planar_local", rect=rect)
    return p


def flat(name, parent, w, h, pos, rot_z, mat, rect, thickness=0.0):
    """Printed item lying flat, face up."""
    if thickness > 0:
        b = box(name, (w, h, thickness), pos, mat, parent, rot=(0, 0, rot_z), bev=min(0.002, thickness * 0.3))
    else:
        b = plane(name, w, h, pos, mat, parent, rot=(0, 0, rot_z))
    uv_job(b, "planar", axis="Z", rect=rect)
    return b


def tape(name, parent, pos, spin, w=0.05, h=0.018):
    """A strip of masking tape on a wall item (faces local -Y)."""
    return plane(name, w, h, pos, "M_Tape", parent, rot=(90, spin, 0))


def taped(name, parent, w, h, x, z, spin, mat, rect, depth=0.0, corners=4, seed=0, bend=0.0):
    """Sheet taped to a wall group: its face at local y = -depth, tape over the corners
    (corners = 4, 2 for the top two, or 1 for a single strip across the top edge)."""
    rnd = random.Random(seed)
    y = -0.002 - depth
    card(name, parent, w, h, (x, y, z), (0, spin, 0), mat, rect, bend=bend)
    ca, sa = math.cos(math.radians(-spin)), math.sin(math.radians(-spin))
    spots = {1: [(0, 1)], 2: [(-1, 1), (1, 1)]}.get(corners, [(-1, 1), (1, 1), (-1, -1), (1, -1)])
    for i, (sx, sz) in enumerate(spots):
        lx, lz = sx * (w / 2 - 0.004), sz * (h / 2 - 0.004)
        px, pz = x + lx * ca - lz * sa, z + lx * sa + lz * ca
        angle = spin + (sx * sz * 45 if sx else 90) + rnd.uniform(-10, 10)
        tape("%s_Tape%d" % (name, i), parent, (px, y - 0.002, pz), angle)


def pin(name, parent, pos, mat):
    """Push pin, head toward local -Y."""
    pg = group(name, parent, pos, (90, 0, 0))
    cylinder(name + "_Head", 0.009, 0.01, (0, 0, 0.004), mat, pg, verts=12, bev=0.002)
    cylinder(name + "_Neck", 0.004, 0.006, (0, 0, 0.0), mat, pg, verts=8)
    return pg


# ---------------------------------------------------------------- frames, boards

def frame(parent, name, pos, rot, w, h, border, mat, rect, standing=False, mount=True, photo_mat="M_Photos", depth=0.025):
    """Picture frame facing local -Y; origin at its back centre (standing: at its foot)."""
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


def cork_board(parent, name, pos, w, h, frame_mat="M_Wood_Light"):
    """Cork board facing local -Y, origin at its back centre. Returns the group."""
    g = group(name, parent, pos)
    name = part_name(name)
    cork = plane(name + "_Cork", w - 0.06, h - 0.06, (0, -0.004, 0), "M_Cork", g, rot=(90, 0, 0))
    uv_job(cork, "planar_local", rect=(0, 0, 2.0 * w, 2.0 * h))
    parts = [
        box(name + "_FrameT", (w, 0.024, 0.035), (0, -0.012, h / 2 - 0.0175), frame_mat, g, bev=0.005),
        box(name + "_FrameB", (w, 0.024, 0.035), (0, -0.012, -h / 2 + 0.0175), frame_mat, g, bev=0.005),
        box(name + "_FrameL", (0.035, 0.024, h - 0.07), (-w / 2 + 0.0175, -0.012, 0), frame_mat, g, bev=0.005),
        box(name + "_FrameR", (0.035, 0.024, h - 0.07), (w / 2 - 0.0175, -0.012, 0), frame_mat, g, bev=0.005),
        box(name + "_Back", (w - 0.02, 0.008, h - 0.02), (0, 0.006, 0), "M_Wood_Dark", g, bev=0),
    ]
    join(name + "_Frame", parts)
    return g


def pinned(board, items):
    """Pin sheets to a cork board: (name, w, h, (x, z), spin, mat, rect, pin material)."""
    for i, (name, w, h, (x, z), r, mat, rect, pin_mat) in enumerate(items):
        y = -0.006 - i * 0.0012
        card(name, board, w, h, (x, y, z), (0, r, 0), mat, rect, bend=0.004 if w > 0.15 else 0.0)
        px = x + math.sin(math.radians(-r)) * (h / 2 - 0.018)
        pz = z + h / 2 - 0.018
        pin(name + "_Pin", board, (px, y - 0.002, pz), pin_mat)


# ---------------------------------------------------------------- furniture parts

def part_name(name):
    """Parts of an interactable are named after it without the INT_ prefix, so only the group counts."""
    return name[4:] if name.startswith("INT_") else name


def drawer(name, parent, pos, w, h, d, front_mat="M_Wood_Light", body_mat="M_Wood_Light", handle_mat="M_Metal_Chrome"):
    """Drawer with front, box and bar handle. Origin at the closed front face centre; the box runs to +Y."""
    g = group(name, parent, pos)
    name = part_name(name)
    box(name + "_Front", (w, 0.02, h), (0, 0.01, 0), front_mat, g, bev=0.004)
    inner = [
        box(name + "_Bottom", (w - 0.04, d - 0.03, 0.01), (0, 0.02 + (d - 0.03) / 2, -h / 2 + 0.02), body_mat, g, bev=0),
        box(name + "_SideL", (0.012, d - 0.03, h - 0.04), (-w / 2 + 0.026, 0.02 + (d - 0.03) / 2, 0), body_mat, g, bev=0),
        box(name + "_SideR", (0.012, d - 0.03, h - 0.04), (w / 2 - 0.026, 0.02 + (d - 0.03) / 2, 0), body_mat, g, bev=0),
        box(name + "_Back", (w - 0.04, 0.012, h - 0.04), (0, 0.02 + d - 0.03, 0), body_mat, g, bev=0),
    ]
    join(name + "_Box", inner)
    hg = [box(name + "_HandleBar", (min(0.12, w * 0.4), 0.012, 0.012), (0, -0.026, 0), handle_mat, g, bev=0.005)]
    for sx in (-1, 1):
        hg.append(cylinder(name + "_HandlePost%d" % sx, 0.005, 0.026, (sx * min(0.05, w * 0.16), 0.0, 0), handle_mat, g,
                           rot=(90, 0, 0), verts=10))
    join(name + "_Handle", hg)
    return g


def curtain(name, parent, x_outer, x_inner_top, ztop, zbot, y, tie_x=None, tie_z=1.15, folds=7, mat="M_Curtain",
            tie_mat="M_Fabric_Mustard"):
    """Curtain hanging from ztop to zbot in the local XZ plane, folds bulging toward -Y.
    The outer edge stays put; when tied, the inner edge sweeps to tie_x at tie_z and flares below."""
    nx, nz = 40, 48
    tied = tie_x is not None
    w_top = abs(x_inner_top - x_outer)

    def inner(z):
        if not tied:
            return x_inner_top
        if z >= tie_z:
            t = (ztop - z) / max(ztop - tie_z, 1e-3)
            return x_inner_top + (tie_x - x_inner_top) * (t ** 1.25)
        t = (tie_z - z) / max(tie_z - zbot, 1e-3)
        return tie_x + (x_inner_top - tie_x) * 0.22 * (t ** 0.8)

    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    verts = []
    for j in range(nz + 1):
        t = j / nz
        z = ztop - (ztop - zbot) * t
        xi = inner(z)
        width = max(abs(xi - x_outer), 0.05)
        squeeze = min(math.sqrt(w_top / width), 3.0)
        amp = 0.02 * squeeze + 0.008 * t
        row = []
        for i in range(nx + 1):
            u = i / nx
            x = x_outer + (xi - x_outer) * u
            phase = 0.35 * math.sin(z * 2.1)
            yy = y - amp * (0.5 + 0.5 * math.sin(u * folds * 2 * math.pi + phase))
            row.append(bm.verts.new((x, yy, z)))
        verts.append(row)
    for j in range(nz):
        for i in range(nx):
            f = bm.faces.new((verts[j][i], verts[j + 1][i], verts[j + 1][i + 1], verts[j][i + 1]))
            for loop, (a, b) in zip(f.loops, ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j))):
                loop[uv].uv = (a / nx * w_top * 1.4, (1 - b / nz) * (ztop - zbot) * 1.2)
    ob = mesh_object(name, bm, mat, parent, (0, 0, 0), (0, 0, 0))
    solidify(ob, 0.006)
    smooth(ob, 180)
    if tied:
        cx = (x_outer + tie_x) / 2
        rx = abs(tie_x - x_outer) / 2 + 0.025
        ring = [(cx + math.cos(a) * rx, y - 0.03 + math.sin(a) * 0.055, tie_z) for a in [k * 2 * math.pi / 23 for k in range(24)]]
        cable(name + "_Tie", ring, 0.012, tie_mat, parent)
    return ob


def drape(name, parent, w, d, pos, mat, top_w, top_d, radius=0.05, puff=0.02, thickness=0.03, seed=0, res=(28, 36),
          hang=(True, True, True, False), rot=(0, 0, 0)):
    """Blanket lying over a box top (top_w x top_d, centred at pos) with the flat size w x d.
    Cloth past the top's edge rolls over a rounded edge and hangs down. `hang` = (-X, +X, -Y, +Y) edges."""
    rnd = random.Random(seed)
    phases = [rnd.uniform(0, 6.28) for _ in range(6)]
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    nx, ny = res
    hw, hd = top_w / 2, top_d / 2

    def fold(u, half, lo_hang, hi_hang):
        """Map a flat coordinate to (position, drop) with a rounded roll over the edge."""
        side = 1 if u > 0 else -1
        over = abs(u) - half
        if over <= 0 or not (hi_hang if side > 0 else lo_hang):
            return u, 0.0
        arc = radius * math.pi / 2
        if over < arc:
            a = over / radius
            return side * (half + radius * math.sin(a)), radius * (1 - math.cos(a))
        return side * (half + radius), radius + (over - arc)

    verts = []
    for j in range(ny + 1):
        v = j / ny
        row = []
        for i in range(nx + 1):
            s = i / nx
            fx, fy = (s - 0.5) * w, (v - 0.5) * d
            x, dx = fold(fx, hw, hang[0], hang[1])
            y, dy = fold(fy, hd, hang[2], hang[3])
            drop = max(dx, dy) + 0.4 * min(dx, dy)
            wr = (math.sin(fx * 9 + phases[0]) * math.sin(fy * 7 + phases[1]) * 0.6 + math.sin(fx * 23 + fy * 5 + phases[2]) * 0.4)
            z = -drop + (puff * (1 - (fx / (w / 2)) ** 2) * (1 - (fy / (d / 2)) ** 2) if drop == 0 else 0.0) + wr * 0.006
            if drop > 0:
                push = 0.012 * math.sin(fx * 11 + fy * 13 + phases[3])
                if dx >= dy:
                    x += (1 if x > 0 else -1) * push
                else:
                    y += (1 if y > 0 else -1) * push
            row.append(bm.verts.new((x, y, z)))
        verts.append(row)
    for j in range(ny):
        for i in range(nx):
            f = bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
            for loop, (a, b) in zip(f.loops, ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1))):
                loop[uv].uv = (a / nx * w * 1.6, b / ny * d * 1.6)
    ob = mesh_object(name, bm, mat, parent, pos, rot)
    solidify(ob, thickness, offset=1.0)
    subsurf(ob, 1)
    return smooth(ob, 180)


# ---------------------------------------------------------------- small things

def mug(parent, name, pos, mat="M_Ceramic_White", rot_z=0):
    g = group(name, parent, pos, (0, 0, rot_z))
    lathe(name + "_Body", [(0, 0), (0.036, 0), (0.04, 0.006), (0.041, 0.09), (0.038, 0.094), (0.035, 0.09), (0.034, 0.012),
                           (0, 0.012)], (0, 0, 0), mat, g, segs=24)
    lathe(name + "_Coffee", [(0, 0.06), (0.035, 0.06), (0.035, 0.062), (0, 0.062)], (0, 0, 0), "M_Tea", g, segs=20)
    ring = [(0.04 + 0.018 * math.cos(a), 0, 0.048 + 0.022 * math.sin(a)) for a in [k * 2 * math.pi / 13 - math.pi / 2 for k in range(12)]]
    cable(name + "_Handle", ring, 0.005, mat, g, res=4, bevel_res=1)
    return g


def books_row(parent, name, pos, length, seed=0, height=(0.19, 0.26), depth=(0.13, 0.18), lean_last=True, rot_z=0):
    """Books standing on a shelf along local +X from pos, spines facing -Y."""
    g = group(name, parent, pos, (0, 0, rot_z))
    rnd = random.Random(seed)
    x = 0.0
    i = 0
    while True:
        bw = rnd.uniform(0.018, 0.045)
        if x + bw > length - (0.06 if lean_last else 0):
            break
        bh = rnd.uniform(*height)
        bd = rnd.uniform(*depth)
        b = box("%s_Book%d" % (name, i), (bw, bd, bh), (x + bw / 2, bd / 2 - 0.09, bh / 2), "M_Books", g,
                rot=(0, rnd.uniform(-1.5, 1.5), 0), bev=0.003)
        col = rnd.randrange(14)
        uv_job(b, "planar", axis="Y", rect=(col / 16, 0.05, (col + 1) / 16, 0.95))
        x += bw + 0.0015
        i += 1
    if lean_last and x < length - 0.03:
        # The last book leans back against the row: top touching it, foot further out.
        bh = rnd.uniform(*height)
        ang = math.radians(18)
        centre = (x + 0.016 + math.sin(ang) * bh / 2, -0.01, math.cos(ang) * bh / 2 + 0.004)
        lean = box(name + "_Lean", (0.03, 0.16, bh), centre, "M_Books", g, rot=(0, -18, 0), bev=0.003)
        col = rnd.randrange(14)
        uv_job(lean, "planar", axis="Y", rect=(col / 16, 0.05, (col + 1) / 16, 0.95))
    return g


def book_stack(parent, name, pos, count, seed=0, rot_z=0, mats=("M_Cover_Red", "M_Cover_Blue", "M_Cover_Yellow", "M_Kraft"),
               size=((0.15, 0.22), (0.21, 0.28))):
    """Books lying in a pile; size = ((min, max) width, (min, max) depth)."""
    g = group(name, parent, pos, (0, 0, rot_z))
    rnd = random.Random(seed)
    z = 0.0
    for i in range(count):
        w, d, t = rnd.uniform(*size[0]), rnd.uniform(*size[1]), rnd.uniform(0.012, 0.03)
        mat = mats[rnd.randrange(len(mats))]
        bg = group("%s_%d" % (name, i), g, (rnd.uniform(-0.01, 0.01), rnd.uniform(-0.01, 0.01), z), (0, 0, rnd.uniform(-8, 8)))
        box("%s_%dCover" % (name, i), (w, d, t), (0, 0, t / 2), mat, bg, bev=0.003)
        box("%s_%dPages" % (name, i), (w - 0.006, d - 0.012, t * 0.8), (0.004, 0, t / 2), "M_Paper_White", bg, bev=0.001)
        z += t
    return g


def pot(parent, name, pos, r_top, r_bot, h, mat="M_Terracotta", saucer=True):
    g = group(name, parent, pos)
    lathe(name + "_Pot", [(0, 0), (r_bot, 0), (r_top, h - 0.03), (r_top + 0.012, h - 0.028), (r_top + 0.012, h),
                          (r_top - 0.008, h), (r_top - 0.01, h - 0.02), (0, h - 0.02)], (0, 0, 0.01 if saucer else 0), mat, g,
          segs=28)
    if saucer:
        lathe(name + "_Saucer", [(0, 0), (r_bot + 0.025, 0), (r_bot + 0.035, 0.016), (r_bot + 0.028, 0.016), (r_bot + 0.016, 0.006),
                                 (0, 0.006)], (0, 0, 0), mat, g, segs=28)
    cylinder(name + "_Soil", r_top - 0.01, 0.004, (0, 0, h - 0.035 + (0.01 if saucer else 0)), "M_Soil", g, verts=20)
    return g


def leaf_card(name, parent, size, pos, rot, rect, curl=0.02):
    p = plane(name, size, size, pos, "M_Leaves", parent, rot=rot, nx=4, ny=4, bend=curl)
    uv_job(p, "planar_local", rect=rect)
    return p


LEAF_POTHOS = (0.5, 0.5, 1.0, 1.0)


def pothos(parent, name, pos, r=0.06, h=0.1, trail=0.3, leaves=8, seed=0, mat="M_Terracotta"):
    """Small pothos in a pot, one vine trailing over the edge toward local -Y."""
    g = pot(parent, name, pos, r, r * 0.8, h, mat=mat, saucer=False)
    rnd = random.Random(seed)
    for k in range(leaves):
        a = k * 2.4 + rnd.uniform(-0.3, 0.3)
        rr = r * rnd.uniform(0.2, 0.9)
        leaf_card("%s_Leaf%d" % (name, k), g, rnd.uniform(0.06, 0.085), (math.cos(a) * rr, math.sin(a) * rr, h + 0.03 + (k % 4) * 0.02),
                  (50 + rnd.uniform(0, 25), 0, math.degrees(a) + 90), LEAF_POTHOS, curl=0.008)
    if trail > 0:
        pts = [(0.0, -r * 0.8, h), (0.01, -r - 0.03, h - 0.02), (0.0, -r - 0.05, h - trail * 0.5), (-0.01, -r - 0.055, h - trail)]
        cable(name + "_Vine", pts, 0.0025, "M_PlantStem", g)
        for k in range(5):
            t = k / 4
            z = h - 0.03 - trail * t
            leaf_card("%s_VLeaf%d" % (name, k), g, 0.05, ((0.015 if k % 2 else -0.015), -r - 0.06, z),
                      (95, 0, 180 + (20 if k % 2 else -20)), LEAF_POTHOS, curl=0.006)
    return g


def trash_bin(parent, name, pos, mat="M_Plastic_Teal"):
    g = group(name, parent, pos)
    lathe(name + "_Body", [(0, 0), (0.1, 0), (0.125, 0.3), (0.13, 0.31), (0.118, 0.31), (0.11, 0.29), (0.09, 0.02), (0, 0.02)],
          (0, 0, 0), mat, g, segs=28)
    sphere(name + "_Paper", 0.05, (0.02, 0.01, 0.29), "M_Paper_White", g, seg=10, rings=6, scale=(1.1, 0.9, 0.8))
    sphere(name + "_Paper2", 0.035, (-0.04, -0.03, 0.3), "M_Paper_Envelope", g, seg=10, rings=6)
    return g


def slipper(parent, name, pos, rot_z, mat, scale=1.0):
    """Indoor slipper: sole plus a closed toe cap."""
    g = group(name, parent, pos, (0, 0, rot_z))
    outline = []
    for i in range(28):
        t = i / 28 * 2 * math.pi
        x = 0.048 * math.cos(t) * (1.0 + 0.16 * math.sin(t))
        y = 0.13 * math.sin(t)
        outline.append((x * scale, y * scale))
    extrude_shape(name + "_Sole", outline, 0.018, (0, 0, 0), "M_Rubber_Black", g, bev=0.004)
    extrude_shape(name + "_Insole", [(x * 0.94, y * 0.96) for x, y in outline], 0.004, (0, 0, 0.017), mat, g, bev=0.001)
    cap = sphere(name + "_Cap", 0.06 * scale, (0, 0.055 * scale, 0.018), mat, g, seg=20, rings=10, scale=(0.85, 1.2, 0.55))
    return g


def standing_fan(parent, name, pos, rot):
    g = group(name, parent, pos, rot)
    lathe(name + "_Base", [(0, 0), (0.17, 0), (0.175, 0.012), (0.16, 0.03), (0.05, 0.05), (0.03, 0.06), (0, 0.06)], (0, 0, 0),
          "M_Plastic_White", g, segs=32)
    cylinder(name + "_Pole", 0.018, 0.95, (0, 0, 0.05), "M_Metal_Chrome", g, verts=12)
    cylinder(name + "_Collar", 0.028, 0.06, (0, 0, 0.62), "M_Plastic_White", g, verts=16)
    head = group(name + "_Head", g, (0, 0, 1.03), (-8, 0, 0))
    lathe(name + "_Motor", [(0, -0.16), (0.05, -0.16), (0.075, -0.12), (0.08, -0.05), (0.07, 0.0), (0.03, 0.02), (0, 0.02)],
          (0, 0, 0), "M_Plastic_White", head, rot=(90, 0, 0), segs=24)
    box(name + "_Badge", (0.07, 0.006, 0.025), (0, 0.162, 0.0), "M_Plastic_Blue", head, bev=0.002)
    cage = []
    R = 0.2
    for k, (r, y) in enumerate(((R, -0.02), (R * 0.75, -0.06), (R * 0.45, -0.08), (R, 0.06), (R * 0.72, 0.09))):
        ring = [(math.cos(a) * r, y, math.sin(a) * r) for a in [i * 2 * math.pi / 40 for i in range(41)]]
        cage.append(cable("%s_Ring%d" % (name, k), ring, 0.003, "M_Metal_Chrome", head))
    for i in range(24):
        a = i * 2 * math.pi / 24
        pts = [(math.cos(a) * 0.03, -0.095, math.sin(a) * 0.03), (math.cos(a) * R * 0.6, -0.08, math.sin(a) * R * 0.6),
               (math.cos(a) * R, -0.02, math.sin(a) * R), (math.cos(a) * R * 0.85, 0.08, math.sin(a) * R * 0.85)]
        cage.append(cable("%s_Spoke%d" % (name, i), pts, 0.0022, "M_Metal_Chrome", head))
    join(name + "_Cage", cage)
    cylinder(name + "_CageRim", R + 0.006, 0.012, (0, -0.02, 0), "M_Plastic_White", head, rot=(90, 0, 0), verts=40, anchor="center")
    lathe(name + "_Hub", [(0, 0), (0.035, 0), (0.03, 0.02), (0, 0.03)], (0, -0.03, 0), "M_Fan_Blade", head, rot=(90, 0, 0), segs=16)
    for i in range(3):
        a = i * 120 + 20
        extrude_shape("%s_Blade%d" % (name, i), [(0.02, -0.03), (0.17, -0.07), (0.185, 0.0), (0.16, 0.06), (0.02, 0.03)], 0.004,
                      (0, -0.028, 0), "M_Fan_Blade", head, rot=(90, a, 0), bev=0.0015)
    sphere(name + "_Knob", 0.022, (0, -0.1, 0), "M_Plastic_White", head, seg=12, rings=6)
    box(name + "_Switch", (0.06, 0.03, 0.02), (0.06, -0.02, 0.62 + 0.03), "M_Plastic_Blue", g, bev=0.006)
    return g


def cat(parent, pos, rot_z):
    """Orange tabby asleep in the sun."""
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
        cylinder("Cat_Ear%d" % sy, 0.032, 0.062, (0.012, sy * 0.045, 0.045), "M_Cat", head, rot=(sy * -20, 8, 0), verts=3, r2=0.002)
        cylinder("Cat_EarIn%d" % sy, 0.02, 0.042, (0.0, sy * 0.045, 0.05), "M_Flower_Pink", head, rot=(sy * -20, 8, 0), verts=3,
                 r2=0.001)
        eye = [(-0.066 + 0.006 * math.sin(a), sy * (0.03 + 0.014 * math.cos(a)), 0.016 - 0.007 * math.sin(a))
               for a in [k * math.pi / 8 for k in range(9)]]
        cable("Cat_Eye%d" % sy, eye, 0.0026, "M_Cable", head, res=3, bevel_res=1)
        for k in range(3):
            cable("Cat_Whisker%d_%d" % (sy, k), [(-0.075, sy * 0.022, -0.02 + k * 0.006), (-0.1, sy * (0.06 + k * 0.004), -0.018 + k * 0.012)],
                  0.0014, "M_Cat_Light", head, res=2, bevel_res=0)
    tail = [(0.18, 0.06, 0.04), (0.215, -0.05, 0.035), (0.12, -0.145, 0.03), (-0.02, -0.165, 0.028), (-0.13, -0.13, 0.025),
            (-0.19, -0.08, 0.022)]
    cable("Cat_Tail", tail, 0.021, "M_Cat", g, res=8, bevel_res=2)
    sphere("Cat_TailTip", 0.02, (-0.19, -0.08, 0.022), "M_Cat_Light", g, seg=12, rings=6)
    return g


# ---------------------------------------------------------------- interactables

def wallet(parent, pos, rot_z):
    g = group("INT_Wallet", parent, pos, (0, 0, rot_z))
    box("Wallet_Body", (0.115, 0.095, 0.02), (0, 0, 0.01), "M_Leather", g, bev=0.007, segs=3)
    box("Wallet_Flap", (0.112, 0.012, 0.006), (0, -0.045, 0.021), "M_Leather", g, bev=0.002)
    stitch = [(math.cos(a) * 0.05, math.sin(a) * 0.04, 0.0203) for a in [k * 2 * math.pi / 40 for k in range(41)]]
    cable("Wallet_Stitch", [(x * 1.03 if abs(x) > 0.04 else x, y, z) for x, y, z in stitch], 0.0007, "M_Paper_Envelope", g, res=2,
          bevel_res=0)
    cardp = box("Wallet_Card", (0.085, 0.054, 0.001), (0.012, 0.035, 0.012), "M_Labels", g, rot=(0, 0, 4), bev=0)
    uv_job(cardp, "planar", axis="Z", rect=atlas.region("labels", "card_front"))
    box("Wallet_Snap", (0.014, 0.014, 0.003), (0.0, -0.04, 0.0245), "M_Metal_Chrome", g, bev=0.001)
    return g


def rulebook(parent, pos, rot_z):
    """Kraft notebook of the rules the player writes down, with a pen."""
    g = group("INT_Rulebook", parent, pos, (0, 0, rot_z))
    box("Rulebook_Cover", (0.152, 0.212, 0.014), (0, 0, 0.007), "M_Kraft", g, bev=0.004)
    box("Rulebook_Pages", (0.144, 0.206, 0.01), (0.004, 0, 0.007), "M_Paper_White", g, bev=0.001)
    lab = plane("Rulebook_Label", 0.1, 0.052, (0.0, 0.03, 0.0142), "M_Labels", g)
    uv_job(lab, "planar", axis="Z", rect=atlas.region("labels", "rulebook"))
    box("Rulebook_Band", (0.012, 0.214, 0.016), (0.05, 0, 0.007), "M_Plastic_Black", g, bev=0.002)
    pen = group("Pen", g, (-0.02, -0.05, 0.0175), (0, 90, 30))
    lathe("Pen_Body", [(0, 0), (0.0045, 0.0), (0.0045, 0.13), (0.002, 0.14), (0, 0.142)], (0, 0, -0.07), "M_Plastic_Blue", pen, segs=10)
    lathe("Pen_Cap", [(0, 0), (0.0052, 0.0), (0.0052, 0.045), (0, 0.048)], (0, 0, -0.078), "M_Plastic_White", pen, segs=10)
    return g


def newspaper(parent, pos, rot_z):
    """Today's paper, folded in half, front page up."""
    g = group("INT_Newspaper", parent, pos, (0, 0, rot_z))
    box("News_Back", (0.305, 0.415, 0.004), (0.003, -0.004, 0.002), "M_Paper_White", g, bev=0.001)
    front = box("News_Front", (0.3, 0.41, 0.004), (0, 0, 0.006), "M_Newspaper", g, bev=0.001)
    uv_job(front, "planar", axis="Z", rect=(0, 0, 1, 1))
    return g


def bills(parent):
    """Bills and a receipt in the drawer."""
    g = group("DrawerContents", parent)
    env = [("M_Paper_Envelope", 0.0, 3), ("M_Paper_White", 0.004, -5), ("M_Paper_Envelope", 0.008, 9)]
    for i, (mat, dz, rz) in enumerate(env):
        box("Bill%d" % i, (0.22, 0.11, 0.003), (-0.05 + i * 0.03, -0.02 + i * 0.02, dz), mat, g, rot=(0, 0, rz), bev=0)
    box("Bill_Stamp", (0.03, 0.02, 0.0005), (0.02, 0.03, 0.0098), "M_Plastic_Red", g, rot=(0, 0, 9), bev=0)
    rx0, ry0, rx1, ry1 = atlas.region("notes", "receipt")
    flat("Bill_Receipt", g, 0.07, 0.14, (0.13, 0.0, 0.012), 14, "M_Notes", (rx0, ry0, rx1, ry1), thickness=0.001)
    return g
