# Task0131 — Quaternius 원본 애니메이션 확인

## 결과

공식 무료 Standard ZIP을 로그인·이메일·결제 없이 내려받았고, 내장 CC0 라이선스와 Blender 5.2.2의 원본 FBX import를 확인했다. 이어 청년 17본 리그에서 Walk/Stand/Sit 독립 리타겟 시험과 저장 파일 재로드 검증을 수행했다. 이후 최종 제작 중 발견된 FBX 연결본 문제도 별도 재현했다. 아래에 원본 확인, 적용 시험, FBX 진단을 구분하며 최종 6종 적용과 Unity 검증 결과를 대신하지 않는다.

## 원본과 권리

- 제작자 공식 소개: https://quaternius.com/packs/universalanimationlibrary.html
- 제작자 배포: https://quaternius.itch.io/universal-animation-library
- CC0 원문: https://creativecommons.org/publicdomain/zero/1.0/legalcode.txt
- 내려받은 그대로: `Universal Animation Library[Standard].zip`, 15,904,933 bytes.
- SHA256: `cc73fc4e495b82958207316596317a3f40b9fa38065bde1027937452da537724`.
- ZIP 내 `License.txt`가 CC0 1.0 Universal을 명시한다. 수정·재배포·상업적 이용 허용 원문을 `cc0_legalcode.txt`에 보존했다. 상표·특허 등 별도 권리가 CC0로 소멸되는 것은 아니다.
- 제작자 게시글은 생성형 AI 미사용을 표시한다(제작자의 진술; 독립적인 제작 과정 감사는 아님).
- Standard는 무료. Pro 및 Source(.blend)는 유료 항목이므로 내려받지 않았다. 전체 상품의 120+ 홍보 수치와 무료 파일의 실제 43개 액션을 구분한다.
- ZIP 내부 파일을 바꾸지 않고 `standard/`에 풀었다. 서명된 임시 다운로드 URL은 기록하지 않았다.

## 재현 검증

실행: `D:/Tools/Blender/blender.exe -b --python art/source/people-animation/inspect_source.py`

결과: Blender 5.2.2 LTS, FBX version 7400, armature 1, actions 43, exit 0. 상세 본 부모/머리/꼬리 좌표와 프레임 범위는 `blender-source-inspection.json`, glTF 원본 본·액션 대조는 `gltf-inspection.json`.

| 목적 | 실제 FBX 액션 이름 | 원본 프레임 |
|---|---|---|
| Walk | Armature\|Armature\|Walk_Loop | 1–41 |
| Stand | Armature\|Armature\|Idle_Loop | 1–76 |
| Sit | Armature\|Armature\|Sitting_Idle_Loop | 1–51 |

`Unity/UAL1_Standard.fbx`는 root motion disabled, `_RM` 접미 파일은 root motion 포함이라는 설명이 내장 README에 있다. 원본 전체의 root motion 비활성 여부를 수치로 검사하거나 원본 캐릭터 미리보기를 렌더하지는 않았다. 아래 미리보기는 대상 청년 리그에 리타겟한 결과다.

## 적용 판단과 수정점

원본 주요 관절은 `root → pelvis → spine_01 → spine_02 → spine_03 → neck_01 → Head`; 양팔은 `clavicle_l/upperarm_l/lowerarm_l/hand_l`, 양다리는 `thigh_l/calf_l/foot_l/ball_l`이며 오른쪽은 `_r`이다. glTF skin joints는 64개로 손가락과 말단 본을 포함한다.

청년/노인의 16–20본 리그에는 골반·척추·머리·팔/다리 대응이 가능하다는 구조적 판단이다. 손가락/쇄골/발가락이 없는 대상은 해당 자유도를 생략하거나 상위 관절로 합칠 필요가 있고, 단일 척추라면 원본 척추 체인의 누적 회전을 사용해야 한다. 로컬 Euler를 이름만 바꿔 복사하면 rest pose 축 차이로 뒤틀릴 수 있으므로 원본 pose와 rest의 상대 회전을 대상 rest 축으로 변환해야 한다. 골반 이동량은 다리 길이 비율로 조정하고 in-place 수평 위치를 제거하되 Sit 골반 하강은 보존한다.

위 내용은 원본 구조를 확인한 시점의 적용 제안이다. 이후 아래 독립 시험으로 청년 리그의 적용 가능성을 확인했다. 원본을 임의 수정한 파일을 '원본 재현'으로 부르지 않았다. 자료와 시험 결과를 제작 담당에게 전달했으며, 이 조사 담당은 Unity import/Play/씬 저장, 대상 생성기/Assets/provenance 수정, commit을 하지 않았다.


## 후속 독립 리타겟 시험

- 모듈: `tools/asset-gen/bayloop_people_retarget.py`, 함수 `retarget(target,mesh)`와 독립 캐릭터 빌드용 `cleanup_actions(target, rename=True)`.
- 입력: 원본 비RM FBX와 `art/models/bayloop_people_v01/people_young_01.blend`의 ResidentRig/스킨 메시. 시험은 기존 파일을 수정하지 않고 별도 파일에 저장했다.
- 출력: [시험 파일](retarget-trial.blend), [표본 측정](retarget-trial.json), [재로드 측정](retarget-reload-check.json), [서기](trial-Stand.png), [걷기](trial-Walk.png), [앉기](trial-Sit.png).
- 대상 기본 자세의 팔은 내려간 A자이고 소스는 T자다. 대상 본 방향을 소스 기본 본 방향으로 맞춘 가상 기준 회전을 계산하고, 소스의 현재 회전을 적용했다. 대상 L의 x좌표가 음수이므로 소스 r와 대응했다. 골반 이동은 키 비율로 조정하고 수평 이동은 제거했으며, Sit의 골반 하강은 보존했다.
- 최초 바닥 보정에서는 Root의 로컬 Z를 월드 Z로 오인해 약 -0.03m가 남았다. 기본 행렬의 역회전으로 월드 높이 보정을 로컬 좌표로 바꾼 뒤 해결했다.
- 3클립의 시작/중간/끝 총 9표본에서 정점 좌표가 유한함을 확인했다. 저장 파일 재로드 후 minZ는 -1.12e-7~7.64e-8m였고, 최고점은 Stand 약 1.671m, Sit 약 1.331m였다. 연속 발 미끄러짐이나 모든 프레임의 자기 교차 검사는 수행하지 않았다.
- Walk/Sit 렌더를 직접 확인했다. 팔다리 폭발이나 팔 반전은 없고 앉기 자세가 성립했다. Walk 정면 투영의 다리 겹침과 원래 메시 얼굴 얼룩은 남아 제작 담당에게 전달했다. 이는 정식 그래픽 게이트 PASS 판정이 아니다.
- 재현: `D:/Tools/Blender/blender.exe -b --python tools/asset-gen/bayloop_people_retarget.py` 이후 같은 실행 파일로 `art/source/people-animation/check_trial.py`와 `art/source/people-animation/render_trial.py`를 실행한다.
- cleanup 함수는 FBX all-actions 출력에 원본 43액션과 기존 자체 3액션이 섞이지 않도록 UAL 3액션만 보존한다. 다른 캐릭터가 함께 있는 공유 씬에서는 호출하면 안 된다.

## 후속 FBX 연결본 진단

독립 리타겟 시험 성공은 FBX 재임포트까지 성공했다는 의미가 아니었다. 최종 제작 담당의 FBX에서 Sit이 뜨는 문제가 발견되어 [진단 스크립트](diagnose_fbx_hips.py)로 원인을 별도 재현했다. 수치는 [진단 결과](fbx-hips-diagnostic.json)에 있다.

| Sit 첫 프레임 | Hips 연결 여부 | 메시 최저 Z |
|---|---|---|
| 당시 제작 중 blend | false | 0m |
| 같은 캐릭터 FBX 재임포트 | true | 0.36014134m |
| 재임포트 후 메모리에서 Hips 연결만 해제 | false | 0.000000036m |

FBX에도 Hips의 이동 키 -0.36014128이 존재했다. 연결된 본이 이동을 무시한 것이므로 키 누락으로 판단하지 않았다. `evaluated_get.data`와 `evaluated.to_mesh()`의 최저 높이는 동일하여 메시 측정 방식의 오류도 배제했다. 최종 파일을 수정하지 않고 제작 담당에게 전달했으며, 담당자가 진행한 Hips 수직 이동의 Root 베이크 수정과 최종 6종 재검증은 해당 담당의 결과를 참조해야 한다.
