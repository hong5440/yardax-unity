// 키보드·게임패드 입력 (G-2, Input System) — 라우터 경유.
//  주행 W/S · 트롤리A A/D · 트롤리L Q/E · 권상AB R/F · 권상L T/G
//  게임패드: 왼스틱 Y=주행, X=트롤리A, 오른스틱 Y=권상AB

using UnityEngine;
using UnityEngine.InputSystem;
using YardAX.Core;
using YardAX.Safety;

namespace YardAX.Goliath
{
    public class GoliathKeyboardInput : MonoBehaviour
    {
        public OperatorInputRouter router;

        void Update()
        {
            if (router == null) return;
            var kb = Keyboard.current;
            var pad = Gamepad.current;

            float gantry = Axis(kb?.wKey, kb?.sKey) + (pad?.leftStick.y.ReadValue() ?? 0f);
            float trolleyA = Axis(kb?.dKey, kb?.aKey) + (pad?.leftStick.x.ReadValue() ?? 0f);
            float trolleyL = Axis(kb?.eKey, kb?.qKey);
            float hoistAB = Axis(kb?.rKey, kb?.fKey) + (pad?.rightStick.y.ReadValue() ?? 0f);
            float hoistL = Axis(kb?.tKey, kb?.gKey);

            router.TryJog(CraneAxis.GANTRY, Mathf.Clamp(gantry, -1f, 1f));
            router.TryJog(CraneAxis.TROLLEY_A, Mathf.Clamp(trolleyA, -1f, 1f));
            router.TryJog(CraneAxis.TROLLEY_L, trolleyL);
            router.TryJog(CraneAxis.HOIST_A, Mathf.Clamp(hoistAB, -1f, 1f));
            router.TryJog(CraneAxis.HOIST_L, hoistL);
        }

        static float Axis(KeyControl plus, KeyControl minus) =>
            (plus != null && plus.isPressed ? 1f : 0f) - (minus != null && minus.isPressed ? 1f : 0f);
    }
}
