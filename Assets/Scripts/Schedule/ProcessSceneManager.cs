// W-4 공정 장면 관리 — 작업지시 선택 시 해당 공정 장면만 활성화.
// 장면 자체는 Tools > YardAX > Create Process Scenes 가 생성.

using System.Collections.Generic;
using UnityEngine;
using YardAX.Data;

namespace YardAX.Schedule
{
    public class ProcessScene : MonoBehaviour
    {
        public string processKey;   // cut|sub|asm|outfit|paint|quay|erect
        public int facilityId;
    }

    public class ProcessSceneManager : MonoBehaviour
    {
        readonly List<ProcessScene> _scenes = new();

        void Awake()
        {
            _scenes.AddRange(FindObjectsOfType<ProcessScene>(true));
            foreach (var s in _scenes) s.gameObject.SetActive(false);
        }

        public static string KeyFor(WorkOrder wo) =>
            wo.process.Contains("절단") ? "cut"
            : wo.process.Contains("소조") || wo.process.Contains("취부") && wo.facilityId == 6 ? "sub"
            : wo.process.Contains("대조립") || wo.process.Contains("조립") ? "asm"
            : wo.process.Contains("족장") || wo.process.Contains("배관") && wo.facilityId == 5 ? "outfit"
            : wo.process.Contains("도장") ? "paint"
            : wo.facilityId == 9 ? "quay"
            : wo.process.Contains("탑재") ? "erect" : "";

        public void ActivateFor(WorkOrder wo)
        {
            string key = KeyFor(wo);
            foreach (var s in _scenes)
                s.gameObject.SetActive(s.processKey == key ||
                    (s.processKey == key && s.facilityId == wo.facilityId));
        }

        public void ActivateKey(string key)
        {
            foreach (var s in _scenes) s.gameObject.SetActive(s.processKey == key);
        }

        public void DeactivateAll()
        {
            foreach (var s in _scenes) s.gameObject.SetActive(false);
        }
    }
}
