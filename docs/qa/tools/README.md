# 재기동 QA 재사용 도구

## Spline M1 기하 경계 측정 (2026-10-20)

`spline-measure.py`의 M1은 `shoulder_boundary.py`를 사용한다. 월드 좌표를0.1mm 이내로 결합한 삼각형 메시에서 외곽 변을 찾고, 보도/골목의 실제 변과 접촉하는 안쪽 경계 및 끝막음을 제외한다. 분할 수·사각형 순서·정점 번호·면 방향을 사용하지 않는다. 가까운 다른 도로의 중심선 거리로 안/밖을 고르지 않는다. 판별 모호한 변은 `M1_unclassified_boundary`로 기록하며, 모호/비다양체 경계가 있으면 `classification_complete=False`이므로 M1 PASS로 판정하면 안 된다. 비정합 T접점이나 어깨와 도로의 접합 자체가 끊긴 메시에는 별도 검토가 필요하다.

검증: `python -X utf8 docs/qa/tools/test_shoulder_boundary.py`. 4개 테스트 그룹(분할1/4/8/16 × 회전·경사 변형, 가까운 다른 도로, 정점·삼각형 순서와 winding, 중복 뒷면, 저장 메시 순서 변형)이 통과했다. [로그](../evidence/m1-boundary-static-tests.txt).

기존 증거 보존 재계산: `python -X utf8 docs/qa/tools/spline-measure.py --input-dir docs/qa/evidence --output-dir docs/qa/evidence/m1-static-review`. 원래 씬 추출 데이터의 M1은30202표본/446위반/최대2.372737916m로 기존과 일치하고 다른 측정값 변화도 없다. [결과](../evidence/m1-static-review/spline-measurements.json). 이번 작업은 에디터 없이 **이전 저장 데이터**만 검사했으므로 수정된 게임 씬의 재검증이 아니다. BUG-003·005·007 상태 변경은 다음 토큰 재측정에서 한다. 다른 M항목의 기존 메시 해석 방식까지 순서 독립으로 바꾼 것은 아니다.

## BUG-002 재검증 추가 도구
후속 실행 확인(2026-10-19): 위 정적 수정의 런타임 미검증 상태를 해소했다. 동일 Play에서 유리 Plaza(8.5초/30m) 정산→Restart→생선 PierEnd(3.1초/10m) 모두 성공. 두 번째 QA_ROUTE는 현재(66.99,0,83.79), source=(11,14)에서 계산했다. 증거 `../evidence/dedup-continuous-runtime-2026-10-19.json` 및 `dedup-continuous-runs-2026-10-19.csv`. 이는 두 경로의 실제 실행 확인이며 전체 목적지/차량 방향/장애물 탈출 보증은 아니다. 기존 하드코딩 시기의 CSV와 분리한다.

중복 카드 재검증: Play 후 `bash docs/qa/tools/exec-file.sh docs/qa/tools/dedup-board.cs.txt`. 지갑을 최소2500으로 조작한 뒤 Open을20회 호출하여 실제 카드의 OrderJob 참조·화물·제목을 집계한다. 20회는 한 exec 안에서 동기적으로 실행하며 마지막 보드만 화면 캡처한다.

2026-10-19 후속 정적 수정: 세 drive 스크립트는 현재 차량 위치의 최근접 도로 셀에서 목적지까지 roadMask 4방향 BFS를 계산하고 직선 구간을 압축한다. 공통 원문은 `current-position-route.cs.txt`이며 세 exec 파일에 같은 블록을 포함한다. 게임 경로 UI는 현재 차 기준 목적지 순서/직선을 계산하므로 주행 경유점 생성기로 직접 재사용하지 않았다. 대신 기존 TrackLayout과 TileMapAuto.CellToWorld 매핑을 재사용한다. 현재 ComplexTrack 전용이며 도로에서6m 이상 이탈/경로 없음은 실패 반환한다. 장애물 회피·후진 탈출 기능은 없다. **최초 수정 시점에는 정적 확인만 했다. 이후 두 경로 실행 확인은 위 후속 기록 참조. 최초 성공 CSV는 수정 전 도구의 결과다.**

`exec-file.sh`로 `bug-002-*.cs.txt`를 Play에서 실행한다. board는 8회 재오픈하여 실제 카드 텍스트를 기록한다. boundary는 지갑 0에서 999/1000/2499/2500으로 AddReward 이벤트를 발생시키는 명시적 테스트 조작이다. 경계 검사 뒤 Stop/Play로 초기화한다.

select는 유리(Plaza), select-fish는 생선(PierEnd)이 나올 때까지 최대30회 보드를 열고 실제 카드 클릭 이벤트를 호출한다. BoardGoButton → Suggest → Confirm 버튼 이벤트 후 drag, BtnGo 후 drag, 마지막 drive 순서다. drive는 BtnGo로 출발하고 프레임별 합성 키를 넣으며 실제 물리/배달 트리거를 사용한다. 스크린샷을 주행 중 찍으면 프레임 표본이 오염되므로 FPS 비교에서는 제외한다. 평균 FPS=1000/meanMs, 최저 순간 FPS=1000/maxMs; 릴리스 성능 보증이 아니다.

에디터 토큰 필수. 저장소 루트에서 실행. 게임 코드/씬 저장 없음.

1. 기존 persistentDataPath/runs.csv를 별도 보관한다. 필요하면 `PitCrew/Metrics/Reset runs.csv` 메뉴로 초기화한다.
2. Play가 꺼져 있을 때 `bash docs/qa/tools/synthetic-run.sh Depot` 실행. FactoryA/ContainerYard도 지원한다. ContainerYard는 포장·배치 드래그를 추가한다.
3. 스크립트는 드라이버 등록 후 종료한다. Result와 실제 CSV를 확인한다. 드라이버는 최대90초 후 키를 놓으며 정산을 강제하지 않는다. 멈춤/실패 시 임의로 성공 CSV를 만들지 않는다.
4. 다음 런 전 `bash scripts/ucli.sh editor stop`. 각 런은 Play 재진입으로 초기화한다.
5. **마지막에는 입력 설정 원복**: exec로 `UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus; return 1;` 실행 후 Stop. 설정/씬 저장 금지. 콘솔·isPlaying·scene.isDirty 확인 후 토큰 반납.

합성 입력 정책·한계는 ../evidence/restart-method-notes-2026-10-19.md 참조. UI 이벤트를 호출하며, 실제 OS 입력 사용성 검증을 대체하지 않는다. 모든 경로는 현재 씬의 roadMask/초기 주문에 한정한다. 스크립트 실행 중 파일을 편집하지 않는다.
## Spline §2.9 재판정 (2026-10-19 실행 / 2026-10-20 이터레이션)

새 씬 추출본은 `../evidence/spline-section29/`에 보존했다. 기존 증거는 덮어쓰지 않는다.

- `spline_criteria.py`: 스펙 §2.9를 읽어 높이 배율·경사·깎기 상한·경고 비율·변경 지형 분모·절대 상한을 결정하고 SHA256 기록. 문구/구조 변경으로 파싱 실패하면 중단한다.
- `python docs/qa/tools/spline-measure.py --input-dir docs/qa/evidence/spline-section29 --output-dir docs/qa/evidence/spline-section29`: 실제 씬 메시 재측정. M7 분모는 원DEM 대비 변경≥0.1m인 지형 정점이다. 길 중심 표본은 참고치만 기록한다.
- `python docs/qa/tools/spline-disc-grade.py docs/qa/evidence/spline-section29`: 교차점 삼각형 평면 기울기 검증.
- `python docs/qa/tools/test_shoulder_boundary.py`: 분할 수·순서·와인딩·양면·기존 씬 변형 회귀4그룹.

도구 변경은 기존 독립 QA 방법을 재사용했다. 새 라이브러리 설치 없음. 결과와 분류 메타데이터 의존/OSM 이탈 미검증 등의 한계는 `../GATE-2026-10-20-spline-town.md` §QA 최신 항목 참조. 씬 추출/회귀는 에디터 토큰 필요; 위 Python 정적 재계산은 불필요.
## Spline 3차 QA

`../evidence/spline-third/`는 clipA/clipB와 교차점 부채, 진입로가 추가된 저장씬의 독립 증거다. 이전 출력 폴더를 덮어쓰지 않는다.

- `spline_geometry.py`: 절단 호 길이 어댑터(작성자 포맷 참고), 모든 사각형 양 끝 좌표/개수 검증, 실제 아스팔트 삼각형0.1mm 용접 연결. `test_spline_geometry.py`의5그룹으로 잘못된 메시·근접 거짓연결·겹침면적 경계 검증.
- `spline-disc-grade.py <증거 폴더>`: 교차점 판의 실제 삼각형 경사, 인덱스·3정점·면적 출력.
- `spline-radius-audit.py <증거 폴더>`: §2.3 연속 반경<30m 굽이의 방향차≥120°로 헤어핀 전체 예외 판정. 고정40m 창과 작성자 hairpin 라벨은 참고치.
- `spline-third-extra.py <증거 폴더>`: 기본 측정도 실행한 뒤 진입로293개의 실제 상면·접점·하단·투영 겹침 검사. `driveway-check.json`에 참조 표면 없는 null 표본도 보존하며 결함으로 세지 않는다.
- `spline-osm-distance.py <증거 폴더>`: 같은 highway 최근접 OSM 거리의 보수적 하한 검사. 원래 대응 way를 보장하지 않으므로 위반0을 정식 PASS로 쓰지 않는다.
- `spline-own-contact.py <증거 폴더>`: 자기 집 하부 경계와의 겹침 후보를 실제 교차 다각형 높이로 확인한다. 중심점이 교차부 밖일 수 있으므로 중심 높이를 침투 깊이로 쓰지 않는다. 3차의4후보는0~2.14mm 접합으로 결함 아님.

라이브러리 추가 설치 없음. `convex_clip.py`는 SAT 후보의 실제 교차 면적을 계산한다. 진입로의 의도된 도로 접합15cm 띠를 제외하고0.001㎡ 이상만 겹침 후보로 남긴다. 동작 재현은 게이트3차, 한계는 해당 §미검증 참조. 일반 Python은 토큰 불필요, C# 추출·캡처·회귀는 에디터 토큰 필수.
