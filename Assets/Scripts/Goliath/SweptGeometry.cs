// Swept Area 순수 기하 (G-3) — EditMode 테스트 대상. Unity 타입 없이 계산.
// 좌표계: 크레인 레일 로컬 2D (x = 거더 방향, z = 레일 방향).

using System;

namespace YardAX.Goliath
{
    public struct Rect2
    {
        public float MinX, MinZ, MaxX, MaxZ;
        public float LenX => MaxX - MinX;
        public float LenZ => MaxZ - MinZ;
        public bool Contains(float x, float z, float margin = 0f) =>
            x >= MinX - margin && x <= MaxX + margin && z >= MinZ - margin && z <= MaxZ + margin;

        public static Rect2 Centered(float cx, float cz, float sizeX, float sizeZ) => new Rect2
        { MinX = cx - sizeX / 2f, MaxX = cx + sizeX / 2f, MinZ = cz - sizeZ / 2f, MaxZ = cz + sizeZ / 2f };

        public static Rect2 Union(Rect2 a, Rect2 b) => new Rect2
        {
            MinX = Math.Min(a.MinX, b.MinX), MaxX = Math.Max(a.MaxX, b.MaxX),
            MinZ = Math.Min(a.MinZ, b.MinZ), MaxZ = Math.Max(a.MaxZ, b.MaxZ),
        };
    }

    /// <summary>블록 바닥 사각형이 직선 경로를 쓸고 간 영역 (사각형들의 합 = 합 사각형)</summary>
    public static class SweptGeometry
    {
        public enum SweepAxis { AlongX, AlongZ, None }

        /// <summary>시작 중심 + 이동량 → 전체 예측 영역</summary>
        public static Rect2 FullSweep(float cx, float cz, float sizeX, float sizeZ,
                                      SweepAxis axis, float signedDist)
        {
            var a = Rect2.Centered(cx, cz, sizeX, sizeZ);
            float ex = axis == SweepAxis.AlongX ? signedDist : 0f;
            float ez = axis == SweepAxis.AlongZ ? signedDist : 0f;
            var b = Rect2.Centered(cx + ex, cz + ez, sizeX, sizeZ);
            return Rect2.Union(a, b);
        }

        /// <summary>진행도(0..1)로 지나온/남은 영역 분할</summary>
        public static void Split(float cx, float cz, float sizeX, float sizeZ,
                                 SweepAxis axis, float signedDist, float progress01,
                                 out Rect2 passed, out Rect2 remaining, out Rect2 current)
        {
            progress01 = Math.Clamp(progress01, 0f, 1f);
            float px = axis == SweepAxis.AlongX ? signedDist * progress01 : 0f;
            float pz = axis == SweepAxis.AlongZ ? signedDist * progress01 : 0f;
            current = Rect2.Centered(cx + px, cz + pz, sizeX, sizeZ);
            passed = Rect2.Union(Rect2.Centered(cx, cz, sizeX, sizeZ), current);
            float ex = axis == SweepAxis.AlongX ? signedDist : 0f;
            float ez = axis == SweepAxis.AlongZ ? signedDist : 0f;
            remaining = Rect2.Union(current, Rect2.Centered(cx + ex, cz + ez, sizeX, sizeZ));
        }

        /// <summary>주행 위험 구간: 다리 전방(진행 방향) 레일 스트립</summary>
        public static Rect2 GantryDangerStrip(float gantryZ, int dir, float stripLen,
                                              float spanX, float bogieHalfLen)
        {
            float z0 = gantryZ + dir * bogieHalfLen;
            float z1 = z0 + dir * stripLen;
            return new Rect2
            {
                MinX = -spanX / 2f - 4f, MaxX = spanX / 2f + 4f,
                MinZ = Math.Min(z0, z1), MaxZ = Math.Max(z0, z1),
            };
        }
    }
}
