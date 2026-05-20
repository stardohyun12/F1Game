# 화물 원물화 + 포장 의미 부여 — 설계 (2026-05-20)

## 문제

지금 화물은 포장 **전부터** 카드보드 박스(`CargoBoxAppearance`)라서, 포장 미니게임이
"박스를 박스로 만드는" 꼴 → 포장이 의미가 없다. 포장이 의미를 가지려면 그 전엔
**깨지기 쉬운 원물**(와인병·계란·생선·케이크)이 보이다가, 플레이어가 버블랩으로 싸서
**보호하는 게 눈에 보여야** 한다.

도현 결정 (브레인스토밍 2026-05-20):
- **B안 채택** — 원물 유지 + 랩핑 덧입힘 (원물→박스 변신 X).
- 원물 모델 = **Meshy AI 생성** (유기물엔 AI-3D가 강함, 이전 확인).
- 보호 비주얼 = **버블랩**(원물이 비쳐서 "보호됨"이 제일 잘 읽힘).
- 진행 = **1번 파일럿 먼저** (한 종류 끝까지 검증 → 되면 복제, UV 엉키면 2번 폴백).

## 현재 시스템 (변경 대상)

- `PackingScreen.SpawnItem` — 큐브 프리미티브 생성 + 카드보드 머티리얼 + WrapState/WrapPainter.
- `WrapPainter` — 메시 표면 레이캐스트 → `hit.textureCoord`(UV) → RenderTexture(`WrapState.WrapRT`)에 strip
  blit + 3D 리본 메시(`WrapRibbon`)를 곡면에 붙임. **UV 의존**.
- `WrapState.ComputeCoverage` / `QueryAbsorption` — WrapRT의 UV 픽셀로 커버리지%·손상 흡수 계산.
- `CargoShakeSimulator` — 트렁크 물리. 아이템을 자식으로 두고 슬라이드·충돌·손상.

**핵심 리스크**: Meshy 자동 생성 메시의 UV 전개도가 엉키면(병목·병바닥이 UV상 겹침)
커버리지·손상 계산이 깨진다. 큐브는 UV가 완벽해서 문제 없었음.

## 화물 매핑

| CargoType | 원물 | 단계 |
|---|---|---|
| Glass | 와인병 | **파일럿** |
| Eggs | 계란판 | 복제 (파일럿 통과 후) |
| Fish | 생선 | 복제 |
| Cake | 케이크 | 복제 |
| Box | 카드보드 박스 | 그대로 (Meshy 불필요) |

→ Meshy 모델 4개만 필요. 지금은 와인병 1개만 와이어.

## 아키텍처 — 콜라이더 2개 분리

핵심: **시각 메시 = 유기 원물 / 물리 = 볼록 프리미티브**. 오목 MeshCollider를 강체에 쓰면
PhysX concave 에러 (세션 24 버그) → 트렁크 물리는 절대 볼록 프리미티브로.

### CargoModelCatalog (ScriptableObject)
- `CargoType → { GameObject 시각프리팹, ColliderShape(Box/Capsule), Vector3 colliderSize }`.
- 코드 하드코드 금지 (asset.md). `Resources` 또는 SerializeField로 로드.
- Box 엔트리 = 시각프리팹 null → **기존 큐브+카드보드 폴백** (씬 안 깨짐).

### SpawnItem 개편
큐브 대신 카탈로그 조회:
1. 시각 프리팹 인스턴스 (원물 메시) — 자식.
2. 시각 메시에 **MeshCollider** (페인트용, UV로 `textureCoord`). 버블랩 리본이 이 곡면 따라 붙음.
3. 루트에 **볼록 프리미티브 콜라이더**(Box/Capsule, 카탈로그 크기) — 트렁크 물리용.
4. WrapState/WrapPainter는 시각 메시의 MeshCollider를 타깃으로 (`WirePainterFor` 조정).
5. 두 콜라이더 충돌 회피 — 페인트 MeshCollider는 트렁크 물리 레이어에서 제외 (또는 placement 진입 시 비활성).

### 버블랩 비주얼
- 이미 있는 `WrapRibbon`(채널0 "버블랩")이 곡면 레이캐스트로 달라붙음 — 추가 작업 거의 없음.
- 버블랩 룩 강화(반투명·버블 노멀)는 후속 폴리시. 파일럿은 기존 리본 머티리얼로.
- `WrapRT`는 보이는 게 아니라 커버리지%·손상 흡수 **부기**로만.

## 파일럿 검증 체크리스트 (와인병 1개 끝까지)

미지수를 5종 만들기 전에 1개로 깬다. unity-cli로 검증 (compile/play/screenshot/exec):

1. Meshy 와인병 import → UV 채널 존재 확인.
2. 포장 화면에 와인병 표시 + 버블랩 리본이 병 곡면 따라 감김 (screenshot).
3. 커버리지% 자연 증가 — **한 획에 80% 점프 = UV 겹침 신호** (실패 판정).
4. `QueryAbsorption`: 칠한 면 흡수>0, 안 칠한 면 0.
5. 트렁크 투하 → 볼록 콜라이더로 슬라이드·흔들림, **PhysX concave 에러 0**.
6. 손상: 안 싼 병 손상 > 싼 병 손상 (재현 시나리오).

## 폴백 분기 (3·4 실패 시 → 2번)

UV가 못 쓸 수준이면 **칠하는 콜라이더를 숨긴 볼록 프리미티브**로 교체:
- 와인병 = 숨긴 **실린더** 프록시(UV 깔끔 + 병 곡면과 근사)에 페인트.
- 원물 시각 메시는 프록시 위에 덮어씌움 (렌더만, 콜라이더 X).
- 리본은 프록시 곡면 따라 붙음 — 종류별 프록시 모양(병=실린더, 케이크=박스 등)을 카탈로그에서 골라
  밀착 손해 최소화.
- 정확도(커버리지·손상) 보장, 비주얼 밀착 소폭 양보.

→ 카탈로그에 `paintColliderShape` 필드를 미리 둬서 1번(=시각 메시)/2번(=프록시) 전환을 데이터로.

## 스코프 / 역할

- **지금**: Glass(와인) 1종만 와이어. Eggs/Fish/Cake는 파일럿 통과 후 복제 (각 Meshy 생성 + 카탈로그 엔트리).
- **내 영역**: Meshy 와인병 생성, CargoModelCatalog·SpawnItem 코드, 6단계 검증.
- **도현 영역**: 생성된 와인병 룩 승인 (보여주면 OK/재생성).
- **스코프 주의**: 세션 28 = 비주얼 폴리시 단계지만, 이건 단순 꾸미기가 아니라 **포장 메커니즘에
  의미를 주는 코어 수정**이라 정당. 단 파일럿이 폴백으로 빠지면 그 선에서 멈추고 복제 전 재평가.

## 검증 도구

- 컴파일: `unity-cli editor refresh --compile` → `console --type error,warning` 0건.
- 런타임: `editor play --wait` → `console --type error` 0건.
- 시각: Play 중 `menu "PitCrew/Capture/Auto Screenshot"` → 병+리본 육안 확인.
- 수치: `exec`로 커버리지% / QueryAbsorption / 콜라이더 종류 측정.
- 씬 wiring 변경 → 검증 서브에이전트 dispatch (session.md D-073).
