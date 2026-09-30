"""Build the retired living room and export it (snapshot kept for reference).

Run: Blender -b --factory-startup -P Tools/ArtGen/legacy/living_room/blender/build_living_room.py [-- --preview OUT_DIR]
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(1, os.path.abspath(os.path.join(HERE, "..", "..", "..", "blender")))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import dcm  # noqa: E402
import room_exterior  # noqa: E402
import room_furniture as rf  # noqa: E402
import room_props  # noqa: E402
import room_shell  # noqa: E402

PROJECT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
FBX_PATH = os.path.join(PROJECT, "Assets", "_Game", "Art", "Models", "SM_LivingRoom.fbx")


def build():
    dcm.reset()
    dcm.load_materials(os.path.join(HERE, "..", "materials_living_room.json"))
    root = dcm.group("LivingRoom")
    room_shell.build(root)

    fg = dcm.group("Furniture", root)
    rf.seat_unit("Sofa", fg, (2.58, 0.0, 0), (0, 0, -90), 1.9, 3, pillow=(0.66, "M_Fabric_Mustard"))
    rf.seat_unit("Armchair_N", fg, (0.95, 1.22, 0), (0, 0, 0), 0.8, 1)
    rf.seat_unit("Armchair_S", fg, (0.95, -1.22, 0), (0, 0, 180), 0.8, 1, pillow=(0.0, "M_Fabric_Cream"))
    table = rf.coffee_table(fg, (0.95, 0.0, 0), (0, 0, 90))
    rug = dcm.box("Rug", (1.85, 2.75, 0.008), (0.95, 0.0, 0.004), "M_Rug", fg, bev=0.003)
    dcm.uv_job(rug, "planar", axis="Z", rect=(0, 0, 1, 1))
    bufet, drawer = rf.bufet(fg, (-2.74, 0.0, 0), (0, 0, 90))
    rf.tv(bufet, (0.0, 0.03, 0.783), (0, 0, 0))
    side = rf.side_table(fg, (2.72, -1.4, 0), (0, 0, 0))
    rf.standing_fan(fg, (-2.45, 1.9, 0), (0, 0, 45))
    rf.dispenser(fg, (-0.95, -2.27, 0), (0, 0, 180))
    rf.ceiling_lamp(fg, (0.5, 0.0, room_shell.ROOM_H))

    room_props.build(root, {"table": table, "bufet": bufet, "drawer": drawer, "side": side})
    room_exterior.build(root)
    dcm.finalize_all()
    return root


def look(cam, target):
    d = Vector(target) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()


def preview(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    scn = bpy.context.scene
    scn.render.engine = 'BLENDER_WORKBENCH'
    sh = scn.display.shading
    sh.light = 'STUDIO'
    sh.color_type = 'MATERIAL'
    sh.show_object_outline = True
    sh.object_outline_color = (0.05, 0.04, 0.06)
    sh.show_cavity = True
    scn.render.resolution_x, scn.render.resolution_y = 1280, 720
    shots = {
        "a_tv_wall": ((2.0, 1.4, 1.6), (-3.0, -0.2, 1.1)),
        "b_window": ((1.2, -1.9, 1.6), (-0.8, 2.5, 1.2)),
        "c_back": ((0.6, 1.9, 1.6), (0.2, -2.5, 1.3)),
        "d_sofa": ((-1.8, -0.6, 1.6), (3.0, 0.2, 1.0)),
        "e_table": ((0.95, -0.55, 1.35), (0.95, 0.05, 0.42)),
        "f_top": ((0.0, 0.0, 9.0), (0.0, 0.0, 0.0)),
    }
    for name, (loc, target) in shots.items():
        cam_data = bpy.data.cameras.new(name)
        cam_data.lens = 16 if name != "e_table" else 24
        if name == "f_top":
            cam_data.type = 'ORTHO'
            cam_data.ortho_scale = 7.5
        cam = bpy.data.objects.new(name, cam_data)
        bpy.context.scene.collection.objects.link(cam)
        cam.location = loc
        look(cam, target)
        scn.camera = cam
        scn.render.filepath = os.path.join(out_dir, name + ".png")
        bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    root = build()
    dcm.export_fbx(FBX_PATH, root)
    print("EXPORTED", FBX_PATH, "objects:", len([o for o in bpy.data.objects if o.type == 'MESH']))
    if "--preview" in argv:
        preview(argv[argv.index("--preview") + 1])
