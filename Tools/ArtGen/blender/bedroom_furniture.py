"""Bedroom furniture: bed, wardrobe, bookshelf, desk with drawers, office chair,
corner shelf, TV cabinet with a CRT TV, dehumidifier, floor mirror and a backpack
on a hook rail. Every piece faces local -Y with its origin on the floor."""
import math

from dcm import box, cable, cushion, cylinder, extrude_shape, group, join, lathe, plane, sphere, uv_job
from props import drape, drawer

BED_TOP = 0.52


# ---------------------------------------------------------------- bed

def teddy(parent, pos, rot):
    """Sitting teddy bear."""
    g = group("TeddyBear", parent, pos, rot)
    sphere("Teddy_Body", 0.1, (0, 0, 0.1), "M_Plush", g, seg=20, rings=10, scale=(1.0, 0.85, 1.15))
    sphere("Teddy_Belly", 0.062, (0, -0.06, 0.09), "M_Plush_Light", g, seg=16, rings=8, scale=(1.0, 0.5, 1.1))
    head = group("Teddy_HeadGroup", g, (0, -0.01, 0.26), (8, 0, 6))
    sphere("Teddy_Head", 0.085, (0, 0, 0), "M_Plush", head, seg=20, rings=10, scale=(1.05, 0.95, 0.95))
    sphere("Teddy_Snout", 0.035, (0, -0.075, -0.018), "M_Plush_Light", head, seg=14, rings=7, scale=(1.1, 0.8, 0.8))
    sphere("Teddy_Nose", 0.012, (0, -0.105, -0.008), "M_Plastic_Black", head, seg=10, rings=5, scale=(1.3, 0.8, 0.9))
    for s in (-1, 1):
        sphere("Teddy_Ear%d" % s, 0.03, (s * 0.062, 0.0, 0.065), "M_Plush", head, seg=12, rings=6, scale=(1.0, 0.6, 1.0))
        sphere("Teddy_Eye%d" % s, 0.009, (s * 0.032, -0.074, 0.02), "M_Plastic_Black", head, seg=8, rings=4)
        sphere("Teddy_Arm%d" % s, 0.035, (s * 0.1, -0.03, 0.14), "M_Plush", g, rot=(0, s * 25, 0), seg=12, rings=6, scale=(0.9, 0.9, 1.7))
        sphere("Teddy_Leg%d" % s, 0.042, (s * 0.06, -0.09, 0.035), "M_Plush", g, seg=12, rings=6, scale=(0.95, 1.6, 0.85))
        sphere("Teddy_Pad%d" % s, 0.026, (s * 0.06, -0.155, 0.04), "M_Plush_Light", g, rot=(90, 0, 0), seg=10, rings=5,
               scale=(1.0, 1.0, 0.3))
    cable("Teddy_Ribbon", [(math.cos(a) * 0.066, -0.01 + math.sin(a) * 0.06, 0.2) for a in [k * 2 * math.pi / 20 for k in range(21)]],
          0.007, "M_Plastic_Red", g)
    return g


def bed(parent, pos, rot):
    """Single bed, pine frame with a slatted headboard; the comforter is thrown back."""
    g = group("Bed", parent, pos, rot)
    W, L = 1.06, 2.1
    hw, hl = W / 2, L / 2
    frame = []
    for sx in (-1, 1):
        frame.append(box("Bed_PostHead%d" % sx, (0.06, 0.06, 0.95), (sx * (hw - 0.03), hl - 0.03, 0.475), "M_Wood_Pine", g, bev=0.008))
        frame.append(box("Bed_PostFoot%d" % sx, (0.06, 0.06, 0.5), (sx * (hw - 0.03), -hl + 0.03, 0.25), "M_Wood_Pine", g, bev=0.008))
        frame.append(box("Bed_Rail%d" % sx, (0.03, L - 0.12, 0.16), (sx * (hw - 0.03), 0, 0.3), "M_Wood_Pine", g, bev=0.004))
    frame.append(box("Bed_HeadTop", (W - 0.06, 0.05, 0.07), (0, hl - 0.03, 0.88), "M_Wood_Pine", g, bev=0.008))
    for k in range(5):
        frame.append(box("Bed_HeadSlat%d" % k, (W - 0.12, 0.022, 0.05), (0, hl - 0.03, 0.42 + k * 0.085), "M_Wood_Pine", g, bev=0.004))
    frame.append(box("Bed_FootBoard", (W - 0.12, 0.03, 0.16), (0, -hl + 0.03, 0.3), "M_Wood_Pine", g, bev=0.004))
    frame.append(box("Bed_FootTop", (W - 0.06, 0.05, 0.04), (0, -hl + 0.03, 0.4), "M_Wood_Pine", g, bev=0.006))
    frame.append(box("Bed_Base", (W - 0.12, L - 0.12, 0.02), (0, 0, 0.3), "M_Wood_Dark", g, bev=0))
    join("Bed_Frame", frame)

    mattress = cushion("Bed_Mattress", (W - 0.08, L - 0.12, BED_TOP - 0.32), 0.05, (0, 0, (BED_TOP + 0.32) / 2 - 0.004), "M_Sheet", g,
                       cuts=6)
    uv_job(mattress, "box", scale=3.0)
    pillow = cushion("Bed_Pillow", (0.62, 0.38, 0.13), 0.55, (0.05, hl - 0.3, BED_TOP + 0.05), "M_Pillow", g, rot=(0, 0, -4))
    uv_job(pillow, "box", scale=3.0)
    back = cushion("Bed_PillowUp", (0.5, 0.34, 0.12), 0.5, (-0.2, hl - 0.13, BED_TOP + 0.2), "M_Pillow", g, rot=(-68, 0, 10))
    uv_job(back, "box", scale=3.0)
    teddy(g, (0.24, hl - 0.26, BED_TOP + 0.09), (-6, 0, 22))

    # Comforter: from the foot edge to half a metre short of the pillows, hanging over the sides.
    foot = -hl + 0.06
    head_edge = 0.46
    d = head_edge - foot
    blanket = drape("Bed_Comforter", g, W + 0.56, d, (0, (foot + head_edge) / 2, BED_TOP - 0.004), "M_Bedding", W - 0.08, d,
                    radius=0.05, puff=0.035, thickness=0.035, seed=3, hang=(True, True, False, False))
    uv_job(blanket, "box", scale=2.2)
    roll = cushion("Bed_ComforterFold", (W + 0.02, 0.2, 0.07), 0.45, (0.0, head_edge - 0.05, BED_TOP + 0.05), "M_Bedding", g,
                   rot=(8, 0, 1.5))
    uv_job(roll, "box", scale=2.2)
    return g


# ---------------------------------------------------------------- wardrobe, bookshelf, desk

def wardrobe(parent, pos, rot):
    """Two-door wardrobe with boxes and a suitcase on top."""
    g = group("Wardrobe", parent, pos, rot)
    W, D, H = 1.0, 0.58, 1.85
    body = [
        box("Wardrobe_SideL", (0.022, D, H - 0.06), (-W / 2 + 0.011, 0, 0.06 + (H - 0.06) / 2), "M_Paint_Ivory", g, bev=0.003),
        box("Wardrobe_SideR", (0.022, D, H - 0.06), (W / 2 - 0.011, 0, 0.06 + (H - 0.06) / 2), "M_Paint_Ivory", g, bev=0.003),
        box("Wardrobe_Top", (W + 0.02, D + 0.02, 0.03), (0, 0, H - 0.015), "M_Paint_Ivory", g, bev=0.005),
        box("Wardrobe_Back", (W - 0.04, 0.01, H - 0.08), (0, D / 2 - 0.005, 0.06 + (H - 0.08) / 2), "M_Paint_Ivory", g, bev=0),
        box("Wardrobe_Plinth", (W - 0.02, D - 0.04, 0.06), (0, 0.01, 0.03), "M_Wood_Cherry", g, bev=0.003),
        box("Wardrobe_Cornice", (W + 0.03, 0.03, 0.05), (0, -D / 2 - 0.004, H - 0.04), "M_Wood_Cherry", g, bev=0.006),
    ]
    join("Wardrobe_Body", body)
    dh = H - 0.06 - 0.07
    for s, name in ((-1, "L"), (1, "R")):
        x = s * (W / 4 - 0.002)
        box("Wardrobe_Door" + name, (W / 2 - 0.012, 0.022, dh), (x, -D / 2 - 0.011, 0.065 + dh / 2), "M_Wardrobe_Door", g, bev=0.005)
        box("Wardrobe_DoorPanel" + name, (W / 2 - 0.1, 0.006, dh - 0.14), (x, -D / 2 - 0.024, 0.065 + dh / 2), "M_Paint_Ivory", g,
            bev=0.01, segs=1)
        sphere("Wardrobe_Knob" + name, 0.016, (-s * 0.03, -D / 2 - 0.035, 1.0), "M_Gold", g, seg=12, rings=8)
    box("Wardrobe_Lock", (0.012, 0.004, 0.03), (0.035, -D / 2 - 0.023, 1.08), "M_Gold", g, bev=0.002)
    # On top: spare bedding under the air conditioner (left), a suitcase and a box (right).
    top = H
    quilt = cushion("Wardrobe_Quilt", (0.46, 0.42, 0.13), 0.3, (-0.24, 0.0, top + 0.065), "M_Fabric_Cream", g, rot=(0, 0, 4), cuts=4)
    uv_job(quilt, "box", scale=3.0)
    sg = group("Suitcase", g, (0.2, 0.02, top), (0, 0, -5))
    box("Suitcase_Shell", (0.52, 0.36, 0.18), (0, 0, 0.09), "M_Plastic_Blue", sg, bev=0.03, segs=3)
    box("Suitcase_Seam", (0.525, 0.365, 0.012), (0, 0, 0.09), "M_Plastic_Black", sg, bev=0.004)
    cable("Suitcase_Handle", [(-0.06, -0.182, 0.12), (-0.05, -0.2, 0.14), (0.05, -0.2, 0.14), (0.06, -0.182, 0.12)], 0.008,
          "M_Plastic_Black", sg)
    bx = group("StorageBox", g, (0.24, 0.0, top + 0.18), (0, 0, 9))
    box("Box_Body", (0.3, 0.26, 0.16), (0, 0, 0.08), "M_Kraft", bx, bev=0.004)
    box("Box_Tape", (0.05, 0.265, 0.004), (0, 0, 0.162), "M_Tape", bx, bev=0)
    box("Box_Label", (0.12, 0.004, 0.06), (0.05, -0.132, 0.085), "M_Paper_White", bx, bev=0)
    return g


def bookshelf(parent, pos, rot):
    """Five-shelf bookcase with a cupboard at the bottom. Returns (group, shelf heights)."""
    g = group("Bookshelf", parent, pos, rot)
    W, D, H = 0.64, 0.3, 1.5
    shelves = [0.43, 0.79, 1.14]
    body = [
        box("Shelf_SideL", (0.02, D, H), (-W / 2 + 0.01, 0, H / 2), "M_Paint_Ivory", g, bev=0.003),
        box("Shelf_SideR", (0.02, D, H), (W / 2 - 0.01, 0, H / 2), "M_Paint_Ivory", g, bev=0.003),
        box("Shelf_Top", (W + 0.02, D + 0.01, 0.022), (0, 0, H - 0.011), "M_Paint_Ivory", g, bev=0.004),
        box("Shelf_Bottom", (W - 0.04, D - 0.02, 0.02), (0, 0, 0.06), "M_Paint_Ivory", g, bev=0.002),
        box("Shelf_Kick", (W - 0.04, 0.02, 0.05), (0, -D / 2 + 0.03, 0.025), "M_Paint_Ivory", g, bev=0.002),
        box("Shelf_Back", (W - 0.02, 0.008, H - 0.02), (0, D / 2 - 0.004, H / 2), "M_Wood_Light", g, bev=0),
    ]
    for k, z in enumerate(shelves):
        body.append(box("Shelf_Board%d" % k, (W - 0.04, D - 0.02, 0.018), (0, 0.005, z - 0.009), "M_Paint_Ivory", g, bev=0.002))
    join("Bookshelf_Body", body)
    box("Shelf_CupboardDoor", (W - 0.05, 0.018, shelves[0] - 0.1), (0, -D / 2 + 0.009, 0.07 + (shelves[0] - 0.1) / 2), "M_Paint_Ivory", g,
        bev=0.004)
    sphere("Shelf_Knob", 0.012, (0.2, -D / 2 - 0.012, shelves[0] - 0.12), "M_Gold", g, seg=10, rings=6)
    return g, shelves + [H]


def desk(parent, pos, rot):
    """Desk with a three-drawer pedestal on the right. Returns (group, top drawer)."""
    g = group("Desk", parent, pos, rot)
    W, D, H = 1.0, 0.55, 0.74
    pw = 0.4
    px = W / 2 - pw / 2
    body = [
        box("Desk_Top", (W, D, 0.03), (0, 0, H - 0.015), "M_Wood_Light", g, bev=0.006),
        box("Desk_PanelL", (0.025, D - 0.02, H - 0.03), (-W / 2 + 0.0125, 0, (H - 0.03) / 2), "M_Wood_Light", g, bev=0.003),
        box("Desk_Modesty", (W - pw - 0.03, 0.018, 0.34), (-pw / 2 - 0.01, D / 2 - 0.03, H - 0.2), "M_Wood_Light", g, bev=0.002),
        box("Desk_PedSideL", (0.02, D - 0.02, H - 0.03), (px - pw / 2 + 0.01, 0, (H - 0.03) / 2), "M_Wood_Light", g, bev=0.003),
        box("Desk_PedSideR", (0.02, D - 0.02, H - 0.03), (px + pw / 2 - 0.01, 0, (H - 0.03) / 2), "M_Wood_Light", g, bev=0.003),
        box("Desk_PedBack", (pw - 0.04, 0.012, H - 0.05), (px, D / 2 - 0.016, (H - 0.03) / 2), "M_Wood_Light", g, bev=0),
        box("Desk_PedKick", (pw - 0.04, 0.02, 0.04), (px, -D / 2 + 0.03, 0.02), "M_Wood_Dark", g, bev=0.002),
    ]
    join("Desk_Body", body)
    spans = [(0.535, 0.705), (0.315, 0.525), (0.045, 0.305)]
    top_drawer = None
    for k, (z0, z1) in enumerate(spans):
        name = "INT_Drawer" if k == 0 else "Desk_Drawer%d" % k
        dg = drawer(name, g, (px, -D / 2 + 0.005, (z0 + z1) / 2), pw - 0.05, z1 - z0 - 0.006, D - 0.06, front_mat="M_Wood_Light",
                    body_mat="M_Wood_Light", handle_mat="M_Metal_Chrome")
        if k == 0:
            top_drawer = dg
    return g, top_drawer


def chair(parent, pos, rot):
    """Rolling desk chair with a round seat."""
    g = group("DeskChair", parent, pos, rot)
    for i in range(5):
        a = i * 2 * math.pi / 5
        arm = group("Chair_Leg%d" % i, g, (0, 0, 0.09), (0, 0, math.degrees(a)))
        box("Chair_LegBar%d" % i, (0.3, 0.04, 0.03), (0.15, 0, 0), "M_Plastic_Black", arm, rot=(0, -4, 0), bev=0.01)
        sphere("Chair_Caster%d" % i, 0.028, (0.29, 0, -0.058), "M_Plastic_Black", arm, seg=12, rings=6, scale=(1.0, 0.7, 1.0))
    cylinder("Chair_Hub", 0.04, 0.06, (0, 0, 0.07), "M_Plastic_Black", g, verts=16)
    cylinder("Chair_Column", 0.025, 0.28, (0, 0, 0.12), "M_Metal_Chrome", g, verts=14)
    cylinder("Chair_Shroud", 0.034, 0.12, (0, 0, 0.1), "M_Plastic_Black", g, verts=14)
    box("Chair_Mech", (0.18, 0.2, 0.04), (0, 0, 0.41), "M_Plastic_Black", g, bev=0.01)
    lathe("Chair_Seat", [(0, 0), (0.2, 0), (0.225, 0.02), (0.23, 0.045), (0.215, 0.07), (0.18, 0.08), (0, 0.082)], (0, 0, 0.43),
          "M_Fabric_Navy", g, segs=36, angle=35)
    cable("Chair_Spine", [(0, 0.12, 0.44), (0, 0.22, 0.46), (0, 0.24, 0.6), (0, 0.23, 0.68)], 0.018, "M_Plastic_Black", g)
    backrest = cushion("Chair_Back", (0.38, 0.06, 0.26), 0.35, (0, 0.235, 0.8), "M_Fabric_Navy", g, rot=(-8, 0, 0), cuts=4)
    return g


def corner_shelf(parent, pos, rot):
    """Five-tier metal-frame shelf with X bracing, standing in the north-east corner. Returns (group, tier heights)."""
    g = group("CornerShelf", parent, pos, rot)
    W, D, H = 0.6, 0.34, 1.75
    tiers = [0.06, 0.42, 0.78, 1.14, 1.5]
    frame = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            frame.append(box("CShelf_Post%d%d" % (sx, sy), (0.022, 0.022, H), (sx * (W / 2 - 0.011), sy * (D / 2 - 0.011), H / 2),
                             "M_Metal_Black", g, bev=0.003))
        for k in range(len(tiers) - 1):
            za, zb = tiers[k] + 0.02, tiers[k + 1] - 0.02
            x = sx * (W / 2 - 0.011)
            frame.append(cable("CShelf_BraceA%d%d" % (sx, k), [(x, -D / 2 + 0.02, za), (x, D / 2 - 0.02, zb)], 0.004, "M_Metal_Black", g,
                               res=2, bevel_res=1))
            frame.append(cable("CShelf_BraceB%d%d" % (sx, k), [(x, D / 2 - 0.02, za), (x, -D / 2 + 0.02, zb)], 0.004, "M_Metal_Black", g,
                               res=2, bevel_res=1))
    join("CornerShelf_Frame", frame)
    boards = [box("CShelf_Board%d" % k, (W - 0.01, D - 0.01, 0.018), (0, 0, z), "M_Wood_Light", g, bev=0.003) for k, z in enumerate(tiers)]
    join("CornerShelf_Boards", boards)
    return g, [z + 0.009 for z in tiers]


# ---------------------------------------------------------------- TV corner, appliances

def crt_tv(parent, pos, rot):
    """Small 14-inch CRT TV with a rabbit-ear antenna."""
    g = group("CRT_TV", parent, pos, rot)
    W, H = 0.42, 0.36
    box("TV_Bezel", (W, 0.06, H), (0, -0.16, H / 2 + 0.01), "M_Plastic_Gray", g, bev=0.02, segs=3)
    box("TV_Tube", (W - 0.06, 0.3, H - 0.06), (0, 0.0, H / 2 + 0.01), "M_Plastic_Gray", g, bev=0.05, segs=3)
    box("TV_Neck", (0.2, 0.12, 0.18), (0, 0.18, H / 2 - 0.02), "M_Plastic_Gray", g, bev=0.04, segs=3)
    box("TV_Foot", (W - 0.08, 0.3, 0.02), (0, -0.02, 0.01), "M_Plastic_Black", g, bev=0.006)
    sw, sh, sc = W - 0.1, H - 0.1, (-0.03, H / 2 + 0.04)
    scr = plane("TV_Screen", sw, sh, (sc[0], -0.203, sc[1]), "M_TV_Screen", g, rot=(90, 0, 0), nx=6, ny=1, bend=-0.01)
    uv_job(scr, "planar_local", rect=(0, 0, 1, 1))
    rim = [box("TV_RimT", (sw + 0.04, 0.012, 0.02), (sc[0], -0.197, sc[1] + sh / 2), "M_Plastic_Black", g, bev=0.004),
           box("TV_RimB", (sw + 0.04, 0.012, 0.02), (sc[0], -0.197, sc[1] - sh / 2), "M_Plastic_Black", g, bev=0.004),
           box("TV_RimL", (0.02, 0.012, sh), (sc[0] - sw / 2, -0.197, sc[1]), "M_Plastic_Black", g, bev=0.004),
           box("TV_RimR", (0.02, 0.012, sh), (sc[0] + sw / 2, -0.197, sc[1]), "M_Plastic_Black", g, bev=0.004)]
    join("TV_ScreenRim", rim)
    for k in range(4):
        box("TV_Button%d" % k, (0.018, 0.008, 0.012), (W / 2 - 0.035, -0.192, 0.11 + k * 0.03), "M_Plastic_Black", g, bev=0.002)
    for k in range(5):
        box("TV_Grille%d" % k, (0.03, 0.006, 0.004), (W / 2 - 0.035, -0.191, 0.26 + k * 0.012), "M_Plastic_Black", g, bev=0)
    sphere("TV_LED", 0.004, (W / 2 - 0.035, -0.192, 0.07), "M_LED_Red", g, seg=8, rings=4)
    ag = group("TV_Antenna", g, (0.02, 0.05, H + 0.01))
    lathe("Antenna_Base", [(0, 0), (0.05, 0), (0.045, 0.02), (0.02, 0.035), (0, 0.035)], (0, 0, 0), "M_Plastic_Black", ag, segs=20)
    for s in (-1, 1):
        cable("Antenna_Rod%d" % s, [(s * 0.01, 0, 0.03), (s * 0.14, 0.02, 0.3)], 0.003, "M_Metal_Chrome", ag, res=2, bevel_res=1)
        sphere("Antenna_Tip%d" % s, 0.007, (s * 0.14, 0.02, 0.3), "M_Metal_Chrome", ag, seg=8, rings=4)
    return g


def tv_cabinet(parent, pos, rot):
    """Low dark cabinet: two drawers over an open shelf with a video player and tapes."""
    g = group("TVCabinet", parent, pos, rot)
    W, D, H = 0.92, 0.4, 0.5
    body = [
        box("TVC_Top", (W + 0.02, D + 0.01, 0.025), (0, 0, H - 0.0125), "M_Wood_Dark", g, bev=0.005),
        box("TVC_SideL", (0.022, D, H - 0.025), (-W / 2 + 0.011, 0, (H - 0.025) / 2), "M_Wood_Dark", g, bev=0.003),
        box("TVC_SideR", (0.022, D, H - 0.025), (W / 2 - 0.011, 0, (H - 0.025) / 2), "M_Wood_Dark", g, bev=0.003),
        box("TVC_Bottom", (W - 0.04, D - 0.02, 0.02), (0, 0, 0.06), "M_Wood_Dark", g, bev=0.002),
        box("TVC_Shelf", (W - 0.04, D - 0.02, 0.018), (0, 0, 0.3), "M_Wood_Dark", g, bev=0.002),
        box("TVC_Back", (W - 0.02, 0.008, H - 0.03), (0, D / 2 - 0.004, H / 2), "M_Wood_Dark", g, bev=0),
        box("TVC_Kick", (W - 0.04, 0.02, 0.05), (0, -D / 2 + 0.03, 0.025), "M_Wood_Dark", g, bev=0.002),
    ]
    join("TVCabinet_Body", body)
    for k, x in enumerate((-W / 4 + 0.006, W / 4 - 0.006)):
        drawer("TVC_Drawer%d" % k, g, (x, -D / 2 + 0.005, 0.4), W / 2 - 0.03, 0.15, D - 0.06, front_mat="M_Wood_Cherry", body_mat="M_Wood_Dark",
               handle_mat="M_Gold")
    vg = group("VideoPlayer", g, (-0.16, -0.02, 0.07))
    box("VCR_Body", (0.36, 0.26, 0.085), (0, 0, 0.0425), "M_Plastic_Black", vg, bev=0.006)
    box("VCR_Slot", (0.2, 0.004, 0.012), (-0.04, -0.131, 0.06), "M_Plastic_Gray", vg, bev=0)
    sphere("VCR_LED", 0.003, (0.14, -0.131, 0.06), "M_LED_Green", vg, seg=8, rings=4)
    for k in range(4):
        box("VHS_Tape%d" % k, (0.025, 0.19, 0.105), (0.12 + k * 0.03, 0.0, 0.07 + 0.0525), ("M_Cover_Red", "M_Plastic_Black", "M_Cover_Blue",
            "M_Plastic_Black")[k], g, rot=(0, 0, 0), bev=0.003)
    return g


def dehumidifier(parent, pos, rot):
    """Compact dehumidifier, running all summer in a humid room."""
    g = group("Dehumidifier", parent, pos, rot)
    W, D, H = 0.3, 0.22, 0.5
    box("Dehum_Body", (W, D, H), (0, 0, H / 2), "M_Plastic_White", g, bev=0.035, segs=3)
    box("Dehum_Top", (W - 0.06, D - 0.06, 0.01), (0, 0, H - 0.002), "M_Plastic_Gray", g, bev=0.004)
    for k in range(7):
        box("Dehum_Slat%d" % k, (W - 0.08, 0.006, 0.004), (0, -D / 2 + 0.05 + k * 0.02, H + 0.004), "M_Plastic_Gray", g, bev=0)
    box("Dehum_Tank", (W - 0.06, 0.012, 0.16), (0, -D / 2 - 0.002, 0.12), "M_Plastic_Blue", g, bev=0.01)
    box("Dehum_Window", (0.05, 0.006, 0.1), (0.07, -D / 2 - 0.009, 0.12), "M_Glass_Bottle", g, bev=0.003)
    box("Dehum_Panel", (0.12, 0.006, 0.04), (0, -D / 2 - 0.002, H - 0.08), "M_Plastic_Black", g, bev=0.006)
    sphere("Dehum_LED", 0.004, (0.04, -D / 2 - 0.006, H - 0.08), "M_LED_Green", g, seg=8, rings=4)
    for s in (-1, 1):
        box("Dehum_Grip%d" % s, (0.012, 0.1, 0.03), (s * (W / 2 + 0.002), 0, H - 0.1), "M_Plastic_Gray", g, bev=0.004)
    return g


def floor_mirror(parent, pos, rot):
    """Tall mirror leaning against a wall; origin at its foot, leaning back (+Y)."""
    g = group("FloorMirror", parent, pos, rot)
    lean = group("Mirror_Lean", g, (0, 0, 0), (-8, 0, 0))
    W, H = 0.38, 1.45
    parts = [
        box("Mirror_FrameL", (0.035, 0.03, H), (-W / 2 + 0.0175, 0, H / 2), "M_Wood_Light", lean, bev=0.006),
        box("Mirror_FrameR", (0.035, 0.03, H), (W / 2 - 0.0175, 0, H / 2), "M_Wood_Light", lean, bev=0.006),
        box("Mirror_FrameT", (W, 0.03, 0.035), (0, 0, H - 0.0175), "M_Wood_Light", lean, bev=0.006),
        box("Mirror_FrameB", (W, 0.03, 0.05), (0, 0, 0.025), "M_Wood_Light", lean, bev=0.006),
    ]
    join("Mirror_Frame", parts)
    box("Mirror_Glass", (W - 0.06, 0.006, H - 0.08), (0, -0.004, H / 2 + 0.008), "M_Mirror", lean, bev=0)
    box("Mirror_Sheen", (W - 0.06, 0.002, H - 0.08), (0, -0.009, H / 2 + 0.008), "M_Glass", lean, bev=0)
    return g


def backpack(parent, pos, rot):
    """Backpack hanging from a hook by its top loop; origin at the hook."""
    g = group("Backpack", parent, pos, rot)
    body = cushion("Backpack_Body", (0.29, 0.14, 0.4), 0.3, (0, -0.075, -0.25), "M_Backpack", g, cuts=4)
    uv_job(body, "box", scale=4.0)
    cushion("Backpack_Pocket", (0.22, 0.06, 0.16), 0.35, (0, -0.15, -0.34), "M_Backpack", g, cuts=3)
    cable("Backpack_Zip", [(-0.11, -0.18, -0.27), (0, -0.182, -0.26), (0.11, -0.18, -0.27)], 0.003, "M_Metal_Chrome", g)
    cable("Backpack_Loop", [(-0.03, -0.05, -0.05), (-0.02, -0.02, 0.02), (0.02, -0.02, 0.02), (0.03, -0.05, -0.05)], 0.006,
          "M_Plastic_Black", g)
    for s in (-1, 1):
        cable("Backpack_Strap%d" % s, [(s * 0.07, -0.01, -0.07), (s * 0.1, 0.005, -0.2), (s * 0.11, 0.0, -0.36), (s * 0.12, -0.03, -0.44)],
              0.012, "M_Plastic_Black", g)
    return g


def hook_rail(parent, pos, rot, hooks=3, spacing=0.16):
    """Wooden board with coat hooks; hooks stick out toward local -Y."""
    g = group("HookRail", parent, pos, rot)
    W = spacing * (hooks - 1) + 0.16
    box("HookRail_Board", (W, 0.02, 0.08), (0, -0.01, 0), "M_Wood_Cherry", g, bev=0.004)
    for k in range(hooks):
        x = -W / 2 + 0.08 + k * spacing
        cable("HookRail_Hook%d" % k, [(x, -0.02, 0.0), (x, -0.06, -0.01), (x, -0.075, 0.02), (x, -0.07, 0.035)], 0.005, "M_Metal_Black", g)
    return g
