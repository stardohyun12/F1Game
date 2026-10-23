# 밤샘 퀄리티 루프 (2026-10-23 밤 → 10-24 아침) — 도현 자는 동안

도현: "에이전트로 계속 퀄리티 올려 놔 줄 수 있나? 자는 동안." 방향은 이미 정해짐 — 묻지 않고 진행, 아침에 캡처로 보고.

## 기준 (이 순서로 읽기)
1. `art/_briefs/2026-10-23-course-kyoto-positano.md` — 코스: 교토 관광지 주변 비탈길 → 포지타노 마을 → **기존 해안 절벽·바다 유지** → 언덕 위 도착
2. `art/_briefs/2026-10-23-art-direction-v2.md` — 제작 규칙 1~14 (AI 에셋 0, 실제 캡처로만 판정, 예쁨보다 구체성)
3. `art/reference/mood-2026-10-23/` — 실사진 보드·GAP.md
4. Codex 리서치 결과(생기면): `docs/research/2026-10-23-non-ai-art-codex.md`, 요약 `docs/team/reports/research-codex-20261023-nonai.md`

## 틱마다 (90분 간격)
1. `bash scripts/team-status.sh`, 최신 보고서 읽기. 일하는 팀원이 있으면 건드리지 않는다.
2. programmer 가 끝났고 QA 안 받았으면 → `team.sh qa "[EDITOR] ..."` (보고서 경로 + 확인 못 한 항목 + 비교판 판정).
3. QA PASS 면 PD 가 커밋·push (폰트 SDF 3개 `git checkout --`, `git status --untracked-files=all`, 대용량 확인). FAIL 이면 결함을 programmer 에 되돌린다.
4. 비어 있으면 아래 백로그에서 다음 하나를 보낸다(한 번에 programmer 1개 + art 1개까지, 에디터 과제는 하나씩).
5. 이 파일 맨 아래 "기록"에 한 줄 추가 (시각 · 무엇 · 커밋 · 이번 비용).

## 백로그 (위에서부터)
- P1 [programmer, EDITOR] 코스 재구성 블록아웃 (19:5x 시작한 과제) — 진행 중
- P2 [qa] P1 판정 + 사진 비교판 (구도가 사진과 닮았는가: 길 폭·붙은 건물·높이차·탑 시선)
- P3 [programmer] P2 결함 수정 + 사람 기척 소품 배치 밀도(오토바이·실외기·화분·빨래·전선) — 구간 1·2
- A1 [art, Codex] 리서치 결과가 나온 뒤: CC0 사진(ambientCG·Poly Haven) → 64px·공용 팔레트로 **교토 목조 벽·돌포장·포지타노 회벽·기와** 질감 세트 + check_asset 통과 + provenance 기록. 기존 LofiAtlas 칸 교체 제안(F1/Assets 투입은 승인 전이라도 블록아웃 구간 한정 시험 허용, 독립 게이트 7/10 이상만)
- A0 [PD] Codex 리서치 끝(00:2x) → A1 바로 시작 가능. 리서치 §9 실행 목록·규칙 수정 제안(32색 검사 albedo vs 최종 화면, ambient 한 색 재검토)을 A1·P3 과제에 반영.
- S1 [programmer] 감사에서 새로 나온 **AI 엔진음 1개** 위치 확인 → 출시 의존성에서 빼고 비AI 대체(CC0 녹음) 후보 목록. 화물 4개 교체(BUG-001)도 이 묶음.
- P4 [programmer] A1 질감을 구간 1·2 에 적용 → 캡처 4장
- P5 [qa] 최종 회귀 + 비교판 4장 + 하루 요약

## 멈춤 조건
- 오늘 밤 Claude 팀원 과제 비용 합계 **$35** 넘으면 Claude 과제 중단(Codex 는 계속 가능).
- 같은 FAIL 2번 연속 → 그 항목 멈추고 아침 보고에 올린다.
- 에디터 크래시/복구 대화상자 → 손대지 않고 아침 보고.
- 해안 절벽·바다·언덕 위 장면을 망가뜨리는 변경은 되돌린다.

## 기록
- 23:xx PD: 루프 시작. P1 programmer 진행 중, Codex 리서치(max) 진행 중.
- 00:15 programmer P1 끝($4.44): 블록아웃 메뉴 재현, 비교판 docs/qa/evidence/course-kp-compare.png. **Play 주행 미실행(kp-play.sh 권한)** → QA 가 주행·FAIL 2건 확인할 것. exit 1 은 tail 경로 오타일 뿐.
- 00:2x PD: Codex 리서치(max) 완료 — docs/research/2026-10-23-non-ai-art-codex.md, 요약 research-codex-20261023-nonai.md. A1·S1 백로그 추가.
