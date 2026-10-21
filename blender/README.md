# 골리앗 크레인 Blender 모델

`goliath_crane.py` 는 Unity G-1 리그(`GoliathRoot` 계층)와 **오브젝트 이름·피벗
위치가 동일한** 골리앗 크레인을 절차적으로 생성한다. Unity 쪽에서는 FBX의 메시만
교체하고, 움직이는 축은 같은 이름의 피벗(`UpperTrolleyA/B`, `LowerTrolley`,
`HookA/B/L`, `GoliathRoot`)을 그대로 구동하면 된다.

## 생성·내보내기

```bash
# 생성만 (Blender UI에서 열어 확인)
blender -P blender/goliath_crane.py

# FBX 내보내기까지 (헤드리스)
blender -b -P blender/goliath_crane.py -- --export build/goliath_crane.fbx
```

치수는 스크립트 상단의 설정 블록(`SPAN`, `LEG_H`, `LOAD_BLOCK` …)에서 조정한다.
와이어는 모델에 포함하지 않는다 — Unity `LineRenderer` 가 그린다.

## Unity 임포트 설정

내보내기 옵션이 이미 Unity 축에 맞춰져 있다
(`apply_unit_scale` + `FBX_SCALE_ALL`, `axis_forward=-Z`, `axis_up=Y`,
`bake_space_transform=True`). Unity 임포트 시:

1. **Scale Factor = 1** (Convert Units 체크 상태 그대로, 루트 스케일 100이 보이면
   내보내기 옵션이 빠진 것)
2. 루트에 -89.98° 회전이 보이면 `bake_space_transform` 이 꺼진 것 — 다시 내보낼 것
3. 계층 확인: `GoliathRoot / Legs / Girder / UpperTrolleyA·B / LowerTrolley /
   HookA·B·L / LoadBlock` 이름이 그대로 보여야 Unity 리그 교체 스크립트가 매칭한다
4. 트러스 부재·사다리 가로대는 메시 공유(인스턴스)라 드로우콜 부담이 적다

## 피벗 규약 (Unity 기준)

| 오브젝트 | 피벗 위치 | 움직임 |
|---|---|---|
| GoliathRoot | 레일 중앙·지면 | 주행 (레일 방향) |
| UpperTrolleyA/B | 거더 윗면·트롤리 중심 | 횡행 (거더 방향) |
| LowerTrolley | 거더 밑면·트롤리 중심 | 횡행 |
| HookA/B/L | 후크 블록 상단 | 권상 (수직) |
| LoadBlock | 블록 바닥 중심 | 후크에 연결/해제 |
