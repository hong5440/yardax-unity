// F-2 경로 — Cinemachine이 없어도 동작하는 Catmull-Rom 웨이포인트 보간.
// (Cinemachine 설치 시 CinemachineSplineDolly로 교체 가능하도록 IWorkerPath 뒤에 숨김)
// 웨이포인트는 Tools > YardAX > Create Worker Path 가 자식으로 생성.

using System.Collections.Generic;
using UnityEngine;

namespace YardAX.Performance
{
    public interface IWorkerPath
    {
        float Length { get; }
        Vector3 Position(float t01);
        Vector3 Direction(float t01);
    }

    public class WorkerPath : MonoBehaviour, IWorkerPath
    {
        readonly List<Vector3> _pts = new();
        float _length;

        public float Length { get { EnsureBuilt(); return _length; } }

        void OnEnable() => Rebuild();

        public void Rebuild()
        {
            _pts.Clear();
            foreach (Transform c in transform) _pts.Add(c.position);
            _length = 0f;
            for (int i = 1; i < _pts.Count; i++) _length += Vector3.Distance(_pts[i - 1], _pts[i]);
        }

        void EnsureBuilt() { if (_pts.Count == 0) Rebuild(); }

        public Vector3 Position(float t) { EnsureBuilt(); return Sample(Mathf.Clamp01(t)); }

        public Vector3 Direction(float t)
        {
            EnsureBuilt();
            const float dt = 0.01f;
            var d = Sample(Mathf.Clamp01(t + dt)) - Sample(Mathf.Clamp01(t - dt));
            return d.sqrMagnitude < 1e-8f ? transform.forward : d.normalized;
        }

        Vector3 Sample(float t)
        {
            int n = _pts.Count;
            if (n == 0) return transform.position;
            if (n == 1) return _pts[0];
            float f = t * (n - 1);
            int i = Mathf.Min(n - 2, Mathf.FloorToInt(f));
            float u = f - i;
            Vector3 p0 = _pts[Mathf.Max(0, i - 1)], p1 = _pts[i], p2 = _pts[i + 1],
                    p3 = _pts[Mathf.Min(n - 1, i + 2)];
            // Catmull-Rom
            return 0.5f * ((2f * p1) + (-p0 + p2) * u +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * (u * u) +
                (-p0 + 3f * p1 - 3f * p2 + p3) * (u * u * u));
        }

        void OnDrawGizmos()
        {
            Rebuild();
            Gizmos.color = Color.cyan;
            for (float t = 0; t < 1f; t += 0.02f)
                Gizmos.DrawLine(Position(t), Position(t + 0.02f));
        }
    }
}
