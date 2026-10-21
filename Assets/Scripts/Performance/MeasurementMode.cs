// F-6 체험·측정 모드 — WASD로 지정 위치까지 이동, 하중 통과 동안
// 영역 내 체류 시간·경고 후 회피 시간·성공 여부 기록.
// HUD 없음 → HUD 있음 순서로 같은 과제, CSV 저장(persistentDataPath).

using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using YardAX.Performance.Hud;
using YardAX.Viz;

namespace YardAX.Performance
{
    /// <summary>순수 계산: 영역 내 체류 적산 (EditMode 테스트 대상)</summary>
    public class DwellTimer
    {
        public float DwellS { get; private set; }
        public float WarnAt { get; private set; } = -1f;
        public float EvadeS { get; private set; } = -1f;
        bool _wasIn;

        public void Tick(bool inZone, bool warned, float now, float dt)
        {
            if (inZone) DwellS += dt;
            if (warned && WarnAt < 0f) WarnAt = now;
            if (_wasIn && !inZone && WarnAt >= 0f && EvadeS < 0f) EvadeS = now - WarnAt;
            _wasIn = inZone;
        }
    }

    public class MeasurementMode : MonoBehaviour
    {
        public WorkerHud hud;
        public FirstPersonRig rig;
        public Transform goal;              // 지정 위치
        public float moveSpeed = 2f;
        public int participantId = 1;

        public bool Running { get; private set; }
        public bool HudCondition { get; private set; }
        DwellTimer _timer;
        float _t0;
        Vector3 _pos;

        public void StartTrial(bool hudOn)
        {
            HudCondition = hudOn;
            if (hud != null) hud.HudOn = hudOn;
            _timer = new DwellTimer();
            _t0 = Time.time;
            _pos = rig != null && rig.cam != null ? rig.cam.transform.position : transform.position;
            Running = true;
        }

        void Update()
        {
            if (!Running || rig == null || rig.cam == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            var fwd = rig.cam.transform.forward; fwd.y = 0; fwd.Normalize();
            var right = rig.cam.transform.right; right.y = 0; right.Normalize();
            var dir = Vector3.zero;
            if (kb.wKey.isPressed) dir += fwd;
            if (kb.sKey.isPressed) dir -= fwd;
            if (kb.dKey.isPressed) dir += right;
            if (kb.aKey.isPressed) dir -= right;
            _pos += dir.normalized * moveSpeed * Time.deltaTime;
            rig.cam.transform.position = new Vector3(_pos.x, _pos.y, _pos.z);

            bool inZone = hud != null && hud.WorkerInZone;
            bool warned = hud != null && hud.HudOn && inZone;
            _timer.Tick(inZone, warned, Time.time, Time.deltaTime);

            if (goal != null &&
                Vector2.Distance(new Vector2(_pos.x, _pos.z),
                                 new Vector2(goal.position.x, goal.position.z)) < 1.2f)
                Finish(success: true);
        }

        public void Finish(bool success)
        {
            if (!Running) return;
            Running = false;
            string path = Path.Combine(Application.persistentDataPath, "yardax_measure.csv");
            bool fresh = !File.Exists(path);
            using var w = new StreamWriter(path, append: true);
            if (fresh) w.WriteLine("participant,condition,dwell_s,evade_s,success,duration_s,timestamp");
            w.WriteLine(string.Join(",",
                participantId,
                HudCondition ? "HUD_ON" : "HUD_OFF",
                _timer.DwellS.ToString("0.00", CultureInfo.InvariantCulture),
                (_timer.EvadeS < 0 ? -1 : _timer.EvadeS).ToString("0.00", CultureInfo.InvariantCulture),
                success ? 1 : 0,
                (Time.time - _t0).ToString("0.00", CultureInfo.InvariantCulture),
                System.DateTime.Now.ToString("s")));
            Debug.Log($"[YardAX] 측정 저장: {path}");
        }
    }
}
