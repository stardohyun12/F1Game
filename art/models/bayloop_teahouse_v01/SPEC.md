# 언덕 찻집 / PD 20261026-0155

| 파일 | 치수·피벗 | 역할 |
|---|---|---|
| teahouse_01.fbx / glb / blend | 본체 6.4 × 6m, 층고 3.05 + 2.6m, 바닥 앞벽 가운데·앞면 +Z | 물러난 위층, 옥상 테라스, 테라코타, 아치, 셔터 |
| tea_table_01.fbx / glb / blend | 상판 0.8 × 0.66m, 상판 0.765m, 의자 좌면 0.45m; 바닥 중심 | 2인용 사용 중 찻상 |
| tea_tray_display_01.fbx / glb / blend | 선반 0.76 × 0.49m, 0.85m; 바닥 중심 | 쟁반·잔 6개·차통·접은 닦개 |
| tea_owner_bicycle_01.fbx / glb / blend | 기존 실물 비례 자전거 + 뒤 짐받이; 바닥 중심 | 주인 앞바구니 자전거 |

정확한 export bounds와 tris는 `manifest.json`. 소품 모두 800 tris 이내. 전체 32px/m, 공유 1024² `TeaHousePhotoAtlas.png`, Point/Nearest. UV 면마다 metric projection, 큰 면은 252px 내부 단위로 절단. 정보층 `찻집` 역시 32px/m이고 고밀도 예외 없음. 글자는 Galmuri11 SIL OFL1.1: `art/_review/2026-10-23-course-ui/fonts/Galmuri-OFL.txt`.

## 원천과 변경

`bayloop_buildings.py`, `bayloop_italy_buildings.py`, `bayloop_italy_props.py`의 검증된 geometry·metric UV·수출을 재사용했다. 원본 조건의 독립 재현이라고 주장하지 않는다. 변경은 새 찻집 비례, 후퇴한 위층, 3개 구체물, 공용 팔레트 내 갈색 목재 색 사다리, 실제 폰트 간판이다. `bayloop_teahouse_textures.py`는 기존 CC0 사진 기초 아틀라스의 명암 구조를 유지하고 팔레트 사다리만 변경한다. 랜덤 노이즈·AI 생성 이미지 0.

사진 원본 URL/다운로드 기록: `manifest.json.source_manifests`; 새 파생파일 해시/URL: `provenance-rows.json` (root가 중앙 장부에 병합). 원본 albedo 팔레트 검사와 렌더 화면 색은 별개: 조명 후 화면을 32색으로 재양자화하지 않는다.

## 일본 생활 층과 이탈리아 형태

| 요소 | 층 | 이유 |
|---|---|---|
| 밝은 회벽·아치·녹색 셔터·철 난간·물러난 위층·테라코타 | 이탈리아 형태 | 아트라니 공식 관광 사진/설명의 아치·포개진 집 문법 |
| 작은 한글 찻집 간판·전기계량기·배수관·화분 | 눈높이 생활 층 | 관광용 장식보다 매일 쓰는 손때 있는 가게 |
| 나무 찻상·쟁반 진열·주인 자전거 | 기억 속 구체물 | 받는 사람의 습관과 머무르는 자리를 암시 |

공식 비례 참고 https://atraniturismo.it/contenuti/2164308/borgo-atrani 및 https://www.italia.it/en/campania/salerno/atrani-places-to-visit . 사진은 게임에 넣지 않음. 문·층 높이는 사진 추정과 일반 실물 비례의 제작 가정이며 실측 건물 복제 아님.

## 배치 인계

Blender의 수취인 앵커 `(0.6,-1.0,0.1)` → Unity `(0.6,0.1,1.0)`. 1.2 × 1.2m 빈 접근 공간을 문 앞에 남김. 실제 NavMesh/콜라이더/수취인 상호작용은 미실행. 상판·의자에 사람을 배치할 때 별도 Seat anchor 생성 필요.

`review_scene.blend`는 소품 3종 조합을 보여주는 검토 전용이고 게임 복사 금지. `arrival.png`/`arrival_close.png`는 실제 bpy 렌더, 인게임 배치 증거가 아니다. `tut_4_arrive_tea.png`는 파일명과 달리 상가 복귀거리이므로 톤만 참고했다.

재생성: `py -3.13 tools/asset-gen/bayloop_teahouse_textures.py`, `D:/Tools/Blender/blender.exe --background --python tools/asset-gen/bayloop_teahouse.py`.

도현 판정: 구체물 세 가지 조합·찻집 정체성. QA/프로그래밍 임포트 필요. Unity 실행/씬 수정/커밋 하지 않음.
