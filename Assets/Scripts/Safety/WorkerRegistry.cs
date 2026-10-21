// 작업자 위치 등록부 (G-5) — SIM 작업자(씬 오브젝트)와 LIVE worker 메시지를 합친다.

using System.Collections.Generic;
using UnityEngine;

namespace YardAX.Safety
{
    public class WorkerRegistry : MonoBehaviour
    {
        public static WorkerRegistry Instance { get; private set; }

        readonly List<Transform> _sceneWorkers = new();
        readonly Dictionary<string, Vector2> _remote = new(); // LIVE: id → worldXZ

        void Awake() => Instance = this;

        public void Register(Transform worker) { if (!_sceneWorkers.Contains(worker)) _sceneWorkers.Add(worker); }
        public void Unregister(Transform worker) => _sceneWorkers.Remove(worker);
        public void SetRemote(string id, Vector2 worldXZ) => _remote[id] = worldXZ;

        public IEnumerable<Vector2> AllXZ()
        {
            foreach (var t in _sceneWorkers)
                if (t != null) yield return new Vector2(t.position.x, t.position.z);
            foreach (var v in _remote.Values) yield return v;
        }

        public bool Any(System.Func<Vector2, bool> inArea)
        {
            foreach (var p in AllXZ()) if (inArea(p)) return true;
            return false;
        }

        public bool AnyInCircle(Vector2 centerXZ, float radius) =>
            Any(p => (p - centerXZ).sqrMagnitude <= radius * radius);
    }
}
