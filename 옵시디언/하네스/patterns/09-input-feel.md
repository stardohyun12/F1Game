# 09. Input Feel

## 증상
점프 키 누르고 한 frame 뒤 점프. 동시키(Shift+W) 한쪽만 인식. 마우스 드래그 시작은 빠른데 끝부분이 끊김. coyote time/jump buffer/dead zone 같은 미세 보정 누락. AI는 코드는 짰지만 "손맛"이 없음.

## 왜 발생 (AI 한계)
- 입력 반응성은 ms 단위 체감. 코드로 보면 "키 누르면 처리"라 OK처럼 보임.
- 좋은 컨트롤러는 input buffering, coyote time, dead zone, accel curve 등 세부 보정 필요. AI는 표준 패턴 추가하지만 정확한 ms는 모름.
- 게임패드 deadzone, 키보드 동시키, 마우스 raw vs smoothed — 입력 디바이스별로 다름.

## 회피책
- **즉시**: 입력 코드 짤 때 input buffering(0.1초), coyote time(0.1초), dead zone(0.2) 같은 표준 보정을 *기본*으로 넣음. 값은 사용자가 튜닝.
- **워크플로**: 입력 변경은 사용자가 직접 손맛 검증. AI에게 "OK?" 묻지 말기.
- **도구**: 입력 debug overlay — 현재 입력 vector, 마지막 이벤트 timestamp 표시. on/off 토글.
- **AI 룰**: 점프·대시·attack 같은 핵심 입력은 buffering·grace window 코드를 *생성 시점에* 넣음. 추가 요청 기다리지 말 것.

## 적용 신호
- 캐릭터 컨트롤러 — 점프·대시·이동
- 콤보 시스템
- 게임패드 지원 추가
- 마우스 드래그·click·release 흐름
- 메뉴 nav (방향키 hold repeat rate 등)
- "반응이 굼떠요" / "씹혀요" 사용자 보고
