"""The view from the west window. The bedroom is on the third floor; across a narrow
alley stands a two-storey brick villa with a green rooftop, a water tank, planters,
a drying rack and a rooftop room. Power lines cross the alley, and the painted
city (tex_prints.backdrop) closes the view."""
import math

from dcm import box, cable, cylinder, group, join, lathe, plane, sphere, uv_job
from bedroom_shell import ROOM_X, WALL_T

GROUND_Z = -5.6            # alley level; our floor is two storeys up
NB_EAST = -4.2             # neighbour's east face
NB_WEST = -10.6
NB_SOUTH, NB_NORTH = -4.5, 5.5
ROOF_Z = -0.4              # neighbour's roof deck
PARAPET = 0.9


def neighbour_villa(parent):
    g = group("NeighbourVilla", parent)
    cx, cy = (NB_EAST + NB_WEST) / 2, (NB_SOUTH + NB_NORTH) / 2
    w, d = NB_EAST - NB_WEST, NB_NORTH - NB_SOUTH
    box("Villa_Body", (w, d, ROOF_Z - GROUND_Z), (cx, cy, (ROOF_Z + GROUND_Z) / 2), "M_Brick_Red", g, bev=0)
    deck = box("Villa_RoofDeck", (w - 0.3, d - 0.3, 0.02), (cx, cy, ROOF_Z + 0.01), "M_Roof_Green", g, bev=0)
    uv_job(deck, "box", scale=0.5)
    t = 0.15
    walls = [
        box("Parapet_E", (t, d, PARAPET), (NB_EAST - t / 2, cy, ROOF_Z + PARAPET / 2), "M_Brick_Red", g, bev=0.004),
        box("Parapet_W", (t, d, PARAPET), (NB_WEST + t / 2, cy, ROOF_Z + PARAPET / 2), "M_Brick_Red", g, bev=0.004),
        box("Parapet_S", (w, t, PARAPET), (cx, NB_SOUTH + t / 2, ROOF_Z + PARAPET / 2), "M_Brick_Red", g, bev=0.004),
        box("Parapet_N", (w, t, PARAPET), (cx, NB_NORTH - t / 2, ROOF_Z + PARAPET / 2), "M_Brick_Red", g, bev=0.004),
    ]
    join("Villa_Parapet", walls)
    caps = [
        box("ParapetCap_E", (t + 0.06, d + 0.06, 0.05), (NB_EAST - t / 2, cy, ROOF_Z + PARAPET + 0.025), "M_Concrete", g, bev=0.008),
        box("ParapetCap_W", (t + 0.06, d + 0.06, 0.05), (NB_WEST + t / 2, cy, ROOF_Z + PARAPET + 0.025), "M_Concrete", g, bev=0.008),
        box("ParapetCap_S", (w, t + 0.06, 0.05), (cx, NB_SOUTH + t / 2, ROOF_Z + PARAPET + 0.025), "M_Concrete", g, bev=0.008),
        box("ParapetCap_N", (w, t + 0.06, 0.05), (cx, NB_NORTH - t / 2, ROOF_Z + PARAPET + 0.025), "M_Concrete", g, bev=0.008),
    ]
    join("Villa_ParapetCap", caps)
    # Windows on the east face, two storeys, and the yellow gas pipe along the top.
    wins = []
    for fl, z in enumerate((GROUND_Z + 1.0, GROUND_Z + 3.8)):
        for y in (-2.5, 0.2, 3.0):
            wins.append(box("Villa_Win%d_%d" % (fl, int(y * 10)), (0.04, 1.2, 1.1), (NB_EAST + 0.01, y, z + 0.55), "M_Window_Dark", g, bev=0))
            wins.append(box("Villa_WinFrame%d_%d" % (fl, int(y * 10)), (0.06, 1.3, 0.06), (NB_EAST + 0.02, y, z - 0.03), "M_Concrete", g,
                            bev=0.004))
    join("Villa_Windows", wins)
    pipe = [cylinder("Gas_Run", 0.03, d - 1.0, (NB_EAST + 0.06, NB_SOUTH + 0.5, ROOF_Z - 0.35), "M_Gas_Pipe", g, rot=(-90, 0, 0), verts=12),
            cylinder("Gas_Riser", 0.03, 5.0, (NB_EAST + 0.06, 1.4, GROUND_Z), "M_Gas_Pipe", g, verts=12)]
    for k in range(6):
        pipe.append(box("Gas_Clip%d" % k, (0.09, 0.03, 0.08), (NB_EAST + 0.04, NB_SOUTH + 0.8 + k * 1.6, ROOF_Z - 0.35), "M_Metal_Gray", g,
                        bev=0.004))
    join("Villa_GasPipe", pipe)
    box("Villa_GasMeter", (0.14, 0.3, 0.36), (NB_EAST + 0.08, 1.4, ROOF_Z - 0.85), "M_Metal_Gray", g, bev=0.01)
    return g


def rooftop(parent):
    g = group("Rooftop", parent)
    z = ROOF_Z + 0.02
    # Rooftop room (oktapbang) with a corrugated roof, door and window facing us.
    rg = group("RooftopRoom", g, (-9.35, 2.4, z))
    box("Oktap_Walls", (2.2, 3.4, 2.1), (0, 0, 1.05), "M_ExteriorWall", rg, bev=0.01)
    roof = []
    for k in range(16):
        roof.append(box("Oktap_Rib%d" % k, (2.7, 0.03, 0.03), (0.05, -1.85 + k * 0.245, 2.2 + 0.0), "M_Roof_Sheet", rg, rot=(0, -6, 0), bev=0.006))
    roof.append(box("Oktap_Roof", (2.7, 3.9, 0.02), (0.05, 0, 2.17), "M_Roof_Sheet", rg, rot=(0, -6, 0), bev=0))
    join("Oktap_RoofSheet", roof)
    box("Oktap_Door", (0.04, 0.8, 1.8), (1.11, -0.8, 0.9), "M_Plastic_Teal", rg, bev=0.006)
    box("Oktap_DoorKnob", (0.04, 0.05, 0.05), (1.14, -0.5, 0.95), "M_Metal_Chrome", rg, bev=0.01)
    box("Oktap_Window", (0.04, 0.9, 0.7), (1.11, 0.7, 1.25), "M_Window_Dark", rg, bev=0)
    box("Oktap_WindowFrame", (0.05, 0.98, 0.78), (1.105, 0.7, 1.25), "M_PVC", rg, bev=0.004)
    box("Oktap_Step", (0.4, 0.9, 0.15), (1.3, -0.8, 0.075), "M_Concrete", rg, bev=0.01)
    box("Oktap_ACUnit", (0.3, 0.8, 0.55), (1.3, 1.2, 0.3), "M_Plastic_White", rg, bev=0.02)
    cylinder("Oktap_ACFan", 0.2, 0.02, (1.46, 1.2, 0.3), "M_Plastic_Gray", rg, rot=(0, 90, 0), verts=24, anchor="center")

    # Water tank on a steel stand.
    tg = group("WaterTank", g, (-7.6, -0.6, z))
    for sx in (-1, 1):
        for sy in (-1, 1):
            box("Tank_Leg%d%d" % (sx, sy), (0.05, 0.05, 0.6), (sx * 0.45, sy * 0.45, 0.3), "M_Metal_Gray", tg, bev=0.004)
    box("Tank_Deck", (1.1, 1.1, 0.05), (0, 0, 0.62), "M_Metal_Gray", tg, bev=0.006)
    lathe("Tank_Body", [(0, 0), (0.52, 0), (0.55, 0.05), (0.55, 1.0), (0.5, 1.08), (0.2, 1.12), (0.2, 1.18), (0, 1.18)], (0, 0, 0.645),
          "M_Plastic_Blue", tg, segs=32, angle=30)
    for k in range(3):
        lathe("Tank_Rib%d" % k, [(0.549, 0.2 + k * 0.3), (0.565, 0.2 + k * 0.3), (0.565, 0.25 + k * 0.3), (0.549, 0.25 + k * 0.3)],
              (0, 0, 0.645), "M_Plastic_Blue", tg, segs=32)
    cable("Tank_Pipe", [(0.5, 0.0, 0.8), (0.8, 0.0, 0.5), (0.9, 0.3, 0.05), (1.5, 1.0, 0.03)], 0.025, "M_Metal_Gray", tg)

    # Styrofoam planters lined up on the east parapet: spring onions and lettuce.
    for k in range(4):
        pg = group("Planter%d" % k, g, (NB_EAST - 0.075, 1.0 + k * 0.62, ROOF_Z + PARAPET + 0.05), (0, 0, 90 + (k % 2) * 3))
        box("Planter_Box%d" % k, (0.55, 0.36, 0.26), (0, 0, 0.13), "M_Styrofoam", pg, bev=0.01)
        box("Planter_Soil%d" % k, (0.5, 0.31, 0.02), (0, 0, 0.24), "M_Soil", pg, bev=0)
        for j in range(9):
            x, y = -0.2 + (j % 5) * 0.1, -0.08 + (j // 5) * 0.16
            if k % 2 == 0:
                cylinder("Onion%d_%d" % (k, j), 0.008, 0.28 + (j % 3) * 0.05, (x, y, 0.24), "M_PlantStem", pg, rot=((j % 3 - 1) * 8, 0, 0),
                         verts=6)
            else:
                sphere("Lettuce%d_%d" % (k, j), 0.06, (x, y, 0.28), "M_Lettuce", pg, seg=10, rings=6, scale=(1, 1, 0.6))

    # Drying rack with towels.
    dg = group("DryingRack", g, (-6.3, 2.6, z), (0, 0, 10))
    for s in (-1, 1):
        cable("Rack_Leg%d" % s, [(s * 0.7, -0.3, 0.0), (s * 0.7, 0.0, 1.1), (s * 0.7, 0.3, 0.0)], 0.012, "M_Metal_Chrome", dg, res=2)
    for k, y in enumerate((-0.08, 0.08)):
        cylinder("Rack_Bar%d" % k, 0.01, 1.4, (-0.7, y, 1.05), "M_Metal_Chrome", dg, rot=(0, 90, 0), verts=8)
    for k, (x, mat, h) in enumerate(((-0.4, "M_Towel_Blue", 0.55), (0.05, "M_Towel_Pink", 0.45), (0.45, "M_Paper_White", 0.6))):
        box("Towel%d" % k, (0.36, 0.02, h), (x, -0.08 + (k % 2) * 0.16, 1.05 - h / 2), mat, dg, bev=0.008)
    box("Rooftop_Stool", (0.3, 0.3, 0.42), (-5.3, 0.9, z + 0.21), "M_Plastic_Red", g, rot=(0, 0, 20), bev=0.03)
    return g


def power_lines(parent):
    """Two poles in the alley and wires sagging across the window view."""
    g = group("PowerLines", parent)
    x = -3.2
    wires = []
    for k, y in enumerate((-5.2, 6.4)):
        pg = group("Pole%d" % k, g, (x, y, GROUND_Z))
        lathe("Pole_Shaft%d" % k, [(0, 0), (0.15, 0), (0.1, 10.5), (0, 10.5)], (0, 0, 0), "M_Pole", pg, segs=16, angle=30)
        box("Pole_Arm%d" % k, (1.4, 0.1, 0.1), (0, 0, 9.4), "M_Pole", pg, bev=0.01)
        box("Pole_Box%d" % k, (0.3, 0.2, 0.45), (0.14, 0, 7.4), "M_Plastic_Gray", pg, bev=0.02)
        for i, dx in enumerate((-0.6, -0.2, 0.2, 0.6)):
            cylinder("Pole_Insulator%d_%d" % (k, i), 0.03, 0.1, (dx, 0, 9.45), "M_Plastic_White", pg, verts=10)
    z_top = GROUND_Z + 9.5
    for i, dx in enumerate((-0.6, -0.2, 0.2, 0.6)):
        sag = 1.6 + i * 0.15
        pts = [(x + dx, -5.2 + (6.4 + 5.2) * t, z_top - sag * math.sin(math.pi * t)) for t in (0.0, 0.25, 0.5, 0.75, 1.0)]
        wires.append(cable("Wire%d" % i, pts, 0.008, "M_Cable", g, res=12, bevel_res=1))
    drop = [(x - 0.6, 6.4, z_top), (x + 0.2, 3.5, z_top - 1.8), (-2.6, 1.9, 2.9), (-ROOM_X - WALL_T - 0.05, 1.75, 2.45)]
    wires.append(cable("Wire_Drop", drop, 0.008, "M_Cable", g, res=12, bevel_res=1))
    join("PowerLine_Wires", wires)
    box("Wire_Anchor", (0.08, 0.06, 0.06), (-ROOM_X - WALL_T - 0.03, 1.75, 2.45), "M_Metal_Gray", g, bev=0.01)
    return g


def build(root):
    g = group("Exterior", root)
    neighbour_villa(g)
    rooftop(g)
    power_lines(g)
    ground = box("Alley", (8.0, 16.0, 0.1), (-2.0, 0.5, GROUND_Z - 0.05), "M_Asphalt", g, bev=0)
    uv_job(ground, "box", scale=0.5)
    plane("Backdrop", 30.0, 15.0, (-19.0, 0.8, 4.5), "M_Backdrop", g, rot=(90, 0, 90))
    return g
