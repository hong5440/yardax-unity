// 시설 번호(1~22) ↔ 3D 맵 오브젝트 매핑 테이블 (ScriptableObject)
// 씬 오브젝트는 이름으로 찾는다 — 씬·프리팹을 직접 수정하지 않는 규칙 유지.
// 카메라 프리셋은 작업 액티브 화면(W-3)의 시설별 고정 시점에 쓴다.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace YardAX.Data
{
    [CreateAssetMenu(menuName = "YardAX/Facility Map Table", fileName = "FacilityMapTable")]
    public class FacilityMapTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public int facilityId;              // 1~22
            public string displayName;          // "1도크와 900톤 골리앗 크레인"
            public string sceneObjectName;      // 씬에서 찾을 루트 오브젝트 이름
            [Header("작업 액티브 카메라 프리셋 (시설 로컬)")]
            public Vector3 cameraOffset = new Vector3(0, 35, -55);
            public Vector3 cameraLookOffset = Vector3.zero;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Find(int facilityId) =>
            entries.Find(e => e.facilityId == facilityId);

        public Transform ResolveSceneObject(int facilityId)
        {
            var e = Find(facilityId);
            if (e == null || string.IsNullOrEmpty(e.sceneObjectName)) return null;
            var go = GameObject.Find(e.sceneObjectName);
            return go ? go.transform : null;
        }
    }
}
