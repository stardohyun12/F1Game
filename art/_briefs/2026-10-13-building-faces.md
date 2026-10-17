# 브리프: 로파이 해안 마을 — 건물 벽 단면 (2026-10-13, 저녁)

## 도현의 한 줄 (합격 기준)
> 아트 부분 같이 해봐. — 네 로파이 키트(D-096) 그대로, 건물이 "창 하나 붙은 베이지 평면"으로 보이지 않게.

## 바탕 (읽고 시작)
- 네 키트: `tools/asset-gen/lofi_pilot.py` (CornerShop / RepairGarage / CoastalHouse). 오늘 Claude 가 소품 31종과 아틀라스 칸 15개를 **뒤에 붙였다**(기존 칸·rng 순서 보존). 네가 할 때도 같은 규칙: 팔레트 칸은 목록 끝에만 추가, 기존 함수의 rng 호출 순서를 바꾸지 않는다.
- 지금 상태 캡처: `art/source/coastal-town/01_ingame_neighborhood.png`, `03_ingame_harbor.png`, `06_downtown_center.png`. 비교 3단 `v3_vs_v2_vs_codex.png`. 경위 `art/source/coastal-town/README.md`(3차까지).
- 외부 눈(graphic-gate, `docs/gates/2026-10-13.md` 19:34 줄) 6/10 FAIL. 남은 지적 중 네 몫: **"건물 벽이 잘린 거대한 단면 — 화면 20% 가 베이지 얼룩 평면에 문 하나"**, **"건물 반복(같은 집 3종)"**.
- 배치 규칙 `F1/Assets/Editor/CoastalTownDensity.cs` 의 `Body()` 가 몸통 치수를 안다: 집 5×4.8, 상점 6×5.2, 정비소 6.4×5.6. **이 치수는 유지**(돌출은 벽에서 0.35m 이내). 벽걸이 소품(차양 y=hd+.46·실외기 2.25s·벽등 2.15s)이 벽 바깥에 붙으니 그 높이 띠(1.9~2.5m)의 돌출은 피한다.

## 해야 할 것 (이 순서)
1. **벽면 디테일** — 세 건물 모두: 층 구분선(1층/2층 사이 띠), 벽 하단 기단 띠(석재·0.5m), 창 줄(층마다 창 2~3개, 창틀·창턱), 처마 밑 그림자 띠, 모서리 기둥(쿼인). 2m 이상 민무늬 면이 안 남게. 전부 같은 아틀라스(64px 칸, Point 샘플링, 2m UV 분할).
2. **변형** — 건물 종류마다 hash 로 고르는 변형 2~3개: 집(발코니 있음/없음, 덧문, 굴뚝, 지붕 색), 상점(간판 글자 MART 외 1개, 차양 색), 정비소(셔터 열림/닫힘, 간판 위치). 변형은 `lofi_pilot.py` 에서 별도 FBX 이름(예 `CoastalHouseB`)으로 내보내고, `CoastalTownInstaller.Layout()` 의 asset 선택 한 줄에서 hash 로 고른다. `CoastalPropsBuilder` 는 손대지 말 것(소품 전용). 새 건물 프리팹은 네 `LofiPilotBuilder` 쪽에서.
3. **검증** — Blender: `D:/Tools/Blender/blender.exe -b -P tools/asset-gen/lofi_pilot.py -- --root D:/Github/F1Game`. Unity 는 켜져 있다(포트 8090): `bash scripts/ucli.sh exec "<C#>"` / `console --type error`. 순서: `AssetDatabase.Refresh()` → 컴파일 대기 → 프리팹 빌드 → 메뉴 `PitCrew/Art/Coastal Town/Rebuild Layout + Install`(저장된 깨끗한 ComplexTrack 필요) → `CoastalTownSmoke.Run()` 이 `art/source/coastal-town/play-verification.txt` 에 8 PASS + SUCCESS. `validation.txt` 의 Issues 가 비어야 한다.
4. **캡처** — 스모크가 `01/02/03_*.png` 를 덮어쓴다. 추가로 `ArtCapture.CaptureChaseAt(path,84,60,0,1600,900)` 로 `06_downtown_center.png`, 건물 한 채 정면·측면 클로즈업 `07_house_closeup.png`.
5. **기록** — `art/source/coastal-town/README.md` 끝에 "4차 — 건물 벽 단면 (Codex)" 절: 바꾼 것 / 안 한 것 / 못 본 것. 씬 저장. **git 커밋 금지.**

## 하지 말 것
- `CoastalTownDensity.cs` 수정 금지 (Claude 가 같은 시간에 2칸 폭 도로 쪽을 고치고 있다). `CoastalTownInstaller.cs` 는 asset 선택 한 줄만.
- 소품(Awning~Boat) 함수·아틀라스 칸 수정 금지. 추가만.
- 차량·카메라·배달·물리 코드 변경 금지. 새 스타일 제안 금지.
- 결과를 좋게 요약하지 말 것. 안 한 것·못 본 것은 그대로.
