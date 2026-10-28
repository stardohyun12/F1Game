# 건물 뷰어 (Unity 없이 브라우저로 건물 둘러보기)

## 도현: 여는 법
1. git-bash 에서 `bash tools/viewer/serve.sh` (창은 켜 둔 채로)
2. 브라우저에서 **http://127.0.0.1:8770/** — 특정 건물 바로 열기: `http://127.0.0.1:8770/?b=garage` (room · garage · depot · postoffice)
3. 다 보면 serve 창에서 Ctrl+C

## 조작
| | |
|---|---|
| 왼쪽 목록 | 방 · 정비소 · 택배 보관소 · 우체국 |
| 둘러보기 | 왼쪽 드래그 = 돌기, 오른쪽 드래그 = 옮기기, 휠 = 확대/축소 |
| 걷기 | 화면 클릭 → 마우스 잠김. WASD 이동, 마우스 보기, Shift 달리기, Space 위 / C 아래, Esc 풀기. 눈높이 1.6m, 벽은 통과됨 |
| 그림자 | 실시간 해 그림자 켬/끔 |
| 해 방향 | 해의 방위(높이는 30° 고정) |
| 픽셀 (720) | 켬 = 게임처럼 1280×720 을 Nearest 로 확대, 끔 = 모니터 원래 해상도 |
| 새로고침 | 지금 건물 glb 를 캐시 없이 다시 읽음(시점 유지) |

빛은 게임 A7 근사(따뜻한 낮은 해 + 하늘색 주변광 + 3단 계단 명암 + 40m~ 안개)라 Unity 화면과 완전히 같지는 않습니다. 최종 판정은 Unity `Preview_Candidates` 로.

## AI 팀원: 에셋이 바뀌었을 때
1. `art/export/...` 의 FBX/PNG 를 새로 납품한 뒤 `bash tools/viewer/export_all.sh` (하나만: `bash tools/viewer/export_all.sh depot`)
   - 결과 `tools/viewer/assets/<이름>.glb` + `manifest.json`, 로그에 재질별 텍스처·삼각형 수가 찍힘
2. 도현 브라우저에서 **새로고침** 버튼 (페이지 F5 도 됨)
3. 새 건물 추가 = `tools/viewer/buildings.json` 에 한 줄(name, label_ko, fbx, tex_dir, textures: 재질 이름 → png). 재질에 이미지가 없으면 이 표로 붙이고, 모든 텍스처는 Closest(Nearest)로 내보냄.

검사: 서버를 켠 채 `node tools/viewer/_check/cdp_check.js` (헤드리스 Edge 로 로드·새로고침·모드·픽셀 토글·걷기 이동 확인).
three.js 는 `vendor/` 에 r170 고정(인터넷 불필요).
