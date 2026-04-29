# 리뷰 질문 / 메모 parking lot

> **사용법:** 코드 보다가 의문/이상한 점/개선 후보 발견하면 즉시 미해결에 한 줄 적어. 답 찾으면 해결됨으로 옮김.
>
> 형식: `- [ ] (파일:라인) 한 줄 질문 — 컨텍스트`

---

## 해결됨 ✓

- [x] **(RunController.cs + DeliveryManager.cs) Update가 두 곳에 있어도 OK?**
  - 답: 책임 분리 명확 (RC=시간, DM=거리). 중복·race·강결합 없음. UnityEvent로 느슨한 결합. **분리 OK.**
  - 단, DeliveryManager.FindActiveCar()의 `FindFirstObjectByType`가 매 프레임 호출 → **Awake 캐시 권장**.
  - 결정: 적용 여부 도현 결정 필요 (아래 미해결로 이동).

---

## 미해결 — 도현 결정 필요

- [ ] **(DeliveryManager.cs) FindActiveCar 캐시 적용?**
  - 현재: returning 상태 매 프레임 `FindFirstObjectByType<ArcadeCarController>()` (씬 전체 스캔)
  - 제안: `Awake`에서 1회 캐시 + null 시 fallback
  - 위험: 0 (동작 동일, 성능만 개선)
  - 결정: yes / no / 나중에

- [ ] **(PreRunSetupPanel.OnCancelClicked) timeScale 복귀 누락?**
  - 흐름: 사용자 카드 클릭 → OB.OnCardClicked → PSP.Show (이때 timeScale=0 유지)
  - PSP에서 취소 → OB.Open 재호출. 하지만 OB.Open은 timeScale 안 건드림. 이미 0이라 문제 없을 수 있지만, **재진입 케이스에서 확실히 0?**
  - 검증 방법: OB.Open 시점 직전에 `Debug.Log(Time.timeScale)` 임시 추가.
  - 추정: OK (RunController.OpenBoard에서만 timeScale 0 set, BeginRun에서 1로). PSP.Show는 timeScale 안 건드리므로 0 유지.

- [ ] **(OrderBoard.cs) panelRoot 토글 vs 카드 destroy/spawn 불일치?**
  - 패널은 SetActive(false/true) 토글
  - 카드는 매번 Destroy + Instantiate
  - 일관성 위해 카드도 활성/비활성? — 단, currentOffer가 매번 셔플되므로 N개 카드 미리 만들어두는 게 어색. 현재 패턴이 자연스러움.
  - 결정: 그대로.

- [ ] **(PreRunSetupPanel.SpawnCardsIfNeeded) 카드 4개 영구 보존**
  - 첫 Show 시 인스턴스화, 이후 Hide/Show 반복해도 destroy 안 함 (cardsSpawned flag)
  - 메모리 사소함. 단, 나중에 카드 설정 변경 시 prefab만 바꿔도 런타임에 반영 안 됨.
  - 트레이드오프: prefab 디자인 변경 후 Play 재시작 필요. 정상.

- [ ] **(PackingCardUI.cs) wired flag로 중복 와이어링 방지**
  - Bind가 두 번 호출되면 첫 번째 호출의 onClicked가 사라지고 두 번째만 남음 (RemoveAllListeners + AddListener)
  - 의도된 동작? — 현재 사용 흐름상 Bind는 1회만 호출됨. 단, "wired는 첫 호출에 true로만 set, 이후 Bind 시에도 onClicked 갱신 안 됨"이라 onClicked 콜백 변경 불가.
  - 위험: 낮음 (PreRunSetupPanel은 onCardClicked 콜백 한 종류만). 그러나 코드 의도 약간 모호.

- [ ] **(OrderBoard.cs) 정적 _hoverTarget**
  - 단일 인스턴스 가정. 멀티 OrderBoard가 씬에 있으면 마지막 Awake가 winner.
  - 현재 사용에선 OK. 멀티는 스코프 밖.

- [ ] **(RunController.BeginRun) cargo null 체크는 있는데 deliveryManager null 체크 패턴 불균등**
  - 일부 호출은 null 체크, 일부는 안 함. 일관성 검토 가치는 있음.
  - 우선순위 낮음.

---

## 미해결 — 도현 직접 확인 필요 (Play 모드)

- [ ] **(scene) UIRoot/PreRunSetupRoot/Panel 시각 디자인 폴리시**
  - procedural에서 가져온 좌표 그대로. 직접 prefab/scene에서 픽셀·폰트·색 조정 필요.
  - prefab 모드: `Assets/Prefabs/UI/PackingCard.prefab`
  - scene: `UIRoot/PreRunSetupRoot/Panel/` (활성화 임시로 toggle 후 Scene view 확인)

- [ ] **(scene) UIRoot/OrderBoardPanel/OrderCard 시각 디자인 폴리시**
  - prefab: `Assets/Prefabs/UI/OrderCard.prefab`
  - 현재 fontPrimary 적용됐는지 한글 표시 확인

- [ ] **(scene) StatRow0~5 안 Label/Value 위치 적절한가?**
  - Label: x=0~120 (좌측), Value: 우측 정렬, 둘 사이 10px gap
  - 좁아 보이거나 글자 잘림 시 prefab 편집

---

## 일반 메모

- 자율 루프 worktree 변경(78d4a00, e7cb8f9, 879c95d, e6b3283)은 main에 머지 X. 그 브랜치에 inert 상태로 보존.
- main의 c7566e6, f948e26이 canonical (도현이 검토 중인 코드).
- ResultPanel은 Phase 2 작업 안 함 — 별도 세션 시 동일 패턴(prefab화) 적용 가능.

---

## 새로운 의문 발생 시 여기 추가

- [ ]
- [ ]
- [ ]
