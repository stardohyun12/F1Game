[EDITOR][디버그 창 — 「바다로 가서 낚시」 버튼 하나 (도현 11/05)]
도현 원문: "바다로 가서 낚시 하는 버튼만 만들고 멈춰봐". 여는 파일: Debug/DebugWindow.cs 만. 시간 상자 20분, Play 2회, 캡처 1장.
정적 함수 `GoFish()` = GoPlace(「노을 낚시 자리」 인덱스를 이름으로 찾음) → SetDusk(1) → 앉기 단계가 필요하면 자동 → Fish(). 디버그 창 맨 위에 큰 버튼 「바다로 가서 낚시」. 누르면 낚싯줄이 바다에 들어간 노을 장면이 되어야 함.
확인: 컴파일 0 · world_check · Play 에서 버튼 함수 호출 뒤 1× 캡처 1장(낚시 중) `docs/qa/evidence/debug-gofish/fish.png`. 보고 2줄.
