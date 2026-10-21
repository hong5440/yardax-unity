// 예상 경로·Swept Area 계산/표시 — 타워·골리앗이 공유하는 표시 체계.

using System.Collections.Generic;
using UnityEngine;

namespace YardAX.Core
{
    /// <summary>표시 구획: 전체 예측 / 지나온 / 남은 / 현재 footprint</summary>
    public enum SweptPart { Predicted, Passed, Remaining, CurrentFootprint }

    public interface ISweptAreaProvider
    {
        /// <summary>명령 수신 시 호출 — 예상 경로를 다시 계산한다</summary>
        void Recalculate(CraneAxis axis, float signedAmountM);
        /// <summary>해당 구획의 바닥 폴리곤(월드 XZ). 없으면 빈 리스트</summary>
        IReadOnlyList<Vector2> GetPolygon(SweptPart part);
        /// <summary>월드 점이 현재 위험 영역(Predicted)에 들어 있는가</summary>
        bool Contains(Vector2 worldXZ);
        void Clear();
    }
}
