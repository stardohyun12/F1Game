# Coastal Formula — 낡은 차량 / 부품 복원 / 인게임 연결

## 현재 게임 적용 상태

`Assets/ComplexTrack.unity`의 실제 Race 플레이어에 `FormulaGameplayVisual.prefab`을 연결했다. 기존 Rigidbody, BoxCollider, 컨트롤러, 입력, 카메라를 유지하며 기존 모델 렌더러와 CarVisualLean만 비활성화했다. 새 바퀴 회전/앞바퀴 조향/차체 기울기는 FormulaVehicleVisual이 담당한다. 비주얼에는 추가 Collider/Rigidbody가 없다. 기존 충돌체에 맞춘 자식 스케일은 0.6632653이며, 네 타이어 바닥을 충돌체 바닥에 맞췄다. 맵 전체 아트는 이 작업에서 교체하지 않았다.

### 재질과 복원 매핑

256×256 공용 UV에 맞는 Worn / Serviced / Restored 재질 3개를 제공한다. Worn은 바랜 도장, 작은 도장 벗겨짐, 금속 산화색, 타이어 먼지다. 찌그러짐/파손 물리나 충돌 시 실시간 데미지 누적은 아니다.

| 기존 구매 | 외형 변화 |
|---|---|
| Paint | 차체 도장 복원 |
| Tires | 네 바퀴 재질 복원 |
| Aero | 앞뒤 윙·디퓨저 재질 복원 |
| Suspension | 서스펜션 암 재질 복원 |
| Gearbox | 엔진 그룹을 정비된 상태로 변경 |
| Engine | 엔진 그룹 완전 복원 |
| ReinforcedTrunk | 가방을 정비된 상태로 변경 |

운전자/헬멧은 깨끗한 재질 유지. 전체 차량을 빨갛게 바꾸던 기존 Paint 분기를 신규 외형 컴포넌트가 있는 경우 부품별 갱신으로 대체했다. `Appearance.asset`에서 재질 참조를 바꿀 수 있고, `FormulaVehicleVisual.parts`에서 부품 매핑을 바꿀 수 있다. 기존 구매 이벤트 구독과 OnEnable/Start의 소유 상태 재조회로 외형을 갱신한다. 새 저장 시스템은 만들지 않았으며 기존 복원 소유 상태는 세션 범위다.

[Unity sharedMaterial 문서](https://docs.unity3d.com/ja/6000.0/ScriptReference/Renderer-sharedMaterial.html)를 참고해 런타임에는 준비된 재질 참조만 교환한다. 공유 재질의 색/텍스처 자체를 변경하거나 `.material`로 인스턴스를 누적 생성하지 않는다.

### 실제 검증

`gameplay-smoke.txt`: 실제 Play 모드에서 약 7.78m 이동, 최고 약 3.75m/s. 가속/이동/바퀴 회전/조향, 기본 낡은 상태, 도색 시 타이어·가방 보존, 여섯 복원 구매, 중복 구매 비용 불차감, 적재함 강화 반영, 컴포넌트 재활성화 후 소유 상태 복구를 통과했다. 테스트 구매는 Play 종료 시 되돌아갔다. C# compileFailed=False. 장시간 운전·목표 기기 FPS·별도 실행파일 빌드는 시험하지 않았다.

08_worn / 09_restored는 동일 카메라의 Unity 에셋 렌더다. **10_ingame_worn / 11_ingame_restored는 실제 Play 모드 ScreenCapture로 HUD까지 포함한다.** 현재 게임 카메라 거리에서는 차가 작게 보여 긁힘 디테일보다 차체 색과 가방 실루엣이 먼저 읽힌다.

재생성 후 게임 적용: Unity 편집 모드의 저장된 ComplexTrack에서 `FormulaGameplayInstaller.Install()` 실행. 메뉴 `PitCrew > Art > Coastal Formula > Install Gameplay Vehicle`. 씬이 dirty이면 덮어쓰지 않고 중단한다. 기존 씬의 최초 백업은 `ComplexTrack.before-formula.unity`. 재실행은 기존 새 비주얼을 교체하며 물리 부품은 유지한다. 테스트는 편집 모드에서 `FormulaGameplaySmoke.Run()`을 실행하면 Play 진입·검증·캡처·종료까지 자동 수행한다.

---

선택한 따뜻한 로파이 해안 마을에 맞춘 자체 제작 포뮬러 배달차. 01~09 PNG는 실제 Unity 검토 카메라 렌더이고 10~11은 플레이 중 스크린샷이다.

## 제작 판단

- 낮고 긴 노즈, 노출 바퀴, 앞뒤 윙으로 포뮬러 실루엣 유지.
- 빨간 차체 / 크림색 띠 / 주황 가방 / 청록 수선판. 실제 팀 로고나 외부 차량 모델 사용 없음.
- [Easy Delivery Co. 개발자 인터뷰](https://80.lv/articles/inside-the-making-of-easy-delivery-co)에서 확인한 단순 형상과 차량 픽셀 텍스처 원리를 참고. 해당 게임 원본 모델을 재현하거나 사용한 것은 아니다.
- 프로젝트에서 검증한 Blender → FBX → Unity/URP 경로와 LofiSurface 셰이더를 재사용. 신규 형상과 256×256 텍스처는 coastal_formula.py에서 생성.
- 기존 race_v2와 동일 카메라에서 비교 렌더. 기존 차량을 1.35배로 놓았으며 동일 물리 규격 비교가 아니다.

## 파일

- `F1/Assets/Art/CoastalFormula/CoastalFormula.prefab`: 시각 프리팹 + 단순 차체 BoxCollider. 주행 컨트롤러/Rigidbody/WheelCollider는 포함하지 않음.
- 같은 폴더 `CoastalFormula.fbx`, `FormulaAtlas.png`, `FormulaShared.mat`, `CoastalFormulaReview.unity`.
- 이 폴더 `CoastalFormula.blend`, `CoastalFormula.glb`: 수정 가능한 원본과 휴대용 모델.
- `tools/asset-gen/coastal_formula.py`: 형상/텍스처 재생성.
- `F1/Assets/Editor/CoastalFormulaBuilder.cs`: 프리팹 작성/검증/렌더.
- 새 모델 4,856 triangles, 메시 10개. 공용 UV 아틀라스 규격에 상태별 머티리얼 3개. 폭 약 1.96m / 길이 3.785m. 기존 차량 메시만 즉시 덮어쓸 동일 규격 모델은 아님.

## 부품과 검증

차체, 에어로, 엔진, 서스펜션, 운전자, 가방, 네 바퀴 분리. Unity +Z 전방, 왼쪽 -X, 오른쪽 +X. 네 바퀴는 중심 피벗을 가지며 가방은 CargoMount 아래에 배치.

첫 렌더에서 발견한 앞바퀴 바닥 간격 0.025m를 수정했다. 네 바퀴 바닥과 y=0 오차 <0.002m, 중심 피벗 오차 <0.005m, 좌우 이름/좌표 일치 검사. 회전·조향 변환 후 바퀴 bounds 중심 이동량 기록. 실제 물리 주행/충돌 튜닝이나 가방 흔들림 애니메이션을 시험한 것은 아니다.

가방이 운전자 헬멧을 가려 높이를 줄였다. 정면/후면/측면/상단/기존 모델 비교/마을 추적 카메라의 실제 렌더를 확인했다. C# 컴파일 실패 False, 셰이더 오류 0. 상세 수치는 unity-verification.txt. 기존 ComplexTrack 씬 dirty=False 유지.

현재 남은 미술 판단: 노즈와 윙의 직선적인 형상, 단순한 운전자 몸통이 장난감 느낌을 만든다. 현대 F1의 복잡한 곡면을 충실히 재현한 차가 아니라 선택한 캐주얼 로파이 방향의 차량이다.

## 재생성

저장소 루트 PowerShell:

```powershell
& 'D:/Tools/Blender/blender.exe' -b -P tools/asset-gen/coastal_formula.py
& ./tools/asset-gen/lofi_unity.ps1 -Code 'UnityEditor.AssetDatabase.Refresh(); return "refresh";'
# Unity 컴파일 완료 후
& ./tools/asset-gen/lofi_unity.ps1 -Code 'CoastalFormulaBuilder.Build(); return "done";'
```

Unity 메뉴 `PitCrew > Art > Coastal Formula > Build and Capture`로도 가능. 전용 프리팹/검토 씬을 재작성하므로 수동 수정 버전은 별도 복사본으로 보관. 기존 마을 씬에서는 일시적으로 차량을 교체해 렌더하고 저장하지 않는다. Blender 5.2.2 / Unity 6000.3.19f1 / URP 환경에서 확인. 새 유료 서비스·외부 에셋·패키지 설치 없음.
