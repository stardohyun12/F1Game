# Scene A2 kit — 임포트 규격

- `kit.json`의 28개 FBX+동명 PNG. 각 파일은 독립 물건이며 서로 겹쳐 원점에 놓는 장면 파일이 아니다.
- FBX Y-up, 1unit=1m. Blender export axis_forward=-Z, axis_up=Y. 바닥 바운딩박스 가운데 피벗. 높이·폭·깊이와 앞/중/원경 권장은 kit.json.
- `pole_01`은 원본 그대로 **전신주3개+연결 전선 한 묶음**. 전체6° 기울임, 길이35.24m. 3개 파일을 놓아9개로 만들지 말 것. 나머지2개 기울기 시험본은 최종 kit에서 제외.
- V5와 동일한 `palette32.gpl` 32색, 디더 없음. 원래 텍스처와 형상 유지, 건물 지붕/벽만 재착색. Cycles64samples DIFFUSE+별도AO, V5 낮은 따뜻한 해+청록 하늘빛, 노출+1stop 후 양자화.
- 고유 베이크 UV: 약32texel/m(작은 면 최소2텍셀), UV0. Point / sRGB / 비압축 / 밉맵 끔으로 비교. Unlit BaseMap에 동명 PNG, 틴트 흰색.
- **잎·철망 등 RGBA 알파 컷아웃은0.5, 양면 표시.** 반투명 블렌딩이 아닌 Alpha Clip 사용. FBX에서 Unity Unlit·양면·Alpha Clip 자동 설정은 보장하지 않는다.
- 물건 자체 조명과 AO가 구워져 있다. 같은 방향 배치 권장. 회전하면 명암 방향도 회전한다. 실제 지면 접지 그림자와 물건끼리의 그림자는 포함하지 않았다.
- 일람 시트는 모든 물건을 같은24px/m로 렌더. 사람1.7m 막대는 투영 높이 약40px. 화면에 맞추려고 작은 물건을 키우지 않았다.
- 모든 원본은 art/thirdparty에서 읽기만 했다. 원본 파일 해시·가공 내용·라이선스는 kit.json / CREDITS.md / art/provenance.csv.
- CC-BY 사용품: Bicycle, Telephone pole. 요청된5개 전체 크레딧 문구는 CREDITS.md에 보존(미사용3개 구분). 크레딧과 수정 표시를 게임 배포에 유지할 것.
- Unity 실행·임포트·최종 룩 판정 미실행. QA/프로그래밍 임포트 필요. D-169에 따라 실제 Unity720 화면에서 최종 판정.

- chillydurango 창고2채는 베이크 암부/컷아웃 결함으로 보류. 최종 건물4채는 valsekamerplant 원형이며 보류 파일은 art/models/garage_scene_a2_kit/iterations-held-sheds/에 보존.
