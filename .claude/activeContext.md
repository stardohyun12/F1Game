# activeContext — 2026-10-29 02:20 (PD, 하루 마감)

## 지금 상태
- 맵 `Map_TownV6`: 도현 배치(D-198) 반영됨 — 정비소 (-63.2,-6.7), 보관소 (-43.7,-26.8), 우체국 (-57.7,-25.6), 방 (-117.7,-11.2). 겹치던 OSM 터 삭제, 옛 PitCrew 회색 상자 삭제, TUT_* 물건은 `_TUT_HOLD`(비활성).
- 건물 라이브러리 `F1/Assets/Art/Library/Buildings/{room,garage,depot,postoffice,house_home}` 전부 `python tools/art/build_asset.py <건물>` 로 재생성, `bash tools/art/bin/check.sh <건물>` = GATE PASS 0 위반. 뷰어 glb 동기.
- 스튜디오 = 별도 프로젝트 `F1Studio/`(포트 8093, 라이브러리·SceneA7 정션). 본 `F1`(8090)은 맵·기획 도구. artlive 는 F1Studio 만. 다시 열기 `scripts/open-studio.ps1`.
- Unity 기획 도구: 씬 뷰 [💬 코멘트]/C → 클릭=말풍선(팝업, 한글), 드래그=빨간 선, Enter 저장, 핀, Ctrl+S 때 배치 자동 기록(placements.yaml), [건물 놓기] 드롭다운, 도현 편집 중 잠금(team.sh [EDITOR] 대기). 사용법 docs/design/tutorial/UNITY-PLAN-WINDOW.md. 웹 뷰어(127.0.0.1:8770, /plan/)는 둘러보기용만(정비소 피벗 12m 어긋남).
- 2층집 `house_home` 2차(D-200): 폭 8.6m, 발코니 문, 1층 바깥 흔적만(내부 빈 상자).

## 도현 결정 대기
1. 집 자리: 방 자리·원래 자리 모두 언덕 올려도 바다 안 보임(시뮬 2회). 해안 후보 cand_1 (-7,-142) / cand_2 (-182,-182) / cand_3 (-247,-177, 아침 해 쪽). `docs/qa/evidence/hill/cand_sheet.png`.
2. 집 1층 내부 만들지.
3. 기획 도구 손 확인: 한글 "가나다" 저장, 팝업 위치, 클릭/드래그.

## 보류(D-199·D-201)
- 튜토리얼 고리(1879m 봇 완주 됨, first-delivery.yaml·townv6-tutorial-loop.png 갱신됨) — 더 손대지 않음. 퀘스트 핀 기능 없음.

## 규칙 변화(오늘)
- D-197 결 값 고정 / D-202 모델 배분: Claude 역할 기본 sonnet, `[OPUS]` 표시 과제만 opus, 블렌더·생성기·검사는 Codex art(high), 세션 이어가기 금지, 기본 30턴.
- 교훈 `.claude/team/lessons/art.md` 13줄(검사 명령 필수). 팀원 스크립트는 `tools/art/bin/` 에만(허용 목록 파일별 명시).
- 과제 끝에 Studio_Building 대신 다른 씬 열어 두면 파이프라인 unity 단계 실패 → 이제 build_asset 이 스스로 엶.

## 비용
- 10/28 15시~ Claude Code 2,800 호출·캐시 2.9억·출력 350만(전부 Opus, artlive 2,138). 구독 한도 주의. Codex 0.
