"""Blender helpers for building the Don't Call Me room in code.

Conventions: meters, Z up, room centre on the floor at the origin, +Y is north.
Every prop is built in local space under an empty, then the empty is placed.
Materials are named like the Unity materials they map to (see materials.json).
"""
import json
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

MATERIALS = {}
UV_JOBS = []


# ---------------------------------------------------------------- scene / materials

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATERIALS.clear()
    UV_JOBS.clear()


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def load_materials(path):
    with open(path) as f:
        data = {m["name"]: m for m in json.load(f)["materials"]}
    for name, spec in data.items():
        mat = bpy.data.materials.new(name)
        col = spec.get("color", "#cccccc").lstrip("#")
        rgb = [srgb_to_linear(int(col[i:i + 2], 16) / 255.0) for i in (0, 2, 4)]
        mat.diffuse_color = (*rgb, 1.0)
        MATERIALS[name] = mat
    return data


def M(name):
    if name not in MATERIALS:
        raise KeyError("material not defined in materials.json: " + name)
    return MATERIALS[name]


def link(ob, parent=None, pos=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    bpy.context.scene.collection.objects.link(ob)
    if parent is not None:
        ob.parent = parent
    ob.location = pos
    ob.rotation_euler = [math.radians(a) for a in rot]
    ob.scale = scale
    return ob


def group(name, parent=None, pos=(0, 0, 0), rot=(0, 0, 0)):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_size = 0.15
    return link(ob, parent, pos, rot)


def mesh_object(name, bm, mat, parent, pos, rot, scale=(1, 1, 1)):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    if mat is not None:
        for m in (mat if isinstance(mat, (list, tuple)) else [mat]):
            me.materials.append(M(m) if isinstance(m, str) else m)
    ob = bpy.data.objects.new(name, me)
    return link(ob, parent, pos, rot, scale)


def bevel(ob, width, segments=2, angle=50):
    if width <= 0:
        return ob
    mod = ob.modifiers.new("Bevel", 'BEVEL')
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(angle)
    mod.use_clamp_overlap = True
    return ob


def subsurf(ob, levels=2):
    mod = ob.modifiers.new("Subsurf", 'SUBSURF')
    mod.levels = levels
    mod.render_levels = levels
    return ob


def solidify(ob, thickness, offset=0.0):
    mod = ob.modifiers.new("Solidify", 'SOLIDIFY')
    mod.thickness = thickness
    mod.offset = offset
    mod.use_even_offset = True
    return ob


def uv_job(ob, mode, **kw):
    UV_JOBS.append((ob, mode, kw))
    return ob


def smooth(ob, angle=40):
    ob["smooth_angle"] = angle
    return ob


# ---------------------------------------------------------------- primitives

def box(name, size, pos=(0, 0, 0), mat=None, parent=None, rot=(0, 0, 0), bev=0.004, segs=2, anchor="center",
        cuts=0):
    sx, sy, sz = size
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    if cuts:
        bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    for v in bm.verts:
        v.co.x *= sx
        v.co.y *= sy
        v.co.z *= sz
        if anchor == "bottom":
            v.co.z += sz / 2
    ob = mesh_object(name, bm, mat, parent, pos, rot)
    bevel(ob, min(bev, min(size) * 0.45), segs)
    return ob


def cylinder(name, r, h, pos=(0, 0, 0), mat=None, parent=None, rot=(0, 0, 0), verts=24, bev=0.0, segs=2,
             anchor="bottom", r2=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=verts, radius1=r,
                          radius2=r if r2 is None else r2, depth=h)
    if anchor == "bottom":
        for v in bm.verts:
            v.co.z += h / 2
    ob = mesh_object(name, bm, mat, parent, pos, rot)
    bevel(ob, bev, segs)
    return ob


def sphere(name, r, pos=(0, 0, 0), mat=None, parent=None, rot=(0, 0, 0), seg=24, rings=12, scale=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
    ob = mesh_object(name, bm, mat, parent, pos, rot, scale)
    return smooth(ob, 180)


def lathe(name, profile, pos=(0, 0, 0), mat=None, parent=None, rot=(0, 0, 0), segs=32, angle=60):
    """Revolve a closed (r, z) profile around Z. Points with r == 0 become poles."""
    bm = bmesh.new()
    rings = []
    for r, z in profile:
        if r <= 1e-6:
            rings.append([bm.verts.new((0.0, 0.0, z))])
        else:
            rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / segs), r * math.sin(2 * math.pi * i / segs), z))
                          for i in range(segs)])
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1:
            continue
        for i in range(segs):
            j = (i + 1) % segs
            if len(a) == 1:
                bm.faces.new((a[0], b[j], b[i]))
            elif len(b) == 1:
                bm.faces.new((a[i], a[j], b[0]))
            else:
                bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    ob = mesh_object(name, bm, mat, parent, pos, rot)
    return smooth(ob, angle)


def extrude_shape(name, pts2d, depth, pos=(0, 0, 0), mat=None, parent=None, rot=(0, 0, 0), bev=0.0, axis="Z"):
    """Extrude a closed 2D polygon (x, y) by depth along Z (then rotated into place)."""
    bm = bmesh.new()
    bottom = [bm.verts.new((x, y, 0.0)) for x, y in pts2d]
    top = [bm.verts.new((x, y, depth)) for x, y in pts2d]
    n = len(pts2d)
    bm.faces.new(list(reversed(bottom)))
    bm.faces.new(top)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bottom[i], bottom[j], top[j], top[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    ob = mesh_object(name, bm, mat, parent, pos, rot)
    bevel(ob, bev, 2)
    return ob


def plane(name, w, h, pos=(0, 0, 0), mat=None, parent=None, rot=(0, 0, 0), nx=1, ny=1, bend=0.0):
    """XY plane w x h centred at origin (normal +Z). `bend` curls it along X."""
    bm = bmesh.new()
    verts = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            u, v = i / nx, j / ny
            x, y = (u - 0.5) * w, (v - 0.5) * h
            z = bend * (4 * (u - 0.5) ** 2) if bend else 0.0
            row.append(bm.verts.new((x, y, z)))
        verts.append(row)
    uv = bm.loops.layers.uv.new("UVMap")
    for j in range(ny):
        for i in range(nx):
            f = bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
            for loop, (a, b) in zip(f.loops, ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1))):
                loop[uv].uv = (a / nx, b / ny)
    ob = mesh_object(name, bm, mat, parent, pos, rot)
    return ob


def cushion(name, size, puff=0.35, pos=(0, 0, 0), mat=None, parent=None, rot=(0, 0, 0), cuts=5, levels=2):
    """Puffy pillow: subdivided box inflated along Z, smoothed by subsurf."""
    sx, sy, sz = size
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    for v in bm.verts:
        u, w = v.co.x * 2, v.co.y * 2
        k = 1 + puff * (1 - u * u) * (1 - w * w)
        v.co.x *= sx
        v.co.y *= sy
        v.co.z *= sz * k
    ob = mesh_object(name, bm, mat, parent, pos, rot)
    subsurf(ob, levels)
    return smooth(ob, 180)


def cable(name, pts, radius, mat, parent=None, pos=(0, 0, 0), rot=(0, 0, 0), res=8, bevel_res=2):
    cu = bpy.data.curves.new(name + "_curve", 'CURVE')
    cu.dimensions = '3D'
    sp = cu.splines.new('BEZIER')
    sp.bezier_points.add(len(pts) - 1)
    for p, co in zip(sp.bezier_points, pts):
        p.co = co
        p.handle_left_type = p.handle_right_type = 'AUTO'
    cu.resolution_u = res
    cu.bevel_depth = radius
    cu.bevel_resolution = bevel_res
    cu.use_fill_caps = True
    tmp = bpy.data.objects.new(name + "_tmp", cu)
    bpy.context.scene.collection.objects.link(tmp)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg))
    bpy.data.objects.remove(tmp)
    bpy.data.curves.remove(cu)
    me.name = name
    me.materials.clear()
    me.materials.append(M(mat))
    ob = bpy.data.objects.new(name, me)
    link(ob, parent, pos, rot)
    return smooth(ob, 180)


def join(name, objs):
    """Merge meshes (after applying their modifiers) into the first object's space."""
    dg = bpy.context.evaluated_depsgraph_get()
    base = objs[0]
    bm = bmesh.new()
    mats = []
    for ob in objs:
        me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
        local = base.matrix_world.inverted() @ ob.matrix_world
        me.transform(local)
        offset = len(mats)
        for m in me.materials:
            mats.append(m)
        for p in me.polygons:
            p.material_index += offset
        bm.from_mesh(me)
        bpy.data.meshes.remove(me)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    uniq = []
    remap = []
    for m in mats:
        if m not in uniq:
            uniq.append(m)
        remap.append(uniq.index(m))
    for p in me.polygons:
        p.material_index = remap[p.material_index]
    for m in uniq:
        me.materials.append(m)
    parent, mw = base.parent, base.matrix_world.copy()
    angle = base.get("smooth_angle", 40)
    for ob in objs:
        bpy.data.objects.remove(ob)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.parent = parent
    ob.matrix_world = mw
    ob["smooth_angle"] = angle
    return ob


def mirror_x(ob):
    mod = ob.modifiers.new("Mirror", 'MIRROR')
    mod.use_axis[0] = True
    return ob


def array(ob, count, offset):
    mod = ob.modifiers.new("Array", 'ARRAY')
    mod.count = count
    mod.use_relative_offset = False
    mod.use_constant_offset = True
    mod.constant_offset_displace = offset
    return ob


# ---------------------------------------------------------------- finalize, UVs, export

def finalize_all():
    dg = bpy.context.evaluated_depsgraph_get()
    for ob in [o for o in bpy.data.objects if o.type == 'MESH']:
        if ob.modifiers:
            me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
            old = ob.data
            ob.modifiers.clear()
            ob.data = me
            if old.users == 0:
                bpy.data.meshes.remove(old)
            me.name = ob.name
        me = ob.data
        angle = ob.get("smooth_angle", 40)
        me.shade_smooth()
        if angle < 180:
            me.set_sharp_from_angle(angle=math.radians(angle))
    for ob, mode, kw in UV_JOBS:
        apply_uv(ob, mode, **kw)


def apply_uv(ob, mode, scale=1.0, rect=(0.0, 0.0, 1.0, 1.0), axis="Z", flip_u=False, flip_v=False, rotate=False,
             world=False, **_):
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    if mode == "planar_local":
        u0, v0, u1, v1 = rect
        for f in bm.faces:
            for loop in f.loops:
                s, t = loop[uv].uv
                if flip_u:
                    s = 1 - s
                loop[uv].uv = (u0 + (u1 - u0) * s, v0 + (v1 - v0) * t)
        bm.to_mesh(me)
        bm.free()
        return
    mw = ob.matrix_world if world else Matrix.Identity(4)
    pts = [mw @ v.co for v in bm.verts]
    if not pts:
        bm.free()
        return
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    ext = hi - lo
    u0, v0, u1, v1 = rect
    for f in bm.faces:
        n = f.normal
        for loop in f.loops:
            co = mw @ loop.vert.co
            if mode == "box":
                ax = max(range(3), key=lambda i: abs(n[i]))
                if ax == 0:
                    u, v = co.y * (1 if n.x > 0 else -1), co.z
                elif ax == 1:
                    u, v = -co.x * (1 if n.y > 0 else -1), co.z
                else:
                    u, v = co.x, co.y * (1 if n.z > 0 else -1)
                u, v = u * scale, v * scale
            elif mode == "planar":
                a, b = {"Z": (0, 1), "Y": (0, 2), "X": (1, 2)}[axis]
                s = (co[a] - lo[a]) / max(ext[a], 1e-6)
                t = (co[b] - lo[b]) / max(ext[b], 1e-6)
                if rotate:
                    s, t = t, 1 - s
                if flip_u:
                    s = 1 - s
                if flip_v:
                    t = 1 - t
                u, v = u0 + (u1 - u0) * s, v0 + (v1 - v0) * t
            elif mode == "cyl":
                ang = math.atan2(co.y, co.x)
                s = (ang / (2 * math.pi)) % 1.0
                t = (co.z - lo.z) / max(ext.z, 1e-6)
                u, v = u0 + (u1 - u0) * s, v0 + (v1 - v0) * t
            else:
                continue
            loop[uv].uv = (u, v)
    bm.to_mesh(me)
    bm.free()


def export_fbx(path, root):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in bpy.data.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = root
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'},
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z',
                             axis_up='Y', bake_space_transform=False, mesh_smooth_type='OFF', use_mesh_modifiers=True,
                             add_leaf_bones=False, path_mode='STRIP', use_custom_props=False)
