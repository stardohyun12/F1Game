# 아트 교훈 (art·art2·artlive 공통, 매 과제 앞에 붙음) — 줄마다 [검사]가 있어야 남는다
1. 표면 = 사진 재료 albedo + 사진 결(±8~12% 명도, 어두운 고무는 ±10~15%) → 그 건물 .gpl 로 재양자화. 단색 채우기·결 없는 칠 금지(D-189·D-194). [검사] `bash tools/art/bin/check.sh <건물>` grain·palette.
2. 빛·그림자·AO 를 텍스처에 굽지 않는다(D-185). [검사] check.sh baked·blob.
3. 녹·때·얼룩 넓게 금지, 손질된 낡음만. [검사] 도현 코멘트.
4. 텍셀 = 세상 32px/m, 작은 물건·글씨·아이콘·데칼 64px/m. 매끈한 글꼴 금지. [검사] check.sh texel·text.
5. 원통 ≥16면. [검사] check.sh cyl.
6. 겹친 면 0(z-fighting = 도현 "치지직"): 데칼·띠·스티커는 1~3cm 띄움. 떠 있는 물건 0(아래 받침에 스냅). [검사] 내보내기 전 fix_1846.py check·snap_1950.py.
7. 만질 수 있는 물건(택배·시계 바늘·의자·타이어)은 낱개 오브젝트 + 이름 + 피벗 바닥 가운데(D-195). [검사] 프리팹 자식 목록.
8. 수정은 원본(.blend/생성기)에 남기고 export 는 재생성. export 만 고치면 다음 내보내기에 사라진다. [검사] 생성기 재실행 결과 = export.
9. 건물끼리 텍스처 공유 금지(md5 같으면 실패, D-196). [검사] md5sum.
10. 끝낼 때: `bash tools/art/bin/check.sh <건물>` 마지막 줄 + `bash tools/art/bin/viewer_export.sh <건물>` + 프리팹 갱신. 하나라도 안 했으면 "미완료"로 보고. [검사] 보고서에 세 줄.
11. 보고서는 과제 시작 5턴 안에 뼈대(docs/team/reports/<role>-STAMP.md)를 만들고 단계마다 갱신. 마지막에 몰아 쓰지 않는다. [검사] 파일 생성 시각.
12. 10점 점수제 판정 금지(JUDGE.md). 판정은 쌍대비교 judge_pair.py 또는 도현. [검사] 보고서에 "N/10" 없음.
13. 커밋 금지. Unity 는 artlive 만. 공유 .mat·ProjectSettings·Map_TownV6 수정 금지. [검사] git status.
