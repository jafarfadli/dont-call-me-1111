"""What the window shows: terrace, fence, chair, plants, pole and wires, tree,
street and the painted backdrop."""
import math

from dcm import box, cable, cylinder, group, join, lathe, plane, sphere, uv_job
from room_shell import FRONT_DOOR, ROOM_Y, VENT_DOOR, VENT_WINDOW, WALL_T, WINDOW, wall_rects

FENCE_Y = 5.4


def facade(parent):
    g = group("Facade", parent)
    parts = []
    for i, (a, b, z0, z1) in enumerate(wall_rects(-3.15, 3.15, 3.1, [WINDOW, FRONT_DOOR, VENT_WINDOW, VENT_DOOR])):
        parts.append(box("Facade_%d" % i, (b - a, 0.006, z1 - z0), ((a + b) / 2, ROOM_Y + WALL_T + 0.003, (z0 + z1) / 2),
                         "M_ExteriorWall", g, bev=0))
    join("Facade_Mesh", parts)
    ledge = box("Facade_WindowLedge", (WINDOW[1] - WINDOW[0] + 0.16, 0.12, 0.05), ((WINDOW[0] + WINDOW[1]) / 2, ROOM_Y + WALL_T + 0.05,
                                                                                     WINDOW[2] - 0.025), "M_Concrete", g, bev=0.008)
    box("Facade_Plinth", (6.3, 0.02, 0.3), (0, ROOM_Y + WALL_T + 0.01, 0.12), "M_Brick", g, bev=0.004)
    box("Eave", (7.2, 1.3, 0.12), (0, ROOM_Y + WALL_T + 0.6, 3.16), "M_ExteriorWall", g, bev=0.01)
    box("Eave_Fascia", (7.2, 0.03, 0.22), (0, ROOM_Y + WALL_T + 1.25, 3.12), "M_Wood_Dark", g, bev=0.006)
    return g


def fence(parent):
    g = group("Fence", parent)
    posts = [-3.8, -1.3, 1.25, 2.65, 4.2]
    parts = []
    for i, x in enumerate(posts):
        parts.append(box("Pillar%d" % i, (0.3, 0.3, 1.25), (x, FENCE_Y, 0.625), "M_ExteriorWall", g, bev=0.01))
        parts.append(box("PillarCap%d" % i, (0.38, 0.38, 0.07), (x, FENCE_Y, 1.285), "M_Concrete", g, bev=0.012))
    join("Fence_Pillars", parts)
    bars = []
    walls = []
    for i, (a, b) in enumerate(zip(posts, posts[1:])):
        x0, x1 = a + 0.15, b - 0.15
        gate = i == 2
        z0 = 0.05 if gate else 0.38
        if not gate:
            walls.append(box("FenceWall%d" % i, (x1 - x0, 0.18, 0.38), ((x0 + x1) / 2, FENCE_Y, 0.19), "M_ExteriorWall", g, bev=0.006))
        for z in (z0 + 0.04, 1.08):
            bars.append(box("FenceRail%d_%d" % (i, int(z * 100)), (x1 - x0, 0.03, 0.03), ((x0 + x1) / 2, FENCE_Y, z), "M_Fence_Iron", g,
                            bev=0.004))
        n = int((x1 - x0) / 0.11)
        for k in range(n + 1):
            x = x0 + (x1 - x0) * k / n
            bars.append(box("FenceBar%d_%d" % (i, k), (0.016, 0.016, 1.16 - z0), (x, FENCE_Y, (1.16 + z0) / 2), "M_Fence_Iron", g,
                            bev=0.002))
            bars.append(cylinder("FenceTip%d_%d" % (i, k), 0.016, 0.06, (x, FENCE_Y, 1.16), "M_Fence_Iron", g, verts=4, r2=0.0))
    join("Fence_Walls", walls)
    join("Fence_Bars", bars)
    return g


def monobloc(parent, pos, rot):
    g = group("MonoblocChair", parent, pos, rot)
    parts = [box("Chair_Seat", (0.46, 0.44, 0.035), (0, 0, 0.44), "M_Plastic_Teal", g, bev=0.03, segs=3)]
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(cylinder("Chair_Leg%d%d" % (sx, sy), 0.024, 0.44, (sx * 0.2, sy * 0.18, 0.0), "M_Plastic_Teal", g, verts=12,
                                  r2=0.018, rot=(sy * 6, -sx * 6, 0)))
    back = group("Chair_Back", g, (0, 0.2, 0.46), (-12, 0, 0))
    for k in range(4):
        parts.append(box("Chair_Slat%d" % k, (0.42 - k * 0.01, 0.028, 0.06), (0, 0, 0.08 + k * 0.1), "M_Plastic_Teal", back, bev=0.012))
    for sx in (-1, 1):
        parts.append(box("Chair_BackPost%d" % sx, (0.045, 0.03, 0.42), (sx * 0.21, 0, 0.21), "M_Plastic_Teal", back, bev=0.012))
        parts.append(cable("Chair_Arm%d" % sx, [(sx * 0.23, -0.2, 0.44), (sx * 0.25, -0.16, 0.6), (sx * 0.24, 0.05, 0.64), (sx * 0.22, 0.2, 0.62)],
                           0.018, "M_Plastic_Teal", g))
    return g


def pole(parent):
    g = group("PowerPole", parent, (1.05, 6.2, -0.12))
    lathe("Pole_Shaft", [(0, 0), (0.13, 0), (0.09, 8.0), (0, 8.0)], (0, 0, 0), "M_Pole", g, segs=16, angle=30)
    box("Pole_Arm", (1.6, 0.1, 0.1), (0, 0, 7.4), "M_Pole", g, bev=0.01)
    wires = []
    for i, x in enumerate((-0.7, -0.25, 0.25, 0.7)):
        cylinder("Pole_Insulator%d" % i, 0.03, 0.1, (x, 0, 7.45), "M_Plastic_White", g, verts=10)
        for side in (-1, 1):
            far = side * 14.0
            sag = 1.2 + i * 0.1
            pts = [(x, 0, 7.52), (x + far * 0.33, -0.3 * side, 7.52 - sag), (x + far * 0.66, -0.6 * side, 7.52 - sag * 1.1),
                   (x + far, -1.0 * side, 7.3)]
            wires.append(cable("Wire%d_%d" % (i, side), pts, 0.008, "M_Cable", g, res=10, bevel_res=1))
    drop = [(-0.7, 0, 7.5), (-1.4, -1.2, 5.6), (-1.7, -2.6, 3.8), (-1.3, -3.4, 3.25)]
    wires.append(cable("Wire_House", drop, 0.008, "M_Cable", g, res=10, bevel_res=1))
    box("Pole_Box", (0.3, 0.18, 0.4), (0, -0.15, 2.6), "M_Plastic_White", g, bev=0.02)
    join("Pole_Wires", wires)
    return g


def tree(parent, pos, scale=1.0):
    g = group("MangoTree", parent, pos)
    lathe("Tree_Trunk", [(0, 0), (0.2 * scale, 0), (0.14 * scale, 0.8), (0.12 * scale, 2.2 * scale), (0, 2.3 * scale)], (0, 0, 0),
          "M_Tree_Trunk", g, segs=12, angle=35)
    branches = []
    for i, (dx, dy, dz) in enumerate(((0.8, 0.2, 1.2), (-0.7, 0.4, 1.0), (0.1, -0.6, 1.3))):
        branches.append(cable("Tree_Branch%d" % i, [(0, 0, 1.7 * scale), (dx * 0.5, dy * 0.5, 1.7 * scale + dz * 0.6),
                                                     (dx, dy, 1.7 * scale + dz)], 0.07 * scale, "M_Tree_Trunk", g, res=6))
    join("Tree_Branches", branches)
    blobs = [(0, 0, 3.4, 1.3), (0.9, 0.3, 3.0, 1.0), (-0.9, 0.2, 3.1, 1.05), (0.3, -0.7, 3.2, 0.95), (-0.2, 0.7, 3.7, 0.9),
             (0.6, 0.5, 3.8, 0.8)]
    for i, (x, y, z, r) in enumerate(blobs):
        sphere("Tree_Canopy%d" % i, r * scale, (x * scale, y * scale, z * scale), "M_Tree_Leaves", g, seg=18, rings=10,
               scale=(1.0, 1.0, 0.82))
    return g


def bougainvillea(parent, pos):
    g = group("Bougainvillea", parent, pos)
    lathe("Bouga_Pot", [(0, 0), (0.2, 0), (0.28, 0.45), (0.3, 0.46), (0.3, 0.5), (0.26, 0.5), (0, 0.48)], (0, 0, 0), "M_Terracotta",
          g, segs=28)
    for i, (x, y, z, r, mat) in enumerate(((0, 0, 0.9, 0.35, "M_Tree_Leaves"), (0.2, 0.1, 1.15, 0.25, "M_Bougainvillea"),
                                           (-0.18, 0.05, 1.05, 0.26, "M_Bougainvillea"), (0.05, -0.15, 1.25, 0.22, "M_Bougainvillea"),
                                           (-0.1, 0.2, 0.75, 0.22, "M_Tree_Leaves"), (0.25, -0.12, 0.8, 0.2, "M_Bougainvillea"))):
        sphere("Bouga_Blob%d" % i, r, (x, y, z), mat, g, seg=14, rings=8, scale=(1, 1, 0.85))
    return g


def street(parent):
    g = group("Street", parent)
    road = box("Road", (30, 6.6, 0.1), (0, FENCE_Y + 3.65, -0.17), "M_Asphalt", g, bev=0)
    box("Curb_Near", (30, 0.25, 0.14), (0, FENCE_Y + 0.4, -0.09), "M_Concrete", g, bev=0.01)
    box("Curb_Far", (30, 0.25, 0.14), (0, FENCE_Y + 5.6, -0.09), "M_Concrete", g, bev=0.01)
    dashes = []
    for i in range(12):
        dashes.append(box("RoadDash%d" % i, (0.9, 0.1, 0.01), (-12 + i * 2.2, FENCE_Y + 3.0, -0.115), "M_Plastic_White", g, bev=0))
    join("Road_Dashes", dashes)
    bd = plane("Backdrop", 22.0, 11.0, (0, FENCE_Y + 6.2, 4.7), "M_Backdrop", g, rot=(90, 0, 0))
    return g


def build(root):
    g = group("Exterior", root)
    facade(g)
    terrace = box("Terrace", (7.6, FENCE_Y - ROOM_Y - WALL_T, 0.1), (0, (ROOM_Y + WALL_T + FENCE_Y) / 2, -0.08), "M_Terrace", g, bev=0)
    uv_job(terrace, "box", scale=1 / 1.6)
    box("Terrace_Step", (7.6, 0.3, 0.06), (0, FENCE_Y - 0.25, -0.13), "M_Concrete", g, bev=0.01)
    fence(g)
    monobloc(g, (-0.55, 3.55, -0.03), (0, 0, 200))
    bougainvillea(g, (-2.35, 3.25, -0.03))
    pole(g)
    tree(g, (3.9, 6.9, -0.12), 1.15)
    street(g)
    return g
