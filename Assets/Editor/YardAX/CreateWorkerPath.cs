// Tools > YardAX > Create Worker Path (F-2) — 1도크 E3 블록 하부 진입 경로.
// 멱등: 기존 WorkerPath_E3 를 지우고 다시 만든다. 웨이포인트는 씬에서 끌어 조정.

using UnityEditor;
using UnityEngine;
using YardAX.Performance;

namespace YardAX.EditorTools
{
    public static class CreateWorkerPath
    {
        [MenuItem("Tools/YardAX/Create Worker Path")]
        public static void Create()
        {
            const string Name = "WorkerPath_E3";
            var old = GameObject.Find(Name);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(Name);
            // 기본 경로: 도크 바깥 → E3 블록 하부 (골리앗 config.railOriginWorld 기준 좌표로 조정)
            Vector3[] pts =
            {
                new Vector3(-60, 0, -40), new Vector3(-35, 0, -25),
                new Vector3(-18, 0, -10), new Vector3(-6, 0, 2), new Vector3(0, 0, 10),
            };
            for (int i = 0; i < pts.Length; i++)
            {
                var wp = new GameObject($"WP{i}");
                wp.transform.SetParent(root.transform, false);
                wp.transform.position = pts[i];
            }
            root.AddComponent<WorkerPath>().Rebuild();
            Undo.RegisterCreatedObjectUndo(root, "Create Worker Path");
            Selection.activeGameObject = root;
            Debug.Log("[YardAX] WorkerPath_E3 생성 — 웨이포인트를 씬에서 조정하세요.");
        }
    }
}
