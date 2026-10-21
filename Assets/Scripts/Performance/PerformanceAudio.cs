// F-5 사운드 — 파일은 Assets/Audio/Performance/ (이름 규칙: 그 폴더 README).
// 클립 슬롯이 비어 있으면 조용히 건너뛴다 (팀이 녹음·TTS로 채우면 됨).
// AudioMixer 그룹 Master/Voice를 연결하면 볼륨 분리 — 없으면 volume 배율만.

using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using YardAX.Core;
using YardAX.Goliath;

namespace YardAX.Performance
{
    public class PerformanceAudio : MonoBehaviour
    {
        public GoliathController controller;
        public GoliathRig rig;
        [Header("믹서 (선택)")]
        public AudioMixerGroup masterGroup;
        public AudioMixerGroup voiceGroup;
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float voiceVolume = 1f;

        [Header("클립 — Assets/Audio/Performance/")]
        public AudioClip craneMotorLoop;   // crane_motor_loop.wav
        public AudioClip wireCreakLoop;    // wire_creak_loop.wav
        public AudioClip[] blockMetal;     // block_metal_01..n.wav
        public AudioClip radioKoBefore;    // radio_ko_01.wav
        public AudioClip warnVoiceStop;    // warn_<lang>_stop.wav
        public AudioClip warnVoiceBack;    // warn_<lang>_back.wav
        public AudioClip warnBeep;         // beep_warning.wav

        AudioSource _motor, _wire, _metal, _voice;

        void Awake()
        {
            _motor = Make3D("MotorSrc", craneMotorLoop, true);
            _wire = Make3D("WireSrc", wireCreakLoop, true);
            _metal = Make3D("MetalSrc", null, false);
            _voice = gameObject.AddComponent<AudioSource>(); // 2D 음성
            _voice.outputAudioMixerGroup = voiceGroup != null ? voiceGroup : masterGroup;
            StartCoroutine(MetalLoop());
        }

        AudioSource Make3D(string name, AudioClip clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = loop;
            s.spatialBlend = 1f;                    // 3D
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = 8f; s.maxDistance = 160f;
            s.outputAudioMixerGroup = masterGroup;
            if (clip != null && loop) s.Play();
            return s;
        }

        void LateUpdate()
        {
            _motor.volume = masterVolume; _wire.volume = masterVolume * 0.7f;
            _metal.volume = masterVolume * 0.4f; _voice.volume = voiceVolume;
            if (rig != null)
            {
                var trolley = rig.GetPivot(CraneAxis.TROLLEY_A);
                if (trolley != null) _motor.transform.position = trolley.position;
                var hook = rig.GetPivot(CraneAxis.HOIST_A);
                if (hook != null) _wire.transform.position = hook.position;
                if (rig.LoadBlock != null) _metal.transform.position = rig.LoadBlock.position;
            }
            // 모터 피치: 축 속도 합에 비례 (멈추면 저속 아이들)
            if (controller != null && _motor.clip != null)
            {
                float v = Mathf.Abs(controller.State.GetVelocity(CraneAxis.GANTRY)) +
                          Mathf.Abs(controller.State.GetVelocity(CraneAxis.TROLLEY_A)) +
                          Mathf.Abs(controller.State.GetVelocity(CraneAxis.HOIST_A));
                _motor.pitch = Mathf.Lerp(_motor.pitch, 0.8f + Mathf.Clamp01(v) * 0.5f, Time.deltaTime * 3f);
            }
        }

        IEnumerator MetalLoop() // 블록 흔들림 금속음 — 약하게, 주기 랜덤
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(4f, 11f));
                if (blockMetal != null && blockMetal.Length > 0)
                {
                    var c = blockMetal[Random.Range(0, blockMetal.Length)];
                    if (c != null) _metal.PlayOneShot(c, 0.6f);
                }
            }
        }

        public void PlayRadioBefore() { if (radioKoBefore != null) _voice.PlayOneShot(radioKoBefore); }
        public void PlayWarnStop()
        {
            if (warnBeep != null) _voice.PlayOneShot(warnBeep, 0.9f);
            if (warnVoiceStop != null) _voice.PlayOneShot(warnVoiceStop);
        }
        public void PlayWarnBack() { if (warnVoiceBack != null) _voice.PlayOneShot(warnVoiceBack); }
    }
}
