# D-121 1차 시안 — 도현 승인 필요

현재 단계는 원화/시안이며 F1/Assets 투입·모델 제작·Unity 실행·commit/push는 하지 않았다.

## 참고 → 적용

| 확인한 자료 | 가져온 원리 | 이번 적용·변경 / 검증 범위 |
|---|---|---|
| 프로젝트 `docs/superpowers/specs/2026-10-22-release-quality-art-ui-plan.md` §2·§3, D-121 결정 | 팔레트 9토큰, 종이/도장/법랑, Jua·Pretendard, UI는 직접 제작 | PNG 도형은 Pillow로 직접 작성, 화면은 HTML/CSS 렌더. 생성 AI는 화물 원화에만 사용. |
| `docs/qa/evidence/proto-d119-board-call.png`, `proto-d118-packing.png`, `proto-d118-result.png` | 실제 화면 비율과 배경 | 원본 QA 캡처 위 HTML 합성. 4개 화면은 기능 구현이 아닌 레이아웃 시안. 실게임 적용 전후 비교로 오해하지 말 것. |
| [Unity 6000.3 공식 9-slice 문서](https://docs.unity.com/en-us/engine/6000.3/manual/unity2d/sprite/9-slice/9-slicing) | 모서리 고정·중앙 확장 | 로컬 참조 함수로 동일 모서리 유지 검사. Unity 원본 예제 재현은 안 했으며 엔진 검증을 대신하지 않음. |
| [Kenney Food Kit](https://kenney.nl/assets/food-kit) | 단순한 덩어리·무료 CC0 팩 대안 | 공식 페이지 CC0·200개 확인. 팩 다운로드·메시 검사·재사용 안 함. 장난감 실루엣 원화를 별도 생성. |
| [A Short Hike 공식 press kit](https://ashorthike.com/press/) | 프로젝트가 지정한 로파이 비교작 | 공식 제작 배경·공개 자료 위치 확인만. 엔진 UI 구현/렌더 설정을 검증했다고 주장하지 않음. |
| 기존 Jua·Pretendard TTF 및 OFL.txt | 한글 제목·본문 | 검토 폴더에 사본과 OFL 동봉. 외부 신규 폰트 다운로드 없음. |

다른 게임의 원본 프로젝트를 실행하지 않았다. 기존 프로젝트 스샷과 슬롯 코드를 기준으로 적응한 시안이며 외부 제작법의 완전한 재현이 아니다. 별도 유료 자산 구매·서비스 가입 없음; imagegen 호출 비용은 측정하지 않았다.

## 슬롯 전달 계약

`sprites.json` = 각 슬롯의 정확한 크기와 L/B/R/T px. `sprites/`에는 투명 RGBA 바탕만, `guides/`에는 여백 안내선과 수치. 글자는 TMP 별도. Sprite(2D/UI), Single, Full Rect, mipmap off, alpha transparency on, bilinear, compression none을 최초 검토 설정으로 제안하며 실제 임포트는 QA/프로그래밍 담당.

원색 유지: paperPanel, paperCard, enamelPlate, stampSprite. 나머지는 흰색/회색 원본으로 기존 상태색을 곱한다. paperPanel 계열도 호출자 fill이 있으면 곱셈 틴트되므로 프로그래밍팀이 흰 틴트를 확인해야 한다. 원색 PNG의 그림자는 4px 오프셋, 잉크 40%; 회색 틴트 PNG는 그림자도 틴트되므로 고정 잉크 그림자가 필요하면 코드 Shadow 분리가 필요하다.

stampRing/stampSprite는 원형 유지가 필요해 border=0, Simple, 정비율. stampSprite에는 문자·등급·점수를 넣지 않았다. PackingScreen이 이미 별도로 그린다. 현재 kStampInk=(0.80,0.09,0.10,0.92)는 D-121 signal과 다르므로 승인 후 코드 글자색을 signal로 맞추는 요청 필요. 코드 수정 안 함.

slipHeader의 중앙 절취선은 연속선으로 유지했다. 반복 점선·영수증 톱니를 한 장의 9-slice 중앙에 넣으면 늘어지므로, 목업의 반복 절취선과 영수증 톱니는 향후 별도 tiled 장식/코드가 필요하다. 현재 PNG는 코너의 작은 절취 표시만 포함한다. 목업은 PNG 슬롯의 1:1 런타임 배선 증거가 아니다.

게이지는 현재 150×20 표시, anchorMax.x로 폭 축소. border 8px 채움은 5% 같은 작은 폭에서 모서리 압축 가능; 0/5/50/100% 엔진 확인 필요. 0은 fill 비표시 또는 rect width=0을 프로그래밍 확인해야 한다.

## 화물 제작 명세 — 모두 추정, 실측 아님

| 이름 | W×D×H m | 예상 tris 구성 | 색 | 앞면(+Z) 표식 |
|---|---|---|---|---|
| cargo_box_v01 | .40×.30×.28 | 몸체 240 + 테이프/라벨 120 = 360 | 크라프트 #BD8849, lemon, paper | 오른쪽 아래 |
| cargo_cake_v01 | .30×.30×.24 | 몸체/받침 360 + 크림 480 + 딸기/잎 480 = 1,320 | paper, terracotta, olive | 받침 앞 테두리 중앙 |
| cargo_eggcarton_v01 | .24×.16×.14 | 달걀 6×160 + 트레이/뚜껑 600 = 1,560 | paper, olive | 트레이 긴 변 중앙 |
| cargo_wine_v01 | .12×.12×.30 | 병 600 + 마개/라벨 120 = 720 | 어두운 olive, terracotta, paper | 타원 라벨 중앙 |
| cargo_fish_v01 | .50×.22×.19 | 몸체 960 + 꼬리/지느러미 360 + 상자/라벨 360 = 1,680 | tyrrhenian, paper, terracotta | 상자 긴 변 중앙 |

피벗 바닥 중심, Y-up, m. 최종 명명·폴더는 PIPELINE §5. 실루엣의 비율만 과장하고 치수는 생활 소품 범위. open egg carton 높이는 열린 뚜껑 포함, 케이크 높이는 딸기 포함. 원화 v02에서 계란판 윗면의 뚜껑 누락·생선 오른쪽면 방향을 수정했다. 다만 원화에 약한 원근/뷰 간 딸기 정렬 차이가 남아 있어 정확한 제작 도면이 아니다. 승인 후 bpy에서 정사영 치수와 동일 좌표 딸기를 확정하고 미리보기에서 검증한다. 실제 tris·콜라이더·무게·적재 여부 미검증.

## 확인 및 남은 일

- 실행: `py art/_review/2026-10-23-d121/build_review.py` (PNG·guide·HTML·manifest 재생성).
- 정적 검사: `validation.json`, 10 RGBA 슬롯·9-slice 모서리 복제 검사 3/3. `slice-proof.png`.
- Chrome/Edge headless에서 HTML을 1280×720 PNG로 렌더. `mockup-overview.png`는 4개 화면 합본 2560×1440.
- 재렌더: `powershell -NoProfile -ExecutionPolicy Bypass -File art/_review/2026-10-23-d121/render_review.ps1`. 전화/정산은 기존 QA 화면의 옛 UI 영역을 불투명 받침으로 가려 중복 정보를 제거한 합성이다.
- 독립 시안 검토 7/10, 제출 가능(정식 게이트 아님). 표시 결함 1회 수정: 옛 UI 중복·도장 guide 글자 겹침·index 최종 원화 참조. 딸기 뷰 정렬은 잔여 이슈. 최종 PNG 크기/알파 10/10, HTML 누락 링크 0건.
- 원화: 내장 imagegen, 프롬프트는 `imagegen-prompt.txt`; v02 교정 프롬프트는 `imagegen-edit-prompt.txt`. PNG를 작업 폴더로 복사했고 모델 생성은 안 함.
- 시안 검토만 수행. 그래픽/QA 정식 게이트 PASS 아님. Unity 컴파일·Play·임포트 미실행. QA/프로그래밍 임포트 필요.
- 도현 승인 필요: 종이/법랑/도장과 4화면 형태, 화물 5종 실루엣·색·앞면 위치. 승인 후 실제 에셋 제작 및 미리보기 승인 절차를 따른다.
