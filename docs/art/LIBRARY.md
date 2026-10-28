# 건물 라이브러리 (D-191)

배치는 도현이 한다. 팀은 건물 하나하나를 분위기 있게 뽑아서 여기에 올린다.
건물을 보는 곳은 `Assets/Scenes/Studio_Building.unity` 이다. 판정은 스튜디오 캡처 `docs/qa/evidence/studio-<이름>/sNN_sheet.png` 로 한다.

## 도현 배치 방법

1. `Assets/Art/Library/Buildings/<이름>/<이름>.prefab` 을 씬에 끌어다 놓는다.
2. 피벗은 **바닥 앞 가운데**이고 정면은 **+Z** 이다. 피벗을 길 가장자리 땅에 놓고, Y 회전으로 정면을 길 쪽에 맞춘다.
3. 재질은 그 폴더 안에만 있는 `M_<이름>_*.mat` 이다. 그래서 다른 씬의 재질은 안 바뀐다. 다만 같은 프리팹을 쓰는 곳끼리는 재질을 같이 쓴다.

## 목록

- 크기는 프리팹 BoxCollider 기준이다(소품 포함). 단위 m, 측정은 2026-10-28 `library_import.cs` 로그.
- 상태 값은 둘 중 하나: 후보 / 도현 통과.

| 이름 | 프리팹 경로 | 크기 W×D×H m | 피벗 | 상태 | 원본 export 폴더 | 메모 |
|---|---|---|---|---|---|---|
| room | `Assets/Art/Library/Buildings/room/room.prefab` | 3.50×3.16×2.40 | 바닥 앞 가운데(창 쪽 = +Z) | 후보 | `art/export/q1-room` | 창 안쪽이 보이는 단칸방. fbx 원점(바닥 가운데)을 z −1.66 옮김 |
| garage | `Assets/Art/Library/Buildings/garage/garage.prefab` | 13.47×16.41×5.77 | 바닥 앞 가운데(열린 정면 = +Z) | 후보 | `art/export/q2-garage` | UV0 통일판 `garage_v5_combo.fbx`. 옛 UV 패치는 안 씀. 앞마당 흙 `dirt_v5_combo` 는 안 넣음. 재질 7개 |
| depot | `Assets/Art/Library/Buildings/depot/depot.prefab` | 10.00×8.73×4.00 | 바닥 앞 가운데(fbx 원점 그대로) | 후보 | `art/export/q3-depot` | 본체 깊이는 7m, 앞 소품이 z +1.73 까지 나옴. decals 컷아웃 0.5 |
| postoffice | `Assets/Art/Library/Buildings/postoffice/postoffice.prefab` | 10.44×9.94×8.16 | 바닥 앞 가운데(fbx 원점 그대로) | 후보 | `art/export/q4-postoffice` | 2층. 앞 계단·우체통이 z +1.08 까지 나옴. 재질 6개(postbox_G1 포함) |

## 다시 만들기

- 스튜디오 씬은 `docs/qa/tmp/studio_build.cs` 로 처음부터 다시 만든다.
  - 땅 30×30m 이고 길 띠 4m 는 z 1.5..5.5 에 있다.
  - 재질 사본은 `Assets/Scenes/Studio_Building_Mats/` 에 있다.
- 라이브러리는 `docs/qa/tmp/library_import.cs` 로 만든다.
  - 순서: export 폴더에서 파일을 복사 → 임포트 설정 → 재질 → 프리팹 → `STUDIO_Slot/<이름>` 에 꺼진 채로 둔다.
  - 자세는 그 파일의 `POSE` 표에서 정한다.
- 캡처: `bash docs/qa/tmp/studio_cap.sh <이름> <번호> <설명>`
  - 카메라 4대: road(카트 눈높이 1.0, 40°, 20m) · front(8m) · 34(12m) · close(입구 3m).
  - `STUDIO_Slot/<이름>` 하나만 켜고 찍는다.
