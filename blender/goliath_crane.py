"""
골리앗 크레인 모델링 스크립트 (Blender 3.x/4.x)

Unity G-1 리그(GoliathRoot 계층)와 **오브젝트 이름·피벗 위치가 동일**하도록 생성한다.
Unity에서는 이 FBX의 메시만 교체하고, 움직이는 축(주행/트롤리/후크)은
같은 이름의 피벗을 그대로 구동한다.

계층·피벗 규약 (단위: 미터, +X = 거더 방향(경간), +Y = 위, -Z = 레일 전진 방향):
  GoliathRoot      피벗 = 레일 중앙·지면 (주행축)
  ├─ Legs          피벗 = GoliathRoot와 동일 (고정부)
  ├─ Girder        피벗 = 거더 중앙·거더 밑면 높이
  ├─ UpperTrolleyA 피벗 = 거더 윗면·트롤리 중심 (횡행축, 로컬 X 이동)
  ├─ UpperTrolleyB 〃
  ├─ LowerTrolley  피벗 = 거더 밑면·트롤리 중심 (횡행축)
  ├─ HookA/B/L     피벗 = 후크 블록 상단 (권상축, 로컬 Y 이동; 와이어는 Unity LineRenderer)
  └─ LoadBlock     피벗 = 블록 바닥 중심

실행:
  blender -b -P blender/goliath_crane.py                    # .blend 저장 없이 생성만
  blender -b -P blender/goliath_crane.py -- --export out/goliath_crane.fbx
"""

import sys

import bpy
from mathutils import Vector

# ── 치수 설정 (미터) ─────────────────────────────────────
SPAN = 120.0          # 경간 (다리 사이)
LEG_H = 70.0          # 거더 밑면까지 높이
GIRDER_W = 8.0        # 거더 폭(레일 방향)
GIRDER_H = 9.0        # 거더 높이
LEG_W = 4.0           # 다리 단면
WHEEL_R = 1.4
BOGIE_LEN = 14.0      # 주행 대차 길이
TROLLEY_SIZE = (7.0, 3.5, 6.0)
HOOK_SIZE = (2.4, 1.6, 1.8)
LOAD_BLOCK = (22.0, 6.0, 14.0)   # 기본 블록 크기 (Unity에서 스케일 조정 가능)
TRUSS_PITCH = 6.0     # 거더 측면 트러스 간격
MACHINE_ROOM = (10.0, 4.5, 7.0)
UPPER_A_X = -SPAN * 0.18
UPPER_B_X = SPAN * 0.18
LOWER_X = 0.0

YELLOW = (0.92, 0.65, 0.10, 1.0)
STEEL = (0.35, 0.37, 0.40, 1.0)
DARK = (0.16, 0.17, 0.18, 1.0)


def clean_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def material(name, rgba):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = 0.55
        bsdf.inputs["Metallic"].default_value = 0.6
    return mat


def empty(name, loc, parent=None):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_size = 2.0
    obj.location = loc
    bpy.context.collection.objects.link(obj)
    if parent:
        obj.parent = parent
    return obj


def box(name, size, loc, parent, mat):
    """size/loc 는 부모 로컬 기준. 피벗은 상자 중심."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size[0], size[2], size[1])  # (X, Y(Blender=깊이), Z(높이)) ← 입력은 (x,h,z)
    bpy.ops.object.transform_apply(scale=True)
    obj.parent = parent
    obj.location = loc
    obj.data.materials.append(mat)
    return obj


def cylinder(name, r, depth, loc, parent, mat, axis="X"):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = name
    if axis == "X":
        obj.rotation_euler = (0, 1.5708, 0)
    bpy.ops.object.transform_apply(rotation=True)
    obj.parent = parent
    obj.location = loc
    obj.data.materials.append(mat)
    return obj


def linked_copy(src, name, loc, parent):
    """메시 공유 복제 — 트러스 부재 인스턴싱."""
    obj = bpy.data.objects.new(name, src.data)
    obj.location = loc
    obj.rotation_euler = src.rotation_euler.copy()
    obj.scale = src.scale.copy()
    obj.parent = parent
    bpy.context.collection.objects.link(obj)
    return obj


def build():
    clean_scene()
    yellow = material("GoliathYellow", YELLOW)
    steel = material("GoliathSteel", STEEL)
    dark = material("GoliathDark", DARK)

    # Blender 좌표: X=경간, Y=레일 방향, Z=위. (FBX 내보내기에서 Unity 축으로 변환)
    root = empty("GoliathRoot", (0, 0, 0))

    # ── Legs: 양쪽 A형 다리 + 주행 대차·바퀴 ──
    legs = empty("Legs", (0, 0, 0), root)
    for side, sx in (("L", -SPAN / 2), ("R", SPAN / 2)):
        for fy in (-GIRDER_W * 0.35, GIRDER_W * 0.35):
            leg = box(f"Leg{side}_{'F' if fy < 0 else 'B'}",
                      (LEG_W, LEG_H, LEG_W), (sx, 0, 0), legs, yellow)
            # A형: 위로 갈수록 안쪽으로 — 간단히 기울임
            leg.location = Vector((sx + (8 if sx < 0 else -8) * 0.0, fy, LEG_H / 2))
            leg.rotation_euler = (0.06 if fy < 0 else -0.06, 0, 0)
        bogie = box(f"Bogie{side}", (LEG_W * 1.6, 2.0, BOGIE_LEN),
                    (sx, 0, 1.0), legs, steel)
        for i, wy in enumerate((-BOGIE_LEN / 2 + 2, -BOGIE_LEN / 6, BOGIE_LEN / 6, BOGIE_LEN / 2 - 2)):
            cylinder(f"Wheel{side}_{i}", WHEEL_R, LEG_W * 1.2,
                     (sx, wy, WHEEL_R), legs, dark)

    # ── Girder: 박스 거더 + 측면 트러스(인스턴싱) + 기계실 + 사다리 ──
    girder = empty("Girder", (0, 0, LEG_H), root)
    box("GirderBox", (SPAN + LEG_W * 2, GIRDER_H, GIRDER_W),
        (0, 0, GIRDER_H / 2), girder, yellow)
    # 트러스 원본 부재 1개 → 좌우면·대각 교차 인스턴스
    bpy.ops.mesh.primitive_cube_add(size=1)
    truss_src = bpy.context.active_object
    truss_src.name = "TrussMember_src"
    truss_src.scale = (0.35, 0.35, GIRDER_H * 1.15)
    truss_src.rotation_euler = (0.9, 0, 0)
    bpy.ops.object.transform_apply(scale=True)
    truss_src.parent = girder
    truss_src.data.materials.append(dark)
    truss_src.location = (-SPAN / 2, -GIRDER_W / 2 - 0.2, GIRDER_H / 2)
    n = int(SPAN / TRUSS_PITCH)
    for i in range(1, n + 1):
        x = -SPAN / 2 + i * TRUSS_PITCH
        flip = 1 if i % 2 == 0 else -1
        for side, yy in (("F", -GIRDER_W / 2 - 0.2), ("B", GIRDER_W / 2 + 0.2)):
            m = linked_copy(truss_src, f"Truss{side}_{i}", (x, yy, GIRDER_H / 2), girder)
            m.rotation_euler = (0.9 * flip, 0, 0)
    box("MachineRoom", MACHINE_ROOM, (SPAN * 0.32, GIRDER_H + MACHINE_ROOM[1] / 2, 0),
        girder, steel)
    # 사다리: 다리 하나를 따라 가로대 인스턴싱
    bpy.ops.mesh.primitive_cylinder_add(radius=0.06, depth=0.9)
    rung_src = bpy.context.active_object
    rung_src.name = "LadderRung_src"
    rung_src.rotation_euler = (0, 1.5708, 0)
    bpy.ops.object.transform_apply(rotation=True)
    rung_src.parent = root
    rung_src.data.materials.append(dark)
    rung_src.location = (-SPAN / 2 + LEG_W, 0, 1.0)
    for i in range(1, int(LEG_H / 0.4)):
        linked_copy(rung_src, f"LadderRung_{i}",
                    (-SPAN / 2 + LEG_W, 0, 1.0 + i * 0.4), root)

    # ── 트롤리 3기 (피벗 = 거더 기준면·중심) ──
    def trolley(name, x, upper=True):
        z = LEG_H + GIRDER_H if upper else LEG_H
        t = empty(name, (x, 0, z), root)
        dz = TROLLEY_SIZE[1] / 2 if upper else -TROLLEY_SIZE[1] / 2
        box(f"{name}_Body", TROLLEY_SIZE, (0, 0, dz), t, steel)
        return t

    ta = trolley("UpperTrolleyA", UPPER_A_X, upper=True)
    tb = trolley("UpperTrolleyB", UPPER_B_X, upper=True)
    tl = trolley("LowerTrolley", LOWER_X, upper=False)

    # ── 후크 (피벗 = 블록 상단; 와이어는 Unity LineRenderer) ──
    def hook(name, parent, drop):
        h = empty(name, (0, 0, -drop), parent)
        box(f"{name}_Block", HOOK_SIZE, (0, 0, -HOOK_SIZE[1] / 2), h, yellow)
        return h

    hook("HookA", ta, LEG_H * 0.45)
    hook("HookB", tb, LEG_H * 0.45)
    hook("HookL", tl, LEG_H * 0.3)

    # ── LoadBlock (피벗 = 바닥 중심) ──
    lb = empty("LoadBlock", (UPPER_A_X + (UPPER_B_X - UPPER_A_X) / 2, 0, 0), root)
    box("LoadBlock_Mesh", LOAD_BLOCK, (0, 0, LOAD_BLOCK[1] / 2), lb, steel)

    return root


def export_fbx(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",  # Unity에서 Scale Factor 1
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,            # 루트 -89.98° 회전 방지
        object_types={"EMPTY", "MESH"},
        add_leaf_bones=False,
        mesh_smooth_type="FACE",
    )
    print(f"[goliath_crane] exported: {path}")


if __name__ == "__main__":
    build()
    argv = sys.argv
    if "--" in argv:
        args = argv[argv.index("--") + 1:]
        if "--export" in args:
            import os
            out = args[args.index("--export") + 1]
            os.makedirs(os.path.dirname(out) or ".", exist_ok=True)
            export_fbx(out)
