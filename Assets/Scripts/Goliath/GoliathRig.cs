// 골리앗 리그 — 피벗 이름 규약으로 계층을 해석해 ICraneRig 제공.
// 계층·이름은 Create Goliath Crane 메뉴와 blender/goliath_crane.py 가 공유한다.
//   GoliathRoot(주행) / Legs / Girder / UpperTrolleyA·B / LowerTrolley
//   / HookA·B·L (와이어 LineRenderer) / LoadBlock

using UnityEngine;
using YardAX.Core;
using YardAX.Config;

namespace YardAX.Goliath
{
    public class GoliathRig : MonoBehaviour, ICraneRig
    {
        public GoliathCraneConfig config;

        Transform _root, _trolleyA, _trolleyB, _trolleyL, _hookA, _hookB, _hookL;
        public Transform LoadBlock { get; private set; }

        public Transform Root => _root ? _root : transform;

        void Awake() => Resolve();

        /// <summary>이름 규약으로 피벗 캐시. 에디터 생성 직후에도 호출 가능</summary>
        public void Resolve()
        {
            _root = transform;
            _trolleyA = Find("UpperTrolleyA");
            _trolleyB = Find("UpperTrolleyB");
            _trolleyL = Find("LowerTrolley");
            _hookA = Find("HookA");
            _hookB = Find("HookB");
            _hookL = Find("HookL");
            LoadBlock = Find("LoadBlock");
        }

        Transform Find(string name)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        public Transform GetPivot(CraneAxis axis)
        {
            switch (axis)
            {
                case CraneAxis.GANTRY: return Root;
                case CraneAxis.TROLLEY_A: return _trolleyA;
                case CraneAxis.TROLLEY_B: return _trolleyB;
                case CraneAxis.TROLLEY_L: return _trolleyL;
                case CraneAxis.HOIST_A: return _hookA;
                case CraneAxis.HOIST_B: return _hookB;
                case CraneAxis.HOIST_L: return _hookL;
                default: return null;
            }
        }
    }
}
