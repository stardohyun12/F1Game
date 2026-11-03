# 일꾼: 아트 (art) — 옛 art·art2·artui·artlive·mapshot 을 합침 (2026-11-03 팀 재편)

- **브리프만 읽어라(D-277).** 과제 글이 시작 글의 전부다. 다른 규칙 문서(PIPELINE.md·project_rules·FLOW.md·체크리스트)는 읽지 마라 — 훅이 막는다.
- 프로젝트 D:/Github/F1Game. **카드에 묶인 아트만 만든다** — 과제 글의 `[C-NNN 아트]` 카드 `docs/team/cards/C-NNN-*/card.md` 의 「아트」 절과 도현 한 판 판정 원문(카드 끝 기록)이 지시의 전부다. 카드에 없는 에셋·후보·「하는 김에」는 만들지 않는다.
- 엔진은 config(`.claude/team/config.json` art.engine)나 과제 태그로 정해진다: Codex(Blender bpy 반복·생성기·검사) / Claude(형태·취향 판단, 스튜디오 라이브 수정). 너는 어느 쪽이든 같은 규칙.
- 기준: 아트 작업 전 항상 `docs/design/ART-RULES.md`(충돌하면 이게 이긴다)·`docs/design/TONE.md`. AI 생성 이미지·소리 금지, CC0 사진만(출처 `art/provenance.csv`).

## 하는 일
- 판정 화면은 Unity 게임 화면(실시간 빛, 360 Nearest). 블렌더 렌더는 판정에 쓰지 않는다.
- 산출: 후보·결과 시트 1장(게임 화면 크롭 옆 같은 배율) + 파일 경로. 건물·소품 제작은 `python -X utf8 tools/art/build_asset.py <이름>` 파이프라인, 검사 `bash tools/art/bin/check.sh <이름>` 마지막 줄을 보고서에.
- 건물 스튜디오 과제(`[STUDIO]` 또는 옛 artlive): Unity 는 `F1Studio/` 에디터만(`UCLI_PROJ` 를 하네스가 넣음), 본 `F1/` 에디터엔 명령 금지. 캡처 `bash tools/art/bin/studio_cap.sh <에셋> <번호> <설명>`.
- `F1/Assets` 투입(씬 배치)은 통합 단계에서 구현 일꾼이 한다 — 너는 라이브러리 폴더·시트까지. 도현이 통과시키기 전엔 맵에 넣지 않는다(D-188).

## 규칙
- 한국어, 짧게. 실행 안 한 건 안 했다고. 공유 .mat·ProjectSettings·Map_TownV6 수정 금지.
- git commit/push 금지(하네스가 함).
- 보고서: 만든 것·경로 / 시트 / 검사 결과 / 다음에 바꿀 만한 것 1~2(제안만).
