// GoliathSweptArea (G-3) — ISweptAreaProvider 구현.
// 표시 체계(타워와 동일): 전체 예측 / 지나온 / 남은 / 현재 footprint
// + 주행 시 다리 전방 "주행 위험 구간" 별도 표시.
// Procedural Mesh를 레일 프레임 바닥(y≈0.05)에 깐다.

using System.Collections.Generic;
using UnityEngine;
using YardAX.Config;
using YardAX.Core;
using YardAX.Goliath;

namespace YardAX.Viz
{
    public class GoliathSweptArea : MonoBehaviour, ISweptAreaProvider
    {
        public GoliathController controller;
        public GoliathCraneConfig config;

        [Header("색 (시연용)")]
        public Color predictedColor = new Color(1f, 0.67f, 0.16f, 0.22f);
        public Color passedColor = new Color(0.45f, 0.55f, 0.65f, 0.18f);
        public Color remainingColor = new Color(1f, 0.67f, 0.16f, 0.38f);
        public Color currentColor = new Color(0.24f, 0.78f, 1f, 0.35f);
        public Color dangerStripColor = new Color(1f, 0.23f, 0.18f, 0.3f);

        Transform _ground;                       // 레일 프레임 (railOrigin + yaw)
        readonly Dictionary<SweptPart, MeshFilter> _quads = new();
        MeshFilter _strip;

        CraneAxis _axis;
        float _startPos, _delta;                 // 명령 시작 축 위치·이동량
        float _startCx, _startCz;                // 블록 시작 중심 (레일 로컬)
        bool _active;

        void Awake()
        {
            if (config == null && controller != null) config = controller.config;
            _ground = new GameObject("GoliathSweptGround").transform;
            _ground.SetPositionAndRotation(config.railOriginWorld,
                Quaternion.Euler(0, config.railYawDeg, 0));
            foreach (SweptPart part in System.Enum.GetValues(typeof(SweptPart)))
                _quads[part] = MakeQuad("Swept_" + part, ColorOf(part), part == SweptPart.CurrentFootprint ? 6 : 5);
            _strip = MakeQuad("GantryDangerStrip", dangerStripColor, 5);
        }

        Color ColorOf(SweptPart p) => p switch
        {
            SweptPart.Predicted => predictedColor,
            SweptPart.Passed => passedColor,
            SweptPart.Remaining => remainingColor,
            _ => currentColor,
        };

        public void Recalculate(CraneAxis axis, float signedAmountM)
        {
            _axis = axis;
            _active = true;
            var st = controller.State;
            _startPos = st.GetPosition(axis);
            _delta = signedAmountM;
            // 블록 중심 (레일 로컬): x = 하부? 블록은 상부 A·B 사이 — A·B 중점, z = 주행 위치
            _startCx = (st.GetPosition(CraneAxis.TROLLEY_A) + st.GetPosition(CraneAxis.TROLLEY_B)) / 2f;
            _startCz = st.GetPosition(CraneAxis.GANTRY);
        }

        public void Clear()
        {
            _active = false;
            foreach (var q in _quads.Values) q.gameObject.SetActive(false);
            _strip.gameObject.SetActive(false);
        }

        SweptGeometry.SweepAxis SweepAxisOf(CraneAxis a) => a switch
        {
            CraneAxis.GANTRY => SweptGeometry.SweepAxis.AlongZ,
            CraneAxis.TROLLEY_A or CraneAxis.TROLLEY_B or CraneAxis.TROLLEY_L
                => SweptGeometry.SweepAxis.AlongX,
            _ => SweptGeometry.SweepAxis.None,
        };

        void LateUpdate()
        {
            if (!_active || controller == null) return;
            var size = config.loadBlockSize;
            var sweep = SweepAxisOf(_axis);
            float now = controller.State.GetPosition(_axis);
            float progress = Mathf.Abs(_delta) < 1e-4f ? 1f
                : Mathf.Clamp01((now - _startPos) / _delta);

            var full = SweptGeometry.FullSweep(_startCx, _startCz, size.x, size.z, sweep, _delta);
            SweptGeometry.Split(_startCx, _startCz, size.x, size.z, sweep, _delta, progress,
                out var passed, out var remaining, out var current);

            Apply(SweptPart.Predicted, full);
            Apply(SweptPart.Passed, passed);
            Apply(SweptPart.Remaining, remaining);
            Apply(SweptPart.CurrentFootprint, current);

            bool strip = _axis == CraneAxis.GANTRY && progress < 1f;
            _strip.gameObject.SetActive(strip);
            if (strip)
                ApplyRect(_strip, SweptGeometry.GantryDangerStrip(
                    now, _delta >= 0 ? 1 : -1, config.gantryDangerStripM, config.spanM, 7f));
        }

        void Apply(SweptPart part, Rect2 r)
        {
            var q = _quads[part];
            q.gameObject.SetActive(true);
            ApplyRect(q, r);
        }

        // ── ISweptAreaProvider 조회 ──
        public IReadOnlyList<Vector2> GetPolygon(SweptPart part)
        {
            if (!_active) return System.Array.Empty<Vector2>();
            var size = config.loadBlockSize;
            var sweep = SweepAxisOf(_axis);
            float now = controller.State.GetPosition(_axis);
            float progress = Mathf.Abs(_delta) < 1e-4f ? 1f : Mathf.Clamp01((now - _startPos) / _delta);
            SweptGeometry.Split(_startCx, _startCz, size.x, size.z, sweep, _delta, progress,
                out var passed, out var remaining, out var current);
            var r = part switch
            {
                SweptPart.Predicted => SweptGeometry.FullSweep(_startCx, _startCz, size.x, size.z, sweep, _delta),
                SweptPart.Passed => passed,
                SweptPart.Remaining => remaining,
                _ => current,
            };
            return RectWorld(r);
        }

        public bool Contains(Vector2 worldXZ)
        {
            if (!_active) return false;
            var local = _ground.InverseTransformPoint(new Vector3(worldXZ.x, 0, worldXZ.y));
            var size = config.loadBlockSize;
            var full = SweptGeometry.FullSweep(_startCx, _startCz, size.x, size.z,
                SweepAxisOf(_axis), _delta);
            return full.Contains(local.x, local.z, margin: 1.5f);
        }

        List<Vector2> RectWorld(Rect2 r)
        {
            var list = new List<Vector2>(4);
            foreach (var (x, z) in new[] { (r.MinX, r.MinZ), (r.MaxX, r.MinZ), (r.MaxX, r.MaxZ), (r.MinX, r.MaxZ) })
            {
                var w = _ground.TransformPoint(new Vector3(x, 0, z));
                list.Add(new Vector2(w.x, w.z));
            }
            return list;
        }

        // ── 메시 ──
        MeshFilter MakeQuad(string name, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_ground, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = new Mesh { name = name };
            var mr = go.AddComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh) { color = c };
            m.renderQueue = 3000 + order; // 투명 위 순서
            TrySetTransparent(m);
            mr.sharedMaterial = m;
            go.SetActive(false);
            return mf;
        }

        static void TrySetTransparent(Material m)
        {
            if (m.HasProperty("_Surface")) { m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); }
            m.SetOverrideTag("RenderType", "Transparent");
            if (m.HasProperty("_SrcBlend"))
            {
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
            }
        }

        void ApplyRect(MeshFilter mf, Rect2 r)
        {
            var mesh = mf.sharedMesh;
            mesh.Clear();
            float y = 0.05f;
            mesh.vertices = new[]
            {
                new Vector3(r.MinX, y, r.MinZ), new Vector3(r.MaxX, y, r.MinZ),
                new Vector3(r.MaxX, y, r.MaxZ), new Vector3(r.MinX, y, r.MaxZ),
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
