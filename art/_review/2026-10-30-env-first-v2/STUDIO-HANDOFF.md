# 다음 촬영 담당자에게 — D-211 2판

**QA/프로그래밍 임포트 필요. 도현 승인 필요.** 이 과제는 Blender 검토까지이며 Unity를 열거나 씬을 저장하지 않았다.

1. 새 산출물은 `art/export/env-first-v2/manifest.json` 및 같은 폴더의 `_v02.fbx` 8개다. 원래8개터별한루트, 내부16채. 위치는 `pivot_xz`와`y`; 방향은 메시 내부에 포함하므로 터 루트에 추가 yaw를 넣지 않는다. 루트y는1판보다0.10m낮고 돌기단은0.9m이상이다.
2. `textures/`를 FBX와 함께 유지. 벽/트림/지붕/바닥은1판과동일PNG이고 `retain_stone.png`는현재축대사진을32px/m로축소한사본이다. GLB도8개 제공하지만 Unity 실제임포트는검증안했다. A7재질매핑은기존담당자가수행한다.
3. 1판모델과동시에두면겹친다. 기존`_v01`8개만촬영사본에서교체하고 이야기건물·현재배치·본맵은유지한다. 기존 `env_stage.py`는1판경로고정이므로 그대로실행하지 않는다. 이번에임포트용C#를실행/검증하지않았다.
4. `art/_review/2026-10-30-env-first/unity/sheet.png`의동일게임카메라에서1/2판비교. Blender시트는같은기존Blender카메라3장이고게임카메라재현을주장하지않는다.
5. 우선확인: 204696009 정비소/인접건물겹침, 모든돌기단실제맵접지, 계단2곳통행/문, 도로/마당연결, 가림,주행8초지글거림. site-audit의정비소외곽은원본FBX라현재Unity축/배치차이를해결했다고볼수없다.

증거: `readback.json`(기하), `reproducibility.json`(재생성), `site-audit.json`(검토공간), `rules.md`(재질미통과), `details.png`/`seams_*.png`(이음매). 전체명령과규격은 `art/models/env_first_v2/SPEC.md`.
