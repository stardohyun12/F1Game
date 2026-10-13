# 브리프: 로파이 해안 마을 — 밀도·비례·톤 (2026-10-13)

## 도현의 한 줄 (합격 기준)
> Codex 에서 만들던 거 계속 만들어라. — 같은 결(따뜻한 로파이 해안)로, 마을이 "살고 있는 동네"로 보이게.

## 바탕 (읽고 시작)
- 결정: `옵시디언/notes/decisions-log.md` D-096(스타일 확정, 네가 만든 로파이 키트 채택) · D-095(비례·밀도 규칙 유지).
- 네 이전 산출물: `art/source/lofi-pilot/README.md`, `art/source/coastal-formula/README.md`, `art/source/coastal-town/validation.txt`, `play-verification.txt`.
- 설치된 상태: `F1/Assets/ComplexTrack.unity` 에 `CoastalTown`(417 배치) + 플레이어 차 `FormulaGameplayVisual`. 커밋 `21c7782`.
- 외부 눈(graphic-gate) 판정 4/10 FAIL, 이유 셋: ① 건물이 소품 없는 거대한 단면 벽 ② 도로가 빈약 ③ 낡은 차 장면이 차가운 고층 도심이라 톤 불일치.

## 해야 할 것 (이 순서)
1. **건물당 소품 5개 안팎** — 차양·화분·상자·간판·전신주·빨랫줄·에어컨 실외기·계단 옆 화분 같은 생활 소품을 키트에 추가하고(`lofi_pilot.py` 아틀라스 규칙 유지, 64px 칸·Point 샘플링·2m UV), `CoastalTownInstaller` 배치 규칙에 "건물 앞·옆에 N개" 를 넣는다. 집마다 조합이 달라야 한다.
2. **도로 디테일** — 연석·배수구·횡단보도·바퀴 자국·맨홀·중앙선 마모 같은 것을 같은 아틀라스로. 도로 폭·충돌·배달 경로는 바꾸지 않는다.
3. **톤 통일** — 플레이어가 달리는 모든 구역이 같은 결이어야 한다. 아직 Kenney 고층(회색·파란 유리)이 보이는 곳이 있으면 로파이 키트로 바꾸거나 숨긴다. 세 구역(다운타운·공업·항구)은 색·간판·소품 종류로만 구분한다.
4. **검증** — 네 `CoastalTownSmoke.Run()`(편집 모드에서 호출, 자동 Play·캡처·종료)이 SUCCESS 여야 한다. `bash scripts/regression-check.sh` 가 12 PASS / 1 FAIL(기존 CargoPolyBudget) 이상이어야 한다. Unity 는 `bash scripts/ucli.sh exec "<C#>"` / `menu` / `console --type error` 로 다룬다(에디터는 켜져 있고 포트 8090).
5. **캡처** — 같은 자리 세 장을 `art/source/coastal-town/` 에 덮어쓴다: `01_ingame_neighborhood.png`, `03_ingame_harbor.png`, 그리고 플레이어 차가 보이는 추적캠 한 장 `05_ingame_chase.png`. 비교용으로 이전 장은 `before_*.png` 로 남긴다.
6. **기록** — `art/source/coastal-town/README.md` 에 무엇을 바꿨고 무엇은 안 했는지(실제 검증 범위 포함) 짧게. 씬은 저장한다. git 커밋은 하지 않는다(사람이 게이트 뒤에 한다).

## 하지 말 것
- 새 아트 방향 제안, 스타일 재논의. D-096 안에서만.
- 차량 물리·카메라·배달 시스템 코드 변경. 외형과 배치만.
- 결과를 좋게 요약하지 말 것. 안 한 것·못 본 것은 그대로 적는다.
