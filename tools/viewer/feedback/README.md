# 뷰어 코멘트 (도현 → AI 팀원)

도현이 건물 뷰어(`http://127.0.0.1:8770/`)에서 물건을 고르고·옮기고·코멘트를 보내면 여기에 쌓인다.

- `feedback/<건물>.jsonl` — 한 줄 = 코멘트 하나 (오래된 것부터). 건물 = `room` · `garage` · `depot` · `postoffice`
- `feedback/shots/<id>.png` — 보낼 때 화면(1280×720, 고른 물건은 노란색 강조, 이동 화살표는 숨김)
- 서버가 켜져 있으면 `curl "http://127.0.0.1:8770/api/feedback?b=garage"` 로도 같은 목록(JSON 배열)

## 필드

| 필드 | 뜻 |
|---|---|
| `id`, `time` | `<건물>-YYYYMMDD-HHMMSS-xxxx`, 도현 PC 시각(+09:00) |
| `comment` | 도현이 쓴 말 그대로 — **이게 지시다.** 나머지는 위치를 찾기 위한 근거 |
| `node` | (마지막으로) 고른 물건의 glTF 노드 이름 = **Blender 오브젝트 이름 = Unity 프리팹 자식 이름**. `null` 이면 "화면 전체"에 대한 코멘트 |
| `mesh` | 여러 재질인 노드는 three.js 가 재질별로 쪼갠 메시 이름(`<node>_1` 등) — 참고용 |
| `node_extras` | 합쳐 납품된 메시를 뷰어가 부품으로 나눈 경우만: `split_from`(원래 오브젝트 이름), `part_index`, `materials`, `center_blender_m`, `center_gltf_m` |
| `material`, `texture`, `texture_size` | 찍은 면의 재질 슬롯 이름, 텍스처(이미지) 이름, 크기 px |
| `uv` / `uv_blender` / `texel` | 찍은 점의 UV. glTF UV 는 v 가 아래로(왼쪽 위 원점) → Blender UV = (u, 1−v). `texel` = 이미지 픽셀 (x, y), **왼쪽 위 원점** — 텍스처를 고칠 자리 |
| `point` / `point_blender` | 찍은 점, m. 선택이 없으면 화면 가운데가 맞은 곳(`point_source: "view-center"`) |
| `camera` | 그때 시점 `pos`, `target`(m), `mode`, `fov` — 같은 화면을 다시 볼 때 |
| `transform_delta` | 고른 물건을 옮겼을 때만(아니면 `null`). 아래 참고 |
| `nodes` | 고른 물건 **전부**(Shift/Ctrl+클릭으로 여러 개) `[{node, material, texture, point, transform_delta, extras}]`. 여러 개를 같이 옮기면 묶음 가운데 둘레로 돌기 때문에 각자의 `transform_delta` 가 다르다 — 각 노드에 자기 값을 적용 |
| `moved_nodes` | 이 탭에서 옮겨 둔 모든 물건의 `{노드 이름: transform_delta}` (고른 것 말고도) |
| `view` | 픽셀(720) 켬 여부, 해 방위, 그림자 |
| `screenshot` | `shots/<id>.png` (이 폴더 기준) |

### 좌표 (중요)
- `point`·`camera`·`transform_delta.pos`·`pivot` = **건물 glTF 좌표**: m, **Y-up**, 원점 = FBX 원점(뷰어가 화면에서 건물을 땅·가운데로 옮겨 그리는 것은 빼고 저장).
- Blender(Z-up) 로 바꾸기: `(x, y, z)_glTF → (x, −z, y)_Blender`. `*_blender` 필드에 이미 바꿔 둠.
- Unity: 뷰어 정면 +Z = Unity 프리팹 정면 +Z. FBX 임포트는 보통 x 부호만 반대 `(−x, y, z)` 이지만 프리팹 축 설정에 따라 다를 수 있으니 **한 번 옮겨 보고 방향을 확인**한다.

### transform_delta
| 키 | 뜻 |
|---|---|
| `pos` / `pos_blender` | 원래 대비 이동, m (월드 축). 뷰어 이동 단위 0.05 m |
| `angle_deg`, `axis` / `axis_blender` | 원래 대비 회전: 축(단위 벡터) 둘레로 몇 도. 회전 중심 = `pivot` |
| `rot_euler_xyz_deg` | 같은 회전의 glTF 축 오일러 XYZ(도) — 한 축만 돌렸으면 이걸 보면 됨(glTF Y 축 = Blender Z 축) |
| `pivot` / `pivot_blender` | 그 노드 원점(회전 중심). 나눠진 부품(`_pNNN`)은 부품 상자 가운데 |
| `local_now`, `local_orig` | 부모 기준 position / quaternion(x, y, z, w) 원값 |

## 적용하는 법
1. `comment` 를 읽고 `shots/<id>.png` 를 연다 (`camera` 로 뷰어에서 같은 시점 — 오른쪽 목록을 누르면 그 시점으로 감).
2. `node` 로 Blender 오브젝트 / Unity 프리팹 자식을 찾는다.
   - `node` 가 `<이름>_pNNN` 이고 `node_extras.split_from` 이 있으면: 원본은 한 메시(`split_from`)로 합쳐져 있다 → `center_blender_m` 근처의 연결된 면 묶음이 그 물건이다(Blender 편집 모드에서 그 점 근처 면 하나 고르고 Ctrl+L). `_pNNN` 번호는 같은 FBX 에서만 같다.
3. 옮겼다면 `transform_delta.pos_blender` 만큼 이동, `axis_blender`·`angle_deg` 만큼 `pivot_blender` 둘레로 회전. 텍스처 지적이면 `texture` 의 `texel` 근처.
4. 다시 내보내기(`bash tools/viewer/export_all.sh <건물>`) → 도현은 뷰어에서 새로고침.
5. 처리한 코멘트는 보고서에 `id` 와 함께 적는다(이 파일은 지우지 않는다).
