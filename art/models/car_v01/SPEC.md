# car_v01 — 낡은 배달 포뮬러

PD 20261023-1919. **도현 미리보기 승인 대기. F1/Assets에 투입하지 않았다.**

## 계약

| 항목 | 최종 값 |
|---|---|
| 폭 × 길이 × 높이 | 1.958909 × 3.785000 × 1.072000 m |
| Blender bounds | min(-0.979454517,-2.030000210,0), max(0.979454517,1.754999995,1.072000027) |
| 기준 원점 | (0,0,0), 타이어 접지 z=0. bbox 중심으로 재중앙화 금지 |
| 전방·상방 | Blender -Y / +Z. GLB +Z / +Y |
| 삼각형 | 2,168 / 예산 3,000 |
| 텍스처 | CarAtlas.png 512², 4×4의128px칸·4px가드, 활성120px=1.875m |
| 픽셀 밀도 | 목표64px/m, export 실측63.996479~64.101336 |
| 색 | 공용32색 중18색. 팔레트 밖0. 별도 tint/normal/metallic 없음 |
| 재질 | Car_photo64, opaque, roughness1, Point/Closest |
| 제작 | Blender5.2.2 bpy. 실제 CC0 사진+스크립트 정보층. 출시물 AI 이미지0 |

원본 실측은 `art/_review/2026-10-23-car/current-car-measure.md/.json`.

## 휠과 계층

| 이름 | Blender 중심 (m) | 현재 Unity prefab 중심 (m) |
|---|---|---|
| wheel_front_left | (+.79,-1.20,.35) | (-.79,.35,+1.20) |
| wheel_front_right | (-.79,-1.20,.35) | (+.79,.35,+1.20) |
| wheel_rear_left | (+.79,+1.10,.375) | (-.79,.375,-1.10) |
| wheel_rear_right | (-.79,+1.10,.375) | (+.79,.375,-1.10) |

각 휠은 별도 mesh object이고 자신의 중심이 pivot이다. 회전 local X, Unity 조향 Y. 기존 FBX 경로와 GLB 경로는 좌표 변환이 다를 수 있으므로 이름만으로 좌우를 추정하지 말고 위 중심을 맞춘다. GLB 자체에서는 left-labelled wheel이 +X인 기존 소스 계약을 유지했다. Blender 재임포트에서는 FBX/GLB position·UV·피벗 일치 PASS. Unity의 축·회전 방향·직진 접지는 아직 실행 검증하지 않았다.

차체 별도 객체: body, aero, engine, suspension, driver, delivery_bag. 기존 FormulaVehicleVisual의 네 wheel 이름을 유지했다. 기존 restoration 재질 스왑은 이번 모델의 한 장 아틀라스와 그대로 호환된다고 보장하지 않는다. 프로그래밍팀이 기존 FormulaWorn/Restored를 새 재질에 덮어쓰지 않게 연결해야 한다.

현재 해안 코스 Visual local scale1.4285712 × Race0.7 ≈1. Visual local position Unity(0,.1285497,-.19642855)을 유지한다. 옛 gameplay-install.txt의0.6632653은 현재 코스 값이 아니다.

## 표면과 형태

- 외형: 좁은 노즈, 노출 휠, 낮은 후미 날개, 프레임과 서스펜션. 헬멧 아래 목깃 연결.
- 가방: 각진 운송 상자와 구분하도록 눌린 모서리·살짝 부푼 패널·패딩 뚜껑·검은 고정끈·지퍼/봉제. 공용 팔레트에서 가능한 바랜 황토 주황색 사용.
- 세월: 하단 돌 튐 칩, 왼쪽 패널 이음 테이프, 프레임 접합부 작은 산화, 타이어 압인/접지 마모. 무작위 전체 노이즈를 만들지 않았다.
- 숫자07은 Galmuri11 실제 글리프. 정보층과 사진 원천은 `art/source/car-v01/SOURCES.md`, `texture-contract.json`.
- 기준 원화의 RGB 픽셀을 텍스처로 복사하지 않았다. 기존 공용 팔레트만 재사용했다.

## 재현·검사

```powershell
py -3.13 tools/asset-gen/car_texture.py
& 'D:/Tools/Blender/blender.exe' -b --python art/models/car_v01/generate.py
py -3.13 art/_review/2026-10-23-car/register_provenance.py
py -3.13 tools/asset-gen/check_car.py
& 'D:/Tools/Blender/blender.exe' -b --python art/_review/2026-10-23-car/check_fbx.py
```

차량 단색면 검사는 실제 연결된 면≥0.04m²에서2색 이상·주색≤98%; 화물의 모든8×8창 규칙을 그대로 적용한 초판과 차이를 `check-policy-review.md`에 기록했다. 8×8 단색 창은 진단에 계속 남긴다. 최종 의미·룩은 독립 PNG 게이트로 별도 판정한다.

## 미리보기와 인계

`art/_review/2026-10-23-car/index.html`: 같은 해안 캡처 합성, 960본, 기준 원화 비교,4방향,근접,기존 카메라 잘림 비교.

전체 보기의6.6m 카메라는 검토 제안이며 게임에 적용하지 않았다. 현재4.2m 카메라 기준 후미 잘림은 남는다. 검토 배경의 차가 가린 도로만 AI 복원했고, 게임 파일에는 그 이미지가 들어 있지 않다.

Unity 설정 권장: Point, texture compression None, mipmap Off를 시작점으로, 실제960×540에서 깜빡임을 확인한다. 모델 scale1m, 기존 Visual root transform 유지. **QA/프로그래밍 임포트 필요**: FBX 축·wheel spin/steer, 셰이더/아틀라스, restoration 연결, ground contact, 실제 카메라와 색 보정, 프레임 성능. Unity 에디터·Play·임포트·커밋·푸시는 실행하지 않았다.
