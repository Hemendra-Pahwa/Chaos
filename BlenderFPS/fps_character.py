"""
FPS Arms — v6  REALISTIC
CSGO-style tactical forearms: fabric sleeve, leather glove, exposed skin, M4 rifle.

HOW TO USE:
  1. Blender  →  Scripting tab  →  Open  →  select this file
  2. Press  Alt+P  to run
  3. Press  Numpad-0  (camera view)  then  Z → Rendered shading
  4. F12 for a full render to Image Editor
"""

import bpy, bmesh, math
from mathutils import Vector

R = math.radians

# ─────────────────────────────────────────────────────────────────────────────
#  CLEAR SCENE
# ─────────────────────────────────────────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for d in [bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights]:
    for b in list(d):
        d.remove(b)

sc = bpy.context.scene

# ─────────────────────────────────────────────────────────────────────────────
#  WORLD  (dark grey background so the arms pop)
# ─────────────────────────────────────────────────────────────────────────────
if sc.world is None:
    sc.world = bpy.data.worlds.new("World")
w = sc.world
w.use_nodes = True
wn, wl = w.node_tree.nodes, w.node_tree.links
wn.clear()
wb = wn.new('ShaderNodeBackground')
wo = wn.new('ShaderNodeOutputWorld')
wb.inputs[0].default_value = (0.08, 0.08, 0.08, 1)
wb.inputs[1].default_value = 1.0
wl.new(wb.outputs[0], wo.inputs[0])

# ─────────────────────────────────────────────────────────────────────────────
#  MATERIAL FACTORY
# ─────────────────────────────────────────────────────────────────────────────
def _new_bsdf(name):
    """Create a material, clear nodes, return (mat, node_dict, link_fn, bsdf_node)."""
    m  = bpy.data.materials.new(name)
    m.use_nodes = True
    nd = m.node_tree.nodes
    lk = m.node_tree.links
    nd.clear()
    bsdf = nd.new('ShaderNodeBsdfPrincipled')
    out  = nd.new('ShaderNodeOutputMaterial')
    lk.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return m, nd, lk, bsdf


def mat_sleeve():
    """Blue-grey military sleeve with diagonal fabric-weave bump."""
    m, nd, lk, bsdf = _new_bsdf("Sleeve")
    wave  = nd.new('ShaderNodeTexWave')
    cramp = nd.new('ShaderNodeValToRGB')
    bump  = nd.new('ShaderNodeBump')

    wave.wave_type        = 'BANDS'
    wave.bands_direction  = 'DIAGONAL'
    wave.inputs['Scale'].default_value        = 80
    wave.inputs['Distortion'].default_value   = 3.5
    wave.inputs['Detail'].default_value       = 10
    wave.inputs['Detail Scale'].default_value = 5

    # Dark  →  light steel-blue (military BDU style)
    cramp.color_ramp.elements[0].position = 0.30
    cramp.color_ramp.elements[0].color    = (0.13, 0.19, 0.26, 1)
    cramp.color_ramp.elements[1].position = 0.80
    cramp.color_ramp.elements[1].color    = (0.24, 0.33, 0.42, 1)

    bump.inputs['Strength'].default_value  = 0.65
    bump.inputs['Distance'].default_value  = 0.003
    bsdf.inputs['Roughness'].default_value = 0.88

    lk.new(wave.outputs['Fac'],    cramp.inputs['Fac'])
    lk.new(wave.outputs['Fac'],    bump.inputs['Height'])
    lk.new(cramp.outputs['Color'], bsdf.inputs['Base Color'])
    lk.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    return m


def mat_glove():
    """Dark tactical glove — noise-grain leather with subtle rubber sheen."""
    m, nd, lk, bsdf = _new_bsdf("Glove")
    noise = nd.new('ShaderNodeTexNoise')
    cramp = nd.new('ShaderNodeValToRGB')
    bump  = nd.new('ShaderNodeBump')

    noise.inputs['Scale'].default_value      = 50
    noise.inputs['Detail'].default_value     = 8
    noise.inputs['Roughness'].default_value  = 0.65
    noise.inputs['Distortion'].default_value = 0.12

    cramp.color_ramp.elements[0].position = 0.35
    cramp.color_ramp.elements[0].color    = (0.04, 0.04, 0.05, 1)
    cramp.color_ramp.elements[1].position = 0.75
    cramp.color_ramp.elements[1].color    = (0.16, 0.16, 0.17, 1)

    bump.inputs['Strength'].default_value  = 0.7
    bump.inputs['Distance'].default_value  = 0.003
    bsdf.inputs['Roughness'].default_value = 0.42

    # specular — compatible with Blender 3.x and 4.x
    for inp_name in ('Specular IOR Level', 'Specular'):
        if inp_name in bsdf.inputs:
            bsdf.inputs[inp_name].default_value = 0.20
            break

    lk.new(noise.outputs['Fac'],   cramp.inputs['Fac'])
    lk.new(noise.outputs['Fac'],   bump.inputs['Height'])
    lk.new(cramp.outputs['Color'], bsdf.inputs['Base Color'])
    lk.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    return m


def mat_skin():
    """Warm skin with subtle subsurface scattering for the exposed wrist strip."""
    m, nd, lk, bsdf = _new_bsdf("Skin")
    bsdf.inputs['Base Color'].default_value = (0.79, 0.55, 0.40, 1)
    bsdf.inputs['Roughness'].default_value  = 0.55
    for inp in ('Subsurface Weight', 'Subsurface'):
        if inp in bsdf.inputs:
            bsdf.inputs[inp].default_value = 0.10
            break
    try:
        bsdf.inputs['Subsurface Color'].default_value = (0.96, 0.62, 0.49, 1)
    except Exception:
        pass
    return m


def mat_metal(name, rgb, rough=0.28, metal=0.92):
    m, nd, lk, bsdf = _new_bsdf(name)
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value  = rough
    bsdf.inputs['Metallic'].default_value   = metal
    return m


SLEEVE = mat_sleeve()
GLOVE  = mat_glove()
SKIN   = mat_skin()
GUNB   = mat_metal("GunBody", (0.15, 0.15, 0.16), 0.28, 0.92)
GUND   = mat_metal("GunDet",  (0.09, 0.09, 0.10), 0.42, 0.82)
WOOD   = mat_metal("Wood",    (0.30, 0.18, 0.06), 0.96, 0.00)

# ─────────────────────────────────────────────────────────────────────────────
#  MESH HELPERS
# ─────────────────────────────────────────────────────────────────────────────
def smooth_mesh(mesh):
    for p in mesh.polygons:
        p.use_smooth = True
    if hasattr(mesh, 'use_auto_smooth'):
        mesh.use_auto_smooth   = True
        mesh.auto_smooth_angle = R(60)


def add_tube(bm, pts, rx_list, ry_list=None, segs=16):
    """
    Append an elliptical tube to an existing bmesh.
    rx_list : horizontal radii at each waypoint
    ry_list : vertical radii (same as rx_list when None = circular tube)
    segs    : polygon loop count around circumference
    """
    pts = [Vector(p) for p in pts]
    if ry_list is None:
        ry_list = rx_list
    rings = []

    for i, (pt, rx, ry) in enumerate(zip(pts, rx_list, ry_list)):
        if   i == 0:              fwd = (pts[1]  - pts[0]).normalized()
        elif i == len(pts) - 1:   fwd = (pts[-1] - pts[-2]).normalized()
        else:                     fwd = (pts[i+1] - pts[i-1]).normalized()

        up_hint = Vector((0, 0, 1))
        if abs(fwd.dot(up_hint)) > 0.9:
            up_hint = Vector((1, 0, 0))
        right = fwd.cross(up_hint).normalized()
        up    = right.cross(fwd).normalized()

        rings.append([
            bm.verts.new(
                pt
                + right * math.cos(2 * math.pi * j / segs) * rx
                + up    * math.sin(2 * math.pi * j / segs) * ry
            )
            for j in range(segs)
        ])

    # Side faces
    for i in range(len(rings) - 1):
        r1, r2 = rings[i], rings[i + 1]
        for j in range(segs):
            bm.faces.new([r1[j], r1[(j+1) % segs], r2[(j+1) % segs], r2[j]])

    # End caps
    for ring, flip in [(rings[0], True), (rings[-1], False)]:
        cx = sum((v.co for v in ring), Vector((0, 0, 0))) / segs
        cv = bm.verts.new(cx)
        for j in range(segs):
            if flip:
                bm.faces.new([cv, ring[(j+1) % segs], ring[j]])
            else:
                bm.faces.new([cv, ring[j],            ring[(j+1) % segs]])


def build(name, specs, mat, sub=2):
    """
    Build a single mesh object from multiple tube specs.
    specs : list of  (pts, rx_list [, ry_list [, segs]])
    """
    mesh = bpy.data.meshes.new(name)
    bm   = bmesh.new()
    for s in specs:
        add_tube(bm, s[0], s[1],
                 s[2] if len(s) > 2 else None,
                 s[3] if len(s) > 3 else 16)
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    smooth_mesh(mesh)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    mod = obj.modifiers.new("Sub", 'SUBSURF')
    mod.levels        = sub
    mod.render_levels = sub
    return obj


# Primitive helpers for the gun
def cube(name, loc, sc_v, rot=(0, 0, 0), mat=None):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object
    o.name = name;  o.scale = sc_v;  o.rotation_euler = rot
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    smooth_mesh(o.data)
    if mat: o.data.materials.append(mat)
    return o


def seg(name, p1, p2, r, segs=8, mat=None):
    v1, v2 = Vector(p1), Vector(p2)
    bpy.ops.mesh.primitive_cylinder_add(
        radius=r, depth=(v2-v1).length, vertices=segs, location=(v1+v2)/2)
    o = bpy.context.active_object
    o.name = name
    o.rotation_euler = (v2-v1).normalized().to_track_quat('Z', 'Y').to_euler()
    bpy.ops.object.transform_apply(rotation=True)
    smooth_mesh(o.data)
    if mat: o.data.materials.append(mat)
    return o


def cyl(name, loc, r, depth, segs=8, rot=(0,0,0), mat=None):
    bpy.ops.mesh.primitive_cylinder_add(
        radius=r, depth=depth, vertices=segs, location=loc)
    o = bpy.context.active_object
    o.name = name;  o.rotation_euler = rot
    bpy.ops.object.transform_apply(rotation=True)
    smooth_mesh(o.data)
    if mat: o.data.materials.append(mat)
    return o


def join_objs(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    valid = [o for o in objs if o and o.name in bpy.data.objects]
    for o in valid: o.select_set(True)
    bpy.context.view_layer.objects.active = valid[0]
    if len(valid) > 1:
        bpy.ops.object.join()
    bpy.context.active_object.name = name
    return bpy.context.active_object


# ═════════════════════════════════════════════════════════════════════════════
#  ARM GEOMETRY
#
#  Camera:   pos=(0,-1,0)  rot=Rx(90°)  →  looks along world +Y
#  Lens:     18 mm  =  90° H-FOV
#
#  At distance D from camera the visible half-width = D (tan 45°).
#
#  Arms enter from the bottom corners of the screen.
#  All waypoints verified to be within frustum.
#
#  Forearm cross-section is ELLIPTICAL:
#    rx  =  horizontal radius  (side-to-side)  — wider
#    ry  =  depth    radius  (front-to-back)   — narrower
#  This is anatomically correct: human forearms are wider than they are deep.
# ═════════════════════════════════════════════════════════════════════════════

# ─────────────────────────────────────────────────────────────────────────────
#  RIGHT ARM  (dominant hand — grips pistol grip)
# ─────────────────────────────────────────────────────────────────────────────

# Sleeve: elbow (bottom-right) → wrist cuff
RS_pts = [
    ( 0.44,  0.06, -0.38),   # elbow — enters from corner
    ( 0.38,  0.14, -0.34),
    ( 0.30,  0.24, -0.30),
    ( 0.22,  0.33, -0.27),
    ( 0.14,  0.41, -0.25),
    ( 0.10,  0.47, -0.24),
    ( 0.09,  0.49, -0.24),   # cuff end
]
RS_rx = [0.055, 0.052, 0.049, 0.046, 0.042, 0.039, 0.037]
RS_ry = [0.042, 0.040, 0.037, 0.035, 0.032, 0.030, 0.028]

# Exposed skin: a short strip between sleeve cuff and glove
RSK_pts = [( 0.090, 0.490, -0.241), ( 0.088, 0.498, -0.241), ( 0.086, 0.504, -0.241)]
RSK_rx  = [0.033, 0.033, 0.033]
RSK_ry  = [0.027, 0.027, 0.027]

# Glove wrist + palm
RG_pts = [
    ( 0.085, 0.505, -0.242),
    ( 0.082, 0.514, -0.239),
    ( 0.079, 0.522, -0.236),
    ( 0.076, 0.530, -0.232),   # palm base
]
RG_rx = [0.036, 0.037, 0.038, 0.040]
RG_ry = [0.029, 0.029, 0.030, 0.030]

# Fingers — right hand grips the pistol grip (slight downward/inward curl)
R_THUMB  = ([(0.058,0.516,-0.225),(0.044,0.532,-0.216),(0.034,0.545,-0.208),(0.026,0.556,-0.202)],
            [0.015,0.013,0.011,0.005], [0.012,0.011,0.009,0.004], 10)
R_INDEX  = ([(0.068,0.532,-0.235),(0.066,0.552,-0.228),(0.064,0.566,-0.222),(0.063,0.576,-0.218)],
            [0.012,0.011,0.010,0.005], [0.010,0.009,0.008,0.004], 10)
R_MIDDLE = ([(0.080,0.533,-0.235),(0.078,0.555,-0.227),(0.076,0.569,-0.221),(0.075,0.579,-0.217)],
            [0.013,0.012,0.011,0.006], [0.011,0.010,0.009,0.005], 10)
R_RING   = ([(0.092,0.529,-0.233),(0.090,0.551,-0.227),(0.088,0.564,-0.222),(0.087,0.574,-0.218)],
            [0.011,0.010,0.009,0.004], [0.009,0.008,0.007,0.003], 10)
R_PINKY  = ([(0.103,0.521,-0.230),(0.101,0.541,-0.225),(0.099,0.553,-0.221),(0.098,0.562,-0.218)],
            [0.009,0.009,0.008,0.004], [0.007,0.007,0.006,0.003], 10)

rarm_sleeve = build("Arm_R_Sleeve",
    [(RS_pts, RS_rx, RS_ry, 18)], SLEEVE)

rarm_skin = build("Arm_R_Skin",
    [(RSK_pts, RSK_rx, RSK_ry, 14)], SKIN)

rarm_glove = build("Arm_R_Glove",
    [(RG_pts, RG_rx, RG_ry, 16),
     R_THUMB, R_INDEX, R_MIDDLE, R_RING, R_PINKY], GLOVE)

# ─────────────────────────────────────────────────────────────────────────────
#  LEFT ARM  (support hand — grips handguard from below)
# ─────────────────────────────────────────────────────────────────────────────

LS_pts = [
    (-0.42,  0.06, -0.34),   # elbow — enters from left corner
    (-0.36,  0.15, -0.30),
    (-0.28,  0.25, -0.27),
    (-0.20,  0.34, -0.24),
    (-0.13,  0.42, -0.22),
    (-0.08,  0.47, -0.21),
    (-0.06,  0.49, -0.21),   # cuff end
]
LS_rx = [0.055, 0.052, 0.049, 0.046, 0.042, 0.039, 0.037]
LS_ry = [0.042, 0.040, 0.037, 0.035, 0.032, 0.030, 0.028]

LSK_pts = [(-0.058, 0.492, -0.213), (-0.056, 0.499, -0.213), (-0.054, 0.505, -0.212)]
LSK_rx  = [0.033, 0.033, 0.033]
LSK_ry  = [0.027, 0.027, 0.027]

LG_pts = [
    (-0.052, 0.506, -0.212),
    (-0.048, 0.516, -0.210),
    (-0.044, 0.524, -0.208),
    (-0.040, 0.532, -0.205),
]
LG_rx = [0.036, 0.037, 0.038, 0.040]
LG_ry = [0.029, 0.029, 0.030, 0.030]

# Fingers — left hand grips handguard (fingers extend more forward along barrel)
L_THUMB  = ([(-0.016,0.520,-0.198),(-0.004,0.536,-0.190),(0.006,0.548,-0.183),(0.012,0.558,-0.177)],
            [0.015,0.013,0.011,0.005], [0.012,0.011,0.009,0.004], 10)
L_INDEX  = ([(-0.028,0.536,-0.208),(-0.026,0.558,-0.201),(-0.024,0.574,-0.195),(-0.023,0.585,-0.191)],
            [0.012,0.011,0.010,0.005], [0.010,0.009,0.008,0.004], 10)
L_MIDDLE = ([(-0.040,0.538,-0.208),(-0.038,0.561,-0.201),(-0.036,0.578,-0.195),(-0.035,0.589,-0.191)],
            [0.013,0.012,0.011,0.006], [0.011,0.010,0.009,0.005], 10)
L_RING   = ([(-0.052,0.533,-0.207),(-0.050,0.556,-0.201),(-0.048,0.572,-0.196),(-0.047,0.582,-0.192)],
            [0.011,0.010,0.009,0.004], [0.009,0.008,0.007,0.003], 10)
L_PINKY  = ([(-0.063,0.524,-0.206),(-0.061,0.545,-0.201),(-0.060,0.558,-0.197),(-0.059,0.566,-0.194)],
            [0.009,0.009,0.008,0.004], [0.007,0.007,0.006,0.003], 10)

larm_sleeve = build("Arm_L_Sleeve",
    [(LS_pts, LS_rx, LS_ry, 18)], SLEEVE)

larm_skin = build("Arm_L_Skin",
    [(LSK_pts, LSK_rx, LSK_ry, 14)], SKIN)

larm_glove = build("Arm_L_Glove",
    [(LG_pts, LG_rx, LG_ry, 16),
     L_THUMB, L_INDEX, L_MIDDLE, L_RING, L_PINKY], GLOVE)

# ═════════════════════════════════════════════════════════════════════════════
#  GUN  —  M4-style assault rifle
#  Barrel points along +Y.  Centred at GX=0.04, GZ=-0.190.
#  Right hand grips pistol grip  ≈  Y 0.38–0.50
#  Left  hand cups handguard     ≈  Y 0.54–0.64
# ═════════════════════════════════════════════════════════════════════════════
GX, GZ = 0.040, -0.190

# Receiver body
recv  = cube("Recv",    (GX, 0.462, GZ),        (0.024, 0.112, 0.022),         mat=GUNB)
urecv = cube("URecv",   (GX, 0.452, GZ+0.018),  (0.019, 0.104, 0.009),         mat=GUNB)
# Dust cover panel
dcov  = cube("DustCov", (GX+0.022, 0.450, GZ-0.003), (0.002, 0.070, 0.008),    mat=GUND)

# Barrel + gas tube
brl   = seg ("Brl",     (GX, 0.504, GZ), (GX, 0.775, GZ),  0.007, 8,           GUNB)
gastube = seg("Gas",    (GX, 0.530, GZ+0.010), (GX, 0.700, GZ+0.010), 0.004, 6, GUND)

# Handguard
hg    = cube("HG",      (GX, 0.600, GZ),        (0.018, 0.068, 0.016),         mat=GUND)
hg_lo = cube("HGLow",   (GX, 0.600, GZ-0.014),  (0.016, 0.066, 0.004),         mat=GUND)

# Charging handle
ch    = cube("CH",      (GX, 0.420, GZ+0.024),  (0.012, 0.018, 0.004),         mat=GUND)

# Magazine
mag   = cube("Mag",     (GX, 0.452, GZ-0.042),  (0.016, 0.028, 0.046),
             rot=(R(6), 0, 0), mat=GUND)

# Pistol grip
grip  = cube("Grip",    (GX, 0.392, GZ-0.048),  (0.016, 0.026, 0.048),
             rot=(R(17), 0, 0), mat=GUNB)

# Trigger guard + trigger
tgrd  = cube("TGuard",  (GX, 0.418, GZ-0.022),  (0.013, 0.030, 0.003),         mat=GUNB)
trig  = seg ("Trig",    (GX, 0.420, GZ-0.020), (GX, 0.410, GZ-0.034), 0.003, 6, GUND)

# Stock
stk   = cube("Stock",   (GX, 0.304, GZ),        (0.019, 0.055, 0.018),         mat=WOOD)
sbutt = cube("SButt",   (GX, 0.254, GZ-0.010),  (0.019, 0.015, 0.026),         mat=WOOD)
sbuff = cube("SBuff",   (GX, 0.305, GZ+0.015),  (0.019, 0.050, 0.003),         mat=GUND)

# Muzzle device
muzz  = cyl ("Muzz",    (GX, 0.777, GZ),         0.012, 0.026, 8,
             rot=(R(90), 0, 0), mat=GUND)
muzz2 = cyl ("Muzz2",   (GX, 0.792, GZ),         0.009, 0.006, 6,
             rot=(R(90), 0, 0), mat=GUND)

# Sights
fsight = cube("FSight", (GX, 0.740, GZ+0.012),  (0.003, 0.006, 0.008),         mat=GUND)
rsight = cube("RSight", (GX, 0.352, GZ+0.012),  (0.011, 0.005, 0.007),         mat=GUND)

gun = join_objs(
    [recv, urecv, dcov, brl, gastube, hg, hg_lo, ch,
     mag, grip, tgrd, trig, stk, sbutt, sbuff,
     muzz, muzz2, fsight, rsight],
    "Gun"
)

# ═════════════════════════════════════════════════════════════════════════════
#  CAMERA
#  Position  : (0, -1, 0)
#  Rotation  : Rx(90°)  →  looks along world +Y
#  Lens      : 18 mm  =  90° H-FOV  (standard FPS)
#  Arms are 1.05–1.78 units in front of camera — fully in frame.
# ═════════════════════════════════════════════════════════════════════════════
bpy.ops.object.camera_add(location=(0, -1, 0))
cam = bpy.context.active_object
cam.name = "FPS_Camera"
cam.rotation_euler        = (R(90), 0, 0)
cam.data.lens             = 18        # 90° H-FOV
cam.data.clip_start       = 0.005
cam.data.clip_end         = 100
sc.camera                 = cam

# ═════════════════════════════════════════════════════════════════════════════
#  LIGHTING
# ═════════════════════════════════════════════════════════════════════════════
# Key — SUN light guarantees illumination everywhere regardless of distance
bpy.ops.object.light_add(type='SUN', location=(2, -1, 3))
sun = bpy.context.active_object
sun.name = "KeySun"
sun.data.energy    = 5.0
sun.rotation_euler = (R(40), 0, R(25))

# Fill — warm area from camera-left
bpy.ops.object.light_add(type='AREA', location=(-1.5, -0.8, 0.5))
fill = bpy.context.active_object
fill.name = "Fill"
fill.data.energy = 300
fill.data.size   = 3.0
fill.data.color  = (1.0, 0.92, 0.80)   # warm fill
fill.rotation_euler = (R(50), 0, R(-35))

# Rim — cool blue backlight to separate arms from background
bpy.ops.object.light_add(type='AREA', location=(0, 1.2, 0.8))
rim = bpy.context.active_object
rim.name = "Rim"
rim.data.energy = 150
rim.data.size   = 2.0
rim.data.color  = (0.70, 0.85, 1.0)    # cool rim
rim.rotation_euler = (R(135), 0, 0)

# ═════════════════════════════════════════════════════════════════════════════
#  RENDER SETTINGS
# ═════════════════════════════════════════════════════════════════════════════
try:    sc.render.engine = 'BLENDER_EEVEE_NEXT'
except: sc.render.engine = 'BLENDER_EEVEE'

try:    sc.eevee.use_bloom = True
except AttributeError: pass
try:    sc.eevee.use_ssr   = True
except AttributeError: pass

sc.render.film_transparent = False    # solid dark background, not alpha
sc.render.resolution_x     = 1920
sc.render.resolution_y     = 1080

# ═════════════════════════════════════════════════════════════════════════════
#  DEBUG — print camera orientation so you can verify it's looking at the arms
# ═════════════════════════════════════════════════════════════════════════════
bpy.context.view_layer.update()
cam_fwd = -(cam.matrix_world.to_3x3() @ Vector((0, 0, 1)))
print()
print("=" * 56)
print("  FPS Arms v6  —  scene ready!")
print(f"  Camera pos    : {tuple(round(v,3) for v in cam.location)}")
print(f"  Camera forward: {tuple(round(v,3) for v in cam_fwd)}")
print(f"    ↑ should be approx (0, 1, 0)  =  looking along +Y")
print()
print("  TO VIEW:")
print("    Numpad-0   →  camera view")
print("    Z          →  Rendered shading")
print("    F12        →  full render")
print("=" * 56)
print()
