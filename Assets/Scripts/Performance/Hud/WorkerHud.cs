// F-4 작업자 HUD (After 전용) — 최소한·작업자 언어·공간 부착 원칙.
// - 하중 이동 띠: Swept 예측 영역을 1인칭 가독용 반투명 띠로 + 화살표 흐름(텍스처 오프셋)
// - 띠 위 월드 텍스트 카운트다운 / 영역 밖이면 초록 "안전"
// - 하중이 시야 밖이면 화면 가장자리 화살표

using UnityEngine;
using YardAX.Core;
using YardAX.Goliath;
using YardAX.Viz;

namespace YardAX.Performance.Hud
{
    public class WorkerHud : MonoBehaviour
    {
        public GoliathController controller;
        public GoliathSweptArea swept;
        public FirstPersonRig rig;
        public HudStrings strings;
        public HudLang lang = HudLang.vi;

        public bool HudOn { get; set; }
        public bool WorkerInZone { get; private set; }
        public float CountdownS { get; private set; } = -1f;

        MeshRenderer _band;
        Texture2D _arrowTex;
        TextMesh _label;

        void Awake()
        {
            // 화살표 흐름 텍스처 (절차 생성)
            _arrowTex = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    bool arrow = Mathf.Abs(y - 16) < (x % 32) / 2 && (x % 32) > 8;
                    _arrowTex.SetPixel(x, y, arrow ? new Color(1, 1, 1, 0.9f) : Color.clear);
                }
            _arrowTex.Apply();
            _arrowTex.wrapMode = TextureWrapMode.Repeat;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = "HudLoadBand";
            go.layer = ProjectionCamera.OverlayLayer;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            _band = go.GetComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            var m = new Material(sh) { mainTexture = _arrowTex, color = new Color(1f, 0.5f, 0.2f, 0.45f) };
            m.renderQueue = 3010;
            _band.sharedMaterial = m;
            go.SetActive(false);

            var lbl = new GameObject("HudLabel") { layer = ProjectionCamera.OverlayLayer };
            _label = lbl.AddComponent<TextMesh>();
            _label.characterSize = 0.5f;
            _label.fontSize = 48;
            _label.anchor = TextAnchor.MiddleCenter;
            lbl.SetActive(false);
        }

        void LateUpdate()
        {
            bool show = HudOn && rig != null && rig.CurrentPhase != FirstPersonRig.Phase.Idle;
            _band.gameObject.SetActive(show);
            _label.gameObject.SetActive(show);
            if (!show) return;

            var poly = swept.GetPolygon(SweptPart.Predicted);
            if (poly.Count < 4) return;
            // 띠 = 예측 영역 (축 정렬 근사)
            Vector2 min = poly[0], max = poly[0];
            foreach (var p in poly) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            var center = (min + max) / 2f;
            _band.transform.position = new Vector3(center.x, 0.12f, center.y);
            _band.transform.localScale = new Vector3(max.x - min.x, max.y - min.y, 1f);
            _band.sharedMaterial.mainTextureOffset += new Vector2(Time.deltaTime * 0.6f, 0);

            var worker = rig.cam != null ? rig.cam.transform.position : Vector3.zero;
            WorkerInZone = swept.Contains(new Vector2(worker.x, worker.z));

            // 카운트다운: 주행 축 기준
            float v = controller.State.GetVelocity(CraneAxis.GANTRY);
            float loadZ = controller.State.GetPosition(CraneAxis.GANTRY);
            CountdownS = PassCountdown.SecondsToReach(
                loadZ, controller.config.loadBlockSize.z / 2f, worker.z, v);

            _band.sharedMaterial.color = WorkerInZone
                ? new Color(1f, 0.4f, 0.2f, 0.5f)
                : new Color(0.2f, 0.85f, 0.45f, 0.35f);

            _label.transform.position = worker + rig.cam.transform.forward * 4f + Vector3.up * 0.5f;
            _label.transform.rotation = Quaternion.LookRotation(
                _label.transform.position - rig.cam.transform.position);
            _label.color = WorkerInZone ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 1f, 0.6f);
            _label.text = !WorkerInZone
                ? strings.Get("safe", lang)
                : CountdownS >= 0f
                    ? strings.Get("countdown", lang, Mathf.CeilToInt(CountdownS))
                    : strings.Get("move_back", lang);
        }

        void OnGUI() // 화면 밖 방향 화살표
        {
            if (!HudOn || rig == null || rig.cam == null || controller == null) return;
            var rigComp = controller.GetComponent<GoliathRig>();
            var trolley = rigComp != null ? rigComp.GetPivot(CraneAxis.TROLLEY_A) : null;
            var loadWorld = trolley != null ? trolley.position : controller.transform.position;
            var vp = rig.cam.WorldToViewportPoint(loadWorld);
            bool off = vp.z < 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1;
            if (!off) return;
            float x = Mathf.Clamp(vp.x, 0.06f, 0.94f) * Screen.width;
            float y = (1f - Mathf.Clamp(vp.y, 0.08f, 0.92f)) * Screen.height;
            var style = new GUIStyle(GUI.skin.label)
            { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = new Color(1f, 0.6f, 0.2f);
            GUI.Label(new UnityEngine.Rect(x - 80, y - 20, 160, 40),
                strings != null ? strings.Get("load_offscreen", lang) : "→", style);
            GUI.color = Color.white;
        }
    }
}
