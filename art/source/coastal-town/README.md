# Coastal Town — 밀도·도로 디테일 2차 (Claude, 2026-10-13)

브리프 `art/_briefs/2026-10-13-town-density.md` 를 Codex 대신 Claude 가 수행한 결과. 출발점은 커밋 `21c7782`(Codex 의 1차 설치, 417 배치).

## 바꾼 것
- **소품 16종 추가** — `tools/asset-gen/lofi_pilot.py` 에 함수만 추가(기존 칸·rng 순서 보존, 아틀라스 칸 8개 신설: awning·sign_tea/ice/rest·zebra·grate·terracotta·cloth). 차양(Awning)·화분(Planter)·화분3개(Pots)·벤치·드럼통·실외기·벽등·빨랫줄·간판 3종·볼라드·상자더미·횡단보도·맨홀·배수구. 전부 1~120 삼각형.
- **프리팹** — `CoastalPropsBuilder.cs`(메뉴 Coastal Town/Build Prop Prefabs). 바닥 소품 MeshCollider, 작은 것 BoxCollider, 데칼·벽걸이는 콜라이더 없음.
- **배치 규칙** — `CoastalTownDensity.cs`. 건물당: 앞(보도) 2~3개 + 옆(도로·건물 없는 쪽) 1~2개 + 벽걸이(집은 창 위 차양, 전부 실외기·벽등) = 4~6개. 집마다 hash 로 조합이 다름. 도로: 교차로 팔마다 횡단보도, 직선 셀에 맨홀(1/3)·배수구(1/2). `CoastalTownInstaller.Layout()` 에서 호출, 메뉴 `Coastal Town/Rebuild Layout + Install`(레이아웃 캐시 삭제 후 설치).
- **설치 결과** — 배치 417 → **2053**, 도로 297/297 연결, 바닥 샘플 1,554 이상 없음, 셰이더 오류 0 (`validation.txt`).
- **스모크 수정 2건** — ① 배달 완료 FAIL 은 더미 런이 route 를 남겨 라벨이 무시되던 테스트 버그 → 라벨로 ActivateRoute. ② 에디터가 뒤에 있으면 Play 가 frame 1 에서 멈춰 차가 스폰 공중(바닥 0.34)에 뜬 채 판정이 틀어짐 → `Application.runInBackground=true`. 결과 8/8 SUCCESS.

## 안 한 것 / 못 본 것
- 톤 통일(브리프 ③): 플레이 캡처 3곳(골목·외곽·항구)에서 Kenney 고층은 안 보였고 건물은 전부 로파이 키트였다. **다운타운 중심부는 따로 캡처하지 않았다** — 거기 남은 게 있는지는 미확인.
- 소품이 보도 위에 놓이므로 길 가장자리 0.5m 는 띄웠지만, **차로 들이받았을 때의 충돌 느낌(벤치·드럼통 MeshCollider)은 시험하지 않았다.** 차 폭 0.9m, 차도 폭 3.4m 는 그대로.
- 교차로 횡단보도는 팔마다 하나씩이라 4거리에서 겹쳐 보일 수 있다(01 캡처 앞쪽). 간격·크기 조정은 판정 뒤.
- 빨랫줄은 집 옆면에만, 상점·정비소엔 없음. 밤/다른 무드는 없음.
- 성능(FPS)·빌드는 측정하지 않았다. 배치 수가 5배가 됐다.

## 캡처
- `before_0*.png` = Codex 1차(커밋 21c7782 시점), `0*.png` = 이번. 같은 자리·같은 스모크 카메라. 비교 한 장 `claude_vs_codex.png`.
- 판정은 `docs/gates/2026-10-13.md` (graphic-gate / verifier).
