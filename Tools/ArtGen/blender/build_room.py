"""Build the bedroom and export it for Unity.

Run: Blender -b --factory-startup -P Tools/ArtGen/blender/build_room.py [-- --preview OUT_DIR]
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(1, os.path.dirname(HERE))   # atlas.py

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import bedroom_exterior  # noqa: E402
import bedroom_props  # noqa: E402
import bedroom_shell  # noqa: E402
import dcm  # noqa: E402

PROJECT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
FBX_PATH = os.path.join(PROJECT, "Assets", "_Game", "Art", "Models", "SM_Bedroom.fbx")


def build():
    dcm.reset()
    dcm.load_materials(os.path.join(HERE, "..", "materials.json"))
    root = dcm.group("Bedroom")
    bedroom_shell.build(root)
    bedroom_props.build(root)
    bedroom_exterior.build(root)
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
        "a_overview": ((1.45, -1.55, 1.65), (-1.0, 1.0, 0.9), 16),
        "b_window_bed": ((0.4, -0.3, 1.5), (-1.8, 0.9, 1.1), 18),
        "c_desk": ((0.5, 0.3, 1.5), (1.0, 1.9, 1.0), 18),
        "d_door": ((-0.6, 1.0, 1.6), (0.9, -1.9, 1.0), 16),
        "e_wardrobe": ((0.4, 0.6, 1.6), (-1.6, -1.3, 1.1), 16),
        "f_top": ((0.0, 0.0, 9.0), (0.0, 0.0, 0.0), 0),
        "g_outside": ((-0.6, 0.85, 1.55), (-8.0, 1.0, 0.8), 24),
        "h_desktop": ((0.92, 1.05, 1.25), (0.92, 1.6, 0.74), 24),
    }
    for name, (loc, target, lens) in shots.items():
        cam_data = bpy.data.cameras.new(name)
        if name == "f_top":
            cam_data.type = 'ORTHO'
            cam_data.ortho_scale = 4.6
        else:
            cam_data.lens = lens
        cam = bpy.data.objects.new(name, cam_data)
        bpy.context.scene.collection.objects.link(cam)
        cam.location = loc
        look(cam, target)
        if name == "f_top":
            cam.rotation_euler = (0, 0, 0)
            cam.location = (0, 0, 2.3)
            cam_data.clip_end = 3.0
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
