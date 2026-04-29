# PreRunSetupPanel.OnConfirmClicked ★

**스크립트:** [[PreRunSetupPanel]]
**파일:** `Assets/Scripts/Order/PreRunSetupPanel.cs:OnConfirmClicked()`

---

## 호출되는 곳 (incoming)

- confirmBtn.onClick (Awake에서 와이어링)

---

## 호출하는 것 (outgoing)

- [[PreRunSetupPanel.Hide]]
- **[[RunController.BeginRun]] 오버로드 (job, selectedTier)** ★ 버그 fix 핵심

---

## 무엇을 하나

선택한 포장 등급으로 출발. **selectedTier가 cargo까지 도달하는 결정적 순간.**

---

## 코드 핵심

```csharp
void OnConfirmClicked() {
    Hide();
    if (runController != null && currentJob != null) {
        runController.BeginRun(currentJob, selectedTier);
        // 세션 16 후속 — 오버로드 사용
        // 이전엔 RunController.BeginRun(job)이 cargo.SetPacking(job.defaultPacking)으로
        // selectedTier 덮어쓰던 버그. 오버로드가 packingOverride 받아서 해결.
    }
}
```

---

## ★ 버그 fix 흐름

이전 버그:
```
PSP.OnConfirmClicked → cargo.SetPacking(selectedTier) → RunController.BeginRun(job)
                                                                  ↓
                                                       cargo.SetCargoType → ApplyPreset
                                                       cargo.SetPacking(job.defaultPacking)  ← 덮어씀
```

수정 후:
```
PSP.OnConfirmClicked → RunController.BeginRun(job, selectedTier)
                                          ↓
                              cargo.SetCargoType → ApplyPreset
                              cargo.SetPacking(packingOverride ?? job.defaultPacking)  ← 보존
```

---

## 관련 흐름

- [[Flow.GameStart]] — 핵심 종착점
