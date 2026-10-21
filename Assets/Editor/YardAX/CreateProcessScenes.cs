// Tools > YardAX > Create Process Scenes (W-4)
// 무거운 모델 대신 단순 형상 + 파티클·애니메이션. 멱등(루트 재생성).
// 탑재(1도크)는 기존 골리앗을 쓰므로 마커만 둔다.

using UnityEditor;
using UnityEngine;
using YardAX.Schedule;

namespace YardAX.EditorTools
{
    public static class CreateProcessScenes
    {
        const string RootName = "ProcessScenes";

        [MenuItem("Tools/YardAX/Create Process Scenes")]
        public static void Create()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName);

            Cutting(Scene(root, "cut", 11, new Vector3(120, 0, -60)));
            SubAssembly(Scene(root, "sub", 6, new Vector3(160, 0, -60)));
            Assembly(Scene(root, "asm", 12, new Vector3(200, 0, -60)));
            Outfitting(Scene(root, "outfit", 5, new Vector3(240, 0, -60)));
            Paint(Scene(root, "paint", 22, new Vector3(280, 0, -60)));
            Quay(Scene(root, "quay", 9, new Vector3(320, 0, -60)));
            Erect(Scene(root, "erect", 1, Vector3.zero)); // 골리앗 위치 — 마커만

            Undo.RegisterCreatedObjectUndo(root, "Create Process Scenes");
            Debug.Log("[YardAX] 공정 장면 7종 생성 — 작업 액티브에서 자동 활성화.");
        }

        static Transform Scene(GameObject root, string key, int facility, Vector3 pos)
        {
            var go = new GameObject($"Scene_{key}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ProcessScene>();
            ps.processKey = key; ps.facilityId = facility;
            go.SetActive(false);
            return go.transform;
        }

        static GameObject Box(Transform p, string n, Vector3 pos, Vector3 size, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            go.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = c };
            return go;
        }

        static ParticleSystem Sparks(Transform p, string n, Vector3 pos, Color c, float rate = 40f)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = c; main.startSize = 0.08f; main.startSpeed = 3.5f;
            main.startLifetime = 0.5f; main.gravityModifier = 1.2f;
            var em = ps.emission; em.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f;
            go.GetComponent<ParticleSystemRenderer>().material =
                new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                             ?? Shader.Find("Particles/Standard Unlit"));
            return ps;
        }

        static GameObject Quad(Transform p, string n, Vector3 pos, Vector2 size, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh) { color = c }; m.renderQueue = 3002;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static void Cutting(Transform s) // 절단(11): 불꽃 + 마그넷 크레인 강판 이송 + 경로
        {
            Box(s, "CutTable", new Vector3(0, 0.5f, 0), new Vector3(12, 1, 6), new Color(0.3f, 0.33f, 0.38f));
            Sparks(s, "CutSparks", new Vector3(0, 1.2f, 0), new Color(1f, 0.7f, 0.2f), 80f);
            var crane = Box(s, "MagnetCrane", new Vector3(0, 6, -4), new Vector3(10, 0.8f, 1.2f), new Color(0.9f, 0.65f, 0.1f));
            var plate = Box(s, "SteelPlate", new Vector3(0, 5f, -4), new Vector3(4, 0.15f, 2.5f), new Color(0.45f, 0.48f, 0.52f));
            plate.transform.SetParent(crane.transform, true);
            var anim = crane.AddComponent<SimpleShuttle>(); anim.axis = Vector3.right; anim.amplitude = 6f; anim.period = 9f;
            Quad(s, "TransferPath", new Vector3(0, 0.05f, -8), new Vector2(16, 3), new Color(0.3f, 0.75f, 1f, 0.25f));
        }

        static void SubAssembly(Transform s) // 소조립(6): 부재 맞춤·용접 불꽃 + 통로 적치물
        {
            Box(s, "Bench", new Vector3(0, 0.45f, 0), new Vector3(8, 0.9f, 4), new Color(0.35f, 0.4f, 0.45f));
            Box(s, "PartA", new Vector3(-1, 1.2f, 0), new Vector3(3, 0.5f, 2), new Color(0.5f, 0.55f, 0.6f));
            Sparks(s, "WeldSparks", new Vector3(0.6f, 1.3f, 0), new Color(0.6f, 0.8f, 1f), 60f);
            for (int i = 0; i < 4; i++)
                Box(s, $"AisleClutter{i}", new Vector3(5.5f, 0.4f, -3 + i * 2f),
                    new Vector3(1.2f, 0.8f, 1.2f), new Color(0.55f, 0.45f, 0.3f));
            Quad(s, "AisleWarn", new Vector3(5.5f, 0.05f, 0), new Vector2(2.5f, 9f), new Color(1f, 0.75f, 0.2f, 0.3f));
        }

        static void Assembly(Transform s) // 조립(12): 천장크레인 2기 반전 + 포락선 (턴오버 로직 재사용 표시)
        {
            Box(s, "OHCrane1", new Vector3(-4, 9, 0), new Vector3(1.2f, 0.8f, 10), new Color(0.9f, 0.65f, 0.1f));
            Box(s, "OHCrane2", new Vector3(4, 9, 0), new Vector3(1.2f, 0.8f, 10), new Color(0.9f, 0.65f, 0.1f));
            var block = Box(s, "TurnBlock", new Vector3(0, 4, 0), new Vector3(7, 1.2f, 5), new Color(0.5f, 0.56f, 0.62f));
            var rot = block.AddComponent<SimpleRotate>(); rot.axis = Vector3.right; rot.degPerSec = 8f;
            float r = YardAX.Goliath.TurnoverMath.EnvelopeRadius(5f, 1.2f);
            Quad(s, "Envelope", new Vector3(0, 0.05f, 0), new Vector2(r * 2, r * 2), new Color(1f, 0.35f, 0.2f, 0.22f));
        }

        static void Outfitting(Transform s) // 선행의장(5): 고소작업대 배관 + 하부 낙하 위험
        {
            Box(s, "LiftPlatform", new Vector3(0, 5, 0), new Vector3(3, 0.3f, 2), new Color(0.9f, 0.65f, 0.1f));
            Box(s, "Pipe", new Vector3(0, 5.6f, 0), new Vector3(6, 0.3f, 0.3f), new Color(0.7f, 0.72f, 0.75f));
            Quad(s, "DropZone", new Vector3(0, 0.05f, 0), new Vector2(8, 6), new Color(1f, 0.3f, 0.2f, 0.25f));
        }

        static void Paint(Transform s) // 선행도장(22): 탱크·송풍 덕트·가스 수치·인접 화기
        {
            Box(s, "Tank", new Vector3(0, 3, 0), new Vector3(8, 6, 6), new Color(0.35f, 0.5f, 0.65f));
            Box(s, "VentDuct", new Vector3(5, 1, 0), new Vector3(4, 0.8f, 0.8f), new Color(0.75f, 0.75f, 0.2f));
            var label = new GameObject("GasLabel");
            label.transform.SetParent(s, false);
            label.transform.localPosition = new Vector3(0, 7, 0);
            var tm = label.AddComponent<TextMesh>();
            tm.text = "O₂ 20.9% · 측정 후 00:12 경과 (시뮬레이션 값)";
            tm.characterSize = 0.35f; tm.fontSize = 48; tm.anchor = TextAnchor.MiddleCenter;
            Sparks(s, "AdjacentHotwork", new Vector3(8, 1, 3), new Color(1f, 0.6f, 0.2f), 50f);
            label.AddComponent<GasLabelTicker>();
        }

        static void Quay(Transform s) // 안벽의장(9): ZONE 작업 박스 + 상하 동시작업 경고
        {
            Box(s, "ShipHull", new Vector3(0, 3, 0), new Vector3(30, 6, 8), new Color(0.6f, 0.25f, 0.2f));
            for (int i = 0; i < 4; i++)
                Box(s, $"Zone{i + 1}", new Vector3(-10 + i * 7, 6.6f, 0), new Vector3(5, 1, 7.5f),
                    new Color(0.3f, 0.75f, 1f, 1f));
            Quad(s, "VerticalWarn", new Vector3(-3, 0.05f, 5), new Vector2(6, 3), new Color(1f, 0.75f, 0.2f, 0.35f));
        }

        static void Erect(Transform s) // 탑재(1도크): 골리앗·Swept 재사용 — 하부 작업자만 추가
        {
            var worker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            worker.name = "ErectWorker";
            worker.transform.SetParent(s, false);
            worker.transform.localPosition = new Vector3(8, 1, 12);
            worker.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            worker.GetComponent<MeshRenderer>().sharedMaterial =
                new Material(sh) { color = new Color(1f, 0.85f, 0.3f) };
            worker.AddComponent<RegisterWorkerOnEnable>();
        }
    }
}
