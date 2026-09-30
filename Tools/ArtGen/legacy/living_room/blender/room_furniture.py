"""Furniture: kursi tamu set, coffee table, bufet with TV, side table, shoe rack,
standing fan, water dispenser, ceiling lamp."""
import math

from dcm import (box, cable, cushion, cylinder, extrude_shape, group, join, lathe, plane, sphere, uv_job)


def turned(name, h, r, parent, pos, mat="M_Wood_Teak", segs=16):
    """Turned wooden leg/spindle, bottom at pos."""
    prof = [(0, 0), (r * 0.72, 0), (r * 0.8, h * 0.05), (r * 0.66, h * 0.13), (r * 0.9, h * 0.28), (r * 1.12, h * 0.42),
            (r * 0.78, h * 0.56), (r * 0.72, h * 0.66), (r * 0.98, h * 0.8), (r * 1.0, h * 0.94), (r * 0.9, h), (0, h)]
    return lathe(name, prof, pos, mat, parent, segs=segs, angle=50)


def crest_shape(width, z0, z1, arch):
    hw = width / 2
    pts = [(-hw, z0), (hw, z0), (hw, z1 - 0.02)]
    n = 24
    for i in range(n + 1):
        x = hw - width * i / n
        t = x / hw
        z = z1 + arch * math.cos(t * math.pi / 2) ** 2 - 0.015 * abs(math.sin(t * 5 * math.pi))
        pts.append((x, z))
    pts.append((-hw, z1 - 0.02))
    return pts


def seat_unit(name, parent, pos, rot, width, seats, pillow=None):
    g = group(name, parent, pos, rot)
    D = 0.78
    hw, hd = width / 2, D / 2
    frame = []
    for sx in (-1, 1):
        x = sx * (hw - 0.045)
        frame.append(turned("%s_FrontPost%d" % (name, sx), 0.64, 0.032, g, (x, -hd + 0.05, 0)))
        frame.append(box("%s_BackPost%d" % (name, sx), (0.055, 0.055, 0.95), (x, hd - 0.05, 0.475), "M_Wood_Teak", g, bev=0.008))
        frame.append(sphere("%s_Finial%d" % (name, sx), 0.03, (x, hd - 0.05, 0.965), "M_Wood_Teak", g, seg=16, rings=8))
        frame.append(box("%s_Arm%d" % (name, sx), (0.1, D - 0.02, 0.035), (x, -0.012, 0.655), "M_Wood_Teak", g, bev=0.012, segs=3))
        for k, yy in enumerate((-0.17, 0.02, 0.2)):
            frame.append(turned("%s_Spindle%d_%d" % (name, sx, k), 0.32, 0.013, g, (x, yy, 0.32), segs=10))
        frame.append(box("%s_SideRail%d" % (name, sx), (0.05, D - 0.1, 0.1), (x, 0, 0.27), "M_Wood_Teak", g, bev=0.006))
    frame.append(box(name + "_FrontRail", (width - 0.06, 0.05, 0.1), (0, -hd + 0.05, 0.27), "M_Wood_Teak", g, bev=0.006))
    frame.append(box(name + "_BackRail", (width - 0.06, 0.05, 0.1), (0, hd - 0.05, 0.27), "M_Wood_Teak", g, bev=0.006))
    frame.append(box(name + "_SeatBoard", (width - 0.1, D - 0.12, 0.02), (0, 0, 0.315), "M_Wood_Teak", g, bev=0.003))
    frame.append(box(name + "_LowerBack", (width - 0.1, 0.035, 0.055), (0, hd - 0.05, 0.37), "M_Wood_Teak", g, bev=0.006))
    crest = extrude_shape(name + "_Crest", crest_shape(width - 0.08, 0.78, 0.87, 0.07), 0.035,
                          (0, hd - 0.05 + 0.0175, 0), "M_Wood_Teak", g, rot=(90, 0, 0), bev=0.006)
    frame.append(crest)
    slats = seats * 2 + 1
    for i in range(slats):
        x = -(width - 0.2) / 2 + (width - 0.2) * i / (slats - 1)
        frame.append(box("%s_Slat%d" % (name, i), (0.045, 0.02, 0.42), (x, hd - 0.05, 0.6), "M_Wood_Teak", g, bev=0.004))
    join(name + "_Frame", frame)

    cw = (width - 0.14) / seats
    for i in range(seats):
        x = -(width - 0.14) / 2 + cw * (i + 0.5)
        c = cushion("%s_SeatCushion%d" % (name, i), (cw - 0.012, D - 0.17, 0.11), 0.35, (x, -0.025, 0.385), "M_Batik", g)
        uv_job(c, "box", scale=2.6)
        b = cushion("%s_BackCushion%d" % (name, i), (cw - 0.02, 0.4, 0.11), 0.4, (x, hd - 0.15, 0.64), "M_Batik", g,
                    rot=(78, 0, 0))
        uv_job(b, "box", scale=2.6)
    if pillow:
        px, mat = pillow
        p = cushion(name + "_Pillow", (0.38, 0.38, 0.12), 0.55, (px, 0.1, 0.58), mat, g, rot=(70, 0, 18 * (1 if px < 0 else -1)))
    return g


def coffee_table(parent, pos, rot):
    g = group("CoffeeTable", parent, pos, rot)
    L, Dp, H = 1.1, 0.6, 0.42
    parts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(box("CT_Leg%d%d" % (sx, sy), (0.05, 0.05, H - 0.03), (sx * (L / 2 - 0.05), sy * (Dp / 2 - 0.05), (H - 0.03) / 2),
                             "M_Wood_Teak", g, bev=0.008))
            parts.append(cylinder("CT_Foot%d%d" % (sx, sy), 0.028, 0.015, (sx * (L / 2 - 0.05), sy * (Dp / 2 - 0.05), 0), "M_Plastic_Black", g,
                                  verts=12))
    for sy in (-1, 1):
        parts.append(box("CT_ApronX%d" % sy, (L - 0.1, 0.025, 0.07), (0, sy * (Dp / 2 - 0.05), H - 0.065), "M_Wood_Teak", g, bev=0.004))
    for sx in (-1, 1):
        parts.append(box("CT_ApronY%d" % sx, (0.025, Dp - 0.1, 0.07), (sx * (L / 2 - 0.05), 0, H - 0.065), "M_Wood_Teak", g, bev=0.004))
    parts.append(box("CT_Top", (L, Dp, 0.03), (0, 0, H - 0.015), "M_Wood_Teak", g, bev=0.01, segs=3))
    parts.append(box("CT_Shelf", (L - 0.12, Dp - 0.12, 0.018), (0, 0, 0.1), "M_Wood_Teak", g, bev=0.004))
    join("CoffeeTable_Frame", parts)
    box("CT_Glass", (L - 0.03, Dp - 0.03, 0.006), (0, 0, H + 0.003), "M_Glass_Table", g, bev=0)
    snap = plane("CT_UnderGlassPhoto", 0.1, 0.1, (0.36, 0.17, H + 0.0006), "M_Notes", g, rot=(0, 0, 12))
    uv_job(snap, "planar", axis="Z", rect=(0.75, 0.0, 1.0, 0.25))
    card = plane("CT_UnderGlassCard", 0.09, 0.09, (0.47, -0.2, H + 0.0006), "M_Notes", g, rot=(0, 0, -8))
    uv_job(card, "planar", axis="Z", rect=(0.75, 0.25, 1.0, 0.5))
    mags = [("M_Magazine_A", 0.0, 3), ("M_Magazine_B", 0.012, -6), ("M_Magazine_C", 0.024, 9)]
    for i, (mat, z, r) in enumerate(mags):
        box("CT_Magazine%d" % i, (0.28, 0.21, 0.01), (-0.25 + i * 0.015, 0.02, 0.114 + z), mat, g, rot=(0, 0, r), bev=0.002)
    news = box("CT_OldPaper", (0.3, 0.22, 0.012), (0.2, -0.01, 0.115), "M_Newspaper", g, rot=(0, 0, -4), bev=0.002)
    uv_job(news, "planar", axis="Z", rect=(0.0, 0.5, 1.0, 1.0))
    return g


def drawer(name, parent, pos, w, h, d, handle_mat="M_Gold"):
    """Drawer with front, box and handle. Origin at the closed front face centre."""
    g = group(name, parent, pos)
    box(name + "_Front", (w, 0.022, h), (0, 0.011, 0), "M_Wood_Light", g, bev=0.006)
    inner = []
    inner.append(box(name + "_Bottom", (w - 0.04, d - 0.03, 0.01), (0, 0.022 + (d - 0.03) / 2, -h / 2 + 0.02), "M_Wood_Light", g, bev=0))
    inner.append(box(name + "_SideL", (0.012, d - 0.03, h - 0.04), (-w / 2 + 0.026, 0.022 + (d - 0.03) / 2, 0), "M_Wood_Light", g, bev=0))
    inner.append(box(name + "_SideR", (0.012, d - 0.03, h - 0.04), (w / 2 - 0.026, 0.022 + (d - 0.03) / 2, 0), "M_Wood_Light", g, bev=0))
    inner.append(box(name + "_Back", (w - 0.04, 0.012, h - 0.04), (0, 0.022 + d - 0.03, 0), "M_Wood_Light", g, bev=0))
    join(name + "_Box", inner)
    hg = []
    hg.append(box(name + "_HandleBar", (0.12, 0.012, 0.014), (0, -0.03, 0), handle_mat, g, bev=0.005))
    for sx in (-1, 1):
        hg.append(cylinder(name + "_HandlePost%d" % sx, 0.006, 0.03, (sx * 0.05, -0.001, 0), handle_mat, g, rot=(90, 0, 0), verts=10))
    join(name + "_Handle", hg)
    return g


def bufet(parent, pos, rot):
    g = group("Bufet", parent, pos, rot)
    W, D, H = 1.8, 0.45, 0.78
    hd = D / 2
    body = []
    body.append(box("Bufet_Plinth", (W - 0.06, D - 0.06, 0.08), (0, 0.01, 0.04), "M_Wood_Dark", g, bev=0.004))
    body.append(box("Bufet_Top", (W + 0.04, D + 0.03, 0.035), (0, 0, H - 0.0175), "M_Wood_Teak", g, bev=0.01, segs=3))
    body.append(box("Bufet_Bottom", (W, D, 0.025), (0, 0, 0.0925), "M_Wood_Teak", g, bev=0.004))
    inner_h = H - 0.035 - 0.105
    zc = 0.105 + inner_h / 2
    for side, cx in (("L", -0.65), ("R", 0.65)):
        for sx in (-1, 1):
            body.append(box("Bufet_Side%s%d" % (side, sx), (0.02, D, inner_h), (cx + sx * 0.24, 0, zc), "M_Wood_Teak", g, bev=0.003))
        body.append(box("Bufet_Back" + side, (0.46, 0.015, inner_h), (cx, hd - 0.0075, zc), "M_Wood_Dark", g, bev=0))
        body.append(box("Bufet_Divider" + side, (0.46, D - 0.02, 0.02), (cx, 0.01, 0.555), "M_Wood_Teak", g, bev=0.002))
        body.append(box("Bufet_RailTop" + side, (0.46, 0.02, 0.025), (cx, -hd + 0.01, 0.7325), "M_Wood_Teak", g, bev=0.002))
        body.append(box("Bufet_RailBot" + side, (0.46, 0.02, 0.025), (cx, -hd + 0.01, 0.1175), "M_Wood_Teak", g, bev=0.002))
    for sx in (-1, 1):
        body.append(box("Bufet_End%d" % sx, (0.02, D, inner_h), (sx * (W / 2 - 0.01), 0, zc), "M_Wood_Teak", g, bev=0.003))
    body.append(box("Bufet_BackMid", (0.8, 0.02, H - 0.14), (0, hd - 0.01, 0.105 + (H - 0.14) / 2), "M_Wood_Dark", g, bev=0))
    body.append(box("Bufet_ShelfMid", (0.8, D - 0.04, 0.02), (0, 0.01, 0.43), "M_Wood_Teak", g, bev=0.003))
    join("Bufet_Body", body)
    for side, x in (("L", -0.65), ("R", 0.65)):
        box("Bufet_Door" + side, (0.46, 0.022, 0.42), (x, -hd - 0.011, 0.34), "M_Wood_Light", g, bev=0.006)
        box("Bufet_DoorPanel" + side, (0.36, 0.008, 0.32), (x, -hd - 0.024, 0.34), "M_Wood_Teak", g, bev=0.008, segs=1)
        sphere("Bufet_Knob" + side, 0.014, (x + (0.18 if side == "L" else -0.18), -hd - 0.035, 0.44), "M_Gold", g, seg=12, rings=8)
    drawer("Bufet_DrawerL", g, (-0.65, -hd - 0.022, 0.64), 0.46, 0.16, D - 0.04)
    dr = drawer("INT_Drawer", g, (0.65, -hd - 0.022, 0.64), 0.46, 0.16, D - 0.04)
    for gx, off in ((-1, -0.1), (1, 0.12)):
        box("Bufet_GlassDoor%d" % gx, (0.41, 0.005, 0.6), (gx * 0.19, -hd + 0.01 + (0.008 if gx > 0 else 0), 0.43), "M_Glass", g, bev=0)
        box("Bufet_GlassRail%d" % gx, (0.41, 0.01, 0.015), (gx * 0.19, -hd + 0.012, 0.12), "M_Wood_Dark", g, bev=0.002)
        cylinder("Bufet_GlassPull%d" % gx, 0.012, 0.004, (gx * 0.19 - gx * 0.17, -hd + 0.006, 0.43), "M_Metal_Chrome", g, rot=(90, 0, 0), verts=12)
    box("Bufet_GlassTrack", (0.8, 0.03, 0.012), (0, -hd + 0.015, 0.735), "M_Wood_Dark", g, bev=0.002)
    return g, dr


def tv(parent, pos, rot):
    g = group("TV", parent, pos, rot)
    box("TV_Body", (0.76, 0.05, 0.46), (0, 0, 0.12 + 0.23), "M_Plastic_Black", g, bev=0.008)
    box("TV_Back", (0.5, 0.05, 0.3), (0, 0.04, 0.12 + 0.22), "M_Plastic_Black", g, bev=0.03, segs=3)
    scr = plane("TV_Screen", 0.72, 0.41, (0, -0.026, 0.12 + 0.235), "M_TV_Screen", g, rot=(90, 0, 0))
    box("TV_Neck", (0.06, 0.03, 0.12), (0, 0.01, 0.06), "M_Plastic_Black", g, bev=0.006)
    box("TV_Foot", (0.34, 0.2, 0.014), (0, 0.0, 0.007), "M_Plastic_Black", g, bev=0.006)
    box("TV_Logo", (0.05, 0.004, 0.008), (0, -0.026, 0.132), "M_Metal_Chrome", g, bev=0)
    sphere("TV_LED", 0.004, (0.33, -0.026, 0.13), "M_LED_Red", g, seg=8, rings=4)
    return g


def side_table(parent, pos, rot):
    g = group("SideTable", parent, pos, rot)
    S, H = 0.45, 0.56
    parts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(turned("ST_Leg%d%d" % (sx, sy), H - 0.03, 0.022, g, (sx * (S / 2 - 0.04), sy * (S / 2 - 0.04), 0)))
    parts.append(box("ST_Top", (S, S, 0.03), (0, 0, H - 0.015), "M_Wood_Teak", g, bev=0.01, segs=3))
    parts.append(box("ST_Shelf", (S - 0.08, S - 0.08, 0.018), (0, 0, 0.14), "M_Wood_Teak", g, bev=0.004))
    for sy in (-1, 1):
        parts.append(box("ST_Apron%d" % sy, (S - 0.1, 0.02, 0.06), (0, sy * (S / 2 - 0.04), H - 0.06), "M_Wood_Teak", g, bev=0.003))
        parts.append(box("ST_ApronB%d" % sy, (0.02, S - 0.1, 0.06), (sy * (S / 2 - 0.04), 0, H - 0.06), "M_Wood_Teak", g, bev=0.003))
    join("SideTable_Frame", parts)
    return g


def table_lamp(parent, pos):
    g = group("TableLamp", parent, pos)
    lathe("Lamp_Base", [(0, 0), (0.07, 0), (0.075, 0.015), (0.06, 0.03), (0.085, 0.12), (0.075, 0.2), (0.03, 0.25),
                        (0.02, 0.28), (0.012, 0.29), (0.012, 0.36), (0, 0.36)], (0, 0, 0), "M_Ceramic_Blue", g, segs=24)
    shade = []
    n = 18
    r0, r1, h0, h1 = 0.16, 0.1, 0.3, 0.52
    for i in range(n):
        a = 2 * math.pi * i / n
        rr0 = r0 * (1.0 if i % 2 == 0 else 0.94)
        rr1 = r1 * (1.0 if i % 2 == 0 else 0.93)
        shade.append((a, rr0, rr1))
    import bmesh
    from dcm import mesh_object, smooth, solidify
    bm = bmesh.new()
    bot = [bm.verts.new((math.cos(a) * rr0, math.sin(a) * rr0, h0)) for a, rr0, _ in shade]
    top = [bm.verts.new((math.cos(a) * rr1, math.sin(a) * rr1, h1)) for a, _, rr1 in shade]
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot[i], bot[j], top[j], top[i]))
    ob = mesh_object("Lamp_Shade", bm, "M_Lampshade", g, (0, 0, 0), (0, 0, 0))
    solidify(ob, 0.004)
    smooth(ob, 30)
    sphere("Lamp_Bulb", 0.035, (0, 0, 0.39), "M_LampDiffuser", g, seg=16, rings=8)
    lathe("Lamp_Finial", [(0, 0), (0.012, 0), (0.016, 0.012), (0.008, 0.03), (0, 0.035)], (0, 0, h1), "M_Gold", g, segs=12)
    return g


def shoe_rack(parent, pos, rot):
    g = group("ShoeRack", parent, pos, rot)
    W, D, H = 0.56, 0.28, 0.4
    parts = []
    for sx in (-1, 1):
        parts.append(box("SR_Side%d" % sx, (0.02, D, H), (sx * (W / 2 - 0.01), 0, H / 2), "M_Wood_Light", g, bev=0.004))
    for z in (0.06, 0.22, 0.39):
        for k in range(4):
            parts.append(box("SR_Slat%d_%d" % (int(z * 100), k), (W - 0.04, 0.05, 0.014), (0, -D / 2 + 0.035 + k * 0.07, z), "M_Wood_Light", g, bev=0.003))
    join("ShoeRack_Frame", parts)
    return g


def standing_fan(parent, pos, rot):
    g = group("StandingFan", parent, pos, rot)
    lathe("Fan_Base", [(0, 0), (0.17, 0), (0.175, 0.012), (0.16, 0.03), (0.05, 0.05), (0.03, 0.06), (0, 0.06)], (0, 0, 0),
          "M_Plastic_White", g, segs=32)
    cylinder("Fan_Pole", 0.018, 0.95, (0, 0, 0.05), "M_Metal_Chrome", g, verts=12)
    cylinder("Fan_Collar", 0.028, 0.06, (0, 0, 0.62), "M_Plastic_White", g, verts=16)
    head = group("Fan_Head", g, (0, 0, 1.03), (-8, 0, 0))
    lathe("Fan_Motor", [(0, -0.16), (0.05, -0.16), (0.075, -0.12), (0.08, -0.05), (0.07, 0.0), (0.03, 0.02), (0, 0.02)],
          (0, 0, 0), "M_Plastic_White", head, rot=(90, 0, 0), segs=24)
    badge = box("Fan_Badge", (0.08, 0.004, 0.03), (0, 0.162, 0.0), "M_Labels", head, rot=(0, 0, 0), bev=0)
    uv_job(badge, "planar", axis="Y", rect=(0.0, 0.0, 0.5, 0.25), flip_u=True)
    cage = []
    R = 0.2
    for k, (r, y) in enumerate(((R, -0.02), (R * 0.75, -0.06), (R * 0.45, -0.08), (R, 0.06), (R * 0.72, 0.09))):
        ring = [(math.cos(a) * r, y, math.sin(a) * r) for a in [i * 2 * math.pi / 40 for i in range(41)]]
        cage.append(cable("Fan_Ring%d" % k, ring, 0.003, "M_Metal_Chrome", head))
    for i in range(24):
        a = i * 2 * math.pi / 24
        pts = [(math.cos(a) * 0.03, -0.095, math.sin(a) * 0.03), (math.cos(a) * R * 0.6, -0.08, math.sin(a) * R * 0.6),
               (math.cos(a) * R, -0.02, math.sin(a) * R), (math.cos(a) * R * 0.85, 0.08, math.sin(a) * R * 0.85)]
        cage.append(cable("Fan_Spoke%d" % i, pts, 0.0022, "M_Metal_Chrome", head))
    join("Fan_Cage", cage)
    cylinder("Fan_CageRim", R + 0.006, 0.012, (0, -0.02, 0), "M_Plastic_White", head, rot=(90, 0, 0), verts=40, anchor="center")
    lathe("Fan_Hub", [(0, 0), (0.035, 0), (0.03, 0.02), (0, 0.03)], (0, -0.03, 0), "M_Fan_Blade", head, rot=(90, 0, 0), segs=16)
    for i in range(3):
        a = i * 120 + 20
        blade = extrude_shape("Fan_Blade%d" % i, [(0.02, -0.03), (0.17, -0.07), (0.185, 0.0), (0.16, 0.06), (0.02, 0.03)], 0.004,
                              (0, -0.028, 0), "M_Fan_Blade", head, rot=(90, a, 0), bev=0.0015)
    sphere("Fan_Knob", 0.022, (0, -0.1, 0), "M_Plastic_White", head, seg=12, rings=6)
    box("Fan_Switch", (0.06, 0.03, 0.02), (0.06, -0.02, 0.62 + 0.03), "M_Plastic_Blue", g, bev=0.006)
    return g


def dispenser(parent, pos, rot):
    g = group("Dispenser", parent, pos, rot)
    box("Disp_Body", (0.31, 0.31, 0.92), (0, 0, 0.46), "M_Plastic_White", g, bev=0.02, segs=3)
    box("Disp_Recess", (0.24, 0.05, 0.3), (0, -0.14, 0.62), "M_Plastic_Black", g, bev=0.01)
    box("Disp_Tray", (0.22, 0.09, 0.03), (0, -0.19, 0.46), "M_Plastic_Black", g, bev=0.008)
    for sx, mat in ((-1, "M_Plastic_Red"), (1, "M_Plastic_Blue")):
        tg = group("Disp_Tap%d" % sx, g, (sx * 0.06, -0.17, 0.72))
        box("Tap_Body%d" % sx, (0.04, 0.05, 0.05), (0, 0, 0), mat, tg, bev=0.01)
        cylinder("Tap_Spout%d" % sx, 0.008, 0.035, (0, -0.005, -0.055), "M_Plastic_White", tg, verts=10)
    box("Disp_Panel", (0.2, 0.004, 0.05), (0, -0.156, 0.85), "M_Plastic_Blue", g, bev=0.002)
    lathe("Disp_Collar", [(0, 0), (0.11, 0), (0.12, 0.02), (0.06, 0.04), (0, 0.04)], (0, 0, 0.92), "M_Plastic_White", g, segs=24)
    gal = group("Galon", g, (0, 0, 0.935))
    prof = [(0, 0.0), (0.02, 0.0), (0.03, 0.03), (0.03, 0.08), (0.12, 0.14), (0.135, 0.2), (0.135, 0.45),
            (0.13, 0.49), (0.1, 0.5), (0, 0.5)]
    lathe("Galon_Bottle", prof, (0, 0, 0), "M_Glass_Bottle", gal, segs=32)
    for k, z in enumerate((0.22, 0.4)):
        lathe("Galon_Rib%d" % k, [(0, z - 0.005), (0.1385, z - 0.005), (0.1385, z + 0.005), (0, z + 0.005)], (0, 0, 0),
              "M_Glass_Bottle", gal, segs=32)
    band = lathe("Galon_Label", [(0.1372, 0.25), (0.1384, 0.25), (0.1384, 0.37), (0.1372, 0.37)], (0, 0, 0), "M_Labels", gal, segs=32)
    uv_job(band, "cyl", rect=(0.5, 0.75, 1.0, 1.0))
    lathe("Galon_Cap", [(0, 0.0), (0.034, 0.0), (0.034, 0.025), (0, 0.025)], (0, 0, -0.01), "M_Plastic_Blue", gal, segs=16)
    lathe("Galon_Water", [(0, 0.03), (0.024, 0.03), (0.024, 0.08), (0.113, 0.14), (0.127, 0.2), (0.127, 0.33), (0, 0.33)],
          (0, 0, 0), "M_Glass_Bottle", gal, segs=24)
    return g


def ceiling_lamp(parent, pos):
    g = group("CeilingLamp", parent, pos)
    lathe("CLamp_Base", [(0, 0), (0.2, 0), (0.2, -0.02), (0, -0.02)], (0, 0, 0), "M_Plastic_White", g, segs=40)
    lathe("CLamp_Diffuser", [(0, -0.1), (0.08, -0.098), (0.15, -0.085), (0.19, -0.05), (0.195, -0.02), (0, -0.02)], (0, 0, 0),
          "M_LampDiffuser", g, segs=40)
    lathe("CLamp_Ring", [(0.19, -0.03), (0.2, -0.03), (0.2, -0.018), (0.19, -0.018)], (0, 0, 0), "M_Gold", g, segs=40)
    return g
