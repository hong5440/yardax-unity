// F-3 명령 기록 — Before/After가 시간까지 똑같도록 타임스탬프 재생.
// 기존 명령 수신 구조(OperatorInputRouter) 재사용.

using System.Collections.Generic;
using UnityEngine;
using YardAX.Core;
using YardAX.Safety;

namespace YardAX.Performance
{
    [CreateAssetMenu(menuName = "YardAX/Command Recording", fileName = "CommandRecording")]
    public class CommandRecording : ScriptableObject
    {
        [System.Serializable]
        public struct Item
        {
            public float atS;        // 재생 시작 기준 (s)
            public CraneAxis axis;
            public int direction;    // +1 | -1
            public float amountM;
        }
        public List<Item> items = new();
    }

    public class CommandPlayback : MonoBehaviour
    {
        public OperatorInputRouter router;
        public CommandRecording recording;
        public string craneId = "goliath-1";

        public bool Playing { get; private set; }
        public float Clock { get; private set; }
        int _next;

        public void Play() { Playing = true; Clock = 0f; _next = 0; }
        public void StopAndRewind() { Playing = false; Clock = 0f; _next = 0; }

        void Update()
        {
            if (!Playing || recording == null || router == null) return;
            Clock += Time.deltaTime;
            while (_next < recording.items.Count && recording.items[_next].atS <= Clock)
            {
                var it = recording.items[_next++];
                router.TryExecute(new CraneCommandV2
                {
                    craneId = craneId, axis = it.axis,
                    direction = it.direction, amountM = it.amountM,
                }, source: "recording");
            }
            if (_next >= recording.items.Count) Playing = false;
        }
    }
}
