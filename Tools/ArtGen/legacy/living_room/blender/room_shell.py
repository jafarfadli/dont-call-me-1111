"""Room shell: floor, ceiling, walls with openings, window, doors, vents, trims."""
import math

from dcm import box, cable, cylinder, extrude_shape, group, join, plane, uv_job, lathe, sphere, smooth

ROOM_X = 3.0          # interior half width (east-west)
ROOM_Y = 2.5          # interior half depth (street side is +Y)
ROOM_H = 2.9
WALL_T = 0.15

WINDOW = (-1.7, -0.1, 0.85, 2.15)       # x0, x1, z0, z1 on the front wall
FRONT_DOOR = (1.45, 2.35, 0.0, 2.1)
BACK_DOOR = (-2.35, -1.45, 0.0, 2.1)    # on the back wall
VENT_WINDOW = (-1.7, -0.1, 2.3, 2.52)
VENT_DOOR = (1.45, 2.35, 2.25, 2.47)


def wall_rects(lo, hi, height, openings):
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


def build_wall(name, parent, axis, fixed, lo, hi, openings, mat="M_Wall"):
    parts = []
    for i, (a, b, z0, z1) in enumerate(wall_rects(lo, hi, ROOM_H, openings)):
        if axis == "x":
            size, pos = (b - a, WALL_T, z1 - z0), ((a + b) / 2, fixed, (z0 + z1) / 2)
        else:
            size, pos = (WALL_T, b - a, z1 - z0), (fixed, (a + b) / 2, (z0 + z1) / 2)
        parts.append(box("%s_%d" % (name, i), size, pos, mat, parent, bev=0))
    return join(name, parts)


def vent_blocks(name, parent, x0, x1, z0, z1, y_center, depth=0.12):
    """Concrete ventilation grille ("roster"): blocks with diamond holes."""
    g = group(name, parent)
    width = x1 - x0
    count = max(1, round(width / 0.2))
    bw = width / count
    h = z1 - z0
    pieces = []
    for i in range(count):
        cx = x0 + bw * (i + 0.5)
        cz = z0 + h / 2
        hw, hh = bw / 2, h / 2
        inset = 0.022
        tris = [
            [(-hw, -hh), (0, -hh + inset), (-hw + inset, 0)],
            [(hw, -hh), (hw - inset, 0), (0, -hh + inset)],
            [(hw, hh), (0, hh - inset), (hw - inset, 0)],
            [(-hw, hh), (-hw + inset, 0), (0, hh - inset)],
        ]
        for k, tri in enumerate(tris):
            full = [tri[0], tri[1], tri[2]]
            corner = tri[0]
            pieces.append(extrude_shape("%s_c%d_%d" % (name, i, k), [(p[0], p[1]) for p in full], depth,
                                        (cx, y_center + depth / 2, cz), "M_Concrete", g, rot=(90, 0, 0)))
        frame_w = 0.012
        pieces.append(box("%s_fL%d" % (name, i), (frame_w, depth, h), (cx - hw + frame_w / 2, y_center, cz), "M_Concrete", g, bev=0))
        pieces.append(box("%s_fR%d" % (name, i), (frame_w, depth, h), (cx + hw - frame_w / 2, y_center, cz), "M_Concrete", g, bev=0))
        pieces.append(box("%s_fB%d" % (name, i), (bw, depth, frame_w), (cx, y_center, z0 + frame_w / 2), "M_Concrete", g, bev=0))
        pieces.append(box("%s_fT%d" % (name, i), (bw, depth, frame_w), (cx, y_center, z1 - frame_w / 2), "M_Concrete", g, bev=0))
        pieces.append(box("%s_d%d" % (name, i), (0.03, depth * 0.96, 0.03), (cx, y_center, cz), "M_Concrete", g, rot=(0, 45, 0), bev=0))
    join(name + "_Mesh", pieces)
    return g


def door_frame(name, parent, x0, x1, z1, y_wall_center, room_side_y, facing, jamb=0.06):
    """Jambs + head inside the opening, plus casing trims on the room side."""
    g = group(name, parent)
    h = z1
    depth = WALL_T + 0.01
    parts = [
        box(name + "_JambL", (jamb, depth, h), (x0 + jamb / 2, y_wall_center, h / 2), "M_Wood_Dark", g),
        box(name + "_JambR", (jamb, depth, h), (x1 - jamb / 2, y_wall_center, h / 2), "M_Wood_Dark", g),
        box(name + "_Head", (x1 - x0, depth, jamb), ((x0 + x1) / 2, y_wall_center, h - jamb / 2), "M_Wood_Dark", g),
    ]
    trim_w, trim_t = 0.07, 0.016
    ty = room_side_y + facing * trim_t / 2
    parts += [
        box(name + "_TrimL", (trim_w, trim_t, h + trim_w), (x0 - trim_w / 2 + 0.01, ty, (h + trim_w) / 2), "M_Wood_Dark", g, bev=0.004),
        box(name + "_TrimR", (trim_w, trim_t, h + trim_w), (x1 + trim_w / 2 - 0.01, ty, (h + trim_w) / 2), "M_Wood_Dark", g, bev=0.004),
        box(name + "_TrimT", (x1 - x0 + 2 * trim_w - 0.02, trim_t, trim_w), ((x0 + x1) / 2, ty, h + trim_w / 2), "M_Wood_Dark", g, bev=0.004),
    ]
    join(name + "_Mesh", parts)
    return g


def window(parent):
    x0, x1, z0, z1 = WINDOW
    g = group("Window_Front", parent)
    yc = ROOM_Y + WALL_T / 2
    fw = 0.06
    parts = [
        box("WinFrame_L", (fw, 0.13, z1 - z0), (x0 + fw / 2, yc, (z0 + z1) / 2), "M_Wood_Dark", g),
        box("WinFrame_R", (fw, 0.13, z1 - z0), (x1 - fw / 2, yc, (z0 + z1) / 2), "M_Wood_Dark", g),
        box("WinFrame_T", (x1 - x0, 0.13, fw), ((x0 + x1) / 2, yc, z1 - fw / 2), "M_Wood_Dark", g),
        box("WinFrame_B", (x1 - x0, 0.13, 0.05), ((x0 + x1) / 2, yc, z0 + 0.025), "M_Wood_Dark", g),
    ]
    trim_w, trim_t = 0.07, 0.016
    ty = ROOM_Y - trim_t / 2
    parts += [
        box("WinTrim_L", (trim_w, trim_t, z1 - z0 + trim_w), (x0 - trim_w / 2 + 0.01, ty, (z0 + z1 + trim_w) / 2), "M_Wood_Dark", g, bev=0.004),
        box("WinTrim_R", (trim_w, trim_t, z1 - z0 + trim_w), (x1 + trim_w / 2 - 0.01, ty, (z0 + z1 + trim_w) / 2), "M_Wood_Dark", g, bev=0.004),
        box("WinTrim_T", (x1 - x0 + 2 * trim_w - 0.02, trim_t, trim_w), ((x0 + x1) / 2, ty, z1 + trim_w / 2), "M_Wood_Dark", g, bev=0.004),
    ]
    join("Window_Frame", parts)
    box("Window_Sill", (x1 - x0 + 0.2, 0.17, 0.035), ((x0 + x1) / 2, ROOM_Y + 0.0, z0 - 0.0175 + 0.002), "M_Wood_Dark", g, bev=0.008)

    # Two casement sashes, the right one opened outward.
    ix0, ix1 = x0 + fw, x1 - fw
    iz0, iz1 = z0 + 0.05, z1 - fw
    sw = (ix1 - ix0) / 2
    sh = iz1 - iz0
    for side, hinge_x, sign, open_deg in (("L", ix0, 1, 0), ("R", ix1, -1, -32)):
        hinge = group("Sash_" + side, g, (hinge_x, ROOM_Y + 0.105, iz0), (0, 0, open_deg))
        st, dp = 0.05, 0.035
        cx = sign * sw / 2
        sp = [
            box("Sash%s_StileA" % side, (st, dp, sh), (sign * st / 2, 0, sh / 2), "M_Wood_Dark", hinge),
            box("Sash%s_StileB" % side, (st, dp, sh), (sign * (sw - st / 2), 0, sh / 2), "M_Wood_Dark", hinge),
            box("Sash%s_RailB" % side, (sw, dp, st), (cx, 0, st / 2), "M_Wood_Dark", hinge),
            box("Sash%s_RailT" % side, (sw, dp, st), (cx, 0, sh - st / 2), "M_Wood_Dark", hinge),
            box("Sash%s_MullV" % side, (0.022, dp * 0.8, sh), (cx, 0, sh / 2), "M_Wood_Dark", hinge),
            box("Sash%s_MullH1" % side, (sw, dp * 0.8, 0.022), (cx, 0, sh / 3), "M_Wood_Dark", hinge),
            box("Sash%s_MullH2" % side, (sw, dp * 0.8, 0.022), (cx, 0, sh * 2 / 3), "M_Wood_Dark", hinge),
        ]
        join("Sash%s_Frame" % side, sp)
        box("Sash%s_Glass" % side, (sw - 2 * st + 0.01, 0.004, sh - 2 * st + 0.01), (cx, 0, sh / 2), "M_Glass", hinge, bev=0)
        box("Sash%s_Handle" % side, (0.012, 0.04, 0.09), (sign * (sw - 0.04), -0.03, sh / 2), "M_Metal_Chrome", hinge, bev=0.004)

    # Iron window grille on the room side of the frame.
    tg = group("Teralis", g)
    ty = ROOM_Y + 0.025
    bars = []
    n = 12
    for i in range(n + 1):
        x = ix0 + (ix1 - ix0) * i / n
        bars.append(box("Ter_V%d" % i, (0.012, 0.012, sh), (x, ty, iz0 + sh / 2), "M_Metal_Black", tg, bev=0.002))
    for z in (iz0 + 0.01, iz0 + sh * 0.36, iz0 + sh * 0.64, iz1 - 0.01):
        bars.append(box("Ter_H%d" % int(z * 100), (ix1 - ix0, 0.014, 0.014), ((ix0 + ix1) / 2, ty, z), "M_Metal_Black", tg, bev=0.002))
    join("Teralis_Bars", bars)
    scrolls = []
    zc = iz0 + sh * 0.5
    for i in range(n):
        x = ix0 + (ix1 - ix0) * (i + 0.5) / n
        r = (ix1 - ix0) / n * 0.42
        pts = []
        for k in range(26):
            t = k / 25
            a = t * 3.6 * math.pi
            rr = r * (1 - 0.72 * t)
            pts.append((x + math.cos(a) * rr * (1 if i % 2 else -1), ty, zc + math.sin(a) * rr * 1.5))
        scrolls.append(cable("Ter_S%d" % i, pts, 0.004, "M_Metal_Black", tg))
    join("Teralis_Scrolls", scrolls)

    # Curtain rod and curtains.
    rod_z, rod_y = z1 + 0.18, ROOM_Y - 0.09
    rg = group("CurtainRod", g)
    cylinder("Rod", 0.012, x1 - x0 + 0.62, ((x0 + x1) / 2 - (x1 - x0 + 0.62) / 2, rod_y, rod_z), "M_Metal_Chrome", rg,
             rot=(0, 90, 0), verts=16, anchor="bottom")
    for sx in (x0 - 0.31, x1 + 0.31):
        sphere("RodEnd_%d" % int(sx * 100), 0.028, (sx, rod_y, rod_z), "M_Metal_Chrome", rg)
    for sx in (x0 - 0.2, x1 + 0.2):
        box("RodBracket_%d" % int(sx * 100), (0.02, 0.09, 0.02), (sx, rod_y + 0.045, rod_z), "M_Metal_Chrome", rg, bev=0.004)
    curtain("Curtain_L", g, x0 - 0.3, x0 + 0.48, rod_z - 0.03, 0.05, rod_y + 0.005, tie_x=x0 - 0.06)
    curtain("Curtain_R", g, x1 + 0.3, x1 - 0.48, rod_z - 0.03, 0.05, rod_y + 0.005, tie_x=x1 + 0.06)

    vent_blocks("Vent_Window", parent, VENT_WINDOW[0], VENT_WINDOW[1], VENT_WINDOW[2], VENT_WINDOW[3], yc)
    return g


def curtain(name, parent, x_outer, x_inner_top, ztop, zbot, y, tie_x=None, tie_z=1.15, folds=7, mat="M_Curtain"):
    """Curtain hanging from ztop to zbot. The outer edge stays put; when tied, the inner
    edge sweeps from its top corner to tie_x at tie_z and flares a little below."""
    import bmesh
    from dcm import mesh_object, solidify
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
        amp = 0.022 * squeeze + 0.008 * t
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
        cable(name + "_Tie", ring, 0.012, "M_Fabric_Mustard", parent)
    return ob


def front_door(parent):
    x0, x1, _, z1 = FRONT_DOOR
    g = group("FrontDoor", parent)
    yc = ROOM_Y + WALL_T / 2
    door_frame("FrontDoor_Frame", g, x0, x1, z1, yc, ROOM_Y, -1)
    hinge_x = x0 + 0.06
    leaf_w = (x1 - x0) - 0.12
    leaf_h = z1 - 0.06 - 0.01
    leaf = group("FrontDoor_Leaf", g, (hinge_x, ROOM_Y + 0.045, 0.01))
    box("Door_Slab", (leaf_w, 0.04, leaf_h), (leaf_w / 2, 0, leaf_h / 2), "M_Wood_Dark", leaf, bev=0.006)
    stile, mid, top_rail, bot_rail, rail = 0.1, 0.08, 0.12, 0.2, 0.1
    pw = (leaf_w - 2 * stile - mid) / 2
    ph = (leaf_h - top_rail - bot_rail - 2 * rail) / 3
    panels = []
    for c in range(2):
        px = stile + pw / 2 + c * (pw + mid)
        for r in range(3):
            pz = bot_rail + ph / 2 + r * (ph + rail)
            panels.append(box("Door_Panel_%d%d" % (c, r), (pw - 0.03, 0.014, ph - 0.03), (px, -0.026, pz), "M_Wood_Panel", leaf, bev=0.012, segs=1))
            panels.append(box("Door_PanelMold_%d%d" % (c, r), (pw + 0.01, 0.006, ph + 0.01), (px, -0.021, pz), "M_Wood_Dark", leaf, bev=0.003))
    join("Door_Panels", panels)
    lx = leaf_w - 0.07
    hw = []
    hw.append(box("Door_Backplate", (0.045, 0.008, 0.2), (lx, -0.024, 1.0), "M_Metal_Chrome", leaf, bev=0.004))
    hw.append(box("Door_Lever", (0.13, 0.018, 0.02), (lx - 0.055, -0.05, 1.04), "M_Metal_Chrome", leaf, bev=0.006))
    hw.append(cylinder("Door_LeverNeck", 0.01, 0.03, (lx, -0.028, 1.04), "M_Metal_Chrome", leaf, rot=(90, 0, 0), verts=12, anchor="center"))
    join("Door_Handle", hw)
    box("Door_Keyhole", (0.01, 0.004, 0.022), (lx, -0.029, 0.95), "M_Plastic_Black", leaf, bev=0)
    kg = group("Door_Key", leaf, (lx, -0.03, 0.95))
    box("Key_Blade", (0.006, 0.03, 0.012), (0, -0.015, 0), "M_Gold", kg, bev=0.002)
    ring = [(math.cos(a) * 0.016, -0.04, -0.02 + math.sin(a) * 0.016) for a in [k * 2 * math.pi / 15 for k in range(16)]]
    cable("Key_Ring", ring, 0.0025, "M_Metal_Chrome", kg)
    box("Key_Tag", (0.03, 0.006, 0.045), (0.004, -0.042, -0.062), "M_Plastic_Red", kg, rot=(0, 8, 0), bev=0.006)
    bolt = group("Door_Bolt", leaf, (lx - 0.02, -0.024, 1.82))
    box("Bolt_Plate", (0.14, 0.006, 0.035), (0, 0, 0), "M_Metal_Chrome", bolt, bev=0.003)
    cylinder("Bolt_Rod", 0.007, 0.16, (-0.05, -0.012, 0), "M_Metal_Chrome", bolt, rot=(0, 90, 0), verts=10, anchor="center")
    sphere("Bolt_Knob", 0.011, (-0.02, -0.025, 0.0), "M_Metal_Chrome", bolt)
    vent_blocks("Vent_Door", parent, VENT_DOOR[0], VENT_DOOR[1], VENT_DOOR[2], VENT_DOOR[3], yc)
    mat = plane("Doormat", 0.62, 0.4, ((x0 + x1) / 2, ROOM_Y - 0.32, 0.006), "M_Labels", g)
    uv_job(mat, "planar", axis="Z", rect=(0.5, 0.0, 1.0, 0.25))
    box("Doormat_Base", (0.62, 0.4, 0.01), ((x0 + x1) / 2, ROOM_Y - 0.32, 0.002), "M_Rubber_Black", g, bev=0.003)
    return g


def back_doorway(parent):
    x0, x1, _, z1 = BACK_DOOR
    g = group("BackDoorway", parent)
    yc = -ROOM_Y - WALL_T / 2
    door_frame("BackDoor_Frame", g, x0, x1, z1, yc, -ROOM_Y, 1)
    rod_z = z1 - 0.1
    cylinder("BackDoor_Rod", 0.009, x1 - x0 - 0.1, (x0 + 0.05, -ROOM_Y - 0.02, rod_z), "M_Metal_Chrome", g, rot=(0, 90, 0),
             verts=12)
    mid = (x0 + x1) / 2
    curtain("DoorCurtain_L", g, x0 + 0.05, mid + 0.03, rod_z - 0.01, 0.22, -ROOM_Y - 0.03, folds=5, mat="M_DoorCurtain")
    curtain("DoorCurtain_R", g, x1 - 0.05, mid - 0.03, rod_z - 0.01, 0.22, -ROOM_Y - 0.045, folds=5, mat="M_DoorCurtain")
    hall = group("Hallway", g)
    L = 1.6
    parts = [
        box("Hall_Floor", (1.6, L, 0.1), (mid, -ROOM_Y - WALL_T - L / 2, -0.05), "M_Hallway_Dark", hall, bev=0),
        box("Hall_Ceil", (1.6, L, 0.1), (mid, -ROOM_Y - WALL_T - L / 2, 2.45), "M_Hallway_Dark", hall, bev=0),
        box("Hall_WallW", (0.1, L, 2.5), (mid - 0.8, -ROOM_Y - WALL_T - L / 2, 1.2), "M_Hallway_Dark", hall, bev=0),
        box("Hall_WallE", (0.1, L, 2.5), (mid + 0.8, -ROOM_Y - WALL_T - L / 2, 1.2), "M_Hallway_Dark", hall, bev=0),
        box("Hall_WallEnd", (1.6, 0.1, 2.5), (mid, -ROOM_Y - WALL_T - L, 1.2), "M_Hallway_Dark", hall, bev=0),
    ]
    join("Hallway_Mesh", parts)
    return g


def trims(parent):
    g = group("Trims", parent)
    sk_h, sk_t = 0.09, 0.012
    runs = [
        ("SkirtFront_A", "x", ROOM_Y - sk_t / 2, -ROOM_X, FRONT_DOOR[0] - 0.07),
        ("SkirtFront_B", "x", ROOM_Y - sk_t / 2, FRONT_DOOR[1] + 0.07, ROOM_X),
        ("SkirtBack_A", "x", -ROOM_Y + sk_t / 2, -ROOM_X, BACK_DOOR[0] - 0.07),
        ("SkirtBack_B", "x", -ROOM_Y + sk_t / 2, BACK_DOOR[1] + 0.07, ROOM_X),
        ("SkirtEast", "y", ROOM_X - sk_t / 2, -ROOM_Y, ROOM_Y),
        ("SkirtWest", "y", -ROOM_X + sk_t / 2, -ROOM_Y, ROOM_Y),
    ]
    parts = []
    for name, axis, fixed, a, b in runs:
        if axis == "x":
            parts.append(box(name, (b - a, sk_t, sk_h), ((a + b) / 2, fixed, sk_h / 2), "M_Skirting", g, bev=0.003))
        else:
            parts.append(box(name, (sk_t, b - a, sk_h), (fixed, (a + b) / 2, sk_h / 2), "M_Skirting", g, bev=0.003))
    join("Skirting", parts)
    c = 0.05
    parts = [
        box("Cornice_F", (2 * ROOM_X, c, c), (0, ROOM_Y - c / 2, ROOM_H - c / 2), "M_Ceiling", g, bev=0.012, segs=3),
        box("Cornice_B", (2 * ROOM_X, c, c), (0, -ROOM_Y + c / 2, ROOM_H - c / 2), "M_Ceiling", g, bev=0.012, segs=3),
        box("Cornice_E", (c, 2 * ROOM_Y, c), (ROOM_X - c / 2, 0, ROOM_H - c / 2), "M_Ceiling", g, bev=0.012, segs=3),
        box("Cornice_W", (c, 2 * ROOM_Y, c), (-ROOM_X + c / 2, 0, ROOM_H - c / 2), "M_Ceiling", g, bev=0.012, segs=3),
    ]
    join("Cornice", parts)
    inset, bw = 0.55, 0.07
    ix, iy = ROOM_X - inset, ROOM_Y - inset
    z = ROOM_H - 0.012
    frame = [
        box("Molding_F", (2 * ix + bw, bw, 0.024), (0, iy, z), "M_Ceiling", g, bev=0.008, segs=2),
        box("Molding_B", (2 * ix + bw, bw, 0.024), (0, -iy, z), "M_Ceiling", g, bev=0.008, segs=2),
        box("Molding_E", (bw, 2 * iy - bw, 0.024), (ix, 0, z), "M_Ceiling", g, bev=0.008, segs=2),
        box("Molding_W", (bw, 2 * iy - bw, 0.024), (-ix, 0, z), "M_Ceiling", g, bev=0.008, segs=2),
    ]
    ix2, iy2, bw2 = ix - 0.14, iy - 0.14, 0.03
    frame += [
        box("Molding2_F", (2 * ix2 + bw2, bw2, 0.016), (0, iy2, ROOM_H - 0.008), "M_Ceiling", g, bev=0.005),
        box("Molding2_B", (2 * ix2 + bw2, bw2, 0.016), (0, -iy2, ROOM_H - 0.008), "M_Ceiling", g, bev=0.005),
        box("Molding2_E", (bw2, 2 * iy2 - bw2, 0.016), (ix2, 0, ROOM_H - 0.008), "M_Ceiling", g, bev=0.005),
        box("Molding2_W", (bw2, 2 * iy2 - bw2, 0.016), (-ix2, 0, ROOM_H - 0.008), "M_Ceiling", g, bev=0.005),
    ]
    join("Ceiling_Molding", frame)
    ros = [(0.0, 0.0), (0.38, 0.0), (0.37, -0.012), (0.33, -0.016), (0.3, -0.01), (0.27, -0.022), (0.23, -0.025),
           (0.2, -0.016), (0.0, -0.016)]
    lathe("Ceiling_Rosette", ros, (0.5, 0.0, ROOM_H), "M_Ceiling", g, segs=48, angle=35)
    return g


def wainscot(parent, height=0.95):
    """Two-tone wall: darker lower paint band with a wooden dado rail on top."""
    g = group("Wainscot", parent)
    t, rail_h, rail_d = 0.004, 0.04, 0.022
    walls = [
        ("Front", "x", ROOM_Y - t / 2, ROOM_Y - rail_d / 2, -ROOM_X, ROOM_X, [WINDOW, FRONT_DOOR]),
        ("Back", "x", -ROOM_Y + t / 2, -ROOM_Y + rail_d / 2, -ROOM_X, ROOM_X, [BACK_DOOR]),
        ("East", "y", ROOM_X - t / 2, ROOM_X - rail_d / 2, -ROOM_Y, ROOM_Y, []),
        ("West", "y", -ROOM_X + t / 2, -ROOM_X + rail_d / 2, -ROOM_Y, ROOM_Y, []),
    ]
    panels, rails = [], []
    for name, axis, fixed, rail_fixed, lo, hi, openings in walls:
        for i, (a, b, z0, z1) in enumerate(wall_rects(lo, hi, height, openings)):
            if axis == "x":
                panels.append(box("Wainscot_%s%d" % (name, i), (b - a, t, z1 - z0), ((a + b) / 2, fixed, (z0 + z1) / 2),
                                  "M_Wall_Lower", g, bev=0))
            else:
                panels.append(box("Wainscot_%s%d" % (name, i), (t, b - a, z1 - z0), (fixed, (a + b) / 2, (z0 + z1) / 2),
                                  "M_Wall_Lower", g, bev=0))
            if abs(z1 - height) < 1e-4:
                if axis == "x":
                    rails.append(box("Dado_%s%d" % (name, i), (b - a, rail_d, rail_h), ((a + b) / 2, rail_fixed, height),
                                     "M_Wood_Dark", g, bev=0.006))
                else:
                    rails.append(box("Dado_%s%d" % (name, i), (rail_d, b - a, rail_h), (rail_fixed, (a + b) / 2, height),
                                     "M_Wood_Dark", g, bev=0.006))
    join("Wainscot_Paint", panels)
    join("Dado_Rail", rails)
    return g


def switches(parent):
    g = group("Electrical", parent)
    sw = group("LightSwitch", g, (-1.2, -ROOM_Y + 0.006, 1.35))
    box("Switch_Plate", (0.08, 0.012, 0.12), (0, 0, 0), "M_Plastic_White", sw, bev=0.004)
    box("Switch_RockerA", (0.022, 0.01, 0.05), (-0.017, 0.008, 0.0), "M_Plastic_White", sw, rot=(8, 0, 0), bev=0.003)
    box("Switch_RockerB", (0.022, 0.01, 0.05), (0.017, 0.008, 0.0), "M_Plastic_White", sw, rot=(-8, 0, 0), bev=0.003)
    sockets = [("Socket_TV", (-ROOM_X + 0.006, -1.05, 0.32), (0, 0, -90)),
               ("Socket_Side", (ROOM_X - 0.006, -1.85, 0.32), (0, 0, 90)),
               ("Socket_Fan", (-1.95, ROOM_Y - 0.006, 0.32), (0, 0, 180))]
    for name, pos, rot in sockets:
        s = group(name, g, pos, rot)
        box(name + "_Plate", (0.08, 0.012, 0.08), (0, 0, 0), "M_Plastic_White", s, bev=0.004)
        cylinder(name + "_Well", 0.022, 0.006, (0, 0.007, 0), "M_Plastic_White", s, rot=(-90, 0, 0), verts=20, anchor="center")
        for dx in (-0.009, 0.009):
            cylinder(name + "_Hole%d" % int(dx * 1000), 0.003, 0.004, (dx, 0.01, 0), "M_Plastic_Black", s, rot=(-90, 0, 0), verts=8, anchor="center")
    return g


def build(root):
    g = group("Architecture", root)
    floor = box("Floor", (2 * ROOM_X + 2 * WALL_T, 2 * ROOM_Y + 2 * WALL_T, 0.1), (0, 0, -0.05), "M_Floor", g, bev=0)
    uv_job(floor, "box", scale=1 / 1.6)
    box("Ceiling", (2 * ROOM_X + 2 * WALL_T, 2 * ROOM_Y + 2 * WALL_T, 0.12), (0, 0, ROOM_H + 0.06), "M_Ceiling", g, bev=0)
    span = ROOM_X + WALL_T
    build_wall("Wall_Front", g, "x", ROOM_Y + WALL_T / 2, -span, span, [WINDOW, FRONT_DOOR, VENT_WINDOW, VENT_DOOR])
    build_wall("Wall_Back", g, "x", -ROOM_Y - WALL_T / 2, -span, span, [BACK_DOOR])
    build_wall("Wall_East", g, "y", ROOM_X + WALL_T / 2, -ROOM_Y, ROOM_Y, [])
    build_wall("Wall_West", g, "y", -ROOM_X - WALL_T / 2, -ROOM_Y, ROOM_Y, [])
    window(g)
    front_door(g)
    back_doorway(g)
    trims(g)
    wainscot(g)
    switches(g)
    return g
