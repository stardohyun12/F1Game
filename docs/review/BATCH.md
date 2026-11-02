# 도현 한 번에 확인 묶음
도현(D-256): "할일 쌓아뒀으면 계속 해서 개선하고 개선한거 확인하는건 한번에 할게". 과제가 끝날 때마다 자동으로 아래에 쌓임.

## 이미 끝난 것(11/02)
- 목적지 표시 4cccf317 — ![](../qa/evidence/dest-clarity/pd_before_after.png)
- 포장 작업대 1판 be4bba3f — Proto_PackBench 씬
- 디버그 창 37c0bbbe(` 또는 F1) · 점프 6bea8645(Space) · 장소별 환경음 e4d8a704 · 하늘 291b72d5 · 화면 경고 2e5c0f1a

## 11/02 15:11 ✅ 포장 작업대 2판 (d5222ee8) — 물리로 끌기(벽 통과 막음)·휠 앞뒤·오른쪽 버튼 회전·뽁뽁이 당겨 풀기/깔기/덮기/감싸기. Proto_PackBench 씬. 요약 docs/team/reports/programmer-20261102-1442.md

## 11/02 15:43 ⚠️ 보류  새 세션. 먼저 editor stop·폰트. 커밋 금지. 시간 상자: 20분 뼈대, 25분마다 갱신, 80분 넘으면 마무리. ()
cost=$8.73
`docs/team/reports/programmer-20261102-1515.md`
- **Flow:** you take the cart from the garage, but the order doesn't start yet — the arrow points at the depot. At the depot you pick an order on the chalkboard; empty lines can't be picked. The Tab slip then shows the order, you pack the box on the counter, and "E 싣고 타기" starts the order to the teahouse.
- **Board view:** the capture is 1920×1080, which is the 640×360 screen at 3×. Text is about 17 px on the 360 screen, readable, and all four board edges are visible. "택배 보관소 앞" and "택배 보관소 칠판" are in the debug window's place list, and jumping to the board place shows the "E 칠판 보기" prompt. In this scene the depot route replaces the old feature-2 bot route. Play is off, fonts are reverted, and no shared .mat files changed; the scene is saved.
- **Left over:**
- **Yard step:** the depot yard is 0.8–1.7 m higher than the road, so yo

## 11/02 16:11 ⚠️ 보류  새 세션. 먼저 editor stop·폰트. 커밋 금지. 시간 상자: 20분 뼈대, 25분마다 갱신, 80분 넘으면 마무리. ()
cost=$7.02
문 앞 건네기 장면과 화물 3단 반응을 붙였습니다. 3단 검사와 봇 1회 완주 모두 정산창은 0번 떴습니다. 테스트할 때마다 Play를 새로 켜서, 시간 초과·펑크 같은 실패 결과에서는 여전히 옛 정산창이 뜹니다.
보고서: `docs/team/reports/programmer-20261102-1543.md`
**3줄 요약**
1. 흐름: 찻집 앞에서 E로 내리면 회색 상자를 들고 걷습니다 → 문에서 「E 두드리기」 → 똑똑 → 줄리아(보라 캡슐)가 나와 인사 → 반응 줄로 넘어갈 때 상자를 건네고 손 옆에 `+N원`이 떠오릅니다.
2. 화물 상태 3단(가설): 기준은 80% / 40%, 돈 비율은 1.0 / 0.7 / 0.3. HP를 100 / 60 / 15%로 바꿔 3번 돌렸고, 반응 줄과 돈(+1,200 / +840 / +360원)이 각각 다르게 나왔습니다. 콘솔 에러는 0입니다.
3. 받는 사람이 화면 밖일 때: 문 반대쪽을 보다가 두드려도 시선이 돌아와 줄리아가 화면 가운데에 들어옵니다. 봇도 끝까지 갔고, 오는 길에 화물이 53%가 되어 HP를 건드리지 않고 「조금 상함 +840원」이 나왔습니다.
**도현이 확인하는 법**

## 11/02 16:35 ⚠️ 보류  새 세션. 먼저 editor stop·폰트. 커밋 금지. 40분 안. ()
cost=$1.88
목적지 깃발이 멀리서 17×24 로 커졌고, 4쌍 전/후 캡처와 봇 1회까지 끝냈습니다. 콘솔 에러는 0이고 Play 는 꺼져 있으며, 씬은 저장하지 않았습니다.
- **크기**: 25 m 보다 멀면 큰 깃발(17×24), 가까우면 원래 13×18 입니다. 값은 상수라 씬에 남지 않습니다. 캡처에서 천만 재면 9×7 → 13×9 였고, 면적은 최대 0.82 %(과제 기준 4 % 이내)입니다.
- **받침 상자**: 가장자리 표식 받침 상자가 노을에서 배경과 섞여(대비 1.27~1.44:1) 종이색 1px 테를 추가했고, 노을에서 5.8~6.5:1 이 됐습니다. 싫으시면 `MakeBox(Ink, Paper)` → `MakeBox(Ink, Ink)` 한 줄로 되돌립니다.
- **검증**: 오후·노을 각 4쌍(출발·갈림길 2곳·도착 직전)에 처음 보는 지점 3곳을 더 찍었고, 증거는 `docs/qa/evidence/dest-flag-size/`(`pair_*.png`)에 있습니다. 봇은 찻집에 도착(도착=True)했고 막힘 9곳은 기존 흐름의 경사·끼임입니다. 25↔17 m 경계 왕복 시험은 안 돌렸습니다.
- **판정은 도현 Play 로**: 큰 깃발도 천은 화면 높이의 2.5 % 라 "충분히 큰가"는 도현이 정합니다.
보고 파일: `docs/team/reports/programmer-20261102-1616.md`
md5sum: failed to set file descriptor text/binary mode: Bad file descriptor

## 11/02 16:46 ⚠️ 보류  새 세션. 먼저 editor stop·폰트. 커밋 금지. 40분 안. ()
cost=$1.69
깃발 2단 작업을 마쳤습니다. 이전 시도(1616)가 구현과 증거를 이미 남겨 둬서, 그걸 검산하고 한 군데를 고쳐 다시 찍었습니다.
1. **깃발 크기**: 25 m 보다 멀면 17×24(높이 24, 최소 20 기준 넘음), 가까우면 원래 13×18, 15 m 안은 '여기' 칩입니다. 화면 면적은 최대 0.82 %(한도 4 %)이고, 오후·노을 모두 같은 카메라로 전/후를 찍었습니다. 가장자리 받침 상자에는 종이색 1px 테를 둘렀습니다. 노을에서 배경과 대비가 1.3:1 에서 5.8~6.5:1 로 올랐습니다. 다만 오후 밝은 길(p1)에서는 테 대비가 1.82:1 로 낮고, 여기선 잉크 본체(2.01:1)가 배경과 갈라 줍니다.
2. **이번에 바꾼 것**: 큰 깃발에서 원래 깃발로 돌아가는 여유 거리를 8 m 에서 3 m 로 줄였습니다(`DotScreenUI.cs:76`). 8 m 일 때는 다가갈 때 원래 크기가 17~15 m 사이 2 m 에서만 보여서 2단이 사실상 1단이었습니다. 지금은 22~15 m 에서 원래 크기가 보입니다. 거리를 앞뒤로 오가는 시험에서 크기가 4번 바뀌었고, 정해진 경계 밖에서 바뀐 경우는 0번입니다.
3. **확인**: 봇 1회는 찻집 도착=True, 콘솔 에러 0, Play 꺼짐, 폰트 되돌림, 공유 .mat 변경 0입니다. 봇이 8곳에서 막혔다가 후진으로 풀렸고 화물이 6 %로 깨졌는데, 경사·끼임 탓이라 이 변경과는 무관합니다.
남은 것:
- 큰 깃발이 충분히 큰지는 도현 Play 판정입니다.
- 25/22 m 경계값은 가설이라, QA 가 찻집 앞 20~27 m 를 전진·후진하며 깜빡임을 눈으로 봐야 합니다.

## 11/02 17:46 ⚠️ 보류  새 세션. 먼저 editor stop·폰트. 커밋 금지. 시간 상자: 20분 뼈대, 25분마다 갱신, 80분 넘으면 마무리. ()
cost=$9.97
작업 끝났습니다. 보고서: `docs/team/reports/programmer-20261102-1651.md`
- **연결한 것:** 보관소 카운터에 Proto_PackBench 작업대(PackBench·PackBox·PackItem 코드 공용)를 붙였습니다. 칠판에서 주문을 고르면 달걀판이 작업대에 놓이고, [E]로 포장 시점에 들어가 흔든 뒤 Q로 물러나서, [E]로 상자를 들고 카트에 [E]로 싣는 흐름이 이어집니다. Proto 씬과 건물 위치는 건드리지 않았고, Play는 꺼져 있으며 씬 저장과 폰트 되돌림까지 끝냈습니다.
- **"대충 vs 꽉" 검사:** 출발 화물 상태가 대충 싸면 **79%**(`bare4/route_feature2.txt:99`, 이전 회 81%), 꽉 싸면 **100%**(`full2:99`)로 달라졌습니다. 처음 정한 수치로는 94% 대 100%라 차이가 작아서 덜그럭 감점을 키웠습니다(가설값). 봇은 찻집까지 두 번 다 끝까지 갔고, 콘솔 에러는 0입니다.
- **고친 것과 남은 것:**
- 첫 실행에서 작업대 [E] 안내가 안 떴습니다. 작업대 판과 카운터가 같은 높이라 카운터가 먼저 잡혀서였고, 시선 판정을 고친 뒤 다섯 번 모두 통과했습니다.
- 카운터는 충돌면이 보이는 윗면보다 23cm 높고, 위에 건물 소품이 줄지어 있어서 상자와 신문이 떠 보였습니다. 건물은 그대로 두고 그 위에 카운터 재질의 판을 깔아 덮었습니다. 다만 작업면에 초록 줄무늬가 보입니다(`bare4/d_08a_bench_view.png`). 판을 그대로 둘지, 아트 쪽에서 카운터 소품을 뺄지는 도현이 정해야 합니다.
- 시점 전환은 봇 기준 카메라 오차 0cm입니다. 사람 손으로 마우스 커서가 제대로 돌아오는지는 아직 확인하지 않았습니다.

## 11/02 15:15~17:46 (3132b93a — 하네스 오판정으로 보류됐던 것 PD 가 묶어 커밋)
- 택배 보관소 주문 칠판·정비소/택배 흐름 — 보고서 programmer-20261102-1515.md, 증거 docs/qa/evidence/depot-orders/
- 문 앞 건네기·결과 반응 3단 — programmer-20261102-1543.md, docs/qa/evidence/handover/
- 목적지 깃발 크기 — programmer-20261102-1616.md·1635.md, docs/qa/evidence/dest-flag-size/
- 포장 작업대 보관소 안으로 — programmer-20261102-1651.md, docs/qa/evidence/depot-packbench/
