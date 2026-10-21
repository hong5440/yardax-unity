// 발표 모드 (G-7) — 단축키 P: 3D ≥ 70%, 우측 패널은 장면당 하나.
// 장면 프리셋: ① 1도크 전경 ② 골리앗 주행+위험 구간 ③ 동기 권상
// ④ 턴오버 포락선+차단 ⑤ 타워크레인 실물 연동. [ ] 키로 이동.

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YardAX.Viz
{
    [CreateAssetMenu(menuName = "YardAX/Scene Preset", fileName = "ScenePreset")]
    public class ScenePreset : ScriptableObject
    {
        public string title;
        public Vector3 cameraPosition;
        public Vector3 cameraEuler;
        [TextArea] public string presenterNote;
    }

    public class PresentationMode : MonoBehaviour
    {
        public Camera mainCamera;
        public ScenePreset[] presets = Array.Empty<ScenePreset>();
        public float flyTime = 1.6f;

        public bool Active { get; private set; }
        public int PresetIndex { get; private set; } = -1;
        /// <summary>UI가 구독: (발표 모드 on/off)와 (프리셋 index, 제목)</summary>
        public event Action<bool> Toggled;
        public event Action<int, ScenePreset> PresetChanged;

        Vector3 _fromPos, _toPos;
        Quaternion _fromRot, _toRot;
        float _t = 1f;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.pKey.wasPressedThisFrame)
            {
                Active = !Active;
                Toggled?.Invoke(Active);
            }
            if (Active)
            {
                if (kb.rightBracketKey.wasPressedThisFrame) Go(PresetIndex + 1);
                if (kb.leftBracketKey.wasPressedThisFrame) Go(PresetIndex - 1);
            }
            if (_t < 1f && mainCamera != null)
            {
                _t = Mathf.Min(1f, _t + Time.deltaTime / flyTime);
                float s = Mathf.SmoothStep(0, 1, _t);
                mainCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(_fromPos, _toPos, s), Quaternion.Slerp(_fromRot, _toRot, s));
            }
        }

        public void Go(int index)
        {
            if (presets.Length == 0 || mainCamera == null) return;
            PresetIndex = Mathf.Clamp(index, 0, presets.Length - 1);
            var p = presets[PresetIndex];
            _fromPos = mainCamera.transform.position;
            _fromRot = mainCamera.transform.rotation;
            _toPos = p.cameraPosition;
            _toRot = Quaternion.Euler(p.cameraEuler);
            _t = 0f;
            PresetChanged?.Invoke(PresetIndex, p);
        }
    }
}
