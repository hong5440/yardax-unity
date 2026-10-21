// W-4 장면 보조 컴포넌트 — 단순 왕복/회전/가스 라벨/작업자 등록.

using UnityEngine;
using YardAX.Safety;

namespace YardAX.Schedule
{
    public class SimpleShuttle : MonoBehaviour
    {
        public Vector3 axis = Vector3.right;
        public float amplitude = 5f, period = 8f;
        Vector3 _origin;
        void Awake() => _origin = transform.localPosition;
        void Update() => transform.localPosition =
            _origin + axis * Mathf.Sin(Time.time * Mathf.PI * 2f / period) * amplitude;
    }

    public class SimpleRotate : MonoBehaviour
    {
        public Vector3 axis = Vector3.right;
        public float degPerSec = 10f;
        void Update() => transform.Rotate(axis, degPerSec * Time.deltaTime, Space.Self);
    }

    public class GasLabelTicker : MonoBehaviour
    {
        TextMesh _tm; float _t;
        void Awake() => _tm = GetComponent<TextMesh>();
        void Update()
        {
            _t += Time.deltaTime;
            int m = (int)(_t / 60f) + 12, s = (int)(_t % 60f);
            if (_tm != null)
                _tm.text = $"O₂ 20.9% · 측정 후 {m:00}:{s:00} 경과 (시뮬레이션 값)";
        }
    }

    public class RegisterWorkerOnEnable : MonoBehaviour
    {
        void OnEnable() { if (WorkerRegistry.Instance != null) WorkerRegistry.Instance.Register(transform); }
        void OnDisable() { if (WorkerRegistry.Instance != null) WorkerRegistry.Instance.Unregister(transform); }
    }
}
