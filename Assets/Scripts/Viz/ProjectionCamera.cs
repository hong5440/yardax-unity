// 바닥 투영용 정사영 카메라 (G-7) — 두 번째 디스플레이(프로젝터).
// Swept Area·주행 위험 구간·회전 포락선·작업자 마커만 렌더 (레이어 30 = YardOverlay).
// Tags & Layers에서 레이어 30 이름을 "YardOverlay"로 지정할 것 (코드로는 번호만 사용).

using UnityEngine;

namespace YardAX.Viz
{
    public class ProjectionCamera : MonoBehaviour
    {
        public const int OverlayLayer = 30;
        public Transform focus;            // 1도크 중심
        public float orthoSize = 90f;
        public int targetDisplay = 1;      // Display 2

        Camera _cam;

        void Awake()
        {
            _cam = gameObject.GetComponent<Camera>();
            if (_cam == null) _cam = gameObject.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = orthoSize;
            _cam.cullingMask = 1 << OverlayLayer;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;
            _cam.targetDisplay = targetDisplay;
            if (Display.displays.Length > targetDisplay)
                Display.displays[targetDisplay].Activate();
        }

        void LateUpdate()
        {
            var c = focus != null ? focus.position : Vector3.zero;
            transform.position = new Vector3(c.x, 120f, c.z);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        /// <summary>오버레이 오브젝트를 투영 레이어로 (자식 포함)</summary>
        public static void MarkOverlay(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = OverlayLayer;
        }
    }
}
