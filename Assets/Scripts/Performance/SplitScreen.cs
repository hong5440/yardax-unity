// F-6 화면 분할 — 1인칭 메인 + 관제 보조, 좌우 반반 ↔ 우상단 PiP.
// 두 카메라가 같은 시뮬레이션 상태를 본다 (씬 공유).

using UnityEngine;

namespace YardAX.Performance
{
    public class SplitScreen : MonoBehaviour
    {
        public Camera firstPerson;
        public Camera control;

        public enum Layout { FirstPersonOnly, SideBySide, Pip }
        public Layout layout = Layout.FirstPersonOnly;

        public void Apply(Layout l)
        {
            layout = l;
            if (firstPerson == null || control == null) return;
            switch (l)
            {
                case Layout.FirstPersonOnly:
                    firstPerson.rect = new UnityEngine.Rect(0, 0, 1, 1);
                    control.enabled = false;
                    break;
                case Layout.SideBySide:
                    firstPerson.rect = new UnityEngine.Rect(0, 0, 0.5f, 1);
                    control.rect = new UnityEngine.Rect(0.5f, 0, 0.5f, 1);
                    control.enabled = true;
                    break;
                case Layout.Pip:
                    firstPerson.rect = new UnityEngine.Rect(0, 0, 1, 1);
                    control.rect = new UnityEngine.Rect(0.66f, 0.66f, 0.32f, 0.32f);
                    control.enabled = true;
                    break;
            }
        }
    }
}
