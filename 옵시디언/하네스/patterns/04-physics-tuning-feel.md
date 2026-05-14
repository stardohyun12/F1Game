# 04. Physics Tuning Feel

## 증상
AI가 "차량 acceleration=15, drag=2.5, maxSpeed=40"으로 설정. 컴파일·실행 통과. 그런데 차가 둥둥 떠다니거나, 너무 무겁거나, 코너에서 통제 안 됨. AI에게 "느낌 어때?" 물어도 "잘 작동할 거예요" 추측 답.

## 왜 발생 (AI 한계)
- 물리·튜닝은 60fps 손맛이 정답. 숫자 자체로는 옳고 그름 없음.
- AI는 reference 데이터(인기 게임 값) 추측만. 실제 게임 컨텍스트(시야, 카메라 거리, 입력 곡선)와 무관.
- "약간 둔하다"/"너무 빠르다"는 정성적이라 AI가 정답 모름.

## 회피책
- **즉시**: 튜닝 값 변경 시 사용자에게 "이 값으로 실행하고 어떤 느낌인지 알려줘" 요청. 단독으로 종료 금지.
- **워크플로**: 튜닝 PR은 "before/after gif" 또는 "before/after 운전 30초" 첨부 의무.
- **도구**: 실시간 튜닝 패널 (in-game slider for drag/accel) — 사용자가 플레이 중 조정 → AI는 결과만 받음.
- **AI 룰**: 튜닝 값 변경할 땐 1개씩만. "5개를 모두 -20%로 줄였어요"는 사용자가 무엇이 효과인지 분리 못 함.

## 적용 신호
- 차량/캐릭터 컨트롤러 코드
- damage/health/cooldown 밸런싱
- camera follow lerp speed, FOV 변경
- jump height, gravity, drag, friction 등 PhysicsMaterial 값
- AI가 "이 정도면 적당할 것 같습니다" 톤
