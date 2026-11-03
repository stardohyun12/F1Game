[EDITOR][World 층 1단계 — 장소·주민·주문을 명세(YAML)에서 데이터로 (D-289)]
docs/ARCHITECTURE.md 2층 World 를 채운다. 여는 층·파일: 새 `world/world.yaml`, 새 `F1/Assets/Scripts/World/`(PitCrew.World.asmdef, Core·Signals 만 참조), 새 `tools/world/gen.py`, 생성된 `F1/Assets/World/*.asset`. 그 밖(단계·물건·화면·씬 배치)은 손대지 않는다. 시간 상자 60분 — 끝나면 바로 마무리, 못 한 건 보고에.
1. `world/world.yaml` — SPEC.md §3 장소(이름·역할·발 좌표·yaw — 지금 WORLD.md 스냅숏 값 그대로)·§4 주민(이름·자리·역할 한 줄)·§5 물건(화물 종류)·첫 주문 1건(받는 사람 줄리아·물건·보수 1200·배달지 DP_tea)·§7 수치(수리 300·파손 80/40·배율 1/0.7/0.3·노을 60초·달력 28). 사람이 읽는 글은 SPEC.md 에 남고, **구조 있는 값은 yaml 이 원천**. SPEC.md 해당 표 위에 "원천: world/world.yaml" 한 줄.
2. `tools/world/schema.json` + `gen.py --check`: yaml 형식 검사(필수 키·좌표 숫자·이름 중복·배달지가 장소 목록에 있음). 틀리면 줄 번호와 함께 실패.
3. `gen.py` → ScriptableObject 에셋: `WorldPlaces.asset`·`WorldPeople.asset`·`WorldOrders.asset`·`WorldNumbers.asset` (클래스는 `Scripts/World/*.cs`, 에셋은 에디터 exec 로 생성·저장, 손으로 .asset 쓰지 않음). 재실행하면 같은 guid 로 덮어씀.
4. 이어 붙이기 **최소 2곳만**(동작 불변): `Core/Rules.cs` 수치 → `WorldNumbers` 에서 읽기(Core 는 엔진을 모르니 Core 는 순수 값 그대로 두고, Stages 가 시작 때 WorldNumbers 값을 Core 에 넣는 식) · `DebugPlaces` 장소 목록 → `WorldPlaces` 에서. 나머지(주민·주문 사용처)는 목록만 보고에.
5. 불변 조건 1개 추가: "world.yaml 장소 좌표와 씬 DP_/DebugPlaces 좌표 차 ≤ 0.5m" — 어긋나면 어느 쪽이 틀렸는지 좌표와 함께.
확인: `python tools/world/gen.py --check` 통과 · 컴파일 0 · Core 테스트 17 · world_check 통과(새 조건 포함) · DayLoopCheck 전후 SAME · 캡처 1장(디버그 창 장소 목록이 에셋에서 온 것). 보고 4줄 + CODEMAP.md 에 World 절 추가 + yaml 줄 수. 씬 저장.
