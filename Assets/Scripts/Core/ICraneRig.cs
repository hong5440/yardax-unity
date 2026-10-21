// 크레인 리그 — 축별 피벗 트랜스폼 핸들. 씬 계층(또는 FBX 교체 모델)의
// 피벗 이름 규약을 코드에서 쓰는 유일한 통로.

using UnityEngine;

namespace YardAX.Core
{
    public interface ICraneRig
    {
        /// <summary>해당 축을 구동하는 피벗. 없으면 null</summary>
        Transform GetPivot(CraneAxis axis);
        /// <summary>리그 루트 (월드 배치 기준)</summary>
        Transform Root { get; }
    }
}
