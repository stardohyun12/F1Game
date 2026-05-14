# 02. Scene/Inspector Wiring

## 증상
AI가 `[SerializeField]`로 의존 잡고 "자동 와이어링" 코드 짜놨는데, 실행하면 NullRef. 원인: prefab/.unity YAML의 fileID 참조가 비어 있거나, 사용자가 드래그 깜빡함.

## 왜 발생 (AI 한계)
- 씬·prefab YAML은 길고 fileID·GUID 참조라 AI가 정독 안 함 (context overhead).
- "이 컴포넌트는 어디 GameObject에 붙어있고 어떻게 연결되어 있나"를 코드만 보고는 모름.
- AI는 코드 짜는 데 집중하지 "에디터에서 드래그하세요" 단계를 누락하기 쉬움.

## 회피책
- **즉시**: 새 `[SerializeField]` 추가하면 마지막에 "에디터에서 X에 Y 드래그 필요" 명시 메시지.
- **워크플로**: SerializeField 의존이 깊은 컴포넌트는 `Awake`에 자가구성(create-if-null) 폴백 추가. 인스펙터 비어도 동작.
- **도구**: 엔진 CLI로 prefab/씬 inspect (`unity-cli exec "var go = GameObject.Find(\"X\"); ..."`). YAML grep으로 fileID 0 (`m_GameObject: {fileID: 0}`) 검사.
- **AI 룰**: 코드에 `[SerializeField] private Foo foo;` 추가 시 → 와이어링 누락 시 동작이 보장되는지(폴백 또는 명시 에러) 항상 확인.

## 적용 신호
- "NullReferenceException at Awake/Start" 런타임 에러
- "왜 안 돌아가지" 사용자 보고에 코드는 멀쩡할 때
- 새 컴포넌트를 씬에 처음 배치한 직후
- 다른 씬으로 옮긴 직후
