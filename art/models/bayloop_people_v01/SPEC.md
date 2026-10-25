# BayLoop 주민 v01

| 항목 | 사양 |
|---|---|
| 종류 | `people_young_01~03`, `people_elder_01~03` — 공통 성인 실측형 메시, 나이 2×옷 3 |
| 실측 | 청년 1.680000m·어르신 1.620864m(rest GLB POSITION 실측), 머리 약0.235m, 어깨 약0.45m. 어르신은 어깨8% 축소·복부폭13% 추가·약7도 흉추 굽힘과 약한 머리 전방 이동·회색 머리. 양쪽 고관절 간격0.20m |
| 모델 | 원본 bpy 제작. 머리·목·코·귀·손·엄지·팔꿈치·무릎·신발 앞코. 얼굴 애니메이션/손가락 리깅 없음 |
| 좌표 | Blender Z-up, -Y 앞; FBX Unity Y-up·+Z 앞, 미터. 바닥 중심 원점, 신발 rest 최저0m |
| 폴리곤 | 최종 각897 tris (예산≤1,500), 파생 6종 동일 토폴로지 |
| 재질 | 공용 PeoplePhotoAtlas.png 1024² 1장, 16색, Point/Closest, 32px/m. 사진 luminance→제한 색 사다리, 절차 노이즈 없음. R2 바지 RGB차 약10·신발 약7, R3 갈색 재킷≤9·올리브≤13의 낮은 대비로 사진을 유지하고 천 법선을 이어 돌처럼 보이던 면을 수정. UV기준점은 표면 위치에 따라 이동해 같은 가로띠 반복을 완화 |
| 옷 | 01 바랜 청회색 재킷 / 02 흙빛 갈색 재킷 / 03 바랜 회올리브 재킷. 어두운 바지·신발 공통 |
| 리그 | 17본 Generic 스킨. 상완/전완·허벅지/정강이 관절 부근 가중치 블렌드, 모든 정점 합1. Humanoid 자동매핑 미검증 |
| 동작 | Quaternius Universal Animation Library Standard CC0 원본 FBX를 headless import → rest-basis 월드회전 매핑 → 대상 17본 베이크. 30fps, Walk 1~41, Stand 1~76, Sit 1~51. 제자리 루프 |
| 좌석 | 원본 Sitting_Idle_Loop의 골반 높이/다리 자세를 리타깃. 좌면 높이 약0.45m 목표, 정확 접촉 위치는 rig-check/retarget evidence와 인게임 좌석에서 확인. 의자는 FBX에 없음 |
| 파일 | 6 FBX(3개 애니 포함)+6 GLB(정적 기술검사용)+6 blend(리그·액션 원본)·공유 PNG |

출처: Poly Haven `https://polyhaven.com/a/rough_linen` CC0, 기존 직접 다운로드 `art/models/bayloop_props2_v01/sources/rough_linen_diff_1k.jpg`와 `linen-source.json` 재사용. 물리 폭0.271m를 BOX9px로 축소하고 32px/m UV로 투영. 미세 사진 밝기만 색사다리에 재배치하며 패턴을 AI로 만들지 않았다. 피부도 같은 실제사진의 저대비 사다리로 처리해 별도 고해상도 피부 자료를 쓰지 않았다.

애니 원본과 내장 CC0 원문: `art/source/people-animation/standard/Universal Animation Library[Standard]/`. 연구·리타깃 재현 근거는 상위 source 폴더 보고서와 `retarget_evidence` 참조. 원본 캐릭터 메시를 게임에 복사하지 않았다.

수정 재현: UAL Stand 원본의 처음/끝 머리 차이 약4.1mm를 마지막 프레임 첫 포즈 복사로 닫았다. FBX 재임포터가 Hips를 연결본으로 추론하여 이동키를 무시하는 문제(Stand 3.8cm·Sit 36cm 떠 있음)를 실제 재임포트로 재현했다. Hips 이동을 부모 Root에 흡수한 뒤 Hips 위치키0으로 베이크해 월드 포즈를 유지했다. 원본 pose와 새 FBX의 Hips 높이가 1e-6m 이내 일치한다(`debug-fbx.json`); 전체6종 검사는 상위 리뷰 `fbx-check.json` 참조. 걷기 root motion 이동은0이며 높이 접지 보정만 있다.

검토판은 실제 Cycles 960×540. 코드를 직접 작성한 모델과 CC0 애니메이션 재사용이며 AI 이미지/모델 0. Unity 실행·씬 배치·Humanoid 리타깃·걷기 시스템·최종 게임 크기 판정은 하지 않았다. 최종 복사 허용은 독립 게이트와 기술 검사 후 root 담당.
