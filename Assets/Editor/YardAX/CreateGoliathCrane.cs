// Tools > YardAX > Create Goliath Crane (G-1)
// 씬·프리팹을 직접 수정하지 않는 규칙에 따라, 계층 생성은 전부 이 메뉴로.
// 멱등: 같은 이름의 루트가 있으면 지우고 다시 만든다.
// 외형은 단순 형상(노란 도장, 거더 측면 트러스는 메시·재질 공유로 인스턴싱).
// 피벗 이름·위치는 blender/goliath_crane.py 와 동일 — 나중에 FBX 메시만 교체.

using UnityEditor;
using UnityEngine;
using YardAX.Config;
using YardAX.Goliath;

namespace YardAX.EditorTools
{
    public static class CreateGoliathCrane
    {
        const string RootName = "GoliathRoot";
        const string GenDir = "Assets/YardAX/Generated";

        [MenuItem("Tools/YardAX/Create Goliath Crane")]
        public static void Create()
        {
            var config = FindOrCreateConfig();
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var yellow = Mat("GoliathYellow", new Color(0.91f, 0.65f, 0.10f));
            var steel = Mat("GoliathSteel", new Color(0.35f, 0.37f, 0.40f));
            var dark = Mat("GoliathDark", new Color(0.16f, 0.17f, 0.18f));
            // 트러스 공유 메시(인스턴싱): 큐브 메시 + 공유 재질
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            float span = config.spanM, h = config.heightM;
            float gd = config.girderDepthM, gh = config.girderHeightM;

            var root = new GameObject(RootName);
            root.transform.SetPositionAndRotation(
                config.railOriginWorld, Quaternion.Euler(0, config.railYawDeg, 0));

            // ── Legs: 양쪽 다리 + 주행 바퀴 ──
            var legs = Child(root.transform, "Legs", Vector3.zero);
            foreach (var sx in new[] { -span / 2f, span / 2f })
            {
                string side = sx < 0 ? "L" : "R";
                foreach (var fz in new[] { -gd * 0.35f, gd * 0.35f })
                    Box(legs, $"Leg{side}_{(fz < 0 ? "F" : "B")}", yellow,
                        new Vector3(sx, h / 2f, fz), new Vector3(4f, h, 4f));
                Box(legs, $"Bogie{side}", steel,
                    new Vector3(sx, 1.0f, 0), new Vector3(6.5f, 2f, 14f));
                for (int i = 0; i < 4; i++)
                {
                    float wz = -5.25f + i * 3.5f;
                    var w = Cyl(legs, $"Wheel{side}_{i}", dark,
                        new Vector3(sx, 1.4f, wz), 1.4f, 5f);
                    w.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
            }

            // ── Girder: 박스 거더 + 측면 트러스(공유 메시·재질 = 인스턴싱) ──
            var girder = Child(root.transform, "Girder", new Vector3(0, h, 0));
            Box(girder, "GirderBox", yellow,
                new Vector3(0, gh / 2f, 0), new Vector3(span + 8f, gh, gd));
            int n = Mathf.RoundToInt(span / 6f);
            for (int i = 0; i <= n; i++)
            {
                float x = -span / 2f + i * 6f;
                float tilt = (i % 2 == 0) ? 42f : -42f;
                foreach (var zz in new[] { -gd / 2f - 0.25f, gd / 2f + 0.25f })
                {
                    var m = new GameObject($"Truss_{(zz < 0 ? "F" : "B")}_{i}");
                    m.transform.SetParent(girder, false);
                    m.transform.localPosition = new Vector3(x, gh / 2f, zz);
                    m.transform.localRotation = Quaternion.Euler(0, 0, tilt);
                    m.transform.localScale = new Vector3(0.35f, gh * 1.15f, 0.35f);
                    m.AddComponent<MeshFilter>().sharedMesh = cube;
                    var mr = m.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = dark;
                }
            }
            Box(girder, "MachineRoom", steel,
                new Vector3(span * 0.32f, gh + 2.25f, 0), new Vector3(10f, 4.5f, 7f));

            // ── 트롤리 3기 + 후크 (피벗 규약) ──
            MakeTrolley(root.transform, "UpperTrolleyA", -span * 0.18f, h + gh, true, steel, yellow, config);
            MakeTrolley(root.transform, "UpperTrolleyB", span * 0.18f, h + gh, true, steel, yellow, config);
            MakeTrolley(root.transform, "LowerTrolley", 0f, h, false, steel, yellow, config);

            // ── LoadBlock ──
            var lb = Child(root.transform, "LoadBlock", Vector3.zero);
            var s = config.loadBlockSize;
            Box(lb, "LoadBlock_Mesh", steel, new Vector3(0, s.y / 2f, 0), s);

            var rig = root.AddComponent<GoliathRig>();
            rig.config = config;
            rig.Resolve();

            Undo.RegisterCreatedObjectUndo(root, "Create Goliath Crane");
            Selection.activeGameObject = root;
            Debug.Log($"[YardAX] 골리앗 생성 완료 — span {span}m, height {h}m. " +
                      "1도크 배치는 config.railOriginWorld/railYawDeg 로 조정.");
        }

        static void MakeTrolley(Transform root, string name, float x, float y,
            bool upper, Material steel, Material yellow, GoliathCraneConfig cfg)
        {
            var t = Child(root, name, new Vector3(x, y, 0));
            float bodyY = upper ? 1.75f : -1.75f;
            Box(t, name + "_Body", steel, new Vector3(0, bodyY, 0), new Vector3(7f, 3.5f, 6f));
            string hook = name == "UpperTrolleyA" ? "HookA"
                        : name == "UpperTrolleyB" ? "HookB" : "HookL";
            float drop = upper ? cfg.heightM * 0.5f : cfg.heightM * 0.35f;
            var hk = Child(t, hook, new Vector3(0, -drop, 0));
            Box(hk, hook + "_Block", yellow, new Vector3(0, -0.8f, 0), new Vector3(2.4f, 1.6f, 1.8f));
            // 와이어: LineRenderer (트롤리 → 후크)
            var lr = hk.gameObject.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPositions(new[] { Vector3.zero, new Vector3(0, drop, 0) });
            lr.startWidth = lr.endWidth = 0.12f;
            lr.sharedMaterial = Mat("GoliathCable", new Color(0.1f, 0.1f, 0.12f));
        }

        static Transform Child(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        static GameObject Box(Transform parent, string name, Material mat, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject Cyl(Transform parent, string name, Material mat, Vector3 pos, float r, float len)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(r * 2f, len / 2f, r * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Material Mat(string name, Color c)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/YardAX"))
                    AssetDatabase.CreateFolder("Assets", "YardAX");
                if (!AssetDatabase.IsValidFolder(GenDir))
                    AssetDatabase.CreateFolder("Assets/YardAX", "Generated");
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader) { color = c };
                m.enableInstancing = true;   // 트러스 부재 GPU 인스턴싱
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        static GoliathCraneConfig FindOrCreateConfig()
        {
            var guids = AssetDatabase.FindAssets("t:GoliathCraneConfig");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<GoliathCraneConfig>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            if (!AssetDatabase.IsValidFolder("Assets/YardAX"))
                AssetDatabase.CreateFolder("Assets", "YardAX");
            var cfg = ScriptableObject.CreateInstance<GoliathCraneConfig>();
            AssetDatabase.CreateAsset(cfg, "Assets/YardAX/GoliathCraneConfig.asset");
            Debug.Log("[YardAX] GoliathCraneConfig.asset 생성 — 수치를 여기서 조정하세요.");
            return cfg;
        }
    }
}
