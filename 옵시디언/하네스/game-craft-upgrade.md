# Game Craft Upgrade — AI가 "이상함을 자연스럽게 느끼게" 만드는 엔지니어링

> 2026-05-20 도현 메타 요구: "세부적인 것까지 잘 만든다는 느낌, 당연히 이상함을 느꼈어야 할 부분에서 자연스럽게 못 만듦. 수단·방법 가리지 말고 고치는 법 찾아."
>
> 이 문서는 진단 + 7개 메커니즘 + 도구 사양. /loop으로 iterate.

---

## 1. 진단 — 왜 못 느끼는가

세션 27 헛짚음 6건 공통 패턴:
- spec/컴파일 통과 → "완료" 본능
- screenshot 0회 / Play 0회 / 비교 대상 0개
- "이거 사람이 보면 즉시 어색함" 감지기 부재

원인 5축:

| # | 결핍 | 사람이 자연스럽게 하는 것 | 내가 빠뜨리는 것 |
|---|---|---|---|
| 1 | 시각 루프 | 변경 후 즉시 보고 어색하면 다시 만짐 | spec 만족 → 끝 |
| 2 | Reference DB | 좋은 게임 100개 본 누적 감각 | 머릿속에 비교 대상 없음 |
| 3 | 자기 비판 | 자기 결과물 보면 즉시 이상함 발견 | "값 정상" 보고 끝 |
| 4 | 플레이어 시뮬 | "처음 보는 사람이면 어떻게 느낄까" 자동 가동 | 개발자 시야로만 봄 |
| 5 | 에르고 제약 | "버튼 너무 작다, 텍스트 안 보인다" 본능 | spec에 안 적혀있으면 무시 |

---

## 2. 메커니즘 7개 — 각자 결핍 1~2개 메움

### M1. **Mandatory Play Loop** (결핍 #1)

룰: **UI/카메라/플레이어 동작 변경 → commit 전 무조건 Play 10초 + screenshot.**

구현:
- Stop hook에 새 검사 추가: 변경된 파일에 UI/Camera/Player 키워드 있으면 → 최근 `unity-cli screenshot` artifact 존재 확인
- 없으면 commit 차단
- artifact 명 규칙: `docs/screenshots/auto/<commit-sha-short>-<feature>.png`

비용: commit당 +30초. 효과: hover too-small / tooltip far / car embedded 같은 버그 사전 차단.

### M2. **Reference Library** (결핍 #2)

룰: **각 시스템마다 "good example" screenshot 3장 + "bad example" 3장 매핑.**

구현:
- `references/internal/{good,bad}/<system>/` 폴더 (이미 일부 존재)
- 각 PR/feature 시작 시 해당 시스템의 good 3장 의무 Read
- 비교 체크: "내가 만든 게 good 톤에 가까운가? bad 톤에 가까운가?"

추가:
- `references/external/` Pacific Drive(트렁크) / Snowrunner(미니맵) / Hades(메타) / Death Stranding(배달) screenshots
- 도현 영역(자산 수집)이라 placeholder 디렉토리만 만들어두고 도현이 채움

비용: 초기 1회 셋업 + 시스템당 5분. 효과: PostFX 공포톤 같은 톤 미스 사전 감지.

### M3. **Ergonomic Constraints SO** (결핍 #5)

룰: **모든 UI는 `ErgoConstraints` SO 통과 의무.**

구현:
```csharp
// F1/Assets/Scripts/UI/ErgoConstraints.cs (신규)
[CreateAssetMenu]
public class ErgoConstraints : ScriptableObject {
    public int minClickTargetPx = 44;        // Apple HIG
    public int minTooltipDistancePx = 12;    // 너무 가까움
    public int maxTooltipDistancePx = 40;    // 너무 멈
    public float minContrastRatio = 4.5f;    // WCAG AA
    public int minHoverColliderRadius = 2;   // Unity world units
}
```
- 각 UI/Marker MonoBehaviour `OnValidate`에서 이 값과 비교 → 어기면 `Debug.LogWarning`
- Unity-CLI verifier에 "Console에 ErgoWarning 0건" 추가

비용: 첫 셋업 1시간. 효과: 핀 hover radius=0.45 같은 결정적 결핍 자동 검출.

### M4. **Ghost Player Replay** (결핍 #4)

룰: **각 빅 feature 끝에 30~60초 "고스트 플레이" 자동 시나리오 + screenshot 10장.**

구현:
- `Assets/Editor/_Recovery/GhostPlayer.cs` — Play 시작 → InputSimulator로 입력 주입 → 5초마다 screenshot → Result까지 흐름
- 입력 시퀀스 JSON `Assets/Editor/_Recovery/ghost-routes/job-1.json`
- 각 PR에 ghost 결과 첨부 의무 (commit body에 PNG paths)

비용: 첫 구현 4시간. 효과: 1잡 풀 흐름의 어떤 단계가 어색한지 자동 발견.

### M5. **자기 비판 prompt** (결핍 #3)

룰: **commit 직전 "이 변경을 처음 보는 사람이 5초 안에 어색하다고 느낄 게 뭐인가?" 자가질의 5문항.**

5문항 (체크리스트, no-compromise.md §10 후보):
1. 버튼 클릭 영역 손가락 안 들어갈 만큼 작지 않나?
2. 텍스트 배경과 contrast 충분?
3. 이 화면 처음 보는 사람이 "뭐 해야 되지" 5초 안에 알 수 있나?
4. 변경 전 screenshot vs 후 screenshot 옆에 놓고 봤을 때 후가 명확히 나아 보이나?
5. 변경 안 한 다른 부분이 이 변경 때문에 더 이상해 보이진 않나?

5개 다 답 못 하면 commit X.

비용: commit당 +2분. 효과: PostFX 공포톤이나 tooltip 멀어짐 같은 "느낌 미스" 사전 차단.

### M6. **External UX Reviewer** (결핍 #2 #3 보강)

룰: **시각/UX 변경은 별도 reviewer subagent (다른 시스템 prompt)로 검토 분리.**

구현:
- `subagents/ux-reviewer.md` — "당신은 게임을 처음 보는 플레이어다. 아래 screenshot을 5초 보고 첫인상을 한 줄로 적어라" 시스템 prompt
- screenshot 1장 + 한 줄 첫인상 + 의심 이슈 3개 보고
- 구현 subagent와 별도 — bias 차단

비용: feature당 +2분. 효과: 작성자(나) bias 회피.

### M7. **"왜 못 봤지" 포스트모르템 자동화**

룰: **도현이 manual play로 발견한 이슈 → 즉시 incidents.md + "어떤 휴리스틱이 있었으면 잡았을까?" 한 줄 추가.**

구현:
- Stop hook에 "도현 피드백 키워드 감지" 추가 (예: "이상함", "안됨", "왜이래")
- 감지 시 incident 템플릿 자동 열기
- "Missing heuristic:" 필드 강제

비용: 0 (자동). 효과: 패턴 누적 → 룰 승격.

---

## 3. Codex/2nd AI 활용 (도현 질문 답)

도현이 물었던 "codex 섞어 쓰기" — 메커니즘 M6 (UX reviewer)에 자연스럽게 들어맞음.

흐름:
- Claude = builder (지금 하던 것)
- Codex = M6의 UX reviewer 자리에 앉히기 (정말 다른 모델 시야)
- 같은 룰셋(`project_rules/`) 읽음
- diff + screenshot만 받아서 "첫인상 한 줄 + 이슈 3"

이게 모델 섞기의 진짜 가치 — 같은 자리 모델 2개 X, **다른 역할** 모델 2개.

---

## 4. 우선순위 (도현 자고 일어났을 때 즉시 시작 가능 순)

| 순위 | 메커니즘 | 구현 비용 | 효과 측정 |
|---|---|---|---|
| 1 | **M5 자기비판 5문항** — 룰만 박으면 됨 | 30분 | commit 다음 5건 |
| 2 | **M3 ErgoConstraints SO** | 1시간 | Marker/Button 추가 변경 시 |
| 3 | **M1 Mandatory Play screenshot** Stop hook | 2시간 | UI 변경 commit 시 |
| 4 | **M7 incident 자동화** Stop hook | 1시간 | 도현 피드백 발생 시 |
| 5 | **M4 GhostPlayer** | 4시간 | 빅 feature 끝 |
| 6 | **M2 Reference Library** | 도현 영역 — 자산 | 톤 변경 시 |
| 7 | **M6 UX Reviewer subagent** | 1시간 | 시각 변경 시 |

총 ~9시간 (도현 영역 제외) — 1일 sprint 분량.

---

## 5. 측정 — 이게 효과 있는지 어떻게 알까

before/after 추적:
- **선행 지표**: commit당 screenshot 첨부 비율, 자가 5문항 답변 비율
- **후행 지표 (한 세션 후)**: 도현이 manual play 후 "왜 이렇게 만들었어" 빈도, incidents.md "feel/UX" 라벨 incident 수
- 베이스라인: 세션 27 — UX/feel incident **6건**, screenshot 첨부 commit **3건 (57개 중)**.
- 목표: 다음 세션 — UX incident **≤2건**, screenshot 첨부 **≥70%**.

이 측정 자체가 하네스 효과 입증.

---

## 6. 한 줄 요약 (도현용)

"내가 만들 때 안 느끼는 어색함을 hook + SO + subagent로 강제로 느끼게 만듦. 사람이 자연스럽게 하는 5가지(보기·비교·자기비판·플레이어시뮬·에르고 본능)를 각자 메커니즘 1개씩 매핑."

---

생성: 2026-05-20 (도현 자는 동안 /loop 1차 iteration)
다음 iteration: 진단 subagent 결과 + 위 메커니즘 prototype 시작 검토
