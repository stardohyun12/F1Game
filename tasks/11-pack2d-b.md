[EDITOR][포장 2D 작업대 ②/③ — 롤 뜯기·넣기·덮기·테이프·흔들기·결과 (D-294)]
10(03c67c26) 위에 이어서. SPEC §6-4 그대로. 시간 상자 30분, Play 2회, 캡처 1장. 여는 파일: Interactables/Pack2D/PackBoard.cs·PackPiece.cs, Presentation/PackBoardView.cs. Core/PackRules.cs 는 **그대로 쓴다**(FitOf·StartIntegrity, 바꾸지 않음).
① 롤(뽁뽁이·종이)을 끌면 한 장이 뜯겨 나와 손에(롤에 남은 양 줄어듦). 열린 상자 위에 놓으면 바닥에 깔림(첫 장) / 물건 위에 덮임(물건 넣은 뒤). ② 물건을 열린 상자 위로 끌면 들어감 — 소 상자는 안 들어가고 튕김(PackRules.FitOf), 대 상자는 들어가되 헐거움. R 로 90° 회전. ③ 테이프를 끌어 상자 위를 가로지르면 뚜껑 닫히고 테이프 줄 1개(드래그 궤적). ④ 닫힌 상자를 집고 마우스를 흔들면 흔들림·소리(기존 PackBox 흔들기 소리 재사용), 헐거우면 덜그럭 큼. ⑤ 상자를 판 밖(아래)으로 끌면 "들고 나감" = 기존 BoxTaken 신호 → Core StartIntegrity 로 화물 시작값. 안내 글 0.
확인: 컴파일 0 · world_check 통과 · DayLoopCheck Pack2D 줄을 끝까지(롤→넣기→덮기→테이프→흔들기→나감, SimDrag) + 시작값 로그(중 상자+뽁뽁이 = 90%) · 캡처 1장(테이프 붙은 상자). 보고 4줄 + 도현 마우스 순서 4줄.
