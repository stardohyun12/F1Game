# 브리프: 로파이 해안 마을 — 건물 다양성 (2026-10-13, 밤)

## 도현의 한 줄 (합격 기준)
> 이 느낌(네 4차 변형 7종, `codex4_08_building_variants.png`)으로 다양하게 뽑아 봐라. 단, 지금 TEA·MART 는 **책방 건물에 간판만 바꾼 것**처럼 보인다 — 가게는 가게 종류가 정면에서 읽혀야 한다.

## 바탕
- 네 4차 결과가 채택됐다(graphic-gate 7/10 PASS, `docs/gates/2026-10-13.md` 19:52). 지금 씬에 네 판이 설치돼 있다(2752 배치, 스모크 8/8). 그 위에서 이어 간다.
- 같은 규칙: `tools/asset-gen/lofi_pilot.py`, 아틀라스 64px 칸·Point·2m UV, 팔레트 칸은 끝에만 추가, 기존 함수의 rng 순서 보존. 소품(Awning~Boat)·`CoastalTownDensity.cs`·`CoastalPropsBuilder.cs` 는 손대지 않는다.
- 몸통 치수 규칙은 **이번엔 풀린다**(실루엣이 달라야 하므로). 대신 새 치수를 `CoastalTownDensity.Body()` 가 알아야 한다 — Density 는 Claude 가 고치니, 네가 만든 건물마다 **(이름, 폭 w, 깊이 d, 층수, 정면 문 x)** 를 `art/source/coastal-town/building-catalog.json` 에 적어라. 바닥 원점·앞=-y(Unity +z)·벽걸이 띠(1.9~2.5m) 규칙은 유지.
- 삼각형 예산: 건물당 **4,500 이하**(지금 3,000~4,100). 넘으면 창살·장식부터 줄인다.

## 해야 할 것
1. **실루엣이 다른 집 3종 추가** (기존 3종 유지): ① 좁고 높은 3층집(폭 4, 깊이 4.8, 지붕 급경사) ② 단층 오두막(폭 6, 깊이 4.4, 큰 지붕·처마 깊음·현관 포치) ③ 모서리가 깎인 L자/모서리집(폭 6, 깊이 6, 한쪽 날개). 지붕 형태(박공/모임/한쪽 경사)와 색이 서로 달라야 한다.
2. **가게는 종류가 읽히게** — 지금 CornerShop/B 를 "같은 몸통에 간판"에서 벗어나게:
   - **MART**: 정면에 과일·채소 상자 진열대(가로로 긴 선반 2단), 냉장 쇼케이스 유리, 벽에 포스터 2~3장, 입구 위 큰 간판, 출입문은 넓은 유리문.
   - **TEA(찻집)**: 작은 창 격자(종이창 느낌), 입구 옆 둥근 테이블 2 + 의자, 처마에 등(랜턴) 2~3개, 간판은 세로로 매단 나무판 + 찻주전자 아이콘(픽셀), 차양은 천이 아니라 나무 처마.
   - 가능하면 **BAKERY** 한 종 더(빵 진열창, 굴뚝에서 연기 없이 굴뚝만 큼, 줄무늬 차양). 이름은 `CornerShop`(MART) / `CornerShopB`(TEA) / `CornerShopC`(BAKERY) 로.
3. **정비소** 2종 유지, 지붕 색 하나는 바꿔라(회색 일색 탈출).
4. **배치**: `CoastalTownInstaller.Layout()` 의 asset 선택에서 새 집 3종이 hash 로 섞이게(집 6종 균등, 상점 3종, 정비소 2종). `scale` 줄은 종류별 기본 .62~.64 유지. 새 집의 치수가 다르니 **`CoastalTownDensity.Body()` 는 Claude 가 catalog.json 으로 맞춘다** — 네가 설치를 돌릴 때는 Density 가 옛 치수를 쓰므로 옆 소품이 조금 어긋나도 무시하고 진행, README 에 적어라.
5. **검증**: Blender 생성 → `AssetDatabase.Refresh()` → 컴파일 대기 → `PitCrew/Art/Lofi Pilot/Build Building Prefabs` → `PitCrew/Art/Coastal Town/Rebuild Layout + Install` → `CoastalTownSmoke.Run()` 8 PASS + SUCCESS, `validation.txt` Issues 비어 있음. Unity 는 켜져 있다(8090). 네 `verify_building_faces.py` 는 새 치수에 맞게 고쳐서 돌려라.
6. **캡처**: 스모크 01~04 + `06_downtown_center.png`(`ArtCapture.CaptureChaseAt(path,84,60,0,1600,900)`) + 변형 전체 모음 `08_building_variants.png`(네 Capture Building Faces) + 가게 3종 정면 근접 `10_shops_front.png`.
7. **기록**: `art/source/coastal-town/README.md` 끝에 "5차 — 건물 다양성 (Codex)" 절. 씬 저장. **git 커밋 금지.**

## 하지 말 것
- 간판·색만 바꾼 변형. 가게는 진열·가구·창 형태가 달라야 한다.
- 차량·카메라·배달·물리·도로 코드 변경. 새 스타일 제안.
- 결과를 좋게 요약하지 말 것. 안 한 것·못 본 것은 그대로.
