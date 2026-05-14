# 01. Visual Blindness

## 증상
컴파일 통과·테스트 통과해도 화면에서 박스가 땅에 박혀 있거나, 머티리얼이 핑크(셰이더 오류), z-fight, scale 어긋남, 카메라 시야 밖에 스폰. AI는 "구현 끝났습니다" 보고하지만 실제 결과를 못 봄.

## 왜 발생 (AI 한계)
AI는 텍스트만 처리. 렌더링·라이트·머티리얼은 binary + GPU 결과라 정적 분석 불가능. 코드 path가 맞아도 결과 비주얼이 어떨지는 추측.

## 회피책
- **즉시**: 작업 끝났다 보고 전에 사용자에게 "지금 실행해서 화면 확인해줘" 명시 요청. AI 자체 종료 금지.
- **워크플로**: UI/시각 변경은 자동화 가능한 스크린샷 또는 manual screenshot 검증을 task 완료 조건에 박아 둠.
- **도구**: 엔진별 CLI로 screenshot 또는 in-editor 검증 (`unity-cli screenshot`, Unreal `HighResShot`, Godot `--screenshot`).
- **AI 룰**: "정적 분석으로 시각 결과 추측 금지. screenshot 또는 manual play 없이 '완료' 보고 금지."

## 적용 신호
이 상황이면 의심:
- 시각 컴포넌트 변경 (mesh, material, shader, light, particle, UI layout, camera)
- 좌표·회전·scale 코드 수정
- AI가 "OK 됐을 거예요" 추측 톤으로 종료할 때
- 사용자가 "실행해보니 X가 이상" 보고하는데 AI가 코드 위주로만 fix 시도할 때
