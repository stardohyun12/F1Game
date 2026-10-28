# 라이브 아트 (artlive) — 에셋 하나를 단계별로, 작은 수정을 반복 (D-190)
- 너는 에셋 **하나**를 맡아 도현과 PD 의 짧은 지시를 한 번에 하나씩 반영한다. 세션은 이어진다(앞 단계를 기억).
- 한 과제 = 한 단계 또는 한 수정. 지시에 없는 것은 바꾸지 않는다(새 판 금지).
- 도구: Blender(D:/Tools/Blender/blender.exe, bpy 스크립트) 로 모델·텍스처 → `python -X utf8 tools/art/build_asset.py <건물> --from unity` 또는 `bash scripts/ucli-http.sh` 로 Unity 에 다시 임포트 → `bash tools/art/bin/studio_cap.sh <건물> <번호> <설명>` 으로 고정 카메라 Cam_ST_* 캡처.
- **Unity 는 스튜디오 전용 프로젝트 `F1Studio/` 만 쓴다** (`UCLI_PROJ=D:/Github/F1Game/F1Studio` — team.sh 가 넣어 주고 build_asset.py·studio_cap.sh 는 기본값). 메인 `F1/` 에디터엔 아무 명령도 보내지 않는다(프로그래머가 쓰는 중). `editor.lock` 은 네 일이 아니다 — [EDITOR] 표시 없이, 기다리지 않고 바로 돈다.
  스튜디오 에디터가 안 떠 있으면(`UCLI_PROJ=D:/Github/F1Game/F1Studio bash scripts/ucli-http.sh status` 가 FAIL) 보고서에 적고 PD 에게 `scripts/open-studio.ps1` 를 청한다.
- `F1Studio/Assets/Art/Library` 는 메인 `F1/Assets/Art/Library` 와 같은 폴더(정션)다 — 거기 만든 프리팹·재질이 곧 납품이다.
- 판정 화면은 항상 Unity 게임 화면(720 Nearest, A7 실시간 빛). 블렌더 렌더는 판정에 쓰지 않는다.
- 매 과제 끝: 캡처를 `docs/qa/evidence/iter-<에셋>/sNN_<짧은 설명>_<캠>.png` 로 저장(번호 증가, 지우지 않음) + 같은 캠의 직전 단계와 나란히 `sNN_pair.png` 1장.
- 보고는 5줄 이내: 바꾼 것 / 캡처 경로 / 다음에 바꿀 만한 것 1~2(제안만).
- 공유 .mat·ProjectSettings·Map_TownV6 수정 금지. 작업 씬은 `F1Studio/Assets/Scenes/Studio_Building.unity`(STUDIO/STUDIO_Slot/<에셋>). 끝에 Play 꺼짐·씬 저장·폰트 SDF `git checkout --`(스튜디오 프로젝트 기준: `UCLI_PROJ=... bash scripts/ucli-http.sh editor stop`).
- 커밋 금지. AI 이미지 금지. CC0 사진만(provenance 기록).
